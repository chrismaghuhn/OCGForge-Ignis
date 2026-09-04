# I4B EFFECTYN + CHAIN Runtime Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend the accepted I4A flat-prompt layer with complete, deterministic, privacy-safe EFFECTYN and CHAIN projections while preserving I3/I4A semantics and keeping response sending, model input, I4C, I5, and I6 out of scope.

**Architecture:** Keep the I4A one-argument `FlatPromptSessionV1` API unchanged and add one per-call authority overload for card-bearing prompts. Parse a private wire draft first, validate one captured mirror snapshot against the accepted I3D projection, correlate every card reference through a stateless internal helper, then commit the complete public domain and private binding atomically.

**Tech Stack:** C#/.NET 10, nullable reference types, immutable records and read-only collections, `System.Buffers.Binary`, existing Gameplay/Protocol/Client executable harnesses, existing I3D projection APIs, and no new packages or network/model dependencies.

---

## Current task boundary

This document is a design/plan artifact only. The current task may change only:

```text
docs/superpowers/specs/2026-09-04-i4b-effectyn-chain-design.md
docs/superpowers/plans/2026-09-04-i4b-effectyn-chain-plan.md
```

The current task must not change production code, tests, fixtures, frozen
contracts, project files, workflows, or generated evidence. The branch is
created from:

```text
BASE=fdd12ceda24b61f3855a5f272537ab9dcc968c4a
BRANCH=chris/i4b-effectyn-chain-design-plan
```

The future implementation is not authorized by this plan. It requires a
separate explicit authorization after independent review.

## Future implementation scope map

The later implementation continues from this branch's committed `PLAN_HEAD`.
Its exact feature scope is eight files:

```text
CREATE
src/OCGForge.Ignis.Gameplay/FlatPromptCardCorrelationV1.cs
src/OCGForge.Ignis.Gameplay/MirrorAddressNormalizationV1.cs
tests/OCGForge.Ignis.Gameplay.Tests/Tests/I4BEffectYnChainPromptTests.cs

MODIFY
src/OCGForge.Ignis.Gameplay/FlatPromptTypesV1.cs
src/OCGForge.Ignis.Gameplay/FlatPromptProjectionV1.cs
src/OCGForge.Ignis.Gameplay/FlatPromptSessionV1.cs
src/OCGForge.Ignis.Gameplay/PerspectiveStateMirrorV1.cs
tests/OCGForge.Ignis.Gameplay.Tests/Program.cs
```

The counts are fixed:

```text
FUTURE_IMPLEMENTATION_PRODUCTION_FILES=6
FUTURE_IMPLEMENTATION_TEST_FILES=2
FUTURE_IMPLEMENTATION_FILES=8
```

When implementation continues from `PLAN_HEAD`:

```text
PLAN_HEAD_TO_FEATURE_HEAD=8
ORIGINAL_BASE_TO_FEATURE_HEAD=10
```

The ten-file original-base diff is the two committed design/plan documents
plus the eight implementation/test files. The eight-file feature diff is the
implementation/test files only.

Files that must remain untouched by the future implementation:

```text
src/OCGForge.Ignis.Gameplay/GameplayMessageDecoderV1.cs
src/OCGForge.Ignis.Gameplay/GameplayMessageTypesV1.cs
src/OCGForge.Ignis.Gameplay/GameplayMirrorSessionV1.cs
src/OCGForge.Ignis.Gameplay/PublicStateProjectionV1.cs
src/OCGForge.Ignis.Gameplay/PublicSemanticLocatorV1.cs
tests/OCGForge.Ignis.Gameplay.Tests/Fixtures/*
fixtures/gameplay/v1/i4-flat-prompt-vectors.v1.json
fixtures/gameplay/v1/game-message-support.v1.json
docs/contracts/flat-prompt-projection-v1.md
Protocol/Client source and tests
network, model, workflow, and generated evidence files
```

`PerspectiveStateMirrorV1.cs` is the only accepted I3 source exception. Its
existing private address normalization is delegated to the new pure helper;
the state reducer, snapshot shape, entity identity, and all public semantics
remain unchanged.

## Task 1: Future implementation start guard

**Files:** none.

The document-only branch creation and docs commit are already complete by the
current task. A future implementation worker must begin from that committed
plan head; it must not switch the branch back to `main` or create a second
branch.

- [ ] **Step 1: Require the committed PLAN_HEAD, branch, base, and clean worktree.**

Run from `C:\Users\chris\Documents\OCGForge-Ignis`:

```powershell
git fetch origin --prune
$base = 'fdd12ceda24b61f3855a5f272537ab9dcc968c4a'
$planPath = 'docs/superpowers/plans/2026-09-04-i4b-effectyn-chain-plan.md'
$planHead = (git log -1 --format='%H' -- $planPath).Trim()
$head = (git rev-parse HEAD).Trim()
$branch = (git branch --show-current).Trim()
$remoteMain = (git rev-parse origin/main).Trim()
if ($head -cne $planHead) {
    throw "HEAD_NOT_PLAN_HEAD HEAD=$head EXPECTED=$planHead"
}
if ($branch -cne 'chris/i4b-effectyn-chain-design-plan') {
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
```

Require:

```text
HEAD=PLAN_HEAD
BRANCH=chris/i4b-effectyn-chain-design-plan
origin/main=fdd12ceda24b61f3855a5f272537ab9dcc968c4a
WORKTREE=CLEAN
```

- [ ] **Step 2: Verify that BASE→PLAN_HEAD contains exactly the two documents.**

Run:

```powershell
$expectedDocs = @(
    'docs/superpowers/specs/2026-09-04-i4b-effectyn-chain-design.md',
    'docs/superpowers/plans/2026-09-04-i4b-effectyn-chain-plan.md'
) | Sort-Object
$baseChanged = @(git diff --name-only $base $planHead | Sort-Object -Unique)
if (@($baseChanged).Count -ne 2 -or
    (@($baseChanged) -join "`n") -cne ($expectedDocs -join "`n")) {
    throw "BASE_TO_PLAN_SCOPE_MISMATCH CHANGED=$($baseChanged -join ',')"
}
Write-Output 'BASE_TO_PLAN_HEAD_SCOPE=2_DOCS_PASS'
```

Only after these guards pass may the worker begin Task 2. If any guard fails,
stop without editing implementation files; do not rebase, merge, or adopt a
newer base.

- [ ] **Step 3: Audit the live accepted inputs at PLAN_HEAD.**

Read the frozen contract, I4A design/plan, I4A implementation, I3 mirror and
I3D projection files named in the task. Confirm the current Gameplay harness
has exactly 67 registrations and the I4A baseline runs `67/67`. Confirm the
I4A0 pins remain EDOPro `30935e847165a9ef0e547fb51a43f36168fab7c7` and
ocgcore `46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57`.

## Task 2: Define the future red-test catalog

**Files:**

- Create: `tests/OCGForge.Ignis.Gameplay.Tests/Tests/I4BEffectYnChainPromptTests.cs`
- Modify: `tests/OCGForge.Ignis.Gameplay.Tests/Program.cs`

- [ ] **Step 1: Append exactly 18 top-level Gameplay registrations.**

Append these entries after the existing 67 without renaming, removing, or
reordering any existing entry:

```csharp
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
        I4BEffectYnChainPromptTests.TestI4AAndI3RegressionBoundary)
```

The fixed count is:

```text
CURRENT_GAMEPLAY_TEST_COUNT=67
PLANNED_NEW_I4B_TEST_GROUPS=18
EXPECTED_GAMEPLAY_TEST_COUNT=85
```

- [ ] **Step 2: Anchor tests to the frozen positive vectors.**

Use exact bytes and decoded values from:

```text
EFFECTYN_MODERN_LOC_INFO
CHAIN_OPTIONAL_WITH_NO_CHAIN
CHAIN_FORCED_TRIGGER_MARKER
CHAIN_OPTIONAL_EMPTY
```

Add independent duplicate-entry, forced-single-entry, and authority-mismatch
fixtures in the new test file. Do not edit either JSON fixture.

- [ ] **Step 3: Cover the eight EFFECTYN groups.**

The tests must assert the exact 24-byte layout, shared context, NO/YES order,
0/1 binding, malformed/trailing/legacy/player/controller/location failures,
all authority barriers, indexed/Hand/Extra/Overlay/Main Deck correlation,
CardCode-safe/unsafe variants, ambiguity, no raw sequence identity, source
ownership, stale rejection, and privacy reflection. The indexed group must
include a same-player/same-sequence cross-zone case and accepted
`SpellTrapZone`, `FieldZone`, and `PendulumRelevantState` cases. It must prove
that the accepted semantic zone participates in the match and that a card from
another indexed zone with the same sequence cannot match.

- [ ] **Step 4: Cover the eight CHAIN groups.**

The tests must assert exact shared context and `16 + 23*c` lengths, `0x7f`
marker semantics, source order, duplicates, Entry `i` bindings, forced
NO_CHAIN rules, optional empty chain, forced single-entry selection,
malformed/legacy/checked arithmetic/player/controller/mode failures, per-entry
correlation, ambiguity, CardCode safety, ownership, staleness, and atomicity.

- [ ] **Step 5: Run the required RED check.**

Run:

```powershell
dotnet run --project tests/OCGForge.Ignis.Gameplay.Tests/OCGForge.Ignis.Gameplay.Tests.csproj --configuration Release
```

Expected: compilation failure because I4B types, parser support, correlation,
and the authority overload are not yet implemented. Do not weaken the tests or
introduce fallback behavior.

## Task 3: Extract the shared pure Mirror address normalizer

**Files:**

- Create: `src/OCGForge.Ignis.Gameplay/MirrorAddressNormalizationV1.cs`
- Modify: `src/OCGForge.Ignis.Gameplay/PerspectiveStateMirrorV1.cs`

- [ ] **Step 1: Define the internal normalized-address record.**

Use:

```csharp
internal readonly record struct MirrorAddressNormalizationV1(
    byte Controller,
    MirrorZoneV1 Zone,
    uint Sequence,
    bool IsOverlay,
    uint OverlayIndex)
{
    internal static bool TryNormalize(
        ModernLocInfoV1 value,
        out MirrorAddressNormalizationV1 normalized,
        out GameplayErrorCode error);
}
```

Move the existing controller, location-bit, overlay-only-on-monster,
field-sequence, and overlay-index rules into this pure helper without changing
any accepted value or error.

- [ ] **Step 2: Delegate the existing Mirror private normalization.**

Keep the private nested `MirrorAddress` and all reducer callers unchanged. Make
the existing private `TryNormalizeAddress` wrapper call the shared helper and
convert its result to `MirrorAddress`. Do not expose `MirrorEntityIdV1`, public
zones, public locators, or card codes.

- [ ] **Step 3: Build the Gameplay library and defer the full harness until I4B compiles.**

Run:

```powershell
dotnet build src/OCGForge.Ignis.Gameplay/OCGForge.Ignis.Gameplay.csproj --configuration Release
```

The full Gameplay harness is intentionally red from Task 2 until the I4B
types and session overload exist. After Tasks 4 through 7, Task 8 runs all
67 accepted I3/I4A groups and the 18 I4B groups, including the I3D golden
canonical bytes, SHA-256 values, paired-world outputs, and projection IDs.
Any changed I3 evidence stops the plan with
`STATUS=BLOCKED_I3_AUTHORITY_GAP`.

## Task 4: Extend the discriminated I4 public and private value model

**File:** `src/OCGForge.Ignis.Gameplay/FlatPromptTypesV1.cs`

- [ ] **Step 1: Extend only the I4 family/choice/source enums.**

Add:

```csharp
FlatPromptFamilyV1.MsgSelectEffectYn = 12
FlatPromptFamilyV1.MsgSelectChain = 16

FlatPromptChoiceKindV1.ChainEntry
FlatPromptChoiceKindV1.NoChain

FlatPromptSourceSectionV1.ChainChoices
```

Do not add I4C families or any I5 value.

- [ ] **Step 2: Add closed EFFECTYN and CHAIN context records.**

Add these exact public shapes:

```csharp
public abstract record FlatPromptEffectYnPublicContextBaseV1
    : FlatPromptPublicContextV1
{
    public PublicSemanticLocatorV1 EffectCardLocator { get; }
    public ulong EffectDescriptionId { get; }
}

public sealed record FlatPromptEffectYnPublicContextV1
    : FlatPromptEffectYnPublicContextBaseV1
{
}

public sealed record FlatPromptEffectYnCardCodePublicContextV1
    : FlatPromptEffectYnPublicContextBaseV1
{
    public uint EffectCardCode { get; }
}

public sealed record FlatPromptChainPublicContextV1
    : FlatPromptPublicContextV1
{
    public byte ChainSpeCount { get; }
    public bool ChainForced { get; }
    public uint ChainHintTimingForPlayer { get; }
    public uint ChainHintTimingForOtherPlayer { get; }
}
```

Every concrete type is sealed. There is no nullable CardCode property and no
raw wire-field property.

- [ ] **Step 3: Add closed EFFECTYN and CHAIN candidate records.**

Add:

```csharp
public sealed record FlatEffectYnPublicCandidateDescriptorV1
    : FlatPublicCandidateDescriptorV1
{
}

public sealed record FlatChainNoChainPublicCandidateDescriptorV1
    : FlatPublicCandidateDescriptorV1
{
}

public abstract record FlatChainEntryPublicCandidateDescriptorBaseV1
    : FlatPublicCandidateDescriptorV1
{
    public FlatPromptSourceSectionV1 SourceSection { get; }
    public int SourceOrdinal { get; }
    public PublicSemanticLocatorV1 PublicSemanticCardLocator { get; }
    public ulong DescriptionOrEffectId { get; }
    public byte ClientMode { get; }
}

public sealed record FlatChainPublicCandidateDescriptorV1
    : FlatChainEntryPublicCandidateDescriptorBaseV1
{
}

public sealed record FlatChainCardCodePublicCandidateDescriptorV1
    : FlatChainEntryPublicCandidateDescriptorBaseV1
{
    public uint CardCode { get; }
}
```

`NO_CHAIN` has no source, locator, description, mode, or CardCode member.

- [ ] **Step 4: Extend binding validation for every new concrete runtime type.**

Extend `CurrentFlatPromptBindingV1.TryGetExpectedBinding` so it validates the
full tuple before creating a binding:

```text
EFFECTYN + FlatEffectYn... + NO
    key=MSG_SELECT_EFFECTYN:NO, response=0

EFFECTYN + FlatEffectYn... + YES
    key=MSG_SELECT_EFFECTYN:YES, response=1

CHAIN + FlatChainEntry... + CHAIN_ENTRY
    source_section=CHAIN_CHOICES
    key=MSG_SELECT_CHAIN:CHAIN_ENTRY:<source_ordinal>
    response=source_ordinal

CHAIN + FlatChainNoChain... + NO_CHAIN
    key=MSG_SELECT_CHAIN:NO_CHAIN
    response=-1
```

Require candidate count/key count/response count equality, nonzero domain,
non-null values, ordinal ASCII key alignment, family/subtype alignment,
choice-kind alignment, exact expected key, exact expected response, and
ordinal key uniqueness with `StringComparer.Ordinal`. A failure returns
`InvalidResponseBinding` and no binding.

- [ ] **Step 5: Define the private wire and per-call authority values used by the parser split.**

Add these internal value-owned types in `FlatPromptTypesV1.cs`:

```csharp
internal abstract record FlatPromptWireDraftV1(
    FlatPromptFamilyV1 Family);

internal sealed record FlatPromptEffectYnWireDraftV1(
    byte ActingPlayer,
    uint SourceCardCode,
    ModernLocInfoV1 SourceLocation,
    ulong EffectDescriptionId)
    : FlatPromptWireDraftV1(FlatPromptFamilyV1.MsgSelectEffectYn);

internal readonly record struct FlatPromptChainWireEntryV1(
    uint SourceCardCode,
    ModernLocInfoV1 SourceLocation,
    ulong DescriptionOrEffectId,
    byte ClientMode);

internal sealed record FlatPromptChainWireDraftV1(
    byte ActingPlayer,
    byte SpeCount,
    bool Forced,
    uint HintTimingForPlayer,
    uint HintTimingForOtherPlayer,
    FlatPromptChainWireEntryV1[] Entries)
    : FlatPromptWireDraftV1(FlatPromptFamilyV1.MsgSelectChain);

internal sealed record FlatPromptCardAuthorityContextV1(
    MirrorSnapshotV1 CapturedMirror,
    PublicStateSnapshotV1 AcceptedSnapshot);
```

The constructors must copy entry arrays and reject null values internally.
These types are never public, never retained by the session, and never
serialized into public canonical data. Extend `FlatPromptErrorCodeV1` with
`InvalidLocation`, `InvalidBoolean`, `InvalidClientMode`, and
`AuthorityMismatch` while retaining all I4A error values unchanged.

## Task 5: Add private I4B wire drafts and strict parsing

**File:** `src/OCGForge.Ignis.Gameplay/FlatPromptProjectionV1.cs`

- [ ] **Step 1: Define the private parse/project split.**

Add internal immutable private drafts for EFFECTYN and CHAIN. The parser entry
point is:

```csharp
internal static bool TryParseWireDraft(
    ReadOnlySpan<byte> bytes,
    out FlatPromptWireDraftV1? draft,
    out FlatPromptErrorCodeV1 error);
```

The projection entry point consumes a fully parsed draft and optional per-call
authority:

```csharp
internal static bool TryBuildProjectedDraft(
    FlatPromptWireDraftV1 draft,
    FlatPromptCardAuthorityContextV1? authority,
    out FlatPromptProjectionDraftV1? projected,
    out FlatPromptErrorCodeV1 error);
```

The private wire draft owns raw `ModernLocInfoV1` and source CardCode only
until correlation. Neither draft type is public or retained by the session.

- [ ] **Step 2: Parse EFFECTYN exactly and fail closed before correlation.**

Accept only 24 bytes:

```text
u8       12
u8       player
u32_le   card_code
ModernLocInfoV1 10 bytes
u64_le   description
```

Reject every other width, including legacy/compatibility widths. Validate
`player` before reading semantic fields. Preserve raw location and CardCode
only in the private draft. Do not construct a public context or candidate at
this phase.

- [ ] **Step 3: Parse CHAIN exactly with checked length arithmetic.**

Accept the fixed 16-byte header and 23-byte entries:

```text
u8       16
u8       player
u8       spe_count
u8       forced
u32_le   hint_timing_for_player
u32_le   hint_timing_for_other_player
u32_le   chain_count = c
repeat c:
    u32_le card_code
    ModernLocInfoV1 10 bytes
    u64_le description
    u8 client_mode
```

Compute `16 + 23*c` with checked arithmetic in a wide intermediate before
allocating entries. Require exact supplied length, `player ∈ {0,1}`,
`forced ∈ {0,1}`, and `client_mode ∈ {0,1,2}`. Preserve exact `spe_count`;
`0x7f` is a marker and never an entry count. No alternate width is accepted.

- [ ] **Step 4: Build the complete private-to-public CHAIN domain only after correlation.**

For each parsed entry in input order, call the correlation helper. If any
entry fails, discard every entry and return `UnprovenPublicReference` or the
specific structured error. If all entries succeed, create one candidate per
entry, preserving duplicate values and exact source ordinal. Append NO_CHAIN
after entries only when `forced == false`. Require `c >= 1` when forced.

The no-chain candidate is not a fabricated pass; it is the exact protocol
sentinel `-1` authorized by `forced=false`.

## Task 6: Implement the stateless private card-correlation helper

**File:** `src/OCGForge.Ignis.Gameplay/FlatPromptCardCorrelationV1.cs`

- [ ] **Step 1: Define the stateless helper and ephemeral result.**

Use this internal shape:

```csharp
internal static class FlatPromptCardCorrelationV1
{
    internal static bool TryCorrelate(
        MirrorSnapshotV1 capturedMirror,
        PublicStateSnapshotV1 acceptedSnapshot,
        uint sourceCardCode,
        ModernLocInfoV1 sourceLocation,
        out FlatPromptCardCorrelationResultV1? result,
        out FlatPromptErrorCodeV1 error);
}

internal sealed class FlatPromptCardCorrelationResultV1
{
    internal PublicSemanticLocatorV1 AcceptedLocator { get; }
    internal uint? SafeCardCode { get; }
}
```

The result contains only the exact accepted public locator and a conditional
safe CardCode. The helper owns no fields, mirror, snapshot, dictionary, cache,
or retained lookup state.

- [ ] **Step 2: Resolve the wire location privately against the captured snapshot.**

Call `MirrorAddressNormalizationV1.TryNormalize` and map each captured
`MirrorCardSnapshotV1.Controller` to an absolute player using the existing
perspective mapping. Require exactly one captured card matching:

```text
absolute controller
MirrorZoneV1
sequence
overlay flag
overlay index when overlay
```

Use the resolved Mirror card's normalized facts for every later step. Do not
re-read the original mirror or reuse raw `ModernLocInfoV1.position` after this
resolution. If zero or multiple captured cards match, return
`UnprovenPublicReference`.

- [ ] **Step 3: Correlate indexed cards using existing accepted I3D semantics.**

For non-pile, non-overlay resolved cards, inspect accepted
`PublicCardStateV1` entries and their already-classified `Zone` and
`PublicSemanticLocatorV1`. Use the existing locator codec only to validate a
candidate locator shape for comparison. First apply this compatibility
predicate; it never chooses a public zone:

```text
INDEXED_ZONE_COMPATIBLE(resolved MirrorZoneV1, accepted PublicSemanticZoneV1)

MirrorZoneV1.MonsterZone
    ↔ PublicSemanticZoneV1.MonsterZone only

MirrorZoneV1.Graveyard
    ↔ PublicSemanticZoneV1.Graveyard only

MirrorZoneV1.Banished
    ↔ PublicSemanticZoneV1.Banished only

MirrorZoneV1.SpellTrapZone
    ↔ PublicSemanticZoneV1.SpellTrapZone
      or PublicSemanticZoneV1.FieldZone
      or PublicSemanticZoneV1.PendulumRelevantState
```

For the surviving accepted card, perform the comparison with the existing
I3D locator codec rather than parsing locator strings in I4B:

```text
1. acceptedCard.AbsolutePlayer == resolved absolute player
2. INDEXED_ZONE_COMPATIBLE(resolved MirrorZoneV1, acceptedCard.Zone)
3. PublicSemanticLocatorV1.TryCreateIndexed(
       resolved absolute player,
       acceptedCard.Zone,
       resolved Mirror sequence,
       out expectedLocator)
4. require TryCreateIndexed succeeded
5. require acceptedCard.Locator == expectedLocator
```

`expectedLocator` is a local comparison value only. Do not store, return, or
cache it. Publish only the exact `acceptedCard.Locator` from the accepted I3D
snapshot.

Then require exactly one accepted card with all three required indexed facts:

```text
exact absolute player
exact accepted PublicSemanticZoneV1 already classified by I3D
exact resolved indexed sequence
```

The accepted card's existing indexed locator must equal the local
`TryCreateIndexed` comparison value for the exact player, accepted zone, and
sequence. The comparison value is never published, stored, returned, or
cached.
For SpellTrapZone, I4B accepts only the three listed I3D-classified values and
does not select among them.

The indexed tests must use one table-driven matrix containing these six
allowed pairs and rejecting every other pair in this indexed domain:

```text
MonsterZone   → MonsterZone
Graveyard     → Graveyard
Banished      → Banished
SpellTrapZone → SpellTrapZone
SpellTrapZone → FieldZone
SpellTrapZone → PendulumRelevantState
```

The matrix belongs to the existing `TestEffectYnIndexedCorrelation` and
`TestChainCorrelationAuthorityAndCardCodeSafety` groups. It does not add a
new top-level test registration.

The helper must not add a `MirrorZoneV1 → PublicSemanticZoneV1` switch. The
accepted snapshot's existing public `Zone` is the I3D classification authority.
If the accepted public facts cannot prove one card, fail closed.

- [ ] **Step 4: Correlate Hand/Extra and Overlay without raw sequence identity.**

For Hand/Extra:

```text
resolved Mirror card has known/proven nonzero CardCode
absolute player and zone match
exactly one accepted public card has that resolved proven CardCode
```

Do not use raw Hand/Extra sequence, collection order, physical continuity,
allocation order, or Mirror entity identity. Duplicate accepted public cards
with the same resolved code are ambiguous and fail. The wire source CardCode
is evaluated only afterward by the separate `CARD_CODE_SAFE` predicate; a zero
or mismatching wire code removes only the public CardCode member and does not
invalidate the proven locator.

For Overlay, use only the resolved Mirror card's absolute player, parent
sequence, and overlay index. Require exactly one accepted card already carrying
the matching I3D overlay locator. Do not derive the overlay index again from
the wire field.

Main Deck has no V1 public locator and always fails correlation.

- [ ] **Step 5: Apply `CARD_CODE_SAFE` separately from locator proof.**

After one public card is proven:

```text
sourceCardCode != 0
acceptedCard.CardCode present
acceptedCard.CardCode == sourceCardCode
    → return safe CardCode

otherwise
    → return no CardCode
    → retain proven accepted locator
```

For Hand/Extra, the proven CardCode is required for the correlation itself.
For indexed/overlay cards, an unsafe separate `CARD_CODE_SAFE` predicate
removes only the CardCode member and never removes the proven locator/domain.

## Task 7: Add per-call authority validation and atomic session integration

**File:** `src/OCGForge.Ignis.Gameplay/FlatPromptSessionV1.cs`

- [ ] **Step 1: Preserve the existing I4A API and add only the I4B overload.**

Keep:

```csharp
public FlatPromptProjectionResultV1 TryAcceptPrompt(
    ReadOnlySpan<byte> completeInnerGameMessage)
```

Add:

```csharp
public FlatPromptProjectionResultV1 TryAcceptPrompt(
    ReadOnlySpan<byte> completeInnerGameMessage,
    PerspectiveStateMirrorV1? mirror,
    PublicStateProjectionResultV1? acceptedProjection)
```

The one-argument I4A path must produce the same YESNO/OPTION/POSITION values
and has no authority dependency. The overload is the only path that can
successfully project EFFECTYN or CHAIN.

- [ ] **Step 2: Enforce the exact I4B transaction order.**

Implement this sequence:

```text
0. TryParseWireDraft; on failure clear currentBinding and stop.
1. Require acceptedProjection != null.
2. Require acceptedProjection.IsSuccess.
3. Require acceptedProjection.Snapshot != null.
4. Require mirror != null.
5. Capture MirrorSnapshotV1 capturedMirror = mirror.Snapshot exactly once.
6. Reproject capturedMirror using acceptedSnapshot.DuelFlags.
7. Require recomputedProjection.IsSuccess.
8. Compare recomputed CanonicalBytes byte-for-byte.
9. Compare recomputed Sha256 with StringComparison.Ordinal.
10. Compare recomputed PublicProjectionId with StringComparison.Ordinal.
11. Pass only capturedMirror and acceptedSnapshot to card correlation.
12. Build every public context/candidate from successful private results.
13. Validate binding Family + runtime type + ChoiceKind + Key + Response.
14. Compute checked next ordinal without mutating state.
15. Commit currentBinding and next ordinal together.
16. Return the public success result.
```

After step 5, no code may access `mirror` again. `recomputedProjection.Snapshot`
is consistency evidence only; accepted snapshot values are the sole publication
source.

- [ ] **Step 3: Preserve failure atomicity.**

Every failure from parsing, authority validation, reprojection, byte/hash/ID
mismatch, private resolution, public correlation, candidate construction,
binding construction, or ordinal overflow must produce:

```text
IsSuccess=false
Context=null
Candidates=null
currentBinding=null
nextPromptOrdinal unchanged
no response sent
```

No previous binding remains usable after a failed new prompt. A valid
`forced=false,c=0` CHAIN prompt is the only one-candidate case and still waits
for explicit selection.

- [ ] **Step 4: Extend selection resolution without public response exposure.**

Reuse the accepted I4A opaque value-owned selection handle. Validate current
ordinal, family, complete ordered domain, runtime candidate type, exact local
key, and private response map before returning the internal signed `i32`.
Do not expose response bytes/integer publicly and do not write
`CTOS_RESPONSE`.

## Task 8: Run focused green tests and the complete regression suite

**Files:** the eight future implementation/test files only.

- [ ] **Step 1: Run the complete Gameplay harness.**

Run:

```powershell
dotnet run --project tests/OCGForge.Ignis.Gameplay.Tests/OCGForge.Ignis.Gameplay.Tests.csproj --configuration Release
```

Expected:

```text
RESULT passed=85 failed=0
```

The original 67 tests must remain green. Every one of the 18 I4B registrations
must execute; an all-skipped or compile-only result is not a pass.

- [ ] **Step 2: Run the three harnesses in two fresh processes each.**

For Protocol, Client, and Gameplay, execute each compiled harness twice as a
fresh child process. Compare complete stdout byte-for-byte and exit codes. The
required results are:

```text
Protocol 20/20
Client 17/17
Gameplay 85/85
stdout identical per pair
exit codes identical per pair
```

- [ ] **Step 3: Verify I3/I4A semantic and privacy boundaries.**

Run the existing I3D golden, canonical-byte, paired-world, and API-boundary
tests through the full harness. Inspect the production diff and assert:

```text
I3_MIRROR_BEHAVIOR=BYTE/SEMANTIC_IDENTICAL
I3D_GOLDEN_CANONICAL_BYTES=UNCHANGED
I3D_PUBLIC_PROJECTION_ID=UNCHANGED
I3_PRIVACY_PAIRED_WORLDS=PASS
I4A_REGRESSION=UNCHANGED
PUBLIC_CARD_REFERENCE_AUTHORITY=I3D_ONLY
MIRROR_PUBLIC_LOCATOR_AUTHORITY=NO
```

Search production files for network writes, model inputs, public action keys,
raw response fields, raw Mirror identity, pointer/time/PID/path identity, and
fallback names. Any match that is not an existing allowed contract reference
fails the review.

## Task 9: Build, scope-audit, commit, push, and stop

**Files:** no additional files.

- [ ] **Step 1: Run all 12 Release build invocations.**

Build these six projects once with restore and once with `--no-restore`:

```text
src/OCGForge.Ignis.Protocol/OCGForge.Ignis.Protocol.csproj
src/OCGForge.Ignis.Client/OCGForge.Ignis.Client.csproj
src/OCGForge.Ignis.Gameplay/OCGForge.Ignis.Gameplay.csproj
tests/OCGForge.Ignis.Protocol.Tests/OCGForge.Ignis.Protocol.Tests.csproj
tests/OCGForge.Ignis.Client.Tests/OCGForge.Ignis.Client.Tests.csproj
tests/OCGForge.Ignis.Gameplay.Tests/OCGForge.Ignis.Gameplay.Tests.csproj
```

Every invocation must exit 0 with `0 Warning(s)` and `0 Error(s)`. Record all
12 invocations; do not infer them from one aggregate build.

- [ ] **Step 2: Audit the pre-commit feature scope including untracked files.**

Run from the implementation branch before staging:

```powershell
$planHead = (git log -1 --format='%H' -- docs/superpowers/plans/2026-09-04-i4b-effectyn-chain-plan.md).Trim()
$tracked = @(git diff --name-only $planHead)
$untracked = @(git ls-files --others --exclude-standard)
$changed = @($tracked + $untracked | Sort-Object -Unique)
$expected = @(
    'src/OCGForge.Ignis.Gameplay/FlatPromptCardCorrelationV1.cs',
    'src/OCGForge.Ignis.Gameplay/MirrorAddressNormalizationV1.cs',
    'src/OCGForge.Ignis.Gameplay/FlatPromptTypesV1.cs',
    'src/OCGForge.Ignis.Gameplay/FlatPromptProjectionV1.cs',
    'src/OCGForge.Ignis.Gameplay/FlatPromptSessionV1.cs',
    'src/OCGForge.Ignis.Gameplay/PerspectiveStateMirrorV1.cs',
    'tests/OCGForge.Ignis.Gameplay.Tests/Program.cs',
    'tests/OCGForge.Ignis.Gameplay.Tests/Tests/I4BEffectYnChainPromptTests.cs'
) | Sort-Object
if (@($changed).Count -ne 8 -or
    (@($changed) -join "`n") -cne (@($expected) -join "`n")) {
    throw "FEATURE_SCOPE_MISMATCH CHANGED=$($changed -join ',')"
}
Write-Output 'PLAN_HEAD_TO_WORKTREE_SCOPE=8_FILES_PASS'
```

`git diff --stat $planHead` is informational only because it omits untracked
files. The tracked-plus-untracked union is authoritative before staging.

- [ ] **Step 3: Commit the future implementation with the exact subject.**

After all tests and builds pass, stage only the eight feature files and run:

```powershell
git commit -m "feat: implement I4B effect and chain prompts"
```

Do not amend the docs-only commit. Do not stage fixtures, contracts, build
output, or unrelated work.

- [ ] **Step 4: Verify both feature-parent and original-base scope.**

Run:

```powershell
$base = 'fdd12ceda24b61f3855a5f272537ab9dcc968c4a'
$planHead = (git log -1 --format='%H' -- docs/superpowers/plans/2026-09-04-i4b-effectyn-chain-plan.md).Trim()
$head = (git rev-parse HEAD).Trim()
$featureChanged = @(git diff --name-only $planHead $head | Sort-Object -Unique)
$baseChanged = @(git diff --name-only $base $head | Sort-Object -Unique)
if (@($featureChanged).Count -ne 8) {
    throw "PLAN_HEAD_FEATURE_SCOPE_COUNT=$(@($featureChanged).Count)"
}
if (@($baseChanged).Count -ne 10) {
    throw "BASE_FEATURE_SCOPE_COUNT=$(@($baseChanged).Count)"
}
git diff --check $base $head
if ($LASTEXITCODE -ne 0) { throw 'DIFF_CHECK_FAILED' }
if (@(git status --short).Count -ne 0) { throw 'WORKTREE_NOT_CLEAN' }
Write-Output "PLAN_HEAD_TO_FEATURE_HEAD=8"
Write-Output "ORIGINAL_BASE_TO_FEATURE_HEAD=10"
```

The feature diff must be exactly the eight listed implementation/test paths;
the original-base diff must be those eight plus the two docs.

- [ ] **Step 5: Push only the authorized branch and stop.**

Run:

```powershell
git push -u origin chris/i4b-effectyn-chain-design-plan
git ls-remote origin refs/heads/chris/i4b-effectyn-chain-design-plan
git status --short --branch
```

The remote branch SHA must equal local `HEAD`. Do not create a PR, merge, begin
I4C, begin I4D, begin I5, or begin I6. Stop for independent review.

## Future implementation handoff fields

Report concrete values from executed commands using these fields:

```text
TASK=I4B_EFFECTYN_CHAIN_RUNTIME_01
BASE=fdd12ceda24b61f3855a5f272537ab9dcc968c4a
PLAN_HEAD=the exact committed plan SHA
HEAD=the exact feature commit SHA
PARENT=PLAN_HEAD
REMOTE_HEAD=the exact pushed SHA
BRANCH=chris/i4b-effectyn-chain-design-plan

FILES_CHANGED_BRANCH_WIDE=10
FEATURE_COMMIT_FILES_CHANGED=8
PRODUCTION_FILES_CHANGED=6
TEST_FILES_CHANGED=2
FIXTURES_CHANGED=NO
FROZEN_CONTRACT_CHANGED=NO

EFFECTYN_DOMAIN_COMPLETE=YES
CHAIN_DOMAIN_COMPLETE=YES
CHAIN_SOURCE_ORDER_PRESERVED=YES
CHAIN_DUPLICATES_PRESERVED=YES
CHAIN_NO_CHAIN_FORCED_RULE=PASS
PUBLIC_CARD_REFERENCE_AUTHORITY=I3D_ONLY
MIRROR_PUBLIC_LOCATOR_AUTHORITY=NO
AMBIGUOUS_PUBLIC_CORRELATION=FAIL_CLOSED
PRIVATE_RESPONSE_BINDING_PUBLIC=NO
STALE_PROMPT_BINDING_REJECTED=YES
FAILED_PROMPT_CREATES_BINDING=NO
FAILED_PROMPT_ADVANCES_ORDINAL=NO
N1_AUTO_ANSWER=NO
FALLBACK_ADDED=NO
NETWORK_RESPONSE_SENDING_ADDED=NO
MODEL_INPUT_ADDED=NO

CURRENT_GAMEPLAY_TEST_COUNT=67
PLANNED_NEW_I4B_TEST_GROUPS=18
EXPECTED_GAMEPLAY_TEST_COUNT=85
PROTOCOL_TESTS=20/20
CLIENT_TESTS=17/17
GAMEPLAY_TESTS=85/85
FRESH_PROCESS_DETERMINISM=PASS
RELEASE_BUILD_INVOCATIONS=12
WARNINGS=0
ERRORS=0
DIFF_CHECK=PASS

I3_SEMANTICS_CHANGED=NO
I3_PUBLIC_CANONICAL_BYTES_CHANGED=NO
I3_PUBLIC_PROJECTION_ID_CHANGED=NO
I4A_SEMANTICS_CHANGED=NO
I4B_IMPLEMENTATION_STARTED=YES
I4C_AUTHORIZED=NO
I5_AUTHORIZED=NO
I6_AUTHORIZED=NO
PR_CREATED=NO
SELF_FINAL_PASS=NO
WORKTREE=CLEAN
STATUS=STOP_FOR_INDEPENDENT_REVIEW
```

Independent review owns I4B acceptance. This plan never self-authorizes the
future implementation and never authorizes I4C, I5, or I6.
