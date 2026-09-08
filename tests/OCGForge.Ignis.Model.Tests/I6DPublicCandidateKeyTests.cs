using System.Buffers.Binary;
using System.Globalization;
using System.Reflection;
using OCGForge.Ignis.Gameplay;
using OCGForge.Ignis.Model;

namespace OCGForge.Ignis.Model.Tests;

internal static class I6DPublicCandidateKeyTests
{
    private const string HiddenCardKey =
        "public_action.v1.000000226f6367666f7267652e7075626c69635f616374696f6e5f6964656e746974792e7631" +
        "000000226f6367666f7267652e7075626c69635f616374696f6e5f6964656e746974792e7631" +
        "0000000e636172645f73656c656374696f6e0001010000001470303a5350454c4c5f545241505f5a4f4e453a3000000001000000030000000000";

    private const string HiddenCardDomainDigest =
        "b35640b35822c76ed165a65f86c6d7ac5520abdf4359482b7608bf125274e1e6";

    private const string NativeRaceDirectKey =
        "public_action.v1.000000226f6367666f7267652e7075626c69635f616374696f6e5f6964656e746974792e7631" +
        "000000226f6367666f7267652e7075626c69635f616374696f6e5f6964656e746974792e7631" +
        "0000000c616e6e6f756e63656d656e74000000000001000000000000000000";

    private const string NativeRaceContinuationKey =
        "public_action.v1.000000226f6367666f7267652e7075626c69635f616374696f6e5f6964656e746974792e7631" +
        "000000226f6367666f7267652e7075626c69635f616374696f6e5f6964656e746974792e7631" +
        "000000047069636b0000000000010000000100000000047069636b";

    private const string NativeIdleKey =
        "public_action.v1.000000226f6367666f7267652e7075626c69635f616374696f6e5f6964656e746974792e7631" +
        "000000226f6367666f7267652e7075626c69635f616374696f6e5f6964656e746974792e7631" +
        "0000000c69646c655f636f6d6d616e64010300000000000000030001000000001170303a4d4f4e535445525f5a4f4e453a3000010000000000000000000000";

    private const string NativeBattleKey =
        "public_action.v1.000000226f6367666f7267652e7075626c69635f616374696f6e5f6964656e746974792e7631" +
        "000000226f6367666f7267652e7075626c69635f616374696f6e5f6964656e746974792e7631" +
        "0000000e626174746c655f636f6d6d616e64010300000000000000020001000000001170303a4d4f4e535445525f5a4f4e453a3000010000000000000000000000";

    private const string NativeUnselectKey =
        "public_action.v1.000000226f6367666f7267652e7075626c69635f616374696f6e5f6964656e746974792e7631" +
        "000000226f6367666f7267652e7075626c69635f616374696f6e5f6964656e746974792e7631" +
        "0000000e636172645f73656c656374696f6e0001010000001470313a5350454c4c5f545241505f5a4f4e453a3000000001000000010000000000";

    private const string NativeCounterKey =
        "public_action.v1.000000226f6367666f7267652e7075626c69635f616374696f6e5f6964656e746974792e7631" +
        "000000226f6367666f7267652e7075626c69635f616374696f6e5f6964656e746974792e7631" +
        "0000000d61737369676e5f616d6f756e740001000000001170303a4d4f4e535445525f5a4f4e453a300000000100000000010000000200000006616d6f756e74";

    private const string NativePlaceKey =
        "public_action.v1.000000226f6367666f7267652e7075626c69635f616374696f6e5f6964656e746974792e7631" +
        "000000226f6367666f7267652e7075626c69635f616374696f6e5f6964656e746974792e7631" +
        "00000005706c616365000000000001000000000000000000";

    private const string NativePlacePickKey =
        "public_action.v1.000000226f6367666f7267652e7075626c69635f616374696f6e5f6964656e746974792e7631" +
        "000000226f6367666f7267652e7075626c69635f616374696f6e5f6964656e746974792e7631" +
        "000000047069636b0000000000010000001a00000000047069636b";

    public static void TestPublicCandidateActionKeyBridge()
    {
        OcgForgePublicActionIdentityResultV1 identity =
            OcgForgePublicActionIdentityV1.TryCreate(
                new OcgForgePublicActionDescriptorV1(
                    "card_selection",
                    null,
                    new OcgForgePublicCardReferenceV1(
                        OcgForgePublicCardReferenceKindV1.RedactedSlot,
                        "p0:SPELL_TRAP_ZONE:0"),
                    null,
                    null,
                    null,
                    3,
                    null,
                    string.Empty));

        Require(identity.IsSuccess, identity.ErrorCode.ToString());
        Require(identity.PublicActionKey == HiddenCardKey,
            "V1 public action key differs from the independent native KAT");
        Require(identity.CanonicalDescriptorBytes is not null,
            "successful identity must expose canonical descriptor bytes");
        Require(OcgForgePublicActionIdentityV1.IsCanonicalPublicActionKey(
                identity.PublicActionKey!),
            "generated public action key must validate canonically");

        FlatPromptProjectionResultV1 projection =
            new FlatPromptSessionV1().TryAcceptPrompt(CreateYesNoMessage());
        Require(projection.IsSuccess &&
                projection.Context is not null &&
                projection.Candidates is not null,
            "public YESNO projection must be accepted");

        PerspectiveSafeFrameV1 frame = CreateEmptyFrame();
        OcgForgePublicDecisionContextResultV1 mapped =
            TryMap(
                frame,
                projection,
                decisionIndex: 17);

        Require(mapped.IsSuccess, mapped.Error?.ToString() ?? "mapping failed");
        Require(mapped.Context is not null, "accepted mapping must contain context");
        OcgForgePublicDecisionContextV1 context = mapped.Context!;
        IReadOnlyList<FlatPublicCandidateDescriptorV1> projectedCandidates =
            projection.Candidates!;
        Require(context.PlayerToAct == 0,
            "player_to_act must come from the accepted decision actor");
        Require(context.DecisionIndex == 17 &&
                context.GlobalsPlayerToAct == 0 &&
                context.PublicDecisionContext.Kind == "yes_no" &&
                context.PublicDecisionContext.Player == 0 &&
                context.ReferencedEntities.Count == 0,
            "decision-boundary fields must be composed from the accepted decision");
        Require(context.Candidates.Count == projectedCandidates.Count,
            "candidate count must be preserved");
        Require(context.Candidates[0].Descriptor.ActionKind == "yes_no",
            "YESNO action kind must use the OCGForge public token");
        Require(context.Candidates[0].Descriptor.Choice is
                { Kind: OcgForgePublicChoiceKindV1.YesNo, Value: 0 },
            "YESNO choice must be typed as an OCGForge public choice");

        TestPublicGameplayProjectionIsAccepted();
        TestDecisionBoundaryComposition();
        TestPairedPublicFramesProduceSameBridgeOutput();
        TestActionIdentityMatrix();
        TestCandidateFamilyMatrix();
        TestNativeCandidateMappingVectors();
        TestNToNAndFailClosedMapping();
    }

    private static byte[] CreateYesNoMessage()
    {
        byte[] message = new byte[10];
        message[0] = 13;
        message[1] = 0;
        BinaryPrimitives.WriteUInt64LittleEndian(message.AsSpan(2), 42);
        return message;
    }

    private static void TestPublicGameplayProjectionIsAccepted()
    {
        byte[] message = new byte[29];
        message[0] = 15;
        message[1] = 0;
        message[2] = 0;
        BinaryPrimitives.WriteUInt32LittleEndian(message.AsSpan(3, 4), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(message.AsSpan(7, 4), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(message.AsSpan(11, 4), 1);

        FlatPromptProjectionResultV1 projection =
            new FlatPromptSessionV1().TryAcceptI5Prompt(message);
        Require(projection.IsSuccess &&
                projection.Context is FlatPromptCardSelectionPublicContextV1 &&
                projection.Candidates is not null,
            "public Gameplay card-selection projection must be accepted");
        OcgForgePublicDecisionContextResultV1 mapped =
            TryMap(
                CreateEmptyFrame(),
                projection,
                decisionIndex: 3);
        Require(mapped.IsSuccess, mapped.Error?.ToString() ?? "card-selection mapping failed");
        Require(mapped.Context!.Candidates.Count == 1,
            "one accepted Gameplay occurrence must map to one OCGForge occurrence");
        Require(mapped.Context.Candidates[0].Descriptor.ActionKind == "card_selection",
            "single card-selection occurrence must use direct OCGForge action kind");
    }

    private static void TestPairedPublicFramesProduceSameBridgeOutput()
    {
        PerspectiveSafeFrameV1 frameA = CreatePublicFrame();
        PerspectiveSafeFrameV1 frameB = CreatePublicFrame(
            new[]
            {
                ("p0:HAND:public:12345678:0", true),
                ("p1:SPELL_TRAP_ZONE:0", false),
                ("p0:MONSTER_ZONE:0", true)
            });
        FlatPromptYesNoPublicContextV1 decision =
            New<FlatPromptYesNoPublicContextV1>((byte)0, 42UL);
        FlatYesNoPublicCandidateDescriptorV1 candidate =
            New<FlatYesNoPublicCandidateDescriptorV1>(
                "local.paired-world",
                FlatPromptChoiceKindV1.Yes);

        OcgForgePublicDecisionContextResultV1 resultA =
            TryMap(
                frameA,
                decision,
                new FlatPublicCandidateDescriptorV1[] { candidate },
                decisionIndex: 17);
        OcgForgePublicDecisionContextResultV1 resultB =
            TryMap(
                frameB,
                decision,
                new FlatPublicCandidateDescriptorV1[] { candidate },
                decisionIndex: 17);

        Require(resultA.IsSuccess && resultB.IsSuccess &&
                resultA.Context is not null && resultB.Context is not null,
            "paired public frames must both cross the I6D boundary");
        OcgForgePublicCandidateV1 candidateA = resultA.Context!.Candidates[0];
        OcgForgePublicCandidateV1 candidateB = resultB.Context!.Candidates[0];
        Require(candidateA.PublicActionKey == candidateB.PublicActionKey &&
                candidateA.CanonicalDescriptorBytes.SequenceEqual(
                    candidateB.CanonicalDescriptorBytes) &&
                resultA.Context.PublicCandidateDomainDigest ==
                    resultB.Context.PublicCandidateDomainDigest,
            "paired worlds with equal public frames must have equal I6D identity");
    }

    private static void TestDecisionBoundaryComposition()
    {
        PerspectiveSafeFrameV1 frame = CreatePublicFrame();
        PublicSemanticLocatorV1 visible = Locator("p0:MONSTER_ZONE:0");
        FlatPromptPublicContextV1 decision =
            New<FlatPromptChainPublicContextV1>((byte)0, (byte)0, true, 0U, 0U);
        FlatPublicCandidateDescriptorV1 candidate =
            New<FlatChainPublicCandidateDescriptorV1>(
                "local.context-reference",
                0,
                visible,
                42UL,
                (byte)0);

        OcgForgePublicDecisionContextResultV1 result = TryMap(
            frame,
            decision,
            new[] { candidate },
            decisionIndex: 22);
        Require(result.IsSuccess && result.Context is not null,
            result.Error?.ToString() ?? "decision-boundary composition failed");

        OcgForgePublicDecisionContextV1 context = result.Context!;
        Require(context.DecisionIndex == 22 &&
                context.PlayerToAct == 0 &&
                context.GlobalsPlayerToAct == 0 &&
                context.PublicDecisionContext.Kind == "chain" &&
                context.PublicDecisionContext.Player == 0 &&
                context.ReferencedEntities.SequenceEqual(
                    new[] { "p0:MONSTER_ZONE:0" },
                    StringComparer.Ordinal),
            "public decision context must include the ordered safe references");
    }

    private static void TestActionIdentityMatrix()
    {
        OcgForgePublicActionIdentityResultV1 no =
            OcgForgePublicActionIdentityV1.TryCreate(
                new OcgForgePublicActionDescriptorV1(
                    "yes_no",
                    new OcgForgePublicChoiceV1(
                        OcgForgePublicChoiceKindV1.YesNo,
                        0,
                        null),
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    string.Empty));
        OcgForgePublicActionIdentityResultV1 yes =
            OcgForgePublicActionIdentityV1.TryCreate(
                no.IsSuccess
                    ? new OcgForgePublicActionDescriptorV1(
                        "yes_no",
                        new OcgForgePublicChoiceV1(
                            OcgForgePublicChoiceKindV1.YesNo,
                            1,
                            null),
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        string.Empty)
                    : null);
        Require(no.IsSuccess && yes.IsSuccess &&
                no.PublicActionKey != yes.PublicActionKey,
            "typed scalar choices must have distinct public identities");

        OcgForgePublicActionIdentityResultV1 optionWithoutSelector =
            OcgForgePublicActionIdentityV1.TryCreate(
                new OcgForgePublicActionDescriptorV1(
                    "option",
                    new OcgForgePublicChoiceV1(
                        OcgForgePublicChoiceKindV1.OptionValue,
                        7,
                        null),
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    string.Empty));
        Require(!optionWithoutSelector.IsSuccess &&
                optionWithoutSelector.ErrorCode ==
                    OcgForgePublicActionIdentityErrorCodeV1.InvalidChoice,
            "option without its response selector must fail closed");

        OcgForgePublicActionIdentityResultV1 signed =
            OcgForgePublicActionIdentityV1.TryCreate(
                new OcgForgePublicActionDescriptorV1(
                    "assign_amount",
                    null,
                    null,
                    null,
                    null,
                    null,
                    2,
                    -1,
                    "amount"));
        Require(signed.IsSuccess && signed.CanonicalDescriptorBytes is not null,
            "signed amount descriptor must be representable");
        byte[] signedBytes = signed.CanonicalDescriptorBytes!;
        Require(signedBytes.Any(value => value == 0xff),
            "negative amount must use its two's-complement byte representation");
        byte originalDescriptorByte = signedBytes[0];
        signedBytes[0] ^= 0xff;
        Require(signed.CanonicalDescriptorBytes![0] == originalDescriptorByte,
            "canonical descriptor bytes must be immutable to callers");

        OcgForgePublicActionIdentityResultV1 targetReference =
            OcgForgePublicActionIdentityV1.TryCreate(
                new OcgForgePublicActionDescriptorV1(
                    "battle_command",
                    null,
                    new OcgForgePublicCardReferenceV1(
                        OcgForgePublicCardReferenceKindV1.VisibleCard,
                        "p0:MONSTER_ZONE:0"),
                    new OcgForgePublicCardReferenceV1(
                        OcgForgePublicCardReferenceKindV1.RedactedSlot,
                        "p1:SPELL_TRAP_ZONE:0"),
                    1,
                    4,
                    0,
                    null,
                    string.Empty));
        Require(targetReference.IsSuccess,
            "optional public references and position must be encodable");

        OcgForgePublicCandidateDomainResultV1 domain =
            OcgForgePublicActionIdentityV1.TryCreateCandidateDomain(
                "yes_no",
                new[] { no.PublicActionKey!, yes.PublicActionKey! });
        OcgForgePublicCandidateDomainResultV1 reversed =
            OcgForgePublicActionIdentityV1.TryCreateCandidateDomain(
                "yes_no",
                new[] { yes.PublicActionKey!, no.PublicActionKey! });
        Require(domain.IsSuccess && reversed.IsSuccess &&
                domain.Digest != reversed.Digest,
            "public candidate-domain identity must bind source order");

        OcgForgePublicCandidateDomainResultV1 nativeDomainVector =
            OcgForgePublicActionIdentityV1.TryCreateCandidateDomain(
                "card_selection",
                new[] { HiddenCardKey });
        Require(nativeDomainVector.IsSuccess &&
                nativeDomainVector.Digest == HiddenCardDomainDigest,
            "candidate-domain digest differs from the independent native KAT");

        OcgForgePublicCandidateDomainResultV1 duplicate =
            OcgForgePublicActionIdentityV1.TryCreateCandidateDomain(
                "yes_no",
                new[] { no.PublicActionKey!, no.PublicActionKey! });
        Require(!duplicate.IsSuccess &&
                duplicate.ErrorCode ==
                    OcgForgePublicCandidateDomainErrorCodeV1.DuplicateActionKey,
            "duplicate public keys must fail closed");
    }

    private static void TestCandidateFamilyMatrix()
    {
        PerspectiveSafeFrameV1 frame = CreatePublicFrame();
        PublicSemanticLocatorV1 visible = Locator("p0:MONSTER_ZONE:0");
        PublicSemanticLocatorV1 hidden = Locator("p1:SPELL_TRAP_ZONE:0");

        AssertMapped(
            frame,
            New<FlatPromptOptionPublicContextV1>((byte)0),
            New<FlatOptionPublicCandidateDescriptorV1>("option.local", 2, 99UL),
            "option",
            null,
            null,
            null,
            null,
            OcgForgePublicChoiceKindV1.OptionValue,
            99UL,
            2);

        AssertMapped(
            frame,
            New<FlatPromptPositionPublicContextV1>((byte)0, (byte)0x0f),
            New<FlatPositionPublicCandidateDescriptorV1>(
                "position.local",
                FlatPromptChoiceKindV1.FaceupDefense,
                (byte)0x04),
            "position",
            null,
            null,
            null,
            null,
            null,
            null,
            position: 0x04);

        AssertMapped(
            frame,
            New<FlatPromptEffectYnPublicContextV1>((byte)0, visible, 42UL),
            New<FlatEffectYnPublicCandidateDescriptorV1>(
                "effect.local",
                FlatPromptChoiceKindV1.Yes),
            "yes_no",
            "p0:MONSTER_ZONE:0",
            null,
            null,
            null,
            OcgForgePublicChoiceKindV1.EffectYesNo,
            1UL,
            null);

        AssertMapped(
            frame,
            New<FlatPromptChainPublicContextV1>((byte)0, (byte)0, false, 0U, 0U),
            New<FlatChainPublicCandidateDescriptorV1>(
                "chain.local",
                1,
                visible,
                42UL,
                (byte)0),
            "chain",
            "p0:MONSTER_ZONE:0",
            0U,
            null,
            null,
            OcgForgePublicChoiceKindV1.EffectChoice,
            1UL,
            null);

        AssertMapped(
            frame,
            New<FlatPromptChainPublicContextV1>((byte)0, (byte)0, false, 0U, 0U),
            New<FlatChainNoChainPublicCandidateDescriptorV1>("chain.pass.local"),
            "chain",
            null,
            1U,
            null,
            null,
            null,
            null,
            null);

        AssertMapped(
            frame,
            New<FlatPromptBattlePublicContextV1>((byte)0),
            New<FlatBattleActivatablePublicCandidateV1>(
                "battle.activate.local",
                2,
                visible,
                42UL,
                (byte)0),
            "battle_command",
            "p0:MONSTER_ZONE:0",
            0U,
            null,
            null,
            OcgForgePublicChoiceKindV1.EffectChoice,
            2UL,
            null);

        AssertMapped(
            frame,
            New<FlatPromptBattlePublicContextV1>((byte)0),
            New<FlatBattleAttackPublicCandidateV1>(
                "battle.attack.local",
                1,
                hidden,
                false),
            "battle_command",
            "p1:SPELL_TRAP_ZONE:0",
            1U,
            null,
            null,
            OcgForgePublicChoiceKindV1.EffectChoice,
            1UL,
            null);

        AssertMapped(
            frame,
            New<FlatPromptBattlePublicContextV1>((byte)0),
            New<FlatBattleToMainPhase2PublicCandidateV1>("battle.m2.local"),
            "battle_command",
            null,
            2U,
            null,
            null,
            null,
            null,
            null);

        AssertMapped(
            frame,
            New<FlatPromptBattlePublicContextV1>((byte)0),
            New<FlatBattleToEndPhasePublicCandidateV1>("battle.ep.local"),
            "battle_command",
            null,
            3U,
            null,
            null,
            null,
            null,
            null);

        AssertMapped(
            frame,
            New<FlatPromptIdlePublicContextV1>((byte)0),
            New<FlatIdleSummonPublicCandidateV1>("idle.summon.local", 3, visible),
            "idle_command",
            "p0:MONSTER_ZONE:0",
            0U,
            null,
            null,
            OcgForgePublicChoiceKindV1.EffectChoice,
            3UL,
            null);
        AssertMapped(
            frame,
            New<FlatPromptIdlePublicContextV1>((byte)0),
            New<FlatIdleSpecialSummonPublicCandidateV1>("idle.special.local", 4, visible),
            "idle_command",
            "p0:MONSTER_ZONE:0",
            1U,
            null,
            null,
            OcgForgePublicChoiceKindV1.EffectChoice,
            4UL,
            null);
        AssertMapped(
            frame,
            New<FlatPromptIdlePublicContextV1>((byte)0),
            New<FlatIdleRepositionPublicCandidateV1>("idle.reposition.local", 1, visible),
            "idle_command",
            "p0:MONSTER_ZONE:0",
            2U,
            null,
            null,
            OcgForgePublicChoiceKindV1.EffectChoice,
            1UL,
            null);
        AssertMapped(
            frame,
            New<FlatPromptIdlePublicContextV1>((byte)0),
            New<FlatIdleMsetPublicCandidateV1>("idle.mset.local", 0, visible),
            "idle_command",
            "p0:MONSTER_ZONE:0",
            3U,
            null,
            null,
            OcgForgePublicChoiceKindV1.EffectChoice,
            0UL,
            null);
        AssertMapped(
            frame,
            New<FlatPromptIdlePublicContextV1>((byte)0),
            New<FlatIdleSsetPublicCandidateV1>("idle.sset.local", 0, visible),
            "idle_command",
            "p0:MONSTER_ZONE:0",
            4U,
            null,
            null,
            OcgForgePublicChoiceKindV1.EffectChoice,
            0UL,
            null);
        AssertMapped(
            frame,
            New<FlatPromptIdlePublicContextV1>((byte)0),
            New<FlatIdleActivatablePublicCandidateV1>(
                "idle.activate.local",
                1,
                visible,
                42UL,
                (byte)0),
            "idle_command",
            "p0:MONSTER_ZONE:0",
            5U,
            null,
            null,
            OcgForgePublicChoiceKindV1.EffectChoice,
            1UL,
            null);
        AssertMapped(
            frame,
            New<FlatPromptIdlePublicContextV1>((byte)0),
            New<FlatIdleToBattlePhasePublicCandidateV1>("idle.bp.local"),
            "idle_command",
            null,
            6U,
            null,
            null,
            null,
            null,
            null);
        AssertMapped(
            frame,
            New<FlatPromptIdlePublicContextV1>((byte)0),
            New<FlatIdleToEndPhasePublicCandidateV1>("idle.ep.local"),
            "idle_command",
            null,
            7U,
            null,
            null,
            null,
            null,
            null);
        AssertMapped(
            frame,
            New<FlatPromptIdlePublicContextV1>((byte)0),
            New<FlatIdleShuffleHandPublicCandidateV1>("idle.shuffle.local"),
            "idle_command",
            null,
            8U,
            null,
            null,
            null,
            null,
            null);

        AssertMapped(
            frame,
            New<FlatPromptCardSelectionPublicContextV1>((byte)0, 1U, 1U, false),
            New<FlatPromptCardSelectionLocatorCandidateV1>(
                "card.direct.local",
                0,
                visible),
            "card_selection",
            "p0:MONSTER_ZONE:0",
            null,
            0U,
            null,
            null,
            null,
            null);
        AssertMapped(
            frame,
            New<FlatPromptCardSelectionPublicContextV1>((byte)0, 1U, 2U, false),
            New<FlatPromptCardSelectionLocatorCandidateV1>(
                "card.continuation.local",
                1,
                hidden),
            "pick",
            "p1:SPELL_TRAP_ZONE:0",
            null,
            1U,
            null,
            null,
            null,
            null,
            "pick");
        AssertMapped(
            frame,
            New<FlatPromptTributeSelectionPublicContextV1>((byte)0, 1U, 2U, false),
            New<FlatPromptTributeSelectionLocatorCandidateV1>(
                "tribute.local",
                0,
                visible),
            "pick",
            "p0:MONSTER_ZONE:0",
            null,
            0U,
            null,
            null,
            null,
            null,
            "pick");
        AssertMapped(
            frame,
            New<FlatPromptSelectUnselectCardPublicContextV1>((byte)0, true, true, 1U, 2U, 1, 1),
            New<FlatPromptSelectUnselectLocatorCandidateV1>(
                "select.local",
                FlatPromptChoiceKindV1.Select,
                FlatPromptSourceSectionV1.Selectable,
                0,
                visible),
            "card_selection",
            "p0:MONSTER_ZONE:0",
            null,
            0U,
            null,
            null,
            null,
            null);
        AssertMapped(
            frame,
            New<FlatPromptSelectUnselectCardPublicContextV1>((byte)0, true, true, 1U, 2U, 1, 1),
            New<FlatPromptSelectUnselectLocatorCandidateV1>(
                "unselect.local",
                FlatPromptChoiceKindV1.Unselect,
                FlatPromptSourceSectionV1.Unselectable,
                0,
                hidden),
            "card_selection",
            "p1:SPELL_TRAP_ZONE:0",
            null,
            1U,
            null,
            null,
            null,
            null);
        AssertMapped(
            frame,
            New<FlatPromptSelectUnselectCardPublicContextV1>((byte)0, true, true, 1U, 2U, 1, 1),
            New<FlatPromptFinishOrCancelPublicCandidateV1>("finish-or-cancel.local"),
            "finish",
            null,
            null,
            null,
            null,
            null,
            null,
            null);
        AssertMapped(
            frame,
            New<FlatPromptAnnounceNumberPublicContextV1>((byte)0, 2),
            New<FlatPromptAnnounceNumberPublicCandidateV1>(
                "number.local",
                1,
                123UL),
            "announcement",
            null,
            null,
            null,
            null,
            OcgForgePublicChoiceKindV1.AnnouncementNumber,
            123UL,
            1);

        FlatPromptFieldPlaceV1[] places =
        {
            New<FlatPromptFieldPlaceV1>((byte)0, FlatPromptFieldZoneV1.MonsterZone, (byte)0)
        };
        FlatPromptFieldPlaceV1[] continuationPlaces =
        {
            places[0],
            New<FlatPromptFieldPlaceV1>(
                (byte)1,
                FlatPromptFieldZoneV1.SpellTrapZone,
                (byte)2)
        };
        AssertMapped(
            frame,
            New<FlatPromptPlaceSelectionPublicContextV1>((byte)0, (byte)1, places),
            New<FlatPromptFieldPlacePublicCandidateV1>(
                "place.local",
                (byte)0,
                FlatPromptFieldZoneV1.MonsterZone,
                (byte)0),
            "place",
            null,
            null,
            0U,
            null,
            null,
            null,
            null);
        AssertMapped(
            frame,
            New<FlatPromptPlaceSelectionPublicContextV1>(
                (byte)0,
                (byte)2,
                continuationPlaces),
            New<FlatPromptFieldPlacePublicCandidateV1>(
                "place.continuation.local",
                (byte)1,
                FlatPromptFieldZoneV1.SpellTrapZone,
                (byte)2),
            "pick",
            null,
            null,
            26U,
            null,
            null,
            null,
            null,
            "pick");
        AssertMapped(
            frame,
            New<FlatPromptRaceSelectionPublicContextV1>((byte)0, (byte)1, 1UL),
            New<FlatPromptMaskBitPublicCandidateV1>("race.local", 0, 1UL),
            "announcement",
            sourceLocator: null,
            phase: null,
            sourceIndex: 0U,
            amount: null,
            choiceKind: null,
            choiceValue: null,
            responseIndex: null,
            continuationOperation: "");
        AssertMapped(
            frame,
            New<FlatPromptAttributeSelectionPublicContextV1>((byte)0, (byte)1, 1U),
            New<FlatPromptMaskBitPublicCandidateV1>("attribute.local", 0, 1UL),
            "announcement",
            sourceLocator: null,
            phase: null,
            sourceIndex: 0U,
            amount: null,
            choiceKind: null,
            choiceValue: null,
            responseIndex: null,
            continuationOperation: "");
        AssertMapped(
            frame,
            New<FlatPromptRaceSelectionPublicContextV1>((byte)0, (byte)2, 3UL),
            New<FlatPromptMaskBitPublicCandidateV1>("race.continuation.local", 1, 2UL),
            "pick",
            null,
            null,
            1U,
            null,
            null,
            null,
            null,
            "pick");

        FlatPromptCounterSourcePublicDescriptorV1 counterSource =
            New<FlatPromptCounterSourcePublicDescriptorV1>(0, (ushort)3, visible);
        AssertMapped(
            frame,
            New<FlatPromptCounterSelectionPublicContextV1>(
                (byte)0,
                (ushort)5,
                (ushort)3,
                new[] { counterSource }),
            New<FlatPromptCounterAmountPublicCandidateV1>("counter.local", 0, 2),
            "assign_amount",
            "p0:MONSTER_ZONE:0",
            null,
            0U,
            2,
            null,
            null,
            null,
            "amount");

        FlatPromptSortSourcePublicDescriptorBaseV1 sortSource =
            New<FlatPromptSortSourceLocatorPublicDescriptorV1>(0, visible);
        AssertMapped(
            frame,
            New<FlatPromptSortSelectionPublicContextV1>(
                (byte)0,
                FlatPromptSortKindV1.SortCard,
                new[] { sortSource }),
            New<FlatPromptSortLocatorPublicCandidateV1>(
                "sort.local",
                (FlatPromptFamilyV1)25,
                0,
                visible),
            "pick",
            "p0:MONSTER_ZONE:0",
            null,
            0U,
            null,
            null,
            null,
            null,
            "pick");
        AssertMapped(
            frame,
            New<FlatPromptSortSelectionPublicContextV1>(
                (byte)0,
                FlatPromptSortKindV1.SortCard,
                new[] { sortSource }),
            New<FlatPromptCancelPublicCandidateV1>("sort.cancel.local"),
            "cancel",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            "bypass");
    }

    private static void TestNativeCandidateMappingVectors()
    {
        PerspectiveSafeFrameV1 frame = CreatePublicFrame();
        PublicSemanticLocatorV1 visible = Locator("p0:MONSTER_ZONE:0");
        PublicSemanticLocatorV1 hidden = Locator("p1:SPELL_TRAP_ZONE:0");

        Require(
            MapSingle(
                frame,
                New<FlatPromptRaceSelectionPublicContextV1>((byte)0, (byte)1, 1UL),
                New<FlatPromptMaskBitPublicCandidateV1>("native.race.direct", 0, 1UL))
            .PublicActionKey == NativeRaceDirectKey,
            "native RACE direct mapping vector differs");
        Require(
            MapSingle(
                frame,
                New<FlatPromptRaceSelectionPublicContextV1>((byte)0, (byte)2, 3UL),
                New<FlatPromptMaskBitPublicCandidateV1>("native.race.pick", 1, 2UL))
            .PublicActionKey == NativeRaceContinuationKey,
            "native RACE continuation mapping vector differs");

        Require(
            MapSingle(
                frame,
                New<FlatPromptIdlePublicContextV1>((byte)0),
                New<FlatIdleSummonPublicCandidateV1>(
                    "native.idle",
                    3,
                    visible))
            .PublicActionKey == NativeIdleKey,
            "native IDLE mapping vector differs");
        Require(
            MapSingle(
                frame,
                New<FlatPromptBattlePublicContextV1>((byte)0),
                New<FlatBattleActivatablePublicCandidateV1>(
                    "native.battle",
                    2,
                    visible,
                    42UL,
                    (byte)0))
            .PublicActionKey == NativeBattleKey,
            "native BATTLE mapping vector differs");

        Require(
            MapSingle(
                frame,
                New<FlatPromptSelectUnselectCardPublicContextV1>(
                    (byte)0,
                    true,
                    true,
                    1U,
                    2U,
                    1,
                    1),
                New<FlatPromptSelectUnselectLocatorCandidateV1>(
                    "native.unselect",
                    FlatPromptChoiceKindV1.Unselect,
                    FlatPromptSourceSectionV1.Unselectable,
                    0,
                    hidden))
            .PublicActionKey == NativeUnselectKey,
            "native SELECT_UNSELECT mapping vector differs");

        FlatPromptCounterSourcePublicDescriptorV1 counterSource =
            New<FlatPromptCounterSourcePublicDescriptorV1>(0, (ushort)3, visible);
        Require(
            MapSingle(
                frame,
                New<FlatPromptCounterSelectionPublicContextV1>(
                    (byte)0,
                    (ushort)5,
                    (ushort)3,
                    new[] { counterSource }),
                New<FlatPromptCounterAmountPublicCandidateV1>(
                    "native.counter",
                    0,
                    2))
            .PublicActionKey == NativeCounterKey,
            "native COUNTER mapping vector differs");

        FlatPromptFieldPlaceV1[] directPlaces =
        {
            New<FlatPromptFieldPlaceV1>(
                (byte)0,
                FlatPromptFieldZoneV1.MonsterZone,
                (byte)0)
        };
        Require(
            MapSingle(
                frame,
                New<FlatPromptPlaceSelectionPublicContextV1>(
                    (byte)0,
                    (byte)1,
                    directPlaces),
                New<FlatPromptFieldPlacePublicCandidateV1>(
                    "native.place",
                    (byte)0,
                    FlatPromptFieldZoneV1.MonsterZone,
                    (byte)0))
            .PublicActionKey == NativePlaceKey,
            "native PLACE mapping vector differs");

        FlatPromptFieldPlaceV1[] continuationPlaces =
        {
            New<FlatPromptFieldPlaceV1>(
                (byte)1,
                FlatPromptFieldZoneV1.SpellTrapZone,
                (byte)2)
        };
        Require(
            MapSingle(
                frame,
                New<FlatPromptPlaceSelectionPublicContextV1>(
                    (byte)0,
                    (byte)2,
                    continuationPlaces),
                New<FlatPromptFieldPlacePublicCandidateV1>(
                    "native.place.pick",
                    (byte)1,
                    FlatPromptFieldZoneV1.SpellTrapZone,
                    (byte)2))
            .PublicActionKey == NativePlacePickKey,
            "native PLACE continuation mapping vector differs");
    }

    private static void TestNToNAndFailClosedMapping()
    {
        PerspectiveSafeFrameV1 frame = CreatePublicFrame();
        PublicSemanticLocatorV1 visible = Locator("p0:MONSTER_ZONE:0");
        FlatPromptOptionPublicContextV1 context =
            New<FlatPromptOptionPublicContextV1>((byte)0);
        FlatOptionPublicCandidateDescriptorV1 first =
            New<FlatOptionPublicCandidateDescriptorV1>("local.first", 0, 77UL);
        FlatOptionPublicCandidateDescriptorV1 second =
            New<FlatOptionPublicCandidateDescriptorV1>("local.second", 1, 77UL);
        OcgForgePublicDecisionContextResultV1 mapped =
            TryMap(
                frame,
                context,
                new FlatPublicCandidateDescriptorV1[] { first, second },
                4);
        Require(mapped.IsSuccess && mapped.Context is not null,
            mapped.Error?.ToString() ?? "N-to-N mapping failed");
        OcgForgePublicDecisionContextV1 mappedContext = mapped.Context!;
        Require(mappedContext.Candidates.Count == 2 &&
                mappedContext.Candidates[0].Descriptor.Choice is
                    { ResponseIndex: 0 } &&
                mappedContext.Candidates[1].Descriptor.Choice is
                    { ResponseIndex: 1 },
            "candidate count and source order must be preserved");
        Require(mappedContext.Candidates[0].PublicActionKey !=
                mappedContext.Candidates[1].PublicActionKey,
            "distinct source occurrences must retain distinct public keys");

        FlatYesNoPublicCandidateDescriptorV1 sameNoA =
            New<FlatYesNoPublicCandidateDescriptorV1>(
                "local.no.a",
                FlatPromptChoiceKindV1.No);
        FlatYesNoPublicCandidateDescriptorV1 sameNoB =
            New<FlatYesNoPublicCandidateDescriptorV1>(
                "local.no.b",
                FlatPromptChoiceKindV1.No);
        OcgForgePublicDecisionContextResultV1 collision =
            TryMap(
                frame,
            New<FlatPromptYesNoPublicContextV1>((byte)0, 42UL),
                new FlatPublicCandidateDescriptorV1[] { sameNoA, sameNoB },
                5);
        Require(!collision.IsSuccess &&
                collision.Error!.Value.Code ==
                    OcgForgePublicCandidateBridgeErrorCodeV1.DuplicatePublicActionKey &&
                collision.Context is null,
            "lost semantic distinction must reject the whole domain");

        OcgForgePublicDecisionContextResultV1 promptCode =
            TryMap(
                frame,
                New<FlatPromptCardSelectionPublicContextV1>((byte)0, 1U, 1U, false),
                new FlatPublicCandidateDescriptorV1[]
                {
                    New<FlatPromptCardSelectionPromptCodeCandidateV1>(
                        "local.prompt-code",
                        0,
                        12345678U)
                },
                6);
        Require(!promptCode.IsSuccess &&
                promptCode.Error!.Value.Code ==
                    OcgForgePublicCandidateBridgeErrorCodeV1.PromptLocalCardCode,
            "prompt-local CardCode must reject the complete frame");

        OcgForgePublicDecisionContextResultV1 noPersistentLocator =
            TryMap(
                frame,
                New<FlatPromptCardSelectionPublicContextV1>((byte)0, 1U, 1U, false),
                new FlatPublicCandidateDescriptorV1[]
                {
                    New<FlatPromptCardSelectionAnonymousCandidateV1>("local.anonymous", 0)
                },
                7);
        Require(noPersistentLocator.IsSuccess &&
                noPersistentLocator.Context!.Candidates[0].Descriptor.SourceReference is null,
            "anonymous public card occurrences must not receive guessed references");

        OcgForgePublicDecisionContextResultV1 invalidLocator =
            TryMap(
                CreatePublicFrame(Array.Empty<(string Locator, bool IdentityKnown)>()),
                New<FlatPromptChainPublicContextV1>((byte)0, (byte)0, true, 0U, 0U),
                new FlatPublicCandidateDescriptorV1[]
                {
                    New<FlatChainPublicCandidateDescriptorV1>(
                        "local.invalid-reference",
                        0,
                        Locator("p0:MONSTER_ZONE:0"),
                        42UL,
                        (byte)0)
                },
                8);
        Require(!invalidLocator.IsSuccess &&
                invalidLocator.Error!.Value.Code ==
                    OcgForgePublicCandidateBridgeErrorCodeV1.InvalidPublicReference,
            "detached public locators must fail closed");

        OcgForgePublicDecisionContextResultV1 actorMismatch =
            TryMap(
                CreatePublicFrame(
                    new[] { ("p0:MONSTER_ZONE:0", true) },
                    playerToAct: 1),
            New<FlatPromptYesNoPublicContextV1>((byte)0, 42UL),
                new FlatPublicCandidateDescriptorV1[]
                {
                    New<FlatYesNoPublicCandidateDescriptorV1>(
                        "local.yes",
                        FlatPromptChoiceKindV1.Yes)
                },
                9);
        Require(!actorMismatch.IsSuccess &&
                actorMismatch.Error!.Value.Code ==
                    OcgForgePublicCandidateBridgeErrorCodeV1.DecisionActorMismatch,
            "a conflicting frame actor must reject rather than infer turn state");

        OcgForgePublicDecisionContextResultV1 invalidMask =
            TryMap(
                frame,
                New<FlatPromptRaceSelectionPublicContextV1>((byte)0, (byte)1, 1UL),
                new FlatPublicCandidateDescriptorV1[]
                {
                    New<FlatPromptMaskBitPublicCandidateV1>("local.invalid-mask", 1, 2UL)
                },
                10);
        Require(!invalidMask.IsSuccess &&
                invalidMask.Error!.Value.Code ==
                    OcgForgePublicCandidateBridgeErrorCodeV1.InvalidPublicValue,
            "a mask bit outside the accepted context must fail closed");

        FlatPromptCounterSourcePublicDescriptorV1 lowCapacitySource =
            New<FlatPromptCounterSourcePublicDescriptorV1>(0, (ushort)1, visible);
        OcgForgePublicDecisionContextResultV1 invalidAmount =
            TryMap(
                frame,
                New<FlatPromptCounterSelectionPublicContextV1>(
                    (byte)0,
                    (ushort)5,
                    (ushort)1,
                    new[] { lowCapacitySource }),
                new FlatPublicCandidateDescriptorV1[]
                {
                    New<FlatPromptCounterAmountPublicCandidateV1>(
                        "local.invalid-amount",
                        0,
                        2)
                },
                11);
        Require(!invalidAmount.IsSuccess &&
                invalidAmount.Error!.Value.Code ==
                    OcgForgePublicCandidateBridgeErrorCodeV1.InvalidPublicValue,
            "counter amounts above their source capacity must fail closed");

        FlatPromptSortSourcePublicDescriptorBaseV1 sortChainSource =
            New<FlatPromptSortSourceLocatorPublicDescriptorV1>(0, visible);
        OcgForgePublicDecisionContextResultV1 wrongSortFamily =
            TryMap(
                frame,
                New<FlatPromptSortSelectionPublicContextV1>(
                    (byte)0,
                    FlatPromptSortKindV1.SortCard,
                    new[] { sortChainSource }),
                new FlatPublicCandidateDescriptorV1[]
                {
                    New<FlatPromptSortLocatorPublicCandidateV1>(
                        "local.wrong-sort-family",
                        (FlatPromptFamilyV1)21,
                        0,
                        visible)
                },
                12);
        Require(!wrongSortFamily.IsSuccess &&
                wrongSortFamily.Error!.Value.Code ==
                    OcgForgePublicCandidateBridgeErrorCodeV1.UnsupportedCandidate,
            "a sort candidate from another family must fail closed");

        OcgForgePublicDecisionContextResultV1 effectPromptCode =
            TryMap(
                frame,
                New<FlatPromptEffectYnCardCodePublicContextV1>(
                    (byte)0,
                    visible,
                    42UL,
                    12345678U),
                new FlatPublicCandidateDescriptorV1[]
                {
                    New<FlatEffectYnPublicCandidateDescriptorV1>(
                        "local.effect-code",
                        FlatPromptChoiceKindV1.No)
                },
                13);
        Require(!effectPromptCode.IsSuccess &&
                effectPromptCode.Error!.Value.Code ==
                    OcgForgePublicCandidateBridgeErrorCodeV1.PromptLocalCardCode,
            "effect-context prompt-local CardCode must fail closed");

        OcgForgePublicCandidateV1 keyA =
            MapSingle(
                frame,
                New<FlatPromptOptionPublicContextV1>((byte)0),
                New<FlatOptionPublicCandidateDescriptorV1>("local.key.a", 0, 9UL));
        OcgForgePublicCandidateV1 keyB =
            MapSingle(
                frame,
                New<FlatPromptOptionPublicContextV1>((byte)0),
                New<FlatOptionPublicCandidateDescriptorV1>("local.key.b", 0, 9UL));
        OcgForgePublicCandidateV1 changed =
            MapSingle(
                frame,
                New<FlatPromptOptionPublicContextV1>((byte)0),
                New<FlatOptionPublicCandidateDescriptorV1>("local.key.c", 0, 10UL));
        Require(keyA.PublicActionKey == keyB.PublicActionKey,
            "Ignis-local routing keys must not affect the OCGForge key");
        Require(keyA.PublicActionKey != changed.PublicActionKey,
            "an OCGForge-semantic option value must affect the public key");
    }

    private static OcgForgePublicCandidateV1 AssertMapped(
        PerspectiveSafeFrameV1 frame,
        FlatPromptPublicContextV1 context,
        FlatPublicCandidateDescriptorV1 candidate,
        string actionKind,
        string? sourceLocator,
        uint? phase,
        uint? sourceIndex,
        int? amount,
        OcgForgePublicChoiceKindV1? choiceKind,
        ulong? choiceValue,
        uint? responseIndex = null,
        string continuationOperation = "",
        byte? position = null)
    {
        OcgForgePublicCandidateV1 result = MapSingle(frame, context, candidate);
        Require(result.Descriptor.ActionKind == actionKind,
            $"unexpected action kind for {candidate.GetType().Name}");
        Require(result.Descriptor.SourceReference?.ObservationLocator == sourceLocator,
            $"unexpected source reference for {candidate.GetType().Name}");
        Require(result.Descriptor.Phase == phase,
            $"unexpected phase for {candidate.GetType().Name}");
        Require(result.Descriptor.SourceIndex == sourceIndex,
            $"unexpected source index for {candidate.GetType().Name}: " +
            $"expected={sourceIndex?.ToString(CultureInfo.InvariantCulture) ?? "absent"} " +
            $"actual={result.Descriptor.SourceIndex?.ToString(CultureInfo.InvariantCulture) ?? "absent"}");
        Require(result.Descriptor.Amount == amount,
            $"unexpected amount for {candidate.GetType().Name}");
        Require(result.Descriptor.Position == position,
            $"unexpected position for {candidate.GetType().Name}");
        Require(result.Descriptor.ContinuationOperation == continuationOperation,
            $"unexpected continuation operation for {candidate.GetType().Name}");
        if (choiceKind.HasValue)
        {
            Require(result.Descriptor.Choice is
                    { } choice &&
                choice.Kind == choiceKind.Value &&
                choice.Value == choiceValue &&
                choice.ResponseIndex == responseIndex,
                $"unexpected typed choice for {candidate.GetType().Name}");
        }
        else
        {
            Require(result.Descriptor.Choice is null,
                $"unexpected choice for {candidate.GetType().Name}");
        }

        return result;
    }

    private static OcgForgePublicDecisionContextResultV1 TryMap(
        PerspectiveSafeFrameV1 frame,
        FlatPromptProjectionResultV1 projection,
        ulong decisionIndex)
    {
        OcgForgeAcceptedDecisionBoundaryV1 accepted =
            New<OcgForgeAcceptedDecisionBoundaryV1>(
                frame,
                projection,
                New<OcgForgeAcceptedDecisionIndexV1>(decisionIndex));
        return OcgForgePublicCandidateBridgeV1.TryCreate(accepted);
    }

    private static OcgForgePublicDecisionContextResultV1 TryMap(
        PerspectiveSafeFrameV1 frame,
        FlatPromptPublicContextV1 context,
        IReadOnlyList<FlatPublicCandidateDescriptorV1> candidates,
        ulong decisionIndex)
    {
        FlatPromptProjectionResultV1 projection =
            New<FlatPromptProjectionResultV1>(
                true,
                FlatPromptErrorCodeV1.None,
                context,
                candidates);
        return TryMap(frame, projection, decisionIndex);
    }

    private static OcgForgePublicCandidateV1 MapSingle(
        PerspectiveSafeFrameV1 frame,
        FlatPromptPublicContextV1 context,
        FlatPublicCandidateDescriptorV1 candidate)
    {
        OcgForgePublicDecisionContextResultV1 result = TryMap(
            frame,
            context,
            new[] { candidate },
            0);
        Require(result.IsSuccess && result.Context is not null,
            result.Error?.ToString() ?? "candidate mapping failed");
        OcgForgePublicDecisionContextV1 mappedContext = result.Context!;
        return mappedContext.Candidates[0];
    }

    private static T New<T>(params object?[] arguments)
        where T : class
    {
        object? value = Activator.CreateInstance(
            typeof(T),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: arguments,
            culture: CultureInfo.InvariantCulture);
        return (T)(value ?? throw new InvalidOperationException(
            $"Could not construct {typeof(T).Name}."));
    }

    private static PublicSemanticLocatorV1 Locator(string text)
    {
        Require(PublicSemanticLocatorV1.TryParse(text, out PublicSemanticLocatorV1? locator) &&
                locator is not null,
            $"invalid test locator {text}");
        return locator!;
    }

    private static PerspectiveSafeFrameV1 CreateEmptyFrame()
    {
        PerspectiveSafeFrameSourceResultV1 result =
            PerspectiveSafePublicFrameSourceV1.TryCreate(
                new PerspectiveSafeFrameSourceInputV1(
                    new PerspectiveSafeGlobalsV1(
                        duelFlags: 0,
                        lifePoints: new uint[] { 8000, 8000 },
                        turnPlayer: 1,
                        turnCount: 2,
                        phase: 4),
                    Array.Empty<PerspectiveSafeZoneV1>(),
                    Array.Empty<PerspectiveSafeEntityV1>(),
                    Array.Empty<PerspectiveSafeRelationshipV1>(),
                    new PerspectiveSafeChainStateV1(
                        0,
                        Array.Empty<PerspectiveSafeChainLinkV1>()),
                    Array.Empty<PerspectiveSafeVisibleEventV1>(),
                    new PerspectiveSafeMatchContextV1(
                        0,
                        0,
                        new PerspectiveSafeKnowledgeV1(false, false),
                        new PerspectiveSafeDeckV1(false),
                        new PerspectiveSafeDeckV1(false))));
        Require(result.IsSuccess && result.Frame is not null,
            result.Error?.ToString() ?? "empty frame rejected");
        return result.Frame!;
    }

    private static PerspectiveSafeFrameV1 CreatePublicFrame(
        IEnumerable<(string Locator, bool IdentityKnown)>? entities = null,
        byte? playerToAct = null)
    {
        (string Locator, bool IdentityKnown)[] source =
            entities?.ToArray() ??
            new[]
            {
                ("p0:MONSTER_ZONE:0", true),
                ("p1:SPELL_TRAP_ZONE:0", false),
                ("p0:HAND:public:12345678:0", true)
            };
        PerspectiveSafeEntityV1[] safeEntities = source
            .OrderBy(value => value.Locator, StringComparer.Ordinal)
            .Select(value => new PerspectiveSafeEntityV1(
                value.Locator,
                value.IdentityKnown,
                value.IdentityKnown ? 12345678U : null,
                value.Locator.StartsWith("p1:", StringComparison.Ordinal) ? (byte)1 : (byte)0,
                value.Locator.StartsWith("p1:", StringComparison.Ordinal) ? (byte)1 : (byte)0,
                value.IdentityKnown
                    ? PerspectiveSafeSemanticZoneV1.MonsterZone
                    : PerspectiveSafeSemanticZoneV1.SpellTrapZone,
                0,
                null,
                value.IdentityKnown
                    ? PerspectiveSafePositionV1.FaceUpAttack
                    : PerspectiveSafePositionV1.FaceDownDefense,
                value.IdentityKnown,
                !value.IdentityKnown))
            .ToArray();
        PerspectiveSafeFrameSourceResultV1 result =
            PerspectiveSafePublicFrameSourceV1.TryCreate(
                new PerspectiveSafeFrameSourceInputV1(
                    new PerspectiveSafeGlobalsV1(
                        duelFlags: 0,
                        lifePoints: new uint[] { 8000, 8000 },
                        playerToAct: playerToAct,
                        turnPlayer: 1,
                        turnCount: 2,
                        phase: 4),
                    Array.Empty<PerspectiveSafeZoneV1>(),
                    safeEntities,
                    Array.Empty<PerspectiveSafeRelationshipV1>(),
                    new PerspectiveSafeChainStateV1(
                        0,
                        Array.Empty<PerspectiveSafeChainLinkV1>()),
                    Array.Empty<PerspectiveSafeVisibleEventV1>(),
                    new PerspectiveSafeMatchContextV1(
                        0,
                        0,
                        new PerspectiveSafeKnowledgeV1(false, false),
                        new PerspectiveSafeDeckV1(false),
                        new PerspectiveSafeDeckV1(false))));
        Require(result.IsSuccess && result.Frame is not null,
            result.Error?.ToString() ?? "public test frame rejected");
        return result.Frame!;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
