using System.Buffers.Binary;
using System.Collections.ObjectModel;

namespace OCGForge.Ignis.Gameplay;

public enum QueryFlagV1 : uint
{
    Code = 0x00000001,
    Position = 0x00000002,
    Alias = 0x00000004,
    Type = 0x00000008,
    Level = 0x00000010,
    Rank = 0x00000020,
    Attribute = 0x00000040,
    Race = 0x00000080,
    Attack = 0x00000100,
    Defense = 0x00000200,
    BaseAttack = 0x00000400,
    BaseDefense = 0x00000800,
    Reason = 0x00001000,
    ReasonCard = 0x00002000,
    EquipCard = 0x00004000,
    TargetCard = 0x00008000,
    OverlayCard = 0x00010000,
    Counters = 0x00020000,
    Owner = 0x00040000,
    Status = 0x00080000,
    IsPublic = 0x00100000,
    LScale = 0x00200000,
    RScale = 0x00400000,
    Link = 0x00800000,
    IsHidden = 0x01000000,
    Cover = 0x02000000,
    End = 0x80000000
}

public abstract class ModernQueryPayloadV1
{
    protected abstract bool ValueEquals(ModernQueryPayloadV1 other);

    public override bool Equals(object? obj) =>
        obj is ModernQueryPayloadV1 other && ValueEquals(other);

    public abstract override int GetHashCode();
}

public sealed class ModernQueryUInt8PayloadV1 : ModernQueryPayloadV1
{
    public ModernQueryUInt8PayloadV1(byte value)
    {
        Value = value;
    }

    public byte Value { get; }

    protected override bool ValueEquals(ModernQueryPayloadV1 other) =>
        other is ModernQueryUInt8PayloadV1 value && Value == value.Value;

    public override int GetHashCode() => Value.GetHashCode();
}

public sealed class ModernQueryUInt32PayloadV1 : ModernQueryPayloadV1
{
    public ModernQueryUInt32PayloadV1(uint value)
    {
        Value = value;
    }

    public uint Value { get; }

    protected override bool ValueEquals(ModernQueryPayloadV1 other) =>
        other is ModernQueryUInt32PayloadV1 value && Value == value.Value;

    public override int GetHashCode() => Value.GetHashCode();
}

public sealed class ModernQueryInt32PayloadV1 : ModernQueryPayloadV1
{
    public ModernQueryInt32PayloadV1(int value)
    {
        Value = value;
    }

    public int Value { get; }

    protected override bool ValueEquals(ModernQueryPayloadV1 other) =>
        other is ModernQueryInt32PayloadV1 value && Value == value.Value;

    public override int GetHashCode() => Value.GetHashCode();
}

public sealed class ModernQueryUInt64PayloadV1 : ModernQueryPayloadV1
{
    public ModernQueryUInt64PayloadV1(ulong value)
    {
        Value = value;
    }

    public ulong Value { get; }

    protected override bool ValueEquals(ModernQueryPayloadV1 other) =>
        other is ModernQueryUInt64PayloadV1 value && Value == value.Value;

    public override int GetHashCode() => Value.GetHashCode();
}

public sealed class ModernQueryLocInfoPayloadV1 : ModernQueryPayloadV1
{
    public ModernQueryLocInfoPayloadV1(ModernLocInfoV1 value)
    {
        Value = value;
    }

    public ModernLocInfoV1 Value { get; }

    protected override bool ValueEquals(ModernQueryPayloadV1 other) =>
        other is ModernQueryLocInfoPayloadV1 value && Value.Equals(value.Value);

    public override int GetHashCode() => ModernQueryHashV1.LocInfo(Value);
}

public sealed class ModernQueryLocInfoVectorPayloadV1 : ModernQueryPayloadV1
{
    private readonly ModernLocInfoV1[] values;
    private readonly ReadOnlyCollection<ModernLocInfoV1> valuesView;

    internal ModernQueryLocInfoVectorPayloadV1(
        IEnumerable<ModernLocInfoV1> values)
    {
        this.values = values.ToArray();
        valuesView = Array.AsReadOnly(this.values);
    }

    public IReadOnlyList<ModernLocInfoV1> Values => valuesView;

    protected override bool ValueEquals(ModernQueryPayloadV1 other) =>
        other is ModernQueryLocInfoVectorPayloadV1 value &&
        values.AsSpan().SequenceEqual(value.values);

    public override int GetHashCode()
    {
        int hash = 17;
        foreach (ModernLocInfoV1 value in values)
        {
            hash = unchecked(hash * 31 + ModernQueryHashV1.LocInfo(value));
        }

        return hash;
    }
}

public sealed class ModernQueryUInt32VectorPayloadV1 : ModernQueryPayloadV1
{
    private readonly uint[] values;
    private readonly ReadOnlyCollection<uint> valuesView;

    internal ModernQueryUInt32VectorPayloadV1(IEnumerable<uint> values)
    {
        this.values = values.ToArray();
        valuesView = Array.AsReadOnly(this.values);
    }

    public IReadOnlyList<uint> Values => valuesView;

    protected override bool ValueEquals(ModernQueryPayloadV1 other) =>
        other is ModernQueryUInt32VectorPayloadV1 value &&
        values.AsSpan().SequenceEqual(value.values);

    public override int GetHashCode()
    {
        int hash = 17;
        foreach (uint value in values)
        {
            hash = unchecked(hash * 31 + value.GetHashCode());
        }

        return hash;
    }
}

public sealed class ModernQueryPackedUInt32VectorPayloadV1 : ModernQueryPayloadV1
{
    private readonly uint[] values;
    private readonly ReadOnlyCollection<uint> valuesView;

    internal ModernQueryPackedUInt32VectorPayloadV1(IEnumerable<uint> values)
    {
        this.values = values.ToArray();
        valuesView = Array.AsReadOnly(this.values);
    }

    public IReadOnlyList<uint> Values => valuesView;

    protected override bool ValueEquals(ModernQueryPayloadV1 other) =>
        other is ModernQueryPackedUInt32VectorPayloadV1 value &&
        values.AsSpan().SequenceEqual(value.values);

    public override int GetHashCode()
    {
        int hash = 17;
        foreach (uint value in values)
        {
            hash = unchecked(hash * 31 + value.GetHashCode());
        }

        return hash;
    }
}

public sealed class ModernQueryLinkPayloadV1 : ModernQueryPayloadV1
{
    public ModernQueryLinkPayloadV1(uint link, uint linkMarker)
    {
        Link = link;
        LinkMarker = linkMarker;
    }

    public uint Link { get; }

    public uint LinkMarker { get; }

    protected override bool ValueEquals(ModernQueryPayloadV1 other) =>
        other is ModernQueryLinkPayloadV1 value &&
        Link == value.Link && LinkMarker == value.LinkMarker;

    public override int GetHashCode() =>
        unchecked(31 * Link.GetHashCode() + LinkMarker.GetHashCode());
}

public sealed record ModernQueryFieldV1(
    QueryFlagV1 Flag,
    ModernQueryPayloadV1 Payload)
{
    public override int GetHashCode() =>
        unchecked(31 * (int)Flag + Payload.GetHashCode());
}

public sealed class ModernQueryV1 : IEquatable<ModernQueryV1>
{
    private readonly ModernQueryFieldV1[] fields;
    private readonly ReadOnlyCollection<ModernQueryFieldV1> fieldsView;
    private readonly byte[] rawBytes;

    internal ModernQueryV1(
        bool isOnFieldSkipped,
        IEnumerable<ModernQueryFieldV1> fields,
        ReadOnlySpan<byte> rawBytes)
    {
        IsOnFieldSkipped = isOnFieldSkipped;
        this.fields = fields.ToArray();
        fieldsView = Array.AsReadOnly(this.fields);
        this.rawBytes = rawBytes.ToArray();
    }

    public bool IsOnFieldSkipped { get; }

    public IReadOnlyList<ModernQueryFieldV1> Fields => fieldsView;

    public ReadOnlyMemory<byte> RawBytes => rawBytes;

    public bool Equals(ModernQueryV1? other)
    {
        if (other is null || IsOnFieldSkipped != other.IsOnFieldSkipped ||
            fields.Length != other.fields.Length)
        {
            return false;
        }

        return fields.AsSpan().SequenceEqual(other.fields);
    }

    public override bool Equals(object? obj) =>
        obj is ModernQueryV1 other && Equals(other);

    public override int GetHashCode()
    {
        int hash = IsOnFieldSkipped ? 1 : 0;
        foreach (ModernQueryFieldV1 field in fields)
        {
            hash = unchecked(hash * 31 + field.GetHashCode());
        }

        return hash;
    }
}

public readonly record struct ModernQueryDecodeResult(
    bool IsSuccess,
    GameplayErrorCode Error,
    ModernQueryV1? Value)
{
    internal static ModernQueryDecodeResult Success(ModernQueryV1 value) =>
        new(true, GameplayErrorCode.None, value);

    internal static ModernQueryDecodeResult Failure(GameplayErrorCode error) =>
        new(false, error, null);
}

public readonly record struct ModernQueryStreamDecodeResult(
    bool IsSuccess,
    GameplayErrorCode Error,
    IReadOnlyList<ModernQueryV1> Values)
{
    internal static ModernQueryStreamDecodeResult Success(
        IReadOnlyList<ModernQueryV1> values) =>
        new(true, GameplayErrorCode.None, values);

    internal static ModernQueryStreamDecodeResult Failure(
        GameplayErrorCode error) =>
        new(false, error, Array.Empty<ModernQueryV1>());
}

public static class ModernQueryDecoderV1
{
    private const uint LocInfoVectorMaximumCount = 6552;
    private const uint UInt32VectorMaximumCount = 16381;

    public static ModernQueryDecodeResult Decode(ReadOnlySpan<byte> bytes)
    {
        if (!TryDecodeOne(bytes, out ModernQueryV1? value, out int consumed, out GameplayErrorCode error))
        {
            return ModernQueryDecodeResult.Failure(error);
        }

        if (consumed != bytes.Length)
        {
            return ModernQueryDecodeResult.Failure(
                GameplayErrorCode.QueryLengthMismatch);
        }

        return ModernQueryDecodeResult.Success(value!);
    }

    public static ModernQueryStreamDecodeResult DecodeStream(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 4)
        {
            return ModernQueryStreamDecodeResult.Failure(
                GameplayErrorCode.MalformedQuery);
        }

        uint declaredLength = BinaryPrimitives.ReadUInt32LittleEndian(bytes[..4]);
        if (declaredLength != bytes.Length - 4)
        {
            return ModernQueryStreamDecodeResult.Failure(
                GameplayErrorCode.QueryLengthMismatch);
        }

        ReadOnlySpan<byte> body = bytes[4..];
        List<ModernQueryV1> values = new();
        int offset = 0;
        while (offset < body.Length)
        {
            if (!TryDecodeOne(
                    body[offset..],
                    out ModernQueryV1? value,
                    out int consumed,
                    out GameplayErrorCode error))
            {
                return ModernQueryStreamDecodeResult.Failure(error);
            }

            if (consumed <= 0 || consumed > body.Length - offset)
            {
                return ModernQueryStreamDecodeResult.Failure(
                    GameplayErrorCode.QueryLengthMismatch);
            }

            values.Add(value!);
            offset += consumed;
        }

        return ModernQueryStreamDecodeResult.Success(
            Array.AsReadOnly(values.ToArray()));
    }

    private static bool TryDecodeOne(
        ReadOnlySpan<byte> bytes,
        out ModernQueryV1? value,
        out int consumed,
        out GameplayErrorCode error)
    {
        value = null;
        consumed = 0;
        error = GameplayErrorCode.None;

        if (bytes.Length < 2)
        {
            error = GameplayErrorCode.MalformedQuery;
            return false;
        }

        int offset = 0;
        ushort itemSize = BinaryPrimitives.ReadUInt16LittleEndian(bytes[..2]);
        if (itemSize == 0)
        {
            value = new ModernQueryV1(true, Array.Empty<ModernQueryFieldV1>(), bytes[..2]);
            consumed = 2;
            return true;
        }

        List<ModernQueryFieldV1> fields = new();
        HashSet<QueryFlagV1> seenFlags = new();
        while (true)
        {
            if (bytes.Length - offset < 2)
            {
                error = GameplayErrorCode.MalformedQuery;
                return false;
            }

            itemSize = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(offset, 2));
            if (itemSize == 0)
            {
                error = GameplayErrorCode.QueryLengthMismatch;
                return false;
            }

            if (itemSize < 4)
            {
                error = GameplayErrorCode.QueryLengthMismatch;
                return false;
            }

            int recordLength = 2 + itemSize;
            if (recordLength > bytes.Length - offset)
            {
                error = GameplayErrorCode.MalformedQuery;
                return false;
            }

            uint rawFlag = BinaryPrimitives.ReadUInt32LittleEndian(
                bytes.Slice(offset + 2, 4));
            QueryFlagV1 flag = (QueryFlagV1)rawFlag;
            ReadOnlySpan<byte> payload = bytes.Slice(
                offset + 6,
                itemSize - 4);
            if (flag == QueryFlagV1.End)
            {
                if (itemSize != 4 || !payload.IsEmpty)
                {
                    error = GameplayErrorCode.QueryLengthMismatch;
                    return false;
                }

                consumed = offset + recordLength;
                value = new ModernQueryV1(
                    false,
                    fields,
                    bytes[..consumed]);
                return true;
            }

            if (!IsAdmittedDataFlag(flag) || !seenFlags.Add(flag))
            {
                error = !IsAdmittedDataFlag(flag)
                    ? GameplayErrorCode.UnsupportedQueryFlag
                    : GameplayErrorCode.DuplicateQueryFlag;
                return false;
            }

            if (!TryDecodePayload(flag, payload, out ModernQueryPayloadV1? decodedPayload, out error))
            {
                return false;
            }

            fields.Add(new ModernQueryFieldV1(flag, decodedPayload!));
            offset += recordLength;
        }
    }

    private static bool IsAdmittedDataFlag(QueryFlagV1 flag) =>
        flag is QueryFlagV1.Code or
            QueryFlagV1.Position or
            QueryFlagV1.Alias or
            QueryFlagV1.Type or
            QueryFlagV1.Level or
            QueryFlagV1.Rank or
            QueryFlagV1.Attribute or
            QueryFlagV1.Race or
            QueryFlagV1.Attack or
            QueryFlagV1.Defense or
            QueryFlagV1.BaseAttack or
            QueryFlagV1.BaseDefense or
            QueryFlagV1.Reason or
            QueryFlagV1.ReasonCard or
            QueryFlagV1.EquipCard or
            QueryFlagV1.TargetCard or
            QueryFlagV1.OverlayCard or
            QueryFlagV1.Counters or
            QueryFlagV1.Owner or
            QueryFlagV1.Status or
            QueryFlagV1.IsPublic or
            QueryFlagV1.LScale or
            QueryFlagV1.RScale or
            QueryFlagV1.Link or
            QueryFlagV1.IsHidden or
            QueryFlagV1.Cover;

    private static bool TryDecodePayload(
        QueryFlagV1 flag,
        ReadOnlySpan<byte> payload,
        out ModernQueryPayloadV1? value,
        out GameplayErrorCode error)
    {
        value = null;
        error = GameplayErrorCode.None;
        switch (flag)
        {
            case QueryFlagV1.Owner:
            case QueryFlagV1.IsPublic:
            case QueryFlagV1.IsHidden:
                if (payload.Length != 1)
                {
                    error = GameplayErrorCode.QueryLengthMismatch;
                    return false;
                }

                value = new ModernQueryUInt8PayloadV1(payload[0]);
                return true;
            case QueryFlagV1.Race:
                if (payload.Length != 8)
                {
                    error = GameplayErrorCode.QueryLengthMismatch;
                    return false;
                }

                value = new ModernQueryUInt64PayloadV1(
                    BinaryPrimitives.ReadUInt64LittleEndian(payload));
                return true;
            case QueryFlagV1.Attack:
            case QueryFlagV1.Defense:
            case QueryFlagV1.BaseAttack:
            case QueryFlagV1.BaseDefense:
                if (payload.Length != 4)
                {
                    error = GameplayErrorCode.QueryLengthMismatch;
                    return false;
                }

                value = new ModernQueryInt32PayloadV1(
                    BinaryPrimitives.ReadInt32LittleEndian(payload));
                return true;
            case QueryFlagV1.ReasonCard:
            case QueryFlagV1.EquipCard:
                if (payload.Length != GameplayWirePrimitivesV1.ModernLocInfoByteLength ||
                    !GameplayWirePrimitivesV1.TryDecodeModernLocInfo(
                        payload,
                        out ModernLocInfoV1 locInfo,
                        out error))
                {
                    error = GameplayErrorCode.QueryLengthMismatch;
                    return false;
                }

                value = new ModernQueryLocInfoPayloadV1(locInfo);
                return true;
            case QueryFlagV1.TargetCard:
                return TryDecodeLocInfoVector(payload, out value, out error);
            case QueryFlagV1.OverlayCard:
                return TryDecodeUInt32Vector(payload, out value, out error);
            case QueryFlagV1.Counters:
                return TryDecodePackedUInt32Vector(payload, out value, out error);
            case QueryFlagV1.Link:
                if (payload.Length != 8)
                {
                    error = GameplayErrorCode.QueryLengthMismatch;
                    return false;
                }

                value = new ModernQueryLinkPayloadV1(
                    BinaryPrimitives.ReadUInt32LittleEndian(payload[..4]),
                    BinaryPrimitives.ReadUInt32LittleEndian(payload[4..]));
                return true;
            default:
                if (payload.Length != 4)
                {
                    error = GameplayErrorCode.QueryLengthMismatch;
                    return false;
                }

                value = new ModernQueryUInt32PayloadV1(
                    BinaryPrimitives.ReadUInt32LittleEndian(payload));
                return true;
        }
    }

    private static bool TryDecodeLocInfoVector(
        ReadOnlySpan<byte> payload,
        out ModernQueryPayloadV1? value,
        out GameplayErrorCode error)
    {
        value = null;
        error = GameplayErrorCode.None;
        if (!TryReadVectorLength(
                payload,
                LocInfoVectorMaximumCount,
                GameplayWirePrimitivesV1.ModernLocInfoByteLength,
                out uint count,
                out int requiredLength,
                out error))
        {
            return false;
        }

        List<ModernLocInfoV1> values = new((int)count);
        int offset = 4;
        for (uint index = 0; index < count; index++)
        {
            if (!GameplayWirePrimitivesV1.TryDecodeModernLocInfo(
                    payload.Slice(offset, GameplayWirePrimitivesV1.ModernLocInfoByteLength),
                    out ModernLocInfoV1 locInfo,
                    out error))
            {
                return false;
            }

            values.Add(locInfo);
            offset += GameplayWirePrimitivesV1.ModernLocInfoByteLength;
        }

        if (offset != requiredLength)
        {
            error = GameplayErrorCode.QueryLengthMismatch;
            return false;
        }

        value = new ModernQueryLocInfoVectorPayloadV1(values);
        return true;
    }

    private static bool TryDecodeUInt32Vector(
        ReadOnlySpan<byte> payload,
        out ModernQueryPayloadV1? value,
        out GameplayErrorCode error) =>
        TryDecodeUInt32VectorCore(
            payload,
            out value,
            out error,
            packed: false);

    private static bool TryDecodePackedUInt32Vector(
        ReadOnlySpan<byte> payload,
        out ModernQueryPayloadV1? value,
        out GameplayErrorCode error) =>
        TryDecodeUInt32VectorCore(
            payload,
            out value,
            out error,
            packed: true);

    private static bool TryDecodeUInt32VectorCore(
        ReadOnlySpan<byte> payload,
        out ModernQueryPayloadV1? value,
        out GameplayErrorCode error,
        bool packed)
    {
        value = null;
        error = GameplayErrorCode.None;
        if (!TryReadVectorLength(
                payload,
                UInt32VectorMaximumCount,
                4,
                out uint count,
                out int requiredLength,
                out error))
        {
            return false;
        }

        List<uint> values = new((int)count);
        int offset = 4;
        for (uint index = 0; index < count; index++)
        {
            values.Add(BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(offset, 4)));
            offset += 4;
        }

        if (offset != requiredLength)
        {
            error = GameplayErrorCode.QueryLengthMismatch;
            return false;
        }

        value = packed
            ? new ModernQueryPackedUInt32VectorPayloadV1(values)
            : new ModernQueryUInt32VectorPayloadV1(values);
        return true;
    }

    private static bool TryReadVectorLength(
        ReadOnlySpan<byte> payload,
        uint maximumCount,
        int elementWidth,
        out uint count,
        out int requiredLength,
        out GameplayErrorCode error)
    {
        count = 0;
        requiredLength = 0;
        error = GameplayErrorCode.None;
        if (payload.Length < 4)
        {
            error = GameplayErrorCode.QueryLengthMismatch;
            return false;
        }

        count = BinaryPrimitives.ReadUInt32LittleEndian(payload[..4]);
        if (count > maximumCount)
        {
            error = GameplayErrorCode.QueryCountOverflow;
            return false;
        }

        ulong required = 4ul + ((ulong)count * (uint)elementWidth);
        if (required > int.MaxValue)
        {
            error = GameplayErrorCode.ArithmeticFailure;
            return false;
        }

        requiredLength = (int)required;
        if (requiredLength != payload.Length)
        {
            error = GameplayErrorCode.QueryLengthMismatch;
            return false;
        }

        return true;
    }
}

internal static class ModernQueryHashV1
{
    internal static int LocInfo(ModernLocInfoV1 value)
    {
        int hash = 17;
        hash = unchecked(hash * 31 + value.Controller);
        hash = unchecked(hash * 31 + value.Location);
        hash = unchecked(hash * 31 + value.Sequence.GetHashCode());
        return unchecked(hash * 31 + value.Position.GetHashCode());
    }
}
