# OCGForge-Ignis I4B — EFFECTYN + CHAIN Runtime Design

Status: `PROPOSED / pending independent review`

Date: 2026-09-04

Repository: `https://github.com/chrismaghuhn/OCGForge-Ignis`

Authorized base: `fdd12ceda24b61f3855a5f272537ab9dcc968c4a`

Design branch: `chris/i4b-effectyn-chain-design-plan`

This document defines the next I4B slice after the accepted I4A integration.
It does not authorize or implement production code. The frozen I4A0 contract,
the accepted I4A runtime, and the I3D public-state contract remain normative.

## 1. Scope and non-goals

I4B covers exactly:

```text
MSG_SELECT_EFFECTYN = 12
MSG_SELECT_CHAIN    = 16
```

The already accepted I4A families remain unchanged:

```text
MSG_SELECT_YESNO    = 13
MSG_SELECT_OPTION   = 14
MSG_SELECT_POSITION = 19
```

I4B does not implement `MSG_SELECT_BATTLECMD` or `MSG_SELECT_IDLECMD`; those
belong to I4C. It also does not begin I4D, I5 continuations, I6 OCGForge
compatibility, model input, model-runner work, network response sending, or
any fallback policy.

The priorities remain:

```text
correctness
→ determinism
→ information safety
→ complete legal decisions
→ replay/auditability
→ maintainability
→ performance
→ ML scale
```

Unsupported, malformed, ambiguous, stale, or unproven data fails closed. A
candidate domain is published only after its complete legal source domain and
public semantics are proven.

## 2. Ownership and data flow

I4B is owned by `OCGForge.Ignis.Gameplay`. The existing responsibilities stay
separate:

```text
GameplayMessageDecoderV1
    I3 state/query GAME_MSG decoding

GameplayMirrorSessionV1
    I3 perspective-state lifecycle

PerspectiveStateMirrorV1
    private perspective-safe state evolution

PublicStateProjectionV1 / PublicStateProjectionResultV1
    I3D public snapshot, locator, card-code, canonical-byte authority

FlatPromptSessionV1
    I4 prompt lifecycle, ordinal, current private binding

FlatPromptProjectionV1
    complete private I4 wire draft and public candidate draft

FlatPromptCardCorrelationV1
    stateless private MirrorSnapshot → accepted public snapshot correlation
```

The I4A one-argument API remains unchanged:

```csharp
FlatPromptProjectionResultV1 TryAcceptPrompt(
    ReadOnlySpan<byte> completeInnerGameMessage)
```

I4B adds one overload for card-bearing families:

```csharp
FlatPromptProjectionResultV1 TryAcceptPrompt(
    ReadOnlySpan<byte> completeInnerGameMessage,
    PerspectiveStateMirrorV1? mirror,
    PublicStateProjectionResultV1? acceptedProjection)
```

The nullable authority parameters are deliberate. Missing authority produces a
structured `UnprovenPublicReference` failure; it never throws and never
publishes a partial domain. The one-argument I4A call remains semantically
unchanged and has no dependency on a mirror or public projection.

## 3. Authority model

The three authority values have distinct roles:

```text
capturedMirror
    PRIVATE RESOLUTION AUTHORITY ONLY

acceptedProjection.Snapshot
    PUBLICATION AUTHORITY ONLY

recomputedProjection
    CONSISTENCY PROOF ONLY
```

For an I4B overload call, the transaction is ordered exactly as follows:

```text
0. Strictly parse the complete private I4B wire draft.
1. Require acceptedProjection != null.
2. Require acceptedProjection.IsSuccess.
3. Require acceptedProjection.Snapshot != null.
4. Require mirror != null.
5. Capture MirrorSnapshotV1 capturedMirror = mirror.Snapshot exactly once.
6. Reproject capturedMirror using acceptedProjection.Snapshot.DuelFlags.
7. Require recomputedProjection.IsSuccess.
8. Compare CanonicalBytes byte-for-byte.
9. Compare Sha256 with StringComparison.Ordinal.
10. Compare PublicProjectionId with StringComparison.Ordinal.
11. Resolve every ModernLocInfoV1 only against capturedMirror.
12. Correlate each resolved card to exactly one accepted snapshot card.
13. Copy locator and conditionally safe CardCode only from accepted snapshot.
14. Construct the complete public domain.
15. Validate the private Candidate↔Key↔Response binding independently.
16. Atomically replace the current binding, advance the ordinal, and publish.
```

After step 5, the original `PerspectiveStateMirrorV1` reference is never
read again in that call. `FlatPromptCardCorrelationV1` receives only the
captured `MirrorSnapshotV1` and the accepted `PublicStateSnapshotV1`; it does
not retain either value after the call and has no lookup cache.

The recomputed snapshot is never a publication source. Even when all three
consistency comparisons pass, the published `PublicSemanticLocatorV1` and
CardCode values are copied from `acceptedProjection.Snapshot.Cards` only.

## 4. Shared Mirror address normalization

The current `PerspectiveStateMirrorV1` already owns deterministic private
normalization of `ModernLocInfoV1` into:

```text
controller
MirrorZoneV1
sequence
is_overlay
overlay_index
```

I4B must not duplicate or reinterpret that mapping. A semantically neutral
internal pure helper will be extracted:

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

The helper contains the existing rules without semantic variation:

- controller must be 0 or 1;
- the location base must be one of the existing deck, extra, hand, monster,
  spell/trap, graveyard, or banished locations;
- the overlay bit is legal only with the monster zone;
- non-overlay field sequences use the existing Mirror sequence bounds;
- overlay index is taken from the existing normalized overlay convention.

`PerspectiveStateMirrorV1` delegates its current private normalization to this
helper and converts the neutral value into its existing private
`MirrorAddress`. No public method, public locator, or `MirrorEntityIdV1` value
is added. Existing I3B/I3C/I3D tests must prove byte-/semantic-identical
behavior after the extraction.

This helper owns only raw Mirror address semantics. It does not map
`MirrorZoneV1` to `PublicSemanticZoneV1`, classify spell/trap slots, create a
public locator, or publish a card code.

## 5. Stateless card correlation

`FlatPromptCardCorrelationV1` is an internal stateless helper. Its conceptual
entry point is:

```csharp
internal static bool TryCorrelate(
    MirrorSnapshotV1 capturedMirror,
    PublicStateSnapshotV1 acceptedSnapshot,
    uint sourceCardCode,
    ModernLocInfoV1 sourceLocation,
    out FlatPromptCardCorrelationResultV1? result,
    out FlatPromptErrorCodeV1 error)
```

The internal result owns only the accepted public card values needed by the
I4B draft:

```text
accepted PublicSemanticLocatorV1 locator
accepted CardCode when CARD_CODE_SAFE is true
```

It contains no raw `ModernLocInfoV1`, Mirror entity ID, Mirror object, protocol
offset, or response value.

### 5.1 Private source resolution

The helper first calls `MirrorAddressNormalizationV1.TryNormalize` on the wire
location. It then scans `capturedMirror.Cards` and maps each snapshot card's
private `MirrorParticipantRoleV1` to an absolute player using the accepted
perspective mapping. A card matches only when all normalized private address
facts match:

```text
absolute controller
MirrorZoneV1
sequence
overlay flag
overlay index when overlay
```

Exactly one match is required. Zero or multiple matches produce
`UnprovenPublicReference`. The helper may inspect the resolved card's known
proven CardCode and position as private facts, but never exposes its internal
entity identity.

The source wire CardCode is not part of public-reference proof. For indexed and
overlay cards, a zero or mismatching source wire CardCode leaves a proven
locator valid and makes only the separate CardCode predicate false. A zero or
unavailable source code never becomes a public code.

### 5.2 Public correlation

After private resolution, public correlation uses only permitted semantic facts
from the resolved `MirrorCardSnapshotV1` and the already classified accepted
`PublicCardStateV1` entries. I4B does not implement a second
`MirrorZoneV1 → PublicSemanticZoneV1` classifier.

The public snapshot's existing `Zone` and `Locator` are authoritative. The
helper may call the existing locator codec to validate a candidate locator
shape for comparison, but it never returns a newly created locator.

The indexed compatibility predicate is explicit and symmetric with the
resolved private Mirror zone:

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

This is a compatibility check, not a public-zone classifier. I4B never chooses
which S/T semantic zone applies. That value is already selected by I3D and is
read from `accepted PublicCardStateV1.Zone`.

The permitted correlation forms are:

```text
INDEXED_VISIBLE_CORRELATION
    exact absolute player
    INDEXED_ZONE_COMPATIBLE(resolved MirrorZoneV1, accepted PublicSemanticZoneV1)
    exact semantic PublicSemanticZoneV1 already present on the accepted card
    exact resolved indexed sequence
    accepted card locator parses to the exact player, accepted zone, and sequence
    exactly one accepted card required

HAND_OR_EXTRA_PUBLIC_ORDINAL_CORRELATION
    exact absolute player + resolved Hand/Extra zone
    resolved Mirror CardCode known and proven
    exactly one accepted snapshot card with that resolved proven CardCode
    raw hand/extra sequence is not used

OVERLAY_CORRELATION
    exact absolute player + resolved parent sequence + resolved overlay index
    accepted card must already carry the matching I3D overlay locator form
    exactly one accepted card required

MAIN_DECK_CORRELATION
    unavailable in I3D V1
    fail closed
```

The accepted public snapshot itself determines the public semantic zone,
including spell/trap, field, pendulum-relevant, hand/extra, and overlay
classification. If the permitted facts do not identify exactly one accepted
card, the entire EFFECTYN or CHAIN projection fails. I4B never chooses the
first match and never uses mirror collection order, allocation order,
physical continuity, or a raw wire sequence as a public identity.

### 5.3 Card-code safety

Public locator proof and CardCode inclusion remain separate:

```text
PUBLIC_CARD_REFERENCE_PROVEN=false
    → entire prompt fails

PUBLIC_CARD_REFERENCE_PROVEN=true
and sourceCardCode != 0
and acceptedCard.CardCode is present
and acceptedCard.CardCode == sourceCardCode
    → include CardCode variant

otherwise
    → retain locator
    → use the structurally no-CardCode variant
```

For Hand/Extra, the resolved proven CardCode is part of the permitted
correlation key; the source wire CardCode is checked only by the separate
`CARD_CODE_SAFE` predicate. For indexed and overlay cards, a failed separate
`CARD_CODE_SAFE` predicate only omits the CardCode member after locator proof
succeeds.

## 6. Public type hierarchy

The existing I4A abstract record bases remain the roots. All concrete variants
are sealed records. No conditional field is represented by a nullable member.

### 6.1 Contexts

```text
FlatPromptPublicContextV1
├─ FlatPromptYesNoPublicContextV1
├─ FlatPromptOptionPublicContextV1
├─ FlatPromptPositionPublicContextV1
├─ FlatPromptEffectYnPublicContextBaseV1 (abstract)
│  ├─ FlatPromptEffectYnPublicContextV1
│  │    effect_card_locator
│  │    effect_description_id
│  └─ FlatPromptEffectYnCardCodePublicContextV1
│       effect_card_locator
│       effect_description_id
│       effect_card_code
└─ FlatPromptChainPublicContextV1
     chain_spe_count
     chain_forced
     chain_hint_timing_for_player
     chain_hint_timing_for_other_player
```

The base context continues to expose exactly the common contract fields:

```text
contract_id
prompt_family
acting_player
```

It never exposes prompt ordinal, local candidate key, response data, raw
bytes, authority objects, or public action identity.

### 6.2 Candidates

```text
FlatPublicCandidateDescriptorV1
├─ existing I4A variants
├─ FlatEffectYnPublicCandidateDescriptorV1
├─ FlatChainNoChainPublicCandidateDescriptorV1
└─ FlatChainEntryPublicCandidateDescriptorBaseV1 (abstract)
   ├─ FlatChainPublicCandidateDescriptorV1
   └─ FlatChainCardCodePublicCandidateDescriptorV1
```

The abstract CHAIN entry base contains exactly:

```text
i4_local_candidate_key
choice_kind=CHAIN_ENTRY
source_section=CHAIN_CHOICES
source_ordinal
public_semantic_card_locator
description_or_effect_id
client_mode
```

The CardCode subtype adds only `card_code`. `NO_CHAIN` contains only the
common local key and `choice_kind=NO_CHAIN`. EFFECTYN candidates contain only
the common local key and NO/YES choice kind; card and description values stay
in shared context.

The new concrete variants are sealed. The abstract conditional bases are
public contract types but cannot be instantiated or used as a nullable
CardCode carrier.

## 7. EFFECTYN semantics

The exact modern request is 24 bytes:

```text
u8       message_id = 12
u8       player
u32_le   card_code
10 bytes ModernLocInfoV1
u64_le   description
```

The parser accepts no legacy or compatibility width. It validates exact length,
player, and private location normalization before constructing the public
context. The complete domain is always exactly:

```text
0: MSG_SELECT_EFFECTYN:NO  → response i32 0
1: MSG_SELECT_EFFECTYN:YES → response i32 1
```

The NO/YES order is explicit and deterministic. There is no pass, cancel,
third response, or N=1 shortcut.

`effect_card_locator` and `effect_description_id` are required only after
unique public correlation. `effect_card_code` uses the separate safe-code
predicate and is absent as a member when unsafe.

## 8. CHAIN semantics

The exact modern request is:

```text
u8       message_id = 16
u8       player
u8       spe_count
u8       forced
u32_le   hint_timing_for_player
u32_le   hint_timing_for_other_player
u32_le   chain_count = c
repeat c:
    u32_le card_code
    10 bytes ModernLocInfoV1
    u64_le description
    u8     client_mode
```

The fixed header is 16 bytes and each entry is 23 bytes. The required exact
length is `16 + 23*c`. Count arithmetic is checked and must complete before
entry allocation. `forced` must be 0 or 1; `client_mode` must be 0, 1, or 2.

`spe_count` is preserved as the exact u8 context value. `0x7f` is the frozen
trigger-selection marker and is never treated as 127 entries. No inference or
candidate is derived from the marker.

Each wire entry becomes exactly one public candidate in supplied source order.
The local key is:

```text
MSG_SELECT_CHAIN:CHAIN_ENTRY:<source_ordinal>
```

The ordinal is canonical invariant ASCII decimal with no sign or leading zero.
The binding is Entry `i` → signed `i32 i`. Duplicate descriptions, CardCodes,
locators, and client modes remain separate source occurrences.

`forced` alone controls NO_CHAIN:

```text
forced = false
    → append exactly one NO_CHAIN after all entries
    → NO_CHAIN binds to i32 -1

forced = true
    → append no NO_CHAIN
    → require c >= 1
```

Therefore `forced=false,c=0` is a one-candidate externally selectable domain,
while `forced=true,c=0` fails closed. A forced one-entry domain remains
externally selectable and is never automatically answered.

## 9. Atomic session transaction

The existing I4A `FlatPromptSessionV1` ordinal and binding lifecycle is
extended, not replaced. The session retains only:

```text
next_prompt_ordinal
current private binding
```

It never retains mirror, projection, snapshot, correlation result, or public
authority state.

The private I4B draft contains raw CardCode/ModernLocInfo only until
correlation completes. The public result contains only typed contract values.
The session commits nothing until all entries and all required correlation
proofs succeed. On any failure at parsing, authority validation, private
resolution, public correlation, candidate construction, or binding validation:

```text
IsSuccess=false
Context=null
Candidates=null
currentBinding=null
next_prompt_ordinal unchanged
no response sent
```

On success, checked ordinal increment, immutable binding construction, and
public result publication are one logical commit. The private binding validates
Family + concrete runtime candidate type + ChoiceKind + exact local key + exact
response for every candidate, including EFFECTYN and both CHAIN variants.

## 10. Local identity and replay boundary

The following identities remain separate:

```text
I4 local prompt ordinal
I4 local candidate key
I3D public projection identity
future I6 public_action_key
future model request identity
```

Required local keys are:

```text
MSG_SELECT_EFFECTYN:NO
MSG_SELECT_EFFECTYN:YES
MSG_SELECT_CHAIN:CHAIN_ENTRY:<source_ordinal>
MSG_SELECT_CHAIN:NO_CHAIN
```

Keys use invariant culture, canonical ASCII decimal formatting, ordinal string
comparison, and no pointer, locator, CardCode, description, hash, PID,
allocation, or object identity.

```text
I4_LOCAL_CANDIDATE_KEY_IS_OCGFORGE_PUBLIC_ACTION_KEY=NO
I4_LOCAL_CANDIDATE_KEY_MODEL_INPUT_AUTHORIZED=NO
```

Private response bindings are current-prompt control data. They are not public
semantic values, canonical bytes, public action identity, or replay identity.
The I3D `PublicProjectionId` remains the only accepted public-state identity;
I4B creates no gameplay identity of its own.

## 11. Failure taxonomy and privacy

The implementation uses explicit result errors, including:

```text
MalformedPrompt
UnsupportedPromptLayout
UnprovenPublicReference
UnprovenCandidateDomain
InvalidI4LocalCandidateKey
StalePromptBinding
InvalidResponseBinding
InvalidParticipant
InvalidLocation
InvalidBoolean
InvalidClientMode
AuthorityMismatch
ArithmeticFailure
```

The exact error does not weaken atomic failure. No failure publishes a partial
CHAIN list or leaves an old binding usable.

The public I4B surface excludes:

```text
raw ModernLocInfoV1
raw controller/location/sequence/position
MirrorEntityIdV1
mirror object identity
hidden card identity
raw hand/Extra sequence
response integer and response bytes
socket/session state
protocol offset
path, PID, timestamp, wall clock
public_action_key
model output, Teacher, RandomLegal, fallback
```

Only the accepted successful I3D snapshot can supply a public locator or
CardCode. The mirror is private resolution evidence. The correlation helper
does not become an authority or a public serialization layer.

## 12. Determinism and authority tests

I4B output must not depend on unordered iteration, dictionary iteration,
pointer/object identity, allocation order, culture, PID, time, scheduling,
filesystem order, TCP chunking, or receive-buffer identity. CHAIN entries use
the exact parsed source order; NO_CHAIN is appended only by the forced rule.

The authority barrier compares the captured mirror's reprojected canonical
bytes, SHA-256, and PublicProjectionId against the accepted projection. Any
mismatch fails closed. All correlation occurs against one captured mirror
snapshot and one accepted public snapshot from the same call.

Paired-world tests must prove that hidden/private differences do not alter the
public I4B result when the accepted public projection is equal. Ambiguous
public correlation fails; the helper never selects a first matching card.

## 13. Future implementation scope and test catalog

The future implementation is exactly eight files:

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

This is six production files and two test files. The existing 67 Gameplay
registrations remain unchanged in order and semantics. Exactly 18 new
top-level registrations are planned:

```text
01 EffectYnExactWireAndContext
02 EffectYnDomainOrderAndResponses
03 EffectYnMalformedWireFailures
04 EffectYnAuthorityValidationFailures
05 EffectYnIndexedCorrelation
06 EffectYnPileAndOverlayCorrelation
07 EffectYnCardCodeSafetyAndAmbiguity
08 EffectYnPrivacyAndStaleness

09 ChainOptionalWireContextAndNoChain
10 ChainForcedMarkerAndSingleEntry
11 ChainOptionalEmptyDomain
12 ChainEntryOrderDuplicatesAndValues
13 ChainNoChainAuthority
14 ChainMalformedWireAndEnumeration
15 ChainCorrelationAuthorityAndCardCodeSafety
16 ChainAtomicityStalenessAndOwnership

17 I4BPublicPrivateBoundary
18 I4AAndI3RegressionBoundary
```

The counts are fixed for implementation review:

```text
CURRENT_GAMEPLAY_TEST_COUNT=67
PLANNED_NEW_I4B_TEST_GROUPS=18
EXPECTED_GAMEPLAY_TEST_COUNT=85
```

`EffectYnIndexedCorrelation` and the corresponding CHAIN correlation group
must include a same-player/same-sequence cross-zone case. The test must place
an indexed MonsterZone card and an indexed SpellTrapZone-family card at the
same numeric sequence and prove that only the accepted card whose
`PublicSemanticZoneV1` satisfies `INDEXED_ZONE_COMPATIBLE` can match. It must
also cover accepted `SpellTrapZone`, `FieldZone`, and
`PendulumRelevantState` values. I4B may reject or accept a candidate based on
this compatibility predicate, but I3D alone chooses the concrete S/T public
zone; I4B never classifies it.

The groups cover every required positive, negative, correlation, authority,
privacy, staleness, ownership, deterministic, and I4A/I3 regression case.

## 14. Acceptance boundary

Future implementation acceptance requires:

```text
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
I3_SEMANTICS_CHANGED=NO
I3_PUBLIC_CANONICAL_BYTES_CHANGED=NO
I3_PUBLIC_PROJECTION_ID_CHANGED=NO
```

All six Release projects must be built twice — normal restore and
`--no-restore` — for 12 build invocations with zero warnings and errors.
Protocol, Client, and Gameplay harnesses must each run in two fresh processes
with identical stdout and exit codes. The final implementation diff must be
exactly eight files from `PLAN_HEAD` and ten files from the original I4B base,
including the two docs.

I4B remains design-/plan-only until independent review explicitly authorizes
implementation. No PR, I4C, I5, or I6 work follows automatically.
