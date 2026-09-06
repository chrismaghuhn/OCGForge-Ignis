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
        I3CPublicStateProjectionTests.TestCanonicalByteStorageIsolation),

    ("I3D exact 386-byte golden binds the public projection identity",
        I3DPublicProjectionPrivacyTests.TestFirstGolden),

    ("I3D exact 700-byte golden binds the public projection identity",
        I3DPublicProjectionPrivacyTests.TestSecondGolden),

    ("I3D failed projection has no public projection identity",
        I3DPublicProjectionPrivacyTests.TestFailureHasNoIdentity),

    ("I3D result and source mutation remain isolated",
        I3DPublicProjectionPrivacyTests.TestValueOwnership),

    ("I3D public API has no external identity binding seam",
        I3DPublicProjectionPrivacyTests.TestNoExternalBindingSeam),

    ("I3D paired world A hides opponent hand identity",
        I3DPublicProjectionPrivacyTests.TestPairedWorldA),

    ("I3D paired world B hides opponent deck identity and order",
        I3DPublicProjectionPrivacyTests.TestPairedWorldB),

    ("I3D paired world C destroys reveal-to-hidden continuity",
        I3DPublicProjectionPrivacyTests.TestPairedWorldC),

    ("I3D paired world D ignores duplicate-card history order",
        I3DPublicProjectionPrivacyTests.TestPairedWorldD),

    ("I3D paired world E ignores TCP chunking",
        I3DPublicProjectionPrivacyTests.TestPairedWorldE),

    ("I4A YESNO exact domain and response values",
        I4AFlatPromptProjectionTests.TestYesNoExactDomain),

    ("I4A YESNO malformed input and ownership",
        I4AFlatPromptProjectionTests.TestYesNoFailuresAndOwnership),

    ("I4A OPTION source order and public values",
        I4AFlatPromptProjectionTests.TestOptionSourceOrderAndValues),

    ("I4A OPTION duplicates and local-key metamorphic identity",
        I4AFlatPromptProjectionTests.TestOptionDuplicatesAndMetamorphicKey),

    ("I4A OPTION invalid domains fail closed",
        I4AFlatPromptProjectionTests.TestOptionFailures),

    ("I4A POSITION valid mask order and private responses",
        I4AFlatPromptProjectionTests.TestPositionValidMasks),

    ("I4A POSITION invalid masks fail closed",
        I4AFlatPromptProjectionTests.TestPositionFailures),

    ("I4A POSITION unbound card code stays absent",
        I4AFlatPromptProjectionTests.TestPositionUnboundCardCode),

    ("I4A private binding resolves exact response values",
        I4AFlatPromptProjectionTests.TestExactResponseBindings),

    ("I4A stale same-looking selection is rejected",
        I4AFlatPromptProjectionTests.TestStaleSelection),

    ("I4A invalid key family and domain bindings fail closed",
        I4AFlatPromptProjectionTests.TestBindingValidationFailures),

    ("I4A failed prompts do not publish or advance state",
        I4AFlatPromptProjectionTests.TestFailureAtomicityAndOrdinal),

    ("I4A public values preserve the privacy boundary",
        I4AFlatPromptProjectionTests.TestPublicApiBoundary),

    ("I4A public values own source data",
        I4AFlatPromptProjectionTests.TestValueOwnership),

    ("I4B EFFECTYN exact wire and context",
        I4BEffectYnChainPromptTests.TestEffectYnExactWireAndContext),
    ("I4B EFFECTYN domain order and responses",
        I4BEffectYnChainPromptTests.TestEffectYnDomainOrderAndResponses),
    ("I4B EFFECTYN malformed wire failures",
        I4BEffectYnChainPromptTests.TestEffectYnMalformedWireFailures),
    ("I4B EFFECTYN authority validation failures",
        I4BEffectYnChainPromptTests.TestEffectYnAuthorityValidationFailures),
    ("I4B EFFECTYN indexed correlation",
        I4BEffectYnChainPromptTests.TestEffectYnIndexedCorrelation),
    ("I4B EFFECTYN pile and overlay correlation",
        I4BEffectYnChainPromptTests.TestEffectYnPileAndOverlayCorrelation),
    ("I4B EFFECTYN card code safety and ambiguity",
        I4BEffectYnChainPromptTests.TestEffectYnCardCodeSafetyAndAmbiguity),
    ("I4B EFFECTYN privacy and staleness",
        I4BEffectYnChainPromptTests.TestEffectYnPrivacyAndStaleness),
    ("I4B CHAIN optional wire context and no-chain",
        I4BEffectYnChainPromptTests.TestChainOptionalWireContextAndNoChain),
    ("I4B CHAIN forced marker and single entry",
        I4BEffectYnChainPromptTests.TestChainForcedMarkerAndSingleEntry),
    ("I4B CHAIN optional empty domain",
        I4BEffectYnChainPromptTests.TestChainOptionalEmptyDomain),
    ("I4B CHAIN entry order duplicates and values",
        I4BEffectYnChainPromptTests.TestChainEntryOrderDuplicatesAndValues),
    ("I4B CHAIN no-chain authority",
        I4BEffectYnChainPromptTests.TestChainNoChainAuthority),
    ("I4B CHAIN malformed wire and enumeration",
        I4BEffectYnChainPromptTests.TestChainMalformedWireAndEnumeration),
    ("I4B CHAIN correlation authority and card code safety",
        I4BEffectYnChainPromptTests.TestChainCorrelationAuthorityAndCardCodeSafety),
    ("I4B CHAIN atomicity staleness and ownership",
        I4BEffectYnChainPromptTests.TestChainAtomicityStalenessAndOwnership),
    ("I4B public private boundary",
        I4BEffectYnChainPromptTests.TestI4BPublicPrivateBoundary),
    ("I4B I4A and I3 regression boundary",
        I4BEffectYnChainPromptTests.TestI4AAndI3RegressionBoundary),
    ("I4C BATTLE exact wire and context",
        I4CIdleBattlePromptTests.TestBattleExactWireAndContext),
    ("I4C BATTLE mixed sections and complete order",
        I4CIdleBattlePromptTests.TestBattleMixedSectionsAndCompleteOrder),
    ("I4C BATTLE response bindings and section ordinals",
        I4CIdleBattlePromptTests.TestBattleResponseBindingsAndSectionOrdinals),
    ("I4C BATTLE transition flags and zero-domain",
        I4CIdleBattlePromptTests.TestBattleTransitionFlagsAndZeroDomain),
    ("I4C BATTLE indexed correlation and accepted locator",
        I4CIdleBattlePromptTests.TestBattleIndexedCorrelationAndAcceptedLocator),
    ("I4C BATTLE CardCode safety and ambiguity",
        I4CIdleBattlePromptTests.TestBattleCardCodeSafetyAndAmbiguity),
    ("I4C BATTLE malformed wire and enum validation",
        I4CIdleBattlePromptTests.TestBattleMalformedWireAndEnumValidation),
    ("I4C BATTLE authority atomicity staleness ownership privacy",
        I4CIdleBattlePromptTests.TestBattleAuthorityAtomicityStalenessOwnershipPrivacy),
    ("I4C IDLE exact wire and context",
        I4CIdleBattlePromptTests.TestIdleExactWireAndContext),
    ("I4C IDLE all sections and canonical order",
        I4CIdleBattlePromptTests.TestIdleAllSectionsAndCanonicalOrder),
    ("I4C IDLE per-section response bindings",
        I4CIdleBattlePromptTests.TestIdlePerSectionResponseBindings),
    ("I4C IDLE transition flags and zero-domain",
        I4CIdleBattlePromptTests.TestIdleTransitionFlagsAndZeroDomain),
    ("I4C IDLE indexed and pile correlation",
        I4CIdleBattlePromptTests.TestIdleIndexedAndPileCorrelation),
    ("I4C IDLE CardCode safety and duplicate ambiguity",
        I4CIdleBattlePromptTests.TestIdleCardCodeSafetyAndDuplicateAmbiguity),
    ("I4C IDLE malformed wire and enum validation",
        I4CIdleBattlePromptTests.TestIdleMalformedWireAndEnumValidation),
    ("I4C IDLE authority atomicity staleness ownership privacy",
        I4CIdleBattlePromptTests.TestIdleAuthorityAtomicityStalenessOwnershipPrivacy),
    ("I4C public private boundary",
        I4CIdleBattlePromptTests.TestI4CPublicPrivateBoundary),
    ("I4C I3 I4A I4B regression boundary",
        I4CIdleBattlePromptTests.TestI3I4AI4BRegressionBoundary),
    ("I4D seven-family support and unsupported boundary",
        I4DFinalAcceptanceTests.TestSevenFamilySupportAndUnsupportedBoundary),
    ("I4D cross-family binding lifecycle",
        I4DFinalAcceptanceTests.TestCrossFamilyBindingLifecycle),
    ("I4D failure atomicity and ordinal isolation",
        I4DFinalAcceptanceTests.TestFailureAtomicityAndOrdinalIsolation),
    ("I4D complete domains and response isolation",
        I4DFinalAcceptanceTests.TestCompleteDomainsAndResponseIsolation),
    ("I4D public/private authority determinism barrier",
        I4DFinalAcceptanceTests.TestPublicPrivateAuthorityDeterminismBarrier),
    ("I5A1 SELECT_CARD",
        I5A1SelectionPromptTests.TestSelectCard),
    ("I5A1 SELECT_TRIBUTE",
        I5A1SelectionPromptTests.TestSelectTribute),
    ("I5A1 SELECT_UNSELECT_CARD and ANNOUNCE_NUMBER",
        I5A1SelectionPromptTests.TestSelectUnselectAndAnnounceNumber),
    ("I5A2 SELECT_PLACE and SELECT_DISFIELD",
        I5A2PlaceAndMaskPromptTests.TestPlaceAndDisfield),

    ("I5A2 ANNOUNCE_RACE and ANNOUNCE_ATTRIB",
        I5A2PlaceAndMaskPromptTests.TestRaceAndAttribute),

    ("I5A3 SELECT_COUNTER",
        I5A3CounterPromptTests.TestSelectCounter),

    ("I5A4 SORT_CARD and SORT_CHAIN",
        I5A4SortPromptTests.TestSortPrompts),

    ("I5A5 supported-family dispatch and unsupported boundary",
        I5CrossFamilyFinalAcceptanceTests.TestSupportedFamilyDispatchAndUnsupportedBoundary),

    ("I5A5 cross-family binding lifecycle",
        I5CrossFamilyFinalAcceptanceTests.TestCrossFamilyBindingLifecycle),

    ("I5A5 failure atomicity and ordinal isolation",
        I5CrossFamilyFinalAcceptanceTests.TestFailureAtomicityAndOrdinalIsolation),

    ("I5A5 complete domains and response isolation",
        I5CrossFamilyFinalAcceptanceTests.TestCompleteDomainsAndResponseIsolation),

    ("I5A5 public/private authority determinism barrier",
        I5CrossFamilyFinalAcceptanceTests.TestPublicPrivateAuthorityDeterminismBarrier)
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
