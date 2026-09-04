using OCGForge.Ignis.Gameplay.Tests;

var tests = new (string Name, Action Body)[]
{
    ("MSG_START establishes SelfIsPlayer0",
        I3AGameplayDecoderTests.TestStartSelfIsPlayer0),

    ("MSG_START establishes SelfIsPlayer1",
        I3AGameplayDecoderTests.TestStartSelfIsPlayer1),

    ("MSG_START exact length and no inner NeedMoreData",
        I3AGameplayDecoderTests.TestStartLength),

    ("observer and invalid roles fail closed",
        I3AGameplayDecoderTests.TestRoleRejection),

    ("duplicate and conflicting MSG_START fail closed",
        I3AGameplayDecoderTests.TestDuplicateAndConflict),

    ("perspective-dependent and unknown messages fail closed",
        I3AGameplayDecoderTests.TestUnsupportedMessages),

    ("modern loc_info is explicit little endian",
        I3AGameplayDecoderTests.TestModernLocInfo),

    ("handoff claims exactly once",
        I3AHandoffTests.TestHandoffClaimExactlyOnce),

    ("pending bytes are processed before reads",
        I3AHandoffTests.TestPendingBytesFirst),

    ("partial pending frame continues through I1",
        I3AHandoffTests.TestPartialPendingFrame),

    ("session drains pending before live transport",
        I3AHandoffTests.TestSessionPendingReadFirst),

    ("pump and dispose share lifecycle ownership",
        I3AHandoffTests.TestPumpDisposeLifecycle),

    ("outer chunking has identical semantic output",
        I3AHandoffTests.TestChunkingDeterminism),

    ("pending suffix transfers unchanged",
        I3AHandoffTests.TestPendingSuffixTransfer),

    ("failure closes transport exactly once",
        I3AHandoffTests.TestFailureCloseExactlyOnce),

    ("short inner message fails through complete outer frame",
        I3AHandoffTests.TestShortInnerMessage),

    ("malformed outer frame fails closed",
        I3AHandoffTests.TestMalformedOuterFrame),

    ("privacy values exclude control metadata",
        I3AGameplayDecoderTests.TestPrivacyBoundary),

    ("fresh decoder values are immutable by construction",
        I3AGameplayDecoderTests.TestValueOwnership),

    ("I3B query union decodes every admitted flag",
        I3BModernQueryTests.TestQueryUnion),

    ("I3B query failures are strict and atomic",
        I3BModernQueryTests.TestQueryFailures),

    ("I3B mirror initializes perspective and authoritative turn state",
        I3BPerspectiveStateMirrorTests.TestMirrorInitialization),

    ("I3B mirror applies movement and relations transactionally",
        I3BPerspectiveStateMirrorTests.TestMirrorMovementAndRelations),

    ("I3B face-down transitions destroy stale card facts",
        I3BPerspectiveStateMirrorTests.TestFaceDownTransition),

    ("I3B provenance and locator-safe query semantics",
        I3BPerspectiveStateMirrorTests.TestProvenanceAndLocatorSafety),

    ("I3B visibility flags may overlap without local legality",
        I3BPerspectiveStateMirrorTests.TestVisibilityFlagOverlap),

    ("I3B draw LP and terminal state are fail closed",
        I3BPerspectiveStateMirrorTests.TestDrawLpAndTerminal),

    ("I3B update data preserves wire query order",
        I3BPerspectiveStateMirrorTests.TestUpdateDataWireOrder),

    ("I3B stream chunking preserves mirror semantics",
        I3BPerspectiveStateMirrorTests.TestMirrorChunking),

    ("I3C0 canonical admitted locator forms round-trip exactly",
        I3C0PublicSemanticLocatorTests.TestCanonicalFormsRoundTripExactly),

    ("I3C0 malformed and unsupported locators fail closed",
        I3C0PublicSemanticLocatorTests.TestMalformedFormsFailClosed),

    ("I3C0 maps perspective roles to absolute players",
        I3C0PublicSemanticLocatorTests.TestAbsolutePlayerMapping),

    ("I3C0 locator equality ordering and hash are deterministic",
        I3C0PublicSemanticLocatorTests.TestDeterministicCultureIndependentValue),

    ("I3C0 public API keeps internal identities private",
        I3C0PublicSemanticLocatorTests.TestPublicApiBoundary),

    ("I3C projects core perspective state with absolute participants",
        I3CPublicStateProjectionTests.TestCorePerspectiveState),

    ("I3C preserves own knowledge and redacts unknown opponent identity",
        I3CPublicStateProjectionTests.TestKnowledgeProjection),

    ("I3C hidden populations preserve counts without stable hidden locators",
        I3CPublicStateProjectionTests.TestHiddenPopulationProjection),

    ("I3C locator generation is semantic and independent of mirror identity",
        I3CPublicStateProjectionTests.TestSemanticLocatorIndependence),

    ("I3C paired hidden worlds have identical canonical public bytes",
        I3CPublicStateProjectionTests.TestPairedHiddenWorlds),

    ("I3C canonical bytes and SHA256 are deterministic across culture/order",
        I3CPublicStateProjectionTests.TestCanonicalDeterminism),

    ("I3C SZONE/layout mapping is explicit and fails closed when unproven",
        I3CPublicStateProjectionTests.TestSzoneLayoutMapping),

    ("I3C public API exposes no mirror/protocol/private identity",
        I3CPublicStateProjectionTests.TestPublicApiBoundary),

    ("I3C canonical byte storage is not externally mutable",
        I3CPublicStateProjectionTests.TestCanonicalByteStorageIsolation)
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

Console.WriteLine($"RESULT passed={passed} failed={failed}");
Environment.ExitCode = failed == 0 ? 0 : 1;
