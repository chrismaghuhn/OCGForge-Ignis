using System.Buffers.Binary;
using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using OCGForge.Ignis.Client;
using OCGForge.Ignis.Gameplay;
using OCGForge.Ignis.Model;
using OCGForge.Ignis.Protocol;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;
using static OCGForge.Ignis.Gameplay.Tests.GameplayMessageFixtures;
using static OCGForge.Ignis.Gameplay.Tests.MirrorFixtures;
using static OCGForge.Ignis.Gameplay.Tests.ModernQueryFixtures;
using static OCGForge.Ignis.Gameplay.Tests.TransportFixtures;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I6CPublicFrameSourceTests
{
    internal static void TestI6DFrameOwnedCrossLocatorImplementation()
    {
        I6DCompositionFixture fixture = CreateI6DCompositionFixture();
        try
        {
            FlatPublicCandidateDescriptorV1 firstCandidate =
                fixture.PromptProjection.Candidates![0];
            True(fixture.Composition.Handoff!.TryGetValidatedTarget(
                    firstCandidate,
                    fixture.Composition.Frame,
                    out PublicSemanticLocatorV1? firstTarget,
                    out I6DPrivateCrossLocatorHandoffErrorV1? targetError),
                targetError?.ToString() ?? "target rejected");
            Equal("p0:HAND:0", firstTarget!.Value);

            OcgForgeAcceptedDecisionBoundaryProducerV1 noHandoffProducer =
                new();
            False(noHandoffProducer.TryAccept(
                    fixture.Composition.Frame,
                    fixture.PromptProjection,
                    null,
                    out OcgForgeAcceptedDecisionBoundaryV1? noHandoffBoundary,
                    out OcgForgeAcceptedDecisionBoundaryProducerErrorV1? noHandoffError),
                noHandoffError?.ToString() ?? "missing handoff was accepted");
            Null(noHandoffBoundary);
            Equal(
                OcgForgeAcceptedDecisionBoundaryProducerErrorCodeV1.HandoffRejected,
                noHandoffError!.Value.Code);

            OcgForgeAcceptedDecisionBoundaryProducerV1 producer = new();
            True(producer.TryAccept(
                    fixture.Composition.Frame,
                    fixture.PromptProjection,
                    fixture.Composition.Handoff,
                    out OcgForgeAcceptedDecisionBoundaryV1? boundary,
                    out OcgForgeAcceptedDecisionBoundaryProducerErrorV1? boundaryError),
                boundaryError?.ToString() ?? "boundary rejected");
            NotNull(boundary);
            OcgForgePublicDecisionContextResultV1 mapped =
                OcgForgePublicCandidateBridgeV1.TryCreate(boundary);
            True(mapped.IsSuccess, mapped.Error?.ToString() ?? "bridge rejected");
            NotNull(mapped.Context);
            Equal(2, mapped.Context!.Candidates.Count);
            Equal(
                "p0:HAND:0",
                mapped.Context.Candidates[0].Descriptor.SourceReference!
                    .Value.ObservationLocator);
            Equal(
                "p0:HAND:1",
                mapped.Context.Candidates[1].Descriptor.SourceReference!
                    .Value.ObservationLocator);
            False(mapped.Context.Candidates.Any(candidate =>
                candidate.PublicActionKey.Contains(
                    "11223344",
                    StringComparison.Ordinal)));
        }
        finally
        {
            DisposeI6C5Session(fixture.Session, fixture.Consumer);
        }
    }

    internal static void TestI6DSameOccurrenceCanHaveMultipleActions()
    {
        I6DCompositionFixture fixture = CreateI6DCompositionFixture(
            promptMessage: SameOccurrenceMultipleIdleActionsMessage(
                0x11223344));
        try
        {
            Equal(2, fixture.PromptProjection.Candidates!.Count);
            Equal(
                fixture.PromptProjection.Candidates[0]
                    .I4LocalCandidateKey,
                "MSG_SELECT_IDLECMD:SUMMON:0");
            Equal(
                fixture.PromptProjection.Candidates[1]
                    .I4LocalCandidateKey,
                "MSG_SELECT_IDLECMD:MSET:0");

            OcgForgePublicDecisionContextResultV1 mapped =
                CreateMappedI6DContext(fixture);
            True(mapped.IsSuccess,
                mapped.Error?.ToString() ?? "same-occurrence bridge rejected");
            NotNull(mapped.Context);
            Equal(2, mapped.Context!.Candidates.Count);
            Equal(
                "p0:HAND:0",
                mapped.Context.Candidates[0].Descriptor.SourceReference!
                    .Value.ObservationLocator);
            Equal(
                "p0:HAND:0",
                mapped.Context.Candidates[1].Descriptor.SourceReference!
                    .Value.ObservationLocator);
            NotEqual(
                mapped.Context.Candidates[0].PublicActionKey,
                mapped.Context.Candidates[1].PublicActionKey);
        }
        finally
        {
            DisposeI6C5Session(fixture.Session, fixture.Consumer);
        }
    }

    internal static void TestI6DNonIdleLocatorFamiliesUseHandoff()
    {
        I6DCompositionFixture fixture = CreateI6DCompositionFixture();
        try
        {
            PublicSemanticLocatorV1 i4Locator =
                fixture.I4Projection.PrivateOccurrenceSidecar!
                    .Entries.Single(entry => entry.SourceSequence == 0)
                    .AcceptedI4PublicLocator;

            (FlatPromptPublicContextV1 Context,
                FlatPublicCandidateDescriptorV1 Candidate,
                string Key,
                string ExpectedActionKind)[] cases =
            {
                (
                    new FlatPromptEffectYnPublicContextV1(
                        0,
                        i4Locator,
                        42),
                    new FlatEffectYnPublicCandidateDescriptorV1(
                        FlatPromptKeyV1.EffectYnNo,
                        FlatPromptChoiceKindV1.No),
                    FlatPromptKeyV1.EffectYnNo,
                    "yes_no"),
                (
                    new FlatPromptChainPublicContextV1(
                        0,
                        0,
                        true,
                        0,
                        0),
                    new FlatChainPublicCandidateDescriptorV1(
                        "MSG_SELECT_CHAIN:CHAIN_ENTRY:0",
                        0,
                        i4Locator,
                        42,
                        0),
                    "MSG_SELECT_CHAIN:CHAIN_ENTRY:0",
                    "chain"),
                (
                    new FlatPromptBattlePublicContextV1(0),
                    new FlatBattleActivatablePublicCandidateV1(
                        "MSG_SELECT_BATTLECMD:ACTIVATE:0",
                        0,
                        i4Locator,
                        42,
                        0),
                    "MSG_SELECT_BATTLECMD:ACTIVATE:0",
                    "battle_command"),
                (
                    new FlatPromptCardSelectionPublicContextV1(
                        0,
                        1,
                        1,
                        false),
                    new FlatPromptCardSelectionLocatorCandidateV1(
                        FlatPromptKeyV1.SelectCardPickPrefix + "0",
                        0,
                        i4Locator),
                    FlatPromptKeyV1.SelectCardPickPrefix + "0",
                    "card_selection"),
                (
                    new FlatPromptTributeSelectionPublicContextV1(
                        0,
                        1,
                        1,
                        false),
                    new FlatPromptTributeSelectionLocatorCandidateV1(
                        FlatPromptKeyV1.SelectTributePickPrefix + "0",
                        0,
                        i4Locator),
                    FlatPromptKeyV1.SelectTributePickPrefix + "0",
                    "pick"),
                (
                    new FlatPromptSelectUnselectCardPublicContextV1(
                        0,
                        true,
                        true,
                        1,
                        1,
                        1,
                        0),
                    new FlatPromptSelectUnselectLocatorCandidateV1(
                        FlatPromptKeyV1.SelectUnselectSelectPrefix + "0",
                        FlatPromptChoiceKindV1.Select,
                        FlatPromptSourceSectionV1.Selectable,
                        0,
                        i4Locator),
                    FlatPromptKeyV1.SelectUnselectSelectPrefix + "0",
                    "card_selection"),
                (
                    new FlatPromptSortSelectionPublicContextV1(
                        0,
                        FlatPromptSortKindV1.SortCard,
                        new FlatPromptSortSourcePublicDescriptorBaseV1[]
                        {
                            new FlatPromptSortSourceLocatorPublicDescriptorV1(
                                0,
                                i4Locator)
                        }),
                    new FlatPromptSortLocatorPublicCandidateV1(
                        FlatPromptKeyV1.SortCardPlacePrefix + "0",
                        FlatPromptFamilyValueV1.MsgSortCard,
                        0,
                        i4Locator),
                    FlatPromptKeyV1.SortCardPlacePrefix + "0",
                    "pick"),
                (
                    new FlatPromptCounterSelectionPublicContextV1(
                        0,
                        1,
                        1,
                        new FlatPromptCounterSourcePublicDescriptorV1[]
                        {
                            new FlatPromptCounterSourcePublicDescriptorV1(
                                0,
                                1,
                                i4Locator)
                        }),
                    new FlatPromptCounterAmountPublicCandidateV1(
                        FlatPromptKeyV1.SelectCounterAssignAmountPrefix + "0:1",
                        0,
                        1),
                    FlatPromptKeyV1.SelectCounterAssignAmountPrefix + "0:1",
                    "assign_amount")
            };

            foreach ((FlatPromptPublicContextV1 Context,
                         FlatPublicCandidateDescriptorV1 Candidate,
                         string Key,
                         string ExpectedActionKind) testCase in cases)
            {
                FlatPromptProjectionResultV1 projection =
                    FlatPromptProjectionResultV1.Success(
                        testCase.Context,
                        new[] { testCase.Candidate });
                True(fixture.Session.TryGetCurrentFrameAuthority(
                        out PrivateGameplayFrameAuthorityV1? authority));
                NotNull(authority);
                True(CurrentFlatPromptBindingV1.TryCreate(
                        99,
                        testCase.Context.PromptFamily,
                        new[] { testCase.Candidate },
                        new[] { testCase.Key },
                        new[] { 0 },
                    out CurrentFlatPromptBindingV1? binding,
                    out FlatPromptErrorCodeV1 bindingError,
                    frameAuthority: authority,
                    acceptedProjection: projection),
                    $"{testCase.Context.PromptFamily}/{testCase.Key}: " +
                    bindingError);
                NotNull(binding);

                PrivateI6DFrameCompositionResultV1 source =
                    PerspectiveSafePublicFrameSourceV1.TryCreateI6DFrame(
                        fixture.Session.Mirror,
                        CreateValidI6C5MatchContext(),
                        fixture.Provider);
                True(source.FrameResult.IsSuccess,
                    source.FrameResult.Error?.ToString() ??
                    "I6C5 source rejected");
                True(I6DPrivateCrossLocatorBindingHandoffV1.TryCreate(
                        authority!,
                        binding!,
                        projection,
                        fixture.I4Projection,
                        source.FrameResult.Frame!,
                        authority!.MirrorSnapshot,
                        source.LocatorMap!,
                        out I6DPrivateCrossLocatorBindingHandoffV1? handoff,
                        out I6DPrivateCrossLocatorHandoffErrorV1? handoffError),
                    handoffError?.ToString() ??
                    "locator-family handoff rejected");
                NotNull(handoff);

                OcgForgeAcceptedDecisionBoundaryProducerV1 noHandoffProducer =
                    new();
                False(noHandoffProducer.TryAccept(
                        source.FrameResult.Frame,
                        projection,
                        null,
                        out OcgForgeAcceptedDecisionBoundaryV1?
                            noHandoffBoundary,
                        out OcgForgeAcceptedDecisionBoundaryProducerErrorV1?
                            noHandoffError));
                Null(noHandoffBoundary);
                Equal(
                    OcgForgeAcceptedDecisionBoundaryProducerErrorCodeV1
                        .HandoffRejected,
                    noHandoffError!.Value.Code);

                True(noHandoffProducer.TryAccept(
                        source.FrameResult.Frame,
                        projection,
                        handoff,
                        out OcgForgeAcceptedDecisionBoundaryV1?
                            recoveredBoundary,
                        out OcgForgeAcceptedDecisionBoundaryProducerErrorV1?
                            recoveredError),
                    recoveredError?.ToString() ??
                    "handoff retry was rejected");
                Equal(0ul, recoveredBoundary!.DecisionIndex);

                OcgForgeAcceptedDecisionBoundaryProducerV1 producer = new();
                True(producer.TryAccept(
                        source.FrameResult.Frame,
                        projection,
                        handoff,
                        out OcgForgeAcceptedDecisionBoundaryV1? boundary,
                        out OcgForgeAcceptedDecisionBoundaryProducerErrorV1?
                            boundaryError),
                    boundaryError?.ToString() ??
                    "locator-family boundary rejected");
                OcgForgePublicDecisionContextResultV1 mapped =
                    OcgForgePublicCandidateBridgeV1.TryCreate(boundary);
                True(mapped.IsSuccess,
                    mapped.Error?.ToString() ??
                    "locator-family bridge rejected");
                Equal(
                    testCase.ExpectedActionKind,
                    mapped.Context!.Candidates[0].Descriptor.ActionKind);
                Equal(
                    "p0:HAND:0",
                    mapped.Context.Candidates[0].Descriptor.SourceReference!
                        .Value.ObservationLocator);
            }
        }
        finally
        {
            DisposeI6C5Session(fixture.Session, fixture.Consumer);
        }
    }

    internal static void TestI6DBoundaryHandoffCurrentnessAndAtomicity()
    {
        I6DCompositionFixture fixture = CreateI6DCompositionFixture();
        try
        {
            I6DPrivateCrossLocatorBindingHandoffV1 handoff =
                fixture.Composition.Handoff!;
            PerspectiveSafeFrameSourceResultV1 equivalentFrameResult =
                fixture.Session.TryCreateI6C5Frame();
            True(equivalentFrameResult.IsSuccess);

            False(handoff.TryAcquireBoundaryAcceptanceLease(
                    equivalentFrameResult.Frame,
                    fixture.PromptProjection,
                    out I6DBoundaryAcceptanceLeaseV1? mismatchedFrameLease,
                    out I6DPrivateCrossLocatorHandoffErrorV1? frameError));
            Null(mismatchedFrameLease);
            Equal(
                I6DPrivateCrossLocatorHandoffErrorCodeV1.FrameMismatch,
                frameError!.Value.Code);

            FlatPromptProjectionResultV1 detachedProjection =
                FlatPromptProjectionResultV1.Success(
                    fixture.PromptProjection.Context!,
                    fixture.PromptProjection.Candidates!);
            False(handoff.TryAcquireBoundaryAcceptanceLease(
                    fixture.Composition.Frame,
                    detachedProjection,
                    out I6DBoundaryAcceptanceLeaseV1? mismatchedProjectionLease,
                    out I6DPrivateCrossLocatorHandoffErrorV1? projectionError));
            Null(mismatchedProjectionLease);
            Equal(
                I6DPrivateCrossLocatorHandoffErrorCodeV1.ProjectionMismatch,
                projectionError!.Value.Code);

            OcgForgeAcceptedDecisionBoundaryProducerV1 producer = new();
            False(producer.TryAccept(
                    equivalentFrameResult.Frame,
                    fixture.PromptProjection,
                    handoff,
                    out OcgForgeAcceptedDecisionBoundaryV1? rejectedBoundary,
                    out OcgForgeAcceptedDecisionBoundaryProducerErrorV1? rejectedError));
            Null(rejectedBoundary);
            Equal(
                OcgForgeAcceptedDecisionBoundaryProducerErrorCodeV1.HandoffRejected,
                rejectedError!.Value.Code);

            True(producer.TryAccept(
                    fixture.Composition.Frame,
                    fixture.PromptProjection,
                    handoff,
                    out OcgForgeAcceptedDecisionBoundaryV1? acceptedBoundary,
                    out OcgForgeAcceptedDecisionBoundaryProducerErrorV1? acceptedError),
                acceptedError?.ToString() ?? "valid handoff was rejected");
            Equal(0ul, acceptedBoundary!.DecisionIndex);

            FlatPublicCandidateDescriptorV1 candidate =
                fixture.PromptProjection.Candidates![0];
            True(handoff.TryGetValidatedTarget(
                    candidate,
                    fixture.Composition.Frame,
                    out PublicSemanticLocatorV1? currentTarget,
                    out I6DPrivateCrossLocatorHandoffErrorV1? currentError),
                currentError?.ToString() ?? "current target was rejected");
            Equal("p0:HAND:0", currentTarget!.Value);

            True(ApplyI6C4ThroughSession(
                    fixture.Session,
                    fixture.Transport,
                    new byte[] { 40, 0 }).IsSuccess);
            False(handoff.TryGetValidatedTarget(
                    candidate,
                    fixture.Composition.Frame,
                    out PublicSemanticLocatorV1? staleTarget,
                    out I6DPrivateCrossLocatorHandoffErrorV1? staleError));
            Null(staleTarget);
            Equal(
                I6DPrivateCrossLocatorHandoffErrorCodeV1.StaleFrame,
                staleError!.Value.Code);
        }
        finally
        {
            DisposeI6C5Session(fixture.Session, fixture.Consumer);
        }

        I6DCompositionFixture promptFixture = CreateI6DCompositionFixture();
        try
        {
            I6DPrivateCrossLocatorBindingHandoffV1 handoff =
                promptFixture.Composition.Handoff!;
            True(promptFixture.Prompt.TryAcceptFrameOwnedPrompt(
                    DuplicateOwnHandIdleMessage(0x11223344),
                    GetCurrentFrame(promptFixture.Session),
                    promptFixture.I4Projection).IsSuccess);
            FlatPublicCandidateDescriptorV1 candidate =
                promptFixture.PromptProjection.Candidates![0];
            False(handoff.TryGetValidatedTarget(
                    candidate,
                    promptFixture.Composition.Frame,
                    out PublicSemanticLocatorV1? staleTarget,
                    out I6DPrivateCrossLocatorHandoffErrorV1? staleError));
            Null(staleTarget);
            Equal(
                I6DPrivateCrossLocatorHandoffErrorCodeV1.StalePrompt,
                staleError!.Value.Code);
        }
        finally
        {
            DisposeI6C5Session(promptFixture.Session, promptFixture.Consumer);
        }
    }

    internal static void TestI6DCompositionRejectsUnprovenTargets()
    {
        I6DCompositionFixture fixture = CreateI6DCompositionFixture();
        try
        {
            True(fixture.Session.TryGetCurrentFrameAuthority(
                    out PrivateGameplayFrameAuthorityV1? authority));
            NotNull(authority);
            True(fixture.Prompt.TryGetCurrentFrameOwnedBinding(
                    authority!,
                    fixture.PromptProjection,
                    out CurrentFlatPromptBindingV1? binding));
            NotNull(binding);
            PrivateI6DFrameCompositionResultV1 source =
                PerspectiveSafePublicFrameSourceV1.TryCreateI6DFrame(
                    fixture.Session.Mirror,
                    CreateValidI6C5MatchContext(),
                    fixture.Provider);
            True(source.FrameResult.IsSuccess);
            NotNull(source.FrameResult.Frame);
            NotNull(source.LocatorMap);
            PerspectiveSafeFrameV1 sourceFrame = source.FrameResult.Frame!;
            PrivateI6C3LocatorMapV1 locatorMap = source.LocatorMap!;
            MirrorSnapshotV1 currentSnapshot = authority!.MirrorSnapshot;
            CurrentFlatPromptBindingV1 currentBinding = binding!;

            PerspectiveSafeFrameV1 missingTargetFrame = CopyPublicFrame(
                sourceFrame,
                sourceFrame.Entities.Where(entity =>
                    entity.Locator != "p0:HAND:0"));
            False(I6DPrivateCrossLocatorBindingHandoffV1.TryCreate(
                    authority!,
                    currentBinding,
                    fixture.PromptProjection,
                    fixture.I4Projection,
                    missingTargetFrame,
                    currentSnapshot,
                    locatorMap,
                    out I6DPrivateCrossLocatorBindingHandoffV1? missingHandoff,
                    out I6DPrivateCrossLocatorHandoffErrorV1? missingError));
            Null(missingHandoff);
            Equal(
                I6DPrivateCrossLocatorHandoffErrorCodeV1.TargetUnavailable,
                missingError!.Value.Code);

            PerspectiveSafeEntityV1 targetEntity = sourceFrame.Entities
                .Single(entity => entity.Locator == "p0:HAND:0");
            PerspectiveSafeFrameV1 ambiguousTargetFrame = CopyPublicFrame(
                sourceFrame,
                sourceFrame.Entities.Append(targetEntity));
            False(I6DPrivateCrossLocatorBindingHandoffV1.TryCreate(
                    authority!,
                    currentBinding,
                    fixture.PromptProjection,
                    fixture.I4Projection,
                    ambiguousTargetFrame,
                    currentSnapshot,
                    locatorMap,
                    out I6DPrivateCrossLocatorBindingHandoffV1? ambiguousHandoff,
                    out I6DPrivateCrossLocatorHandoffErrorV1? ambiguousError));
            Null(ambiguousHandoff);
            Equal(
                I6DPrivateCrossLocatorHandoffErrorCodeV1.AmbiguousBinding,
                ambiguousError!.Value.Code);

            MirrorCardSnapshotV1[] ownHandCards = currentSnapshot.Cards
                .Where(card => card.Zone == MirrorZoneV1.Hand)
                .ToArray();
            Equal(2, ownHandCards.Length);
            True(PrivateI6C3LocatorMapV1.TryCreate(
                    ownHandCards.ToDictionary(
                        card => card.EntityId,
                        _ => "p0:HAND:0"),
                    out PrivateI6C3LocatorMapV1? collidingLocatorMap));
            False(I6DPrivateCrossLocatorBindingHandoffV1.TryCreate(
                    authority!,
                    currentBinding,
                    fixture.PromptProjection,
                    fixture.I4Projection,
                    sourceFrame,
                    currentSnapshot,
                    collidingLocatorMap!,
                    out I6DPrivateCrossLocatorBindingHandoffV1?
                        collidingHandoff,
                    out I6DPrivateCrossLocatorHandoffErrorV1? collidingError));
            Null(collidingHandoff);
            Equal(
                I6DPrivateCrossLocatorHandoffErrorCodeV1.AmbiguousBinding,
                collidingError!.Value.Code);

            PerspectiveSafeEntityV1 shadowI4Locator = new(
                "p0:HAND:public:287454020:0",
                identityKnown: true,
                passcode: 0x11223344,
                owner: 0,
                controller: 0,
                zone: PerspectiveSafeSemanticZoneV1.Hand,
                sequence: 0,
                overlaySequence: null,
                position: PerspectiveSafePositionV1.Unknown,
                faceUp: false,
                faceDown: true);
            PerspectiveSafeFrameV1 frameWithShadowI4Locator = CopyPublicFrame(
                sourceFrame,
                sourceFrame.Entities.Append(shadowI4Locator));
            True(I6DPrivateCrossLocatorBindingHandoffV1.TryCreate(
                    authority!,
                    currentBinding,
                    fixture.PromptProjection,
                    fixture.I4Projection,
                    frameWithShadowI4Locator,
                    currentSnapshot,
                    locatorMap,
                    out I6DPrivateCrossLocatorBindingHandoffV1? shadowHandoff,
                    out I6DPrivateCrossLocatorHandoffErrorV1? shadowError),
                shadowError?.ToString() ?? "shadow handoff rejected");
            NotNull(shadowHandoff);
            OcgForgeAcceptedDecisionBoundaryProducerV1 shadowProducer = new();
            True(shadowProducer.TryAccept(
                    frameWithShadowI4Locator,
                    fixture.PromptProjection,
                    shadowHandoff,
                    out OcgForgeAcceptedDecisionBoundaryV1? shadowBoundary,
                    out OcgForgeAcceptedDecisionBoundaryProducerErrorV1? shadowBoundaryError),
                shadowBoundaryError?.ToString() ?? "shadow boundary rejected");
            OcgForgePublicDecisionContextResultV1 shadowContext =
                OcgForgePublicCandidateBridgeV1.TryCreate(shadowBoundary);
            True(shadowContext.IsSuccess,
                shadowContext.Error?.ToString() ?? "shadow bridge rejected");
            Equal(
                "p0:HAND:0",
                shadowContext.Context!.Candidates[0].Descriptor.SourceReference!
                    .Value.ObservationLocator);
        }
        finally
        {
            DisposeI6C5Session(fixture.Session, fixture.Consumer);
        }
    }

    internal static void TestI6DPrivateOccurrenceDoesNotChangePublicIdentity()
    {
        I6DCompositionFixture first =
            CreateI6DCompositionFixture(0x11223344);
        I6DCompositionFixture second =
            CreateI6DCompositionFixture(0x55667788);
        try
        {
            OcgForgePublicDecisionContextResultV1 firstContext =
                CreateMappedI6DContext(first);
            OcgForgePublicDecisionContextResultV1 secondContext =
                CreateMappedI6DContext(second);
            True(firstContext.IsSuccess);
            True(secondContext.IsSuccess);
            True(firstContext.Context!.Candidates.Select(
                    candidate => candidate.PublicActionKey)
                .SequenceEqual(secondContext.Context!.Candidates.Select(
                    candidate => candidate.PublicActionKey)));
            for (int index = 0;
                 index < firstContext.Context.Candidates.Count;
                 index++)
            {
                BytesEqual(
                    firstContext.Context.Candidates[index].CanonicalDescriptorBytes,
                    secondContext.Context.Candidates[index].CanonicalDescriptorBytes);
            }
            Equal(
                firstContext.Context.PublicCandidateDomainDigest,
                secondContext.Context.PublicCandidateDomainDigest);
        }
        finally
        {
            DisposeI6C5Session(first.Session, first.Consumer);
            DisposeI6C5Session(second.Session, second.Consumer);
        }
    }

    internal static void TestI6DBoundaryAcceptanceLeaseContention()
    {
        TestBoundaryAcceptanceWinsAgainstFrameAdvance();
        TestFrameAdvanceWinsAgainstBoundaryAcceptance();
    }

    internal static void TestI6DBoundaryAcceptanceWinsAgainstFrameAdvance() =>
        TestBoundaryAcceptanceWinsAgainstFrameAdvance();

    internal static void TestI6DFrameAdvanceWinsAgainstBoundaryAcceptance() =>
        TestFrameAdvanceWinsAgainstBoundaryAcceptance();

    private static void TestBoundaryAcceptanceWinsAgainstFrameAdvance()
    {
        I6DCompositionFixture fixture = CreateI6DCompositionFixture();
        PrivateGameplayFrameAuthorityV1? authority = null;
        I6DBoundaryAcceptanceLeaseV1? acceptanceLease = null;
        Task<bool>? competitorTask = null;
        TaskCompletionSource<bool>? releaseCompetitor = null;
        CancellationTokenSource? cleanup = null;
        try
        {
            True(fixture.Session.TryGetCurrentFrameAuthority(
                    out authority));
            NotNull(authority);
            True(fixture.Composition.Handoff!.TryAcquireBoundaryAcceptanceLease(
                    fixture.Composition.Frame,
                    fixture.PromptProjection,
                    out acceptanceLease,
                    out I6DPrivateCrossLocatorHandoffErrorV1? acceptanceError),
                acceptanceError?.ToString() ?? "acceptance lease rejected");

            TaskCompletionSource<bool> pumpAttempted =
                NewSignal();
            releaseCompetitor = NewSignal();
            authority!.SetTestLeaseAcquisitionHook(acquired =>
            {
                if (!acquired)
                {
                    pumpAttempted.TrySetResult(true);
                    return;
                }

                releaseCompetitor.Task.GetAwaiter().GetResult();
            });
            cleanup = new CancellationTokenSource();
            cleanup.CancelAfter(TimeSpan.FromSeconds(5));
            competitorTask = Task.Factory.StartNew(
                () =>
                {
                    using PrivateGameplayFrameAuthorityLeaseV1? lease =
                        authority.TryAcquire();
                    return lease is not null;
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
            pumpAttempted.Task.WaitAsync(cleanup.Token)
                .GetAwaiter()
                .GetResult();
            Task<bool> availabilityTask = Task.Factory.StartNew(
                authority.IsLeaseAvailableForTest,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
            False(availabilityTask.WaitAsync(cleanup.Token)
                .GetAwaiter()
                .GetResult());

            acceptanceLease!.Dispose();
            releaseCompetitor.TrySetResult(true);
            True(competitorTask.WaitAsync(cleanup.Token)
                .GetAwaiter()
                .GetResult());
            True(ApplyI6C4ThroughSession(
                    fixture.Session,
                    fixture.Transport,
                    new byte[] { 40, 0 }).IsSuccess);
        }
        finally
        {
            acceptanceLease?.Dispose();
            releaseCompetitor?.TrySetResult(true);
            authority?.SetTestLeaseAcquisitionHook(null);
            cleanup?.Cancel();

            DisposeI6C5Session(fixture.Session, fixture.Consumer);
        }
    }

    private static void TestFrameAdvanceWinsAgainstBoundaryAcceptance()
    {
        I6DCompositionFixture fixture = CreateI6DCompositionFixture();
        PrivateGameplayFrameAuthorityV1? authority = null;
        TaskCompletionSource<bool>? releasePumpLease = null;
        Task<GameplayMirrorPumpResult>? pumpTask = null;
        Task<(bool Success,
            I6DBoundaryAcceptanceLeaseV1? Lease,
            I6DPrivateCrossLocatorHandoffErrorV1? Error)>? handoffTask = null;
        CancellationTokenSource? cleanup = null;
        try
        {
            True(fixture.Session.TryGetCurrentFrameAuthority(
                    out authority));
            NotNull(authority);
            TaskCompletionSource<bool> pumpLeaseHeld = NewSignal();
            releasePumpLease = NewSignal();
            TaskCompletionSource<bool> handoffAttempted = NewSignal();
            int pumpPhase = 0;
            authority!.SetTestLeaseAcquisitionHook(acquired =>
            {
                if (!acquired)
                {
                    if (Volatile.Read(ref pumpPhase) == 1)
                    {
                        handoffAttempted.TrySetResult(true);
                    }

                    return;
                }

                if (Interlocked.CompareExchange(ref pumpPhase, 1, 0) == 0)
                {
                    pumpLeaseHeld.TrySetResult(true);
                    releasePumpLease.Task.GetAwaiter().GetResult();
                }
            });

            cleanup = new CancellationTokenSource();
            cleanup.CancelAfter(TimeSpan.FromSeconds(5));
            fixture.Transport.Enqueue(WireFrameCodec.EncodeStoc(
                StocPacketType.GameMsg,
                new byte[] { 40, 0 }));
            pumpTask = Task.Factory.StartNew(
                async () => await fixture.Session.PumpAsync(cleanup.Token)
                    .ConfigureAwait(false),
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).Unwrap();
            pumpLeaseHeld.Task.WaitAsync(cleanup.Token)
                .GetAwaiter()
                .GetResult();

            handoffTask = Task.Factory.StartNew(() =>
                {
                    bool success = fixture.Composition.Handoff!
                        .TryAcquireBoundaryAcceptanceLease(
                            fixture.Composition.Frame,
                            fixture.PromptProjection,
                            out I6DBoundaryAcceptanceLeaseV1? lease,
                            out I6DPrivateCrossLocatorHandoffErrorV1? error);
                    return (success, lease, error);
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
            handoffAttempted.Task.WaitAsync(cleanup.Token)
                .GetAwaiter()
                .GetResult();

            releasePumpLease.TrySetResult(true);
            GameplayMirrorPumpResult pumped = pumpTask.WaitAsync(cleanup.Token)
                .GetAwaiter()
                .GetResult();
            True(pumped.IsSuccess, pumped.Error.ToString());
            (bool Success,
                I6DBoundaryAcceptanceLeaseV1? Lease,
                I6DPrivateCrossLocatorHandoffErrorV1? Error) outcome =
                handoffTask!.WaitAsync(cleanup.Token)
                    .GetAwaiter()
                    .GetResult();
            False(outcome.Success);
            Null(outcome.Lease);
            Equal(
                I6DPrivateCrossLocatorHandoffErrorCodeV1.StaleFrame,
                outcome.Error!.Value.Code);
        }
        finally
        {
            releasePumpLease?.TrySetResult(true);
            authority?.SetTestLeaseAcquisitionHook(null);
            cleanup?.Cancel();

            DisposeI6C5Session(fixture.Session, fixture.Consumer);
        }
    }

    internal static void TestI6DBoundaryAcceptanceWinsAgainstPromptInvalidation() =>
        TestBoundaryAcceptanceWinsAgainstPromptInvalidation();

    internal static void TestI6DPromptInvalidationWinsAgainstBoundaryAcceptance() =>
        TestPromptInvalidationWinsAgainstBoundaryAcceptance();

    private static void TestBoundaryAcceptanceWinsAgainstPromptInvalidation()
    {
        I6DCompositionFixture fixture = CreateI6DCompositionFixture();
        PrivateFlatPromptBindingLifetimeAuthorityV1? promptAuthority = null;
        TaskCompletionSource<bool>? acceptancePromptLeaseHeld = null;
        TaskCompletionSource<bool>? releaseAcceptance = null;
        TaskCompletionSource<bool>? invalidationAttempted = null;
        Task<(bool Success,
            OcgForgeAcceptedDecisionBoundaryV1? Boundary,
            OcgForgeAcceptedDecisionBoundaryProducerErrorV1? Error)>?
            acceptanceTask = null;
        Task? invalidationTask = null;
        CancellationTokenSource? cleanup = null;
        try
        {
            promptAuthority = GetCurrentPromptAuthority(fixture);
            acceptancePromptLeaseHeld = NewSignal();
            releaseAcceptance = NewSignal();
            invalidationAttempted = NewSignal();
            promptAuthority.SetTestLeaseAcquisitionHook(acquired =>
            {
                if (acquired)
                {
                    acceptancePromptLeaseHeld.TrySetResult(true);
                    releaseAcceptance.Task.GetAwaiter().GetResult();
                }
            });
            promptAuthority.SetTestInvalidationHook(attempted =>
            {
                if (!attempted)
                {
                    invalidationAttempted.TrySetResult(true);
                }
            });

            cleanup = NewCleanupCancellation();
            OcgForgeAcceptedDecisionBoundaryProducerV1 producer = new();
            acceptanceTask = Task.Factory.StartNew(
                () =>
                {
                    bool success = producer.TryAccept(
                        fixture.Composition.Frame,
                        fixture.PromptProjection,
                        fixture.Composition.Handoff,
                        out OcgForgeAcceptedDecisionBoundaryV1? boundary,
                        out OcgForgeAcceptedDecisionBoundaryProducerErrorV1?
                            error);
                    return (success, boundary, error);
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

            acceptancePromptLeaseHeld.Task.WaitAsync(cleanup.Token)
                .GetAwaiter()
                .GetResult();
            invalidationTask = Task.Factory.StartNew(
                promptAuthority.Invalidate,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
            invalidationAttempted.Task.WaitAsync(cleanup.Token)
                .GetAwaiter()
                .GetResult();
            False(promptAuthority.IsLeaseAvailableForTest());

            releaseAcceptance.TrySetResult(true);
            (bool Success,
                OcgForgeAcceptedDecisionBoundaryV1? Boundary,
                OcgForgeAcceptedDecisionBoundaryProducerErrorV1? Error)
                outcome = acceptanceTask.WaitAsync(cleanup.Token)
                    .GetAwaiter()
                    .GetResult();
            True(outcome.Success, outcome.Error?.ToString() ??
                "boundary acceptance lost to prompt invalidation");
            NotNull(outcome.Boundary);
            invalidationTask.WaitAsync(cleanup.Token)
                .GetAwaiter()
                .GetResult();
        }
        finally
        {
            releaseAcceptance?.TrySetResult(true);
            promptAuthority?.SetTestLeaseAcquisitionHook(null);
            promptAuthority?.SetTestInvalidationHook(null);
            cleanup?.Cancel();
            DisposeI6C5Session(fixture.Session, fixture.Consumer);
        }
    }

    private static void TestPromptInvalidationWinsAgainstBoundaryAcceptance()
    {
        I6DCompositionFixture fixture = CreateI6DCompositionFixture();
        PrivateFlatPromptBindingLifetimeAuthorityV1? promptAuthority = null;
        TaskCompletionSource<bool>? invalidationLeaseHeld = null;
        TaskCompletionSource<bool>? releaseInvalidation = null;
        TaskCompletionSource<bool>? acceptancePromptAttempted = null;
        Task<(bool Success,
            OcgForgeAcceptedDecisionBoundaryV1? Boundary,
            OcgForgeAcceptedDecisionBoundaryProducerErrorV1? Error)>?
            acceptanceTask = null;
        Task? invalidationTask = null;
        CancellationTokenSource? cleanup = null;
        try
        {
            promptAuthority = GetCurrentPromptAuthority(fixture);
            invalidationLeaseHeld = NewSignal();
            releaseInvalidation = NewSignal();
            acceptancePromptAttempted = NewSignal();
            promptAuthority.SetTestInvalidationHook(acquired =>
            {
                if (acquired)
                {
                    invalidationLeaseHeld.TrySetResult(true);
                    releaseInvalidation.Task.GetAwaiter().GetResult();
                }
            });
            promptAuthority.SetTestLeaseAcquisitionHook(acquired =>
            {
                if (!acquired)
                {
                    acceptancePromptAttempted.TrySetResult(true);
                }
            });

            cleanup = NewCleanupCancellation();
            invalidationTask = Task.Factory.StartNew(
                promptAuthority.Invalidate,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
            invalidationLeaseHeld.Task.WaitAsync(cleanup.Token)
                .GetAwaiter()
                .GetResult();

            OcgForgeAcceptedDecisionBoundaryProducerV1 producer = new();
            acceptanceTask = Task.Factory.StartNew(
                () =>
                {
                    bool success = producer.TryAccept(
                        fixture.Composition.Frame,
                        fixture.PromptProjection,
                        fixture.Composition.Handoff,
                        out OcgForgeAcceptedDecisionBoundaryV1? boundary,
                        out OcgForgeAcceptedDecisionBoundaryProducerErrorV1?
                            error);
                    return (success, boundary, error);
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
            acceptancePromptAttempted.Task.WaitAsync(cleanup.Token)
                .GetAwaiter()
                .GetResult();
            False(promptAuthority.IsLeaseAvailableForTest());

            releaseInvalidation.TrySetResult(true);
            invalidationTask.WaitAsync(cleanup.Token)
                .GetAwaiter()
                .GetResult();
            (bool Success,
                OcgForgeAcceptedDecisionBoundaryV1? Boundary,
                OcgForgeAcceptedDecisionBoundaryProducerErrorV1? Error)
                outcome = acceptanceTask.WaitAsync(cleanup.Token)
                    .GetAwaiter()
                    .GetResult();
            False(outcome.Success);
            Null(outcome.Boundary);
            Equal(
                OcgForgeAcceptedDecisionBoundaryProducerErrorCodeV1.HandoffRejected,
                outcome.Error!.Value.Code);

            True(producer.TryAccept(
                    fixture.Composition.Frame,
                    fixture.PromptProjection,
                    out OcgForgeAcceptedDecisionBoundaryV1? retryBoundary,
                    out OcgForgeAcceptedDecisionBoundaryProducerErrorV1?
                        retryError),
                retryError?.ToString() ?? "legacy retry was rejected");
            Equal(0ul, retryBoundary!.DecisionIndex);
        }
        finally
        {
            releaseInvalidation?.TrySetResult(true);
            promptAuthority?.SetTestLeaseAcquisitionHook(null);
            promptAuthority?.SetTestInvalidationHook(null);
            cleanup?.Cancel();
            DisposeI6C5Session(fixture.Session, fixture.Consumer);
        }
    }

    private static TaskCompletionSource<bool> NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static CancellationTokenSource NewCleanupCancellation()
    {
        CancellationTokenSource cleanup = new();
        cleanup.CancelAfter(TimeSpan.FromSeconds(5));
        return cleanup;
    }

    private static PrivateGameplayFrameAuthorityV1 GetCurrentFrame(
        GameplayMirrorSessionV1 session)
    {
        True(session.TryGetCurrentFrameAuthority(
                out PrivateGameplayFrameAuthorityV1? authority));
        return authority!;
    }

    private static PrivateFlatPromptBindingLifetimeAuthorityV1
        GetCurrentPromptAuthority(I6DCompositionFixture fixture)
    {
        True(fixture.Session.TryGetCurrentFrameAuthority(
                out PrivateGameplayFrameAuthorityV1? authority));
        NotNull(authority);
        True(fixture.Prompt.TryGetCurrentFrameOwnedBinding(
                authority!,
                fixture.PromptProjection,
                out CurrentFlatPromptBindingV1? binding));
        NotNull(binding);
        return binding!.PromptLifetimeAuthority ??
            throw new InvalidOperationException(
                "frame-owned prompt has no lifetime authority");
    }

    internal static void TestI6C1SourceContainer()
    {
        Run("complete structural value", AssertCompleteStructuralValue);
        Run("missing sections fail closed", AssertMissingSectionsFailClosed);
        Run("invalid players and enums fail closed", AssertInvalidValuesFailClosed);
        Run("duplicate and ordered values fail closed", AssertDuplicateAndOrderFailures);
        Run("optional presence is semantic", AssertOptionalPresenceIsSemantic);
        Run("deep value ownership", AssertDeepValueOwnership);
        Run("read-only public collections", AssertReadOnlyCollections);
        Run("equivalent values are deterministic", AssertEquivalentValuesAreDeterministic);
        Run("structured failures contain no sensitive data", AssertStructuredFailureSurface);
        Run("locator bytes are printable", AssertPrintableLocatorBoundaries);
        Run("cross-section invariants are enforced", AssertCrossSectionInvariants);
        Run("first invalid invariant is diagnosed", AssertFirstInvalidInvariant);
        Run("public surface has no private escape hatch", AssertPublicSurface);
        Run("I6C2 mirror source closure", TestI6C2MirrorSourceClosure);
        Run("I6C5 Extra UPDATE_DATA bootstrap", AssertI6C5ExtraUpdateDataBootstrap);
    }

    internal static void TestProvisionalI6C5FrameFailurePreservesAuthority()
    {
        (GameplaySessionV1 transportSession,
            PerspectiveStateMirrorV1 mirror,
            GameplayHandoffConsumerV1 consumer,
            TestTransport transport) = CreateStartedSession(
                0,
                extraCount0: 2,
                extraCount1: 0);
        GameplayMirrorSessionV1? session = null;
        try
        {
            session = new GameplayMirrorSessionV1(
                transportSession,
                mirror,
                CreateValidI6C5MatchContext(),
                CreatePrintedProviderForMirror(mirror));
            True(session.TryGetCurrentFrameAuthority(
                out PrivateGameplayFrameAuthorityV1? beforeAuthority));
            NotNull(beforeAuthority);
            string beforeSnapshot = mirror.Snapshot.ToDeterministicString();
            int readsBeforeFrameAttempts = transport.ReadCallCount;

            PerspectiveSafeFrameSourceResultV1 firstFrame =
                session.TryCreateI6C5Frame();
            False(firstFrame.IsSuccess);
            Equal(
                PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
                firstFrame.Error!.Value.Code);
            Equal(
                PerspectiveSafeSourceSectionV1.Entities,
                firstFrame.Error.Value.Section);
            Equal(beforeSnapshot, mirror.Snapshot.ToDeterministicString());
            Equal(readsBeforeFrameAttempts, transport.ReadCallCount);
            True(session.TryGetCurrentFrameAuthority(
                out PrivateGameplayFrameAuthorityV1? afterFirstFrame));
            NotNull(afterFirstFrame);
            Equal(
                beforeAuthority!.FrameInstanceOrdinal,
                afterFirstFrame!.FrameInstanceOrdinal);
            True(afterFirstFrame.IsCurrent);

            PerspectiveSafeFrameSourceResultV1 repeatedFrame =
                session.TryCreateI6C5Frame();
            False(repeatedFrame.IsSuccess);
            Equal(
                PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
                repeatedFrame.Error!.Value.Code);
            Equal(
                PerspectiveSafeSourceSectionV1.Entities,
                repeatedFrame.Error.Value.Section);
            Equal(readsBeforeFrameAttempts, transport.ReadCallCount);
            True(session.TryGetCurrentFrameAuthority(
                out PrivateGameplayFrameAuthorityV1? afterRepeatedFrame));
            NotNull(afterRepeatedFrame);
            True(afterRepeatedFrame!.IsCurrent);

            GameplayMirrorPumpResult next = ApplyI6C4ThroughSession(
                session,
                transport,
                new byte[] { 40, 0 });
            True(next.IsSuccess, next.Error.ToString());
            NotNull(next.Message);
            Equal(GameplayMessageKindV1.NewTurn, next.Message!.Kind);
            False(afterRepeatedFrame.IsCurrent);
            True(session.TryGetCurrentFrameAuthority(
                out PrivateGameplayFrameAuthorityV1? advancedAuthority));
            NotNull(advancedAuthority);
            Equal(1ul, advancedAuthority!.FrameInstanceOrdinal);
            True(advancedAuthority.IsCurrent);
            False(string.Equals(
                beforeSnapshot,
                mirror.Snapshot.ToDeterministicString(),
                StringComparison.Ordinal));
            Equal(readsBeforeFrameAttempts + 1, transport.ReadCallCount);
        }
        finally
        {
            if (session is not null)
            {
                session.DisposeAsync().GetAwaiter().GetResult();
            }

            consumer.DisposeAsync().GetAwaiter().GetResult();
        }
    }

    internal static void TestSuccessfulI6C5FrameAuthorityAdvances()
    {
        (GameplaySessionV1 transportSession,
            PerspectiveStateMirrorV1 mirror,
            GameplayHandoffConsumerV1 consumer,
            TestTransport transport) = CreateStartedSession(
                0,
                extraCount0: 0,
                extraCount1: 0);
        GameplayMirrorSessionV1? session = null;
        try
        {
            session = new GameplayMirrorSessionV1(
                transportSession,
                mirror,
                CreateValidI6C5MatchContext(),
                CreatePrintedProviderForMirror(mirror));
            True(session.TryGetCurrentFrameAuthority(
                out PrivateGameplayFrameAuthorityV1? initialAuthority));
            NotNull(initialAuthority);
            PerspectiveSafeFrameSourceResultV1 frame =
                session.TryCreateI6C5Frame();
            True(frame.IsSuccess, frame.Error?.ToString() ?? "frame rejected");
            True(session.TryGetCurrentFrameAuthority(
                out PrivateGameplayFrameAuthorityV1? afterFrame));
            NotNull(afterFrame);
            Equal(
                initialAuthority!.FrameInstanceOrdinal,
                afterFrame!.FrameInstanceOrdinal);
            True(afterFrame.IsCurrent);

            GameplayMirrorPumpResult next = ApplyI6C4ThroughSession(
                session,
                transport,
                new byte[] { 40, 0 });
            True(next.IsSuccess, next.Error.ToString());
            False(afterFrame.IsCurrent);
            True(session.TryGetCurrentFrameAuthority(
                out PrivateGameplayFrameAuthorityV1? advancedAuthority));
            NotNull(advancedAuthority);
            Equal(1ul, advancedAuthority!.FrameInstanceOrdinal);
            True(advancedAuthority.IsCurrent);
        }
        finally
        {
            if (session is not null)
            {
                session.DisposeAsync().GetAwaiter().GetResult();
            }

            consumer.DisposeAsync().GetAwaiter().GetResult();
        }
    }

    private static void Run(string name, Action test)
    {
        try
        {
            test();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                name + ": " + exception.Message,
                exception);
        }
    }

    private static byte[] DuplicateOwnHandIdleMessage(uint cardCode)
    {
        return Join(
            new byte[] { 11, 0 },
            U32(2),
            U32(cardCode),
            new byte[] { 0, 0x02 },
            U32(0),
            U32(cardCode),
            new byte[] { 0, 0x02 },
            U32(1),
            U32(0),
            U32(0),
            U32(0),
            U32(0),
            U32(0),
            new byte[] { 0, 0, 0 });
    }

    private static byte[] SameOccurrenceMultipleIdleActionsMessage(
        uint cardCode)
    {
        ModernLocInfoV1 occurrence = new(0, 0x02, 0, 0);
        return IdleMessage(
            0,
            new[] { new IdleSimpleSpec(cardCode, occurrence) },
            Array.Empty<IdleSimpleSpec>(),
            Array.Empty<IdleSimpleSpec>(),
            new[] { new IdleSimpleSpec(cardCode, occurrence) },
            Array.Empty<IdleSimpleSpec>(),
            Array.Empty<IdleActivationSpec>(),
            0,
            0,
            0);
    }

    private static byte[] IdleMessage(
        byte player,
        IReadOnlyList<IdleSimpleSpec> summon,
        IReadOnlyList<IdleSimpleSpec> specialSummon,
        IReadOnlyList<IdleSimpleSpec> reposition,
        IReadOnlyList<IdleSimpleSpec> mset,
        IReadOnlyList<IdleSimpleSpec> sset,
        IReadOnlyList<IdleActivationSpec> activatable,
        byte toBattlePhase,
        byte toEndPhase,
        byte shuffleHand)
    {
        List<byte[]> parts = new()
        {
            new[] { (byte)11, player }
        };
        AddIdleSimpleSection(parts, summon, wideSequence: true);
        AddIdleSimpleSection(parts, specialSummon, wideSequence: true);
        AddIdleSimpleSection(parts, reposition, wideSequence: false);
        AddIdleSimpleSection(parts, mset, wideSequence: true);
        AddIdleSimpleSection(parts, sset, wideSequence: true);
        parts.Add(U32((uint)activatable.Count));
        parts.AddRange(activatable.Select(entry => Join(
            U32(entry.CardCode),
            new[] { entry.Location.Controller, entry.Location.Location },
            U32(entry.Location.Sequence),
            U64(entry.Description),
            new[] { entry.ClientMode })));
        parts.Add(new[] { toBattlePhase, toEndPhase, shuffleHand });
        return Join(parts.ToArray());
    }

    private static void AddIdleSimpleSection(
        List<byte[]> parts,
        IReadOnlyList<IdleSimpleSpec> entries,
        bool wideSequence)
    {
        parts.Add(U32((uint)entries.Count));
        foreach (IdleSimpleSpec entry in entries)
        {
            parts.Add(Join(
                U32(entry.CardCode),
                new[] { entry.Location.Controller, entry.Location.Location },
                wideSequence
                    ? U32(entry.Location.Sequence)
                    : new[] { checked((byte)entry.Location.Sequence) }));
        }
    }

    private static I6DCompositionFixture CreateI6DCompositionFixture(
        uint duplicateCardCode = 0x11223344,
        byte[]? promptMessage = null)
    {
        PerspectiveSafePrintedProviderV1 printedProvider =
            CreatePrintedProviderForCodes(new[] { duplicateCardCode });
        (GameplayMirrorSessionV1 session,
            GameplayHandoffConsumerV1 consumer,
            TestTransport transport) = CreateI6C5Session(
                0,
                CreateValidI6C5MatchContext(),
                printedProvider);
        try
        {
            ModernLocInfoV1 empty = new(0, 0, 0, 0);
            True(ApplyI6C4ThroughSession(
                    session,
                    transport,
                    MoveMessage(
                        duplicateCardCode,
                        empty,
                        new ModernLocInfoV1(0, 0x02, 0, 0x08),
                        0)).IsSuccess);
            True(ApplyI6C4ThroughSession(
                    session,
                    transport,
                    MoveMessage(
                        duplicateCardCode,
                        empty,
                        new ModernLocInfoV1(0, 0x02, 1, 0x08),
                        0)).IsSuccess);

            True(session.TryGetCurrentFrameAuthority(
                    out PrivateGameplayFrameAuthorityV1? frameAuthority));
            NotNull(frameAuthority);
            PublicStateProjectionResultV1 acceptedI4Projection =
                PublicStateProjectionV1.TryProject(
                    frameAuthority!.MirrorSnapshot,
                    new PublicStateProjectionContextV1(0x234),
                    frameAuthority.FrameInstanceOrdinal);
            True(acceptedI4Projection.IsSuccess,
                acceptedI4Projection.Error.ToString());

            FlatPromptSessionV1 promptSession = new();
            FlatPromptProjectionResultV1 acceptedPrompt =
                promptSession.TryAcceptFrameOwnedPrompt(
                    promptMessage ?? DuplicateOwnHandIdleMessage(
                        duplicateCardCode),
                    frameAuthority,
                    acceptedI4Projection);
            True(acceptedPrompt.IsSuccess, acceptedPrompt.Error.ToString());
            Equal(2, acceptedPrompt.Candidates!.Count);

            I6DFrameOwnedCompositionResultV1 composition =
                session.TryCreateI6DFrameOwnedComposition(
                    promptSession,
                    acceptedPrompt,
                    acceptedI4Projection);
            True(composition.IsSuccess,
                composition.Error?.ToString() ?? "I6D composition rejected");
            NotNull(composition.Frame);
            NotNull(composition.Handoff);
            return new I6DCompositionFixture(
                session,
                consumer,
                transport,
                promptSession,
                acceptedPrompt,
                acceptedI4Projection,
                composition,
                printedProvider);
        }
        catch
        {
            DisposeI6C5Session(session, consumer);
            throw;
        }
    }

    private sealed class I6DCompositionFixture
    {
        internal I6DCompositionFixture(
            GameplayMirrorSessionV1 session,
            GameplayHandoffConsumerV1 consumer,
            TestTransport transport,
            FlatPromptSessionV1 prompt,
            FlatPromptProjectionResultV1 promptProjection,
            PublicStateProjectionResultV1 i4Projection,
            I6DFrameOwnedCompositionResultV1 composition,
            PerspectiveSafePrintedProviderV1 provider)
        {
            Session = session;
            Consumer = consumer;
            Transport = transport;
            Prompt = prompt;
            PromptProjection = promptProjection;
            I4Projection = i4Projection;
            Composition = composition;
            Provider = provider;
        }

        internal GameplayMirrorSessionV1 Session { get; }

        internal GameplayHandoffConsumerV1 Consumer { get; }

        internal TestTransport Transport { get; }

        internal FlatPromptSessionV1 Prompt { get; }

        internal FlatPromptProjectionResultV1 PromptProjection { get; }

        internal PublicStateProjectionResultV1 I4Projection { get; }

        internal I6DFrameOwnedCompositionResultV1 Composition { get; }

        internal PerspectiveSafePrintedProviderV1 Provider { get; }
    }

    private static OcgForgePublicDecisionContextResultV1 CreateMappedI6DContext(
        I6DCompositionFixture fixture)
    {
        OcgForgeAcceptedDecisionBoundaryProducerV1 producer = new();
        True(producer.TryAccept(
                fixture.Composition.Frame,
                fixture.PromptProjection,
                fixture.Composition.Handoff,
                out OcgForgeAcceptedDecisionBoundaryV1? boundary,
                out OcgForgeAcceptedDecisionBoundaryProducerErrorV1? error),
            error?.ToString() ?? "boundary rejected");
        return OcgForgePublicCandidateBridgeV1.TryCreate(boundary);
    }

    private static PerspectiveSafeFrameV1 CopyPublicFrame(
        PerspectiveSafeFrameV1 source,
        IEnumerable<PerspectiveSafeEntityV1> entities) =>
        new(
            new PerspectiveSafeFrameSourceInputV1(
                source.Globals,
                source.Zones,
                entities,
                source.Relationships,
                source.Chain,
                source.VisibleEvents,
                source.MatchContext));

    private static void AssertCompleteStructuralValue()
    {
        PerspectiveSafeFrameSourceResultV1 result =
            PerspectiveSafePublicFrameSourceV1.TryCreate(CreateValidInput());

        True(result.IsSuccess, result.Error?.ToString() ?? "source value rejected");
        NotNull(result.Frame);
        Null(result.Error);
        Equal(2, result.Frame!.Globals.LifePoints.Count);
        Equal(2, result.Frame.Zones.Count);
        Equal(2, result.Frame.Entities.Count);
        Equal(1, result.Frame.Relationships.Count);
        Equal((uint)1, result.Frame.Chain.Length);
        Equal(2, result.Frame.VisibleEvents.Count);
        Equal((byte)0, result.Frame.MatchContext.PerspectivePlayer);
    }

    private static void AssertMissingSectionsFailClosed()
    {
        PerspectiveSafeGlobalsV1 globals = CreateValidGlobals();
        IReadOnlyList<PerspectiveSafeZoneV1> zones = CreateValidZones();
        IReadOnlyList<PerspectiveSafeEntityV1> entities = CreateValidEntities();
        IReadOnlyList<PerspectiveSafeRelationshipV1> relationships =
            CreateValidRelationships();
        PerspectiveSafeChainStateV1 chain = CreateValidChain();
        IReadOnlyList<PerspectiveSafeVisibleEventV1> events =
            CreateValidEvents();
        PerspectiveSafeMatchContextV1 context = CreateValidMatchContext();

        AssertFailure(
            new PerspectiveSafeFrameSourceInputV1(
                null,
                zones,
                entities,
                relationships,
                chain,
                events,
                context),
            PerspectiveSafeFrameSourceErrorCodeV1.MissingGlobals);
        AssertFailure(
            new PerspectiveSafeFrameSourceInputV1(
                globals,
                null,
                entities,
                relationships,
                chain,
                events,
                context),
            PerspectiveSafeFrameSourceErrorCodeV1.MissingZones);
        AssertFailure(
            new PerspectiveSafeFrameSourceInputV1(
                globals,
                zones,
                null,
                relationships,
                chain,
                events,
                context),
            PerspectiveSafeFrameSourceErrorCodeV1.MissingEntities);
        AssertFailure(
            new PerspectiveSafeFrameSourceInputV1(
                globals,
                zones,
                entities,
                null,
                chain,
                events,
                context),
            PerspectiveSafeFrameSourceErrorCodeV1.MissingRelationships);
        AssertFailure(
            new PerspectiveSafeFrameSourceInputV1(
                globals,
                zones,
                entities,
                relationships,
                null,
                events,
                context),
            PerspectiveSafeFrameSourceErrorCodeV1.MissingChain);
        AssertFailure(
            new PerspectiveSafeFrameSourceInputV1(
                globals,
                zones,
                entities,
                relationships,
                chain,
                null,
                context),
            PerspectiveSafeFrameSourceErrorCodeV1.MissingVisibleEvents);
        AssertFailure(
            new PerspectiveSafeFrameSourceInputV1(
                globals,
                zones,
                entities,
                relationships,
                chain,
                events,
                null),
            PerspectiveSafeFrameSourceErrorCodeV1.MissingMatchContext);
    }

    private static void AssertInvalidValuesFailClosed()
    {
        PerspectiveSafeFrameSourceInputV1 invalidPlayer = CreateInput(
            new PerspectiveSafeGlobalsV1(
                duelFlags: 0,
                lifePoints: new uint[] { 8000, 7000 },
                playerToAct: 2,
                turnPlayer: 0),
            CreateValidZones(),
            CreateValidEntities(),
            CreateValidRelationships(),
            CreateValidChain(),
            CreateValidEvents(),
            CreateValidMatchContext());
        AssertFailure(
            invalidPlayer,
            PerspectiveSafeFrameSourceErrorCodeV1.InvalidPlayer);

        PerspectiveSafeEntityV1 unknownZone = new(
            "entity-a",
            identityKnown: true,
            passcode: 7,
            owner: 0,
            controller: 0,
            zone: (PerspectiveSafeSemanticZoneV1)255,
            sequence: null,
            overlaySequence: null,
            position: PerspectiveSafePositionV1.Unknown,
            faceUp: false,
            faceDown: false);
        AssertFailure(
            CreateInput(
                CreateValidGlobals(),
                CreateValidZones(),
                new[] { unknownZone },
                CreateValidRelationships(),
                CreateValidChain(),
                CreateValidEvents(),
                CreateValidMatchContext()),
            PerspectiveSafeFrameSourceErrorCodeV1.UnknownEnum);

        PerspectiveSafeEntityV1 contradictory = new(
            "entity-a",
            identityKnown: false,
            passcode: null,
            owner: 0,
            controller: 0,
            zone: PerspectiveSafeSemanticZoneV1.Hand,
            sequence: null,
            overlaySequence: null,
            position: PerspectiveSafePositionV1.FaceUpAttack,
            faceUp: true,
            faceDown: true);
        AssertFailure(
            CreateInput(
                CreateValidGlobals(),
                CreateValidZones(),
                new[] { contradictory },
                CreateValidRelationships(),
                CreateValidChain(),
                CreateValidEvents(),
                CreateValidMatchContext()),
            PerspectiveSafeFrameSourceErrorCodeV1.ContradictoryEntityState);
    }

    private static void AssertDuplicateAndOrderFailures()
    {
        PerspectiveSafeEntityV1 first = CreateValidEntities()[0];
        PerspectiveSafeEntityV1 duplicate = CreateValidEntities()[0];
        AssertFailure(
            CreateInput(
                CreateValidGlobals(),
                CreateValidZones(),
                new[] { first, duplicate },
                CreateValidRelationships(),
                CreateValidChain(),
                CreateValidEvents(),
                CreateValidMatchContext()),
            PerspectiveSafeFrameSourceErrorCodeV1.DuplicateLocator);

        PerspectiveSafeVisibleEventV1 duplicateEvent =
            new(0, PerspectiveSafeVisibleEventKindV1.PhaseChanged);
        AssertFailure(
            CreateInput(
                CreateValidGlobals(),
                CreateValidZones(),
                CreateValidEntities(),
                CreateValidRelationships(),
                CreateValidChain(),
                new[] { duplicateEvent, duplicateEvent },
                CreateValidMatchContext()),
            PerspectiveSafeFrameSourceErrorCodeV1.DuplicateEventIndex);

        PerspectiveSafeVisibleEventV1 later =
            new(2, PerspectiveSafeVisibleEventKindV1.PhaseChanged);
        PerspectiveSafeVisibleEventV1 earlier =
            new(1, PerspectiveSafeVisibleEventKindV1.TurnStarted);
        AssertFailure(
            CreateInput(
                CreateValidGlobals(),
                CreateValidZones(),
                CreateValidEntities(),
                CreateValidRelationships(),
                CreateValidChain(),
                new[] { later, earlier },
                CreateValidMatchContext()),
            PerspectiveSafeFrameSourceErrorCodeV1.EventIndexNotIncreasing);

        PerspectiveSafeChainStateV1 mismatchedChain =
            new(2, new[] { CreateValidChain().Links[0] });
        AssertFailure(
            CreateInput(
                CreateValidGlobals(),
                CreateValidZones(),
                CreateValidEntities(),
                CreateValidRelationships(),
                mismatchedChain,
                CreateValidEvents(),
                CreateValidMatchContext()),
            PerspectiveSafeFrameSourceErrorCodeV1.ChainLengthMismatch);
    }

    private static void AssertOptionalPresenceIsSemantic()
    {
        PerspectiveSafeFrameSourceInputV1 absent = CreateInput(
            new PerspectiveSafeGlobalsV1(
                duelFlags: 0x1234,
                lifePoints: new uint[] { 8000, 7000 },
                turnPlayer: 0,
                chainLength: 1),
            CreateValidZones(),
            CreateValidEntities(),
            CreateValidRelationships(),
            CreateValidChain(),
            CreateValidEvents(),
            CreateValidMatchContext());
        PerspectiveSafeFrameSourceInputV1 presentZero = CreateInput(
            new PerspectiveSafeGlobalsV1(
                duelFlags: 0x1234,
                lifePoints: new uint[] { 8000, 7000 },
                playerToAct: 0,
                turnPlayer: 0,
                chainLength: 1),
            CreateValidZones(),
            CreateValidEntities(),
            CreateValidRelationships(),
            CreateValidChain(),
            CreateValidEvents(),
            CreateValidMatchContext());

        PerspectiveSafeFrameV1 absentFrame = Accept(absent);
        PerspectiveSafeFrameV1 presentFrame = Accept(presentZero);
        True(absentFrame.Globals.PlayerToAct is null);
        Equal((byte)0, presentFrame.Globals.PlayerToAct!.Value);
        NotEqual(
            FrameSignature(absentFrame),
            FrameSignature(presentFrame));
    }

    private static void AssertDeepValueOwnership()
    {
        List<uint> lifePoints = new() { 8000, 7000 };
        List<PerspectiveSafeZoneV1> zones = new(CreateValidZones());
        List<PerspectiveSafeLinkMarkerV1> markers = new()
        {
            PerspectiveSafeLinkMarkerV1.Bottom,
            PerspectiveSafeLinkMarkerV1.Top
        };
        List<PerspectiveSafeCounterV1> counters = new()
        {
            new(1, 0),
            new(2, 1)
        };
        PerspectiveSafeCardPropertiesV1 properties =
            new(linkMarkers: markers, counters: counters);
        List<PerspectiveSafeEntityV1> entities = new()
        {
            new(
                "entity-a",
                identityKnown: true,
                passcode: 7,
                owner: 0,
                controller: 0,
                zone: PerspectiveSafeSemanticZoneV1.Hand,
                sequence: 0,
                overlaySequence: null,
                position: PerspectiveSafePositionV1.Unknown,
                faceUp: false,
                faceDown: false,
                printed: properties,
                current: properties)
        };
        List<PerspectiveSafeRelationshipV1> relationships = new()
        {
            new(
                PerspectiveSafeRelationshipKindV1.Target,
                "entity-a",
                "entity-a")
        };
        List<string> chainTargets = new() { "entity-a" };
        PerspectiveSafeChainLinkV1 chainLink = new(
            index: 0,
            activatingPlayer: 0,
            source: "entity-a",
            activationZone: PerspectiveSafeSemanticZoneV1.Hand,
            targets: chainTargets);
        List<PerspectiveSafeVisibleEventV1> events = new()
        {
            new(
                0,
                PerspectiveSafeVisibleEventKindV1.CardRevealed,
                entityLocator: "entity-a",
                publicPasscode: 7,
                targets: chainTargets)
        };
        List<uint> ownMain = new() { 1, 2 };
        List<uint> ownExtra = new() { 3 };
        PerspectiveSafeFrameSourceInputV1 input = CreateInput(
            new PerspectiveSafeGlobalsV1(
                duelFlags: 0,
                lifePoints: lifePoints,
                turnPlayer: 0,
                chainLength: 1),
            zones,
            entities,
            relationships,
            new PerspectiveSafeChainStateV1(1, new[] { chainLink }),
            events,
            new PerspectiveSafeMatchContextV1(
                perspectivePlayer: 0,
                duelFlags: 0,
                knowledge: new(true, false),
                ownDeck: new(true, ownMain, ownExtra),
                opponentDeck: new(false)));

        PerspectiveSafeFrameV1 frame = Accept(input);
        string before = FrameSignature(frame);

        lifePoints[0] = 1;
        zones[0] = new(
            1,
            PerspectiveSafeSemanticZoneV1.Banished,
            99,
            99,
            0,
            false);
        markers[0] = PerspectiveSafeLinkMarkerV1.TopRight;
        counters[0] = new(99, 99);
        entities.Clear();
        relationships.Clear();
        chainTargets[0] = "mutated";
        events.Clear();
        ownMain[0] = 99;
        ownExtra.Clear();

        Equal(before, FrameSignature(frame));
        Equal((uint)8000, frame.Globals.LifePoints[0]);
        Equal("entity-a", frame.Entities[0].Locator);
        Equal("entity-a", frame.Chain.Links[0].Targets[0]);
        Equal((uint)1, frame.MatchContext.OwnDeck.MainDeck[0]);
    }

    private static void AssertReadOnlyCollections()
    {
        PerspectiveSafeFrameV1 frame = Accept(CreateValidInput());
        AssertReadOnly(frame.Zones);
        AssertReadOnly(frame.Entities);
        AssertReadOnly(frame.Relationships);
        AssertReadOnly(frame.VisibleEvents);
        AssertReadOnly(frame.Entities[0].Current!.LinkMarkers);
        AssertReadOnly(frame.Chain.Links[0].Targets);
        AssertReadOnly(frame.MatchContext.OwnDeck.MainDeck);
    }

    private static void AssertEquivalentValuesAreDeterministic()
    {
        PerspectiveSafeFrameV1 first = Accept(CreateValidInput());
        PerspectiveSafeFrameV1 second = Accept(CreateValidInput());
        Equal(FrameSignature(first), FrameSignature(second));
        Equal(first.Entities[0].Locator, second.Entities[0].Locator);
        Equal(first.VisibleEvents[1].EventIndex, second.VisibleEvents[1].EventIndex);
    }

    private static void AssertPublicSurface()
    {
        Type[] publicTypes =
        {
            typeof(PerspectiveSafeSourceSectionV1),
            typeof(PerspectiveSafeFrameSourceErrorCodeV1),
            typeof(PerspectiveSafeFrameSourceErrorV1),
            typeof(PerspectiveSafeSemanticZoneV1),
            typeof(PerspectiveSafePositionV1),
            typeof(PerspectiveSafeLinkMarkerV1),
            typeof(PerspectiveSafeRelationshipKindV1),
            typeof(PerspectiveSafeVisibleEventKindV1),
            typeof(PerspectiveSafeCounterV1),
            typeof(PerspectiveSafeZoneV1),
            typeof(PerspectiveSafeKnowledgeV1),
            typeof(PerspectiveSafeFrameSourceInputV1),
            typeof(PerspectiveSafeFrameV1),
            typeof(PerspectiveSafeFrameSourceResultV1),
            typeof(PerspectiveSafePublicFrameSourceV1),
            typeof(PerspectiveSafeGlobalsV1),
            typeof(PerspectiveSafeCardPropertiesV1),
            typeof(PerspectiveSafeEntityV1),
            typeof(PerspectiveSafeRelationshipV1),
            typeof(PerspectiveSafeChainLinkV1),
            typeof(PerspectiveSafeChainStateV1),
            typeof(PerspectiveSafeVisibleEventV1),
            typeof(PerspectiveSafeDeckV1),
            typeof(PerspectiveSafeMatchContextV1),
            typeof(PerspectiveSafeI6C2SourceStatusV1),
            typeof(PerspectiveSafeI6C2ConstituentV1),
            typeof(PerspectiveSafeI6C2ConstituentStatusV1),
            typeof(PerspectiveSafeI6C2GlobalsV1),
            typeof(PerspectiveSafeI6C2StateSourceV1),
            typeof(PerspectiveSafeI6C2SourceResultV1),
            typeof(PerspectiveSafeI6C3SourceStatusV1),
            typeof(PerspectiveSafeI6C3ConstituentV1),
            typeof(PerspectiveSafeI6C3ConstituentStatusV1),
            typeof(PerspectiveSafeI6C3StateSourceV1),
            typeof(PerspectiveSafeI6C3SourceResultV1)
        };
        string[] forbidden =
        {
            "OCGForge.Ignis.Protocol",
            "OCGForge.Ignis.Client",
            "FlatPrompt",
            "MirrorEntityIdV1",
            "PrivateResponse",
            "Socket",
            "Stream",
            "DateTime",
            "Guid",
            "Random",
            "SHA256"
        };

        foreach (Type type in publicTypes)
        {
            AssertNoForbiddenType(type, forbidden);
            foreach (PropertyInfo property in type.GetProperties(
                         BindingFlags.Public |
                         BindingFlags.Instance |
                         BindingFlags.Static |
                         BindingFlags.DeclaredOnly))
            {
                AssertNoForbiddenType(property.PropertyType, forbidden);
                False(
                    property.Name is "CanonicalBytes" or "SourceHash" or
                        "PublicObservationDigest" or "ContractId" or "FrameId",
                    property.Name);
            }

            foreach (MethodInfo method in type.GetMethods(
                         BindingFlags.Public |
                         BindingFlags.Instance |
                         BindingFlags.Static |
                         BindingFlags.DeclaredOnly))
            {
                AssertNoForbiddenType(method.ReturnType, forbidden);
                foreach (ParameterInfo parameter in method.GetParameters())
                {
                    AssertNoForbiddenType(parameter.ParameterType, forbidden);
                }
            }
        }
    }

    private static void AssertStructuredFailureSurface()
    {
        PerspectiveSafeFrameSourceResultV1 result =
            PerspectiveSafePublicFrameSourceV1.TryCreate(null);
        Null(result.Frame);
        NotNull(result.Error);
        string rendered = result.Error!.Value.ToString();
        AssertDoesNotContainForbidden(
            rendered,
            new[]
            {
                "\\",
                "/",
                "payload",
                "MirrorEntity",
                "Socket",
                "127.0.0.1"
            });

        PropertyInfo[] properties =
            typeof(PerspectiveSafeFrameSourceErrorV1).GetProperties(
                BindingFlags.Public | BindingFlags.Instance);
        Equal(2, properties.Length);
        True(properties.All(property => property.PropertyType.IsEnum));
    }

    private static void AssertPrintableLocatorBoundaries()
    {
        PerspectiveSafeEntityV1 controlEntity = new(
            "entity-\n",
            identityKnown: false,
            passcode: null,
            owner: null,
            controller: 0,
            zone: PerspectiveSafeSemanticZoneV1.Hand,
            sequence: null,
            overlaySequence: null,
            position: PerspectiveSafePositionV1.Unknown,
            faceUp: false,
            faceDown: false);
        AssertFailure(
            CreateInput(
                CreateValidGlobals(),
                CreateValidZones(),
                new[] { controlEntity },
                CreateValidRelationships(),
                CreateValidChain(),
                CreateValidEvents(),
                CreateValidMatchContext()),
            PerspectiveSafeFrameSourceErrorCodeV1.InvalidLocator);

        PerspectiveSafeRelationshipV1 controlRelationship = new(
            PerspectiveSafeRelationshipKindV1.Target,
            "source\r",
            "target");
        AssertFailure(
            CreateInput(
                CreateValidGlobals(),
                CreateValidZones(),
                CreateValidEntities(),
                new[] { controlRelationship },
                CreateValidChain(),
                CreateValidEvents(),
                CreateValidMatchContext()),
            PerspectiveSafeFrameSourceErrorCodeV1.InvalidLocator);

        PerspectiveSafeChainLinkV1 controlChainLink = new(
            index: 0,
            source: "source\0",
            targets: new[] { "target" });
        AssertFailure(
            CreateInput(
                CreateValidGlobals(),
                CreateValidZones(),
                CreateValidEntities(),
                CreateValidRelationships(),
                new PerspectiveSafeChainStateV1(1, new[] { controlChainLink }),
                CreateValidEvents(),
                CreateValidMatchContext()),
            PerspectiveSafeFrameSourceErrorCodeV1.InvalidLocator);

        PerspectiveSafeVisibleEventV1 controlEvent = new(
            0,
            PerspectiveSafeVisibleEventKindV1.CardRevealed,
            entityLocator: "entity\u007f");
        AssertFailure(
            CreateInput(
                CreateValidGlobals(),
                CreateValidZones(),
                CreateValidEntities(),
                CreateValidRelationships(),
                CreateValidChain(),
                new[] { controlEvent },
                CreateValidMatchContext()),
            PerspectiveSafeFrameSourceErrorCodeV1.InvalidLocator);
    }

    private static void AssertCrossSectionInvariants()
    {
        PerspectiveSafeFrameSourceInputV1 wrongLifePointCardinality = CreateInput(
            new PerspectiveSafeGlobalsV1(
                duelFlags: 0x1234,
                lifePoints: new uint[] { 8000 },
                turnPlayer: 0,
                chainLength: 1),
            CreateValidZones(),
            CreateValidEntities(),
            CreateValidRelationships(),
            CreateValidChain(),
            CreateValidEvents(),
            CreateValidMatchContext());
        AssertFailure(
            wrongLifePointCardinality,
            PerspectiveSafeFrameSourceErrorCodeV1.InvalidLifePointCardinality);

        PerspectiveSafeFrameSourceInputV1 wrongChainLength = CreateInput(
            new PerspectiveSafeGlobalsV1(
                duelFlags: 0x1234,
                lifePoints: new uint[] { 8000, 7000 },
                turnPlayer: 0,
                chainLength: 2),
            CreateValidZones(),
            CreateValidEntities(),
            CreateValidRelationships(),
            CreateValidChain(),
            CreateValidEvents(),
            CreateValidMatchContext());
        AssertFailure(
            wrongChainLength,
            PerspectiveSafeFrameSourceErrorCodeV1.CrossSectionMismatch);

        PerspectiveSafeFrameSourceInputV1 wrongDuelFlags = CreateInput(
            CreateValidGlobals(),
            CreateValidZones(),
            CreateValidEntities(),
            CreateValidRelationships(),
            CreateValidChain(),
            CreateValidEvents(),
            new PerspectiveSafeMatchContextV1(
                perspectivePlayer: 0,
                duelFlags: 0x4321,
                knowledge: new(true, false),
                ownDeck: new(true, new uint[] { 1, 2 }, new uint[] { 3 }),
                opponentDeck: new(false)));
        AssertFailure(
            wrongDuelFlags,
            PerspectiveSafeFrameSourceErrorCodeV1.CrossSectionMismatch);
    }

    private static void AssertFirstInvalidInvariant()
    {
        PerspectiveSafeVisibleEventV1 invalidLocatorWithZone = new(
            0,
            PerspectiveSafeVisibleEventKindV1.CardMoved,
            entityLocator: "entity\n",
            fromZone: PerspectiveSafeSemanticZoneV1.Hand);
        AssertFailure(
            CreateInput(
                CreateValidGlobals(),
                CreateValidZones(),
                CreateValidEntities(),
                CreateValidRelationships(),
                CreateValidChain(),
                new[] { invalidLocatorWithZone },
                CreateValidMatchContext()),
            PerspectiveSafeFrameSourceErrorCodeV1.InvalidLocator);

        PerspectiveSafeChainLinkV1 invalidSourceWithZone = new(
            index: 0,
            source: "source\r",
            activationZone: PerspectiveSafeSemanticZoneV1.Hand);
        AssertFailure(
            CreateInput(
                CreateValidGlobals(),
                CreateValidZones(),
                CreateValidEntities(),
                CreateValidRelationships(),
                new PerspectiveSafeChainStateV1(1, new[] { invalidSourceWithZone }),
                CreateValidEvents(),
                CreateValidMatchContext()),
            PerspectiveSafeFrameSourceErrorCodeV1.InvalidLocator);
    }

    private static void TestI6C2MirrorSourceClosure()
    {
        Run("I6C2 missing Mirror fails closed", AssertI6C2MissingMirror);
        Run("I6C2 absolute globals", AssertI6C2AbsoluteGlobals);
        Run("I6C2 LP source and failed apply atomicity", AssertI6C2LifePoints);
        Run("I6C2 terminal winner and reason", AssertI6C2TerminalValues);
        Run("I6C2 ordinary zones and locators", AssertI6C2ZonesAndLocators);
        Run("I6C2 hidden-world privacy", AssertI6C2PairedPrivacy);
        Run("I6C2 knowledge destruction", AssertI6C2KnowledgeDestruction);
        Run("I6C2 current properties", AssertI6C2CurrentProperties);
        Run("I6C2 layout and overlay boundaries", AssertI6C2DeferredBoundaries);
        Run("I6C2 semantic ordering", AssertI6C2SemanticOrdering);
        Run("I6C2 cross-pile ordinal continuity", AssertI6C2CrossPileOrdinalContinuity);
        Run("I6C2 transport chunking", AssertI6C2TransportChunking);
        Run("I6C3 overlay and XyzMaterial source", AssertI6C3OverlayRelationSource);
        Run("I6C3 preserves I6C2 source", AssertI6C3PreservesI6C2Source);
        Run("I6C3 overlay lifecycle", AssertI6C3OverlayLifecycle);
        Run("I6C3 hidden overlay privacy", AssertI6C3HiddenOverlayPrivacy);
        Run("I6C3 hidden-only unrelated privacy", AssertI6C3HiddenOnlyPrivacy);
        Run("I6C3 relation lifecycle", AssertI6C3RelationLifecycle);
        Run("I6C3 chain target source separation", AssertI6C3ChainTargetSourceSeparation);
        Run("I6C3 visible opponent overlay identity", AssertI6C3VisibleOpponentOverlayIdentity);
        Run("I6C3 overlay raw position and fresh properties", AssertI6C3OverlayPositionAndFreshProperties);
        Run("I6C3 unresolved public endpoint fails closed", AssertI6C3UnresolvedPublicEndpoint);
        Run("I6C3 relation ordering and privacy", AssertI6C3RelationOrderingAndPrivacy);
        Run("I6C3 chain lifecycle", AssertI6C3ChainLifecycle);
        Run("I6C3 SZONE chain boundary", AssertI6C3SzoneChainBoundary);
        Run("I6C3 failed chain apply atomicity", AssertI6C3FailedChainApplyAtomicity);
        Run("I6C3 transport chunking", AssertI6C3TransportChunking);
        Run("I6C5 MSG_SWAP_GRAVE_DECK transition", AssertSwapGraveDeckTransition);
        Run("I6C4 draw event ledger", AssertI6C4DrawEventLedger);
        Run("I6C4 event index lifecycle", AssertI6C4EventIndexLifecycle);
        Run("I6C4 event kind mapping", AssertI6C4EventKindMapping);
        Run("I6C4 packet shapes and privacy", AssertI6C4PacketShapesAndPrivacy);
        Run("I6C4 shuffle boundary and knowledge destruction", AssertI6C4ShuffleBoundary);
        Run("I6C4 atomicity and overflow", AssertI6C4AtomicityAndOverflow);
        Run("I6C4 historical locators do not rebind", AssertI6C4HistoricalLocators);
        Run("I6C4 paired hidden worlds", AssertI6C4PairedPrivacy);
        Run("I6C4 transport chunking", AssertI6C4TransportChunking);
        Run("I6C5 outer public frame source", AssertI6C5OuterPublicFrameSource);
        Run("I6C5 run configuration immutability", AssertI6C5RunConfigurationImmutability);
        Run("I6C5 printed provider API", AssertI6C5PrintedProviderApi);
        Run("I6C5 printed provider contract", AssertI6C5PrintedProviderContract);
    }

    private static void AssertI6C5PrintedProviderApi()
    {
        Type? providerType = typeof(PerspectiveSafePublicFrameSourceV1)
            .Assembly
            .GetType("OCGForge.Ignis.Gameplay.PerspectiveSafePrintedProviderV1");
        NotNull(providerType);
    }

    private static void AssertI6C5PrintedProviderContract()
    {
        Run("synthetic semantic mappings", AssertPrintedProviderMappings);
        Run("missing provider fails closed", AssertPrintedProviderRequired);
        Run("hash and digest validation", AssertPrintedProviderDigests);
        Run("coverage and malformed rows fail closed", AssertPrintedProviderFailures);
        Run("environment compatibility fails closed", AssertPrintedProviderEnvironment);
        Run("unknown identities have no reverse lookup surface", AssertPrintedProviderPrivacySurface);
        Run("provider supplies Printed frame properties", AssertPrintedProviderFrameIntegration);
        Run("session missing provider fails closed", AssertPrintedProviderSessionMissing);
        Run("session provider binding is immutable", AssertPrintedProviderSessionBinding);
    }

    private static void AssertI6C5RunConfigurationImmutability()
    {
        MethodInfo? method = typeof(GameplayMirrorSessionV1)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .SingleOrDefault(candidate =>
                candidate.Name == "TryCreateI6C5Frame" &&
                candidate.GetParameters().Length == 0);
        NotNull(method);

        Run("I6C5 session without context fails closed", AssertI6C5NoContext);
        Run("I6C5 bound context remains stable", AssertI6C5BoundContextStability);
        Run("I6C5 sessions keep distinct contexts", AssertI6C5DistinctContexts);
        Run("I6C5 context perspective mismatch fails closed", AssertI6C5PerspectiveMismatch);
        Run("I6C5 invalid context fails closed", AssertI6C5InvalidContext);
        Run("I6C5 bound context owns caller values", AssertI6C5ContextOwnership);
        Run("I6C5 session surface has no replacement API", AssertI6C5SessionSurface);
    }

    private static void AssertSwapGraveDeckTransition()
    {
        foreach (byte player in new byte[] { 0, 1 })
        {
            GameplayMessageDecoderV1 decoder = CreateEstablishedDecoder(player);
            GameplayMessageDecodeResult decoded = decoder.Decode(
                new StocGameMessagePayload(
                    SwapGraveDeckMessage(player, 3, 0x05)));
            True(decoded.IsSuccess, decoded.Error.ToString());
            Equal(GameplayMessageKindV1.SwapGraveDeck, decoded.Message!.Kind);
            Equal(player, decoded.Message.SwapGraveDeck!.Player);
            Equal((uint)3, decoded.Message.SwapGraveDeck.ReportedExtraCount);
            True(decoded.Message.SwapGraveDeck.ExtraMask.SequenceEqual(
                new byte[] { 0x05 }));

            GameplayMessageDecodeResult invalidPlayer = decoder.Decode(
                new StocGameMessagePayload(
                    SwapGraveDeckMessage(2, 0, 0x00)));
            False(invalidPlayer.IsSuccess);
            Equal(GameplayErrorCode.InvalidParticipant, invalidPlayer.Error);

            GameplayMessageDecodeResult truncated = decoder.Decode(
                new StocGameMessagePayload(new byte[] { 35, player }));
            False(truncated.IsSuccess);
            Equal(GameplayErrorCode.MalformedGameMessage, truncated.Error);

            GameplayMessageDecodeResult mismatchedMask = decoder.Decode(
                new StocGameMessagePayload(
                    Join(new byte[] { 35, player }, U32(0), U32(1))));
            False(mismatchedMask.IsSuccess);
            Equal(GameplayErrorCode.MalformedGameMessage, mismatchedMask.Error);
        }

        (PerspectiveStateMirrorV1 emptyMirror,
            GameplayMessageDecoderV1 emptyDecoder) =
            CreateMirror(
                0,
                deckCount0: 0,
                extraCount0: 0,
                deckCount1: 0,
                extraCount1: 0);
        ApplyI6C4Success(
            emptyMirror,
            emptyDecoder,
            SwapGraveDeckMessage(0, 0));
        Equal(
            (uint)0,
            emptyMirror.Snapshot.GetZone(
                MirrorParticipantRoleV1.Self,
                MirrorZoneV1.MainDeck).Count.Value);
        Equal(
            (uint)0,
            emptyMirror.Snapshot.GetZone(
                MirrorParticipantRoleV1.Self,
                MirrorZoneV1.Graveyard).Count.Value);

        (PerspectiveStateMirrorV1 deckGraveMirror,
            GameplayMessageDecoderV1 deckGraveDecoder) =
            CreateMirror(
                0,
                deckCount0: 1,
                extraCount0: 0,
                deckCount1: 0,
                extraCount1: 0);
        ApplyI6C4Success(
            deckGraveMirror,
            deckGraveDecoder,
            MoveMessage(
                0xD200,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(0, 0x01, 0, 0x08),
                0));
        ApplyI6C4Success(
            deckGraveMirror,
            deckGraveDecoder,
            MoveMessage(
                0xD201,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(0, 0x10, 0, 0x05),
                0));
        ApplyI6C4Success(
            deckGraveMirror,
            deckGraveDecoder,
            SwapGraveDeckMessage(0, 0, 0x00));
        Equal(
            (uint)1,
            deckGraveMirror.Snapshot.GetZone(
                MirrorParticipantRoleV1.Self,
                MirrorZoneV1.MainDeck).Count.Value);
        Equal(
            (uint)2,
            deckGraveMirror.Snapshot.GetZone(
                MirrorParticipantRoleV1.Self,
                MirrorZoneV1.Graveyard).Count.Value);
        MirrorCardSnapshotV1 movedDeckCard = deckGraveMirror.Snapshot.GetZone(
            MirrorParticipantRoleV1.Self,
            MirrorZoneV1.Graveyard).Cards.Single();
        Equal((uint)0xD200, movedDeckCard.CardCode.Value);
        True(movedDeckCard.Position.IsKnown);
        Equal((uint)0x05, movedDeckCard.Position.Value);
        True(movedDeckCard.CardCode.Provenance ==
             MirrorProvenanceV1.PublicProtocolFact);

        (PerspectiveStateMirrorV1 bulkInsertMirror,
            GameplayMessageDecoderV1 bulkInsertDecoder) =
            CreateMirror(
                0,
                deckCount0: 0,
                extraCount0: 2,
                deckCount1: 0,
                extraCount1: 0);
        ApplyI6C4Success(
            bulkInsertMirror,
            bulkInsertDecoder,
            UpdateDataMessage(
                0,
                0x40,
                Join(
                    ExtraQuery(0xD300, 0, 0x08),
                    ExtraQuery(0xD301, 1, 0x05))));
        AddGraveCards(
            bulkInsertMirror,
            bulkInsertDecoder,
            0,
            0xD302,
            0xD303);
        MirrorApplyResult bulkInsertResult;
        try
        {
            bulkInsertResult = bulkInsertMirror.Apply(
                DecodeMessage(
                    bulkInsertDecoder,
                    SwapGraveDeckMessage(0, 1, 0x03)));
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                "multi-card Extra insertion escaped the structured apply path",
                exception);
        }

        True(
            bulkInsertResult.IsSuccess,
            $"multi-card Extra insertion failed: {bulkInsertResult.Error}");
        MirrorCardSnapshotV1[] bulkInsertCards =
            bulkInsertMirror.Snapshot.GetZone(
                MirrorParticipantRoleV1.Self,
                MirrorZoneV1.ExtraDeck).Cards.ToArray();
        Equal(4, bulkInsertCards.Length);
        Equal((uint)0xD300, bulkInsertCards[0].CardCode.Value);
        Equal((uint)0xD302, bulkInsertCards[1].CardCode.Value);
        Equal((uint)0xD303, bulkInsertCards[2].CardCode.Value);
        Equal((uint)0xD301, bulkInsertCards[3].CardCode.Value);

        (PerspectiveStateMirrorV1 interleavedMirror,
            GameplayMessageDecoderV1 interleavedDecoder) =
            CreateMirror(
                0,
                deckCount0: 0,
                extraCount0: 2,
                deckCount1: 0,
                extraCount1: 0);
        ApplyI6C4Success(
            interleavedMirror,
            interleavedDecoder,
            UpdateDataMessage(
                0,
                0x40,
                Join(
                    ExtraQuery(0xD400, 0, 0x08),
                    ExtraQuery(0xD401, 1, 0x05))));
        AddGraveCards(
            interleavedMirror,
            interleavedDecoder,
            0,
            0xD402,
            0xD403,
            0xD404);
        ApplyI6C4Success(
            interleavedMirror,
            interleavedDecoder,
            SwapGraveDeckMessage(0, 1, 0x05));
        MirrorCardSnapshotV1[] interleavedCards =
            interleavedMirror.Snapshot.GetZone(
                MirrorParticipantRoleV1.Self,
                MirrorZoneV1.ExtraDeck).Cards.ToArray();
        Equal(4, interleavedCards.Length);
        Equal((uint)0xD400, interleavedCards[0].CardCode.Value);
        Equal((uint)0xD402, interleavedCards[1].CardCode.Value);
        Equal((uint)0xD404, interleavedCards[2].CardCode.Value);
        Equal((uint)0xD401, interleavedCards[3].CardCode.Value);

        (PerspectiveStateMirrorV1 orderedMirror,
            GameplayMessageDecoderV1 orderedDecoder) =
            CreateMirror(
                0,
                deckCount0: 0,
                extraCount0: 2,
                deckCount1: 0,
                extraCount1: 0);
        ApplyI6C4Success(
            orderedMirror,
            orderedDecoder,
            UpdateDataMessage(
                0,
                0x40,
                Join(
                    ExtraQuery(0xA000, 0, 0x08),
                    ExtraQuery(0xA001, 1, 0x05))));
        ApplyI6C4Success(
            orderedMirror,
            orderedDecoder,
            MoveMessage(
                0xB000,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(0, 0x10, 0, 0x05),
                0));
        string orderedBefore = orderedMirror.Snapshot.ToDeterministicString();
        int orderedEventCount = orderedMirror.VisibleEvents.Count;
        ulong orderedEventIndex = orderedMirror.NextEventIndex;
        ApplyI6C4Success(
            orderedMirror,
            orderedDecoder,
            SwapGraveDeckMessage(0, 1, 0x01));
        Equal(
            orderedEventCount,
            orderedMirror.VisibleEvents.Count);
        Equal(orderedEventIndex, orderedMirror.NextEventIndex);
        NotEqual(orderedBefore, orderedMirror.Snapshot.ToDeterministicString());
        Equal(
            (uint)0,
            orderedMirror.Snapshot.GetZone(
                MirrorParticipantRoleV1.Self,
                MirrorZoneV1.MainDeck).Count.Value);
        Equal(
            (uint)0,
            orderedMirror.Snapshot.GetZone(
                MirrorParticipantRoleV1.Self,
                MirrorZoneV1.Graveyard).Count.Value);
        MirrorCardSnapshotV1[] orderedExtra = orderedMirror.Snapshot.GetZone(
            MirrorParticipantRoleV1.Self,
            MirrorZoneV1.ExtraDeck).Cards.ToArray();
        Equal(3, orderedExtra.Length);
        Equal((uint)0xA000, orderedExtra[0].CardCode.Value);
        Equal((uint)0xB000, orderedExtra[1].CardCode.Value);
        Equal((uint)0xA001, orderedExtra[2].CardCode.Value);
        Equal((uint)0, orderedExtra[0].Sequence);
        Equal((uint)1, orderedExtra[1].Sequence);
        Equal((uint)2, orderedExtra[2].Sequence);
        NotEqual(orderedExtra[0].EntityId, orderedExtra[1].EntityId);
        NotEqual(orderedExtra[1].EntityId, orderedExtra[2].EntityId);
        True(orderedExtra[1].CardCode.Provenance ==
             MirrorProvenanceV1.PerspectivePrivateFact);

        ApplyI6C4Success(
            orderedMirror,
            orderedDecoder,
            new byte[] { 32, 0 });
        ApplyI6C4Success(
            orderedMirror,
            orderedDecoder,
            MoveMessage(
                0xA000,
                new ModernLocInfoV1(0, 0x40, 0, 0x08),
                new ModernLocInfoV1(0, 0x04, 0, 0x05),
                0));
        ApplyI6C4Success(
            orderedMirror,
            orderedDecoder,
            MoveMessage(
                0xA000,
                new ModernLocInfoV1(0, 0x04, 0, 0x05),
                new ModernLocInfoV1(0, 0x40, 2, 0x08),
                0));
        Equal(
            (uint)3,
            orderedMirror.Snapshot.GetZone(
                MirrorParticipantRoleV1.Self,
                MirrorZoneV1.ExtraDeck).Count.Value);

        (PerspectiveStateMirrorV1 duplicateMirror,
            GameplayMessageDecoderV1 duplicateDecoder) =
            CreateMirror(
                0,
                deckCount0: 0,
                extraCount0: 0,
                deckCount1: 0,
                extraCount1: 0);
        AddGraveCards(
            duplicateMirror,
            duplicateDecoder,
            0,
            0xC100,
            0xC100,
            0xC200);
        ApplyI6C4Success(
            duplicateMirror,
            duplicateDecoder,
            SwapGraveDeckMessage(0, 0, 0x07));
        MirrorCardSnapshotV1[] duplicateCards = duplicateMirror.Snapshot.GetZone(
            MirrorParticipantRoleV1.Self,
            MirrorZoneV1.ExtraDeck).Cards.ToArray();
        Equal(3, duplicateCards.Length);
        Equal((uint)0xC100, duplicateCards[0].CardCode.Value);
        Equal((uint)0xC100, duplicateCards[1].CardCode.Value);
        Equal((uint)0xC200, duplicateCards[2].CardCode.Value);
        NotEqual(duplicateCards[0].EntityId, duplicateCards[1].EntityId);

        (PerspectiveStateMirrorV1 playerOneMirror,
            GameplayMessageDecoderV1 playerOneDecoder) =
            CreateMirror(
                1,
                deckCount0: 0,
                extraCount0: 0,
                deckCount1: 0,
                extraCount1: 0);
        AddGraveCards(
            playerOneMirror,
            playerOneDecoder,
            1,
            0xD100,
            0xD101);
        ApplyI6C4Success(
            playerOneMirror,
            playerOneDecoder,
            SwapGraveDeckMessage(1, 0, 0x03));
        MirrorCardSnapshotV1[] playerOneCards = playerOneMirror.Snapshot.GetZone(
            MirrorParticipantRoleV1.Self,
            MirrorZoneV1.ExtraDeck).Cards.ToArray();
        Equal(2, playerOneCards.Length);
        True(playerOneCards.All(card => card.CardCode.IsKnown));
        True(playerOneCards.All(card => card.CardCode.Provenance ==
            MirrorProvenanceV1.PerspectivePrivateFact));

        PerspectiveStateMirrorV1 opponentWorldA =
            CreateOpponentSwapWorld(0xE100);
        PerspectiveStateMirrorV1 opponentWorldB =
            CreateOpponentSwapWorld(0xF100);
        MirrorCardSnapshotV1[] opponentCards = opponentWorldA.Snapshot.GetZone(
            MirrorParticipantRoleV1.Opponent,
            MirrorZoneV1.ExtraDeck).Cards.ToArray();
        Equal(2, opponentCards.Length);
        Equal(
            0,
            opponentWorldA.Snapshot.GetZone(
                MirrorParticipantRoleV1.Opponent,
                MirrorZoneV1.Graveyard).Cards.Count);
        True(opponentCards.All(card => !card.CardCode.IsKnown));
        Equal(
            opponentWorldA.Snapshot.ToDeterministicString(),
            opponentWorldB.Snapshot.ToDeterministicString());
        PerspectiveSafePrintedProviderV1 opponentProvider =
            CreatePrintedProviderForMirror(opponentWorldA);
        PerspectiveSafeFrameSourceResultV1 opponentFrameA =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C5(
                opponentWorldA,
                CreateValidI6C5MatchContext(),
                opponentProvider);
        PerspectiveSafeFrameSourceResultV1 opponentFrameB =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C5(
                opponentWorldB,
                CreateValidI6C5MatchContext(),
                opponentProvider);
        True(opponentFrameA.IsSuccess);
        True(opponentFrameB.IsSuccess);
        Equal(0, opponentFrameA.Frame!.Entities.Count);
        Equal(0, opponentFrameB.Frame!.Entities.Count);
        True(opponentFrameA.Frame!.Entities.All(
            entity => entity.Zone == PerspectiveSafeSemanticZoneV1.ExtraDeck));
        True(opponentFrameB.Frame!.Entities.All(
            entity => entity.Zone == PerspectiveSafeSemanticZoneV1.ExtraDeck));
        Equal(
            FrameSignature(opponentFrameA.Frame!),
            FrameSignature(opponentFrameB.Frame!));

        (PerspectiveStateMirrorV1 frameMirror,
            GameplayMessageDecoderV1 frameDecoder) =
            CreateMirror(
                0,
                deckCount0: 0,
                extraCount0: 0,
                deckCount1: 0,
                extraCount1: 0);
        AddGraveCards(frameMirror, frameDecoder, 0, 0xF200, 0xF201);
        ApplyI6C4Success(
            frameMirror,
            frameDecoder,
            SwapGraveDeckMessage(0, 0, 0x03));
        PerspectiveSafeFrameSourceResultV1 frame =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C5(
                frameMirror,
                CreateValidI6C5MatchContext(),
                CreatePrintedProviderForMirror(frameMirror));
        True(frame.IsSuccess, frame.Error?.ToString() ?? "frame rejected");
        True(frame.Frame!.Entities.Any(entity => entity.Passcode == 0xF200));
        True(frame.Frame.Entities.Any(entity => entity.Passcode == 0xF201));

        (PerspectiveStateMirrorV1 malformedMirror,
            GameplayMessageDecoderV1 malformedDecoder) =
            CreateMirror(
                0,
                deckCount0: 0,
                extraCount0: 0,
                deckCount1: 0,
                extraCount1: 0);
        AddGraveCards(malformedMirror, malformedDecoder, 0, 0xA300);
        string malformedBefore = malformedMirror.Snapshot.ToDeterministicString();
        MirrorApplyResult malformed = malformedMirror.Apply(
            DecodeMessage(
                malformedDecoder,
                SwapGraveDeckMessage(0, 0, 0x02)));
        False(malformed.IsSuccess);
        Equal(GameplayErrorCode.InvalidStateTransition, malformed.Error);
        Equal(malformedBefore, malformedMirror.Snapshot.ToDeterministicString());

        (PerspectiveStateMirrorV1 countMirror,
            GameplayMessageDecoderV1 countDecoder) =
            CreateMirror(
                0,
                deckCount0: 0,
                extraCount0: 0,
                deckCount1: 0,
                extraCount1: 0);
        AddGraveCards(countMirror, countDecoder, 0, 0xA400);
        string countBefore = countMirror.Snapshot.ToDeterministicString();
        MirrorApplyResult countFailure = countMirror.Apply(
            DecodeMessage(
                countDecoder,
                SwapGraveDeckMessage(0, 1, 0x01)));
        False(countFailure.IsSuccess);
        Equal(GameplayErrorCode.StateCapacityExceeded, countFailure.Error);
        Equal(countBefore, countMirror.Snapshot.ToDeterministicString());
    }

    private static void AddGraveCards(
        PerspectiveStateMirrorV1 mirror,
        GameplayMessageDecoderV1 decoder,
        byte player,
        params uint[] cardCodes)
    {
        for (uint sequence = 0; sequence < cardCodes.Length; sequence++)
        {
            ApplyI6C4Success(
                mirror,
                decoder,
                MoveMessage(
                    cardCodes[sequence],
                    new ModernLocInfoV1(0, 0, 0, 0),
                    new ModernLocInfoV1(player, 0x10, sequence, 0x05),
                    0));
        }
    }

    private static PerspectiveStateMirrorV1 CreateOpponentSwapWorld(
        uint firstCode)
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(
                0,
                deckCount0: 0,
                extraCount0: 0,
                deckCount1: 0,
                extraCount1: 0);
        AddGraveCards(mirror, decoder, 1, 0, 0);
        ApplyI6C4Success(
            mirror,
            decoder,
            UpdateDataMessage(
                1,
                0x10,
                Join(
                    ExtraQuery(firstCode, 1, 0x05),
                    ExtraQuery(firstCode + 1, 1, 0x05))));
        ApplyI6C4Success(
            mirror,
            decoder,
            SwapGraveDeckMessage(1, 0, 0x03));
        return mirror;
    }

    private static void AssertI6C4DrawEventLedger()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0, deckCount0: 2);

        ApplyMirrorMessage(
            mirror,
            decoder,
            DrawMessage(0, (0x1000u, 0x05u), (0x1001u, 0x05u)));

        Equal(3, mirror.VisibleEvents.Count);
        Equal(PerspectiveSafeVisibleEventKindV1.Draw, mirror.VisibleEvents[0].Kind);
        Equal(PerspectiveSafeVisibleEventKindV1.CardRevealed, mirror.VisibleEvents[1].Kind);
        Equal(PerspectiveSafeVisibleEventKindV1.CardRevealed, mirror.VisibleEvents[2].Kind);
        Equal((ulong)0, mirror.VisibleEvents[0].EventIndex);
        Equal((ulong)1, mirror.VisibleEvents[1].EventIndex);
        Equal((ulong)2, mirror.VisibleEvents[2].EventIndex);
    }

    private static void AssertI6C4EventIndexLifecycle()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0, extraCount0: 0);
        Equal((ulong)0, mirror.NextEventIndex);
        Equal(0, mirror.VisibleEvents.Count);

        ApplyI6C4Success(mirror, decoder, new byte[] { 100, 0, 1, 0, 0, 0 });
        Equal((ulong)0, mirror.NextEventIndex);
        Equal(0, mirror.VisibleEvents.Count);

        ApplyI6C4Success(mirror, decoder, new byte[] { 40, 0 });
        Equal((ulong)1, mirror.NextEventIndex);
        Equal((ulong)0, mirror.VisibleEvents[0].EventIndex);
        Equal(PerspectiveSafeVisibleEventKindV1.TurnStarted, mirror.VisibleEvents[0].Kind);

        (PerspectiveStateMirrorV1 drawMirror, GameplayMessageDecoderV1 drawDecoder) =
            CreateMirror(0, deckCount0: 2);
        ApplyI6C4Success(
            drawMirror,
            drawDecoder,
            DrawMessage(0, (0x1000u, 0x05u), (0x1001u, 0x05u)));
        Equal(3, drawMirror.VisibleEvents.Count);
        Equal((ulong)0, drawMirror.VisibleEvents[0].EventIndex);
        Equal((ulong)1, drawMirror.VisibleEvents[1].EventIndex);
        Equal((ulong)2, drawMirror.VisibleEvents[2].EventIndex);
        Equal((ulong)3, drawMirror.NextEventIndex);

        (PerspectiveStateMirrorV1 shuffleMirror, GameplayMessageDecoderV1 shuffleDecoder) =
            CreateMirror(0);
        ApplyI6C4Success(shuffleMirror, shuffleDecoder, new byte[] { 32, 1 });
        ApplyI6C4Success(shuffleMirror, shuffleDecoder, new byte[] { 37 });
        Equal(4, shuffleMirror.VisibleEvents.Count);
        Equal((ulong)0, shuffleMirror.VisibleEvents[0].EventIndex);
        Equal((ulong)1, shuffleMirror.VisibleEvents[1].EventIndex);
        Equal((ulong)2, shuffleMirror.VisibleEvents[2].EventIndex);
        Equal((ulong)3, shuffleMirror.VisibleEvents[3].EventIndex);
        Equal((ulong)4, shuffleMirror.NextEventIndex);
    }

    private static void AssertI6C4EventKindMapping()
    {
        (PerspectiveStateMirrorV1 turnMirror, GameplayMessageDecoderV1 turnDecoder) =
            CreateMirror(0);
        ApplyI6C4Success(turnMirror, turnDecoder, new byte[] { 40, 1 });
        Equal(PerspectiveSafeVisibleEventKindV1.TurnStarted, LastI6C4Event(turnMirror).Kind);
        ApplyI6C4Success(turnMirror, turnDecoder, new byte[] { 41, 4, 0 });
        Equal(PerspectiveSafeVisibleEventKindV1.PhaseChanged, LastI6C4Event(turnMirror).Kind);

        (PerspectiveStateMirrorV1 moveMirror, GameplayMessageDecoderV1 moveDecoder) =
            CreateMirror(0);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        ModernLocInfoV1 monster = new(0, 0x04, 0, 0x01);
        ApplyI6C4Success(moveMirror, moveDecoder, MoveMessage(0x1000, empty, monster, 0));
        Equal(PerspectiveSafeVisibleEventKindV1.CardMoved, LastI6C4Event(moveMirror).Kind);
        Equal("p0:MONSTER_ZONE:0", LastI6C4Event(moveMirror).EntityLocator);
        Equal((uint)0x1000, LastI6C4Event(moveMirror).PublicPasscode);

        (PerspectiveStateMirrorV1 destroyedMirror, GameplayMessageDecoderV1 destroyedDecoder) =
            CreateMirror(0);
        ApplyI6C4Success(destroyedMirror, destroyedDecoder, MoveMessage(0x1001, empty, monster, 0));
        ApplyI6C4Success(
            destroyedMirror,
            destroyedDecoder,
            MoveMessage(0x1001, monster, new ModernLocInfoV1(0, 0x10, 0, 0x01), 0x01));
        Equal(PerspectiveSafeVisibleEventKindV1.CardDestroyed, LastI6C4Event(destroyedMirror).Kind);

        (PerspectiveStateMirrorV1 banishedMirror, GameplayMessageDecoderV1 banishedDecoder) =
            CreateMirror(0);
        ApplyI6C4Success(banishedMirror, banishedDecoder, MoveMessage(0x1002, empty, monster, 0));
        ApplyI6C4Success(
            banishedMirror,
            banishedDecoder,
            MoveMessage(0x1002, monster, new ModernLocInfoV1(0, 0x20, 0, 0x01), 0));
        Equal(PerspectiveSafeVisibleEventKindV1.CardBanished, LastI6C4Event(banishedMirror).Kind);

        (PerspectiveStateMirrorV1 returnedMirror, GameplayMessageDecoderV1 returnedDecoder) =
            CreateMirror(0);
        ModernLocInfoV1 graveyard = new(0, 0x10, 0, 0x01);
        ApplyI6C4Success(returnedMirror, returnedDecoder, MoveMessage(0x1003, empty, graveyard, 0));
        ApplyI6C4Success(returnedMirror, returnedDecoder, MoveMessage(0x1003, graveyard, monster, 0));
        Equal(PerspectiveSafeVisibleEventKindV1.CardReturned, LastI6C4Event(returnedMirror).Kind);

        (PerspectiveStateMirrorV1 positionMirror, GameplayMessageDecoderV1 positionDecoder) =
            CreateMirror(0);
        ApplyI6C4Success(positionMirror, positionDecoder, MoveMessage(0x1004, empty, monster, 0));
        ApplyI6C4Success(positionMirror, positionDecoder, PosChangeMessage(0, 0x04, 0, 0x01, 0x08));
        Equal(PerspectiveSafeVisibleEventKindV1.PositionChanged, LastI6C4Event(positionMirror).Kind);

        (PerspectiveStateMirrorV1 presentationMirror, GameplayMessageDecoderV1 presentationDecoder) =
            CreateMirror(0);
        ApplyI6C4Success(presentationMirror, presentationDecoder, SetMessage(0x1005, monster));
        Equal(PerspectiveSafeVisibleEventKindV1.Set, LastI6C4Event(presentationMirror).Kind);
        ApplyI6C4Success(
            presentationMirror,
            presentationDecoder,
            SummoningMessage(60, 0x1006, monster));
        Equal(PerspectiveSafeVisibleEventKindV1.Summoned, LastI6C4Event(presentationMirror).Kind);
        ApplyI6C4Success(presentationMirror, presentationDecoder, new byte[] { 61 });
        Equal(PerspectiveSafeVisibleEventKindV1.Summoned, LastI6C4Event(presentationMirror).Kind);
        ApplyI6C4Success(
            presentationMirror,
            presentationDecoder,
            SummoningMessage(62, 0x1007, monster));
        Equal(PerspectiveSafeVisibleEventKindV1.Summoned, LastI6C4Event(presentationMirror).Kind);
        ApplyI6C4Success(presentationMirror, presentationDecoder, new byte[] { 63 });
        Equal(PerspectiveSafeVisibleEventKindV1.Summoned, LastI6C4Event(presentationMirror).Kind);
        ApplyI6C4Success(
            presentationMirror,
            presentationDecoder,
            SummoningMessage(64, 0x1008, monster));
        Equal(PerspectiveSafeVisibleEventKindV1.Summoned, LastI6C4Event(presentationMirror).Kind);
        ApplyI6C4Success(presentationMirror, presentationDecoder, new byte[] { 65 });
        Equal(PerspectiveSafeVisibleEventKindV1.Summoned, LastI6C4Event(presentationMirror).Kind);

        (PerspectiveStateMirrorV1 lifeMirror, GameplayMessageDecoderV1 lifeDecoder) =
            CreateMirror(0);
        ApplyI6C4Success(lifeMirror, lifeDecoder, new byte[] { 94, 0, 0xf4, 0x01, 0, 0 });
        Equal(PerspectiveSafeVisibleEventKindV1.LifePointsChanged, LastI6C4Event(lifeMirror).Kind);
        Equal(500, LastI6C4Event(lifeMirror).Amount);
        ApplyI6C4Success(lifeMirror, lifeDecoder, new byte[] { 91, 0, 0x64, 0, 0, 0 });
        Equal(-100, LastI6C4Event(lifeMirror).Amount);
        ApplyI6C4Success(lifeMirror, lifeDecoder, new byte[] { 92, 0, 0x32, 0, 0, 0 });
        Equal(50, LastI6C4Event(lifeMirror).Amount);

        (PerspectiveStateMirrorV1 chainMirror, GameplayMessageDecoderV1 chainDecoder) =
            CreateMirror(0);
        ModernLocInfoV1 chainTarget = new(0, 0x04, 1, 0x01);
        ApplyI6C4Success(chainMirror, chainDecoder, MoveMessage(0x1007, empty, monster, 0));
        ApplyI6C4Success(chainMirror, chainDecoder, MoveMessage(0x1008, empty, chainTarget, 0));
        ApplyI6C4Success(chainMirror, chainDecoder, ChainingMessage(monster, 1, 0x1007));
        Equal(PerspectiveSafeVisibleEventKindV1.ChainActivated, LastI6C4Event(chainMirror).Kind);
        ApplyI6C4Success(chainMirror, chainDecoder, new byte[] { 71, 1 });
        Equal(PerspectiveSafeVisibleEventKindV1.ChainActivated, LastI6C4Event(chainMirror).Kind);
        ApplyI6C4Success(chainMirror, chainDecoder, BecomeTargetMessage(chainTarget));
        Equal(PerspectiveSafeVisibleEventKindV1.Targeted, LastI6C4Event(chainMirror).Kind);
        ApplyI6C4Success(chainMirror, chainDecoder, new byte[] { 72, 1 });
        Equal(PerspectiveSafeVisibleEventKindV1.ChainResolved, LastI6C4Event(chainMirror).Kind);
        ApplyI6C4Success(chainMirror, chainDecoder, new byte[] { 73, 1 });
        Equal(PerspectiveSafeVisibleEventKindV1.ChainResolved, LastI6C4Event(chainMirror).Kind);
        ApplyI6C4Success(chainMirror, chainDecoder, new byte[] { 74 });
        Equal(PerspectiveSafeVisibleEventKindV1.ChainEnded, LastI6C4Event(chainMirror).Kind);

        (PerspectiveStateMirrorV1 negatedMirror, GameplayMessageDecoderV1 negatedDecoder) =
            CreateMirror(0);
        ApplyI6C4Success(negatedMirror, negatedDecoder, MoveMessage(0x1010, empty, monster, 0));
        ApplyI6C4Success(negatedMirror, negatedDecoder, ChainingMessage(monster, 1, 0x1010));
        ApplyI6C4Success(negatedMirror, negatedDecoder, new byte[] { 71, 1 });
        int negatedEventCount = negatedMirror.VisibleEvents.Count;
        ApplyI6C4Success(negatedMirror, negatedDecoder, new byte[] { 75, 1 });
        Equal(negatedEventCount, negatedMirror.VisibleEvents.Count);

        (PerspectiveStateMirrorV1 disabledMirror, GameplayMessageDecoderV1 disabledDecoder) =
            CreateMirror(0);
        ApplyI6C4Success(disabledMirror, disabledDecoder, MoveMessage(0x1011, empty, monster, 0));
        ApplyI6C4Success(disabledMirror, disabledDecoder, ChainingMessage(monster, 1, 0x1011));
        ApplyI6C4Success(disabledMirror, disabledDecoder, new byte[] { 71, 1 });
        int disabledEventCount = disabledMirror.VisibleEvents.Count;
        ApplyI6C4Success(disabledMirror, disabledDecoder, new byte[] { 76, 1 });
        Equal(disabledEventCount, disabledMirror.VisibleEvents.Count);

        (PerspectiveStateMirrorV1 relationMirror, GameplayMessageDecoderV1 relationDecoder) =
            CreateMirror(0);
        ModernLocInfoV1 target = new(0, 0x04, 1, 0x01);
        ApplyI6C4Success(relationMirror, relationDecoder, MoveMessage(0x1008, empty, monster, 0));
        ApplyI6C4Success(relationMirror, relationDecoder, MoveMessage(0x1009, empty, target, 0));
        ApplyI6C4Success(relationMirror, relationDecoder, EquipMessage(monster, target));
        Equal(PerspectiveSafeVisibleEventKindV1.Equipped, LastI6C4Event(relationMirror).Kind);
        ApplyI6C4Success(relationMirror, relationDecoder, CardTargetMessage(monster, target));
        Equal(PerspectiveSafeVisibleEventKindV1.Targeted, LastI6C4Event(relationMirror).Kind);
        ApplyI6C4Success(relationMirror, relationDecoder, CardTargetMessage(monster, target, cancel: true));
        Equal(PerspectiveSafeVisibleEventKindV1.Targeted, LastI6C4Event(relationMirror).Kind);

        (PerspectiveStateMirrorV1 counterMirror, GameplayMessageDecoderV1 counterDecoder) =
            CreateMirror(0);
        ApplyI6C4Success(
            counterMirror,
            counterDecoder,
            MoveMessage(0x1012, empty, monster, 0));
        ApplyI6C4Success(counterMirror, counterDecoder, CounterMessage(101, 7, 0, 0x04, 0, 3));
        Equal(PerspectiveSafeVisibleEventKindV1.CounterChanged, LastI6C4Event(counterMirror).Kind);
        Equal((uint)7, LastI6C4Event(counterMirror).CounterType);
        Equal(3, LastI6C4Event(counterMirror).Amount);
        ApplyI6C4Success(counterMirror, counterDecoder, CounterMessage(102, 7, 0, 0x04, 0, 1));
        Equal(PerspectiveSafeVisibleEventKindV1.CounterChanged, LastI6C4Event(counterMirror).Kind);
        Equal(1, LastI6C4Event(counterMirror).Amount);

        (PerspectiveStateMirrorV1 winMirror, GameplayMessageDecoderV1 winDecoder) =
            CreateMirror(0);
        ApplyI6C4Success(winMirror, winDecoder, new byte[] { 5, 0, 7 });
        Equal(PerspectiveSafeVisibleEventKindV1.Win, LastI6C4Event(winMirror).Kind);
        Equal((byte)0, LastI6C4Event(winMirror).Winner);
        Equal((byte)7, LastI6C4Event(winMirror).WinReason);

        (PerspectiveStateMirrorV1 drawWinMirror, GameplayMessageDecoderV1 drawWinDecoder) =
            CreateMirror(0);
        ApplyI6C4Success(drawWinMirror, drawWinDecoder, new byte[] { 5, 2, 8 });
        Equal((byte)2, LastI6C4Event(drawWinMirror).Winner);

        False(winMirror.VisibleEvents.Any(
            value => value.Kind == PerspectiveSafeVisibleEventKindV1.Unknown));
    }

    private static void AssertI6C4PacketShapesAndPrivacy()
    {
        (PerspectiveStateMirrorV1 compactMirror, GameplayMessageDecoderV1 compactDecoder) =
            CreateMirror(0);
        ApplyI6C4Success(
            compactMirror,
            compactDecoder,
            ConfirmMessage(31, 0, (0x2000u, new ModernLocInfoV1(1, 0x02, 2, 0))));
        PerspectiveSafeVisibleEventV1 compactEvent = LastI6C4Event(compactMirror);
        Equal(PerspectiveSafeVisibleEventKindV1.CardRevealed, compactEvent.Kind);
        Equal((uint)0x2000, compactEvent.PublicPasscode);
        Equal("p1:HAND:2", compactEvent.EntityLocator);
        Equal(PerspectiveSafeSemanticZoneV1.Hand, compactEvent.ToZone);
        Equal((byte)0x02, compactMirror.EventSourceFacts[0].SourceLocations[0].Location);

        (PerspectiveStateMirrorV1 deckTopMirror, GameplayMessageDecoderV1 deckTopDecoder) =
            CreateMirror(0);
        ApplyI6C4Success(
            deckTopMirror,
            deckTopDecoder,
            ConfirmMessage(
                30,
                0,
                (0x2003u, new ModernLocInfoV1(0, 0x04, 4, 0x01))));
        Equal(PerspectiveSafeVisibleEventKindV1.CardRevealed, LastI6C4Event(deckTopMirror).Kind);
        Equal((uint)0x2003, LastI6C4Event(deckTopMirror).PublicPasscode);

        (PerspectiveStateMirrorV1 extraTopMirror, GameplayMessageDecoderV1 extraTopDecoder) =
            CreateMirror(0);
        ApplyI6C4Success(
            extraTopMirror,
            extraTopDecoder,
            ConfirmMessage(
                42,
                0,
                (0x2004u, new ModernLocInfoV1(0, 0x40, 2, 0x01))));
        Equal(PerspectiveSafeVisibleEventKindV1.CardRevealed, LastI6C4Event(extraTopMirror).Kind);
        Equal((uint)0x2004, LastI6C4Event(extraTopMirror).PublicPasscode);

        (PerspectiveStateMirrorV1 zeroConfirmMirror, GameplayMessageDecoderV1 zeroConfirmDecoder) =
            CreateMirror(0);
        ApplyI6C4Success(
            zeroConfirmMirror,
            zeroConfirmDecoder,
            new byte[] { 31, 0, 0, 0, 0, 0 });
        Equal(0, zeroConfirmMirror.VisibleEvents.Count);
        Equal((ulong)0, zeroConfirmMirror.NextEventIndex);

        (PerspectiveStateMirrorV1 extendedMirror, GameplayMessageDecoderV1 extendedDecoder) =
            CreateMirror(0);
        ApplyI6C4Success(
            extendedMirror,
            extendedDecoder,
            ConfirmMessage(
                42,
                1,
                (0x2001u, new ModernLocInfoV1(1, 0x04, 3, 0x01)),
                extended: true));
        PerspectiveSafeVisibleEventV1 extendedEvent = LastI6C4Event(extendedMirror);
        Null(extendedEvent.PublicPasscode);
        Null(extendedEvent.EntityLocator);
        Equal(PerspectiveSafeSemanticZoneV1.MonsterZone, extendedEvent.ToZone);

        (PerspectiveStateMirrorV1 extraConfirmMirror, GameplayMessageDecoderV1 extraConfirmDecoder) =
            CreateMirror(0);
        ApplyI6C4Success(
            extraConfirmMirror,
            extraConfirmDecoder,
            ConfirmMessage(
                42,
                0,
                (0x2002u, new ModernLocInfoV1(1, 0x40, 1, 0x08))));
        Equal(PerspectiveSafeVisibleEventKindV1.CardRevealed, LastI6C4Event(extraConfirmMirror).Kind);
        Equal((uint)0x2002, LastI6C4Event(extraConfirmMirror).PublicPasscode);
        Equal("p1:EXTRA_DECK:1", LastI6C4Event(extraConfirmMirror).EntityLocator);

        GameplayMessageDecodeResult malformedConfirm = compactDecoder.Decode(
            new StocGameMessagePayload(new byte[] { 31, 0, 1, 0, 0, 0, 0 }));
        False(malformedConfirm.IsSuccess);
        Equal(GameplayErrorCode.QueryLengthMismatch, malformedConfirm.Error);

        GameplayMessageDecodeResult emptySummoning = compactDecoder.Decode(
            new StocGameMessagePayload(
                SummoningMessage(60, 0x2005, new ModernLocInfoV1(0, 0, 0, 0))));
        False(emptySummoning.IsSuccess);
        Equal(GameplayErrorCode.InvalidLocation, emptySummoning.Error);

        GameplayMessageDecodeResult emptyConfirm = compactDecoder.Decode(
            new StocGameMessagePayload(
                ConfirmMessage(31, 0, (0x2006u, new ModernLocInfoV1(0, 0, 0, 0)))));
        False(emptyConfirm.IsSuccess);
        Equal(GameplayErrorCode.InvalidLocation, emptyConfirm.Error);

        GameplayMessageDecodeResult emptyCounter = compactDecoder.Decode(
            new StocGameMessagePayload(CounterMessage(101, 7, 0, 0, 0, 1)));
        False(emptyCounter.IsSuccess);
        Equal(GameplayErrorCode.InvalidLocation, emptyCounter.Error);

        (PerspectiveStateMirrorV1 shuffleMirror, GameplayMessageDecoderV1 shuffleDecoder) =
            CreateMirror(0);
        ApplyI6C4Success(
            shuffleMirror,
            shuffleDecoder,
            ShuffleSetCardMessage(
                0x04,
                new[]
                {
                    new ModernLocInfoV1(0, 0x04, 0, 0x08),
                    new ModernLocInfoV1(0, 0x04, 1, 0x08)
                },
                new[]
                {
                    new ModernLocInfoV1(0, 0x04, 1, 0x08),
                    new ModernLocInfoV1(0, 0, 0, 0)
                }));
        Equal(2, shuffleMirror.VisibleEvents.Count);
        Equal((byte)0, shuffleMirror.VisibleEvents[0].Player);
        Null(shuffleMirror.VisibleEvents[0].PublicPasscode);
        Null(shuffleMirror.VisibleEvents[0].EntityLocator);
        Equal((uint)0, shuffleMirror.VisibleEvents[1].Count);

        byte[] legacyOneVector = Join(
            new byte[] { 36, 0x04, 1 },
            LocInfo(0, 0x04, 0, 0x08));
        GameplayMessageDecodeResult legacyShuffle = shuffleDecoder.Decode(
            new StocGameMessagePayload(legacyOneVector));
        False(legacyShuffle.IsSuccess);
        Equal(GameplayErrorCode.QueryLengthMismatch, legacyShuffle.Error);

        GameplayMessageDecodeResult twoLocationUnequip = shuffleDecoder.Decode(
            new StocGameMessagePayload(
                Join(
                    new byte[] { 95 },
                    LocInfo(0, 0x04, 0, 0x01),
                    LocInfo(0, 0x04, 1, 0x01))));
        False(twoLocationUnequip.IsSuccess);
        Equal(GameplayErrorCode.MalformedGameMessage, twoLocationUnequip.Error);

        GameplayMessageDecodeResult mixedPreviousShuffle = shuffleDecoder.Decode(
            new StocGameMessagePayload(
                ShuffleSetCardMessage(
                    0x04,
                    new[]
                    {
                        new ModernLocInfoV1(0, 0x04, 0, 0x08),
                        new ModernLocInfoV1(1, 0x04, 1, 0x08)
                    },
                    new[]
                    {
                        new ModernLocInfoV1(0, 0x04, 0, 0x08),
                        new ModernLocInfoV1(0, 0, 0, 0)
                    })));
        False(mixedPreviousShuffle.IsSuccess);
        Equal(GameplayErrorCode.InvalidParticipant, mixedPreviousShuffle.Error);

        GameplayMessageDecodeResult mismatchedCurrentShuffle = shuffleDecoder.Decode(
            new StocGameMessagePayload(
                ShuffleSetCardMessage(
                    0x04,
                    new[] { new ModernLocInfoV1(0, 0x04, 0, 0x08) },
                    new[] { new ModernLocInfoV1(1, 0x04, 0, 0x08) })));
        False(mismatchedCurrentShuffle.IsSuccess);
        Equal(GameplayErrorCode.InvalidStateTransition, mismatchedCurrentShuffle.Error);

        (PerspectiveStateMirrorV1 hiddenDrawMirror, GameplayMessageDecoderV1 hiddenDrawDecoder) =
            CreateMirror(0, deckCount1: 1);
        ApplyI6C4Success(
            hiddenDrawMirror,
            hiddenDrawDecoder,
            DrawMessage(1, (0xfeedbeefu, 0x08u)));
        Equal(1, hiddenDrawMirror.VisibleEvents.Count);
        Equal(PerspectiveSafeVisibleEventKindV1.Draw, hiddenDrawMirror.VisibleEvents[0].Kind);
        Null(hiddenDrawMirror.VisibleEvents[0].PublicPasscode);

        (PerspectiveStateMirrorV1 faceUpDrawMirror, GameplayMessageDecoderV1 faceUpDrawDecoder) =
            CreateMirror(0, deckCount1: 1);
        ApplyI6C4Success(
            faceUpDrawMirror,
            faceUpDrawDecoder,
            DrawMessage(1, (0xfeedbeefu, 0x05u)));
        Equal(2, faceUpDrawMirror.VisibleEvents.Count);
        Equal(PerspectiveSafeVisibleEventKindV1.CardRevealed, faceUpDrawMirror.VisibleEvents[1].Kind);
        Equal((uint)0xfeedbeef, faceUpDrawMirror.VisibleEvents[1].PublicPasscode);

        IList<PerspectiveSafeVisibleEventV1> readOnlyEvents =
            (IList<PerspectiveSafeVisibleEventV1>)compactMirror.VisibleEvents;
        bool mutationRejected = false;
        try
        {
            readOnlyEvents.Add(new PerspectiveSafeVisibleEventV1(
                99,
                PerspectiveSafeVisibleEventKindV1.Win));
        }
        catch (NotSupportedException)
        {
            mutationRejected = true;
        }

        True(mutationRejected, "event ledger collection must be read-only");
        Equal(1, compactMirror.VisibleEvents.Count);
    }

    private static void AssertI6C4ShuffleBoundary()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0, deckCount1: 2);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        ModernLocInfoV1 hiddenHand = new(1, 0x02, 0, 0x08);
        ApplyI6C4Success(mirror, decoder, MoveMessage(0x9000, empty, hiddenHand, 0));
        Equal(1, mirror.Snapshot.Cards.Count);
        ApplyI6C4Success(mirror, decoder, ShuffleCodesMessage(33, 1, 0x9000));
        Equal(3, mirror.VisibleEvents.Count);
        Equal(PerspectiveSafeVisibleEventKindV1.Shuffle, mirror.VisibleEvents[1].Kind);
        Equal(PerspectiveSafeVisibleEventKindV1.RandomizationBoundary, mirror.VisibleEvents[2].Kind);
        Equal((byte)1, mirror.VisibleEvents[1].Player);
        Null(mirror.VisibleEvents[1].PublicPasscode);
        Equal(0, mirror.Snapshot.Cards.Count);
        Equal(1u, mirror.Snapshot.GetZone(
            MirrorParticipantRoleV1.Opponent,
            MirrorZoneV1.Hand).Count.Value);

        (PerspectiveStateMirrorV1 selfHandMirror, GameplayMessageDecoderV1 selfHandDecoder) =
            CreateMirror(0);
        ModernLocInfoV1 selfHandFirst = new(0, 0x02, 0, 0x08);
        ModernLocInfoV1 selfHandSecond = new(0, 0x02, 1, 0x08);
        ApplyI6C4Success(
            selfHandMirror,
            selfHandDecoder,
            MoveMessage(0x9100, new ModernLocInfoV1(0, 0, 0, 0), selfHandFirst, 0));
        ApplyI6C4Success(
            selfHandMirror,
            selfHandDecoder,
            MoveMessage(0x9101, new ModernLocInfoV1(0, 0, 0, 0), selfHandSecond, 0));
        ApplyI6C4Success(
            selfHandMirror,
            selfHandDecoder,
            ShuffleCodesMessage(33, 0, 0x9101, 0x9100));
        MirrorCardSnapshotV1[] selfHandCards = selfHandMirror.Snapshot.GetZone(
            MirrorParticipantRoleV1.Self,
            MirrorZoneV1.Hand).Cards.ToArray();
        Equal(2, selfHandCards.Length);
        Equal((uint)0x9101, selfHandCards[0].CardCode.Value);
        Equal((uint)0x9100, selfHandCards[1].CardCode.Value);

        (PerspectiveStateMirrorV1 selfExtraMirror, GameplayMessageDecoderV1 selfExtraDecoder) =
            CreateMirror(0, extraCount0: 0);
        ApplyI6C4Success(
            selfExtraMirror,
            selfExtraDecoder,
            MoveMessage(
                0x9200,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(0, 0x40, 0, 0x08),
                0));
        ApplyI6C4Success(selfExtraMirror, selfExtraDecoder, ShuffleCodesMessage(39, 0, 0x9200));
        MirrorCardSnapshotV1 selfExtraCard = selfExtraMirror.Snapshot.GetZone(
            MirrorParticipantRoleV1.Self,
            MirrorZoneV1.ExtraDeck).Cards.Single();
        Equal((uint)0x9200, selfExtraCard.CardCode.Value);

        (PerspectiveStateMirrorV1 publicHandMirror, GameplayMessageDecoderV1 publicHandDecoder) =
            CreateMirror(0);
        ApplyI6C4Success(
            publicHandMirror,
            publicHandDecoder,
            MoveMessage(
                0x9300,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(1, 0x02, 0, 0x05),
                0));
        ApplyI6C4Success(
            publicHandMirror,
            publicHandDecoder,
            MoveMessage(
                0x9301,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(1, 0x02, 1, 0x08),
                0));
        ApplyI6C4Success(
            publicHandMirror,
            publicHandDecoder,
            ShuffleCodesMessage(33, 1, 0x9300, 0x9301));
        MirrorCardSnapshotV1[] publicHandCards = publicHandMirror.Snapshot.GetZone(
            MirrorParticipantRoleV1.Opponent,
            MirrorZoneV1.Hand).Cards.ToArray();
        Equal(1, publicHandCards.Length);
        Equal((uint)0x9300, publicHandCards[0].CardCode.Value);

        (PerspectiveStateMirrorV1 extraMirror, GameplayMessageDecoderV1 extraDecoder) =
            CreateMirror(0, extraCount1: 1);
        ApplyI6C4Success(extraMirror, extraDecoder, ShuffleCodesMessage(39, 1, 0x9010));
        Equal(2, extraMirror.VisibleEvents.Count);
        Equal(PerspectiveSafeVisibleEventKindV1.Shuffle, extraMirror.VisibleEvents[0].Kind);
        Equal(PerspectiveSafeVisibleEventKindV1.RandomizationBoundary, extraMirror.VisibleEvents[1].Kind);
        Equal((byte)1, extraMirror.VisibleEvents[0].Player);
        Null(extraMirror.VisibleEvents[0].PublicPasscode);

        (PerspectiveStateMirrorV1 setMirror, GameplayMessageDecoderV1 setDecoder) =
            CreateMirror(0);
        ApplyI6C4Success(
            setMirror,
            setDecoder,
            ShuffleSetCardMessage(
                0x08,
                new[] { new ModernLocInfoV1(1, 0x08, 0, 0x08) },
                new[] { new ModernLocInfoV1(0, 0, 0, 0) }));
        Equal((byte)1, setMirror.VisibleEvents[0].Player);
        Null(setMirror.VisibleEvents[0].ToZone);
        Equal((byte)0x08, setMirror.EventSourceFacts[0].SourceLocations[0].Location);
        Equal(2, setMirror.VisibleEvents.Count);

        (PerspectiveStateMirrorV1 compatibilityMirror, GameplayMessageDecoderV1 compatibilityDecoder) =
            CreateMirror(0);
        ModernLocInfoV1 source = new(0, 0x04, 0, 0x01);
        ModernLocInfoV1 target = new(0, 0x04, 1, 0x01);
        ApplyI6C4Success(compatibilityMirror, compatibilityDecoder, MoveMessage(0xa000, new ModernLocInfoV1(0, 0, 0, 0), source, 0));
        ApplyI6C4Success(compatibilityMirror, compatibilityDecoder, MoveMessage(0xa001, new ModernLocInfoV1(0, 0, 0, 0), target, 0));
        ApplyI6C4Success(compatibilityMirror, compatibilityDecoder, EquipMessage(source, target));
        int eventCountBeforeUnequip = compatibilityMirror.VisibleEvents.Count;
        string snapshotBeforeUnequip = compatibilityMirror.Snapshot.ToDeterministicString();
        string eventsBeforeUnequip = I6C4EventSignature(compatibilityMirror);
        ulong nextIndexBeforeUnequip = compatibilityMirror.NextEventIndex;
        Equal(
            PerspectiveSafeEventSourceCertificationV1.Proven,
            compatibilityMirror.EventSourceCertification);
        GameplayMessageDecodeResult decodedUnequip = compatibilityDecoder.Decode(
            new StocGameMessagePayload(UnequipMessage(source)));
        True(decodedUnequip.IsSuccess);
        NotNull(decodedUnequip.Message);
        MirrorApplyResult acceptedUnequip = compatibilityMirror.Apply(decodedUnequip.Message!);
        True(acceptedUnequip.IsSuccess, acceptedUnequip.Error.ToString());
        Equal(
            PerspectiveSafeEventSourceCertificationV1.RejectedUnexpectedUnreachableMessage,
            compatibilityMirror.EventSourceCertification);
        NotEqual(snapshotBeforeUnequip, compatibilityMirror.Snapshot.ToDeterministicString());
        Equal(eventsBeforeUnequip, I6C4EventSignature(compatibilityMirror));
        Equal(nextIndexBeforeUnequip, compatibilityMirror.NextEventIndex);
        Equal(eventCountBeforeUnequip, compatibilityMirror.VisibleEvents.Count);
        False(compatibilityMirror.VisibleEvents.Any(
            value => value.Kind == PerspectiveSafeVisibleEventKindV1.Unequipped));
        Equal(0, compatibilityMirror.Snapshot.EquipmentRelations.Count);

        ApplyI6C4Success(compatibilityMirror, compatibilityDecoder, new byte[] { 40, 1 });
        Equal(
            PerspectiveSafeEventSourceCertificationV1.RejectedUnexpectedUnreachableMessage,
            compatibilityMirror.EventSourceCertification);

        (PerspectiveStateMirrorV1 failedDecodeMirror, GameplayMessageDecoderV1 failedDecodeDecoder) =
            CreateMirror(0);
        GameplayMessageDecodeResult malformedUnequip = failedDecodeDecoder.Decode(
            new StocGameMessagePayload(new byte[] { 95 }));
        False(malformedUnequip.IsSuccess);
        Equal(
            PerspectiveSafeEventSourceCertificationV1.Proven,
            failedDecodeMirror.EventSourceCertification);

        (PerspectiveStateMirrorV1 failedApplyMirror, GameplayMessageDecoderV1 failedApplyDecoder) =
            CreateMirror(0);
        GameplayMessageDecodeResult validUnequip = failedApplyDecoder.Decode(
            new StocGameMessagePayload(UnequipMessage(source)));
        True(validUnequip.IsSuccess);
        MirrorApplyResult failedUnequip = failedApplyMirror.Apply(validUnequip.Message!);
        False(failedUnequip.IsSuccess);
        Equal(GameplayErrorCode.UnknownMirrorReference, failedUnequip.Error);
        Equal(
            PerspectiveSafeEventSourceCertificationV1.Proven,
            failedApplyMirror.EventSourceCertification);

        (PerspectiveStateMirrorV1 zeroEventMirror, GameplayMessageDecoderV1 zeroEventDecoder) =
            CreateMirror(0);
        ApplyI6C4Success(zeroEventMirror, zeroEventDecoder, new byte[] { 100, 0, 1, 0, 0, 0 });
        Equal(
            PerspectiveSafeEventSourceCertificationV1.Proven,
            zeroEventMirror.EventSourceCertification);
    }

    private static void AssertI6C4AtomicityAndOverflow()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        string beforeSnapshot = mirror.Snapshot.ToDeterministicString();
        string beforeEvents = I6C4EventSignature(mirror);
        ulong beforeIndex = mirror.NextEventIndex;

        GameplayMessageDecodeResult failedDecode = decoder.Decode(
            new StocGameMessagePayload(new byte[] { 31, 0, 1, 0, 0, 0, 0 }));
        False(failedDecode.IsSuccess);
        Equal(beforeSnapshot, mirror.Snapshot.ToDeterministicString());
        Equal(beforeEvents, I6C4EventSignature(mirror));
        Equal(beforeIndex, mirror.NextEventIndex);

        MirrorApplyResult failedApply = mirror.Apply(DecodeMessage(
            decoder,
            MoveMessage(
                0xb000,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(0, 0x02, 9, 0x08),
                0)));
        False(failedApply.IsSuccess);
        Equal(beforeSnapshot, mirror.Snapshot.ToDeterministicString());
        Equal(beforeEvents, I6C4EventSignature(mirror));
        Equal(beforeIndex, mirror.NextEventIndex);

        MirrorApplyResult failedProjection = mirror.Apply(DecodeMessage(
            decoder,
            ConfirmMessage(
                31,
                0,
                (0xb001u, new ModernLocInfoV1(0, 0xff, 0, 0)))));
        False(failedProjection.IsSuccess);
        Equal(GameplayErrorCode.InvalidLocation, failedProjection.Error);
        Equal(beforeSnapshot, mirror.Snapshot.ToDeterministicString());
        Equal(beforeEvents, I6C4EventSignature(mirror));
        Equal(beforeIndex, mirror.NextEventIndex);

        byte[] partiallyInvalidConfirm = Join(
            new byte[] { 31, 0 },
            U32(2),
            U32(0xb002),
            new byte[] { 0, 0x04 },
            U32(0),
            U32(0xb003),
            new byte[] { 0, 0xff },
            U32(0));
        MirrorApplyResult failedMultiEventProjection = mirror.Apply(DecodeMessage(
            decoder,
            partiallyInvalidConfirm));
        False(failedMultiEventProjection.IsSuccess);
        Equal(GameplayErrorCode.InvalidLocation, failedMultiEventProjection.Error);
        Equal(beforeSnapshot, mirror.Snapshot.ToDeterministicString());
        Equal(beforeEvents, I6C4EventSignature(mirror));
        Equal(beforeIndex, mirror.NextEventIndex);

        mirror.SetNextEventIndexForTesting(ulong.MaxValue);
        MirrorApplyResult overflow = mirror.Apply(DecodeMessage(decoder, new byte[] { 40, 0 }));
        False(overflow.IsSuccess);
        Equal(GameplayErrorCode.ArithmeticFailure, overflow.Error);
        Equal((ulong)0, mirror.Snapshot.TurnCount);
        Equal(0, mirror.VisibleEvents.Count);
        Equal(ulong.MaxValue, mirror.NextEventIndex);
    }

    private static void AssertI6C4HistoricalLocators()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        ModernLocInfoV1 slot = new(0, 0x04, 0, 0x01);
        ApplyI6C4Success(mirror, decoder, MoveMessage(0xc000, empty, slot, 0));
        PerspectiveSafeVisibleEventV1 historical = mirror.VisibleEvents[0];
        string historicalLocator = historical.EntityLocator!;
        uint historicalCode = historical.PublicPasscode!.Value;

        ApplyI6C4Success(
            mirror,
            decoder,
            MoveMessage(0xc000, slot, new ModernLocInfoV1(0, 0x10, 0, 0x01), 0));
        ApplyI6C4Success(
            mirror,
            decoder,
            MoveMessage(0xc001, empty, slot, 0));

        Equal(historicalLocator, historical.EntityLocator);
        Equal(historicalCode, historical.PublicPasscode);
        Equal("p0:MONSTER_ZONE:0", historical.EntityLocator);
        Equal((uint)0xc000, historical.PublicPasscode);
    }

    private static void AssertI6C4PairedPrivacy()
    {
        PerspectiveStateMirrorV1 first = CreateHiddenWorld(0x11112222);
        PerspectiveStateMirrorV1 second = CreateHiddenWorld(0xaaaabbbb);
        Equal(I6C4EventSignature(first), I6C4EventSignature(second));

        (PerspectiveStateMirrorV1 hiddenConfirmFirst, GameplayMessageDecoderV1 hiddenConfirmFirstDecoder) =
            CreateMirror(0);
        (PerspectiveStateMirrorV1 hiddenConfirmSecond, GameplayMessageDecoderV1 hiddenConfirmSecondDecoder) =
            CreateMirror(0);
        ApplyI6C4Success(
            hiddenConfirmFirst,
            hiddenConfirmFirstDecoder,
            ConfirmMessage(31, 1, (0x11112222u, new ModernLocInfoV1(1, 0x02, 0, 0x08))));
        ApplyI6C4Success(
            hiddenConfirmSecond,
            hiddenConfirmSecondDecoder,
            ConfirmMessage(31, 1, (0xaaaabbbbu, new ModernLocInfoV1(1, 0x02, 0, 0x08))));
        Equal(I6C4EventSignature(hiddenConfirmFirst), I6C4EventSignature(hiddenConfirmSecond));

        (GameplayMessageDecoderV1 firstDecoder, GameplayMessageDecoderV1 secondDecoder) =
            (new GameplayMessageDecoderV1(first.Snapshot.Perspective),
             new GameplayMessageDecoderV1(second.Snapshot.Perspective));
        ApplyI6C4Success(first, firstDecoder, ShuffleCodesMessage(33, 1, 0x11112222));
        ApplyI6C4Success(second, secondDecoder, ShuffleCodesMessage(33, 1, 0xaaaabbbb));
        Equal(I6C4EventSignature(first), I6C4EventSignature(second));
        ApplyI6C4Success(first, firstDecoder, ShuffleCodesMessage(39, 1, 0x11112222, 0x33334444));
        ApplyI6C4Success(second, secondDecoder, ShuffleCodesMessage(39, 1, 0xaaaabbbb, 0xccccdddd));
        Equal(I6C4EventSignature(first), I6C4EventSignature(second));
        AssertDoesNotContainForbidden(
            I6C4EventSignature(first),
            new[] { "286335522", "2863311530", "socket", "MirrorEntity", "raw" });
    }

    private static void AssertI6C4TransportChunking()
    {
        byte[] start = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            CreateStartBytes(0, deckCount0: 2));
        byte[] draw = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            DrawMessage(0, (0xd000u, 0x05u)));
        byte[] turn = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            new byte[] { 40, 1 });
        byte[] shuffle = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            new byte[] { 32, 0 });
        byte[] transcript = Join(start, draw, turn, shuffle);

        string whole = RunI6C4TransportTranscript(new[] { transcript });
        string oneByte = RunI6C4TransportTranscript(
            transcript.Select(value => new[] { value }).ToArray());
        string irregular = RunI6C4TransportTranscript(
            Split(transcript, new[] { 1, 2, 7, 3, 11 }));
        Equal(whole, oneByte);
        Equal(whole, irregular);
    }

    private static void AssertI6C5OuterPublicFrameSource()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0, extraCount0: 0);
        PerspectiveSafeMatchContextV1 context = CreateValidI6C5MatchContext();
        ApplyI6C4Success(mirror, decoder, new byte[] { 40, 1 });

        PerspectiveSafeFrameSourceResultV1 missingConfiguration =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C5(mirror, null);
        False(missingConfiguration.IsSuccess);
        Equal(
            PerspectiveSafeFrameSourceErrorCodeV1.MissingMatchContext,
            missingConfiguration.Error!.Value.Code);
        Null(missingConfiguration.Frame);

        PerspectiveSafeMatchContextV1 contradictoryFlags = new(
            0,
            0x1000,
            new PerspectiveSafeKnowledgeV1(true, false),
            new PerspectiveSafeDeckV1(true, new uint[] { 1, 2 }, new uint[] { 3 }),
            new PerspectiveSafeDeckV1(false));
        PerspectiveSafeFrameSourceResultV1 contradictoryFlagsResult =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C5(
                mirror,
                contradictoryFlags);
        False(contradictoryFlagsResult.IsSuccess);
        Null(contradictoryFlagsResult.Frame);
        Equal(
            PerspectiveSafeFrameSourceErrorCodeV1.InvalidMirrorSnapshot,
            contradictoryFlagsResult.Error!.Value.Code);

        PerspectiveSafeFrameSourceResultV1 result =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C5(
                mirror,
                context,
                CreatePrintedProviderForMirror(mirror));
        True(result.IsSuccess, result.Error?.ToString() ?? "I6C5 frame rejected");
        NotNull(result.Frame);
        Equal((ulong)0x234, result.Frame!.Globals.DuelFlags);
        Equal((byte)0, result.Frame.MatchContext.PerspectivePlayer);
        Equal(true, result.Frame.MatchContext.Knowledge.OwnDecklistKnown);
        Equal(false, result.Frame.MatchContext.Knowledge.OpponentDecklistKnown);
        True(result.Frame.MatchContext.OwnDeck.MainDeck.SequenceEqual(new uint[] { 1, 2 }));
        True(result.Frame.MatchContext.OwnDeck.ExtraDeck.SequenceEqual(new uint[] { 3 }));
        Equal(1, result.Frame.VisibleEvents.Count);
        Equal(PerspectiveSafeVisibleEventKindV1.TurnStarted, result.Frame.VisibleEvents[0].Kind);

        (PerspectiveStateMirrorV1 populatedMirror, GameplayMessageDecoderV1 populatedDecoder) =
            CreateMirror(0, extraCount0: 0);
        PerspectiveSafeMatchContextV1 populatedContext = CreateValidI6C5MatchContext();
        ModernLocInfoV1 populatedSource = new(0, 0x04, 0, 0x04);
        ModernLocInfoV1 populatedTarget = new(0, 0x04, 1, 0x04);
        ApplyI6C4Success(
            populatedMirror,
            populatedDecoder,
            MoveMessage(0xc100, new ModernLocInfoV1(0, 0, 0, 0), populatedSource, 0));
        ApplyI6C4Success(
            populatedMirror,
            populatedDecoder,
            MoveMessage(0xc101, new ModernLocInfoV1(0, 0, 0, 0), populatedTarget, 0));
        ApplyI6C4Success(
            populatedMirror,
            populatedDecoder,
            EquipMessage(populatedSource, populatedTarget));
        ApplyI6C4Success(
            populatedMirror,
            populatedDecoder,
            ChainingMessage(populatedSource, 1, 0xc100));
        PerspectiveSafeI6C3SourceResultV1 populatedState =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C3(populatedMirror);
        True(populatedState.IsSuccess, populatedState.Error?.ToString() ?? "I6C3 source rejected");
        PerspectiveSafeFrameSourceResultV1 populatedResult =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C5(
                populatedMirror,
                populatedContext,
                CreatePrintedProviderForMirror(populatedMirror));
        True(
            populatedResult.IsSuccess,
            populatedResult.Error?.ToString() ?? "provider-backed frame rejected");
        NotNull(populatedResult.Frame);
        True(populatedResult.Frame!.Entities.All(entity =>
            !entity.IdentityKnown || entity.Printed is not null));

        uint[] mutableOwnDeck = new[] { 1u, 2u };
        PerspectiveSafeMatchContextV1 immutableContext = new(
            0,
            0x234,
            new PerspectiveSafeKnowledgeV1(true, false),
            new PerspectiveSafeDeckV1(true, mutableOwnDeck, new[] { 3u }),
            new PerspectiveSafeDeckV1(false));
        mutableOwnDeck[0] = 999;
        PerspectiveSafeFrameSourceResultV1 immutableResult =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C5(
                mirror,
                immutableContext,
                CreatePrintedProviderForMirror(mirror));
        True(immutableResult.IsSuccess, immutableResult.Error?.ToString() ?? "immutable frame rejected");
        Equal((uint)1, immutableResult.Frame!.MatchContext.OwnDeck.MainDeck[0]);

        PerspectiveSafeMatchContextV1 unsorted = new(
            0,
            0x234,
            new PerspectiveSafeKnowledgeV1(true, false),
            new PerspectiveSafeDeckV1(true, new[] { 2u, 1u }, new[] { 3u }),
            new PerspectiveSafeDeckV1(false));
        PerspectiveSafeFrameSourceResultV1 unsortedResult =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C5(mirror, unsorted);
        False(unsortedResult.IsSuccess);
        Equal(
            PerspectiveSafeFrameSourceErrorCodeV1.InvalidOrdering,
            unsortedResult.Error!.Value.Code);

        PerspectiveSafeMatchContextV1 unknownWithPasscodes = new(
            0,
            0x234,
            new PerspectiveSafeKnowledgeV1(true, false),
            new PerspectiveSafeDeckV1(true, new[] { 1u, 2u }, new[] { 3u }),
            new PerspectiveSafeDeckV1(false, new[] { 4u }));
        PerspectiveSafeFrameSourceResultV1 unknownDeckResult =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C5(mirror, unknownWithPasscodes);
        False(unknownDeckResult.IsSuccess);
        Equal(
            PerspectiveSafeFrameSourceErrorCodeV1.InvalidDeckState,
            unknownDeckResult.Error!.Value.Code);

        (PerspectiveStateMirrorV1 layoutMirror, GameplayMessageDecoderV1 layoutDecoder) =
            CreateMirror(0, extraCount0: 0);
        ApplyI6C4Success(
            layoutMirror,
            layoutDecoder,
            MoveMessage(
                0xb200,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(1, 0x08, 0, 0x08),
                0));
        PerspectiveSafeFrameSourceResultV1 layoutResult =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C5(
                layoutMirror,
                context,
                CreatePrintedProviderForMirror(layoutMirror));
        True(layoutResult.IsSuccess, "layout: " + (layoutResult.Error?.ToString() ?? "frame rejected"));
        Equal(
            1u,
            layoutResult.Frame!.Zones.Single(zone =>
                zone.Player == 1 &&
                zone.Kind == PerspectiveSafeSemanticZoneV1.SpellTrapZone).TotalCount);
        Equal(
            PerspectiveSafeSemanticZoneV1.SpellTrapZone,
            layoutResult.Frame.Entities.Single().Zone);

        (PerspectiveStateMirrorV1 pendulumMirror, GameplayMessageDecoderV1 pendulumDecoder) =
            CreateMirror(0, extraCount0: 0);
        ApplyI6C4Success(
            pendulumMirror,
            pendulumDecoder,
            MoveMessage(
                0xb201,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(1, 0x08, 0, 0x08),
                0));
        PerspectiveSafeMatchContextV1 pendulumContext = new(
            0,
            0x800,
            new PerspectiveSafeKnowledgeV1(true, false),
            new PerspectiveSafeDeckV1(true, new[] { 1u, 2u }, new[] { 3u }),
            new PerspectiveSafeDeckV1(false));
        PerspectiveSafeFrameSourceResultV1 pendulumResult =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C5(
                pendulumMirror,
                pendulumContext,
                CreatePrintedProviderForMirror(pendulumMirror));
        True(pendulumResult.IsSuccess, "pendulum: " + (pendulumResult.Error?.ToString() ?? "frame rejected"));
        Equal(
            PerspectiveSafeSemanticZoneV1.PendulumRelevant,
            pendulumResult.Frame!.Entities.Single().Zone);

        (PerspectiveStateMirrorV1 combinedPzoneMirror,
            GameplayMessageDecoderV1 combinedPzoneDecoder) =
            CreateMirror(0, extraCount0: 0);
        ApplyI6C4Success(
            combinedPzoneMirror,
            combinedPzoneDecoder,
            MoveMessage(
                0xb206,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(1, 0x08, 1, 0x08),
                0));
        PerspectiveSafeMatchContextV1 combinedPzoneContext = new(
            0,
            0x800 | 0x1000 | 0x400000,
            new PerspectiveSafeKnowledgeV1(true, false),
            new PerspectiveSafeDeckV1(true, new[] { 1u, 2u }, new[] { 3u }),
            new PerspectiveSafeDeckV1(false));
        PerspectiveSafeFrameSourceResultV1 combinedPzoneResult =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C5(
                combinedPzoneMirror,
                combinedPzoneContext,
                CreatePrintedProviderForMirror(combinedPzoneMirror));
        True(
            combinedPzoneResult.IsSuccess,
            "combined PZONE: " +
            (combinedPzoneResult.Error?.ToString() ?? "frame rejected"));
        Equal(
            PerspectiveSafeSemanticZoneV1.PendulumRelevant,
            combinedPzoneResult.Frame!.Entities.Single().Zone);

        (PerspectiveStateMirrorV1 pendingRelationMirror,
            GameplayMessageDecoderV1 pendingRelationDecoder) =
            CreateMirror(0, extraCount0: 0);
        ModernLocInfoV1 pendingSource = new(1, 0x08, 0, 0x08);
        ModernLocInfoV1 pendingTarget = new(1, 0x04, 0, 0x08);
        ApplyI6C4Success(
            pendingRelationMirror,
            pendingRelationDecoder,
            MoveMessage(
                0xb203,
                new ModernLocInfoV1(0, 0, 0, 0),
                pendingSource,
                0));
        ApplyI6C4Success(
            pendingRelationMirror,
            pendingRelationDecoder,
            MoveMessage(
                0xb204,
                new ModernLocInfoV1(0, 0, 0, 0),
                pendingTarget,
                0));
        ApplyI6C4Success(
            pendingRelationMirror,
            pendingRelationDecoder,
            EquipMessage(pendingSource, pendingTarget));
        PerspectiveSafeFrameSourceResultV1 pendingRelationResult =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C5(
                pendingRelationMirror,
                context,
                CreatePrintedProviderForMirror(pendingRelationMirror));
        True(
            pendingRelationResult.IsSuccess,
            "relation: " + (pendingRelationResult.Error?.ToString() ??
            "frame rejected"));
        Equal(1, pendingRelationResult.Frame!.Relationships.Count);
        Equal(
            PerspectiveSafeRelationshipKindV1.Equip,
            pendingRelationResult.Frame.Relationships[0].Kind);

        (PerspectiveStateMirrorV1 pendingChainMirror,
            GameplayMessageDecoderV1 pendingChainDecoder) =
            CreateMirror(0, extraCount0: 0);
        ModernLocInfoV1 pendingChainSource = new(1, 0x08, 0, 0x08);
        ApplyI6C4Success(
            pendingChainMirror,
            pendingChainDecoder,
            MoveMessage(
                0xb205,
                new ModernLocInfoV1(0, 0, 0, 0),
                pendingChainSource,
                0));
        ApplyI6C4Success(
            pendingChainMirror,
            pendingChainDecoder,
            ChainingMessage(pendingChainSource, 1, 0));
        PerspectiveSafeFrameSourceResultV1 pendingChainResult =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C5(
                pendingChainMirror,
                context,
                CreatePrintedProviderForMirror(pendingChainMirror));
        True(
            pendingChainResult.IsSuccess,
            "chain: " + (pendingChainResult.Error?.ToString() ??
            "frame rejected"));
        Equal(1u, pendingChainResult.Frame!.Chain.Length);
        Null(pendingChainResult.Frame.Chain.Links[0].Source);
        Equal(
            PerspectiveSafeSemanticZoneV1.SpellTrapZone,
            pendingChainResult.Frame.Chain.Links[0].ActivationZone);

        (PerspectiveStateMirrorV1 rejectedMirror, GameplayMessageDecoderV1 rejectedDecoder) =
            CreateMirror(0, extraCount0: 0);
        ModernLocInfoV1 source = new(0, 0x04, 0, 0x01);
        ModernLocInfoV1 target = new(0, 0x04, 1, 0x01);
        ApplyI6C4Success(
            rejectedMirror,
            rejectedDecoder,
            MoveMessage(0xb100, new ModernLocInfoV1(0, 0, 0, 0), source, 0));
        ApplyI6C4Success(
            rejectedMirror,
            rejectedDecoder,
            MoveMessage(0xb101, new ModernLocInfoV1(0, 0, 0, 0), target, 0));
        ApplyI6C4Success(rejectedMirror, rejectedDecoder, EquipMessage(source, target));
        ApplyI6C4Success(rejectedMirror, rejectedDecoder, UnequipMessage(source));
        PerspectiveSafeFrameSourceResultV1 rejectedFrame =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C5(
                rejectedMirror,
                context,
                CreatePrintedProviderForMirror(rejectedMirror));
        False(rejectedFrame.IsSuccess);
        Null(rejectedFrame.Frame);
        Equal(
            PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
            rejectedFrame.Error!.Value.Code);
        ApplyI6C4Success(rejectedMirror, rejectedDecoder, new byte[] { 40, 1 });
        PerspectiveSafeFrameSourceResultV1 stickyRejectedFrame =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C5(
                rejectedMirror,
                context,
                CreatePrintedProviderForMirror(rejectedMirror));
        False(stickyRejectedFrame.IsSuccess);
        Null(stickyRejectedFrame.Frame);

        PerspectiveStateMirrorV1 hiddenWorldA = CreateHiddenWorld(
            0x44110000,
            extraCount0: 0);
        PerspectiveStateMirrorV1 hiddenWorldB = CreateHiddenWorld(
            0x99220000,
            extraCount0: 0);
        PerspectiveSafePrintedProviderV1 hiddenProvider =
            CreatePrintedProviderForCodes(new[] { 1u });
        PerspectiveSafeFrameSourceResultV1 hiddenFrameA =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C5(
                hiddenWorldA,
                context,
                hiddenProvider);
        PerspectiveSafeFrameSourceResultV1 hiddenFrameB =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C5(
                hiddenWorldB,
                context,
                hiddenProvider);
        True(hiddenFrameA.IsSuccess, hiddenFrameA.Error?.ToString() ?? "hidden frame A rejected");
        True(hiddenFrameB.IsSuccess, hiddenFrameB.Error?.ToString() ?? "hidden frame B rejected");
        Equal(FrameSignature(hiddenFrameA.Frame!), FrameSignature(hiddenFrameB.Frame!));
    }

    private static void AssertI6C2MissingMirror()
    {
        PerspectiveSafeI6C2SourceResultV1 result =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C2(null);
        False(result.IsSuccess);
        Null(result.Source);
        NotNull(result.Error);
        Equal(
            PerspectiveSafeFrameSourceErrorCodeV1.MissingMirror,
            result.Error!.Value.Code);
    }

    private static void AssertI6C5ExtraUpdateDataBootstrap()
    {
        (PerspectiveStateMirrorV1 coverageMirror,
            GameplayMessageDecoderV1 coverageDecoder) =
            CreateMirror(0, extraCount0: 2, extraCount1: 0);
        PerspectiveSafeMatchContextV1 coverageContext =
            CreateValidI6C5MatchContext();
        PerspectiveSafeFrameSourceResultV1 beforeBootstrapFrame =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C5(
                coverageMirror,
                coverageContext,
                CreatePrintedProviderForMirror(coverageMirror));
        False(beforeBootstrapFrame.IsSuccess, "pre-bootstrap frame unexpectedly succeeded");
        Null(beforeBootstrapFrame.Frame);
        Equal(
            PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
            beforeBootstrapFrame.Error!.Value.Code);
        Equal(
            PerspectiveSafeI6C2SourceStatusV1.Blocked,
            GetI6C2Source(coverageMirror).GetStatus(
                PerspectiveSafeI6C2ConstituentV1.EntityIdentity));
        ApplyI6C4Success(
            coverageMirror,
            coverageDecoder,
            UpdateDataMessage(
                0,
                0x40,
                Join(
                    ExtraQuery(0x9A00, 0, 0x08),
                    ExtraQuery(0x9B00, 0, 0x08))));
        Equal(
            PerspectiveSafeI6C2SourceStatusV1.Proven,
            GetI6C2Source(coverageMirror).GetStatus(
                PerspectiveSafeI6C2ConstituentV1.EntityIdentity));
        PerspectiveSafeFrameSourceResultV1 afterBootstrapFrame =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C5(
                coverageMirror,
                coverageContext,
                CreatePrintedProviderForMirror(coverageMirror));
        True(
            afterBootstrapFrame.IsSuccess,
            afterBootstrapFrame.Error?.ToString() ?? "post-bootstrap provider frame rejected");
        NotNull(afterBootstrapFrame.Frame);
        True(afterBootstrapFrame.Frame!.Entities.All(entity =>
            !entity.IdentityKnown || entity.Printed is not null));

        (PerspectiveStateMirrorV1 missingSelfPositionMirror,
            GameplayMessageDecoderV1 missingSelfPositionDecoder) =
            CreateMirror(0, extraCount0: 1, extraCount1: 0);
        string missingSelfPositionBefore =
            missingSelfPositionMirror.Snapshot.ToDeterministicString();
        MirrorApplyResult missingSelfPosition =
            missingSelfPositionMirror.Apply(
                DecodeMessage(
                    missingSelfPositionDecoder,
                    UpdateDataMessage(
                        0,
                        0x40,
                        Join(ExtraQueryWithoutPosition(0x9C00, 0)))));
        False(missingSelfPosition.IsSuccess);
        Equal(GameplayErrorCode.UnknownMirrorReference, missingSelfPosition.Error);
        Equal(
            missingSelfPositionBefore,
            missingSelfPositionMirror.Snapshot.ToDeterministicString());

        (PerspectiveStateMirrorV1 missingOpponentPositionMirror,
            GameplayMessageDecoderV1 missingOpponentPositionDecoder) =
            CreateMirror(0, extraCount0: 0, extraCount1: 1);
        string missingOpponentPositionBefore =
            missingOpponentPositionMirror.Snapshot.ToDeterministicString();
        MirrorApplyResult missingOpponentPosition =
            missingOpponentPositionMirror.Apply(
                DecodeMessage(
                    missingOpponentPositionDecoder,
                    UpdateDataMessage(
                        1,
                        0x40,
                        Join(ExtraPublicQueryWithoutPosition(1)))));
        False(missingOpponentPosition.IsSuccess);
        Equal(GameplayErrorCode.UnknownMirrorReference, missingOpponentPosition.Error);
        Equal(
            missingOpponentPositionBefore,
            missingOpponentPositionMirror.Snapshot.ToDeterministicString());

        (PerspectiveStateMirrorV1 zeroCodeMirror,
            GameplayMessageDecoderV1 zeroCodeDecoder) =
            CreateMirror(0, extraCount0: 1, extraCount1: 0);
        string zeroCodeBefore = zeroCodeMirror.Snapshot.ToDeterministicString();
        MirrorApplyResult zeroCodeResult = zeroCodeMirror.Apply(
            DecodeMessage(
                zeroCodeDecoder,
                UpdateDataMessage(
                    0,
                    0x40,
                    Join(ExtraQuery(0, 0, 0x08)))));
        False(zeroCodeResult.IsSuccess);
        Equal(GameplayErrorCode.UnknownMirrorReference, zeroCodeResult.Error);
        Equal(zeroCodeBefore, zeroCodeMirror.Snapshot.ToDeterministicString());

        (PerspectiveStateMirrorV1 selfMirror, GameplayMessageDecoderV1 selfDecoder) =
            CreateMirror(0, extraCount0: 3, extraCount1: 0);
        string selfBefore = selfMirror.Snapshot.ToDeterministicString();
        byte[] selfUpdate = UpdateDataMessage(
            0,
            0x40,
            Join(
                ExtraQuery(0xA100, 0, 0x08),
                ExtraQuery(0xA100, 0, 0x08),
                ExtraQuery(0xA200, 0, 0x08)));
        MirrorApplyResult selfBootstrap = selfMirror.Apply(
            DecodeMessage(selfDecoder, selfUpdate));
        True(
            selfBootstrap.IsSuccess,
            $"self Extra bootstrap failed: {selfBootstrap.Error}");
        MirrorCardSnapshotV1[] selfCards = selfMirror.Snapshot.GetZone(
            MirrorParticipantRoleV1.Self,
            MirrorZoneV1.ExtraDeck).Cards.ToArray();
        Equal(3, selfCards.Length);
        Equal((uint)0xA100, selfCards[0].CardCode.Value);
        Equal((uint)0xA100, selfCards[1].CardCode.Value);
        Equal((uint)0xA200, selfCards[2].CardCode.Value);
        True(selfCards.All(card =>
            card.CardCode.Provenance == MirrorProvenanceV1.PerspectivePrivateFact));
        NotEqual(selfBefore, selfMirror.Snapshot.ToDeterministicString());

        MirrorApplyResult shuffled = selfMirror.Apply(
            DecodeMessage(
                selfDecoder,
                ShuffleCodesMessage(39, 0, 0xA200, 0xA100, 0xA100)));
        True(shuffled.IsSuccess, $"Extra shuffle failed: {shuffled.Error}");
        MirrorCardSnapshotV1[] shuffledCards = selfMirror.Snapshot.GetZone(
            MirrorParticipantRoleV1.Self,
            MirrorZoneV1.ExtraDeck).Cards.ToArray();
        Equal(3, shuffledCards.Length);
        Equal((uint)0xA200, shuffledCards[0].CardCode.Value);
        Equal((uint)0xA100, shuffledCards[1].CardCode.Value);
        Equal((uint)0xA100, shuffledCards[2].CardCode.Value);

        (PerspectiveStateMirrorV1 moveMirror, GameplayMessageDecoderV1 moveDecoder) =
            CreateMirror(0, extraCount0: 2, extraCount1: 0);
        ApplyI6C4Success(
            moveMirror,
            moveDecoder,
            UpdateDataMessage(
                0,
                0x40,
                Join(
                    ExtraQuery(0xB100, 0, 0x08),
                    ExtraQuery(0xB200, 0, 0x08))));
        ApplyI6C4Success(
            moveMirror,
            moveDecoder,
            MoveMessage(
                0xB100,
                new ModernLocInfoV1(0, 0x40, 0, 0x08),
                new ModernLocInfoV1(0, 0x04, 0, 0x04),
                0));
        Equal(
            (uint)1,
            moveMirror.Snapshot.GetZone(
                MirrorParticipantRoleV1.Self,
                MirrorZoneV1.ExtraDeck).Count.Value);
        Equal(
            (uint)1,
            moveMirror.Snapshot.GetZone(
                MirrorParticipantRoleV1.Self,
                MirrorZoneV1.MonsterZone).Count.Value);
        ApplyI6C4Success(
            moveMirror,
            moveDecoder,
            MoveMessage(
                0xB100,
                new ModernLocInfoV1(0, 0x04, 0, 0x04),
                new ModernLocInfoV1(0, 0x40, 1, 0x08),
                0));
        Equal(
            (uint)2,
            moveMirror.Snapshot.GetZone(
                MirrorParticipantRoleV1.Self,
                MirrorZoneV1.ExtraDeck).Count.Value);
        Equal(
            (uint)0,
            moveMirror.Snapshot.GetZone(
                MirrorParticipantRoleV1.Self,
                MirrorZoneV1.MonsterZone).Count.Value);

        (PerspectiveStateMirrorV1 opponentMirror,
            GameplayMessageDecoderV1 opponentDecoder) =
            CreateMirror(0, extraCount0: 0, extraCount1: 2);
        MirrorApplyResult opponentBootstrap = opponentMirror.Apply(
            DecodeMessage(
                opponentDecoder,
                UpdateDataMessage(
                    1,
                    0x40,
                    Join(
                        ExtraPublicQuery(1, 0x08),
                        ExtraPublicQuery(1, 0x08)))));
        True(
            opponentBootstrap.IsSuccess,
            $"opponent Extra bootstrap failed: {opponentBootstrap.Error}");
        MirrorCardSnapshotV1[] opponentCards = opponentMirror.Snapshot.GetZone(
            MirrorParticipantRoleV1.Opponent,
            MirrorZoneV1.ExtraDeck).Cards.ToArray();
        Equal(2, opponentCards.Length);
        True(opponentCards.All(card => !card.CardCode.IsKnown));

        (PerspectiveStateMirrorV1 opponentWorldB,
            GameplayMessageDecoderV1 opponentWorldBDecoder) =
            CreateMirror(0, extraCount0: 0, extraCount1: 2);
        MirrorApplyResult opponentWorldBResult = opponentWorldB.Apply(
            DecodeMessage(
                opponentWorldBDecoder,
                UpdateDataMessage(
                    1,
                    0x40,
                    Join(
                        ExtraPublicQuery(1, 0x08),
                        ExtraPublicQuery(1, 0x08)))));
        True(opponentWorldBResult.IsSuccess);
        Equal(
            opponentMirror.Snapshot.ToDeterministicString(),
            opponentWorldB.Snapshot.ToDeterministicString());

        (PerspectiveStateMirrorV1 skippedMirror,
            GameplayMessageDecoderV1 skippedDecoder) =
            CreateMirror(0, extraCount0: 0, extraCount1: 1);
        string skippedBefore = skippedMirror.Snapshot.ToDeterministicString();
        MirrorApplyResult skippedResult = skippedMirror.Apply(
            DecodeMessage(
                skippedDecoder,
                UpdateDataMessage(
                    1,
                    0x40,
                    new byte[] { 0, 0 })));
        False(
            skippedResult.IsSuccess,
            $"skipped opponent Extra query was accepted: {skippedResult.Error}");
        Equal(GameplayErrorCode.UnknownMirrorReference, skippedResult.Error);
        Equal(skippedBefore, skippedMirror.Snapshot.ToDeterministicString());

        (PerspectiveStateMirrorV1 playerZeroOrderMirror,
            GameplayMessageDecoderV1 playerZeroOrderDecoder) =
            CreateMirror(0, extraCount0: 1, extraCount1: 1);
        ApplyI6C4Success(
            playerZeroOrderMirror,
            playerZeroOrderDecoder,
            UpdateDataMessage(
                0,
                0x40,
                Join(ExtraQuery(0xD000, 0, 0x08))));
        ApplyI6C4Success(
            playerZeroOrderMirror,
            playerZeroOrderDecoder,
            UpdateDataMessage(
                1,
                0x40,
                Join(ExtraPublicQuery(1, 0x08))));
        True(playerZeroOrderMirror.Snapshot.GetZone(
            MirrorParticipantRoleV1.Self,
            MirrorZoneV1.ExtraDeck).Cards.Single().CardCode.IsKnown);
        True(playerZeroOrderMirror.Snapshot.GetZone(
            MirrorParticipantRoleV1.Opponent,
            MirrorZoneV1.ExtraDeck).Cards.Single().CardCode.IsKnown == false);

        (PerspectiveStateMirrorV1 playerOneOrderMirror,
            GameplayMessageDecoderV1 playerOneOrderDecoder) =
            CreateMirror(1, extraCount0: 1, extraCount1: 1);
        ApplyI6C4Success(
            playerOneOrderMirror,
            playerOneOrderDecoder,
            UpdateDataMessage(
                0,
                0x40,
                Join(ExtraPublicQuery(0, 0x08))));
        ApplyI6C4Success(
            playerOneOrderMirror,
            playerOneOrderDecoder,
            UpdateDataMessage(
                1,
                0x40,
                Join(ExtraQuery(0xE000, 1, 0x08))));
        True(playerOneOrderMirror.Snapshot.GetZone(
            MirrorParticipantRoleV1.Self,
            MirrorZoneV1.ExtraDeck).Cards.Single().CardCode.IsKnown);
        True(playerOneOrderMirror.Snapshot.GetZone(
            MirrorParticipantRoleV1.Opponent,
            MirrorZoneV1.ExtraDeck).Cards.Single().CardCode.IsKnown == false);

        (PerspectiveStateMirrorV1 mismatchMirror,
            GameplayMessageDecoderV1 mismatchDecoder) =
            CreateMirror(0, extraCount0: 3, extraCount1: 0);
        string mismatchBefore = mismatchMirror.Snapshot.ToDeterministicString();
        MirrorApplyResult mismatch = mismatchMirror.Apply(
            DecodeMessage(
                mismatchDecoder,
                UpdateDataMessage(
                    0,
                    0x40,
                    Join(
                        ExtraQuery(0xC100, 0, 0x08),
                        ExtraQuery(0xC200, 0, 0x08)))));
        False(mismatch.IsSuccess);
        Equal(GameplayErrorCode.StateCapacityExceeded, mismatch.Error);
        Equal(mismatchBefore, mismatchMirror.Snapshot.ToDeterministicString());

        (PerspectiveStateMirrorV1 malformedMirror,
            GameplayMessageDecoderV1 malformedDecoder) =
            CreateMirror(0, extraCount0: 1, extraCount1: 0);
        string malformedBefore = malformedMirror.Snapshot.ToDeterministicString();
        GameplayMessageDecodeResult malformedDecoded = malformedDecoder.Decode(
            new StocGameMessagePayload(
                UpdateDataMessage(0, 0x40, new byte[] { 1, 0, 0 })));
        False(malformedDecoded.IsSuccess);
        Equal(malformedBefore, malformedMirror.Snapshot.ToDeterministicString());

        (PerspectiveStateMirrorV1 unsupportedMirror,
            GameplayMessageDecoderV1 unsupportedDecoder) =
            CreateMirror(0, extraCount0: 1, extraCount1: 0);
        string unsupportedBefore = unsupportedMirror.Snapshot.ToDeterministicString();
        GameplayMessageDecodeResult unsupportedDecoded = unsupportedDecoder.Decode(
            new StocGameMessagePayload(new byte[] { 35, 0 }));
        False(unsupportedDecoded.IsSuccess);
        Equal(unsupportedBefore, unsupportedMirror.Snapshot.ToDeterministicString());
    }

    private static void AssertI6C5NoContext()
    {
        (GameplayMirrorSessionV1 session,
            GameplayHandoffConsumerV1 consumer,
            TestTransport transport) = CreateI6C5Session(0, null);
        try
        {
            string before = session.Mirror.Snapshot.ToDeterministicString();
            int readsBefore = transport.ReadCallCount;
            PerspectiveSafeFrameSourceResultV1 result =
                session.TryCreateI6C5Frame();

            False(result.IsSuccess);
            Null(result.Frame);
            Equal(
                PerspectiveSafeFrameSourceErrorCodeV1.MissingMatchContext,
                result.Error!.Value.Code);
            Equal(before, session.Mirror.Snapshot.ToDeterministicString());
            Equal(readsBefore, transport.ReadCallCount);
        }
        finally
        {
            DisposeI6C5Session(session, consumer);
        }
    }

    private static void AssertI6C5BoundContextStability()
    {
        PerspectiveSafeMatchContextV1 context =
            CreateValidI6C5MatchContext();
        (GameplayMirrorSessionV1 session,
            GameplayHandoffConsumerV1 consumer,
            TestTransport transport) = CreateI6C5Session(
                0,
                context,
                CreatePrintedProviderForCodes(new[] { 1u }));
        try
        {
            GameplayMirrorPumpResult firstApply =
                ApplyI6C4ThroughSession(
                    session,
                    transport,
                    new byte[] { 40, 0 });
            True(firstApply.IsSuccess, firstApply.Error.ToString());
            Equal((byte)0, firstApply.Message!.NewTurn.Player);
            PerspectiveSafeFrameSourceResultV1 first =
                session.TryCreateI6C5Frame();
            True(first.IsSuccess, first.Error?.ToString() ?? "first frame rejected");
            NotNull(first.Frame);
            string firstMirror = session.Mirror.Snapshot.ToDeterministicString();
            string firstContext = MatchContextSignature(first.Frame!);

            GameplayMirrorPumpResult secondApply =
                ApplyI6C4ThroughSession(
                    session,
                    transport,
                    new byte[] { 40, 0 });
            True(secondApply.IsSuccess, secondApply.Error.ToString());
            PerspectiveSafeFrameSourceResultV1 second =
                session.TryCreateI6C5Frame();
            True(second.IsSuccess, second.Error?.ToString() ?? "second frame rejected");
            NotNull(second.Frame);
            string secondMirror = session.Mirror.Snapshot.ToDeterministicString();
            string secondContext = MatchContextSignature(second.Frame!);

            NotEqual(firstMirror, secondMirror);
            Equal(firstContext, secondContext);
            Equal(
                MatchContextSignature(context),
                firstContext);
            Equal(
                MatchContextSignature(context),
                secondContext);
        }
        finally
        {
            DisposeI6C5Session(session, consumer);
        }
    }

    private static void AssertI6C5DistinctContexts()
    {
        PerspectiveSafeMatchContextV1 contextA =
            CreateValidI6C5MatchContext();
        PerspectiveSafeMatchContextV1 contextB = new(
            perspectivePlayer: 0,
            duelFlags: 0x235,
            knowledge: new(true, false),
            ownDeck: new(
                known: true,
                mainDeck: new uint[] { 4, 5 },
                extraDeck: new uint[] { 6 }),
            opponentDeck: new(known: false));
        (GameplayMirrorSessionV1 sessionA,
            GameplayHandoffConsumerV1 consumerA,
            TestTransport transportA) = CreateI6C5Session(
                0,
                contextA,
                CreatePrintedProviderForCodes(new[] { 1u }));
        (GameplayMirrorSessionV1 sessionB,
            GameplayHandoffConsumerV1 consumerB,
            TestTransport transportB) = CreateI6C5Session(
                0,
                contextB,
                CreatePrintedProviderForCodes(new[] { 1u }));
        try
        {
            True(ApplyI6C4ThroughSession(
                    sessionA,
                    transportA,
                    new byte[] { 40, 0 }).IsSuccess);
            True(ApplyI6C4ThroughSession(
                    sessionB,
                    transportB,
                    new byte[] { 40, 0 }).IsSuccess);
            PerspectiveSafeFrameSourceResultV1 frameA =
                sessionA.TryCreateI6C5Frame();
            PerspectiveSafeFrameSourceResultV1 frameB =
                sessionB.TryCreateI6C5Frame();
            True(frameA.IsSuccess, frameA.Error?.ToString() ?? "session A rejected");
            True(frameB.IsSuccess, frameB.Error?.ToString() ?? "session B rejected");
            NotEqual(
                MatchContextSignature(frameA.Frame!),
                MatchContextSignature(frameB.Frame!));
            Equal(
                MatchContextSignature(contextA),
                MatchContextSignature(frameA.Frame!));
            Equal(
                MatchContextSignature(contextB),
                MatchContextSignature(frameB.Frame!));
        }
        finally
        {
            DisposeI6C5Session(sessionA, consumerA);
            DisposeI6C5Session(sessionB, consumerB);
        }
    }

    private static void AssertI6C5PerspectiveMismatch()
    {
        (GameplaySessionV1 transportSession,
            PerspectiveStateMirrorV1 mirror,
            GameplayHandoffConsumerV1 consumer,
            TestTransport transport) = CreateStartedSession(0);
        try
        {
            string before = mirror.Snapshot.ToDeterministicString();
            int readsBefore = transport.ReadCallCount;
            PerspectiveSafeMatchContextV1 mismatch = new(
                perspectivePlayer: 1,
                duelFlags: 0x234,
                knowledge: new(true, false),
                ownDeck: new(
                    known: true,
                    mainDeck: new uint[] { 1, 2 },
                    extraDeck: new uint[] { 3 }),
                opponentDeck: new(known: false));
            bool rejected = false;
            try
            {
                _ = new GameplayMirrorSessionV1(
                    transportSession,
                    mirror,
                    mismatch);
            }
            catch (ArgumentException exception)
            {
                rejected = true;
                True(exception.Message.StartsWith(
                    "The I6C5 match context is invalid.",
                    StringComparison.Ordinal));
                Equal("matchContext", exception.ParamName);
            }

            True(rejected, "perspective mismatch was accepted");
            Equal(before, mirror.Snapshot.ToDeterministicString());
            Equal(readsBefore, transport.ReadCallCount);
        }
        finally
        {
            transportSession.CloseOwnedTransportAsync().GetAwaiter().GetResult();
            consumer.DisposeAsync().GetAwaiter().GetResult();
        }
    }

    private static void AssertI6C5InvalidContext()
    {
        (GameplaySessionV1 transportSession,
            PerspectiveStateMirrorV1 mirror,
            GameplayHandoffConsumerV1 consumer,
            TestTransport transport) = CreateStartedSession(0);
        try
        {
            string before = mirror.Snapshot.ToDeterministicString();
            int readsBefore = transport.ReadCallCount;
            PerspectiveSafeMatchContextV1 invalid = new(
                perspectivePlayer: 0,
                duelFlags: 0x1000,
                knowledge: new(true, false),
                ownDeck: new(
                    known: true,
                    mainDeck: new uint[] { 1, 2 },
                    extraDeck: new uint[] { 3 }),
                opponentDeck: new(known: false));
            bool rejected = false;
            try
            {
                _ = new GameplayMirrorSessionV1(
                    transportSession,
                    mirror,
                    invalid);
            }
            catch (ArgumentException exception)
            {
                rejected = true;
                True(exception.Message.StartsWith(
                    "The I6C5 match context is invalid.",
                    StringComparison.Ordinal));
                Equal("matchContext", exception.ParamName);
            }

            True(rejected, "invalid match context was accepted");
            Equal(before, mirror.Snapshot.ToDeterministicString());
            Equal(readsBefore, transport.ReadCallCount);
        }
        finally
        {
            transportSession.CloseOwnedTransportAsync().GetAwaiter().GetResult();
            consumer.DisposeAsync().GetAwaiter().GetResult();
        }
    }

    private static void AssertI6C5ContextOwnership()
    {
        uint[] mutableMain = { 1, 2 };
        uint[] mutableExtra = { 3 };
        PerspectiveSafeMatchContextV1 context = new(
            perspectivePlayer: 0,
            duelFlags: 0x234,
            knowledge: new(true, false),
            ownDeck: new(true, mutableMain, mutableExtra),
            opponentDeck: new(false));
        (GameplayMirrorSessionV1 session,
            GameplayHandoffConsumerV1 consumer,
            TestTransport transport) = CreateI6C5Session(
                0,
                context,
                CreatePrintedProviderForCodes(new[] { 1u }));
        try
        {
            mutableMain[0] = 999;
            mutableExtra[0] = 998;
            True(ApplyI6C4ThroughSession(
                    session,
                    transport,
                    new byte[] { 40, 0 }).IsSuccess);
            PerspectiveSafeFrameSourceResultV1 result =
                session.TryCreateI6C5Frame();
            True(result.IsSuccess, result.Error?.ToString() ?? "owned context rejected");
            Equal((uint)1, result.Frame!.MatchContext.OwnDeck.MainDeck[0]);
            Equal((uint)3, result.Frame.MatchContext.OwnDeck.ExtraDeck[0]);
        }
        finally
        {
            DisposeI6C5Session(session, consumer);
        }
    }

    private static void AssertI6C5SessionSurface()
    {
        MethodInfo[] methods = typeof(GameplayMirrorSessionV1)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance);
        False(methods.Any(method =>
            method.Name is "SetMatchContext" or "ReplaceMatchContext"));
        MethodInfo frameMethod = methods.Single(method =>
            method.Name == "TryCreateI6C5Frame");
        Equal(0, frameMethod.GetParameters().Length);

        FieldInfo? contextField = typeof(GameplayMirrorSessionV1)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .SingleOrDefault(field =>
                field.FieldType == typeof(PerspectiveSafeMatchContextV1));
        NotNull(contextField);
        True(contextField!.IsInitOnly);
    }

    private static (GameplayMirrorSessionV1 Session,
        GameplayHandoffConsumerV1 Consumer,
        TestTransport Transport) CreateI6C5Session(
        byte perspectivePlayer,
        PerspectiveSafeMatchContextV1? context,
        PerspectiveSafePrintedProviderV1? printedProvider = null)
    {
        (GameplaySessionV1 transportSession,
            PerspectiveStateMirrorV1 mirror,
            GameplayHandoffConsumerV1 consumer,
            TestTransport transport) = CreateStartedSession(
                perspectivePlayer,
                extraCount0: 0,
                extraCount1: 0);
        try
        {
            GameplayMirrorSessionV1 session = context is null &&
                printedProvider is null
                ? new GameplayMirrorSessionV1(transportSession, mirror)
                : printedProvider is null
                    ? new GameplayMirrorSessionV1(
                        transportSession,
                        mirror,
                        context)
                    : new GameplayMirrorSessionV1(
                        transportSession,
                        mirror,
                        context,
                        printedProvider);
            return (session, consumer, transport);
        }
        catch
        {
            transportSession.CloseOwnedTransportAsync().GetAwaiter().GetResult();
            consumer.DisposeAsync().GetAwaiter().GetResult();
            throw;
        }
    }

    private static (GameplaySessionV1 Session,
        PerspectiveStateMirrorV1 Mirror,
        GameplayHandoffConsumerV1 Consumer,
        TestTransport Transport) CreateStartedSession(
        byte perspectivePlayer,
        ushort extraCount0 = 0,
        ushort extraCount1 = 0)
    {
        TestTransport transport = new(new[]
        {
            WireFrameCodec.EncodeStoc(
                StocPacketType.GameMsg,
                CreateStartBytes(
                    perspectivePlayer,
                    deckCount0: 2,
                    extraCount0: extraCount0,
                    deckCount1: 2,
                    extraCount1: extraCount1))
        });
        GameplayHandoffAcquireResult acquired =
            GameplayHandoffConsumerV1.TryCreate(
                CreateHandoff(transport, Array.Empty<byte>()));
        True(acquired.IsSuccess);
        GameplayPumpResult start = acquired.Consumer!.PumpAsync(
            CancellationToken.None).GetAwaiter().GetResult();
        True(start.IsSuccess, start.Error.ToString());
        MirrorCreateResult created = PerspectiveStateMirrorV1.TryCreate(
            start.Message!,
            start.Perspective!);
        True(created.IsSuccess, created.Error.ToString());
        return (
            start.Session!,
            created.Mirror!,
            acquired.Consumer,
            transport);
    }

    private static GameplayMessageDecoderV1 CreateEstablishedDecoder(
        byte perspectivePlayer)
    {
        GameplayMessageDecoderV1 decoder = new();
        DecodeMessage(
            decoder,
            CreateStartBytes(
                perspectivePlayer,
                deckCount0: 2,
                extraCount0: 0,
                deckCount1: 2,
                extraCount1: 0));
        return decoder;
    }

    private static void DisposeI6C5Session(
        GameplayMirrorSessionV1 session,
        GameplayHandoffConsumerV1 consumer)
    {
        session.DisposeAsync().GetAwaiter().GetResult();
        consumer.DisposeAsync().GetAwaiter().GetResult();
    }

    private static string MatchContextSignature(
        PerspectiveSafeFrameV1 frame) =>
        MatchContextSignature(frame.MatchContext);

    private static string MatchContextSignature(
        PerspectiveSafeMatchContextV1 context) =>
        string.Join(
            "|",
            context.PerspectivePlayer,
            context.DuelFlags,
            context.Knowledge.OwnDecklistKnown,
            context.Knowledge.OpponentDecklistKnown,
            string.Join(",", context.OwnDeck.MainDeck),
            string.Join(",", context.OwnDeck.ExtraDeck),
            context.OpponentDeck.Known,
            string.Join(",", context.OpponentDeck.MainDeck),
            string.Join(",", context.OpponentDeck.ExtraDeck));

    private static void AssertPrintedProviderMappings()
    {
        SyntheticPrintedRow[] rows =
        {
            new(1001, 0x00000001, 4, 1, 2, 1600, 1200, 0, 0, 0),
            new(1002, 0x00800001, 7, 2, 4, 2500, 2000, 0, 0, 0),
            new(1003, 0x04000001, 3, 4, 8, 1800, 0, 0, 0, 0x021),
            new(1004, 0x01000001, 4, 8, 16, 1700, 1500, 2, 8, 0),
            new(1005, 0x05800001, 6, 16, 32, 0, 0, 1, 9, 0x100)
        };
        byte[] artifact = CreatePrintedArtifact(rows);
        PerspectiveSafePrintedProviderV1 provider =
            CreatePrintedProvider(rows, artifact);

        PerspectiveSafeCardPropertiesV1 normal =
            GetPrinted(provider, 1001);
        Equal((uint)0x00000001, normal.Type!.Value);
        Equal((uint)4, normal.Level!.Value);
        Equal((int)1600, normal.Attack!.Value);
        Equal((int)1200, normal.Defense!.Value);
        Null(normal.Rank);
        Equal(0, normal.LinkMarkers.Count);

        PerspectiveSafeCardPropertiesV1 xyz = GetPrinted(provider, 1002);
        Equal((uint)7, xyz.Rank!.Value);
        Null(xyz.Level);
        Equal((int)2000, xyz.Defense!.Value);

        PerspectiveSafeCardPropertiesV1 link = GetPrinted(provider, 1003);
        Null(link.Defense);
        Equal((uint)3, link.LinkRating!.Value);
        Equal(2, link.LinkMarkers.Count);
        Equal(PerspectiveSafeLinkMarkerV1.BottomLeft, link.LinkMarkers[0]);
        Equal(PerspectiveSafeLinkMarkerV1.Right, link.LinkMarkers[1]);

        PerspectiveSafeCardPropertiesV1 pendulum = GetPrinted(provider, 1004);
        Equal((uint)2, pendulum.LeftScale!.Value);
        Equal((uint)8, pendulum.RightScale!.Value);
        Equal((uint)4, pendulum.Level!.Value);

        PerspectiveSafeCardPropertiesV1 xyzLink = GetPrinted(provider, 1005);
        Equal((uint)6, xyzLink.Rank!.Value);
        Equal((uint)6, xyzLink.LinkRating!.Value);
        Null(xyzLink.Level);
        Null(xyzLink.Defense);
        Equal((uint)1, xyzLink.LeftScale!.Value);
        Equal((uint)9, xyzLink.RightScale!.Value);
        Equal(PerspectiveSafeLinkMarkerV1.TopRight, xyzLink.LinkMarkers[0]);
    }

    private static void AssertPrintedProviderRequired()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0, extraCount0: 0, extraCount1: 0);
        ApplyI6C4Success(mirror, decoder, new byte[] { 40, 0 });
        PerspectiveSafeFrameSourceResultV1 result =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C5(
                mirror,
                CreateValidI6C5MatchContext());
        False(result.IsSuccess);
        Equal(
            PerspectiveSafeFrameSourceErrorCodeV1.MissingPrintedProvider,
            result.Error!.Value.Code);
        Null(result.Frame);
    }

    private static void AssertPrintedProviderDigests()
    {
        SyntheticPrintedRow[] rows =
        {
            new(1101, 0x00000001, 4, 1, 2, 100, 200, 0, 0, 0)
        };
        Equal(
            "bd8ffdf7bd8103e410da295f0aac67a357107b8e3aa92a39140a28d4accc17d2",
            TestCoverageDigest(new uint[] { 1101 }));
        Equal(
            "6697290980fe48e7a1abae9cb25112069ade98737c54f511bf1a9bea6f8b208e",
            TestSemanticDigest(rows));
        byte[] artifact = CreatePrintedArtifact(rows);
        PerspectiveSafePrintedProviderManifestV1 validManifest =
            CreatePrintedManifest(rows, artifact);
        PerspectiveSafePrintedProviderResultV1 valid =
            PerspectiveSafePrintedProviderV1.TryCreate(artifact, validManifest);
        True(valid.IsSuccess, valid.Error?.ToString() ?? "valid provider rejected");

        byte[] changedArtifact = artifact.ToArray();
        changedArtifact[^2] = (byte)'1';
        PerspectiveSafePrintedProviderResultV1 rawMismatch =
            PerspectiveSafePrintedProviderV1.TryCreate(changedArtifact, validManifest);
        False(rawMismatch.IsSuccess);
        Equal(
            PerspectiveSafePrintedProviderErrorCodeV1.InvalidArtifactHash,
            rawMismatch.Error!.Value.Code);

        PerspectiveSafePrintedProviderManifestV1 semanticMismatch =
            CreatePrintedManifest(
                rows,
                artifact,
                semanticRowsDigestSha256: new string('0', 64));
        PerspectiveSafePrintedProviderResultV1 semanticResult =
            PerspectiveSafePrintedProviderV1.TryCreate(artifact, semanticMismatch);
        False(semanticResult.IsSuccess);
        Equal(
            PerspectiveSafePrintedProviderErrorCodeV1.SemanticDigestMismatch,
            semanticResult.Error!.Value.Code);

        PerspectiveSafePrintedProviderManifestV1 coverageMismatch =
            CreatePrintedManifest(
                rows,
                artifact,
                coverageDigestSha256: new string('1', 64));
        PerspectiveSafePrintedProviderResultV1 coverageResult =
            PerspectiveSafePrintedProviderV1.TryCreate(artifact, coverageMismatch);
        False(coverageResult.IsSuccess);
        Equal(
            PerspectiveSafePrintedProviderErrorCodeV1.InvalidCoverage,
            coverageResult.Error!.Value.Code);
    }

    private static void AssertPrintedProviderFailures()
    {
        SyntheticPrintedRow[] rows =
        {
            new(1201, 0x00000001, 4, 1, 2, 100, 200, 0, 0, 0),
            new(1202, 0x00000001, 4, 1, 2, 100, 200, 0, 0, 0)
        };
        byte[] validArtifact = CreatePrintedArtifact(rows);
        PerspectiveSafePrintedProviderManifestV1 validManifest =
            CreatePrintedManifest(rows, validArtifact);

        byte[] unsortedArtifact = CreatePrintedArtifact(rows[1], rows[0]);
        PerspectiveSafePrintedProviderResultV1 unsorted =
            PerspectiveSafePrintedProviderV1.TryCreate(
                unsortedArtifact,
                CreatePrintedManifest(rows, unsortedArtifact));
        False(unsorted.IsSuccess);
        Equal(
            PerspectiveSafePrintedProviderErrorCodeV1.MalformedArtifact,
            unsorted.Error!.Value.Code);

        SyntheticPrintedRow invalidLink =
            new(1203, 0x04000001, 3, 1, 2, 100, 1, 0, 0, 1);
        byte[] invalidLinkArtifact = CreatePrintedArtifact(invalidLink);
        PerspectiveSafePrintedProviderResultV1 invalidLinkResult =
            PerspectiveSafePrintedProviderV1.TryCreate(
                invalidLinkArtifact,
                CreatePrintedManifest(new[] { invalidLink }, invalidLinkArtifact));
        False(invalidLinkResult.IsSuccess);
        Equal(
            PerspectiveSafePrintedProviderErrorCodeV1.MalformedArtifact,
            invalidLinkResult.Error!.Value.Code);

        PerspectiveSafePrintedProviderResultV1 missingArtifact =
            PerspectiveSafePrintedProviderV1.TryCreate(
                ReadOnlyMemory<byte>.Empty,
                validManifest);
        False(missingArtifact.IsSuccess);
        Equal(
            PerspectiveSafePrintedProviderErrorCodeV1.InvalidArtifactHash,
            missingArtifact.Error!.Value.Code);

        PerspectiveSafePrintedProviderManifestV1 uncoveredManifest =
            CreatePrintedManifest(
                rows,
                validArtifact,
                coveragePasscodes: new uint[] { 1201 });
        PerspectiveSafePrintedProviderResultV1 uncovered =
            PerspectiveSafePrintedProviderV1.TryCreate(
                validArtifact,
                uncoveredManifest);
        False(uncovered.IsSuccess);
        Equal(
            PerspectiveSafePrintedProviderErrorCodeV1.InvalidCoverage,
            uncovered.Error!.Value.Code);
    }

    private static void AssertPrintedProviderEnvironment()
    {
        SyntheticPrintedRow[] rows =
        {
            new(1301, 0x00000001, 4, 1, 2, 100, 200, 0, 0, 0)
        };
        byte[] artifact = CreatePrintedArtifact(rows);
        PerspectiveSafePrintedProviderManifestV1 manifest =
            CreatePrintedManifest(
                rows,
                artifact,
                ocgForgeSemanticCommit: new string('a', 40));
        PerspectiveSafePrintedProviderResultV1 result =
            PerspectiveSafePrintedProviderV1.TryCreate(artifact, manifest);
        False(result.IsSuccess);
        Equal(
            PerspectiveSafePrintedProviderErrorCodeV1.EnvironmentMismatch,
            result.Error!.Value.Code);
    }

    private static void AssertPrintedProviderPrivacySurface()
    {
        SyntheticPrintedRow[] rows =
        {
            new(1401, 0x00000001, 4, 1, 2, 100, 200, 0, 0, 0)
        };
        byte[] artifact = CreatePrintedArtifact(rows);
        PerspectiveSafePrintedProviderResultV1 result =
            PerspectiveSafePrintedProviderV1.TryCreate(
                artifact,
                CreatePrintedManifest(rows, artifact));
        True(result.IsSuccess, result.Error?.ToString() ?? "provider rejected");

        MethodInfo[] publicMethods = typeof(PerspectiveSafePrintedProviderV1)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
        False(publicMethods.Any(method =>
            method.Name.Contains("Reverse", StringComparison.OrdinalIgnoreCase) ||
            method.Name.Contains("Find", StringComparison.OrdinalIgnoreCase) ||
            method.Name.Contains("Enumerate", StringComparison.OrdinalIgnoreCase) ||
            method.Name.Contains("Possible", StringComparison.OrdinalIgnoreCase)));

        PerspectiveSafePrintedLookupResultV1 unknown =
            result.Provider!.TryGetPrinted(999999);
        False(unknown.IsSuccess);
        Equal(
            PerspectiveSafePrintedProviderErrorCodeV1.MissingCoverage,
            unknown.Error!.Value.Code);
        PerspectiveSafePrintedLookupResultV1 zero =
            result.Provider.TryGetPrinted(0);
        False(zero.IsSuccess);
        Equal(
            PerspectiveSafePrintedProviderErrorCodeV1.InvalidPasscode,
            zero.Error!.Value.Code);
    }

    private static void AssertPrintedProviderFrameIntegration()
    {
        SyntheticPrintedRow[] rows =
        {
            new(1451, 0x00000001, 4, 1, 2, 100, 200, 0, 0, 0)
        };
        byte[] artifact = CreatePrintedArtifact(rows);
        PerspectiveSafePrintedProviderV1 provider =
            CreatePrintedProvider(rows, artifact);
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0, deckCount0: 2, extraCount0: 0, deckCount1: 2, extraCount1: 0);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        ApplyI6C4Success(
            mirror,
            decoder,
            MoveMessage(
                1451,
                empty,
                new ModernLocInfoV1(0, 0x04, 0, 0x05),
                0));
        ApplyI6C4Success(mirror, decoder, new byte[] { 40, 0 });

        PerspectiveSafeFrameSourceResultV1 result =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C5(
                mirror,
                CreateValidI6C5MatchContext(),
                provider);
        True(result.IsSuccess, result.Error?.ToString() ?? "provider frame rejected");
        PerspectiveSafeEntityV1 entity = result.Frame!.Entities.Single(
            value => value.Passcode == 1451);
        NotNull(entity.Printed);
        Equal((int)100, entity.Printed!.Attack!.Value);
        Equal((int)200, entity.Printed.Defense!.Value);
        Equal((uint)4, entity.Printed.Level!.Value);

        SyntheticPrintedRow[] uncoveredRows =
        {
            new(1452, 0x00000001, 4, 1, 2, 100, 200, 0, 0, 0)
        };
        PerspectiveSafePrintedProviderV1 uncoveredProvider =
            CreatePrintedProvider(
                uncoveredRows,
                CreatePrintedArtifact(uncoveredRows));
        PerspectiveSafeFrameSourceResultV1 uncoveredResult =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C5(
                mirror,
                CreateValidI6C5MatchContext(),
                uncoveredProvider);
        False(uncoveredResult.IsSuccess);
        Equal(
            PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
            uncoveredResult.Error!.Value.Code);
    }

    private static void AssertPrintedProviderSessionBinding()
    {
        SyntheticPrintedRow[] rows =
        {
            new(1501, 0x00000001, 4, 1, 2, 100, 200, 0, 0, 0)
        };
        byte[] artifact = CreatePrintedArtifact(rows);
        PerspectiveSafePrintedProviderResultV1 providerResult =
            PerspectiveSafePrintedProviderV1.TryCreate(
                artifact,
                CreatePrintedManifest(rows, artifact));
        True(providerResult.IsSuccess, providerResult.Error?.ToString() ?? "provider rejected");

        FieldInfo? providerField = typeof(GameplayMirrorSessionV1)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .SingleOrDefault(field =>
                field.FieldType == typeof(PerspectiveSafePrintedProviderV1));
        NotNull(providerField);
        True(providerField!.IsInitOnly);

        MethodInfo frameMethod = typeof(GameplayMirrorSessionV1)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Single(method => method.Name == "TryCreateI6C5Frame");
        Equal(0, frameMethod.GetParameters().Length);

        (GameplaySessionV1 transportSession,
            PerspectiveStateMirrorV1 mirror,
            GameplayHandoffConsumerV1 consumer,
            TestTransport transport) = CreateStartedSession(0);
        GameplayMirrorSessionV1 session = new(
            transportSession,
            mirror,
            CreateValidI6C5MatchContext(),
            providerResult.Provider);
        try
        {
            True(ApplyI6C4ThroughSession(
                    session,
                    transport,
                    MoveMessage(
                        1501,
                        new ModernLocInfoV1(0, 0, 0, 0),
                        new ModernLocInfoV1(0, 0x04, 0, 0x05),
                        0)).IsSuccess);
            True(ApplyI6C4ThroughSession(
                    session,
                    transport,
                    new byte[] { 40, 0 }).IsSuccess);
            PerspectiveSafeFrameSourceResultV1 frame =
                session.TryCreateI6C5Frame();
            True(frame.IsSuccess, frame.Error?.ToString() ?? "bound provider frame rejected");
            PerspectiveSafeEntityV1 entity = frame.Frame!.Entities.Single(
                value => value.Passcode == 1501);
            NotNull(entity.Printed);
        }
        finally
        {
            DisposeI6C5Session(session, consumer);
        }
    }

    private static void AssertPrintedProviderSessionMissing()
    {
        (GameplayMirrorSessionV1 session,
            GameplayHandoffConsumerV1 consumer,
            TestTransport transport) = CreateI6C5Session(
                0,
                CreateValidI6C5MatchContext());
        try
        {
            True(ApplyI6C4ThroughSession(
                    session,
                    transport,
                    new byte[] { 40, 0 }).IsSuccess);
            string before = session.Mirror.Snapshot.ToDeterministicString();
            int readsBefore = transport.ReadCallCount;
            PerspectiveSafeFrameSourceResultV1 result =
                session.TryCreateI6C5Frame();
            False(result.IsSuccess);
            Null(result.Frame);
            Equal(
                PerspectiveSafeFrameSourceErrorCodeV1.MissingPrintedProvider,
                result.Error!.Value.Code);
            Equal(before, session.Mirror.Snapshot.ToDeterministicString());
            Equal(readsBefore, transport.ReadCallCount);
        }
        finally
        {
            DisposeI6C5Session(session, consumer);
        }
    }

    private static PerspectiveSafeCardPropertiesV1 GetPrinted(
        PerspectiveSafePrintedProviderV1 provider,
        uint code)
    {
        PerspectiveSafePrintedLookupResultV1 result =
            provider.TryGetPrinted(code);
        True(result.IsSuccess, result.Error?.ToString() ?? "printed lookup failed");
        NotNull(result.Properties);
        return result.Properties!;
    }

    private static PerspectiveSafePrintedProviderV1 CreatePrintedProvider(
        IReadOnlyList<SyntheticPrintedRow> rows,
        byte[] artifact)
    {
        PerspectiveSafePrintedProviderResultV1 result =
            PerspectiveSafePrintedProviderV1.TryCreate(
                artifact,
                CreatePrintedManifest(rows, artifact));
        True(result.IsSuccess, result.Error?.ToString() ?? "provider rejected");
        return result.Provider!;
    }

    private static PerspectiveSafePrintedProviderV1 CreatePrintedProviderForCodes(
        IEnumerable<uint> codes)
    {
        SyntheticPrintedRow[] rows = codes
            .Where(code => code != 0)
            .Distinct()
            .OrderBy(code => code)
            .Select(code => new SyntheticPrintedRow(
                code,
                0x00000001,
                4,
                1,
                2,
                100,
                200,
                0,
                0,
                0))
            .ToArray();
        if (rows.Length == 0)
        {
            rows = new[]
            {
                new SyntheticPrintedRow(1, 0x00000001, 4, 1, 2, 100, 200, 0, 0, 0)
            };
        }

        return CreatePrintedProvider(rows, CreatePrintedArtifact(rows));
    }

    private static PerspectiveSafePrintedProviderV1 CreatePrintedProviderForMirror(
        PerspectiveStateMirrorV1 mirror) =>
        CreatePrintedProviderForCodes(
            mirror.Snapshot.Cards
                .Where(card => card.CardCode.IsKnown)
                .Select(card => card.CardCode.Value));

    private static PerspectiveSafePrintedProviderManifestV1 CreatePrintedManifest(
        IReadOnlyList<SyntheticPrintedRow> rows,
        byte[] artifact,
        IReadOnlyList<uint>? coveragePasscodes = null,
        string? coverageDigestSha256 = null,
        string? semanticRowsDigestSha256 = null,
        string? ocgForgeSemanticCommit = null)
    {
        IReadOnlyList<uint> coverage = coveragePasscodes ??
            rows.Select(row => row.Code).ToArray();
        return new PerspectiveSafePrintedProviderManifestV1(
            manifestContractId: "ocgforge-ignis.i6c5.printed-manifest.v1",
            providerContractId: "ocgforge-ignis.i6c5.printed-provider.v1",
            semanticRowsContractId: "ocgforge-ignis.i6c5.printed-semantic-rows.v1",
            fieldMappingContractId: "ocgforge-ignis.i6c5.printed-field-mapping.v1",
            coverageContractId: "ocgforge-ignis.i6c5.printed-coverage.v1",
            coverageDigestSha256: coverageDigestSha256 ?? TestCoverageDigest(coverage),
            semanticRowsDigestSha256: semanticRowsDigestSha256 ?? TestSemanticDigest(rows),
            ocgForgeSemanticCommit: ocgForgeSemanticCommit ??
                "f929de0b4d4157327dba003067d2e21e42f7ad75",
            rulesBundleId: "3adfe6b4cfe2c2805e50b389fc0eb4e70a3b0b6107436614d328fddc865e585f",
            babelCdbRepository: "https://github.com/ProjectIgnis/BabelCDB.git",
            babelCdbCommit: new string('a', 40),
            babelCdbCheckoutSha256: new string('b', 64),
            cardsCdbSha256: new string('c', 64),
            transformationSourceRepository: "https://github.com/chrismaghuhn/OCGForge.git",
            transformationSourceCommit: new string('d', 40),
            transformationSourcePath: "tools/prepare_card_data.py",
            transformationFileSha256: new string('e', 64),
            sourceArtifactFormatId: "ocgforge-ignis.i6c5.printed-source-artifact.pipe12.v1",
            sourceArtifactSha256: TestHash(artifact),
            coveragePasscodes: coverage);
    }

    private static byte[] CreatePrintedArtifact(
        params SyntheticPrintedRow[] rows)
    {
        StringBuilder builder = new();
        builder.Append(
                "# code|alias|setcode|type|level|attribute|race|atk|def|lscale|rscale|link_marker")
            .Append('\n');
        foreach (SyntheticPrintedRow row in rows)
        {
            builder.Append(row.Code).Append('|')
                .Append(0).Append('|')
                .Append(0).Append('|')
                .Append(row.Type).Append('|')
                .Append(row.Level).Append('|')
                .Append(row.Attribute).Append('|')
                .Append(row.Race).Append('|')
                .Append(row.Attack).Append('|')
                .Append(row.Defense).Append('|')
                .Append(row.LeftScale).Append('|')
                .Append(row.RightScale).Append('|')
                .Append(row.LinkMarker)
                .Append('\n');
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static string TestHash(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static string TestCoverageDigest(IReadOnlyList<uint> coverage)
    {
        using MemoryStream stream = new();
        stream.Write(Encoding.ASCII.GetBytes(
            "OCGFORGE-IGNIS-I6C5-PRINTED-COVERAGE-V1\0"));
        WriteTestUInt32(stream, checked((uint)coverage.Count));
        foreach (uint code in coverage)
        {
            WriteTestUInt32(stream, code);
        }

        return TestHash(stream.ToArray());
    }

    private static string TestSemanticDigest(
        IReadOnlyList<SyntheticPrintedRow> rows)
    {
        using MemoryStream stream = new();
        stream.Write(Encoding.ASCII.GetBytes(
            "OCGFORGE-IGNIS-I6C5-PRINTED-ROWS-V1\0"));
        WriteTestUInt32(stream, checked((uint)rows.Count));
        foreach (SyntheticPrintedRow row in rows)
        {
            WriteTestUInt32(stream, row.Code);
            WriteTestUInt32(stream, row.Type);
            WriteTestUInt32(stream, row.Level);
            WriteTestUInt32(stream, row.Attribute);
            WriteTestUInt64(stream, row.Race);
            WriteTestInt32(stream, row.Attack);
            WriteTestInt32(stream, row.Defense);
            WriteTestUInt32(stream, row.LeftScale);
            WriteTestUInt32(stream, row.RightScale);
            WriteTestUInt32(stream, row.LinkMarker);
        }

        return TestHash(stream.ToArray());
    }

    private static void WriteTestUInt32(Stream stream, uint value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        stream.Write(bytes);
    }

    private static void WriteTestUInt64(Stream stream, ulong value)
    {
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(bytes, value);
        stream.Write(bytes);
    }

    private static void WriteTestInt32(Stream stream, int value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        stream.Write(bytes);
    }

    private readonly record struct IdleSimpleSpec(
        uint CardCode,
        ModernLocInfoV1 Location);

    private readonly record struct IdleActivationSpec(
        uint CardCode,
        ModernLocInfoV1 Location,
        ulong Description,
        byte ClientMode);

    private readonly record struct SyntheticPrintedRow(
        uint Code,
        uint Type,
        uint Level,
        uint Attribute,
        ulong Race,
        int Attack,
        int Defense,
        uint LeftScale,
        uint RightScale,
        uint LinkMarker);

    private static byte[] ExtraQuery(uint code, byte owner, uint position) =>
        Join(
            QueryRecord(QueryFlagV1.Code, U32(code)),
            QueryRecord(QueryFlagV1.Position, U32(position)),
            QueryRecord(QueryFlagV1.Owner, new[] { owner }),
            QueryEnd());

    private static byte[] ExtraQueryWithoutPosition(uint code, byte owner) =>
        Join(
            QueryRecord(QueryFlagV1.Code, U32(code)),
            QueryRecord(QueryFlagV1.Owner, new[] { owner }),
            QueryEnd());

    private static byte[] ExtraPublicQuery(byte owner, uint position) =>
        Join(
            QueryRecord(QueryFlagV1.Position, U32(position)),
            QueryRecord(QueryFlagV1.Owner, new[] { owner }),
            QueryRecord(QueryFlagV1.IsHidden, new byte[] { 1 }),
            QueryEnd());

    private static byte[] ExtraPublicQueryWithoutPosition(byte owner) =>
        Join(
            QueryRecord(QueryFlagV1.Owner, new[] { owner }),
            QueryRecord(QueryFlagV1.IsHidden, new byte[] { 1 }),
            QueryEnd());

    private static void AssertI6C2AbsoluteGlobals()
    {
        foreach (byte playerType in new byte[] { 0, 1 })
        {
            (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
                CreateMirror(playerType);
            PerspectiveSafeI6C2StateSourceV1 initial =
                GetI6C2Source(mirror);
            Equal((uint)8000, initial.Globals.LifePoints[0]);
            Equal((uint)7000, initial.Globals.LifePoints[1]);
            True(initial.Globals.TurnPlayer is null);
            True(initial.Globals.TurnCount is null);
            True(initial.Globals.Phase is null);
            False(initial.Globals.Terminal);
            True(initial.Globals.Winner is null);
            True(initial.Globals.WinReason is null);
            False(initial.IsComplete);
            Equal(
                PerspectiveSafeI6C2SourceStatusV1.Proven,
                initial.GetStatus(PerspectiveSafeI6C2ConstituentV1.LifePoints));
            Equal(
                PerspectiveSafeI6C2SourceStatusV1.BlockedPendingI6C5,
                initial.GetStatus(PerspectiveSafeI6C2ConstituentV1.DuelFlags));
            Equal(
                PerspectiveSafeI6C2SourceStatusV1.OutsideI6CPendingI6D,
                initial.GetStatus(PerspectiveSafeI6C2ConstituentV1.PlayerToAct));
            Equal(
                PerspectiveSafeI6C2SourceStatusV1.BlockedPendingI6C3,
                initial.GetStatus(PerspectiveSafeI6C2ConstituentV1.ChainLength));
            Equal(
                PerspectiveSafeI6C2SourceStatusV1.BlockedPendingI6C3,
                initial.GetStatus(PerspectiveSafeI6C2ConstituentV1.Relationships));
            Equal(
                PerspectiveSafeI6C2SourceStatusV1.Blocked,
                initial.GetStatus(PerspectiveSafeI6C2ConstituentV1.VisibleEvents));
            Equal(
                PerspectiveSafeI6C2SourceStatusV1.Blocked,
                initial.GetStatus(PerspectiveSafeI6C2ConstituentV1.EventIndex));
            Equal(
                PerspectiveSafeI6C2SourceStatusV1.BlockedPendingI6C5,
                initial.GetStatus(PerspectiveSafeI6C2ConstituentV1.MatchContext));

            ApplyMirrorMessage(mirror, decoder, new byte[] { 40, 1 });
            ApplyMirrorMessage(mirror, decoder, new byte[] { 41, 4, 0 });
            PerspectiveSafeI6C2StateSourceV1 updated =
                GetI6C2Source(mirror);
            Equal((byte)1, updated.Globals.TurnPlayer!.Value);
            Equal((uint)1, updated.Globals.TurnCount!.Value);
            Equal((uint)4, updated.Globals.Phase!.Value);
            Equal(
                PerspectiveSafeI6C2SourceStatusV1.Proven,
                updated.GetStatus(PerspectiveSafeI6C2ConstituentV1.TurnPlayer));
            Equal(
                PerspectiveSafeI6C2SourceStatusV1.Proven,
                updated.GetStatus(PerspectiveSafeI6C2ConstituentV1.TurnCount));
        }
    }

    private static void AssertI6C2LifePoints()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        ApplyMirrorMessage(mirror, decoder, new byte[] { 91, 0, 0xf4, 0x01, 0, 0 });
        ApplyMirrorMessage(mirror, decoder, new byte[] { 92, 0, 0xfa, 0, 0, 0 });
        ApplyMirrorMessage(mirror, decoder, new byte[] { 94, 1, 0x70, 0x17, 0, 0 });
        ApplyMirrorMessage(mirror, decoder, new byte[] { 100, 1, 0xf4, 0x01, 0, 0 });

        PerspectiveSafeI6C2StateSourceV1 source = GetI6C2Source(mirror);
        Equal((uint)7750, source.Globals.LifePoints[0]);
        Equal((uint)5500, source.Globals.LifePoints[1]);
        string beforeFailure = I6C2Signature(source);
        MirrorApplyResult failed = mirror.Apply(DecodeMessage(
            decoder,
            new byte[] { 92, 0, 0xff, 0xff, 0xff, 0xff }));
        False(failed.IsSuccess);
        Equal(GameplayErrorCode.ArithmeticFailure, failed.Error);
        Equal(beforeFailure, I6C2Signature(GetI6C2Source(mirror)));
    }

    private static void AssertI6C2TerminalValues()
    {
        foreach (byte winner in new byte[] { 0, 1, 2 })
        {
            (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
                CreateMirror(0);
            byte reason = (byte)(0x30 + winner);
            ApplyMirrorMessage(mirror, decoder, new byte[] { 5, winner, reason });
            PerspectiveSafeI6C2GlobalsV1 globals =
                GetI6C2Source(mirror).Globals;
            True(globals.Terminal);
            Equal(reason, globals.WinReason!.Value);
            if (winner == 2)
            {
                True(globals.Winner is null);
            }
            else
            {
                Equal(winner, globals.Winner!.Value);
            }
        }
    }

    private static void AssertI6C2ZonesAndLocators()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0, deckCount0: 4, extraCount0: 2, deckCount1: 4, extraCount1: 2);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        ApplyMirrorMessage(mirror, decoder, DrawMessage(0, (0x100u, 0x08u)));
        ApplyMirrorMessage(
            mirror,
            decoder,
            DrawMessage(
                1,
                (0x200u, 0x08u),
                (0x200u, 0x04u),
                (0x210u, 0x04u),
                (0x210u, 0x04u)));
        ApplyMirrorMessage(mirror, decoder, MoveMessage(
            0x300,
            empty,
            new ModernLocInfoV1(0, 0x04, 0, 0x04),
            0));
        ApplyMirrorMessage(mirror, decoder, MoveMessage(
            0x400,
            empty,
            new ModernLocInfoV1(1, 0x04, 0, 0x04),
            0));
        ApplyMirrorMessage(mirror, decoder, MoveMessage(
            0x500,
            empty,
            new ModernLocInfoV1(1, 0x04, 1, 0x08),
            0));
        ApplyMirrorMessage(mirror, decoder, MoveMessage(
            0x600,
            empty,
            new ModernLocInfoV1(0, 0x10, 0, 0x04),
            0));
        ApplyMirrorMessage(mirror, decoder, MoveMessage(
            0x700,
            empty,
            new ModernLocInfoV1(0, 0x20, 0, 0x04),
            0));
        ApplyMirrorMessage(mirror, decoder, MoveMessage(
            0x800,
            empty,
            new ModernLocInfoV1(0, 0x40, 0, 0x04),
            0));
        ApplyMirrorMessage(mirror, decoder, MoveMessage(
            0x900,
            empty,
            new ModernLocInfoV1(1, 0x40, 0, 0x08),
            0));

        PerspectiveSafeI6C2StateSourceV1 source = GetI6C2Source(mirror);
        PerspectiveSafeZoneV1 ownDeck = FindZone(
            source,
            0,
            PerspectiveSafeSemanticZoneV1.MainDeck);
        Equal((uint)3, ownDeck.TotalCount);
        Equal((uint)0, ownDeck.PublicIdentityCount);
        Equal((uint)3, ownDeck.HiddenCount);
        False(ownDeck.PlayerObservableOrder);

        PerspectiveSafeZoneV1 ownHand = FindZone(
            source,
            0,
            PerspectiveSafeSemanticZoneV1.Hand);
        Equal((uint)1, ownHand.TotalCount);
        Equal((uint)1, ownHand.PublicIdentityCount);
        Equal((uint)0, ownHand.HiddenCount);
        True(ownHand.PlayerObservableOrder);
        PerspectiveSafeEntityV1 ownHandEntity = FindEntity(source, "p0:HAND:0");
        Equal((uint)0x100, ownHandEntity.Passcode!.Value);
        Equal((uint)0, ownHandEntity.Sequence!.Value);

        PerspectiveSafeZoneV1 opponentHand = FindZone(
            source,
            1,
            PerspectiveSafeSemanticZoneV1.Hand);
        Equal((uint)4, opponentHand.TotalCount);
        Equal((uint)0, opponentHand.PublicIdentityCount);
        Equal((uint)4, opponentHand.HiddenCount);
        False(opponentHand.PlayerObservableOrder);
        Equal(
            3,
            source.Entities.Count(entity =>
                entity.Locator.StartsWith("p1:HAND", StringComparison.Ordinal)));

        PerspectiveSafeEntityV1 opponentPublicHand = FindEntity(
            source,
            "p1:HAND:public:512:0");
        Equal((uint)0x200, opponentPublicHand.Passcode!.Value);
        True(opponentPublicHand.Sequence is null);
        Equal((uint)0x210, FindEntity(
            source,
            "p1:HAND:public:528:0").Passcode!.Value);
        Equal((uint)0x210, FindEntity(
            source,
            "p1:HAND:public:528:1").Passcode!.Value);

        PerspectiveSafeZoneV1 opponentMonster = FindZone(
            source,
            1,
            PerspectiveSafeSemanticZoneV1.MonsterZone);
        Equal((uint)2, opponentMonster.TotalCount);
        Equal((uint)1, opponentMonster.PublicIdentityCount);
        Equal((uint)1, opponentMonster.HiddenCount);
        Equal(PerspectiveSafePositionV1.FaceUpDefense, FindEntity(source, "p1:MONSTER_ZONE:0").Position);
        Equal(PerspectiveSafePositionV1.FaceDownDefense, FindEntity(source, "p1:MONSTER_ZONE:1").Position);

        Equal(
            (uint)0x600,
            FindEntity(source, "p0:GRAVEYARD:0").Passcode!.Value);
        Equal(
            (uint)0x700,
            FindEntity(source, "p0:BANISHED:0").Passcode!.Value);
        PerspectiveSafeEntityV1 ownExtra = FindEntity(
            source,
            "p0:EXTRA_DECK:public:2048:0");
        Equal((uint)0x800, ownExtra.Passcode!.Value);
        True(ownExtra.Sequence is null);
        True(source.Entities.All(entity =>
            !entity.Locator.Contains("p1:EXTRA_DECK", StringComparison.Ordinal)));
        False(source.Entities.Any(entity =>
            entity.Locator.Contains("MAIN_DECK", StringComparison.Ordinal)));
        Equal(
            PerspectiveSafeI6C2SourceStatusV1.BlockedPendingI6C5,
            source.GetStatus(PerspectiveSafeI6C2ConstituentV1.SpellTrapLayout));
        Equal(
            PerspectiveSafeI6C2SourceStatusV1.BlockedPendingI6C3,
            source.GetStatus(PerspectiveSafeI6C2ConstituentV1.OverlayZone));
        Equal(
            PerspectiveSafeI6C2SourceStatusV1.Blocked,
            source.GetStatus(PerspectiveSafeI6C2ConstituentV1.EntityPrintedProperties));
    }

    private static void AssertI6C2PairedPrivacy()
    {
        PerspectiveStateMirrorV1 first = CreateHiddenWorld(0x11112222);
        PerspectiveStateMirrorV1 second = CreateHiddenWorld(0xaaaabbbb);
        PerspectiveSafeI6C2StateSourceV1 firstSource = GetI6C2Source(first);
        PerspectiveSafeI6C2StateSourceV1 secondSource = GetI6C2Source(second);
        Equal(I6C2Signature(firstSource), I6C2Signature(secondSource));
        True(firstSource.Entities.All(entity =>
            !entity.Locator.Contains("p1:HAND", StringComparison.Ordinal)));
        True(firstSource.Entities.All(entity =>
            !entity.Locator.Contains("p1:EXTRA_DECK", StringComparison.Ordinal)));
    }

    private static void AssertI6C2KnowledgeDestruction()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0, deckCount1: 2);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        ModernLocInfoV1 monster = new(1, 0x04, 0, 0x04);
        ApplyMirrorMessage(mirror, decoder, MoveMessage(0x1234, empty, monster, 0));
        PerspectiveSafeI6C2StateSourceV1 revealed = GetI6C2Source(mirror);
        True(revealed.Entities.Any(entity => entity.Locator == "p1:MONSTER_ZONE:0"));

        ApplyMirrorMessage(
            mirror,
            decoder,
            MoveMessage(
                0,
                monster,
                new ModernLocInfoV1(1, 0x02, 0, 0x08),
                0));
        PerspectiveSafeI6C2StateSourceV1 hidden = GetI6C2Source(mirror);
        False(hidden.Entities.Any(entity => entity.Locator == "p1:MONSTER_ZONE:0"));
        False(hidden.Entities.Any(entity =>
            entity.Locator.StartsWith("p1:HAND", StringComparison.Ordinal)));
        Equal((uint)1, FindZone(
            hidden,
            1,
            PerspectiveSafeSemanticZoneV1.Hand).TotalCount);
    }

    private static void AssertI6C2CurrentProperties()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        for (byte sequence = 0; sequence < 4; sequence++)
        {
            ApplyMirrorMessage(
                mirror,
                decoder,
                MoveMessage(
                    (uint)(0x1000 + sequence),
                    empty,
                    new ModernLocInfoV1(0, 0x04, sequence, 0x04),
                    0));
        }

        ModernQueryV1 normal = DecodeQuery(
            QueryRecord(QueryFlagV1.Type, U32(0x01)),
            QueryRecord(QueryFlagV1.Attribute, U32(0x02)),
            QueryRecord(QueryFlagV1.Race, U64(0x03)),
            QueryRecord(QueryFlagV1.Attack, I32(-100)),
            QueryRecord(QueryFlagV1.Defense, I32(2100)),
            QueryRecord(QueryFlagV1.BaseAttack, I32(1900)),
            QueryRecord(QueryFlagV1.BaseDefense, I32(1600)),
            QueryRecord(QueryFlagV1.Level, U32(4)),
            QueryRecord(QueryFlagV1.Status, U32(0x10)),
            QueryRecord(QueryFlagV1.LScale, U32(5)),
            QueryRecord(QueryFlagV1.RScale, U32(6)),
            QueryRecord(QueryFlagV1.Counters, Join(
                U32(2),
                U32(0x00020001),
                U32(0x00010002))),
            QueryRecord(QueryFlagV1.Owner, new byte[] { 0 }),
            QueryEnd());
        ModernQueryV1 xyz = DecodeQuery(
            QueryRecord(QueryFlagV1.Type, U32(0x00800000)),
            QueryRecord(QueryFlagV1.Level, U32(4)),
            QueryRecord(QueryFlagV1.Rank, U32(7)),
            QueryRecord(QueryFlagV1.Defense, I32(999)),
            QueryEnd());
        ModernQueryV1 link = DecodeQuery(
            QueryRecord(QueryFlagV1.Type, U32(0x04000000)),
            QueryRecord(QueryFlagV1.Defense, I32(999)),
            QueryRecord(QueryFlagV1.BaseDefense, I32(998)),
            QueryRecord(QueryFlagV1.Link, Join(U32(3), U32(0x81))),
            QueryEnd());
        ApplyMirrorMessage(mirror, decoder, UpdateCardMessage(0, 0x04, 0, normal));
        ApplyMirrorMessage(mirror, decoder, UpdateCardMessage(0, 0x04, 1, xyz));
        ApplyMirrorMessage(mirror, decoder, UpdateCardMessage(0, 0x04, 2, link));

        PerspectiveSafeI6C2StateSourceV1 source = GetI6C2Source(mirror);
        PerspectiveSafeCardPropertiesV1 normalProperties =
            FindEntity(source, "p0:MONSTER_ZONE:0").Current!;
        Equal((uint)1, normalProperties.Type!.Value);
        Equal((uint)2, normalProperties.Attribute!.Value);
        Equal((ulong)3, normalProperties.Race!.Value);
        Equal(-100, normalProperties.Attack!.Value);
        Equal(2100, normalProperties.Defense!.Value);
        Equal(1900, normalProperties.BaseAttack!.Value);
        Equal(1600, normalProperties.BaseDefense!.Value);
        Equal((uint)4, normalProperties.Level!.Value);
        True(normalProperties.Rank is null);
        Equal((uint)5, normalProperties.LeftScale!.Value);
        Equal((uint)6, normalProperties.RightScale!.Value);
        Equal((uint)1, normalProperties.Counters[0].Type);
        Equal((uint)2, normalProperties.Counters[0].Count);
        Equal((byte)0, FindEntity(source, "p0:MONSTER_ZONE:0").Owner!.Value);

        PerspectiveSafeCardPropertiesV1 xyzProperties =
            FindEntity(source, "p0:MONSTER_ZONE:1").Current!;
        True(xyzProperties.Level is null);
        Equal((uint)7, xyzProperties.Rank!.Value);
        Equal(999, xyzProperties.Defense!.Value);

        PerspectiveSafeCardPropertiesV1 linkProperties =
            FindEntity(source, "p0:MONSTER_ZONE:2").Current!;
        Equal((uint)3, linkProperties.LinkRating!.Value);
        True(
            new[]
            {
                PerspectiveSafeLinkMarkerV1.BottomLeft,
                PerspectiveSafeLinkMarkerV1.TopRight
            }.SequenceEqual(linkProperties.LinkMarkers));
        True(linkProperties.Defense is null);
        True(linkProperties.BaseDefense is null);

        PerspectiveSafeCardPropertiesV1 missing =
            FindEntity(source, "p0:MONSTER_ZONE:3").Current!;
        True(missing.Type is null);
        True(missing.Attack is null);
        True(missing.Defense is null);
        True(FindEntity(source, "p0:MONSTER_ZONE:0").Printed is null);
    }

    private static void AssertI6C2DeferredBoundaries()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        ApplyMirrorMessage(
            mirror,
            decoder,
            MoveMessage(
                0x1111,
                empty,
                new ModernLocInfoV1(0, 0x08, 0, 0x04),
                0));
        PerspectiveSafeI6C2StateSourceV1 layoutBlocked = GetI6C2Source(mirror);
        False(layoutBlocked.Entities.Any(entity =>
            entity.Locator.StartsWith("p0:SPELL_TRAP", StringComparison.Ordinal)));
        Equal(
            PerspectiveSafeI6C2SourceStatusV1.BlockedPendingI6C5,
            layoutBlocked.GetStatus(PerspectiveSafeI6C2ConstituentV1.SpellTrapLayout));

        ApplyMirrorMessage(
            mirror,
            decoder,
            MoveMessage(
                0x2222,
                empty,
                new ModernLocInfoV1(0, 0x04, 0, 0),
                0));
        ApplyMirrorMessage(
            mirror,
            decoder,
            MoveMessage(
                0x3333,
                empty,
                new ModernLocInfoV1(0, 0x84, 0, 0),
                0));
        PerspectiveSafeI6C2StateSourceV1 overlayBlocked = GetI6C2Source(mirror);
        False(overlayBlocked.Entities.Any(entity =>
            entity.Locator.Contains("OVERLAY", StringComparison.Ordinal)));
        Equal(
            PerspectiveSafeI6C2SourceStatusV1.BlockedPendingI6C3,
            overlayBlocked.GetStatus(PerspectiveSafeI6C2ConstituentV1.OverlayZone));
        Equal(
            PerspectiveSafeI6C2SourceStatusV1.BlockedPendingI6C3,
            overlayBlocked.GetStatus(PerspectiveSafeI6C2ConstituentV1.ChainLength));
    }

    private static void AssertI6C2SemanticOrdering()
    {
        (PerspectiveStateMirrorV1 first, GameplayMessageDecoderV1 firstDecoder) =
            CreateMirror(0);
        (PerspectiveStateMirrorV1 second, GameplayMessageDecoderV1 secondDecoder) =
            CreateMirror(0);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        ApplyMirrorMessage(first, firstDecoder, MoveMessage(
            0xaaaa,
            empty,
            new ModernLocInfoV1(0, 0x04, 0, 0x04),
            0));
        ApplyMirrorMessage(first, firstDecoder, MoveMessage(
            0xaaaa,
            empty,
            new ModernLocInfoV1(0, 0x04, 1, 0x04),
            0));
        ApplyMirrorMessage(second, secondDecoder, MoveMessage(
            0xaaaa,
            empty,
            new ModernLocInfoV1(0, 0x04, 1, 0x04),
            0));
        ApplyMirrorMessage(second, secondDecoder, MoveMessage(
            0xaaaa,
            empty,
            new ModernLocInfoV1(0, 0x04, 0, 0x04),
            0));
        Equal(
            I6C2Signature(GetI6C2Source(first)),
            I6C2Signature(GetI6C2Source(second)));
    }

    private static void AssertI6C2TransportChunking()
    {
        byte[][] whole = BuildI6C2TranscriptChunks(new[] { 4096 });
        byte[][] fragmented = BuildI6C2TranscriptChunks(new[] { 1, 2, 5, 3, 7 });
        Equal(
            RunChunkedI6C2Source(whole),
            RunChunkedI6C2Source(fragmented));
    }

    private static void AssertI6C2CrossPileOrdinalContinuity()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0, deckCount1: 2, extraCount1: 1);
        ApplyMirrorMessage(
            mirror,
            decoder,
            DrawMessage(
                1,
                (0x100u, 0x04u),
                (0x200u, 0x04u)));
        ApplyMirrorMessage(
            mirror,
            decoder,
            MoveMessage(
                0x100,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(1, 0x40, 0, 0x05),
                0));

        PerspectiveSafeI6C2StateSourceV1 source = GetI6C2Source(mirror);
        Equal(
            (uint)0x100,
            FindEntity(source, "p1:HAND:public:256:0").Passcode!.Value);
        Equal(
            (uint)0x200,
            FindEntity(source, "p1:HAND:public:512:0").Passcode!.Value);
        Equal(
            (uint)0x100,
            FindEntity(source, "p1:EXTRA_DECK:public:256:1").Passcode!.Value);
    }

    private static void AssertI6C3OverlayRelationSource()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        ModernLocInfoV1 parent = new(0, 0x04, 2, 0x05);
        ModernLocInfoV1 material = new(0, 0x84, 2, 0);
        ApplyMirrorMessage(mirror, decoder, MoveMessage(0x1111, empty, parent, 0));
        ApplyMirrorMessage(mirror, decoder, MoveMessage(0x2222, empty, material, 0));

        PerspectiveSafeI6C3SourceResultV1 result =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C3(mirror);
        True(result.IsSuccess, result.Error?.ToString() ?? "I6C3 source failed");
        NotNull(result.Source);
        Equal(
            1,
            result.Source!.Zones.Count(zone =>
                zone.Player == 0 &&
                zone.Kind == PerspectiveSafeSemanticZoneV1.Overlay));
        Equal(
            1,
            result.Source.Entities.Count(entity =>
                entity.Locator == "p0:OVERLAY:2:0"));
        True(result.Source.Relationships.Any(relation =>
            relation.Kind == PerspectiveSafeRelationshipKindV1.XyzMaterial &&
            relation.Source == "p0:OVERLAY:2:0" &&
            relation.Target == "p0:MONSTER_ZONE:2"));
        False(result.Source.Relationships.Any(relation =>
            relation.Kind == PerspectiveSafeRelationshipKindV1.XyzMaterial &&
            relation.Source == "p0:MONSTER_ZONE:2" &&
            relation.Target == "p0:OVERLAY:2:0"));
    }

    private static void AssertI6C3OverlayLifecycle()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        ModernLocInfoV1 parent = new(0, 0x04, 2, 0x05);
        ModernLocInfoV1 first = new(0, 0x84, 2, 0);
        ModernLocInfoV1 second = new(0, 0x84, 2, 1);
        ApplyMirrorMessage(mirror, decoder, MoveMessage(0x1111, empty, parent, 0));
        ApplyMirrorMessage(mirror, decoder, MoveMessage(0x2222, empty, first, 0));
        ApplyMirrorMessage(mirror, decoder, MoveMessage(0x3333, empty, second, 0));

        PerspectiveSafeI6C3StateSourceV1 initial = GetI6C3Source(mirror);
        PerspectiveSafeZoneV1 overlay = FindI6C3Zone(
            initial,
            0,
            PerspectiveSafeSemanticZoneV1.Overlay);
        Equal((uint)2, overlay.TotalCount);
        Equal((uint)2, overlay.PublicIdentityCount);
        Equal((uint)0, overlay.HiddenCount);
        False(overlay.PlayerObservableOrder);
        Equal((uint)0x2222, FindI6C3Entity(
            initial,
            "p0:OVERLAY:2:0").Passcode!.Value);
        Equal((uint)0x3333, FindI6C3Entity(
            initial,
            "p0:OVERLAY:2:1").Passcode!.Value);
        PerspectiveSafeEntityV1 initialSecond = FindI6C3Entity(
            initial,
            "p0:OVERLAY:2:1");
        Equal(PerspectiveSafePositionV1.FaceUpAttack, initialSecond.Position);
        True(initialSecond.FaceUp);
        False(initialSecond.FaceDown);

        ApplyMirrorMessage(
            mirror,
            decoder,
            MoveMessage(
                0x2222,
                first,
                new ModernLocInfoV1(0, 0x10, 0, 0x04),
                0));
        PerspectiveSafeI6C3StateSourceV1 afterDetach = GetI6C3Source(mirror);
        PerspectiveSafeZoneV1 remaining = FindI6C3Zone(
            afterDetach,
            0,
            PerspectiveSafeSemanticZoneV1.Overlay);
        Equal((uint)1, remaining.TotalCount);
        Equal((uint)0x3333, FindI6C3Entity(
            afterDetach,
            "p0:OVERLAY:2:0").Passcode!.Value);
        PerspectiveSafeEntityV1 reindexedSecond = FindI6C3Entity(
            afterDetach,
            "p0:OVERLAY:2:0");
        Equal(PerspectiveSafePositionV1.Unknown, reindexedSecond.Position);
        False(reindexedSecond.FaceUp);
        False(reindexedSecond.FaceDown);
        False(afterDetach.Entities.Any(entity =>
            entity.Locator == "p0:OVERLAY:2:1"));
        True(afterDetach.Relationships.Any(relation =>
            relation.Kind == PerspectiveSafeRelationshipKindV1.XyzMaterial &&
            relation.Source == "p0:OVERLAY:2:0" &&
            relation.Target == "p0:MONSTER_ZONE:2"));
    }

    private static void AssertI6C3PreservesI6C2Source()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        ModernLocInfoV1 parent = new(0, 0x04, 2, 0x05);
        ModernLocInfoV1 material = new(0, 0x84, 2, 0);
        ApplyMirrorMessage(mirror, decoder, MoveMessage(0x1400, empty, parent, 0));
        ApplyMirrorMessage(mirror, decoder, MoveMessage(0x1401, empty, material, 0));
        string before = I6C2Signature(GetI6C2Source(mirror));
        PerspectiveSafeI6C3StateSourceV1 source = GetI6C3Source(mirror);
        Equal(before, I6C2Signature(source.BaseSource));
        Equal(before, I6C2Signature(GetI6C2Source(mirror)));
    }

    private static void AssertI6C3HiddenOverlayPrivacy()
    {
        PerspectiveSafeI6C3StateSourceV1 first =
            GetI6C3Source(CreateHiddenOverlayWorld(0x8100, 0x8200));
        PerspectiveSafeI6C3StateSourceV1 second =
            GetI6C3Source(CreateHiddenOverlayWorld(0x9100, 0x9200));
        Equal(I6C3Signature(first), I6C3Signature(second));
        PerspectiveSafeZoneV1 overlay = FindI6C3Zone(
            first,
            1,
            PerspectiveSafeSemanticZoneV1.Overlay);
        Equal((uint)1, overlay.TotalCount);
        Equal((uint)0, overlay.PublicIdentityCount);
        Equal((uint)1, overlay.HiddenCount);
        PerspectiveSafeEntityV1 material = FindI6C3Entity(
            first,
            "p1:OVERLAY:2:0");
        False(material.IdentityKnown);
        True(material.Passcode is null);
        True(first.Relationships.Any(relation =>
            relation.Kind == PerspectiveSafeRelationshipKindV1.XyzMaterial &&
            relation.Source == "p1:OVERLAY:2:0" &&
            relation.Target == "p1:MONSTER_ZONE:2"));
    }

    private static void AssertI6C3RelationLifecycle()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        ModernLocInfoV1 source = new(0, 0x04, 0, 0x05);
        ModernLocInfoV1 target = new(0, 0x04, 1, 0x05);
        ModernLocInfoV1 retarget = new(0, 0x04, 2, 0x05);
        ApplyMirrorMessage(mirror, decoder, MoveMessage(0x1000, empty, source, 0));
        ApplyMirrorMessage(mirror, decoder, MoveMessage(0x2000, empty, target, 0));
        ApplyMirrorMessage(mirror, decoder, MoveMessage(0x3000, empty, retarget, 0));

        ApplyMirrorMessage(mirror, decoder, CardTargetMessage(source, target));
        PerspectiveSafeI6C3StateSourceV1 targeted = GetI6C3Source(mirror);
        Equal(1, targeted.Relationships.Count(relation =>
            relation.Kind == PerspectiveSafeRelationshipKindV1.Target));
        True(targeted.Relationships.Any(relation =>
            relation.Kind == PerspectiveSafeRelationshipKindV1.Target &&
            relation.Source == "p0:MONSTER_ZONE:0" &&
            relation.Target == "p0:MONSTER_ZONE:1"));

        string beforeInvalidCancel = I6C3Signature(targeted);
        MirrorApplyResult invalidCancel = mirror.Apply(
            DecodeMessage(decoder, CardTargetMessage(source, retarget, cancel: true)));
        False(invalidCancel.IsSuccess);
        Equal(beforeInvalidCancel, I6C3Signature(GetI6C3Source(mirror)));

        ApplyMirrorMessage(
            mirror,
            decoder,
            CardTargetMessage(source, target, cancel: true));
        False(GetI6C3Source(mirror).Relationships.Any(relation =>
            relation.Kind == PerspectiveSafeRelationshipKindV1.Target));

        ApplyMirrorMessage(mirror, decoder, EquipMessage(source, target));
        True(GetI6C3Source(mirror).Relationships.Any(relation =>
            relation.Kind == PerspectiveSafeRelationshipKindV1.Equip &&
            relation.Target == "p0:MONSTER_ZONE:1"));
        ApplyMirrorMessage(mirror, decoder, EquipMessage(source, retarget));
        PerspectiveSafeI6C3StateSourceV1 retargeted = GetI6C3Source(mirror);
        Equal(1, retargeted.Relationships.Count(relation =>
            relation.Kind == PerspectiveSafeRelationshipKindV1.Equip));
        True(retargeted.Relationships.Any(relation =>
            relation.Kind == PerspectiveSafeRelationshipKindV1.Equip &&
            relation.Source == "p0:MONSTER_ZONE:0" &&
            relation.Target == "p0:MONSTER_ZONE:2"));
        ApplyMirrorMessage(mirror, decoder, UnequipMessage(source));
        False(GetI6C3Source(mirror).Relationships.Any(relation =>
            relation.Kind == PerspectiveSafeRelationshipKindV1.Equip));
    }

    private static void AssertI6C3HiddenOnlyPrivacy()
    {
        Equal(
            I6C3Signature(GetI6C3Source(CreateHiddenWorld(0xA100))),
            I6C3Signature(GetI6C3Source(CreateHiddenWorld(0xB100))));
    }

    private static void AssertI6C3RelationOrderingAndPrivacy()
    {
        Equal(
            I6C3Signature(GetI6C3Source(CreateRelationOrderMirror(reverse: false))),
            I6C3Signature(GetI6C3Source(CreateRelationOrderMirror(reverse: true))));

        Equal(
            I6C3Signature(GetI6C3Source(CreateHiddenTargetWorld(0xAAAA))),
            I6C3Signature(GetI6C3Source(CreateHiddenTargetWorld(0xBBBB))));
        Equal(
            I6C3Signature(GetI6C3Source(CreateOverlayWorld(consumeEntityId: false))),
            I6C3Signature(GetI6C3Source(CreateOverlayWorld(consumeEntityId: true))));
    }

    private static void AssertI6C3UnresolvedPublicEndpoint()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        ModernLocInfoV1 ownHand = new(0, 0x02, 0, 0x08);
        ModernLocInfoV1 target = new(0, 0x04, 1, 0x05);
        ApplyMirrorMessage(mirror, decoder, MoveMessage(0, empty, ownHand, 0));
        ApplyMirrorMessage(mirror, decoder, MoveMessage(0x7400, empty, target, 0));
        ApplyMirrorMessage(mirror, decoder, CardTargetMessage(ownHand, target));

        PerspectiveSafeI6C3SourceResultV1 result =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C3(mirror);
        False(result.IsSuccess);
        Null(result.Source);
        NotNull(result.Error);
        Equal(
            PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
            result.Error!.Value.Code);
        Equal(
            PerspectiveSafeSourceSectionV1.Relationships,
            result.Error.Value.Section);
    }

    private static void AssertI6C3ChainTargetSourceSeparation()
    {
        (PerspectiveStateMirrorV1 becomeOnlyMirror, GameplayMessageDecoderV1 becomeOnlyDecoder) =
            CreateMirror(0);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        ModernLocInfoV1 source = new(0, 0x04, 0, 0x05);
        ModernLocInfoV1 target = new(0, 0x04, 1, 0x05);
        ApplyMirrorMessage(
            becomeOnlyMirror,
            becomeOnlyDecoder,
            MoveMessage(0x7500, empty, source, 0));
        ApplyMirrorMessage(
            becomeOnlyMirror,
            becomeOnlyDecoder,
            MoveMessage(0x7501, empty, target, 0));
        ApplyMirrorMessage(
            becomeOnlyMirror,
            becomeOnlyDecoder,
            ChainingMessage(source, 1, 0x7500));
        ApplyMirrorMessage(
            becomeOnlyMirror,
            becomeOnlyDecoder,
            BecomeTargetMessage(target));
        PerspectiveSafeI6C3StateSourceV1 becomeOnly = GetI6C3Source(becomeOnlyMirror);
        False(becomeOnly.Relationships.Any(relation =>
            relation.Kind == PerspectiveSafeRelationshipKindV1.Target));
        Equal(0, becomeOnly.Chain.Links[0].Targets.Count);

        ApplyMirrorMessage(
            becomeOnlyMirror,
            becomeOnlyDecoder,
            CardTargetMessage(source, target));
        PerspectiveSafeI6C3StateSourceV1 combined = GetI6C3Source(becomeOnlyMirror);
        Equal(1, combined.Relationships.Count(relation =>
            relation.Kind == PerspectiveSafeRelationshipKindV1.Target));
        Equal(1, combined.Chain.Links[0].Targets.Count);
        Equal("p0:MONSTER_ZONE:1", combined.Chain.Links[0].Targets[0]);

        (PerspectiveStateMirrorV1 cardTargetOnlyMirror, GameplayMessageDecoderV1 cardTargetOnlyDecoder) =
            CreateMirror(0);
        ApplyMirrorMessage(
            cardTargetOnlyMirror,
            cardTargetOnlyDecoder,
            MoveMessage(0x7600, empty, source, 0));
        ApplyMirrorMessage(
            cardTargetOnlyMirror,
            cardTargetOnlyDecoder,
            MoveMessage(0x7601, empty, target, 0));
        ApplyMirrorMessage(
            cardTargetOnlyMirror,
            cardTargetOnlyDecoder,
            ChainingMessage(source, 1, 0x7600));
        ApplyMirrorMessage(
            cardTargetOnlyMirror,
            cardTargetOnlyDecoder,
            CardTargetMessage(source, target));
        PerspectiveSafeI6C3StateSourceV1 cardTargetOnly =
            GetI6C3Source(cardTargetOnlyMirror);
        Equal(1, cardTargetOnly.Relationships.Count(relation =>
            relation.Kind == PerspectiveSafeRelationshipKindV1.Target));
        Equal(1, cardTargetOnly.Chain.Links[0].Targets.Count);
        Equal("p0:MONSTER_ZONE:1", cardTargetOnly.Chain.Links[0].Targets[0]);
    }

    private static void AssertI6C3VisibleOpponentOverlayIdentity()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        ModernLocInfoV1 parent = new(1, 0x04, 2, 0x05);
        ModernLocInfoV1 material = new(1, 0x84, 2, 0);
        ApplyMirrorMessage(mirror, decoder, MoveMessage(0x7700, empty, parent, 0));
        ApplyMirrorMessage(mirror, decoder, MoveMessage(0x7701, empty, material, 0));

        PerspectiveSafeEntityV1 projected = FindI6C3Entity(
            GetI6C3Source(mirror),
            "p1:OVERLAY:2:0");
        True(projected.IdentityKnown);
        Equal((uint)0x7701, projected.Passcode!.Value);
        Equal(
            PerspectiveSafeI6C3SourceStatusV1.Blocked,
            GetI6C3Source(mirror).GetStatus(
                PerspectiveSafeI6C3ConstituentV1.OverlayIdentity));
    }

    private static void AssertI6C3OverlayPositionAndFreshProperties()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        ModernLocInfoV1 parent = new(0, 0x04, 2, 0x05);
        ModernLocInfoV1 firstMaterial = new(0, 0x84, 2, 0);
        ModernLocInfoV1 staleFieldCard = new(0, 0x04, 3, 0x05);
        ModernLocInfoV1 secondMaterial = new(0, 0x84, 2, 1);
        ApplyMirrorMessage(mirror, decoder, MoveMessage(0x7800, empty, parent, 0));
        ApplyMirrorMessage(mirror, decoder, MoveMessage(0x7801, empty, firstMaterial, 0));
        ApplyMirrorMessage(mirror, decoder, MoveMessage(0x7802, empty, staleFieldCard, 0));
        ModernQueryV1 staleQuery = DecodeQuery(
            QueryRecord(QueryFlagV1.Attack, I32(123)),
            QueryEnd());
        ApplyMirrorMessage(
            mirror,
            decoder,
            UpdateCardMessage(0, 0x04, 3, staleQuery));
        ApplyMirrorMessage(
            mirror,
            decoder,
            MoveMessage(0x7802, staleFieldCard, secondMaterial, 0));

        PerspectiveSafeEntityV1 projected = FindI6C3Entity(
            GetI6C3Source(mirror),
            "p0:OVERLAY:2:1");
        Equal(PerspectiveSafePositionV1.FaceUpAttack, projected.Position);
        True(projected.FaceUp);
        Null(projected.Current);
        Equal(
            PerspectiveSafeI6C3SourceStatusV1.Blocked,
            GetI6C3Source(mirror).GetStatus(
                PerspectiveSafeI6C3ConstituentV1.OverlayCurrentProperties));
        Equal(
            PerspectiveSafeI6C3SourceStatusV1.Blocked,
            GetI6C3Source(mirror).GetStatus(
                PerspectiveSafeI6C3ConstituentV1.OverlayEntities));
    }

    private static void AssertI6C3ChainLifecycle()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        ModernLocInfoV1 source = new(0, 0x04, 0, 0x05);
        ModernLocInfoV1 secondSource = new(0, 0x04, 1, 0x05);
        ModernLocInfoV1 target = new(0, 0x04, 2, 0x05);
        ModernLocInfoV1 secondTarget = new(0, 0x04, 3, 0x05);
        ApplyMirrorMessage(mirror, decoder, MoveMessage(0x1000, empty, source, 0));
        ApplyMirrorMessage(mirror, decoder, MoveMessage(0x2000, empty, secondSource, 0));
        ApplyMirrorMessage(mirror, decoder, MoveMessage(0x3000, empty, target, 0));
        ApplyMirrorMessage(mirror, decoder, MoveMessage(0x3001, empty, secondTarget, 0));

        ApplyMirrorMessage(mirror, decoder, ChainingMessage(source, 1, 0x1000));
        True(mirror.Snapshot.PendingChainSource is not null);
        Equal((byte)0, mirror.Snapshot.PendingChainSource!.TriggeringController);
        Equal((byte)0x04, mirror.Snapshot.PendingChainSource.TriggeringLocation);
        Equal((uint)0, mirror.Snapshot.PendingChainSource.TriggeringSequence);
        PerspectiveSafeI6C3StateSourceV1 pending = GetI6C3Source(mirror);
        Equal((uint)1, pending.Chain.Length);
        Equal((uint)0, pending.Chain.Links[0].Index);
        Equal((byte)0, pending.Chain.Links[0].ActivatingPlayer!.Value);
        Equal("p0:MONSTER_ZONE:0", pending.Chain.Links[0].Source!);
        Equal(PerspectiveSafeSemanticZoneV1.MonsterZone, pending.Chain.Links[0].ActivationZone!.Value);
        Equal((ulong)0x0102030405060708, pending.Chain.Links[0].EffectDescription!.Value);

        ApplyMirrorMessage(
            mirror,
            decoder,
            BecomeTargetMessage(target));
        ApplyMirrorMessage(mirror, decoder, CardTargetMessage(source, target));
        ApplyMirrorMessage(mirror, decoder, CardTargetMessage(source, secondTarget));
        PerspectiveSafeI6C3StateSourceV1 withTarget = GetI6C3Source(mirror);
        Equal(2, withTarget.Chain.Links[0].Targets.Count);
        Equal("p0:MONSTER_ZONE:2", withTarget.Chain.Links[0].Targets[0]);
        Equal("p0:MONSTER_ZONE:3", withTarget.Chain.Links[0].Targets[1]);
        True(withTarget.Relationships.Any(relation =>
            relation.Kind == PerspectiveSafeRelationshipKindV1.Target &&
            relation.Source == "p0:MONSTER_ZONE:0" &&
            relation.Target == "p0:MONSTER_ZONE:2"));
        True(withTarget.Relationships.Any(relation =>
            relation.Kind == PerspectiveSafeRelationshipKindV1.Target &&
            relation.Source == "p0:MONSTER_ZONE:0" &&
            relation.Target == "p0:MONSTER_ZONE:3"));

        ApplyMirrorMessage(mirror, decoder, new byte[] { 71, 1 });
        Equal((byte)0, mirror.Snapshot.Chains[0].TriggeringController);
        string beforeStatus = I6C3Signature(GetI6C3Source(mirror));
        ApplyMirrorMessage(mirror, decoder, new byte[] { 72, 1 });
        ApplyMirrorMessage(mirror, decoder, new byte[] { 73, 1 });
        Equal(beforeStatus, I6C3Signature(GetI6C3Source(mirror)));

        ApplyMirrorMessage(
            mirror,
            decoder,
            ChainingMessage(secondSource, 2, 0x2000));
        PerspectiveSafeI6C3StateSourceV1 twoLinks = GetI6C3Source(mirror);
        Equal((uint)2, twoLinks.Chain.Length);
        Equal((uint)0, twoLinks.Chain.Links[0].Index);
        Equal((uint)1, twoLinks.Chain.Links[1].Index);
        Equal("p0:MONSTER_ZONE:1", twoLinks.Chain.Links[1].Source!);

        ApplyMirrorMessage(mirror, decoder, new byte[] { 71, 2 });
        ApplyMirrorMessage(
            mirror,
            decoder,
            CardTargetMessage(source, target, cancel: true));
        ApplyMirrorMessage(
            mirror,
            decoder,
            CardTargetMessage(source, secondTarget, cancel: true));
        ApplyMirrorMessage(mirror, decoder, new byte[] { 74 });
        PerspectiveSafeI6C3StateSourceV1 ended = GetI6C3Source(mirror);
        Equal((uint)0, ended.Chain.Length);
        Equal(0, ended.Chain.Links.Count);
        False(ended.Relationships.Any(relation =>
            relation.Kind == PerspectiveSafeRelationshipKindV1.Target));

        (PerspectiveStateMirrorV1 playerOneMirror, GameplayMessageDecoderV1 playerOneDecoder) =
            CreateMirror(1);
        ModernLocInfoV1 playerOneSource = new(1, 0x04, 0, 0x05);
        ApplyMirrorMessage(
            playerOneMirror,
            playerOneDecoder,
            MoveMessage(0x4000, empty, playerOneSource, 0));
        ApplyMirrorMessage(
            playerOneMirror,
            playerOneDecoder,
            ChainingMessage(playerOneSource, 1, 0x4000));
        Equal((byte)1, GetI6C3Source(playerOneMirror).Chain.Links[0].ActivatingPlayer!.Value);
    }

    private static void AssertI6C3SzoneChainBoundary()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        ModernLocInfoV1 source = new(0, 0x08, 0, 0x05);
        ApplyMirrorMessage(mirror, decoder, MoveMessage(0x5000, empty, source, 0));
        ApplyMirrorMessage(mirror, decoder, ChainingMessage(source, 1, 0x5000));

        PerspectiveSafeI6C3StateSourceV1 result = GetI6C3Source(mirror);
        PerspectiveSafeChainLinkV1 link = result.Chain.Links[0];
        Null(link.Source);
        Null(link.ActivationZone);
        Equal(
            PerspectiveSafeI6C3SourceStatusV1.BlockedPendingI6C5,
            result.GetStatus(PerspectiveSafeI6C3ConstituentV1.ChainActivationZone));
        Equal(
            PerspectiveSafeI6C3SourceStatusV1.BlockedPendingI6C5,
            result.GetStatus(PerspectiveSafeI6C3ConstituentV1.ChainSourceLocator));
    }

    private static void AssertI6C3FailedChainApplyAtomicity()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        ModernLocInfoV1 source = new(0, 0x04, 0, 0x05);
        ApplyMirrorMessage(mirror, decoder, MoveMessage(0x6000, empty, source, 0));
        ApplyMirrorMessage(mirror, decoder, ChainingMessage(source, 1, 0x6000));
        string beforeMirror = mirror.Snapshot.ToDeterministicString();
        string beforeSource = I6C3Signature(GetI6C3Source(mirror));
        MirrorApplyResult invalid = mirror.Apply(
            DecodeMessage(decoder, new byte[] { 71, 2 }));
        False(invalid.IsSuccess);
        Equal(GameplayErrorCode.InvalidChainState, invalid.Error);
        Equal(beforeMirror, mirror.Snapshot.ToDeterministicString());
        Equal(beforeSource, I6C3Signature(GetI6C3Source(mirror)));
    }

    private static void AssertI6C3TransportChunking()
    {
        byte[][] whole = BuildI6C3TranscriptChunks(new[] { 4096 });
        byte[][] fragmented = BuildI6C3TranscriptChunks(new[] { 1, 2, 5, 3, 7 });
        Equal(
            RunChunkedI6C3Source(whole),
            RunChunkedI6C3Source(fragmented));
    }

    private static byte[][] BuildI6C3TranscriptChunks(int[] sizes)
    {
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        ModernLocInfoV1 source = new(0, 0x04, 0, 0x05);
        ModernLocInfoV1 target = new(0, 0x04, 1, 0x05);
        byte[] stream = Join(
            WireFrameCodec.EncodeStoc(
                StocPacketType.GameMsg,
                CreateStartBytes(0)),
            WireFrameCodec.EncodeStoc(
                StocPacketType.GameMsg,
                MoveMessage(0x8000, empty, source, 0)),
            WireFrameCodec.EncodeStoc(
                StocPacketType.GameMsg,
                MoveMessage(0x8001, empty, target, 0)),
            WireFrameCodec.EncodeStoc(
                StocPacketType.GameMsg,
                ChainingMessage(source, 1, 0x8000)),
            WireFrameCodec.EncodeStoc(
                StocPacketType.GameMsg,
                BecomeTargetMessage(target)));
        return Split(stream, sizes);
    }

    private static string RunChunkedI6C3Source(byte[][] chunks)
    {
        TestTransport transport = new(chunks);
        GameplayHandoffAcquireResult acquired =
            GameplayHandoffConsumerV1.TryCreate(
                CreateHandoff(transport, Array.Empty<byte>()));
        True(acquired.IsSuccess);
        GameplayPumpResult start = acquired.Consumer!.PumpAsync(
            CancellationToken.None).GetAwaiter().GetResult();
        True(start.IsSuccess, start.Error.ToString());
        MirrorCreateResult created = PerspectiveStateMirrorV1.TryCreate(
            start.Message!,
            start.Perspective!);
        True(created.IsSuccess, created.Error.ToString());
        GameplayMirrorSessionV1 session = new(start.Session!, created.Mirror!);
        for (int index = 0; index < 4; index++)
        {
            GameplayMirrorPumpResult step = session.PumpAsync(
                CancellationToken.None).GetAwaiter().GetResult();
            True(step.IsSuccess, step.Error.ToString());
        }

        string signature = I6C3Signature(GetI6C3Source(session.Mirror));
        session.DisposeAsync().GetAwaiter().GetResult();
        acquired.Consumer.DisposeAsync().GetAwaiter().GetResult();
        return signature;
    }

    private static byte[][] BuildI6C2TranscriptChunks(int[] sizes)
    {
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        byte[] stream = Join(
            WireFrameCodec.EncodeStoc(
                StocPacketType.GameMsg,
                CreateStartBytes(0)),
            WireFrameCodec.EncodeStoc(
                StocPacketType.GameMsg,
                MoveMessage(
                    0x11223344,
                    empty,
                    new ModernLocInfoV1(0, 0x04, 0, 0x04),
                    0)),
            WireFrameCodec.EncodeStoc(
                StocPacketType.GameMsg,
                new byte[] { 40, 0 }),
            WireFrameCodec.EncodeStoc(
                StocPacketType.GameMsg,
                new byte[] { 41, 4, 0 }));
        return Split(stream, sizes);
    }

    private static string RunChunkedI6C2Source(byte[][] chunks)
    {
        TestTransport transport = new(chunks);
        GameplayHandoffAcquireResult acquired =
            GameplayHandoffConsumerV1.TryCreate(
                CreateHandoff(transport, Array.Empty<byte>()));
        True(acquired.IsSuccess);
        GameplayPumpResult start = acquired.Consumer!.PumpAsync(
            CancellationToken.None).GetAwaiter().GetResult();
        True(start.IsSuccess, start.Error.ToString());
        MirrorCreateResult created = PerspectiveStateMirrorV1.TryCreate(
            start.Message!,
            start.Perspective!);
        True(created.IsSuccess, created.Error.ToString());
        GameplayMirrorSessionV1 session = new(start.Session!, created.Mirror!);
        True(session.PumpAsync(CancellationToken.None).GetAwaiter().GetResult().IsSuccess);
        True(session.PumpAsync(CancellationToken.None).GetAwaiter().GetResult().IsSuccess);
        True(session.PumpAsync(CancellationToken.None).GetAwaiter().GetResult().IsSuccess);
        string signature = I6C2Signature(GetI6C2Source(session.Mirror));
        session.DisposeAsync().GetAwaiter().GetResult();
        acquired.Consumer.DisposeAsync().GetAwaiter().GetResult();
        return signature;
    }

    private static PerspectiveStateMirrorV1 CreateHiddenWorld(
        uint hiddenCode,
        ushort extraCount0 = 1)
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(
                0,
                extraCount0: extraCount0,
                deckCount1: 2,
                extraCount1: 1);
        ApplyMirrorMessage(
            mirror,
            decoder,
            DrawMessage(1, (hiddenCode, 0x08u)));
        ApplyMirrorMessage(
            mirror,
            decoder,
            MoveMessage(
                hiddenCode + 1,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(1, 0x40, 0, 0x08),
                0));
        ApplyMirrorMessage(
            mirror,
            decoder,
            MoveMessage(
                hiddenCode + 2,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(1, 0x01, 0, 0x08),
                0));
        return mirror;
    }

    private static PerspectiveSafeI6C2StateSourceV1 GetI6C2Source(
        PerspectiveStateMirrorV1 mirror)
    {
        PerspectiveSafeI6C2SourceResultV1 result =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C2(mirror);
        True(result.IsSuccess, result.Error?.ToString() ?? "I6C2 source failed");
        NotNull(result.Source);
        Null(result.Error);
        return result.Source!;
    }

    private static PerspectiveSafeI6C3StateSourceV1 GetI6C3Source(
        PerspectiveStateMirrorV1 mirror)
    {
        PerspectiveSafeI6C3SourceResultV1 result =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C3(mirror);
        True(result.IsSuccess, result.Error?.ToString() ?? "I6C3 source failed");
        NotNull(result.Source);
        Null(result.Error);
        return result.Source!;
    }

    private static MirrorApplyResult ApplyMirrorMessage(
        PerspectiveStateMirrorV1 mirror,
        GameplayMessageDecoderV1 decoder,
        byte[] bytes)
    {
        MirrorApplyResult result = mirror.Apply(DecodeMessage(decoder, bytes));
        True(result.IsSuccess, result.Error.ToString());
        return result;
    }

    private static PerspectiveSafeZoneV1 FindZone(
        PerspectiveSafeI6C2StateSourceV1 source,
        byte player,
        PerspectiveSafeSemanticZoneV1 kind) =>
        source.Zones.Single(zone => zone.Player == player && zone.Kind == kind);

    private static PerspectiveSafeEntityV1 FindEntity(
        PerspectiveSafeI6C2StateSourceV1 source,
        string locator) =>
        source.Entities.Single(entity => entity.Locator == locator);

    private static PerspectiveSafeZoneV1 FindI6C3Zone(
        PerspectiveSafeI6C3StateSourceV1 source,
        byte player,
        PerspectiveSafeSemanticZoneV1 kind) =>
        source.Zones.Single(zone => zone.Player == player && zone.Kind == kind);

    private static PerspectiveSafeEntityV1 FindI6C3Entity(
        PerspectiveSafeI6C3StateSourceV1 source,
        string locator) =>
        source.Entities.Single(entity => entity.Locator == locator);

    private static string I6C2Signature(
        PerspectiveSafeI6C2StateSourceV1 source)
    {
        List<string> values = new()
        {
            string.Join(",", source.Globals.LifePoints),
            source.Globals.TurnPlayer?.ToString() ?? "absent",
            source.Globals.TurnCount?.ToString() ?? "absent",
            source.Globals.Phase?.ToString() ?? "absent",
            source.Globals.Terminal.ToString(),
            source.Globals.Winner?.ToString() ?? "absent",
            source.Globals.WinReason?.ToString() ?? "absent"
        };
        values.AddRange(source.Zones.Select(zone => string.Join(
            ":",
            zone.Player,
            (byte)zone.Kind,
            zone.TotalCount,
            zone.PublicIdentityCount,
            zone.HiddenCount,
            zone.PlayerObservableOrder)));
        values.AddRange(source.Entities.Select(entity => string.Join(
            ":",
            entity.Locator,
            entity.IdentityKnown,
            entity.Passcode?.ToString() ?? "absent",
            entity.Owner?.ToString() ?? "absent",
            entity.Controller?.ToString() ?? "absent",
            (byte)entity.Zone,
            entity.Sequence?.ToString() ?? "absent",
            (byte)entity.Position,
            entity.FaceUp,
            entity.FaceDown,
            entity.Current?.Type?.ToString() ?? "absent",
            entity.Current?.Attack?.ToString() ?? "absent")));
        return string.Join("|", values);
    }

    private static string I6C3Signature(
        PerspectiveSafeI6C3StateSourceV1 source)
    {
        List<string> values = new()
        {
            I6C2Signature(source.BaseSource),
            string.Join(
                ",",
                source.Zones.Select(zone => string.Join(
                    ":",
                    zone.Player,
                    (byte)zone.Kind,
                    zone.TotalCount,
                    zone.PublicIdentityCount,
                    zone.HiddenCount,
                    zone.PlayerObservableOrder))),
            string.Join(
                ",",
                source.Entities.Select(entity => string.Join(
                    ":",
                    entity.Locator,
                    entity.IdentityKnown,
                    entity.Passcode?.ToString() ?? "absent",
                    entity.Owner?.ToString() ?? "absent",
                    entity.Controller?.ToString() ?? "absent",
                    (byte)entity.Zone,
                    entity.Sequence?.ToString() ?? "absent",
                    entity.OverlaySequence?.ToString() ?? "absent",
                    (byte)entity.Position,
                    entity.FaceUp,
                    entity.FaceDown,
                    DescribeProperties(entity.Current)))),
            string.Join(
                ",",
                source.Relationships.Select(relation => string.Join(
                    ":",
                    (byte)relation.Kind,
                    relation.Source,
                    relation.Target))),
            source.Chain.Length.ToString(),
            string.Join(
                ",",
                source.Chain.Links.Select(link => string.Join(
                    ":",
                    link.Index,
                    link.ActivatingPlayer?.ToString() ?? "absent",
                    link.Source ?? "absent",
                    link.ActivationZone?.ToString() ?? "absent",
                    link.EffectDescription?.ToString() ?? "absent",
                    string.Join("/", link.Targets))))
        };
        return string.Join("|", values);
    }

    private static PerspectiveStateMirrorV1 CreateRelationOrderMirror(bool reverse)
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        ModernLocInfoV1 source = new(0, 0x04, 0, 0x05);
        ModernLocInfoV1 target = new(0, 0x04, 1, 0x05);
        ModernLocInfoV1 secondTarget = new(0, 0x04, 2, 0x05);
        ApplyMirrorMessage(mirror, decoder, MoveMessage(0x7000, empty, source, 0));
        ApplyMirrorMessage(mirror, decoder, MoveMessage(0x7001, empty, target, 0));
        ApplyMirrorMessage(mirror, decoder, MoveMessage(0x7002, empty, secondTarget, 0));
        if (reverse)
        {
            ApplyMirrorMessage(mirror, decoder, EquipMessage(source, secondTarget));
            ApplyMirrorMessage(mirror, decoder, CardTargetMessage(source, target));
        }
        else
        {
            ApplyMirrorMessage(mirror, decoder, CardTargetMessage(source, target));
            ApplyMirrorMessage(mirror, decoder, EquipMessage(source, secondTarget));
        }

        return mirror;
    }

    private static PerspectiveStateMirrorV1 CreateHiddenTargetWorld(uint hiddenCode)
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        ModernLocInfoV1 source = new(0, 0x04, 0, 0x05);
        ModernLocInfoV1 hiddenTarget = new(1, 0x04, 1, 0x08);
        ApplyMirrorMessage(mirror, decoder, MoveMessage(0x7100, empty, source, 0));
        ApplyMirrorMessage(mirror, decoder, MoveMessage(hiddenCode, empty, hiddenTarget, 0));
        ApplyMirrorMessage(mirror, decoder, CardTargetMessage(source, hiddenTarget));
        return mirror;
    }

    private static PerspectiveStateMirrorV1 CreateOverlayWorld(bool consumeEntityId)
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        if (consumeEntityId)
        {
            ModernLocInfoV1 temporary = new(0, 0x04, 0, 0x05);
            ApplyMirrorMessage(mirror, decoder, MoveMessage(0x7200, empty, temporary, 0));
            ApplyMirrorMessage(
                mirror,
                decoder,
                MoveMessage(0x7200, temporary, empty, 0));
        }

        ModernLocInfoV1 parent = new(0, 0x04, 2, 0x05);
        ModernLocInfoV1 material = new(0, 0x84, 2, 0);
        ApplyMirrorMessage(mirror, decoder, MoveMessage(0x7300, empty, parent, 0));
        ApplyMirrorMessage(mirror, decoder, MoveMessage(0x7301, empty, material, 0));
        return mirror;
    }

    private static PerspectiveStateMirrorV1 CreateHiddenOverlayWorld(
        uint parentCode,
        uint materialCode)
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        ModernLocInfoV1 parent = new(1, 0x04, 2, 0x08);
        ModernLocInfoV1 material = new(1, 0x84, 2, 0);
        ApplyMirrorMessage(mirror, decoder, MoveMessage(parentCode, empty, parent, 0));
        ApplyMirrorMessage(mirror, decoder, MoveMessage(materialCode, empty, material, 0));
        return mirror;
    }

    private static PerspectiveSafeFrameV1 Accept(
        PerspectiveSafeFrameSourceInputV1 input)
    {
        PerspectiveSafeFrameSourceResultV1 result =
            PerspectiveSafePublicFrameSourceV1.TryCreate(input);
        True(result.IsSuccess, result.Error?.ToString() ?? "source value rejected");
        NotNull(result.Frame);
        Null(result.Error);
        return result.Frame!;
    }

    private static void AssertFailure(
        PerspectiveSafeFrameSourceInputV1 input,
        PerspectiveSafeFrameSourceErrorCodeV1 expectedCode)
    {
        PerspectiveSafeFrameSourceResultV1 result =
            PerspectiveSafePublicFrameSourceV1.TryCreate(input);
        False(result.IsSuccess);
        Null(result.Frame);
        NotNull(result.Error);
        Equal(expectedCode, result.Error!.Value.Code);
    }

    private static void AssertReadOnly<T>(IReadOnlyList<T> values)
    {
        True(values is IList<T>);
        IList<T> list = (IList<T>)values;
        bool rejected = false;
        try
        {
            list.Add(default!);
        }
        catch (NotSupportedException)
        {
            rejected = true;
        }

        True(rejected, "collection accepted mutation");
    }

    private static void AssertNoForbiddenType(Type type, IEnumerable<string> forbidden)
    {
        string name = type.FullName ?? type.Name;
        AssertDoesNotContainForbidden(name, forbidden);
        if (type.IsGenericType)
        {
            foreach (Type argument in type.GetGenericArguments())
            {
                AssertNoForbiddenType(argument, forbidden);
            }
        }
    }

    private static PerspectiveSafeFrameSourceInputV1 CreateValidInput() =>
        CreateInput(
            CreateValidGlobals(),
            CreateValidZones(),
            CreateValidEntities(),
            CreateValidRelationships(),
            CreateValidChain(),
            CreateValidEvents(),
            CreateValidMatchContext());

    private static PerspectiveSafeFrameSourceInputV1 CreateInput(
        PerspectiveSafeGlobalsV1? globals,
        IEnumerable<PerspectiveSafeZoneV1>? zones,
        IEnumerable<PerspectiveSafeEntityV1>? entities,
        IEnumerable<PerspectiveSafeRelationshipV1>? relationships,
        PerspectiveSafeChainStateV1? chain,
        IEnumerable<PerspectiveSafeVisibleEventV1>? events,
        PerspectiveSafeMatchContextV1? matchContext) =>
        new(
            globals,
            zones,
            entities,
            relationships,
            chain,
            events,
            matchContext);

    private static PerspectiveSafeGlobalsV1 CreateValidGlobals() =>
        new(
            duelFlags: 0x1234,
            lifePoints: new uint[] { 8000, 7000 },
            turnPlayer: 0,
            turnCount: 3,
            phase: 4,
            chainLength: 1,
            terminal: false);

    private static IReadOnlyList<PerspectiveSafeZoneV1> CreateValidZones() =>
        new[]
        {
            new PerspectiveSafeZoneV1(
                0,
                PerspectiveSafeSemanticZoneV1.MainDeck,
                2,
                0,
                2,
                false),
            new PerspectiveSafeZoneV1(
                0,
                PerspectiveSafeSemanticZoneV1.Hand,
                1,
                1,
                0,
                true)
        };

    private static IReadOnlyList<PerspectiveSafeEntityV1> CreateValidEntities()
    {
        PerspectiveSafeCardPropertiesV1 properties =
            new(
                type: 1,
                attribute: 2,
                race: 4,
                attack: 1000,
                defense: 1200,
                baseAttack: 1000,
                baseDefense: 1200,
                level: 4,
                linkMarkers: new[]
                {
                    PerspectiveSafeLinkMarkerV1.Bottom,
                    PerspectiveSafeLinkMarkerV1.Top
                },
                counters: new[]
                {
                    new PerspectiveSafeCounterV1(1, 0),
                    new PerspectiveSafeCounterV1(2, 1)
                });
        return new[]
        {
            new PerspectiveSafeEntityV1(
                "entity-a",
                identityKnown: true,
                passcode: 7,
                owner: 0,
                controller: 0,
                zone: PerspectiveSafeSemanticZoneV1.Hand,
                sequence: 0,
                overlaySequence: null,
                position: PerspectiveSafePositionV1.Unknown,
                faceUp: false,
                faceDown: false,
                printed: properties,
                current: properties),
            new PerspectiveSafeEntityV1(
                "entity-b",
                identityKnown: false,
                passcode: null,
                owner: null,
                controller: 0,
                zone: PerspectiveSafeSemanticZoneV1.MonsterZone,
                sequence: 1,
                overlaySequence: null,
                position: PerspectiveSafePositionV1.FaceDownDefense,
                faceUp: false,
                faceDown: true)
        };
    }

    private static IReadOnlyList<PerspectiveSafeRelationshipV1>
        CreateValidRelationships() =>
        new[]
        {
            new PerspectiveSafeRelationshipV1(
                PerspectiveSafeRelationshipKindV1.Target,
                "entity-a",
                "entity-b")
        };

    private static PerspectiveSafeChainStateV1 CreateValidChain() =>
        new(
            1,
            new[]
            {
                new PerspectiveSafeChainLinkV1(
                    index: 0,
                    activatingPlayer: 0,
                    source: "entity-a",
                    activationZone: PerspectiveSafeSemanticZoneV1.Hand,
                    effectDescription: 42,
                    targets: new[] { "entity-a", "entity-b" })
            });

    private static IReadOnlyList<PerspectiveSafeVisibleEventV1> CreateValidEvents() =>
        new[]
        {
            new PerspectiveSafeVisibleEventV1(
                0,
                PerspectiveSafeVisibleEventKindV1.TurnStarted,
                player: 0,
                phase: 1),
            new PerspectiveSafeVisibleEventV1(
                1,
                PerspectiveSafeVisibleEventKindV1.CardMoved,
                entityLocator: "entity-a",
                fromZone: PerspectiveSafeSemanticZoneV1.Hand,
                toZone: PerspectiveSafeSemanticZoneV1.MonsterZone)
        };

    private static PerspectiveSafeMatchContextV1 CreateValidMatchContext() =>
        new(
            perspectivePlayer: 0,
            duelFlags: 0x1234,
            knowledge: new(true, false),
            ownDeck: new(
                known: true,
                mainDeck: new uint[] { 1, 2 },
                extraDeck: new uint[] { 3 }),
            opponentDeck: new(known: false));

    private static PerspectiveSafeMatchContextV1 CreateValidI6C5MatchContext() =>
        new(
            perspectivePlayer: 0,
            duelFlags: 0x234,
            knowledge: new(true, false),
            ownDeck: new(
                known: true,
                mainDeck: new uint[] { 1, 2 },
                extraDeck: new uint[] { 3 }),
            opponentDeck: new(known: false));

    private static string FrameSignature(PerspectiveSafeFrameV1 frame)
    {
        List<string> parts = new();
        parts.Add(frame.Globals.DuelFlags.ToString());
        parts.Add(string.Join(",", frame.Globals.LifePoints));
        parts.Add(frame.Globals.PlayerToAct?.ToString() ?? "absent");
        parts.Add(frame.Globals.TurnPlayer?.ToString() ?? "absent");
        parts.Add(frame.Globals.TurnCount?.ToString() ?? "absent");
        parts.Add(frame.Globals.Phase?.ToString() ?? "absent");
        parts.Add(frame.Globals.ChainLength.ToString());
        parts.Add(frame.Globals.Winner?.ToString() ?? "absent");
        parts.Add(frame.Globals.WinReason?.ToString() ?? "absent");
        parts.Add(frame.Globals.Terminal.ToString());
        foreach (PerspectiveSafeZoneV1 zone in frame.Zones)
        {
            parts.Add(
                string.Join(
                    ":",
                    zone.Player,
                    (byte)zone.Kind,
                    zone.TotalCount,
                    zone.PublicIdentityCount,
                    zone.HiddenCount,
                    zone.PlayerObservableOrder));
        }

        foreach (PerspectiveSafeEntityV1 entity in frame.Entities)
        {
            parts.Add(
                string.Join(
                    ":",
                    entity.Locator,
                    entity.IdentityKnown,
                    entity.Passcode?.ToString() ?? "absent",
                    entity.Owner?.ToString() ?? "absent",
                    entity.Controller?.ToString() ?? "absent",
                    (byte)entity.Zone,
                    entity.Sequence?.ToString() ?? "absent",
                    entity.OverlaySequence?.ToString() ?? "absent",
                    (byte)entity.Position,
                    entity.FaceUp,
                    entity.FaceDown,
                    DescribeProperties(entity.Printed),
                    DescribeProperties(entity.Current)));
        }

        foreach (PerspectiveSafeRelationshipV1 relationship in frame.Relationships)
        {
            parts.Add(
                string.Join(
                    ":",
                    (byte)relationship.Kind,
                    relationship.Source,
                    relationship.Target));
        }

        parts.Add(frame.Chain.Length.ToString());
        foreach (PerspectiveSafeChainLinkV1 link in frame.Chain.Links)
        {
            parts.Add(
                string.Join(
                    ":",
                    link.Index,
                    link.ActivatingPlayer?.ToString() ?? "absent",
                    link.Source ?? "absent",
                    link.ActivationZone?.ToString() ?? "absent",
                    link.EffectDescription?.ToString() ?? "absent",
                    string.Join(",", link.Targets)));
        }

        foreach (PerspectiveSafeVisibleEventV1 visibleEvent in frame.VisibleEvents)
        {
            parts.Add(
                string.Join(
                    ":",
                    visibleEvent.EventIndex,
                    (byte)visibleEvent.Kind,
                    visibleEvent.Player?.ToString() ?? "absent",
                    visibleEvent.EntityLocator ?? "absent",
                    visibleEvent.PublicPasscode?.ToString() ?? "absent",
                    visibleEvent.FromZone?.ToString() ?? "absent",
                    visibleEvent.ToZone?.ToString() ?? "absent",
                    visibleEvent.Count?.ToString() ?? "absent",
                    visibleEvent.Amount?.ToString() ?? "absent",
                    visibleEvent.CounterType?.ToString() ?? "absent",
                    visibleEvent.Phase?.ToString() ?? "absent",
                    visibleEvent.Winner?.ToString() ?? "absent",
                    visibleEvent.WinReason?.ToString() ?? "absent",
                    visibleEvent.EffectDescription?.ToString() ?? "absent",
                    string.Join(",", visibleEvent.Targets)));
        }

        parts.Add(frame.MatchContext.PerspectivePlayer.ToString());
        parts.Add(frame.MatchContext.DuelFlags.ToString());
        parts.Add(frame.MatchContext.Knowledge.ToString());
        parts.Add(frame.MatchContext.OwnDeck.Known.ToString());
        parts.Add(string.Join(",", frame.MatchContext.OwnDeck.MainDeck));
        parts.Add(string.Join(",", frame.MatchContext.OwnDeck.ExtraDeck));
        parts.Add(frame.MatchContext.OpponentDeck.Known.ToString());
        parts.Add(string.Join(",", frame.MatchContext.OpponentDeck.MainDeck));
        parts.Add(string.Join(",", frame.MatchContext.OpponentDeck.ExtraDeck));
        return string.Join("|", parts);
    }

    private static string DescribeProperties(
        PerspectiveSafeCardPropertiesV1? properties)
    {
        if (properties is null)
        {
            return "absent";
        }

        return string.Join(
            ":",
            properties.Type?.ToString() ?? "absent",
            properties.Attribute?.ToString() ?? "absent",
            properties.Race?.ToString() ?? "absent",
            properties.Attack?.ToString() ?? "absent",
            properties.Defense?.ToString() ?? "absent",
            properties.BaseAttack?.ToString() ?? "absent",
            properties.BaseDefense?.ToString() ?? "absent",
            properties.Level?.ToString() ?? "absent",
            properties.Rank?.ToString() ?? "absent",
            properties.LinkRating?.ToString() ?? "absent",
            string.Join(",", properties.LinkMarkers),
            properties.LeftScale?.ToString() ?? "absent",
            properties.RightScale?.ToString() ?? "absent",
            properties.StatusFlags?.ToString() ?? "absent",
            string.Join(",", properties.Counters));
    }

    private static GameplayMirrorPumpResult ApplyI6C4ThroughSession(
        GameplayMirrorSessionV1 session,
        TestTransport transport,
        byte[] bytes)
    {
        transport.Enqueue(
            WireFrameCodec.EncodeStoc(
                StocPacketType.GameMsg,
                bytes));
        return session.PumpAsync(CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    private static void ApplyI6C4Success(
        PerspectiveStateMirrorV1 mirror,
        GameplayMessageDecoderV1 decoder,
        byte[] bytes)
    {
        MirrorApplyResult result = mirror.Apply(DecodeMessage(decoder, bytes));
        True(result.IsSuccess, $"message {bytes[0]} failed: {result.Error}");
    }

    private static PerspectiveSafeVisibleEventV1 LastI6C4Event(
        PerspectiveStateMirrorV1 mirror)
    {
        True(mirror.VisibleEvents.Count > 0, "expected at least one visible event");
        return mirror.VisibleEvents[^1];
    }

    private static string I6C4EventSignature(
        PerspectiveStateMirrorV1 mirror) =>
        string.Join(
            "|",
            mirror.VisibleEvents.Select(value => string.Join(
                ":",
                value.EventIndex,
                (byte)value.Kind,
                value.Player?.ToString() ?? "absent",
                value.EntityLocator ?? "absent",
                value.PublicPasscode?.ToString() ?? "absent",
                value.FromZone?.ToString() ?? "absent",
                value.ToZone?.ToString() ?? "absent",
                value.Count?.ToString() ?? "absent",
                value.Amount?.ToString() ?? "absent",
                value.CounterType?.ToString() ?? "absent",
                value.Phase?.ToString() ?? "absent",
                value.Winner?.ToString() ?? "absent",
                value.WinReason?.ToString() ?? "absent",
                value.EffectDescription?.ToString() ?? "absent",
                string.Join(",", value.Targets))));

    private static byte[] U16(ushort value)
    {
        byte[] result = new byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(result, value);
        return result;
    }

    private static byte[] SummoningMessage(
        byte messageId,
        uint cardCode,
        ModernLocInfoV1 location) =>
        Join(
            new byte[] { messageId },
            U32(cardCode),
            LocInfo(
                location.Controller,
                location.Location,
                location.Sequence,
                location.Position));

    private static byte[] ConfirmMessage(
        byte messageId,
        byte recipient,
        (uint Code, ModernLocInfoV1 Location) card,
        bool extended = false)
    {
        byte[] location = extended
            ? LocInfo(
                card.Location.Controller,
                card.Location.Location,
                card.Location.Sequence,
                card.Location.Position)
            : Join(
                new byte[] { card.Location.Controller, card.Location.Location },
                U32(card.Location.Sequence));
        return Join(
            new byte[] { messageId, recipient },
            U32(1),
            U32(card.Code),
            location);
    }

    private static byte[] ShuffleCodesMessage(
        byte messageId,
        byte player,
        params uint[] cardCodes)
    {
        List<byte[]> parts = new()
        {
            new byte[] { messageId, player },
            U32((uint)cardCodes.Length)
        };
        parts.AddRange(cardCodes.Select(U32));
        return Join(parts.ToArray());
    }

    private static byte[] ShuffleSetCardMessage(
        byte location,
        ModernLocInfoV1[] previous,
        ModernLocInfoV1[] current)
    {
        List<byte[]> parts = new()
        {
            new byte[] { 36, location, (byte)previous.Length }
        };
        parts.AddRange(previous.Select(value => LocInfo(
            value.Controller,
            value.Location,
            value.Sequence,
            value.Position)));
        parts.AddRange(current.Select(value => LocInfo(
            value.Controller,
            value.Location,
            value.Sequence,
            value.Position)));
        return Join(parts.ToArray());
    }

    private static byte[] CounterMessage(
        byte messageId,
        ushort counterType,
        byte controller,
        byte location,
        byte sequence,
        ushort count) =>
        Join(
            new byte[] { messageId },
            U16(counterType),
            new byte[] { controller, location, sequence },
            U16(count));

    private static string RunI6C4TransportTranscript(byte[][] chunks)
    {
        TestTransport transport = new(chunks);
        GameplayHandoffAcquireResult acquired =
            GameplayHandoffConsumerV1.TryCreate(
                CreateHandoff(transport, Array.Empty<byte>()));
        True(acquired.IsSuccess);
        GameplayPumpResult start = acquired.Consumer!.PumpAsync(
            CancellationToken.None).GetAwaiter().GetResult();
        True(start.IsSuccess, start.Error.ToString());
        MirrorCreateResult created = PerspectiveStateMirrorV1.TryCreate(
            start.Message!,
            start.Perspective!);
        True(created.IsSuccess, created.Error.ToString());

        GameplayMirrorSessionV1 session = new(start.Session!, created.Mirror!);
        for (int index = 0; index < 3; index++)
        {
            GameplayMirrorPumpResult result = session.PumpAsync(
                CancellationToken.None).GetAwaiter().GetResult();
            True(result.IsSuccess, result.Error.ToString());
        }

        string signature = I6C4EventSignature(session.Mirror) +
                           "|next=" + session.Mirror.NextEventIndex;
        session.DisposeAsync().GetAwaiter().GetResult();
        acquired.Consumer.DisposeAsync().GetAwaiter().GetResult();
        return signature;
    }
}
