using OCGForge.Ignis.Gameplay;
using OCGForge.Ignis.Gameplay.Tests.Fixtures;
using static OCGForge.Ignis.Gameplay.Tests.GameplayMessageFixtures;
using static OCGForge.Ignis.Gameplay.Tests.MirrorFixtures;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I6DCrossLocatorMappingAuthorityCharacterizationTests
{
    internal static void TestCrossLocatorAuthorityCases()
    {
        I6DCrossLocatorMappingObservationV1 ownUnique =
            Analyze(OwnHandUnique(), 0);
        Equal(
            I6DCrossLocatorMappingProofKindV1.UniqueSafeAttributeMatch,
            ownUnique.ProofKind);
        Equal(I6DCrossLocatorFormV1.PublicOrdinal, ownUnique.I4LocatorForm);
        Equal(I6DCrossLocatorFormV1.Indexed, ownUnique.I6C5LocatorForm);
        Equal(1, ownUnique.I6C5SafeCurrentMatchCount);
        False(ownUnique.UniqueMatchIsMappingAuthority);

        I6DCrossLocatorMappingObservationV1[] ownDuplicate =
            OwnHandDuplicate();
        True(ownDuplicate.All(value =>
            value.ProofKind ==
                I6DCrossLocatorMappingProofKindV1.AmbiguousSafeCurrentEntities));
        True(ownDuplicate.All(value =>
            value.I4LocatorForm == I6DCrossLocatorFormV1.PublicOrdinal &&
            value.I6C5LocatorForm == I6DCrossLocatorFormV1.Indexed &&
            value.I6C5SafeCurrentMatchCount == 2));

        I6DCrossLocatorMappingObservationV1 opponentHand =
            Analyze(OpponentPublicHand(), 0);
        Equal(
            I6DCrossLocatorMappingProofKindV1.ExactPublicToken,
            opponentHand.ProofKind);
        Equal(I6DCrossLocatorFormV1.PublicOrdinal, opponentHand.I4LocatorForm);
        Equal(I6DCrossLocatorFormV1.PublicOrdinal, opponentHand.I6C5LocatorForm);

        I6DCrossLocatorMappingObservationV1 extraDeck =
            Analyze(ExtraDeck(), 0);
        Equal(
            I6DCrossLocatorMappingProofKindV1.ExactPublicToken,
            extraDeck.ProofKind);
        Equal(I6DCrossLocatorFormV1.PublicOrdinal, extraDeck.I4LocatorForm);
        Equal(I6DCrossLocatorFormV1.PublicOrdinal, extraDeck.I6C5LocatorForm);

        I6DCrossLocatorMappingObservationV1 crossPile =
            Analyze(CrossPile(), 0);
        Equal(
            I6DCrossLocatorMappingProofKindV1.UniqueSafeAttributeMatch,
            crossPile.ProofKind);
        Equal(0, crossPile.I6C5ExactLocatorMatchCount);
        Equal(1, crossPile.I6C5SafeCurrentMatchCount);
        Equal(I6DCrossLocatorFormV1.PublicOrdinal, crossPile.I4LocatorForm);
        Equal(I6DCrossLocatorFormV1.PublicOrdinal, crossPile.I6C5LocatorForm);

        I6DCrossLocatorCaseV1 hiddenCase = HiddenOpponentHand();
        False(hiddenCase.PublicState.Cards.Any(card =>
            card.AbsolutePlayer == 1 &&
            card.Zone == PublicSemanticZoneV1.Hand &&
            card.CardCode.HasValue));
        False(hiddenCase.PublicState.Cards.Any(card =>
            card.AbsolutePlayer == 1 &&
            card.Zone == PublicSemanticZoneV1.Hand &&
            card.Locator.Value.Contains(":public:", StringComparison.Ordinal)));
        False(hiddenCase.I6C5Entities.Any(entity =>
            entity.Controller == 1 &&
            entity.Zone == PerspectiveSafeSemanticZoneV1.Hand &&
            entity.IdentityKnown &&
            entity.Passcode.HasValue));

        I6DCrossLocatorMappingObservationV1 missing =
            Analyze(OwnHandUnique(), 0, Array.Empty<PerspectiveSafeEntityV1>());
        Equal(
            I6DCrossLocatorMappingProofKindV1.MissingSafeCurrentEntity,
            missing.ProofKind);

        I6DCrossLocatorMappingObservationV1 stale =
            Analyze(OwnHandUnique(), 0, frameIsCurrent: false);
        Equal(
            I6DCrossLocatorMappingProofKindV1.StaleFrame,
            stale.ProofKind);

        True(ownUnique.SafePublicIdentityAvailable);
        True(!ownUnique.UsesHiddenIdentity);
        True(!ownUnique.UsesMirrorEntityIdentity);
        True(!ownUnique.UsesRawProtocolAddress);

        I6DCrossLocatorMappingObservationV1 repeat =
            Analyze(OwnHandUnique(), 0);
        Equal(ownUnique, repeat);
    }

    private static I6DCrossLocatorMappingObservationV1 Analyze(
        I6DCrossLocatorCaseV1 value,
        int candidateIndex,
        IReadOnlyList<PerspectiveSafeEntityV1>? entityOverride = null,
        bool frameIsCurrent = true)
    {
        PublicCardStateV1 card = value.PublicState.Cards[candidateIndex];
        True(card.CardCode.HasValue, "characterization card must have a public code");
        FlatIdleSummonCardCodePublicCandidateV1 candidate = new(
            $"I6D_CHARACTERIZATION:{candidateIndex}",
            candidateIndex,
            card.Locator,
            card.CardCode!.Value);
        I6DCrossLocatorMappingResultV1 result =
            I6DCrossLocatorMappingAuthorityCharacterizationV1.Characterize(
                candidate,
                value.PublicState,
                entityOverride ?? value.I6C5Entities,
                frameIsCurrent);
        True(result.IsSuccess);
        return result.Observation;
    }

    private static I6DCrossLocatorMappingObservationV1[] OwnHandDuplicate()
    {
        I6DCrossLocatorCaseV1 value = CreateCase(
            new[]
            {
                MoveMessage(
                    0x1100,
                    new ModernLocInfoV1(0, 0, 0, 0),
                    new ModernLocInfoV1(0, 0x02, 0, 0x08),
                    0),
                MoveMessage(
                    0x1100,
                    new ModernLocInfoV1(0, 0, 0, 0),
                    new ModernLocInfoV1(0, 0x02, 1, 0x08),
                    0),
                new byte[] { 40, 0 }
            },
            deckCount0: 2,
            deckCount1: 0,
            extraCount0: 0,
            extraCount1: 0);
        return value.PublicState.Cards
            .Where(card => card.Zone == PublicSemanticZoneV1.Hand)
            .OrderBy(card => card.Locator.Value, StringComparer.Ordinal)
            .Select((_, index) => Analyze(value, index))
            .ToArray();
    }

    private static I6DCrossLocatorCaseV1 OwnHandUnique() =>
        CreateCase(
            new[]
            {
                MoveMessage(
                    0x1000,
                    new ModernLocInfoV1(0, 0, 0, 0),
                    new ModernLocInfoV1(0, 0x02, 0, 0x08),
                    0),
                new byte[] { 40, 0 }
            },
            deckCount0: 1,
            deckCount1: 0,
            extraCount0: 0,
            extraCount1: 0);

    private static I6DCrossLocatorCaseV1 OpponentPublicHand() =>
        CreateCase(
            new[]
            {
                DrawMessage(1, (0x1200u, 0x05u)),
                new byte[] { 40, 0 }
            },
            deckCount0: 0,
            deckCount1: 1,
            extraCount0: 0,
            extraCount1: 0);

    private static I6DCrossLocatorCaseV1 ExtraDeck() =>
        CreateCase(
            new[]
            {
                MoveMessage(
                    0x1300,
                    new ModernLocInfoV1(0, 0, 0, 0),
                    new ModernLocInfoV1(1, 0x40, 0, 0x05),
                    0),
                new byte[] { 40, 0 }
            },
            deckCount0: 0,
            deckCount1: 0,
            extraCount0: 0,
            extraCount1: 0);

    private static I6DCrossLocatorCaseV1 CrossPile() =>
        CreateCase(
            new[]
            {
                DrawMessage(
                    1,
                    (0x1400u, 0x04u),
                    (0x1500u, 0x04u)),
                MoveMessage(
                    0x1400,
                    new ModernLocInfoV1(0, 0, 0, 0),
                    new ModernLocInfoV1(1, 0x40, 0, 0x05),
                    0),
                new byte[] { 40, 0 }
            },
            deckCount0: 0,
            deckCount1: 2,
            extraCount0: 0,
            extraCount1: 1);

    private static I6DCrossLocatorCaseV1 HiddenOpponentHand() =>
        CreateCase(
            new[]
            {
                DrawMessage(1, (0x1600u, 0x08u)),
                new byte[] { 40, 0 }
            },
            deckCount0: 0,
            deckCount1: 1,
            extraCount0: 0,
            extraCount1: 0);

    private static I6DCrossLocatorCaseV1 CreateCase(
        IReadOnlyList<byte[]> messages,
        ushort deckCount0,
        ushort deckCount1,
        ushort extraCount0,
        ushort extraCount1)
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(
                0,
                deckCount0,
                extraCount0,
                deckCount1,
                extraCount1);
        foreach (byte[] message in messages)
        {
            GameplayMessageV1 decoded = DecodeMessage(decoder, message);
            MirrorApplyResult applied = mirror.Apply(decoded);
            True(applied.IsSuccess, applied.Error.ToString());
        }

        PerspectiveSafeI6C3SourceResultV1 i6C5State =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C3(mirror);
        True(i6C5State.IsSuccess, i6C5State.Error?.ToString() ?? "I6C3 rejected");
        PublicStateProjectionResultV1 i4State =
            PublicStateProjectionV1.TryProject(
                mirror.Snapshot,
                new PublicStateProjectionContextV1(0));
        True(i4State.IsSuccess, i4State.Error.ToString());
        NotNull(i4State.Snapshot);
        return new(i4State.Snapshot!, i6C5State.Source!.Entities);
    }

    private sealed record I6DCrossLocatorCaseV1(
        PublicStateSnapshotV1 PublicState,
        IReadOnlyList<PerspectiveSafeEntityV1> I6C5Entities);
}
