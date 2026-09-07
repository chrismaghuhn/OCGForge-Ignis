using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace OCGForge.Ignis.Gameplay;

public enum PerspectiveSafePrintedProviderErrorCodeV1 : byte
{
    InvalidManifest = 1,
    InvalidArtifactHash = 2,
    MalformedArtifact = 3,
    InvalidRowCanonicality = 4,
    InvalidCoverage = 5,
    SemanticDigestMismatch = 6,
    EnvironmentMismatch = 7,
    InvalidPasscode = 8,
    MissingCoverage = 9
}

public readonly record struct PerspectiveSafePrintedProviderErrorV1(
    PerspectiveSafePrintedProviderErrorCodeV1 Code);

public sealed class PerspectiveSafePrintedProviderManifestV1
{
    private readonly uint[] coveragePasscodes;
    private readonly ReadOnlyCollection<uint> coveragePasscodesView;

    public PerspectiveSafePrintedProviderManifestV1(
        string? manifestContractId,
        string? providerContractId,
        string? semanticRowsContractId,
        string? fieldMappingContractId,
        string? coverageContractId,
        string? coverageDigestSha256,
        string? semanticRowsDigestSha256,
        string? ocgForgeSemanticCommit,
        string? rulesBundleId,
        string? babelCdbRepository,
        string? babelCdbCommit,
        string? babelCdbCheckoutSha256,
        string? cardsCdbSha256,
        string? transformationSourceRepository,
        string? transformationSourceCommit,
        string? transformationSourcePath,
        string? transformationFileSha256,
        string? sourceArtifactFormatId,
        string? sourceArtifactSha256,
        IEnumerable<uint>? coveragePasscodes)
    {
        ManifestContractId = manifestContractId ?? string.Empty;
        ProviderContractId = providerContractId ?? string.Empty;
        SemanticRowsContractId = semanticRowsContractId ?? string.Empty;
        FieldMappingContractId = fieldMappingContractId ?? string.Empty;
        CoverageContractId = coverageContractId ?? string.Empty;
        CoverageDigestSha256 = coverageDigestSha256 ?? string.Empty;
        SemanticRowsDigestSha256 = semanticRowsDigestSha256 ?? string.Empty;
        OcgForgeSemanticCommit = ocgForgeSemanticCommit ?? string.Empty;
        RulesBundleId = rulesBundleId ?? string.Empty;
        BabelCdbRepository = babelCdbRepository ?? string.Empty;
        BabelCdbCommit = babelCdbCommit ?? string.Empty;
        BabelCdbCheckoutSha256 = babelCdbCheckoutSha256 ?? string.Empty;
        CardsCdbSha256 = cardsCdbSha256 ?? string.Empty;
        TransformationSourceRepository = transformationSourceRepository ?? string.Empty;
        TransformationSourceCommit = transformationSourceCommit ?? string.Empty;
        TransformationSourcePath = transformationSourcePath ?? string.Empty;
        TransformationFileSha256 = transformationFileSha256 ?? string.Empty;
        SourceArtifactFormatId = sourceArtifactFormatId ?? string.Empty;
        SourceArtifactSha256 = sourceArtifactSha256 ?? string.Empty;
        this.coveragePasscodes = coveragePasscodes?.ToArray() ?? Array.Empty<uint>();
        coveragePasscodesView = Array.AsReadOnly(this.coveragePasscodes);
    }

    public string ManifestContractId { get; }

    public string ProviderContractId { get; }

    public string SemanticRowsContractId { get; }

    public string FieldMappingContractId { get; }

    public string CoverageContractId { get; }

    public string CoverageDigestSha256 { get; }

    public string SemanticRowsDigestSha256 { get; }

    public string OcgForgeSemanticCommit { get; }

    public string RulesBundleId { get; }

    public string BabelCdbRepository { get; }

    public string BabelCdbCommit { get; }

    public string BabelCdbCheckoutSha256 { get; }

    public string CardsCdbSha256 { get; }

    public string TransformationSourceRepository { get; }

    public string TransformationSourceCommit { get; }

    public string TransformationSourcePath { get; }

    public string TransformationFileSha256 { get; }

    public string SourceArtifactFormatId { get; }

    public string SourceArtifactSha256 { get; }

    public IReadOnlyList<uint> CoveragePasscodes => coveragePasscodesView;
}

public sealed class PerspectiveSafePrintedProviderResultV1
{
    private PerspectiveSafePrintedProviderResultV1(
        PerspectiveSafePrintedProviderV1? provider,
        PerspectiveSafePrintedProviderErrorV1? error)
    {
        if ((provider is null) == (error is null))
        {
            throw new ArgumentException(
                "A printed provider result must contain exactly one outcome.");
        }

        Provider = provider;
        Error = error;
    }

    public PerspectiveSafePrintedProviderV1? Provider { get; }

    public PerspectiveSafePrintedProviderErrorV1? Error { get; }

    public bool IsSuccess => Provider is not null && Error is null;

    internal static PerspectiveSafePrintedProviderResultV1 Success(
        PerspectiveSafePrintedProviderV1 provider) =>
        new(provider, null);

    internal static PerspectiveSafePrintedProviderResultV1 Failure(
        PerspectiveSafePrintedProviderErrorCodeV1 code) =>
        new(null, new PerspectiveSafePrintedProviderErrorV1(code));
}

public sealed class PerspectiveSafePrintedLookupResultV1
{
    private PerspectiveSafePrintedLookupResultV1(
        PerspectiveSafeCardPropertiesV1? properties,
        PerspectiveSafePrintedProviderErrorV1? error)
    {
        if ((properties is null) == (error is null))
        {
            throw new ArgumentException(
                "A printed lookup result must contain exactly one outcome.");
        }

        Properties = properties;
        Error = error;
    }

    public PerspectiveSafeCardPropertiesV1? Properties { get; }

    public PerspectiveSafePrintedProviderErrorV1? Error { get; }

    public bool IsSuccess => Properties is not null && Error is null;

    internal static PerspectiveSafePrintedLookupResultV1 Success(
        PerspectiveSafeCardPropertiesV1 properties) =>
        new(properties, null);

    internal static PerspectiveSafePrintedLookupResultV1 Failure(
        PerspectiveSafePrintedProviderErrorCodeV1 code) =>
        new(null, new PerspectiveSafePrintedProviderErrorV1(code));
}

public sealed class PerspectiveSafePrintedProviderV1
{
    private const string ExpectedManifestContractId =
        "ocgforge-ignis.i6c5.printed-manifest.v1";
    private const string ExpectedProviderContractId =
        "ocgforge-ignis.i6c5.printed-provider.v1";
    private const string ExpectedSemanticRowsContractId =
        "ocgforge-ignis.i6c5.printed-semantic-rows.v1";
    private const string ExpectedFieldMappingContractId =
        "ocgforge-ignis.i6c5.printed-field-mapping.v1";
    private const string ExpectedCoverageContractId =
        "ocgforge-ignis.i6c5.printed-coverage.v1";
    private const string ExpectedSourceArtifactFormatId =
        "ocgforge-ignis.i6c5.printed-source-artifact.pipe12.v1";
    private const string ExpectedOcgForgeSemanticCommit =
        "f929de0b4d4157327dba003067d2e21e42f7ad75";
    private const string ExpectedRulesBundleId =
        "3adfe6b4cfe2c2805e50b389fc0eb4e70a3b0b6107436614d328fddc865e585f";
    private const string ExpectedBabelCdbRepository =
        "https://github.com/ProjectIgnis/BabelCDB.git";
    private const string ExpectedTransformationRepository =
        "https://github.com/chrismaghuhn/OCGForge.git";
    private const string ExpectedTransformationPath =
        "tools/prepare_card_data.py";
    private const string ExpectedRowsDomain =
        "OCGFORGE-IGNIS-I6C5-PRINTED-ROWS-V1\0";
    private const string ExpectedCoverageDomain =
        "OCGFORGE-IGNIS-I6C5-PRINTED-COVERAGE-V1\0";
    private const string ExpectedArtifactHeader =
        "# code|alias|setcode|type|level|attribute|race|atk|def|lscale|rscale|link_marker";
    private const uint TypeXyz = 0x00800000;
    private const uint TypePendulum = 0x01000000;
    private const uint TypeLink = 0x04000000;
    private const uint LinkMarkerBottomLeft = 0x001;
    private const uint LinkMarkerBottom = 0x002;
    private const uint LinkMarkerBottomRight = 0x004;
    private const uint LinkMarkerLeft = 0x008;
    private const uint LinkMarkerRight = 0x020;
    private const uint LinkMarkerTopLeft = 0x040;
    private const uint LinkMarkerTop = 0x080;
    private const uint LinkMarkerTopRight = 0x100;
    private const uint RecognizedLinkMarkers =
        LinkMarkerBottomLeft |
        LinkMarkerBottom |
        LinkMarkerBottomRight |
        LinkMarkerLeft |
        LinkMarkerRight |
        LinkMarkerTopLeft |
        LinkMarkerTop |
        LinkMarkerTopRight;

    private static readonly Encoding StrictUtf8 =
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private readonly ReadOnlyDictionary<uint, PerspectiveSafeCardPropertiesV1> values;

    private PerspectiveSafePrintedProviderV1(
        PerspectiveSafePrintedProviderManifestV1 manifest,
        Dictionary<uint, PerspectiveSafeCardPropertiesV1> values)
    {
        Manifest = manifest;
        this.values = new ReadOnlyDictionary<uint, PerspectiveSafeCardPropertiesV1>(values);
    }

    public PerspectiveSafePrintedProviderManifestV1 Manifest { get; }

    public static PerspectiveSafePrintedProviderResultV1 TryCreate(
        ReadOnlyMemory<byte> artifactBytes,
        PerspectiveSafePrintedProviderManifestV1? manifest)
    {
        if (manifest is null || !TryValidateManifestShape(manifest))
        {
            return PerspectiveSafePrintedProviderResultV1.Failure(
                PerspectiveSafePrintedProviderErrorCodeV1.InvalidManifest);
        }

        if (manifest.OcgForgeSemanticCommit != ExpectedOcgForgeSemanticCommit ||
            manifest.RulesBundleId != ExpectedRulesBundleId)
        {
            return PerspectiveSafePrintedProviderResultV1.Failure(
                PerspectiveSafePrintedProviderErrorCodeV1.EnvironmentMismatch);
        }

        string actualArtifactHash = HashHex(artifactBytes.Span);
        if (!string.Equals(
                actualArtifactHash,
                manifest.SourceArtifactSha256,
                StringComparison.Ordinal))
        {
            return PerspectiveSafePrintedProviderResultV1.Failure(
                PerspectiveSafePrintedProviderErrorCodeV1.InvalidArtifactHash);
        }

        if (!TryParseRows(
                artifactBytes.Span,
                out List<PrintedSemanticRow> rows))
        {
            return PerspectiveSafePrintedProviderResultV1.Failure(
                PerspectiveSafePrintedProviderErrorCodeV1.MalformedArtifact);
        }

        if (!CoverageMatchesRows(manifest.CoveragePasscodes, rows))
        {
            return PerspectiveSafePrintedProviderResultV1.Failure(
                PerspectiveSafePrintedProviderErrorCodeV1.InvalidCoverage);
        }

        string coverageDigest = ComputeCoverageDigest(manifest.CoveragePasscodes);
        if (!string.Equals(
                coverageDigest,
                manifest.CoverageDigestSha256,
                StringComparison.Ordinal))
        {
            return PerspectiveSafePrintedProviderResultV1.Failure(
                PerspectiveSafePrintedProviderErrorCodeV1.InvalidCoverage);
        }

        string semanticDigest = ComputeSemanticRowsDigest(rows);
        if (!string.Equals(
                semanticDigest,
                manifest.SemanticRowsDigestSha256,
                StringComparison.Ordinal))
        {
            return PerspectiveSafePrintedProviderResultV1.Failure(
                PerspectiveSafePrintedProviderErrorCodeV1.SemanticDigestMismatch);
        }

        Dictionary<uint, PerspectiveSafeCardPropertiesV1> values =
            rows.ToDictionary(row => row.Code, CreatePrintedProperties);
        return PerspectiveSafePrintedProviderResultV1.Success(
            new PerspectiveSafePrintedProviderV1(manifest, values));
    }

    public PerspectiveSafePrintedLookupResultV1 TryGetPrinted(uint passcode)
    {
        if (passcode == 0)
        {
            return PerspectiveSafePrintedLookupResultV1.Failure(
                PerspectiveSafePrintedProviderErrorCodeV1.InvalidPasscode);
        }

        return values.TryGetValue(passcode, out PerspectiveSafeCardPropertiesV1? properties)
            ? PerspectiveSafePrintedLookupResultV1.Success(properties)
            : PerspectiveSafePrintedLookupResultV1.Failure(
                PerspectiveSafePrintedProviderErrorCodeV1.MissingCoverage);
    }

    private static bool TryValidateManifestShape(
        PerspectiveSafePrintedProviderManifestV1 manifest)
    {
        return manifest.ManifestContractId == ExpectedManifestContractId &&
               manifest.ProviderContractId == ExpectedProviderContractId &&
               manifest.SemanticRowsContractId == ExpectedSemanticRowsContractId &&
               manifest.FieldMappingContractId == ExpectedFieldMappingContractId &&
               manifest.CoverageContractId == ExpectedCoverageContractId &&
               manifest.BabelCdbRepository == ExpectedBabelCdbRepository &&
               manifest.TransformationSourceRepository == ExpectedTransformationRepository &&
               manifest.TransformationSourcePath == ExpectedTransformationPath &&
               manifest.SourceArtifactFormatId == ExpectedSourceArtifactFormatId &&
               IsSha256(manifest.CoverageDigestSha256) &&
               IsSha256(manifest.SemanticRowsDigestSha256) &&
               IsSha256(manifest.BabelCdbCheckoutSha256) &&
               IsSha256(manifest.CardsCdbSha256) &&
               IsSha256(manifest.TransformationFileSha256) &&
               IsSha256(manifest.SourceArtifactSha256) &&
               IsSha1(manifest.BabelCdbCommit) &&
               IsSha1(manifest.TransformationSourceCommit) &&
               IsCanonicalCoverage(manifest.CoveragePasscodes);
    }

    private static bool TryParseRows(
        ReadOnlySpan<byte> artifactBytes,
        out List<PrintedSemanticRow> rows)
    {
        rows = new();
        if (artifactBytes.Length >= 3 &&
            artifactBytes[0] == 0xef &&
            artifactBytes[1] == 0xbb &&
            artifactBytes[2] == 0xbf)
        {
            return false;
        }

        string text;
        try
        {
            text = StrictUtf8.GetString(artifactBytes);
        }
        catch (DecoderFallbackException)
        {
            return false;
        }

        if (text.Contains('\r') || text.Contains('\0'))
        {
            return false;
        }

        string[] lines = text.Split('\n');
        if (lines.Length == 0 || lines[0] != ExpectedArtifactHeader)
        {
            return false;
        }

        int lastLine = lines.Length;
        if (lastLine > 1 && lines[lastLine - 1].Length == 0)
        {
            lastLine--;
        }

        uint previousCode = 0;
        for (int lineIndex = 1; lineIndex < lastLine; lineIndex++)
        {
            string line = lines[lineIndex];
            string[] fields = line.Split('|');
            if (fields.Length != 12 ||
                !TryParseUInt32(fields[0], out uint code) ||
                !TryParseUInt32(fields[1], out _) ||
                !TryParseUInt64(fields[2], out _) ||
                !TryParseUInt32(fields[3], out uint type) ||
                !TryParseUInt32(fields[4], out uint level) ||
                !TryParseUInt32(fields[5], out uint attribute) ||
                !TryParseUInt64(fields[6], out ulong race) ||
                !TryParseInt32(fields[7], out int attack) ||
                !TryParseInt32(fields[8], out int defense) ||
                !TryParseUInt32(fields[9], out uint leftScale) ||
                !TryParseUInt32(fields[10], out uint rightScale) ||
                !TryParseUInt32(fields[11], out uint linkMarker) ||
                code == 0 ||
                (rows.Count != 0 && code <= previousCode) ||
                level > byte.MaxValue ||
                ((type & TypeLink) == 0 && linkMarker != 0) ||
                ((type & TypeLink) != 0 && defense != 0) ||
                ((type & TypePendulum) == 0 &&
                 (leftScale != 0 || rightScale != 0)) ||
                ((type & TypePendulum) != 0 &&
                 (leftScale > byte.MaxValue || rightScale > byte.MaxValue)) ||
                ((type & TypeLink) != 0 &&
                 (linkMarker & ~RecognizedLinkMarkers) != 0))
            {
                rows.Clear();
                return false;
            }

            rows.Add(new PrintedSemanticRow(
                code,
                type,
                level,
                attribute,
                race,
                attack,
                defense,
                leftScale,
                rightScale,
                linkMarker));
            previousCode = code;
        }

        return rows.Count != 0;
    }

    private static bool CoverageMatchesRows(
        IReadOnlyList<uint> coverage,
        IReadOnlyList<PrintedSemanticRow> rows)
    {
        if (coverage.Count != rows.Count)
        {
            return false;
        }

        for (int index = 0; index < coverage.Count; index++)
        {
            if (coverage[index] != rows[index].Code)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsCanonicalCoverage(IReadOnlyList<uint> coverage)
    {
        if (coverage.Count == 0 || coverage[0] == 0)
        {
            return false;
        }

        for (int index = 1; index < coverage.Count; index++)
        {
            if (coverage[index] <= coverage[index - 1])
            {
                return false;
            }
        }

        return true;
    }

    private static string ComputeCoverageDigest(IReadOnlyList<uint> coverage)
    {
        using MemoryStream stream = new();
        byte[] domain = Encoding.ASCII.GetBytes(ExpectedCoverageDomain);
        stream.Write(domain);
        WriteUInt32BigEndian(stream, checked((uint)coverage.Count));
        foreach (uint passcode in coverage)
        {
            WriteUInt32BigEndian(stream, passcode);
        }

        return HashHex(stream.ToArray());
    }

    private static string ComputeSemanticRowsDigest(
        IReadOnlyList<PrintedSemanticRow> rows)
    {
        using MemoryStream stream = new();
        byte[] domain = Encoding.ASCII.GetBytes(ExpectedRowsDomain);
        stream.Write(domain);
        WriteUInt32BigEndian(stream, checked((uint)rows.Count));
        foreach (PrintedSemanticRow row in rows)
        {
            WriteUInt32BigEndian(stream, row.Code);
            WriteUInt32BigEndian(stream, row.Type);
            WriteUInt32BigEndian(stream, row.Level);
            WriteUInt32BigEndian(stream, row.Attribute);
            WriteUInt64BigEndian(stream, row.Race);
            WriteInt32BigEndian(stream, row.Attack);
            WriteInt32BigEndian(stream, row.Defense);
            WriteUInt32BigEndian(stream, row.LeftScale);
            WriteUInt32BigEndian(stream, row.RightScale);
            WriteUInt32BigEndian(stream, row.LinkMarker);
        }

        return HashHex(stream.ToArray());
    }

    private static PerspectiveSafeCardPropertiesV1 CreatePrintedProperties(
        PrintedSemanticRow row)
    {
        uint? level = null;
        uint? rank = null;
        uint? linkRating = null;
        if ((row.Type & TypeXyz) != 0)
        {
            rank = row.Level;
        }
        else if ((row.Type & TypeLink) == 0)
        {
            level = row.Level;
        }

        if ((row.Type & TypeLink) != 0)
        {
            linkRating = row.Level;
        }

        uint? leftScale = (row.Type & TypePendulum) != 0
            ? row.LeftScale
            : null;
        uint? rightScale = (row.Type & TypePendulum) != 0
            ? row.RightScale
            : null;
        int? defense = (row.Type & TypeLink) == 0
            ? row.Defense
            : null;

        return new PerspectiveSafeCardPropertiesV1(
            type: row.Type,
            attribute: row.Attribute,
            race: row.Race,
            attack: row.Attack,
            defense: defense,
            level: level,
            rank: rank,
            linkRating: linkRating,
            linkMarkers: CreateLinkMarkers(row.LinkMarker),
            leftScale: leftScale,
            rightScale: rightScale);
    }

    private static List<PerspectiveSafeLinkMarkerV1> CreateLinkMarkers(
        uint bits)
    {
        List<PerspectiveSafeLinkMarkerV1> markers = new();
        AddMarker(bits, LinkMarkerBottomLeft, PerspectiveSafeLinkMarkerV1.BottomLeft, markers);
        AddMarker(bits, LinkMarkerBottom, PerspectiveSafeLinkMarkerV1.Bottom, markers);
        AddMarker(bits, LinkMarkerBottomRight, PerspectiveSafeLinkMarkerV1.BottomRight, markers);
        AddMarker(bits, LinkMarkerLeft, PerspectiveSafeLinkMarkerV1.Left, markers);
        AddMarker(bits, LinkMarkerRight, PerspectiveSafeLinkMarkerV1.Right, markers);
        AddMarker(bits, LinkMarkerTopLeft, PerspectiveSafeLinkMarkerV1.TopLeft, markers);
        AddMarker(bits, LinkMarkerTop, PerspectiveSafeLinkMarkerV1.Top, markers);
        AddMarker(bits, LinkMarkerTopRight, PerspectiveSafeLinkMarkerV1.TopRight, markers);
        return markers;
    }

    private static void AddMarker(
        uint bits,
        uint marker,
        PerspectiveSafeLinkMarkerV1 value,
        List<PerspectiveSafeLinkMarkerV1> output)
    {
        if ((bits & marker) != 0)
        {
            output.Add(value);
        }
    }

    private static bool TryParseUInt32(string value, out uint result) =>
        uint.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out result);

    private static bool TryParseUInt64(string value, out ulong result) =>
        ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out result);

    private static bool TryParseInt32(string value, out int result) =>
        int.TryParse(
            value,
            NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture,
            out result);

    private static bool IsSha256(string value) =>
        value.Length == 64 &&
        value.All(character =>
            character is >= '0' and <= '9' or >= 'a' and <= 'f');

    private static bool IsSha1(string value) =>
        value.Length == 40 &&
        value.All(character =>
            character is >= '0' and <= '9' or >= 'a' and <= 'f');

    private static string HashHex(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static void WriteUInt32BigEndian(Stream stream, uint value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
        stream.Write(buffer);
    }

    private static void WriteUInt64BigEndian(Stream stream, ulong value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64BigEndian(buffer, value);
        stream.Write(buffer);
    }

    private static void WriteInt32BigEndian(Stream stream, int value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(buffer, value);
        stream.Write(buffer);
    }

    private readonly record struct PrintedSemanticRow(
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
}
