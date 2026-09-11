using System.Globalization;
using System.Security;
using System.Security.Cryptography;
using OCGForge.Ignis.Gameplay;

namespace OCGForge.Ignis.Gameplay.Tests.Fixtures;

internal enum I6GRealRunMatchContextErrorCodeV1 : byte
{
    None = 0,
    MissingConfiguration = 1,
    ScenarioMismatch = 2,
    MissingPerspective = 3,
    MissingDuelFlags = 4,
    DeckSourceUnavailable = 5,
    DeckSourceHashMismatch = 6,
    SideDeckNotEmpty = 7,
    KnownDeckEmpty = 8,
    InvalidContext = 9,
    CounterOpponentKnowledgeForbidden = 10
}

internal sealed record I6GRealRunMatchContextConfigurationV1(
    string ScenarioId,
    GameplayPerspectiveV1? Perspective,
    ulong? DuelFlags,
    bool OwnDecklistKnown,
    bool OpponentDecklistKnown);

internal readonly record struct I6GRealRunMatchContextBindingResultV1(
    bool IsSuccess,
    I6GRealRunMatchContextErrorCodeV1 ErrorCode,
    PerspectiveSafeMatchContextV1? Context = null);

internal static class I6GRealRunMatchContextAuthorityV1
{
    private const string CounterScenarioId =
        "projectignis.windbot.ai-blackwing.v1";

    internal static I6GRealRunMatchContextBindingResultV1 TryCreateCounter(
        I6C6ClosureScenarioConfigurationV1? scenario,
        I6GRealRunMatchContextConfigurationV1? configuration)
    {
        if (scenario is null || configuration is null)
        {
            return Failure(
                I6GRealRunMatchContextErrorCodeV1.MissingConfiguration);
        }

        if (
            !string.Equals(
                scenario.ScenarioId,
                CounterScenarioId,
                StringComparison.Ordinal))
        {
            return Failure(
                I6GRealRunMatchContextErrorCodeV1.ScenarioMismatch);
        }

        if (configuration.OpponentDecklistKnown)
        {
            return Failure(
                I6GRealRunMatchContextErrorCodeV1
                    .CounterOpponentKnowledgeForbidden);
        }

        return TryCreate(scenario, configuration);
    }

    internal static I6GRealRunMatchContextBindingResultV1 TryCreate(
        I6C6ClosureScenarioConfigurationV1? scenario,
        I6GRealRunMatchContextConfigurationV1? configuration)
    {
        if (scenario is null || configuration is null)
        {
            return Failure(
                I6GRealRunMatchContextErrorCodeV1.MissingConfiguration);
        }

        if (string.IsNullOrWhiteSpace(configuration.ScenarioId) ||
            !string.Equals(
                configuration.ScenarioId,
                scenario.ScenarioId,
                StringComparison.Ordinal))
        {
            return Failure(
                I6GRealRunMatchContextErrorCodeV1.ScenarioMismatch);
        }

        if (configuration.Perspective is null)
        {
            return Failure(
                I6GRealRunMatchContextErrorCodeV1.MissingPerspective);
        }

        if (configuration.Perspective.PlayerType > 1)
        {
            return Failure(
                I6GRealRunMatchContextErrorCodeV1.InvalidContext);
        }

        if (!configuration.DuelFlags.HasValue)
        {
            return Failure(
                I6GRealRunMatchContextErrorCodeV1.MissingDuelFlags);
        }

        if (!TryCreateDeck(
                scenario.PrimaryDeckPath,
                scenario.PrimaryDeckSha256,
                configuration.OwnDecklistKnown,
                out PerspectiveSafeDeckV1 ownDeck,
                out I6GRealRunMatchContextErrorCodeV1 ownDeckError))
        {
            return Failure(ownDeckError);
        }

        if (!TryCreateDeck(
                scenario.OpponentDeckPath,
                scenario.OpponentDeckSha256,
                configuration.OpponentDecklistKnown,
                out PerspectiveSafeDeckV1 opponentDeck,
                out I6GRealRunMatchContextErrorCodeV1 opponentDeckError))
        {
            return Failure(opponentDeckError);
        }

        PerspectiveSafeMatchContextV1 context =
            new(
                configuration.Perspective.PlayerType,
                configuration.DuelFlags.Value,
                new PerspectiveSafeKnowledgeV1(
                    configuration.OwnDecklistKnown,
                    configuration.OpponentDecklistKnown),
                ownDeck,
                opponentDeck);
        if (!PerspectiveSafeMatchContextValidationV1.TryValidate(
                context,
                configuration.Perspective.PlayerType,
                out _))
        {
            return Failure(
                I6GRealRunMatchContextErrorCodeV1.InvalidContext);
        }

        return new(
            true,
            I6GRealRunMatchContextErrorCodeV1.None,
            context);
    }

    private static bool TryCreateDeck(
        string path,
        string expectedSha256,
        bool known,
        out PerspectiveSafeDeckV1 deck,
        out I6GRealRunMatchContextErrorCodeV1 error)
    {
        deck = new PerspectiveSafeDeckV1(known: false);
        error = I6GRealRunMatchContextErrorCodeV1.None;
        if (!known)
        {
            return true;
        }

        if (!IsAbsoluteNonEmptyPath(path) || !File.Exists(path))
        {
            error = I6GRealRunMatchContextErrorCodeV1
                .DeckSourceUnavailable;
            return false;
        }

        string actualSha256;
        try
        {
            actualSha256 = HashFile(path);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or
                NotSupportedException or SecurityException)
        {
            error = I6GRealRunMatchContextErrorCodeV1
                .DeckSourceUnavailable;
            return false;
        }

        if (!string.Equals(
                actualSha256,
                expectedSha256,
                StringComparison.Ordinal))
        {
            error = I6GRealRunMatchContextErrorCodeV1
                .DeckSourceHashMismatch;
            return false;
        }

        try
        {
            List<uint> main = new();
            List<uint> extra = new();
            List<uint> side = new();
            List<uint> active = main;
            foreach (string rawLine in File.ReadLines(path))
            {
                string line = rawLine.Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                if (line.Equals("#main", StringComparison.OrdinalIgnoreCase))
                {
                    active = main;
                    continue;
                }

                if (line.Equals("#extra", StringComparison.OrdinalIgnoreCase))
                {
                    active = extra;
                    continue;
                }

                if (line.Equals("!side", StringComparison.OrdinalIgnoreCase))
                {
                    active = side;
                    continue;
                }

                if (line[0] == '#' || line[0] == '!')
                {
                    continue;
                }

                if (!uint.TryParse(
                        line,
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out uint code) ||
                    code == 0)
                {
                    error = I6GRealRunMatchContextErrorCodeV1
                        .DeckSourceUnavailable;
                    return false;
                }

                active.Add(code);
            }

            if (main.Count == 0)
            {
                error = I6GRealRunMatchContextErrorCodeV1
                    .KnownDeckEmpty;
                return false;
            }

            if (side.Count != 0)
            {
                error = I6GRealRunMatchContextErrorCodeV1
                    .SideDeckNotEmpty;
                return false;
            }

            deck = new PerspectiveSafeDeckV1(
                known: true,
                mainDeck: main.OrderBy(value => value),
                extraDeck: extra.OrderBy(value => value));
            return true;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or
                NotSupportedException or SecurityException)
        {
            error = I6GRealRunMatchContextErrorCodeV1
                .DeckSourceUnavailable;
            return false;
        }
    }

    private static bool IsAbsoluteNonEmptyPath(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        (Path.IsPathFullyQualified(value) || IsWindowsAbsolutePath(value));

    private static bool IsWindowsAbsolutePath(string value) =>
        value.Length >= 3 &&
        char.IsLetter(value[0]) &&
        value[1] == ':' &&
        (value[2] == '\\' || value[2] == '/');

    private static string HashFile(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static I6GRealRunMatchContextBindingResultV1 Failure(
        I6GRealRunMatchContextErrorCodeV1 errorCode) =>
        new(false, errorCode);
}
