using System.Collections;
using System.Reflection;
using OCGForge.Ignis.Client;
using OCGForge.Ignis.Gameplay;
using OCGForge.Ignis.Protocol;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;
using static OCGForge.Ignis.Gameplay.Tests.GameplayMessageFixtures;
using static OCGForge.Ignis.Gameplay.Tests.MirrorFixtures;
using static OCGForge.Ignis.Gameplay.Tests.ModernQueryFixtures;
using static OCGForge.Ignis.Gameplay.Tests.TransportFixtures;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I6CPublicFrameSourceTests
{
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
            typeof(PerspectiveSafeI6C2SourceResultV1)
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

    private static PerspectiveStateMirrorV1 CreateHiddenWorld(uint hiddenCode)
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0, deckCount1: 2, extraCount1: 1);
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
}
