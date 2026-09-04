# I4C IDLECMD + BATTLECMD Implementation Plan

> For agentic workers: REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

Goal: Extend the accepted I4 flat-prompt module with complete, deterministic,
privacy-safe BATTLECMD and IDLECMD domains while preserving all I3D, I4A, and
I4B semantics.

Architecture: Extend the existing FlatPromptSessionV1 deep module and its
private FlatPromptProjectionV1 parser. Reuse the I4B per-call authority
transaction and stateless FlatPromptCardCorrelationV1; add no second public
projection or card-reference authority. Preserve the existing one-argument
I4A interface and the accepted I4B overload.

Tech Stack: C#/.NET 10, nullable reference types, immutable records,
ReadOnlySpan<byte>, System.Buffers.Binary, existing executable Gameplay,
Protocol, and Client harnesses, and no new packages or network/model
dependencies.

---

## Current task boundary

The current task is documentation-only and may change exactly:

    docs/superpowers/specs/2026-09-05-i4c-idlecmd-battlecmd-design.md
    docs/superpowers/plans/2026-09-05-i4c-idlecmd-battlecmd-plan.md

The current task must not change production code, tests, fixtures, frozen
contracts, project files, workflows, generated evidence, networking, model
input, I5, or I6. It must not create a PR or merge.

The authorized implementation base is:

    BASE=ea0d7d51d988e201c246a6660e27bdd20402221b
    BRANCH=chris/i4c-idlecmd-battlecmd-design-plan

The later implementation is separately authorized only after independent
review of these two committed documents.

## Future implementation file map

The later implementation continues from the committed PLAN_HEAD and changes
exactly five files:

    MODIFY
    src/OCGForge.Ignis.Gameplay/FlatPromptTypesV1.cs
    src/OCGForge.Ignis.Gameplay/FlatPromptProjectionV1.cs
    src/OCGForge.Ignis.Gameplay/FlatPromptSessionV1.cs
    tests/OCGForge.Ignis.Gameplay.Tests/Program.cs

    CREATE
    tests/OCGForge.Ignis.Gameplay.Tests/Tests/I4CIdleBattlePromptTests.cs

The counts are fixed:

    FUTURE_IMPLEMENTATION_PRODUCTION_FILES=3
    FUTURE_IMPLEMENTATION_TEST_FILES=2
    FUTURE_IMPLEMENTATION_FILES=5
    PLAN_HEAD_TO_FEATURE_HEAD=5
    ORIGINAL_BASE_TO_FEATURE_HEAD=7

The following files are intentionally not part of the I4C implementation:

    src/OCGForge.Ignis.Gameplay/GameplayMessageDecoderV1.cs
    src/OCGForge.Ignis.Gameplay/GameplayMessageTypesV1.cs
    src/OCGForge.Ignis.Gameplay/GameplayMirrorSessionV1.cs
    src/OCGForge.Ignis.Gameplay/FlatPromptCardCorrelationV1.cs
    src/OCGForge.Ignis.Gameplay/MirrorAddressNormalizationV1.cs
    src/OCGForge.Ignis.Gameplay/PerspectiveStateMirrorV1.cs
    src/OCGForge.Ignis.Gameplay/PublicStateProjectionV1.cs
    src/OCGForge.Ignis.Gameplay/PublicSemanticLocatorV1.cs
    tests/OCGForge.Ignis.Gameplay.Tests/Fixtures/*
    fixtures/gameplay/v1/*
    docs/contracts/*
    Protocol/Client source and tests
    network, model, workflow, and generated evidence files

The existing I4B card-correlation helper is reused without modification.
There is no approved I3 exception.

## Task 1: Future implementation start guard

Files: none.

- [ ] Step 1: Require the committed plan head, branch, base, and clean worktree.

Run from C:\Users\chris\Documents\OCGForge-Ignis:

    git fetch origin --prune
    $base = 'ea0d7d51d988e201c246a6660e27bdd20402221b'
    $planPath = 'docs/superpowers/plans/2026-09-05-i4c-idlecmd-battlecmd-plan.md'
    $planHead = (git log -1 --format='%H' -- $planPath).Trim()
    $head = (git rev-parse HEAD).Trim()
    $branch = (git branch --show-current).Trim()
    $remoteMain = (git rev-parse origin/main).Trim()
    if ($head -cne $planHead) {
        throw "HEAD_NOT_PLAN_HEAD HEAD=$head EXPECTED=$planHead"
    }
    if ($branch -cne 'chris/i4c-idlecmd-battlecmd-design-plan') {
        throw "WRONG_BRANCH BRANCH=$branch"
    }
    if ($remoteMain -cne $base) {
        throw "STATUS=BLOCKED_BASE_MOVED REMOTE_MAIN=$remoteMain EXPECTED=$base"
    }
    if (@(git status --short).Count -ne 0) {
        throw 'WORKTREE_NOT_CLEAN'
    }
    Write-Output "BASE=$base"
    Write-Output "PLAN_HEAD=$planHead"

Require:

    HEAD=PLAN_HEAD
    BRANCH=chris/i4c-idlecmd-battlecmd-design-plan
    origin/main=ea0d7d51d988e201c246a6660e27bdd20402221b
    WORKTREE=CLEAN

- [ ] Step 2: Verify that the original base to PLAN_HEAD contains exactly the
  two design documents.

Run:

    $expectedDocs = @(
        'docs/superpowers/specs/2026-09-05-i4c-idlecmd-battlecmd-design.md',
        'docs/superpowers/plans/2026-09-05-i4c-idlecmd-battlecmd-plan.md'
    ) | Sort-Object
    $baseChanged = @(git diff --name-only $base $planHead | Sort-Object -Unique)
    if (@($baseChanged).Count -ne 2 -or
        ($baseChanged -join [Environment]::NewLine) -cne
        ($expectedDocs -join [Environment]::NewLine)) {
        throw "BASE_TO_PLAN_SCOPE_MISMATCH CHANGED=$($baseChanged -join ',')"
    }
    Write-Output 'BASE_TO_PLAN_SCOPE=2_DOCS_PASS'

- [ ] Step 3: Audit the accepted inputs before writing implementation tests.

Read:

    docs/contracts/flat-prompt-projection-v1.md
    fixtures/gameplay/v1/i4-flat-prompt-vectors.v1.json
    fixtures/gameplay/v1/game-message-support.v1.json
    docs/superpowers/specs/2026-09-04-i4-flat-prompt-design.md
    docs/superpowers/specs/2026-09-04-i4a-simple-flat-prompts-design.md
    docs/superpowers/specs/2026-09-04-i4b-effectyn-chain-design.md
    docs/superpowers/plans/2026-09-04-i4b-effectyn-chain-plan.md
    src/OCGForge.Ignis.Gameplay/FlatPromptTypesV1.cs
    src/OCGForge.Ignis.Gameplay/FlatPromptProjectionV1.cs
    src/OCGForge.Ignis.Gameplay/FlatPromptSessionV1.cs
    src/OCGForge.Ignis.Gameplay/FlatPromptCardCorrelationV1.cs
    src/OCGForge.Ignis.Gameplay/MirrorAddressNormalizationV1.cs
    src/OCGForge.Ignis.Gameplay/PublicStateProjectionV1.cs
    src/OCGForge.Ignis.Gameplay/PublicSemanticLocatorV1.cs
    src/OCGForge.Ignis.Gameplay/PerspectiveStateMirrorV1.cs
    tests/OCGForge.Ignis.Gameplay.Tests/Tests/I4AFlatPromptProjectionTests.cs
    tests/OCGForge.Ignis.Gameplay.Tests/Tests/I4BEffectYnChainPromptTests.cs
    tests/OCGForge.Ignis.Gameplay.Tests/Program.cs

Confirm:

    EDOPRO_PIN=30935e847165a9ef0e547fb51a43f36168fab7c7
    OCGCORE_PIN=46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57
    CURRENT_GAMEPLAY_TEST_COUNT=85
    I4A/I4B tests and I3D golden tests are green at PLAN_HEAD

Do not adopt a newer upstream or rewrite a frozen vector.

## Task 2: Write the complete red-test catalog

Files:

    Create: tests/OCGForge.Ignis.Gameplay.Tests/Tests/I4CIdleBattlePromptTests.cs
    Modify: tests/OCGForge.Ignis.Gameplay.Tests/Program.cs

- [ ] Step 1: Append exactly these 18 registrations after the existing 85.

Use the existing array shape and do not rename, remove, or reorder any
existing registration:

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
        I4CIdleBattlePromptTests.TestI3I4AI4BRegressionBoundary)

The registration count must become:

    CURRENT_GAMEPLAY_TEST_COUNT=85
    PLANNED_NEW_I4C_TEST_GROUPS=18
    EXPECTED_GAMEPLAY_TEST_COUNT=103

- [ ] Step 2: Build the test fixtures from the frozen positive vectors without
  editing JSON.

The new test file must reproduce and assert the exact frozen vectors:

    BATTLECMD_MIXED_SECTIONS
        47 complete bytes
        activatable count 1
        attackable count 2
        TO_M2 flag 1
        TO_EP flag 0

    IDLECMD_ALL_SECTIONS_AND_TRANSITIONS
        95 complete bytes
        one entry in each of the six source sections
        TO_BP, TO_EP, and SHUFFLE_HAND all flagged

Use the existing fixture helpers for little-endian u32/u64 values and for
captured MirrorSnapshotV1 plus accepted PublicStateProjectionResultV1. Add
independent duplicate, maximum-index, invalid-flag, and authority-mismatch
values in the test file. Keep raw response bytes as test evidence only.

- [ ] Step 3: Add the BATTLE test assertions.

The eight BATTLE groups must cover all of these concrete facts:

    exact 12 + 19*a + 8*b length
    player, controller, location, u32 activatable sequence
    u64 description and client_mode 0/1/2
    u8 attackable sequence and direct_attackable 0/1
    activatable wire order before attackable wire order
    TO_M2 before TO_EP, only when the source flag is one
    ACTIVATE response (ordinal << 16) | 0
    ATTACK response (ordinal << 16) | 1
    TO_M2 response 2 and TO_EP response 3
    section-local canonical ASCII ordinals
    duplicate source occurrences retained
    accepted indexed locator copied exactly
    safe/unsafe CardCode structural variants
    missing/ambiguous/Main Deck references fail closed
    truncation, trailing bytes, legacy width, count mismatch, and overflow
    invalid player/controller/location/direct_attackable/client_mode
    invalid transition booleans
    authority byte/SHA/ProjectionId mismatch
    failed prompt invalidates the old handle and does not advance ordinal
    mutable source bytes cannot mutate context, domain, or binding
    public reflection excludes response, raw wire, Mirror, socket, and process data

The BATTLE correlation group must contain a real prompt path with simultaneous
same-player/same-sequence MonsterZone and SpellTrapZone-family cards. It must
prove that the accepted zone-compatible locator is selected and that numeric
sequence alone cannot select the other card.

- [ ] Step 4: Add the IDLE test assertions.

The eight IDLE groups must cover all of these concrete facts:

    exact 29 + 10*s + 10*ss + 7*r + 10*m + 10*st + 19*a length
    independent u32 counts for all six sections
    SUMMON, SPECIAL_SUMMON, REPOSITION, MSET, SSET, ACTIVATE
    32-bit sequence for every section except REPOSITION's u8 sequence
    activation description and client_mode 0/1/2
    canonical section order followed by TO_BP, TO_EP, SHUFFLE_HAND
    responses (i << 16) | 0 through (i << 16) | 5
    transition responses 6, 7, and 8
    section-local canonical keys with no leading zeros or sign
    duplicates retained within and across source sections
    indexed, Hand, Extra Deck, Overlay, and Main Deck semantics
    duplicate known Hand/Extra CardCodes fail closed
    safe/unsafe CardCode variants preserve proven locators
    truncation, trailing bytes, legacy width, count mismatch, and overflow
    invalid player/controller/location/client_mode/direct flags
    invalid transition booleans
    authority byte/SHA/ProjectionId mismatch
    failed prompt invalidates the old handle and does not advance ordinal
    mutable source bytes cannot mutate context, domain, or binding
    public reflection excludes response, raw wire, Mirror, socket, and process data

The IDLE correlation group must include a real Hand ambiguity proof: two Hand
cards with one proven CardCode and different private sequences make the
accepted public ordinal correlation ambiguous. The source sequence must not be
used to choose one public ordinal.

- [ ] Step 5: Run the required RED check before adding I4C production support.

Run:

    dotnet run --project tests/OCGForge.Ignis.Gameplay.Tests/OCGForge.Ignis.Gameplay.Tests.csproj --configuration Release

Expected:

    compilation failure because I4C enum values, candidate records, parser
    branches, and the complete BATTLE/IDLE projection path do not yet exist

Do not weaken the tests, change the expected total, or add fallback behavior.

## Task 3: Extend the closed I4 value model

File: src/OCGForge.Ignis.Gameplay/FlatPromptTypesV1.cs

- [ ] Step 1: Add only the I4C enums.

Add these numeric family values without changing existing numeric values:

    FlatPromptFamilyV1.MsgSelectBattleCmd = 10
    FlatPromptFamilyV1.MsgSelectIdleCmd = 11

Add these choice kinds:

    Activate
    Attack
    ToM2
    ToEp
    Summon
    SpecialSummon
    Reposition
    Mset
    Sset
    ToBp
    ShuffleHand

Add these source sections:

    Activatable
    Attackable
    Summon
    SpecialSummon
    Reposition
    Mset
    Sset
    IdleActivatable

Do not add I4D, I5, or I6 values.

- [ ] Step 2: Add the BATTLE public records as closed variants.

Use abstract record bases and sealed concrete records:

    FlatBattleActivatablePublicCandidateBaseV1
        common key, ACTIVATE, ACTIVATABLE, source ordinal,
        accepted public locator, description/effect id, client mode
        ├─ FlatBattleActivatablePublicCandidateV1
        └─ FlatBattleActivatableCardCodePublicCandidateV1
             adds only CardCode

    FlatBattleAttackPublicCandidateBaseV1
        common key, ATTACK, ATTACKABLE, source ordinal,
        accepted public locator, direct_attackable
        ├─ FlatBattleAttackPublicCandidateV1
        └─ FlatBattleAttackCardCodePublicCandidateV1
             adds only CardCode

    FlatBattleToMainPhase2PublicCandidateV1
        key and transition_token MAIN_PHASE_2 only

    FlatBattleToEndPhasePublicCandidateV1
        key and transition_token END_PHASE only

No BATTLE public record may expose raw location, response, protocol offset,
Mirror identity, or prompt ordinal.

- [ ] Step 3: Add the IDLE public records as closed variants.

Use:

    FlatIdleSummonPublicCandidateBaseV1
        ├─ FlatIdleSummonPublicCandidateV1
        └─ FlatIdleSummonCardCodePublicCandidateV1

    FlatIdleSpecialSummonPublicCandidateBaseV1
        ├─ FlatIdleSpecialSummonPublicCandidateV1
        └─ FlatIdleSpecialSummonCardCodePublicCandidateV1

    FlatIdleRepositionPublicCandidateBaseV1
        ├─ FlatIdleRepositionPublicCandidateV1
        └─ FlatIdleRepositionCardCodePublicCandidateV1

    FlatIdleMsetPublicCandidateBaseV1
        ├─ FlatIdleMsetPublicCandidateV1
        └─ FlatIdleMsetCardCodePublicCandidateV1

    FlatIdleSsetPublicCandidateBaseV1
        ├─ FlatIdleSsetPublicCandidateV1
        └─ FlatIdleSsetCardCodePublicCandidateV1

Each pair fixes its own choice kind and source section. A private constructor
helper may share immutable field-copying code, but the public records must not
collapse the five semantic source sections into one interchangeable runtime
type. Every binding check repeats the exact pair.

    FlatIdleActivatablePublicCandidateBaseV1
        common key, ACTIVATE, ACTIVATABLE, source ordinal,
        accepted public locator, description/effect id, client mode
        ├─ FlatIdleActivatablePublicCandidateV1
        └─ FlatIdleActivatableCardCodePublicCandidateV1
             adds only CardCode

Add sealed transition records:

    FlatIdleToBattlePhasePublicCandidateV1
    FlatIdleToEndPhasePublicCandidateV1
    FlatIdleShuffleHandPublicCandidateV1

Their only family-specific public field is the exact transition_token:

    BATTLE_PHASE
    END_PHASE
    SHUFFLE_HAND

- [ ] Step 4: Add private value-owned wire drafts.

Add private immutable drafts with separate typed arrays for the heterogeneous
sections:

    FlatPromptBattleActivatableWireEntryV1
        SourceCardCode, Controller, Location, Sequence, Description, ClientMode

    FlatPromptBattleAttackableWireEntryV1
        SourceCardCode, Controller, Location, Sequence, DirectAttackable

    FlatPromptBattleWireDraftV1
        ActingPlayer, ActivatableEntries, AttackableEntries,
        ToMainPhase2, ToEndPhase

    FlatPromptIdleCardWireEntryV1
        SourceCardCode, Controller, Location, Sequence

    FlatPromptIdleRepositionWireEntryV1
        SourceCardCode, Controller, Location, Sequence

    FlatPromptIdleActivatableWireEntryV1
        SourceCardCode, Controller, Location, Sequence,
        DescriptionOrEffectId, ClientMode

    FlatPromptIdleWireDraftV1
        ActingPlayer,
        SummonEntries, SpecialSummonEntries, RepositionEntries,
        MonsterSetEntries, SpellTrapSetEntries, ActivatableEntries,
        ToBattlePhase, ToEndPhase, ShuffleHand

Constructors copy arrays and reject null collections. These drafts never
escape publicly and are never retained by FlatPromptSessionV1.

- [ ] Step 5: Extend errors and binding validation.

Retain every existing I4A/I4B enum value and add no unrelated value. Use the
existing InvalidLocation, InvalidBoolean, InvalidClientMode,
AuthorityMismatch, ArithmeticFailure, ZeroOptionDomain,
UnprovenPublicReference, and InvalidResponseBinding semantics.

Extend CurrentFlatPromptBindingV1 so each I4C candidate is independently
validated for:

    family
    exact concrete runtime type
    exact choice kind
    exact source section
    nonnegative source ordinal
    exact canonical local key
    exact private signed i32 response
    CardCode subtype nonzero when present

For response construction, use:

    unchecked((int)(((uint)sourceOrdinal << 16) | (uint)kind))

after requiring sourceOrdinal <= 65535. Use scalar 2/3 for BATTLE
transitions and 6/7/8 for IDLE transitions. A wrong runtime subtype, swapped
key vector, duplicate key, wrong section, or wrong response returns
InvalidResponseBinding and creates no binding.

Update selection-domain equality for every new concrete subtype, including all
semantic fields and the accepted public locator, so stale handles fail closed.

- [ ] Step 6: Build the library and test project.

Run:

    dotnet build src/OCGForge.Ignis.Gameplay/OCGForge.Ignis.Gameplay.csproj --configuration Release
    dotnet build tests/OCGForge.Ignis.Gameplay.Tests/OCGForge.Ignis.Gameplay.Tests.csproj --configuration Release

Expected at this intermediate point: the library and test project compile once
the parser/session references are updated; no I3/I4A/I4B test behavior changes.

## Task 4: Add strict private BATTLECMD and IDLECMD parsing

File: src/OCGForge.Ignis.Gameplay/FlatPromptProjectionV1.cs

- [ ] Step 1: Extend the private parser dispatch.

Keep the existing I4A parser behavior unchanged. Extend the private wire-draft
entry point used by the authority overload:

    TryParseWireDraft(
        ReadOnlySpan<byte> bytes,
        out FlatPromptWireDraftV1? draft,
        out FlatPromptErrorCodeV1 error)

Dispatch exactly:

    10 → BATTLECMD parser
    11 → IDLECMD parser
    12 → existing I4B EFFECTYN parser
    16 → existing I4B CHAIN parser
    all other IDs → UnsupportedPromptLayout

The parser must never retain the caller span.

- [ ] Step 2: Parse BATTLECMD with exact widths and checked arithmetic.

Read in this exact sequence:

    id, player, activatable_count
    a entries of 19 bytes
    attackable_count
    b entries of 8 bytes
    to_main_phase_2, to_end_phase

Compute:

    checked wide total = 12 + (19 * a) + (8 * b)

Require exact supplied length before allocating entry arrays. Validate player
and controller as 0/1, direct_attackable and both transitions as 0/1,
client_mode as 0/1/2, and source base location/field sequence using the
existing Mirror address normalizer. Because these entries have no overlay
index, reject an overlay-bit source as UnprovenPublicReference during
projection rather than assuming overlay index zero.

Reject the frozen BATTLE legacy-narrow vector as UnsupportedPromptLayout.
Return MalformedPrompt for other truncation/trailing/count-body mismatches and
ArithmeticFailure when a checked size or representability limit fails.

- [ ] Step 3: Parse IDLECMD with heterogeneous typed sections.

Read in this exact sequence:

    id, player
    summon_count and 10-byte summon entries
    special_summon_count and 10-byte special-summon entries
    reposition_count and 7-byte reposition entries
    monster_set_count and 10-byte MSET entries
    spell_trap_set_count and 10-byte SSET entries
    activatable_count and 19-byte activation entries
    to_battle_phase, to_end_phase, shuffle_hand

Compute:

    checked wide total =
        29 + (10*s) + (10*ss) + (7*r)
           + (10*m) + (10*st) + (19*a)

Require exact supplied length before allocating any section arrays. Validate
all players/controllers, client modes, and three transition booleans. Preserve
the distinct u8 REPOSITION sequence width and the u32 widths everywhere else.
Use the same no-overlay-index rule as BATTLECMD.

Reject known legacy-width input as UnsupportedPromptLayout and other exact
length violations as MalformedPrompt. Do not infer any section from remaining
bytes.

- [ ] Step 4: Run focused parser tests and confirm complete private drafts.

Run the Gameplay harness. The parser tests must prove:

    no public Context/Candidates are created during parse
    malformed/unsupported parse failures produce null drafts
    source sections remain separate
    raw source fields exist only in private drafts
    no count causes allocation before the exact length gate

## Task 5: Build complete I4C public domains

File: src/OCGForge.Ignis.Gameplay/FlatPromptProjectionV1.cs

- [ ] Step 1: Project BATTLE activatable and attackable entries.

For every activatable entry in input order:

    correlate its private source reference through the I4B helper
    copy the accepted locator
    apply CARD_CODE_SAFE separately
    construct the exact no-Code or CardCode variant
    copy description/effect id and client mode
    set source section ACTIVATABLE and choice kind ACTIVATE
    bind key MSG_SELECT_BATTLECMD:ACTIVATE:i
    bind response (i << 16) | 0

For every attackable entry in input order:

    correlate its private source reference through the I4B helper
    copy the accepted locator
    apply CARD_CODE_SAFE separately
    construct the exact no-Code or CardCode variant
    copy direct_attackable
    set source section ATTACKABLE and choice kind ATTACK
    bind key MSG_SELECT_BATTLECMD:ATTACK:i
    bind response (i << 16) | 1

Do not expose attackable description/client mode or activatable
direct_attackable.

- [ ] Step 2: Project BATTLE transitions and validate zero domain.

After all activatable and attackable entries:

    if ToMainPhase2:
        append TO_M2 / MAIN_PHASE_2 / response 2
    if ToEndPhase:
        append TO_EP / END_PHASE / response 3

If no entry or transition exists, return ZeroOptionDomain with no partial
domain. A one-candidate domain remains selectable and is never auto-answered.

- [ ] Step 3: Project IDLE six sections in canonical order.

Append entries in this exact order:

    SUMMON
    SPECIAL_SUMMON
    REPOSITION
    MSET
    SSET
    ACTIVATE

For the first five sections construct the simple card-action base with the
exact section/choice pair and the accepted locator plus optional safe
CardCode. For ACTIVATE construct its separate description/client-mode base.
Use the exact section-local keys and response kinds 0 through 5.

Do not sort, deduplicate, or merge arrays. Preserve every source occurrence.

- [ ] Step 4: Project IDLE transitions and validate zero domain.

After all six card sections:

    if ToBattlePhase:
        append TO_BP / BATTLE_PHASE / response 6
    if ToEndPhase:
        append TO_EP / END_PHASE / response 7
    if ShuffleHand:
        append SHUFFLE_HAND / SHUFFLE_HAND / response 8

If the complete domain is empty, return ZeroOptionDomain with no partial
domain. Do not fabricate a pass or phase transition.

- [ ] Step 5: Reuse I4B correlation without adding authority.

Call only:

    FlatPromptCardCorrelationV1.TryCorrelate(
        capturedMirror,
        acceptedSnapshot,
        sourceCardCode,
        normalizedNonOverlaySourceLocation,
        out correlation,
        out error)

The accepted snapshot remains the only locator/CardCode publication source.
The mirror remains private resolution evidence. The recomputed projection
remains consistency evidence. A zero or mismatching source CardCode removes
only the CardCode member when the locator is otherwise proven. Hand/Extra
ambiguity and Main Deck lack of locator fail the complete prompt.

## Task 6: Integrate I4C into the existing session transaction

File: src/OCGForge.Ignis.Gameplay/FlatPromptSessionV1.cs

- [ ] Step 1: Preserve both existing interfaces.

Keep the one-argument I4A method exactly:

    TryAcceptPrompt(ReadOnlySpan<byte> completeInnerGameMessage)

Keep the accepted I4B per-call overload as the I4C entry point:

    TryAcceptPrompt(
        ReadOnlySpan<byte> completeInnerGameMessage,
        PerspectiveStateMirrorV1? mirror,
        PublicStateProjectionResultV1? acceptedProjection)

Do not add a session-owned authority context, new overload family, network
method, model method, response sender, or fallback path.

- [ ] Step 2: Apply the exact per-call authority order.

The overload must execute:

    parse complete private I4C draft
    validate accepted projection success and snapshot
    validate mirror presence
    capture mirror.Snapshot exactly once
    reproject captured snapshot using accepted Snapshot.DuelFlags
    compare CanonicalBytes exactly
    compare Sha256 ordinally
    compare PublicProjectionId ordinally
    correlate every card entry against captured/accepted snapshots
    build every candidate and transition
    independently validate concrete candidate/key/response bindings
    compute checked next ordinal
    commit binding, ordinal, and public result together

No live mirror access is permitted after the single capture.

- [ ] Step 3: Preserve failure atomicity and stale handles.

Every parser, authority, correlation, domain, binding, or overflow failure
must return:

    IsSuccess=false
    Context=null
    Candidates=null
    currentBinding=null
    ordinal unchanged
    no response sent

A prior handle must resolve as StalePromptBinding after any failed new prompt.
A new accepted prompt must replace the binding and increment the ordinal once.

- [ ] Step 4: Run all I4A/I4B tests after session integration.

Run:

    dotnet run --project tests/OCGForge.Ignis.Gameplay.Tests/OCGForge.Ignis.Gameplay.Tests.csproj --configuration Release

At this checkpoint all existing 85 groups must remain green. The final I4C
groups are expected to bring the result to 103/103.

## Task 7: Complete the 18-group I4C acceptance suite

File: tests/OCGForge.Ignis.Gameplay.Tests/Tests/I4CIdleBattlePromptTests.cs

- [ ] Step 1: Verify every positive vector through the real session interface.

Do not make the primary positive tests direct calls to private parser or
correlation helpers. Each family must be accepted through the per-call
TryAcceptPrompt overload with a real PerspectiveStateMirrorV1 and accepted
PublicStateProjectionResultV1. Direct internal helper tests may supplement,
but they do not replace the real prompt path.

- [ ] Step 2: Verify public/private structural absence through reflection.

Inspect every new public context/candidate record and assert absence of:

    response_i32, response bytes, ModernLocInfoV1,
    MirrorEntityIdV1, MirrorSnapshotV1, raw offsets,
    socket/network/process metadata, prompt ordinal, and public action key

Assert that every conditional CardCode is a distinct sealed runtime variant.

- [ ] Step 3: Verify deterministic source and section ordering.

Use the frozen mixed BATTLE vector and all-section IDLE vector. Assert every
candidate key, section, ordinal, choice kind, public field, transition token,
and response integer in exact order. Include duplicate-looking entries and
section duplicates. Use invariant culture and a second process for output
comparison.

- [ ] Step 4: Verify response-index limits.

Use table-driven boundary values:

    section ordinal 0
    section ordinal 1
    section ordinal 65535
    section count 65536
    section count 65537

The last case must fail before public domain/binding creation. The 65535
response must preserve its signed i32 bit pattern exactly.

- [ ] Step 5: Verify all authority/privacy failures.

Use accepted projections with independently changed:

    canonical bytes
    SHA-256
    PublicProjectionId
    DuelFlags causing reprojection disagreement

Also test:

    null mirror
    null projection
    failed projection
    missing public card
    ambiguous public card
    private mirror ambiguity
    known Hand/Extra duplicate CardCodes
    Main Deck source
    overlay bit without source overlay index
    zero/mismatching wire CardCode with proven locator

Every case must publish no partial domain and must invalidate the old handle.

## Task 8: Run complete regression and fresh-process determinism

Files: the five future implementation files only.

- [ ] Step 1: Run the complete Gameplay harness.

Run:

    dotnet run --project tests/OCGForge.Ignis.Gameplay.Tests/OCGForge.Ignis.Gameplay.Tests.csproj --configuration Release

Require:

    RESULT passed=103 failed=0

All original 85 I3/I4A/I4B groups must execute and pass. An all-skipped,
compile-only, or partial run is not acceptance.

- [ ] Step 2: Run Protocol, Client, and Gameplay twice as fresh child
  processes.

Run each compiled harness twice and compare complete stdout and exit codes:

    Protocol  20/20
    Client    17/17
    Gameplay  103/103

Require identical stdout per pair, identical exit codes per pair, and empty
stderr. Do not compare toolchain hashes as gameplay identities.

- [ ] Step 3: Verify I3D/I4A/I4B boundaries through the full Gameplay harness.

The final output must preserve:

    I3D golden canonical bytes
    I3D SHA-256 values and PublicProjectionIds
    paired-world privacy outputs
    I4A one-argument behavior
    I4B EFFECTYN/CHAIN authority and correlation behavior
    no mirror/public-locator authority drift

Inspect the production diff and confirm no new network, model, fallback,
public-action-key, raw-response, pointer, time, PID, path, or hidden-identity
seam exists.

## Task 9: Run all 12 Release build invocations

- [ ] Step 1: Build each project once with restore and once with no-restore.

Run these twelve separate invocations:

    dotnet build src/OCGForge.Ignis.Protocol/OCGForge.Ignis.Protocol.csproj --configuration Release
    dotnet build src/OCGForge.Ignis.Protocol/OCGForge.Ignis.Protocol.csproj --configuration Release --no-restore
    dotnet build src/OCGForge.Ignis.Client/OCGForge.Ignis.Client.csproj --configuration Release
    dotnet build src/OCGForge.Ignis.Client/OCGForge.Ignis.Client.csproj --configuration Release --no-restore
    dotnet build src/OCGForge.Ignis.Gameplay/OCGForge.Ignis.Gameplay.csproj --configuration Release
    dotnet build src/OCGForge.Ignis.Gameplay/OCGForge.Ignis.Gameplay.csproj --configuration Release --no-restore
    dotnet build tests/OCGForge.Ignis.Protocol.Tests/OCGForge.Ignis.Protocol.Tests.csproj --configuration Release
    dotnet build tests/OCGForge.Ignis.Protocol.Tests/OCGForge.Ignis.Protocol.Tests.csproj --configuration Release --no-restore
    dotnet build tests/OCGForge.Ignis.Client.Tests/OCGForge.Ignis.Client.Tests.csproj --configuration Release
    dotnet build tests/OCGForge.Ignis.Client.Tests/OCGForge.Ignis.Client.Tests.csproj --configuration Release --no-restore
    dotnet build tests/OCGForge.Ignis.Gameplay.Tests/OCGForge.Ignis.Gameplay.Tests.csproj --configuration Release
    dotnet build tests/OCGForge.Ignis.Gameplay.Tests/OCGForge.Ignis.Gameplay.Tests.csproj --configuration Release --no-restore

Require every invocation to exit 0 with 0 Warning(s) and 0 Error(s).

## Task 10: Scope audit, commit, push, and stop

- [ ] Step 1: Audit the pre-commit feature scope including untracked files.

Run before staging:

    $planHead = (git log -1 --format='%H' -- docs/superpowers/plans/2026-09-05-i4c-idlecmd-battlecmd-plan.md).Trim()
    $tracked = @(git diff --name-only $planHead)
    $untracked = @(git ls-files --others --exclude-standard)
    $changed = @($tracked + $untracked | Sort-Object -Unique)
    $expected = @(
        'src/OCGForge.Ignis.Gameplay/FlatPromptTypesV1.cs',
        'src/OCGForge.Ignis.Gameplay/FlatPromptProjectionV1.cs',
        'src/OCGForge.Ignis.Gameplay/FlatPromptSessionV1.cs',
        'tests/OCGForge.Ignis.Gameplay.Tests/Program.cs',
        'tests/OCGForge.Ignis.Gameplay.Tests/Tests/I4CIdleBattlePromptTests.cs'
    ) | Sort-Object
    if (@($changed).Count -ne 5 -or
        ($changed -join [Environment]::NewLine) -cne
        ($expected -join [Environment]::NewLine)) {
        throw "FEATURE_SCOPE_MISMATCH CHANGED=$($changed -join ',')"
    }
    Write-Output 'PLAN_HEAD_TO_WORKTREE_SCOPE=5_FILES_PASS'

The tracked-plus-untracked union is authoritative before staging. Do not add
docs, fixtures, contracts, build output, or unrelated changes.

- [ ] Step 2: Stage only the five feature/test files and verify the staged
  scope.

Run:

    git add -- src/OCGForge.Ignis.Gameplay/FlatPromptTypesV1.cs src/OCGForge.Ignis.Gameplay/FlatPromptProjectionV1.cs src/OCGForge.Ignis.Gameplay/FlatPromptSessionV1.cs tests/OCGForge.Ignis.Gameplay.Tests/Program.cs tests/OCGForge.Ignis.Gameplay.Tests/Tests/I4CIdleBattlePromptTests.cs
    git diff --cached --check
    git diff --cached --name-only

Require exactly five staged paths.

- [ ] Step 3: Commit the implementation with the exact subject.

Run:

    git commit -m "feat: implement I4C idle and battle prompts"

Do not amend the docs commit.

- [ ] Step 4: Verify feature-parent and original-base scopes.

Run:

    $base = 'ea0d7d51d988e201c246a6660e27bdd20402221b'
    $planHead = (git log -1 --format='%H' -- docs/superpowers/plans/2026-09-05-i4c-idlecmd-battlecmd-plan.md).Trim()
    $head = (git rev-parse HEAD).Trim()
    $parent = (git rev-parse HEAD^).Trim()
    $featureChanged = @(git diff --name-only $planHead $head | Sort-Object -Unique)
    $baseChanged = @(git diff --name-only $base $head | Sort-Object -Unique)
    if ($parent -cne $planHead) {
        throw "FEATURE_PARENT_MISMATCH PARENT=$parent EXPECTED=$planHead"
    }
    if (@($featureChanged).Count -ne 5) {
        throw "PLAN_HEAD_FEATURE_SCOPE_COUNT=$(@($featureChanged).Count)"
    }
    if (@($baseChanged).Count -ne 7) {
        throw "BASE_FEATURE_SCOPE_COUNT=$(@($baseChanged).Count)"
    }
    git diff --check $base $head
    if ($LASTEXITCODE -ne 0) { throw 'DIFF_CHECK_FAILED' }
    if (@(git status --short).Count -ne 0) {
        throw 'WORKTREE_NOT_CLEAN'
    }
    Write-Output "PLAN_HEAD_TO_FEATURE_HEAD=5"
    Write-Output "ORIGINAL_BASE_TO_FEATURE_HEAD=7"

- [ ] Step 5: Push only the authorized branch and stop.

Run:

    git push -u origin chris/i4c-idlecmd-battlecmd-design-plan
    git ls-remote origin refs/heads/chris/i4c-idlecmd-battlecmd-design-plan
    git status --short --branch

Require remote SHA equal local HEAD and a clean worktree. Do not create a PR,
merge, begin I4D, I5, or I6. Stop for independent implementation review.

## Future handoff fields

The future implementation worker must report concrete command output:

    TASK=I4C_IDLECMD_BATTLECMD_RUNTIME_01
    BASE=ea0d7d51d988e201c246a6660e27bdd20402221b
    PLAN_HEAD=exact committed plan SHA
    HEAD=exact feature commit SHA
    PARENT=PLAN_HEAD
    REMOTE_HEAD=exact pushed SHA
    BRANCH=chris/i4c-idlecmd-battlecmd-design-plan

    FILES_CHANGED_BRANCH_WIDE=7
    FEATURE_COMMIT_FILES_CHANGED=5
    PRODUCTION_FILES_CHANGED=3
    TEST_FILES_CHANGED=2
    FIXTURES_CHANGED=NO
    FROZEN_CONTRACT_CHANGED=NO

    BATTLECMD_DOMAIN_COMPLETE=YES
    IDLECMD_DOMAIN_COMPLETE=YES
    SOURCE_ORDER_PRESERVED=YES
    DUPLICATES_PRESERVED=YES
    PUBLIC_CARD_REFERENCE_AUTHORITY=I3D_ONLY
    MIRROR_PUBLIC_LOCATOR_AUTHORITY=NO
    CARD_CODE_SAFE_SEPARATION=PASS
    PRIVATE_RESPONSE_BINDING_PUBLIC=NO
    STALE_PROMPT_BINDING_REJECTED=YES
    FAILED_PROMPT_CREATES_BINDING=NO
    FAILED_PROMPT_ADVANCES_ORDINAL=NO
    N1_AUTO_ANSWER=NO
    FALLBACK_ADDED=NO
    NETWORK_RESPONSE_SENDING_ADDED=NO
    MODEL_INPUT_ADDED=NO

    CURRENT_GAMEPLAY_TEST_COUNT=85
    PLANNED_NEW_I4C_TEST_GROUPS=18
    EXPECTED_GAMEPLAY_TEST_COUNT=103
    PROTOCOL_TESTS=20/20
    CLIENT_TESTS=17/17
    GAMEPLAY_TESTS=103/103
    FRESH_PROCESS_DETERMINISM=PASS
    RELEASE_BUILD_INVOCATIONS=12
    WARNINGS=0
    ERRORS=0
    DIFF_CHECK=PASS

    I3_SEMANTICS_CHANGED=NO
    I4A_SEMANTICS_CHANGED=NO
    I4B_SEMANTICS_CHANGED=NO
    I4C_IMPLEMENTATION_STARTED=YES
    I4C_AUTHORIZED=YES
    I4D_AUTHORIZED=NO
    I5_AUTHORIZED=NO
    I6_AUTHORIZED=NO
    PR_CREATED=NO
    SELF_FINAL_PASS=NO
    WORKTREE=CLEAN
    STATUS=STOP_FOR_INDEPENDENT_IMPLEMENTATION_REVIEW

This current design/plan task itself must report:

    TASK=I4C_IDLECMD_BATTLECMD_DESIGN_PLAN
    BASE=ea0d7d51d988e201c246a6660e27bdd20402221b
    HEAD=exact docs commit SHA
    PARENT=authorized base or prior docs head as verified
    REMOTE_HEAD=exact pushed docs SHA
    BRANCH=chris/i4c-idlecmd-battlecmd-design-plan
    FILES_CHANGED=2
    DOC_FILES_CHANGED=2
    PRODUCTION_CODE_CHANGED=NO
    TEST_CODE_CHANGED=NO
    FIXTURES_CHANGED=NO
    FROZEN_CONTRACT_CHANGED=NO
    BATTLECMD_WIRE_GRAMMAR=RESOLVED
    IDLECMD_WIRE_GRAMMAR=RESOLVED
    BATTLECMD_RESPONSE_BINDINGS=RESOLVED
    IDLECMD_RESPONSE_BINDINGS=RESOLVED
    BATTLECMD_DOMAIN_MODEL=RESOLVED
    IDLECMD_DOMAIN_MODEL=RESOLVED
    CARD_CORRELATION_MODEL=RESOLVED
    TRANSITION_SEMANTICS=RESOLVED
    ZERO_DOMAIN_SEMANTICS=RESOLVED
    CURRENT_GAMEPLAY_TEST_COUNT=85
    PLANNED_NEW_I4C_TEST_GROUPS=18
    EXPECTED_GAMEPLAY_TEST_COUNT=103
    FUTURE_IMPLEMENTATION_FILES=5
    I3_SEMANTICS_CHANGED=NO
    I4A_SEMANTICS_CHANGED=NO
    I4B_SEMANTICS_CHANGED=NO
    I4C_IMPLEMENTATION_AUTHORIZED=NO
    I5_AUTHORIZED=NO
    I6_AUTHORIZED=NO
    PR_CREATED=NO
    WORKTREE=CLEAN
    STATUS=STOP_FOR_INDEPENDENT_DESIGN_PLAN_REVIEW
