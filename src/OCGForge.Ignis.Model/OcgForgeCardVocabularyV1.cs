using System.Security.Cryptography;

namespace OCGForge.Ignis.Model;

public enum OcgForgeCardVocabularyErrorCodeV1 : byte
{
    InvalidPasscodeList = 0,
    InternalFailure = 1
}

public readonly record struct OcgForgeCardVocabularyErrorV1(
    OcgForgeCardVocabularyErrorCodeV1 Code,
    string FieldPath);

public sealed class OcgForgeCardVocabularyResultV1
{
    private OcgForgeCardVocabularyResultV1(
        bool isSuccess,
        OcgForgeCardVocabularyErrorV1? error,
        OcgForgeCardVocabularyV1? value)
    {
        IsSuccess = isSuccess;
        Error = error;
        Value = value;
    }

    public bool IsSuccess { get; }

    public OcgForgeCardVocabularyErrorV1? Error { get; }

    public OcgForgeCardVocabularyV1? Value { get; }

    internal static OcgForgeCardVocabularyResultV1 Success(
        OcgForgeCardVocabularyV1 value) =>
        new(true, null, value);

    internal static OcgForgeCardVocabularyResultV1 Failure(
        OcgForgeCardVocabularyErrorCodeV1 code,
        string fieldPath) =>
        new(false, new(code, fieldPath), null);
}

public sealed class OcgForgeCardVocabularyV1
{
    private readonly uint[] ascendingPasscodes;
    private readonly IReadOnlyList<uint> ascendingPasscodesView;

    private OcgForgeCardVocabularyV1(IEnumerable<uint> passcodes)
    {
        ascendingPasscodes = passcodes.ToArray();
        ascendingPasscodesView = Array.AsReadOnly(ascendingPasscodes);
    }

    public const string SchemaId = "ocgforge.model_card_vocabulary.v1";

    public const string IdentityPrefix = "model_card_vocabulary.v1.";

    public const uint PadId = 0;

    public const uint UnknownOrRedactedId = 1;

    public IReadOnlyList<uint> AscendingPasscodes => ascendingPasscodesView;

    public uint? IdForPublicPasscode(uint publicPasscode)
    {
        int index = Array.BinarySearch(ascendingPasscodes, publicPasscode);
        return index < 0 ? null : checked((uint)index + 2U);
    }

    public byte[] CanonicalBytes
    {
        get
        {
            OcgForgeI6ECanonicalV1.Writer writer = new();
            writer.String(SchemaId);
            writer.String(SchemaId);
            writer.String("ascending_public_passcode_rank_plus_two");
            writer.Count(ascendingPasscodes.Length);
            foreach (uint passcode in ascendingPasscodes)
            {
                writer.U32(passcode);
            }

            return writer.ToArray();
        }
    }

    public string Identity =>
        IdentityPrefix + Convert.ToHexString(SHA256.HashData(CanonicalBytes))
            .ToLowerInvariant();

    public static OcgForgeCardVocabularyResultV1 TryCreate(
        IEnumerable<uint>? ascendingPasscodes)
    {
        if (ascendingPasscodes is null)
        {
            return OcgForgeCardVocabularyResultV1.Failure(
                OcgForgeCardVocabularyErrorCodeV1.InvalidPasscodeList,
                "passcodes");
        }

        try
        {
            uint[] values = ascendingPasscodes.ToArray();
            for (int index = 0; index < values.Length; index++)
            {
                if (values[index] == 0 ||
                    (index > 0 && values[index - 1] >= values[index]))
                {
                    return OcgForgeCardVocabularyResultV1.Failure(
                        OcgForgeCardVocabularyErrorCodeV1.InvalidPasscodeList,
                        $"passcodes[{index}]");
                }
            }

            return OcgForgeCardVocabularyResultV1.Success(
                new OcgForgeCardVocabularyV1(values));
        }
        catch (OverflowException)
        {
            return OcgForgeCardVocabularyResultV1.Failure(
                OcgForgeCardVocabularyErrorCodeV1.InvalidPasscodeList,
                "passcodes");
        }
    }

    public static OcgForgeCardVocabularyResultV1 TryCreateAscending(
        IEnumerable<uint>? ascendingPasscodes) =>
        TryCreate(ascendingPasscodes);
}
