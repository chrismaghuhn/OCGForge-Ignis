using static OCGForge.Ignis.Gameplay.Tests.TestAssert;
using OCGForge.Ignis.Gameplay.Tests.Fixtures;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I6C6RealRunEntryPointTests
{
    internal static void TestMissingInputsFailClosed()
    {
        I6C6RealRunEntryPointResultV1 result =
            I6C6RealRunEntryPointV1.TryPrepare(null);

        False(result.IsSuccess);
        Equal(
            "STATUS=BLOCKED_I6C6_RUNTIME_INPUTS_UNAVAILABLE",
            result.Status);
        False(result.ChildRuntimeStarted);
        False(result.LiveEvidenceProduced);
    }
}
