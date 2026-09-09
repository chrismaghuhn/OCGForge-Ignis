using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace OCGForge.Ignis.Model;

public enum OcgForgePublicChoiceKindV1 : byte
{
    YesNo = 1,
    EffectYesNo = 2,
    EffectChoice = 3,
    OptionValue = 4,
    AnnouncementNumber = 5
}

public readonly record struct OcgForgePublicChoiceV1(
    OcgForgePublicChoiceKindV1 Kind,
    ulong Value,
    uint? ResponseIndex);

public enum OcgForgePublicCardReferenceKindV1 : byte
{
    VisibleCard = 0,
    RedactedSlot = 1
}

public readonly record struct OcgForgePublicCardReferenceV1(
    OcgForgePublicCardReferenceKindV1 Kind,
    string ObservationLocator);

public sealed record OcgForgePublicActionDescriptorV1(
    string ActionKind,
    OcgForgePublicChoiceV1? Choice,
    OcgForgePublicCardReferenceV1? SourceReference,
    OcgForgePublicCardReferenceV1? TargetReference,
    uint? Phase,
    byte? Position,
    uint? SourceIndex,
    int? Amount,
    string ContinuationOperation);

public enum OcgForgePublicActionIdentityErrorCodeV1 : byte
{
    None = 0,
    InvalidInput = 1,
    InvalidActionKind = 2,
    InvalidChoice = 3,
    InvalidReference = 4,
    InvalidContinuationOperation = 5,
    CanonicalizationFailure = 6,
    InvalidCanonicalKey = 7
}

public readonly record struct OcgForgePublicActionIdentityErrorV1(
    OcgForgePublicActionIdentityErrorCodeV1 Code,
    string FieldPath);

public sealed class OcgForgePublicActionIdentityResultV1
{
    private readonly byte[]? canonicalDescriptorBytes;

    private OcgForgePublicActionIdentityResultV1(
        bool isSuccess,
        OcgForgePublicActionIdentityErrorCodeV1 errorCode,
        OcgForgePublicActionIdentityErrorV1? error,
        byte[]? canonicalDescriptorBytes,
        string? publicActionKey)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        Error = error;
        this.canonicalDescriptorBytes = canonicalDescriptorBytes;
        PublicActionKey = publicActionKey;
    }

    public bool IsSuccess { get; }

    public OcgForgePublicActionIdentityErrorCodeV1 ErrorCode { get; }

    public OcgForgePublicActionIdentityErrorV1? Error { get; }

    public byte[]? CanonicalDescriptorBytes =>
        canonicalDescriptorBytes?.ToArray();

    public string? PublicActionKey { get; }

    internal static OcgForgePublicActionIdentityResultV1 Success(
        byte[] bytes,
        string key) =>
        new(
            true,
            OcgForgePublicActionIdentityErrorCodeV1.None,
            null,
            bytes.ToArray(),
            key);

    internal static OcgForgePublicActionIdentityResultV1 Failure(
        OcgForgePublicActionIdentityErrorCodeV1 code,
        string fieldPath) =>
        new(
            false,
            code,
            new OcgForgePublicActionIdentityErrorV1(code, fieldPath),
            null,
            null);
}

public enum OcgForgePublicCandidateDomainErrorCodeV1 : byte
{
    None = 0,
    InvalidRequestKind = 1,
    EmptyDomain = 2,
    InvalidActionKey = 3,
    DuplicateActionKey = 4,
    CanonicalizationFailure = 5
}

public readonly record struct OcgForgePublicCandidateDomainErrorV1(
    OcgForgePublicCandidateDomainErrorCodeV1 Code,
    string FieldPath);

public sealed class OcgForgePublicCandidateDomainResultV1
{
    private readonly byte[]? canonicalBytes;

    private OcgForgePublicCandidateDomainResultV1(
        bool isSuccess,
        OcgForgePublicCandidateDomainErrorCodeV1 errorCode,
        OcgForgePublicCandidateDomainErrorV1? error,
        byte[]? canonicalBytes,
        string? digest)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        Error = error;
        this.canonicalBytes = canonicalBytes;
        Digest = digest;
    }

    public bool IsSuccess { get; }

    public OcgForgePublicCandidateDomainErrorCodeV1 ErrorCode { get; }

    public OcgForgePublicCandidateDomainErrorV1? Error { get; }

    public byte[]? CanonicalBytes => canonicalBytes?.ToArray();

    public string? Digest { get; }

    internal static OcgForgePublicCandidateDomainResultV1 Success(
        byte[] bytes,
        string digest) =>
        new(
            true,
            OcgForgePublicCandidateDomainErrorCodeV1.None,
            null,
            bytes.ToArray(),
            digest);

    internal static OcgForgePublicCandidateDomainResultV1 Failure(
        OcgForgePublicCandidateDomainErrorCodeV1 code,
        string fieldPath) =>
        new(
            false,
            code,
            new OcgForgePublicCandidateDomainErrorV1(code, fieldPath),
            null,
            null);
}

public static class OcgForgePublicActionIdentityV1
{
    public const string SchemaId = "ocgforge.public_action_identity.v1";

    public const string PublicActionKeyPrefix = "public_action.v1.";

    public const string CandidateDomainSchemaId =
        "ocgforge.public_candidate_domain.v1";

    private static readonly Encoding StrictUtf8 =
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static OcgForgePublicActionIdentityResultV1 TryCreate(
        OcgForgePublicActionDescriptorV1? descriptor)
    {
        if (descriptor is null)
        {
            return OcgForgePublicActionIdentityResultV1.Failure(
                OcgForgePublicActionIdentityErrorCodeV1.InvalidInput,
                "descriptor");
        }

        if (!IsLowerToken(descriptor.ActionKind))
        {
            return OcgForgePublicActionIdentityResultV1.Failure(
                OcgForgePublicActionIdentityErrorCodeV1.InvalidActionKind,
                "action_kind");
        }

        if (!string.IsNullOrEmpty(descriptor.ContinuationOperation) &&
            !IsLowerToken(descriptor.ContinuationOperation))
        {
            return OcgForgePublicActionIdentityResultV1.Failure(
                OcgForgePublicActionIdentityErrorCodeV1.InvalidContinuationOperation,
                "continuation_operation");
        }

        if (!TryValidateChoice(descriptor.Choice, out string? choicePath))
        {
            return OcgForgePublicActionIdentityResultV1.Failure(
                OcgForgePublicActionIdentityErrorCodeV1.InvalidChoice,
                choicePath!);
        }

        if (!TryValidateReference(descriptor.SourceReference, out string? sourcePath))
        {
            return OcgForgePublicActionIdentityResultV1.Failure(
                OcgForgePublicActionIdentityErrorCodeV1.InvalidReference,
                sourcePath!);
        }

        if (!TryValidateReference(descriptor.TargetReference, out string? targetPath))
        {
            return OcgForgePublicActionIdentityResultV1.Failure(
                OcgForgePublicActionIdentityErrorCodeV1.InvalidReference,
                targetPath!);
        }

        try
        {
            byte[] bytes = Canonicalize(descriptor);
            string key = PublicActionKeyPrefix +
                Convert.ToHexString(bytes).ToLowerInvariant();
            return OcgForgePublicActionIdentityResultV1.Success(bytes, key);
        }
        catch (EncoderFallbackException)
        {
            return OcgForgePublicActionIdentityResultV1.Failure(
                OcgForgePublicActionIdentityErrorCodeV1.CanonicalizationFailure,
                "descriptor");
        }
        catch (ArgumentException)
        {
            return OcgForgePublicActionIdentityResultV1.Failure(
                OcgForgePublicActionIdentityErrorCodeV1.CanonicalizationFailure,
                "descriptor");
        }
        catch (OverflowException)
        {
            return OcgForgePublicActionIdentityResultV1.Failure(
                OcgForgePublicActionIdentityErrorCodeV1.CanonicalizationFailure,
                "descriptor");
        }
    }

    public static bool IsCanonicalPublicActionKey(string? key)
    {
        if (string.IsNullOrEmpty(key) ||
            !key.StartsWith(PublicActionKeyPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        string encoded = key[PublicActionKeyPrefix.Length..];
        if (encoded.Length == 0 || encoded.Length % 2 != 0 ||
            encoded.Any(character => !IsLowerHex(character)))
        {
            return false;
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromHexString(encoded);
        }
        catch (FormatException)
        {
            return false;
        }

        return IsCanonicalDescriptor(bytes);
    }

    public static OcgForgePublicCandidateDomainResultV1 TryCreateCandidateDomain(
        string? requestKind,
        IReadOnlyList<string>? publicActionKeys)
    {
        if (!IsLowerToken(requestKind))
        {
            return OcgForgePublicCandidateDomainResultV1.Failure(
                OcgForgePublicCandidateDomainErrorCodeV1.InvalidRequestKind,
                "request_kind");
        }

        if (publicActionKeys is null || publicActionKeys.Count == 0)
        {
            return OcgForgePublicCandidateDomainResultV1.Failure(
                OcgForgePublicCandidateDomainErrorCodeV1.EmptyDomain,
                "candidates");
        }

        HashSet<string> seen = new(StringComparer.Ordinal);
        for (int index = 0; index < publicActionKeys.Count; index++)
        {
            string? key = publicActionKeys[index];
            if (!IsCanonicalPublicActionKey(key))
            {
                return OcgForgePublicCandidateDomainResultV1.Failure(
                    OcgForgePublicCandidateDomainErrorCodeV1.InvalidActionKey,
                    $"candidates[{index}].public_action_key");
            }

            if (!seen.Add(key!))
            {
                return OcgForgePublicCandidateDomainResultV1.Failure(
                    OcgForgePublicCandidateDomainErrorCodeV1.DuplicateActionKey,
                    $"candidates[{index}].public_action_key");
            }
        }

        try
        {
            List<byte> bytes = new(64 + (publicActionKeys.Count * 96));
            AppendString(bytes, CandidateDomainSchemaId);
            AppendString(bytes, requestKind!);
            AppendCount(bytes, publicActionKeys.Count);
            foreach (string key in publicActionKeys)
            {
                AppendString(bytes, key);
            }

            byte[] canonical = bytes.ToArray();
            string digest = Convert.ToHexString(SHA256.HashData(canonical))
                .ToLowerInvariant();
            return OcgForgePublicCandidateDomainResultV1.Success(canonical, digest);
        }
        catch (EncoderFallbackException)
        {
            return OcgForgePublicCandidateDomainResultV1.Failure(
                OcgForgePublicCandidateDomainErrorCodeV1.CanonicalizationFailure,
                "candidates");
        }
        catch (ArgumentException)
        {
            return OcgForgePublicCandidateDomainResultV1.Failure(
                OcgForgePublicCandidateDomainErrorCodeV1.CanonicalizationFailure,
                "candidates");
        }
        catch (OverflowException)
        {
            return OcgForgePublicCandidateDomainResultV1.Failure(
                OcgForgePublicCandidateDomainErrorCodeV1.CanonicalizationFailure,
                "candidates");
        }
    }

    private static byte[] Canonicalize(
        OcgForgePublicActionDescriptorV1 descriptor)
    {
        List<byte> bytes = new(224);
        AppendString(bytes, SchemaId);
        AppendString(bytes, SchemaId);
        AppendString(bytes, descriptor.ActionKind);
        AppendChoice(bytes, descriptor.Choice);
        AppendReference(bytes, descriptor.SourceReference);
        AppendReference(bytes, descriptor.TargetReference);
        AppendOptionalUInt32(bytes, descriptor.Phase);
        AppendOptionalByte(bytes, descriptor.Position);
        AppendOptionalUInt32(bytes, descriptor.SourceIndex);
        AppendOptionalInt32(bytes, descriptor.Amount);
        AppendString(bytes, descriptor.ContinuationOperation);
        return bytes.ToArray();
    }

    private static bool IsCanonicalDescriptor(byte[] bytes)
    {
        try
        {
            Cursor cursor = new(bytes);
            if (!cursor.ReadString(out string? value) || value != SchemaId ||
                !cursor.ReadString(out value) || value != SchemaId ||
                !cursor.ReadString(out value) || !IsLowerToken(value))
            {
                return false;
            }

            if (!cursor.ReadOptionalChoice())
            {
                return false;
            }

            if (!cursor.ReadOptionalReference() ||
                !cursor.ReadOptionalReference() ||
                !cursor.ReadOptionalUInt32() ||
                !cursor.ReadOptionalByte() ||
                !cursor.ReadOptionalUInt32() ||
                !cursor.ReadOptionalUInt32() ||
                !cursor.ReadString(out value) ||
                (!string.IsNullOrEmpty(value) && !IsLowerToken(value)))
            {
                return false;
            }

            return cursor.IsAtEnd;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool TryValidateChoice(
        OcgForgePublicChoiceV1? choice,
        out string? fieldPath)
    {
        fieldPath = null;
        if (!choice.HasValue)
        {
            return true;
        }

        OcgForgePublicChoiceV1 value = choice.Value;
        bool valid = value.Kind switch
        {
            OcgForgePublicChoiceKindV1.YesNo or
            OcgForgePublicChoiceKindV1.EffectYesNo =>
                value.Value <= 1 && !value.ResponseIndex.HasValue,
            OcgForgePublicChoiceKindV1.EffectChoice =>
                value.Value <= uint.MaxValue && !value.ResponseIndex.HasValue,
            OcgForgePublicChoiceKindV1.OptionValue or
            OcgForgePublicChoiceKindV1.AnnouncementNumber =>
                value.ResponseIndex.HasValue,
            _ => false
        };
        if (!valid)
        {
            fieldPath = "choice";
        }

        return valid;
    }

    private static bool TryValidateReference(
        OcgForgePublicCardReferenceV1? reference,
        out string? fieldPath)
    {
        fieldPath = null;
        if (!reference.HasValue)
        {
            return true;
        }

        OcgForgePublicCardReferenceV1 value = reference.Value;
        if (value.Kind is not
            (OcgForgePublicCardReferenceKindV1.VisibleCard or
             OcgForgePublicCardReferenceKindV1.RedactedSlot) ||
            !IsObservationLocator(value.ObservationLocator))
        {
            fieldPath = "reference";
            return false;
        }

        return true;
    }

    private static bool IsObservationLocator(string? value) =>
        !string.IsNullOrEmpty(value) &&
        value.All(character => character >= 0x20 && character != 0x7f);

    private static bool IsLowerToken(string? value) =>
        !string.IsNullOrEmpty(value) &&
        value.All(character =>
            character is >= 'a' and <= 'z' or
            >= '0' and <= '9' or '_');

    private static bool IsLowerHex(char value) =>
        value is >= '0' and <= '9' or >= 'a' and <= 'f';

    private static void AppendChoice(
        List<byte> bytes,
        OcgForgePublicChoiceV1? choice)
    {
        bytes.Add(choice.HasValue ? (byte)1 : (byte)0);
        if (!choice.HasValue)
        {
            return;
        }

        OcgForgePublicChoiceV1 value = choice.Value;
        AppendByte(bytes, (byte)value.Kind);
        AppendUInt64(bytes, value.Value);
        bytes.Add(value.ResponseIndex.HasValue ? (byte)1 : (byte)0);
        if (value.ResponseIndex.HasValue)
        {
            AppendUInt32(bytes, value.ResponseIndex.Value);
        }
    }

    private static void AppendReference(
        List<byte> bytes,
        OcgForgePublicCardReferenceV1? reference)
    {
        bytes.Add(reference.HasValue ? (byte)1 : (byte)0);
        if (!reference.HasValue)
        {
            return;
        }

        AppendByte(bytes, (byte)reference.Value.Kind);
        AppendString(bytes, reference.Value.ObservationLocator);
    }

    private static void AppendOptionalUInt32(List<byte> bytes, uint? value)
    {
        bytes.Add(value.HasValue ? (byte)1 : (byte)0);
        if (value.HasValue)
        {
            AppendUInt32(bytes, value.Value);
        }
    }

    private static void AppendOptionalByte(List<byte> bytes, byte? value)
    {
        bytes.Add(value.HasValue ? (byte)1 : (byte)0);
        if (value.HasValue)
        {
            AppendByte(bytes, value.Value);
        }
    }

    private static void AppendOptionalInt32(List<byte> bytes, int? value)
    {
        bytes.Add(value.HasValue ? (byte)1 : (byte)0);
        if (value.HasValue)
        {
            AppendUInt32(bytes, unchecked((uint)value.GetValueOrDefault()));
        }
    }

    private static void AppendString(List<byte> bytes, string value)
    {
        byte[] encoded = StrictUtf8.GetBytes(value ?? throw new ArgumentNullException(nameof(value)));
        AppendCount(bytes, encoded.Length);
        bytes.AddRange(encoded);
    }

    private static void AppendCount(List<byte> bytes, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        AppendUInt32(bytes, checked((uint)count));
    }

    private static void AppendByte(List<byte> bytes, byte value) => bytes.Add(value);

    private static void AppendUInt32(List<byte> bytes, uint value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
        bytes.AddRange(buffer.ToArray());
    }

    private static void AppendUInt64(List<byte> bytes, ulong value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64BigEndian(buffer, value);
        bytes.AddRange(buffer.ToArray());
    }

    private sealed class Cursor
    {
        private readonly byte[] bytes;
        private int offset;

        internal Cursor(byte[] bytes)
        {
            this.bytes = bytes;
        }

        internal bool IsAtEnd => offset == bytes.Length;

        internal bool ReadByte(out byte value)
        {
            value = 0;
            if (offset >= bytes.Length)
            {
                return false;
            }

            value = bytes[offset++];
            return true;
        }

        internal bool ReadUInt32(out uint value)
        {
            value = 0;
            if (bytes.Length - offset < sizeof(uint))
            {
                return false;
            }

            value = BinaryPrimitives.ReadUInt32BigEndian(
                bytes.AsSpan(offset, sizeof(uint)));
            offset += sizeof(uint);
            return true;
        }

        internal bool ReadUInt64(out ulong value)
        {
            value = 0;
            if (bytes.Length - offset < sizeof(ulong))
            {
                return false;
            }

            value = BinaryPrimitives.ReadUInt64BigEndian(
                bytes.AsSpan(offset, sizeof(ulong)));
            offset += sizeof(ulong);
            return true;
        }

        internal bool ReadString(out string? value)
        {
            value = null;
            if (!ReadUInt32(out uint length) ||
                length > int.MaxValue ||
                length > bytes.Length - offset)
            {
                return false;
            }

            try
            {
                value = StrictUtf8.GetString(bytes, offset, checked((int)length));
            }
            catch (DecoderFallbackException)
            {
                return false;
            }

            offset += checked((int)length);
            return true;
        }

        internal bool ReadOptionalChoice()
        {
            if (!ReadByte(out byte present) || present > 1)
            {
                return false;
            }

            if (present == 0)
            {
                return true;
            }

            if (!ReadByte(out byte kind) ||
                !ReadUInt64(out ulong value) ||
                !ReadByte(out byte responsePresent) ||
                responsePresent > 1)
            {
                return false;
            }

            if (responsePresent == 1 && !ReadUInt32(out _))
            {
                return false;
            }

            return kind switch
            {
                (byte)OcgForgePublicChoiceKindV1.YesNo or
                (byte)OcgForgePublicChoiceKindV1.EffectYesNo =>
                    value <= 1 && responsePresent == 0,
                (byte)OcgForgePublicChoiceKindV1.EffectChoice =>
                    value <= uint.MaxValue && responsePresent == 0,
                (byte)OcgForgePublicChoiceKindV1.OptionValue or
                (byte)OcgForgePublicChoiceKindV1.AnnouncementNumber =>
                    responsePresent == 1,
                _ => false
            };
        }

        internal bool ReadOptionalReference()
        {
            if (!ReadByte(out byte present) || present > 1)
            {
                return false;
            }

            if (present == 0)
            {
                return true;
            }

            if (!ReadByte(out byte kind) || kind > 1 ||
                !ReadString(out string? locator))
            {
                return false;
            }

            return IsObservationLocator(locator);
        }

        internal bool ReadOptionalUInt32()
        {
            if (!ReadByte(out byte present) || present > 1)
            {
                return false;
            }

            return present == 0 || ReadUInt32(out _);
        }

        internal bool ReadOptionalByte()
        {
            if (!ReadByte(out byte present) || present > 1)
            {
                return false;
            }

            return present == 0 || ReadByte(out _);
        }
    }
}
