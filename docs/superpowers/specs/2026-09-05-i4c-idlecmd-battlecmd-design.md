# OCGForge-Ignis I4C — IDLECMD + BATTLECMD Runtime Design

Status: PROPOSED / pending independent review

Date: 2026-09-05

Authorized design base:

    ea0d7d51d988e201c246a6660e27bdd20402221b

This document defines the I4C architecture and contract interpretation only.
It does not authorize implementation, test changes, fixture changes, frozen
contract changes, pull-request creation, or merge.

## 1. Scope and non-goals

I4C covers exactly these two frozen prompt families:

    MSG_SELECT_BATTLECMD = 10
    MSG_SELECT_IDLECMD   = 11

The already accepted layers remain unchanged:

    I3D
        PublicStateProjectionResultV1
        accepted public locator/CardCode/canonical-byte authority

    I4A
        MSG_SELECT_YESNO
        MSG_SELECT_OPTION
        MSG_SELECT_POSITION

    I4B
        MSG_SELECT_EFFECTYN
        MSG_SELECT_CHAIN

The following are explicitly outside I4C:

    MSG_SELECT_CARD, MSG_SELECT_TRIBUTE, MSG_SELECT_SUM, MSG_SELECT_PLACE
    I4D
    I5 continuation/combinatorial prompts
    I6 OCGForge public_action_key compatibility
    network response sending and CTOS_RESPONSE construction
    model input, model runners, trajectories, training, and fallback policy
    automatic selection, native AI, heuristics, and candidate capping
    changes to GameplayMessageDecoderV1, GameplayMirrorSessionV1,
    PublicStateProjectionV1, or PublicSemanticLocatorV1

The priority order is correctness, determinism, information safety, complete
legal decisions, replay/auditability, maintainability, performance, and ML
scale. An unproven source entry or response binding fails the complete prompt;
it never becomes a plausible partial candidate.

## 2. Accepted provenance and baseline

The accepted I4A/I4B base is ea0d7d51d988e201c246a6660e27bdd20402221b.
The frozen I4 vectors pin:

    EDOPro   30935e847165a9ef0e547fb51a43f36168fab7c7
    ocgcore  46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57

The checked-in contract and vectors are the local authority:

    docs/contracts/flat-prompt-projection-v1.md
    fixtures/gameplay/v1/i4-flat-prompt-vectors.v1.json
    fixtures/gameplay/v1/game-message-support.v1.json

The BATTLECMD mixed evidence vector intentionally omits `card_code` from its
second ATTACKABLE public descriptor. Its wire source code is different from
the accepted public card at the same locator, so `CARD_CODE_SAFE=false` while
the accepted locator remains proven. The vector must not publish two CardCodes
for one accepted public locator.

The current main baseline was executed before this design work:

    Protocol  = 20/20
    Client    = 17/17
    Gameplay  = 85/85

Those are understanding evidence for the current repository state. No I4C
behavior is claimed as implemented by this document.

## 3. Design alternatives

### Recommended: extend the existing deep flat-prompt module

Extend FlatPromptSessionV1 and its private FlatPromptProjectionV1 parser with
the two new family discriminants. Reuse the existing per-call I4B authority
overload, stateless FlatPromptCardCorrelationV1, immutable binding, prompt
ordinal, and selection-handle lifecycle.

This gives callers one small interface and keeps parsing, complete-domain
construction, authority validation, and stale-safe response resolution behind
one deep module. The existing I4A one-argument interface remains unchanged.

### Rejected: a second BATTLE/IDLE session

A separate Battle/Idle session would duplicate ordinal, binding, failure
atomicity, selection-handle, and public-value rules. It would create two
selection lifecycles for one prompt stream and make future I4 family
extensions harder to audit.

### Rejected: a new I4C public-state projection

I4C does not own public card identity. A parallel projection or locator
generator would compete with I3D and could publish a locator that the
accepted public snapshot did not prove. I4C therefore consumes, but never
replaces, I3D publication authority.

## 4. Ownership and authority

The ownership graph is:

    complete inner GAME_MSG
            │
            ├── I4C private wire draft
            │       │
            │       └── FlatPromptSessionV1
            │
            ├── PerspectiveStateMirrorV1
            │       └── captured MirrorSnapshotV1
            │             private source resolution only
            │
            └── accepted PublicStateProjectionResultV1
                    └── Snapshot
                          sole public locator/CardCode authority

I4C uses the existing I4B overload:

    FlatPromptProjectionResultV1 TryAcceptPrompt(
        ReadOnlySpan<byte> completeInnerGameMessage,
        PerspectiveStateMirrorV1? mirror,
        PublicStateProjectionResultV1? acceptedProjection);

The one-argument I4A interface is unchanged:

    FlatPromptProjectionResultV1 TryAcceptPrompt(
        ReadOnlySpan<byte> completeInnerGameMessage);

The I4C transaction is exactly:

    0  strict private BATTLECMD/IDLECMD wire parse
    1  acceptedProjection is present
    2  acceptedProjection.IsSuccess
    3  acceptedProjection.Snapshot is present
    4  mirror is present
    5  capture mirror.Snapshot exactly once
    6  reproject captured snapshot using accepted Snapshot.DuelFlags
    7  require reprojection success
    8  compare CanonicalBytes byte-for-byte
    9  compare Sha256 with StringComparison.Ordinal
    10 compare PublicProjectionId with StringComparison.Ordinal
    11 privately resolve every card source through the captured snapshot
    12 correlate every required card to exactly one accepted Snapshot.Cards entry
    13 copy only accepted locator/CardCode values into typed public variants
    14 append transition candidates only for supplied true flags
    15 validate every concrete candidate/runtime type/key/response binding
    16 compute the next ordinal without mutation
    17 atomically replace binding, advance ordinal, and publish success

After step 5 the live mirror reference is never read again. The recomputed
projection is consistency evidence only. The accepted projection supplies all
published locators and CardCodes. The session retains no mirror, projection,
snapshot, wire draft, correlation result, or authority state after the call.

Any failure clears the current binding, publishes no context or candidates,
does not advance the ordinal, and sends no network response.

## 5. Common public context

Both I4C families publish only the existing common context:

    contract_id   = ocgforge-ignis.flat-prompt-projection.v1
    prompt_family = MSG_SELECT_BATTLECMD or MSG_SELECT_IDLECMD
    acting_player = prompt player byte, exactly 0 or 1

Counts, transition flags, raw locations, raw CardCodes, protocol offsets,
response integers, and prompt ordinals are not context members. The complete
source domain is represented by the ordered candidate list.

## 6. Exact BATTLECMD wire grammar

The modern complete inner message is:

    u8       message_id = 10
    u8       player
    u32_le   activatable_count = a
    repeat a:
        u32_le card_code
        u8     controller
        u8     location
        u32_le sequence
        u64_le description
        u8     client_mode
    u32_le   attackable_count = b
    repeat b:
        u32_le card_code
        u8     controller
        u8     location
        u8     sequence
        u8     direct_attackable
    u8       to_main_phase_2
    u8       to_end_phase

The fixed entry widths are:

    ActivatableBattleCommandV1 = 19 bytes
    AttackableBattleCommandV1  = 8 bytes

The exact complete length is:

    payload_bytes = 11 + (19 * a) + (8 * b)
    total_bytes   = 12 + (19 * a) + (8 * b)

All arithmetic is performed in a wide checked intermediate before allocating
or iterating entry storage. A count that cannot fit the supplied complete
span, a truncation, or trailing bytes fails closed.

client_mode is exactly one of:

    0 = EFFECT_CLIENT_MODE_NORMAL
    1 = EFFECT_CLIENT_MODE_RESOLVE
    2 = EFFECT_CLIENT_MODE_RESET

to_main_phase_2, to_end_phase, and direct_attackable are exact boolean bytes.
Values other than 0 or 1 are invalid.

The activatable entry has a 32-bit sequence and a 64-bit description. The
attackable entry has an 8-bit sequence, no description, no client mode, and
one direct-attackable boolean. The attackable sequence is not widened to a
fictitious ten-byte ModernLocInfoV1; it remains a private BATTLE source
field. For private correlation, a non-overlay entry may be represented as a
ModernLocInfoV1 with position zero. An overlay bit is not enough to prove an
overlay identity because these BATTLE entries carry no overlay index, so
such a card-bearing entry fails as UnprovenPublicReference.

The player and each controller must be 0 or 1. The location base and field
sequence are validated by the shared existing Mirror address semantics. No
raw wire location, code, sequence, or response body is public.

## 7. BATTLECMD complete domain

The domain cardinality is:

    N = a + b
        + (to_main_phase_2 == 1 ? 1 : 0)
        + (to_end_phase == 1 ? 1 : 0)

The domain must be non-empty. When a=0, b=0, and both flags are zero, there
is no legal response and the complete prompt fails closed with the existing
zero-domain error. A one-candidate domain remains externally selectable; no
automatic response is produced.

The exact public order is:

    all activatable entries in wire order
    all attackable entries in wire order
    TO_M2 if to_main_phase_2 == 1
    TO_EP if to_end_phase == 1

The core has already established any semantic activatable chain order before
writing the wire vector. I4C preserves that supplied order and never exposes
or reconstructs the core effect identity. Duplicate-looking entries remain
separate source occurrences.

## 8. BATTLECMD public candidate variants

All concrete records are sealed. Conditional CardCode presence is represented
by separate runtime types, not a nullable CardCode carrier.

### Activatable entries

    FlatBattleActivatablePublicCandidateBaseV1
    ├─ FlatBattleActivatablePublicCandidateV1
    └─ FlatBattleActivatableCardCodePublicCandidateV1

The base contains:

    i4_local_candidate_key
    choice_kind = ACTIVATE
    source_section = ACTIVATABLE
    source_ordinal
    public_semantic_card_locator
    description_or_effect_id
    client_mode

The CardCode subtype adds only card_code.

### Attackable entries

    FlatBattleAttackPublicCandidateBaseV1
    ├─ FlatBattleAttackPublicCandidateV1
    └─ FlatBattleAttackCardCodePublicCandidateV1

The base contains:

    i4_local_candidate_key
    choice_kind = ATTACK
    source_section = ATTACKABLE
    source_ordinal
    public_semantic_card_locator
    direct_attackable

The CardCode subtype adds only card_code. It has no description or
client-mode member.

### Transitions

    FlatBattleToMainPhase2PublicCandidateV1
        choice_kind = TO_M2
        key = MSG_SELECT_BATTLECMD:TO_M2
        transition_token = MAIN_PHASE_2

    FlatBattleToEndPhasePublicCandidateV1
        choice_kind = TO_EP
        key = MSG_SELECT_BATTLECMD:TO_EP
        transition_token = END_PHASE

Transition candidates have no source section, source ordinal, card locator,
CardCode, description, client mode, or direct-attackable member.

## 9. Exact IDLECMD wire grammar

The modern complete inner message is:

    u8       message_id = 11
    u8       player
    u32_le   summon_count = s
    repeat s:
        u32_le card_code
        u8     controller
        u8     location
        u32_le sequence
    u32_le   special_summon_count = ss
    repeat ss: same 10-byte card entry
    u32_le   reposition_count = r
    repeat r:
        u32_le card_code
        u8     controller
        u8     location
        u8     sequence
    u32_le   monster_set_count = m
    repeat m: same 10-byte card entry
    u32_le   spell_trap_set_count = st
    repeat st: same 10-byte card entry
    u32_le   activatable_count = a
    repeat a:
        u32_le card_code
        u8     controller
        u8     location
        u32_le sequence
        u64_le description
        u8     client_mode
    u8       to_battle_phase
    u8       to_end_phase
    u8       shuffle_hand

The entry widths are:

    SUMMON          10 bytes
    SPECIAL_SUMMON  10 bytes
    REPOSITION       7 bytes
    MSET            10 bytes
    SSET            10 bytes
    ACTIVATE        19 bytes

The exact complete length is:

    payload_bytes = 28
        + (10 * s) + (10 * ss) + (7 * r)
        + (10 * m) + (10 * st) + (19 * a)
    total_bytes = 29
        + (10 * s) + (10 * ss) + (7 * r)
        + (10 * m) + (10 * st) + (19 * a)

All six counts are independent modern u32_le values. The heterogeneous entry
widths are part of the grammar; the parser does not flatten the input into
one guessed card-entry shape. All three final flags and every activation
client_mode use the exact BATTLECMD boolean/mode validation.

The first, second, fourth, fifth, and sixth card sections have 32-bit
sequences. REPOSITION has an 8-bit sequence and no description or mode. As
with BATTLECMD, an entry with an overlay bit has no wire overlay index and
cannot be privately proven as an overlay candidate; it fails closed rather
than assuming overlay index zero.

## 10. IDLECMD complete domain and order

The domain cardinality is:

    N = s + ss + r + m + st + a
        + (to_battle_phase == 1 ? 1 : 0)
        + (to_end_phase == 1 ? 1 : 0)
        + (shuffle_hand == 1 ? 1 : 0)

All supplied card occurrences are retained, even when values are identical.
The domain must be non-empty. Zero counts with all three flags false is a
zero-option prompt and fails closed; it is never converted into a generic
pass or phase transition. A one-candidate domain waits for explicit
selection.

The exact concatenation is:

    SUMMON entries
    SPECIAL_SUMMON entries
    REPOSITION entries
    MSET entries
    SSET entries
    ACTIVATE entries
    TO_BP when to_battle_phase == 1
    TO_EP when to_end_phase == 1
    SHUFFLE_HAND when shuffle_hand == 1

Each section preserves its own wire order and source ordinal. A transition is
present if and only if its corresponding flag is one.

## 11. IDLECMD public candidate variants

### Simple card actions

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

Each pair is a closed runtime discriminant with a fixed choice kind and
source section:

    SUMMON         ↔ (SUMMON, SUMMON)
    SPECIAL_SUMMON ↔ (SPECIAL_SUMMON, SPECIAL_SUMMON)
    REPOSITION     ↔ (REPOSITION, REPOSITION)
    MSET           ↔ (MSET, MSET)
    SSET           ↔ (SSET, SSET)

Each base contains:

    i4_local_candidate_key
    choice_kind
    source_section
    source_ordinal
    public_semantic_card_locator

The CardCode subtype adds only card_code. No description, client mode, or
transition token is present for these five kinds. A private constructor helper
may share field-copying code, but the public records remain section-specific.
Their parser arrays and source semantics remain separate.

### Activatable actions

    FlatIdleActivatablePublicCandidateBaseV1
        ├─ FlatIdleActivatablePublicCandidateV1
        └─ FlatIdleActivatableCardCodePublicCandidateV1

The base contains:

    i4_local_candidate_key
    choice_kind = ACTIVATE
    source_section = ACTIVATE
    source_ordinal
    public_semantic_card_locator
    description_or_effect_id
    client_mode

The CardCode subtype adds only card_code.

### Transitions

    FlatIdleToBattlePhasePublicCandidateV1
        choice_kind = TO_BP
        key = MSG_SELECT_IDLECMD:TO_BP
        transition_token = BATTLE_PHASE

    FlatIdleToEndPhasePublicCandidateV1
        choice_kind = TO_EP
        key = MSG_SELECT_IDLECMD:TO_EP
        transition_token = END_PHASE

    FlatIdleShuffleHandPublicCandidateV1
        choice_kind = SHUFFLE_HAND
        key = MSG_SELECT_IDLECMD:SHUFFLE_HAND
        transition_token = SHUFFLE_HAND

These transition variants contain no card or source-entry fields.

## 12. Local keys and private response bindings

All source ordinals are section-local, invariant-culture, canonical ASCII
decimal with no sign and no leading zero. The required keys are:

    MSG_SELECT_BATTLECMD:ACTIVATE:<i>
    MSG_SELECT_BATTLECMD:ATTACK:<i>
    MSG_SELECT_BATTLECMD:TO_M2
    MSG_SELECT_BATTLECMD:TO_EP

    MSG_SELECT_IDLECMD:SUMMON:<i>
    MSG_SELECT_IDLECMD:SPECIAL_SUMMON:<i>
    MSG_SELECT_IDLECMD:REPOSITION:<i>
    MSG_SELECT_IDLECMD:MSET:<i>
    MSG_SELECT_IDLECMD:SSET:<i>
    MSG_SELECT_IDLECMD:ACTIVATE:<i>
    MSG_SELECT_IDLECMD:TO_BP
    MSG_SELECT_IDLECMD:TO_EP
    MSG_SELECT_IDLECMD:SHUFFLE_HAND

The response integer is a private signed i32 bit pattern:

    BATTLE ACTIVATABLE i: (i << 16) | 0
    BATTLE ATTACKABLE  i: (i << 16) | 1
    BATTLE TO_M2:                 2
    BATTLE TO_EP:                 3

    IDLE SUMMON         i: (i << 16) | 0
    IDLE SPECIAL_SUMMON i: (i << 16) | 1
    IDLE REPOSITION     i: (i << 16) | 2
    IDLE MSET           i: (i << 16) | 3
    IDLE SSET           i: (i << 16) | 4
    IDLE ACTIVATE       i: (i << 16) | 5
    IDLE TO_BP:                    6
    IDLE TO_EP:                    7
    IDLE SHUFFLE_HAND:             8

The implementation constructs the bit pattern in uint and converts it to
signed i32 without changing the four bytes. The high selector is a 16-bit
section-local ordinal, so every repeated section may contain at most 65536
entries (indices 0 through 65535). A larger count is rejected fail-closed as
ArithmeticFailure before public candidate construction, because no exact
private response exists for index 65536 or above. This range check is
independent for each source section.

Binding validation repeats, rather than trusts, the tuple:

    family
    concrete runtime candidate type
    choice kind
    source section
    source ordinal
    exact local key
    exact private i32 response

No response integer or response bytes are public. The local key is not an
OCGForge public_action_key and is not model input.

## 13. Card correlation and CardCode safety

Every card-bearing BATTLE/IDLE entry reuses the accepted I4B correlation
module and authority transaction:

    captured MirrorSnapshotV1
        = private location/entity resolution

    accepted PublicStateSnapshotV1
        = exact published locator/CardCode source

    recomputed projection
        = consistency proof only

The shared MirrorAddressNormalizationV1 remains the only raw Mirror address
normalizer. I4C adds no public zone classifier, locator generator, or
authority cache.

For indexed visible sources, correlation requires the existing I4B predicate:

    exact absolute player
    exact permitted MirrorZoneV1/PublicSemanticZoneV1 compatibility
    exact resolved sequence
    exact accepted locator produced by local comparison with
        PublicSemanticLocatorV1.TryCreateIndexed(...)

The accepted snapshot PublicCardStateV1.Locator is copied. The comparison
locator is local only and is never published, stored, returned, or cached.

For Hand/Extra sources, private resolution may use the raw source sequence to
find one captured Mirror card, but public correlation uses only:

    absolute player
    public Hand or ExtraDeck zone
    known/proven resolved Mirror CardCode
    exactly one accepted public card with that CardCode

Two accepted public ordinal cards with the same code are ambiguous and fail
closed. Raw pile sequence, collection order, allocation order, historical
continuity, and Mirror entity identity never disambiguate them.

For overlay sources, an overlay index must be present in the source
representation. BATTLECMD and IDLECMD entries do not carry one, so an
overlay-bit entry is not made public by assuming an index. Main Deck has no
per-card public locator and always fails card correlation.

PUBLIC_CARD_REFERENCE_PROVEN and CARD_CODE_SAFE remain separate:

    locator proof succeeds
    + wire source CardCode != 0
    + accepted CardCode is present
    + accepted CardCode == wire source CardCode
        → use the accepted snapshot CardCode-bearing variant

    otherwise
        → retain the accepted locator
        → use the structurally no-CardCode variant

A zero or mismatching wire CardCode never destroys an otherwise proven
locator. If the family-specific candidate cannot be represented without a
proven locator, the complete prompt fails closed.

## 14. Failure taxonomy and atomicity

The existing I4 error surface is extended only with the values required by
the frozen grammar:

    InvalidLocation
    InvalidBoolean
    InvalidClientMode
    AuthorityMismatch

Existing values keep their numeric meanings. I4C uses:

    MalformedPrompt
        truncated/trailing or count/body length mismatch

    UnsupportedPromptLayout
        unsupported message family or a known frozen legacy-width layout

    InvalidParticipant
        prompt player or source controller outside 0/1

    InvalidLocation
        invalid base location or field sequence

    InvalidBoolean
        direct_attackable or transition boolean not 0/1

    InvalidClientMode
        activation mode outside 0/1/2

    ArithmeticFailure
        checked length, candidate-count, response-index, or ordinal overflow

    ZeroOptionDomain
        syntactically valid BATTLE/IDLE prompt with no source candidate

    UnprovenPublicReference
        no/ambiguous private resolution or no unique accepted public card

    AuthorityMismatch
        accepted projection/reprojection bytes, SHA, or ID disagree

    InvalidResponseBinding
        candidate/runtime type/key/response tuple is inconsistent

Every failure, including a later entry failure after earlier entries were
correlated, produces:

    IsSuccess=false
    Context=null
    Candidates=null
    currentBinding=null
    prompt ordinal unchanged
    no network response

An accepted one-candidate source domain is not a failure and is never
auto-answered. A transition candidate exists only when its source flag is one.

## 15. Determinism and privacy

I4C output depends only on the complete wire value, the one captured Mirror
snapshot, and the accepted public snapshot. It never depends on:

    unordered-container iteration
    reflection or enum declaration order
    object/pointer addresses
    allocation order
    wall clock, PID, process/thread identity, filesystem paths
    locale or culture-sensitive formatting
    TCP chunks or receive-buffer identity
    hidden opponent identity or deck order
    raw response bytes or protocol offsets
    MirrorEntityIdV1 or relation ordinals
    model/native-AI output

Source order is copied explicitly into arrays and candidate lists. Section
ordinals are deterministic ASCII strings. Duplicate source occurrences remain
separate even when every semantic value looks identical.

Public values contain only contract fields. They expose no raw location,
response, authority, socket, process, or model data. Prompt ordinal and
private bindings remain control-plane values.

## 16. Fixed I4C test catalog

The current Gameplay harness has 85 registrations. I4C adds exactly 18
top-level groups, for an expected total of 103:

    01 BATTLE exact wire and context
    02 BATTLE mixed sections and complete order
    03 BATTLE response bindings and section ordinals
    04 BATTLE transition flags and zero-domain
    05 BATTLE indexed correlation and accepted locator
    06 BATTLE CardCode safety and ambiguity
    07 BATTLE malformed wire and enum validation
    08 BATTLE authority, atomicity, staleness, ownership, and privacy

    09 IDLE exact wire and context
    10 IDLE all sections and canonical order
    11 IDLE per-section response bindings
    12 IDLE transition flags and zero-domain
    13 IDLE indexed and pile correlation
    14 IDLE CardCode safety and duplicate ambiguity
    15 IDLE malformed wire and enum validation
    16 IDLE authority, atomicity, staleness, ownership, and privacy

    17 I4C public/private boundary
    18 I3/I4A/I4B regression boundary

The BATTLE correlation group must exercise the frozen mixed vector, plus a
real prompt containing same-player/same-sequence MonsterZone and
SpellTrapZone-family cards. The accepted snapshot zone and locator must
select the correct source without a raw sequence-only shortcut.

The IDLE correlation group must exercise each of Hand, Extra Deck, indexed
field, Main Deck failure, and duplicate known-CardCode ambiguity. It must
prove that raw Hand/Extra sequence does not choose a public ordinal.

The two CardCode groups cover safe and unsafe source codes without removing a
proven locator. The two binding groups cover swapped keys, wrong section,
wrong concrete runtime type, wrong choice kind, wrong response, duplicate
keys, and exact 16-bit response-index boundaries.

Malformed tests cover truncation, trailing bytes, all count/body formulas,
known legacy-width rejection, checked size overflow, invalid player/controller
/location, invalid booleans, invalid client modes, invalid direct-attackable,
forced zero domains where relevant, and zero complete domains.

All groups also preserve the existing 85-group I3/I4A/I4B regression suite.
No new test registration may be merged into another group without a reviewed
change to this catalog.

## 17. Future implementation file map

The implementation should extend the existing I4 module with exactly five
files from the committed I4C plan head:

    MODIFY
    src/OCGForge.Ignis.Gameplay/FlatPromptTypesV1.cs
    src/OCGForge.Ignis.Gameplay/FlatPromptProjectionV1.cs
    src/OCGForge.Ignis.Gameplay/FlatPromptSessionV1.cs
    tests/OCGForge.Ignis.Gameplay.Tests/Program.cs

    CREATE
    tests/OCGForge.Ignis.Gameplay.Tests/Tests/I4CIdleBattlePromptTests.cs

Responsibilities are:

    FlatPromptTypesV1.cs
        I4C enums, closed public variants, private wire drafts,
        canonical keys, exact runtime binding validation

    FlatPromptProjectionV1.cs
        strict BATTLE/IDLE wire parsing and complete domain construction

    FlatPromptSessionV1.cs
        dispatch through the existing per-call authority transaction,
        ordinal/binding atomicity and I4A/I4B compatibility

    Program.cs + I4CIdleBattlePromptTests.cs
        18 registrations and all I4C acceptance evidence

src/OCGForge.Ignis.Gameplay/FlatPromptCardCorrelationV1.cs,
src/OCGForge.Ignis.Gameplay/MirrorAddressNormalizationV1.cs,
src/OCGForge.Ignis.Gameplay/PerspectiveStateMirrorV1.cs,
src/OCGForge.Ignis.Gameplay/PublicStateProjectionV1.cs, and
src/OCGForge.Ignis.Gameplay/PublicSemanticLocatorV1.cs remain untouched by the
intended I4C implementation.
A new I3 exception would require a separately reviewed authority gap; this
design currently identifies none.

## 18. Acceptance boundary

Future implementation acceptance must prove:

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
    I3_SEMANTICS_CHANGED=NO
    I4A_SEMANTICS_CHANGED=NO
    I4B_SEMANTICS_CHANGED=NO

The future worker must also run Protocol, Client, and Gameplay fresh-process
determinism pairs, twelve Release build invocations with zero warnings/errors,
and git diff --check. The implementation must stop after its exact feature
commit and push for independent review. This document does not authorize
I4C implementation, I5, I6, a PR, or a merge.
