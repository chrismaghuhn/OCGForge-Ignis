using OCGForge.Ignis.Gameplay;
using OCGForge.Ignis.Gameplay.Tests.Fixtures;
using static OCGForge.Ignis.Gameplay.Tests.GameplayMessageFixtures;
using static OCGForge.Ignis.Gameplay.Tests.MirrorFixtures;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I6GLocatorParityCharacterizationTests
{
    internal static void TestI4AndI6C5CrossPileOrdinalParity()
    {
        I6GLocatorParityObservationV1 first = CreateObservation();
        I6GLocatorParityObservationV1 second = CreateObservation();

        Equal(
            I6GLocatorParityClassificationV1.ExactMatch,
            first.Classification);
        Equal(0, first.CandidateIndex);
        Equal(FlatPromptChoiceKindV1.SpecialSummon, first.ChoiceKind);
        Equal((byte)1, first.AbsolutePlayer);
        Equal(PublicSemanticZoneV1.ExtraDeck, first.PublicZone);
        True(first.I4PublicStateLocatorPresent);
        Equal(1, first.I4PublicStateLocatorMatchCount);
        Equal(1, first.I6C5ExactLocatorMatchCount);
        Equal(1, first.I6C5SamePublicCardMatchCount);
        Equal(first, second);
    }

    private static I6GLocatorParityObservationV1 CreateObservation()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(
                0,
                extraCount0: 0,
                deckCount1: 2,
                extraCount1: 1);
        Apply(mirror, decoder, DrawMessage(
            1,
            (0x100u, 0x04u),
            (0x200u, 0x04u)));
        Apply(
            mirror,
            decoder,
            MoveMessage(
                0x100,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(1, 0x40, 0, 0x05),
                0));
        Apply(mirror, decoder, new byte[] { 40, 1 });

        // I6C5 first obtains this I6C3 state source before applying its
        // later full-frame coverage checks. This fixture intentionally
        // contains a public opponent-hand identity, so the complete I6C5
        // wrapper rejects it for that separate coverage reason. Comparing
        // the shared entity source keeps this test about the existing
        // I4/I6C5 ordinalizer and does not manufacture an accepted frame.
        PerspectiveSafeI6C3SourceResultV1 i6C5StateSource =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C3(mirror);
        True(
            i6C5StateSource.IsSuccess,
            i6C5StateSource.Error?.ToString() ?? "I6C3 source rejected");
        NotNull(i6C5StateSource.Source);

        PublicStateProjectionResultV1 publicProjection =
            PublicStateProjectionV1.TryProject(
                mirror.Snapshot,
                new PublicStateProjectionContextV1(0));
        True(publicProjection.IsSuccess, publicProjection.Error.ToString());
        NotNull(publicProjection.Snapshot);

        True(PublicSemanticLocatorV1.TryCreatePublicOrdinal(
                1,
                PublicSemanticZoneV1.ExtraDeck,
                0x100,
                0,
                out PublicSemanticLocatorV1? extraLocator));
        NotNull(extraLocator);
        FlatIdleSpecialSummonCardCodePublicCandidateV1 candidate =
            new(
                "MSG_SELECT_IDLECMD:SPECIAL_SUMMON:0",
                0,
                extraLocator!,
                0x100);

        I6GLocatorParityCharacterizationResultV1 result =
            I6GLocatorParityCharacterizationV1.CompareCandidate(
                0,
                candidate,
                publicProjection.Snapshot!,
                i6C5StateSource.Source!.Entities);
        True(result.IsSuccess, result.Error.ToString());
        return result.Observation;
    }

    private static void Apply(
        PerspectiveStateMirrorV1 mirror,
        GameplayMessageDecoderV1 decoder,
        byte[] bytes)
    {
        GameplayMessageV1 message = DecodeMessage(decoder, bytes);
        MirrorApplyResult result = mirror.Apply(message);
        True(result.IsSuccess, result.Error.ToString());
    }

}
