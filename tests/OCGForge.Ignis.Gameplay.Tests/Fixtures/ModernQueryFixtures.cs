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

internal static class ModernQueryFixtures
{
    internal static ModernQueryV1 DecodeQuery(params byte[][] records)
    {
        ModernQueryDecodeResult result = ModernQueryDecoderV1.Decode(
            Join(records));
        True(result.IsSuccess, result.Error.ToString());
        NotNull(result.Value);
        return result.Value!;
    }

    internal static byte[] QueryRecord(QueryFlagV1 flag, byte[] payload) =>
        QueryRecordRaw((uint)flag, payload);

    internal static byte[] QueryRecordRaw(uint flag, byte[] payload)
    {
        byte[] record = new byte[2 + 4 + payload.Length];
        BinaryPrimitives.WriteUInt16LittleEndian(
            record.AsSpan(0, 2), checked((ushort)(4 + payload.Length)));
        BinaryPrimitives.WriteUInt32LittleEndian(record.AsSpan(2, 4), flag);
        payload.CopyTo(record, 6);
        return record;
    }

    internal static byte[] QueryEnd() => new byte[] { 4, 0, 0, 0, 0, 0x80 };
}
