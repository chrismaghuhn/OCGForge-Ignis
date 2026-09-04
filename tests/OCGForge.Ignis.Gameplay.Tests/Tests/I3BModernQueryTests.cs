using System.Buffers.Binary;
using System.Reflection;
using OCGForge.Ignis.Client;
using OCGForge.Ignis.Gameplay;
using OCGForge.Ignis.Protocol;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;
using static OCGForge.Ignis.Gameplay.Tests.GameplayMessageFixtures;
using static OCGForge.Ignis.Gameplay.Tests.ModernQueryFixtures;
using static OCGForge.Ignis.Gameplay.Tests.MirrorFixtures;
using static OCGForge.Ignis.Gameplay.Tests.TransportFixtures;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I3BModernQueryTests
{
    internal static void TestQueryUnion()
    {
        byte[] query = Join(
            QueryRecord(QueryFlagV1.Code, U32(0x11223344)),
            QueryRecord(QueryFlagV1.Position, U32(0x55667788)),
            QueryRecord(QueryFlagV1.Alias, U32(0x01020304)),
            QueryRecord(QueryFlagV1.Type, U32(0x05060708)),
            QueryRecord(QueryFlagV1.Level, U32(0x090a0b0c)),
            QueryRecord(QueryFlagV1.Rank, U32(0x0d0e0f10)),
            QueryRecord(QueryFlagV1.Attribute, U32(0x11121314)),
            QueryRecord(QueryFlagV1.Race, U64(0x1122334455667788)),
            QueryRecord(QueryFlagV1.Attack, I32(-100)),
            QueryRecord(QueryFlagV1.Defense, I32(2100)),
            QueryRecord(QueryFlagV1.BaseAttack, I32(1900)),
            QueryRecord(QueryFlagV1.BaseDefense, I32(1600)),
            QueryRecord(QueryFlagV1.Reason, U32(0x15161718)),
            QueryRecord(QueryFlagV1.ReasonCard, LocInfo(0, 0x10, 2, 0x1)),
            QueryRecord(QueryFlagV1.EquipCard, LocInfo(1, 0x04, 3, 0x4)),
            QueryRecord(
                QueryFlagV1.TargetCard,
                Join(U32(1), LocInfo(0, 0x04, 0, 0x1))),
            QueryRecord(
                QueryFlagV1.OverlayCard,
                Join(U32(1), U32(0x21222324))),
            QueryRecord(
                QueryFlagV1.Counters,
                Join(U32(1), U32(0x25262728))),
            QueryRecord(QueryFlagV1.Owner, new byte[] { 1 }),
            QueryRecord(QueryFlagV1.Status, U32(0x29303132)),
            QueryRecord(QueryFlagV1.IsPublic, new byte[] { 1 }),
            QueryRecord(QueryFlagV1.LScale, U32(0x33343536)),
            QueryRecord(QueryFlagV1.RScale, U32(0x37383940)),
            QueryRecord(QueryFlagV1.Link, Join(U32(2), U32(0x41424344))),
            QueryRecord(QueryFlagV1.IsHidden, new byte[] { 0 }),
            QueryRecord(QueryFlagV1.Cover, U32(0x45464748)),
            QueryEnd());

        ModernQueryDecodeResult decoded = ModernQueryDecoderV1.Decode(query);
        True(decoded.IsSuccess, decoded.Error.ToString());
        NotNull(decoded.Value);
        Equal(26, decoded.Value!.Fields.Count);
        Equal(QueryFlagV1.Code, decoded.Value.Fields[0].Flag);
        Equal(QueryFlagV1.Cover, decoded.Value.Fields[^1].Flag);
        Equal(
            0x11223344u,
            ((ModernQueryUInt32PayloadV1)decoded.Value.Fields[0].Payload).Value);
        Equal(
            -100,
            ((ModernQueryInt32PayloadV1)decoded.Value.Fields[8].Payload).Value);
        Equal(
            0x1122334455667788ul,
            ((ModernQueryUInt64PayloadV1)decoded.Value.Fields[7].Payload).Value);
        Equal(
            1,
            ((ModernQueryLocInfoVectorPayloadV1)decoded.Value.Fields[15].Payload)
                .Values.Count);
        Equal(
            0x41424344u,
            ((ModernQueryLinkPayloadV1)decoded.Value.Fields[23].Payload).LinkMarker);

        byte[] streamBody = Join(new byte[] { 0, 0 }, query, query);
        ModernQueryStreamDecodeResult stream = ModernQueryDecoderV1.DecodeStream(
            Join(U32((uint)streamBody.Length), streamBody));
        True(stream.IsSuccess, stream.Error.ToString());
        Equal(3, stream.Values.Count);
        True(stream.Values[0].IsOnFieldSkipped);
        Equal(stream.Values[1], stream.Values[2]);
    }

    internal static void TestQueryFailures()
    {
        ModernQueryDecodeResult duplicateScalar = ModernQueryDecoderV1.Decode(
            Join(
                QueryRecord(QueryFlagV1.Code, U32(1)),
                QueryRecord(QueryFlagV1.Code, U32(2)),
                QueryEnd()));
        False(duplicateScalar.IsSuccess);
        Equal(GameplayErrorCode.DuplicateQueryFlag, duplicateScalar.Error);

        ModernQueryDecodeResult duplicateVector = ModernQueryDecoderV1.Decode(
            Join(
                QueryRecord(QueryFlagV1.OverlayCard, Join(U32(1), U32(3))),
                QueryRecord(QueryFlagV1.OverlayCard, Join(U32(1), U32(4))),
                QueryEnd()));
        False(duplicateVector.IsSuccess);
        Equal(GameplayErrorCode.DuplicateQueryFlag, duplicateVector.Error);

        byte[] oneQuery = Join(QueryRecord(QueryFlagV1.Code, U32(7)), QueryEnd());
        ModernQueryStreamDecodeResult repeatedAcrossQueries =
            ModernQueryDecoderV1.DecodeStream(
                Join(U32((uint)(oneQuery.Length * 2)), oneQuery, oneQuery));
        True(repeatedAcrossQueries.IsSuccess, repeatedAcrossQueries.Error.ToString());
        Equal(2, repeatedAcrossQueries.Values.Count);

        foreach (uint invalidFlag in new uint[] { 0, 3, 0x04000000 })
        {
            ModernQueryDecodeResult invalid = ModernQueryDecoderV1.Decode(
                Join(QueryRecordRaw(invalidFlag, U32(1)), QueryEnd()));
            False(invalid.IsSuccess);
            Equal(GameplayErrorCode.UnsupportedQueryFlag, invalid.Error);
        }

        ModernQueryDecodeResult shortFlag = ModernQueryDecoderV1.Decode(
            new byte[] { 8, 0, 1 });
        False(shortFlag.IsSuccess);
        Equal(GameplayErrorCode.MalformedQuery, shortFlag.Error);

        ModernQueryDecodeResult shortVector = ModernQueryDecoderV1.Decode(
            Join(
                QueryRecord(QueryFlagV1.TargetCard, U32(1)),
                QueryEnd()));
        False(shortVector.IsSuccess);
        Equal(GameplayErrorCode.QueryLengthMismatch, shortVector.Error);

        ModernQueryDecodeResult countOverflow = ModernQueryDecoderV1.Decode(
            Join(
                QueryRecord(QueryFlagV1.TargetCard, U32(uint.MaxValue)),
                QueryEnd()));
        False(countOverflow.IsSuccess);
        Equal(GameplayErrorCode.QueryCountOverflow, countOverflow.Error);

        ModernQueryDecodeResult trailing = ModernQueryDecoderV1.Decode(
            Join(
                new byte[] { 9, 0, 1, 0, 0, 0, 0x78, 0x56, 0x34, 0x12, 0xaa },
                QueryEnd()));
        False(trailing.IsSuccess);
        Equal(GameplayErrorCode.QueryLengthMismatch, trailing.Error);

        ModernQueryDecodeResult missingTerminator = ModernQueryDecoderV1.Decode(
            QueryRecord(QueryFlagV1.Code, U32(1)));
        False(missingTerminator.IsSuccess);
        Equal(GameplayErrorCode.MalformedQuery, missingTerminator.Error);
    }
}
