using System.Buffers.Binary;
using System.Reflection;
using OCGForge.Ignis.Client;
using OCGForge.Ignis.Gameplay;
using OCGForge.Ignis.Protocol;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class TestAssert
{
    internal static void AssertDoesNotContainForbidden(string? value, IEnumerable<string> forbidden)
    {
        string text = value ?? string.Empty;
        foreach (string term in forbidden)
        {
            False(text.Contains(term, StringComparison.OrdinalIgnoreCase),
                $"forbidden value '{term}' appeared");
        }
    }

    internal static void True(bool condition, string message = "assertion was false")
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    internal static void False(bool condition, string message = "assertion was true") =>
        True(!condition, message);

    internal static void Null(object? value) => True(value is null, "expected null");

    internal static void NotNull(object? value) => True(value is not null, "expected non-null");

    internal static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"expected {expected}; actual {actual}");
        }
    }

    internal static void NotEqual<T>(T first, T second)
    {
        if (EqualityComparer<T>.Default.Equals(first, second))
        {
            throw new InvalidOperationException($"values unexpectedly equal: {first}");
        }
    }

    internal static void BytesEqual(ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual)
    {
        True(expected.SequenceEqual(actual),
            $"expected {Convert.ToHexString(expected)}; actual {Convert.ToHexString(actual)}");
    }
}
