using OCGForge.Ignis.Model.Tests;

var tests = new (string Name, Action Body)[]
{
    ("I6B canonical bytes and identity KAT", I6BBundlePreflightTests.TestCanonicalKat),
    ("I6B registry order and flags", I6BBundlePreflightTests.TestRegistryOrderAndFlags),
    ("I6B manifest mismatch rejection matrix", I6BBundlePreflightTests.TestManifestMismatchMatrix),
    ("I6B failure atomicity and immutability", I6BBundlePreflightTests.TestFailureAtomicityAndImmutability)
};

int passed = 0;
int failed = 0;
foreach ((string name, Action body) in tests)
{
    try
    {
        body();
        Console.WriteLine($"PASS {name}");
        passed++;
    }
    catch (Exception exception)
    {
        Console.WriteLine($"FAIL {name}: {exception.GetType().Name}: {exception.Message}");
        failed++;
    }
}

if (failed == 0)
{
    I6BBundlePreflightTests.EmitDeterministicEvidence();
}

Console.WriteLine($"RESULT passed={passed} failed={failed}");
Environment.ExitCode = failed == 0 ? 0 : 1;
