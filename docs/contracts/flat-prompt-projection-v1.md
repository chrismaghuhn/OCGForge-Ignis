# OCGForge-Ignis I4A0 — Flat Prompt Projection V1

Status: `I4A0_CONTRACT_FREEZE=YES`; contract only
Contract ID: `ocgforge-ignis.flat-prompt-projection.v1`
Implementation status: `I4_IMPLEMENTED=NO`
Date: 2026-09-04

```text
I4A0=AUTHORIZED
I4_IMPLEMENTATION=NO
I5=NOT_AUTHORIZED
MODEL_INPUT=NOT_AUTHORIZED
LIVE_SERVER_USE=FORBIDDEN
```

This document freezes the modern flat-prompt wire, candidate-domain, source
ordering, privacy, current-prompt binding, and response contracts for exactly
seven message families. It does not implement a decoder, candidate DTO,
response sender, model input, or live server path.

## 1. Authority and exact target

The authoritative sources are the following exact commits:

| Repository | Commit | Role |
| --- | --- | --- |
| https://github.com/edo9300/edopro | `30935e847165a9ef0e547fb51a43f36168fab7c7` | pinned client prompt readers, modern/legacy width selector, response construction, and CTOS routing |
| https://github.com/edo9300/ygopro-core | `46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57` | pinned runtime prompt producers and response validation |

The EDOPro commit's `ocgcore` gitlink is exactly
`46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57`. The V1 target is the modern
EDOPro 41.0.2 / Bagooska, ocgcore API 11.0 path. Legacy/compatibility widths
are not accepted by this contract.

The sources are clean-room semantic authorities only. No upstream parser,
control flow, source implementation, or serialized upstream packet is copied
into Ignis. The machine inventory and the independently constructed vectors
under `fixtures/gameplay/v1/i4-flat-prompt-vectors.v1.json` are the checked-in
research evidence.

The seven and only seven I4 families are:

```text
MSG_SELECT_BATTLECMD   = 10
MSG_SELECT_IDLECMD     = 11
MSG_SELECT_EFFECTYN     = 12
MSG_SELECT_YESNO        = 13
MSG_SELECT_OPTION       = 14
MSG_SELECT_CHAIN        = 16
MSG_SELECT_POSITION     = 19
```

`MSG_SELECT_CARD`, `MSG_SELECT_TRIBUTE`, `MSG_SELECT_SUM`,
`MSG_SELECT_PLACE`, `MSG_SELECT_DISFIELD`, `MSG_SELECT_COUNTER`,
`MSG_SORT_CARD`, `MSG_SORT_CHAIN`, `MSG_ANNOUNCE_RACE`,
`MSG_ANNOUNCE_ATTRIB`, `MSG_ANNOUNCE_NUMBER`, `MSG_SELECT_UNSELECT_CARD`,
and `MSG_ANNOUNCE_CARD` are outside this freeze. They remain unimplemented,
unfrozen, or fail-closed according to the existing inventory.

## 2. Common wire rules

Each value below is the complete inner modern `GAME_MSG` value, beginning with
the one-byte message ID. It is not a TCP frame and has no adapter length
prefix. The existing I1 outer protocol remains authoritative for framing.

The only accepted numeric forms in these seven layouts are:

```text
u8       one unsigned byte
u32_le   four-byte unsigned little-endian integer
u64_le   eight-byte unsigned little-endian integer
i32_le   four-byte signed little-endian response integer
```

The pinned upstream writes native C++ integer objects through
`duel_message::write`/`BufferIO` and reads them through the matching client
functions. The accepted Ignis V1 target fixes those multi-byte wire values to
little-endian forms above. A future implementation must reject a legacy
`u8`/`u32_le` compatibility alternative rather than selecting a width from
available bytes.

The modern nested location value is exactly:

```text
ModernLocInfoV1 =
    u8       controller
    u8       location
    u32_le   sequence
    u32_le   position
```

It is exactly 10 bytes. For an overlay card, the pinned core's
`card::get_info_location` supplies the parent location with the overlay bit,
the parent sequence, and the overlay sequence in `position`. A location is a
protocol-local address until the future I4 implementation proves a matching
perspective-safe `PublicSemanticLocatorV1`.

Every `player`/`controller` field that identifies a duel player must be 0 or 1
in V1. Other values fail closed as an invalid participant. The accepted
location bits are the pinned core values `0x01` deck, `0x02` hand, `0x04`
monster, `0x08` spell/trap, `0x10` graveyard, `0x20` banished, `0x40` extra,
and `0x80` overlay. An unknown or structurally impossible card reference may
be decoded only as private wire data; it may not cross the public candidate
boundary.

The complete inner value must be consumed exactly. A future decoder must:

1. check every fixed-width field before reading it;
2. compute every counted length with checked arithmetic before allocation or iteration;
3. require the computed length to equal the supplied complete-message length;
4. reject truncation, count/body mismatch, overflow, and every trailing byte; and
5. publish no partial candidate domain after any failure.

No following `GAME_MSG` value may be consumed to repair a short prompt.

## 3. Common candidate and binding boundary

The public candidate is a semantic value, not a wire DTO. It may contain only
the fields needed for a perspective-legal decision, for example:

```text
prompt_family
public_action_key
choice_kind
source_section and source_ordinal where applicable
proven public semantic card locator
card_code only when independently perspective-safe
proven description/effect identifier
normalized mode, flag, position, or transition token
```

Raw `ModernLocInfoV1`, raw controller/location/sequence/position tuples, raw
GAME_MSG bytes, protocol offsets, and raw response bytes are never public
candidate fields. A card reference must use the accepted
`PublicSemanticLocatorV1`, or the candidate projection fails closed when a
safe reference is required. A card code is a property, not an entity
identity; it may be retained only when the current perspective proves it.

The private response binding is a separate value owned by the current prompt
instance. It maps one public action key to exactly one original `CTOS_RESPONSE`
body. It may contain the response integer and its exact four bytes, but it is
not included in `PublicActionCandidate`, a public action key, semantic public
gameplay identity, or future model input.

The public action-key grammar is deterministic ASCII with no leading-zero
decimal ordinals:

```text
MSG_SELECT_BATTLECMD:ACTIVATE:<source_ordinal>
MSG_SELECT_BATTLECMD:ATTACK:<source_ordinal>
MSG_SELECT_BATTLECMD:TO_M2
MSG_SELECT_BATTLECMD:TO_EP

MSG_SELECT_IDLECMD:SUMMON:<source_ordinal>
MSG_SELECT_IDLECMD:SPECIAL_SUMMON:<source_ordinal>
MSG_SELECT_IDLECMD:REPOSITION:<source_ordinal>
MSG_SELECT_IDLECMD:MSET:<source_ordinal>
MSG_SELECT_IDLECMD:SSET:<source_ordinal>
MSG_SELECT_IDLECMD:ACTIVATE:<source_ordinal>
MSG_SELECT_IDLECMD:TO_BP
MSG_SELECT_IDLECMD:TO_EP
MSG_SELECT_IDLECMD:SHUFFLE_HAND

MSG_SELECT_EFFECTYN:NO
MSG_SELECT_EFFECTYN:YES
MSG_SELECT_YESNO:NO
MSG_SELECT_YESNO:YES
MSG_SELECT_OPTION:OPTION:<source_ordinal>
MSG_SELECT_CHAIN:CHAIN_ENTRY:<source_ordinal>
MSG_SELECT_CHAIN:NO_CHAIN

MSG_SELECT_POSITION:FACEUP_ATTACK
MSG_SELECT_POSITION:FACEDOWN_ATTACK
MSG_SELECT_POSITION:FACEUP_DEFENSE
MSG_SELECT_POSITION:FACEDOWN_DEFENSE
```

The family and source ordinal are part of the semantic identity. Equal public
descriptions, equal card codes, and equal visible references do not justify
deduplication. A source entry at ordinal 0 and a source entry at ordinal 1
remain two candidates.

For stale-selection protection, a future implementation must create a
value-owned current-prompt binding containing:

```text
prompt_instance_ordinal : u64
message_id/family       : exact one of the seven families
ordered candidate keys   : complete current domain
private key-to-response binding for this prompt only
```

The first accepted I4 prompt in semantic GAME_MSG order has ordinal 0. The
ordinal increments once, with checked arithmetic, for each subsequently
accepted I4 prompt. TCP reads, chunk boundaries, threads, allocation order,
and wall time never advance it. A selected key is valid only when its prompt
ordinal, family, and complete current domain match the current binding. Thus a
selection from prompt A is rejected even when prompt B has identical-looking
candidate fields. A failed or unsupported prompt creates no usable binding.

## 4. Response envelope and no fallback

All seven flat families use the same original scalar response path:

```text
CTOS_RESPONSE frame:
    u16_le   length = 5                 # packet type plus four body bytes
    u8       packet_type = 0x01         # CTOS_RESPONSE
    i32_le   response_body
```

EDOPro's `DuelClient::SetResponseI` stores an `int32_t` body, and
`SendBufferToServer(CTOS_RESPONSE, ...)` sends the body unchanged. The core
copies that body into its response buffer and validates it through
`returns.at<int32_t>(0)`. A future implementation must produce exactly four
body bytes and must not submit an alternative width or an unsigned sentinel.

These rules are absolute:

```text
N=1_AUTO_ANSWER=FORBIDDEN
TEACHER_FALLBACK=NO
RANDOM_LEGAL_FALLBACK=NO
FIRST_CANDIDATE_FALLBACK=NO
PASS_FALLBACK=NO
RETRY_WITH_OTHER_POLICY=NO
```

When a source prompt contains one legal candidate, the future adapter still
publishes one candidate and waits for the agent/model selection. The pinned
core may internally resolve a position or simple-AI path without sending a
prompt; that upstream behavior is not an adapter permission to auto-answer an
I4 prompt that was received. Unsupported, stale, unbound, malformed, or
ambiguous values fail closed and never submit a replacement response.

## 5. MSG_SELECT_BATTLECMD (10)

### Exact modern request grammar

```text
u8       message_id = 10
u8       player
u32_le   activatable_count = a
repeat a times:
    u32_le card_code
    u8     controller
    u8     location
    u32_le sequence
    u64_le description
    u8     client_mode
u32_le   attackable_count = b
repeat b times:
    u32_le card_code
    u8     controller
    u8     location
    u8     sequence
    u8     direct_attackable
u8       to_main_phase_2
u8       to_end_phase
```

The activatable entry is 19 bytes. The attackable entry is 8 bytes. The exact
complete-message length is:

```text
payload_bytes = 11 + 19*a + 8*b
total_bytes   = 12 + 19*a + 8*b
```

The syntactic minimum is 12 bytes. Both flags and `direct_attackable` are
boolean bytes and must be exactly 0 or 1. `client_mode` is one of the pinned
EDOPro values `0=EFFECT_CLIENT_MODE_NORMAL`,
`1=EFFECT_CLIENT_MODE_RESOLVE`, or `2=EFFECT_CLIENT_MODE_RESET`; another
value is unproven and fails closed. The source count fields are modern
`u32_le`; a one-byte compatibility count or narrow sequence is not V1.

### Complete domain and order

The legal response domain has exactly:

```text
N = a + b + (to_main_phase_2 == 1 ? 1 : 0)
                    + (to_end_phase == 1 ? 1 : 0)
```

The domain must be non-empty. A syntactically valid message with no
activatable entries, no attackable entries, and both transition flags zero has
no legal response and fails closed as a prohibited zero-option state.

The deterministic order is the wire/source order:

1. every activatable entry in received order;
2. every attackable entry in received order;
3. `TO_M2` if its flag is 1;
4. `TO_EP` if its flag is 1.

The pinned core sorts `core.select_chains` with its
`chain::chain_operation_sort` before writing the activatable section. Ignis
preserves the resulting wire order and never exposes or recomputes the core's
internal effect ID. The attackable section is preserved as emitted. Duplicate
entries remain distinct by section ordinal.

The low response selector is the section kind and the high 16 bits are the
zero-based ordinal within that section:

| Candidate | Response integer | Body bytes |
| --- | ---: | --- |
| activatable ordinal `i` | `(i << 16) \| 0` | `i32_le` |
| attackable ordinal `i` | `(i << 16) \| 1` | `i32_le` |
| `TO_M2` | `2` | `02 00 00 00` |
| `TO_EP` | `3` | `03 00 00 00` |

The core validates kind 0..3, section bounds, and transition flags. The
`card_code` and coordinates are not public identity; an entry requiring a
card reference must be normalized through the current perspective mirror and
the accepted public locator contract.

## 6. MSG_SELECT_IDLECMD (11)

### Exact modern request grammar

```text
u8       message_id = 11
u8       player
u32_le   summon_count = s
repeat s times:
    u32_le card_code
    u8     controller
    u8     location
    u32_le sequence
u32_le   special_summon_count = ss
repeat ss times: same 10-byte card entry
u32_le   reposition_count = r
repeat r times:
    u32_le card_code
    u8     controller
    u8     location
    u8     sequence
u32_le   monster_set_count = m
repeat m times: same 10-byte card entry
u32_le   spell_trap_set_count = st
repeat st times: same 10-byte card entry
u32_le   activatable_count = a
repeat a times:
    u32_le card_code
    u8     controller
    u8     location
    u32_le sequence
    u64_le description
    u8     client_mode
u8       to_battle_phase
u8       to_end_phase
u8       shuffle_hand
```

The first, second, fourth, fifth, and sixth entry sections are 10 bytes per
entry. The reposition section is 7 bytes per entry. The activation section is
19 bytes per entry. The exact length is:

```text
payload_bytes = 28 + 10*s + 10*ss + 7*r + 10*m + 10*st + 19*a
total_bytes   = 29 + 10*s + 10*ss + 7*r + 10*m + 10*st + 19*a
```

The syntactic minimum is 29 bytes. All three final flags and every activation
`client_mode` are validated as described for BATTLECMD. Every count is a
modern `u32_le`; legacy one-byte counts and narrow sequences are forbidden.

### Complete domain and order

The complete legal domain has exactly:

```text
N = s + ss + r + m + st + a
      + (to_battle_phase == 1 ? 1 : 0)
      + (to_end_phase == 1 ? 1 : 0)
      + (shuffle_hand == 1 ? 1 : 0)
```

The domain must be non-empty. A zero count with all three flags zero is not a
pass candidate; it is a fail-closed zero-option state.

The exact concatenation order is:

1. `SUMMON` entries;
2. `SPECIAL_SUMMON` entries;
3. `REPOSITION` entries;
4. `MSET` entries;
5. `SSET` entries;
6. `ACTIVATE` entries;
7. `TO_BP` when flagged;
8. `TO_EP` when flagged;
9. `SHUFFLE_HAND` when flagged.

Within each repeated section, preserve wire order. The source entries are
identified by `(section, source_ordinal)`, so equal card codes and equal
coordinates do not collapse. Exact response bindings are:

| Candidate | Response integer |
| --- | ---: |
| `SUMMON` ordinal `i` | `(i << 16) \| 0` |
| `SPECIAL_SUMMON` ordinal `i` | `(i << 16) \| 1` |
| `REPOSITION` ordinal `i` | `(i << 16) \| 2` |
| `MSET` ordinal `i` | `(i << 16) \| 3` |
| `SSET` ordinal `i` | `(i << 16) \| 4` |
| `ACTIVATE` ordinal `i` | `(i << 16) \| 5` |
| `TO_BP` | `6` |
| `TO_EP` | `7` |
| `SHUFFLE_HAND` | `8` |

The transition flags are the core's already computed legal choices. Ignis
does not infer a phase transition or add a generic pass. A card-bearing
candidate cannot be public until its card reference is proven through the
current perspective and `PublicSemanticLocatorV1`.

## 7. MSG_SELECT_EFFECTYN (12)

### Exact modern request grammar

```text
u8       message_id = 12
u8       player
u32_le   card_code
ModernLocInfoV1 card_location       # 10 bytes
u64_le   description
```

The exact payload is 23 bytes and the exact complete-message length is 24
bytes. `description` is not a legacy `u32` in V1. The location is the exact
10-byte modern form, including the overlay convention described above.

The complete legal domain is exactly two scalar choices. Because the source
has no repeated choice array, I4 V1 fixes the explicit semantic order
`NO(0)`, then `YES(1)`; this is a deterministic adapter order, not a claim
that a UI button order is a wire list. The bindings are:

```text
MSG_SELECT_EFFECTYN:NO  -> i32 0 -> 00 00 00 00
MSG_SELECT_EFFECTYN:YES -> i32 1 -> 01 00 00 00
```

The core accepts only response integers 0 and 1. A public candidate may carry
the normalized card reference and a proven description identifier only after
the current perspective proves them. Raw location bytes and raw response
bytes stay private. An unproven required card reference fails closed.

## 8. MSG_SELECT_YESNO (13)

### Exact modern request grammar

```text
u8       message_id = 13
u8       player
u64_le   description
```

The exact payload is 9 bytes and the exact complete-message length is 10
bytes. The complete legal domain is ordered `NO(0)`, then `YES(1)` and uses
the same exact four-byte bindings:

```text
MSG_SELECT_YESNO:NO  -> i32 0 -> 00 00 00 00
MSG_SELECT_YESNO:YES -> i32 1 -> 01 00 00 00
```

The pinned core validates only 0 and 1. The description is a normalized prompt
semantic value; it is not a raw response or an identity. No extra pass,
cancel, or third response exists.

## 9. MSG_SELECT_OPTION (14)

### Exact modern request grammar

```text
u8       message_id = 14
u8       player
u8       option_count = n
repeat n times:
    u64_le option_description
```

The exact complete-message length is:

```text
payload_bytes = 2 + 8*n
total_bytes   = 3 + 8*n
0 <= n <= 255
```

`option_count` is intentionally `u8`; it is not a modern `u32`. The pinned
core casts the option-vector size to `uint8_t`. If the source vector exceeds
255, its count byte cannot describe the complete emitted vector and the V1
exact-length rule rejects the resulting value rather than silently truncating
or accepting trailing options. A `n=0` `MSG_SELECT_OPTION` is not the source
path: the pinned core emits `MSG_HINT` for an empty option vector. Therefore
`n=0` fails closed as a prohibited zero-option prompt.

The complete domain is exactly the `n` options in wire order, with duplicate
description values preserved as separate source ordinals `0..n-1`. The
response binding is:

```text
MSG_SELECT_OPTION:OPTION:i -> i32 i -> four-byte i32_le(i)
```

The core accepts exactly `0 <= i < n`. No description sorting, deduplication,
or lexical ordering is permitted.

## 10. MSG_SELECT_CHAIN (16)

### Exact modern request grammar

```text
u8       message_id = 16
u8       player
u8       special_effect_count_or_trigger_marker = spe_count
u8       forced
u32_le   hint_timing_for_player
u32_le   hint_timing_for_other_player
u32_le   chain_count = c
repeat c times:
    u32_le card_code
    ModernLocInfoV1 card_location       # 10 bytes
    u64_le description
    u8     client_mode
```

The chain entry is 23 bytes. The exact complete-message length is:

```text
payload_bytes = 15 + 23*c
total_bytes   = 16 + 23*c
```

The syntactic minimum is 16 bytes. `forced` is exactly boolean 0 or 1.
`client_mode` is the same exact 0/1/2 semantic enum as the other activation
sections. `hint_timing_for_player` and `hint_timing_for_other_player` are
four-byte engine-provided timing masks; they are prompt context, not extra
candidates and not permission to infer a missing candidate.

For `spe_count`, values other than `0x7f` are the u8 representation of the
pinned core's optional special-effect/trigger count context. `0x7f` is the
explicit trigger-selection marker used by the pinned `PointEvent` and
`QuickEffect` paths. It is not a count of 127 entries and never adds a
candidate. If an ordinary source count aliases the marker or cannot be
represented unambiguously by this byte, the prompt is unproven and fails
closed. No public candidate contains a raw pointer or internal effect ID.

### Complete domain, cancel semantics, and order

Every chain entry is a legal source response candidate. The core's response
validator accepts an entry index `0..c-1`. If `forced == 0`, it also accepts
exactly the protocol-defined no-chain sentinel `-1`; if `forced == 1`, `-1` is
illegal. Consequently:

```text
forced == 0: N = c + 1; the final candidate is NO_CHAIN
forced == 1: N = c;     c must be >= 1
```

The optional empty chain (`forced=0`, `c=0`) therefore has one candidate,
`MSG_SELECT_CHAIN:NO_CHAIN`, and is not an invented pass. A forced empty chain
has no legal source response and fails closed. The no-chain candidate is
placed after the wire entries because the `-1` choice has no wire-array
position; this is the explicit V1 canonical placement, not a fabricated source
entry.

The chain entries preserve the exact wire order after the pinned core's
`core.select_chains.sort(chain::chain_operation_sort)`. The future public
domain contains entry source ordinals 0..c-1 and then `NO_CHAIN` when allowed.
Duplicate descriptions, codes, locators, and modes remain distinct.

Bindings are:

```text
MSG_SELECT_CHAIN:CHAIN_ENTRY:i -> i32 i  -> four-byte i32_le(i)
MSG_SELECT_CHAIN:NO_CHAIN   -> i32 -1 -> FF FF FF FF
```

The `forced` bit is the sole source authority for whether `NO_CHAIN` is legal.
The future implementation must not fabricate no-chain because the wire count
is zero, and must not omit it when `forced` is zero.

## 11. MSG_SELECT_POSITION (19)

### Exact modern request grammar

```text
u8       message_id = 19
u8       player
u32_le   card_code
u8       allowed_positions_mask
```

The exact payload is 6 bytes and the exact complete-message length is 7
bytes. The only allowed position bits are the pinned values:

| Bit/value | Semantic position |
| ---: | --- |
| `0x01` | `FACEUP_ATTACK` |
| `0x02` | `FACEDOWN_ATTACK` |
| `0x04` | `FACEUP_DEFENSE` |
| `0x08` | `FACEDOWN_DEFENSE` |

The valid emitted-prompt mask is a nonzero subset of `0x0f` with at least two
bits. The pinned core resolves `positions == 0` to face-up attack and resolves
a singleton mask directly without emitting `MSG_SELECT_POSITION`; neither
shortcut is a prompt candidate or an adapter auto-answer. If a one-bit or
zero-bit prompt is received, or if any bit outside `0x0f` is present, the V1
adapter fails closed as an unsupported/unproven prompt instead of repairing it.

The complete domain has one candidate per set bit. EDOPro's pinned
`ClientAnalyze` presents and handles them in this exact order:

```text
FACEUP_ATTACK (0x01)
FACEDOWN_ATTACK (0x02)
FACEUP_DEFENSE (0x04)
FACEDOWN_DEFENSE (0x08)
```

This order is proven by the pinned reader's four ordered bit tests; it is not a
generic numeric sort selected for convenience. The response body is the
position value itself:

```text
MSG_SELECT_POSITION:FACEUP_ATTACK    -> i32 1 -> 01 00 00 00
MSG_SELECT_POSITION:FACEDOWN_ATTACK  -> i32 2 -> 02 00 00 00
MSG_SELECT_POSITION:FACEUP_DEFENSE   -> i32 4 -> 04 00 00 00
MSG_SELECT_POSITION:FACEDOWN_DEFENSE -> i32 8 -> 08 00 00 00
```

The card code is a wire fact, not automatically a public identity. Since this
layout carries no card locator, a future public candidate may expose the code
only when the current perspective independently proves that scalar identity;
otherwise the prompt fails closed. No raw coordinate is invented.

## 12. Complete-domain invariant and duplicates

The old shorthand “N protocol options = N adapter candidates” is refined for
these heterogeneous families to mean:

```text
N = cardinality of the complete proven legal current source-response domain
```

Repeated sections contribute their entries exactly once in source order.
Boolean transition flags contribute one candidate when and only when the
source flag is 1. Position bits expand to one candidate per set bit in the
proven EDOPro order. `MSG_SELECT_CHAIN` contributes the protocol-defined
`NO_CHAIN` response exactly when `forced == 0`. Scalar yes/no prompts have two
fixed response values. There are no hidden pass/cancel choices beyond these
proven rules.

Every proven legal source response maps to exactly one candidate and every
candidate maps to exactly one response. A malformed or semantically unproven
prompt publishes no domain, not a partial domain. Duplicate source entries are
preserved; only their source section/ordinal distinguishes them when public
fields otherwise compare equal.

## 13. Privacy and determinism exclusions

Public candidates and public action keys must never contain or depend on:

```text
MirrorEntityIdV1
ModernLocInfoV1 raw values
raw controller/location/sequence/position tuples
raw GAME_MSG or CTOS_RESPONSE bytes
raw response indices when not the explicitly normalized public choice token
protocol offsets
pointer/object identity
allocation order
relation ordinals
hidden opponent card identity or hidden deck order
stale identity after knowledge destruction
socket/session identity
host, port, room password
PID, process handle, timestamp, wall clock, thread/task ID
filesystem path
TCP chunk boundaries or receive-buffer identity
dictionary/hash-map iteration order
model output, teacher output, archetype inference, beliefs, or probabilities
```

The seven prompt messages are delivered by the pinned server only to the
selecting player. That routing does not waive the Ignis public boundary: a
future projection still uses the established perspective mirror and the
accepted public semantic locator contract. If the public visibility or card
reference proof is missing, the candidate domain fails closed.

The semantic candidate order, keys, prompt ordinal, and private binding
association depend only on the complete accepted prompt and semantic GAME_MSG
order. They are invariant under TCP segmentation, machine, culture, locale,
timezone, process restart, scheduling, allocation order, and filesystem
location.

## 14. Failure contract

The future implementation must distinguish at least these categories:

```text
MALFORMED_PROMPT
UNSUPPORTED_PROMPT_LAYOUT
UNPROVEN_PUBLIC_REFERENCE
UNPROVEN_CANDIDATE_DOMAIN
INVALID_PUBLIC_ACTION_KEY
STALE_PROMPT_BINDING
INVALID_RESPONSE_BINDING
```

Every failure returns no authoritative candidate domain and sends no response.
Count arithmetic, source-section bounds, boolean/enum values, participant and
location proof, position masks, chain cancel rules, and legacy-width rejection
are all fail-closed checks. No retry policy, teacher, random action, first
candidate, generic pass, or partial answer is allowed.

## 15. Evidence and non-goals

The source authority for each frozen category is explicit:

| Frozen category | Exact pinned authority |
| --- | --- |
| message IDs and position/mode constants | ocgcore `ocgapi_constants.h#MSG_SELECT_*`, `POS_*`; EDOPro `gframe/ocgapi_constants.h#POS_*`, `EFFECT_CLIENT_MODE_*` |
| BATTLECMD layout/domain/validation | ocgcore `playerop.cpp#field::process(SelectBattleCmd&)`; EDOPro `gframe/duelclient.cpp#ClientAnalyze(MSG_SELECT_BATTLECMD)` |
| IDLECMD layout/domain/validation | ocgcore `playerop.cpp#field::process(SelectIdleCmd&)`; EDOPro `gframe/duelclient.cpp#ClientAnalyze(MSG_SELECT_IDLECMD)` |
| EFFECTYN layout/domain/validation | ocgcore `playerop.cpp#field::process(SelectEffectYesNo&)`; EDOPro `gframe/duelclient.cpp#ClientAnalyze(MSG_SELECT_EFFECTYN)` |
| YESNO layout/domain/validation | ocgcore `playerop.cpp#field::process(SelectYesNo&)`; EDOPro `gframe/duelclient.cpp#ClientAnalyze(MSG_SELECT_YESNO)` |
| OPTION layout/domain/validation | ocgcore `playerop.cpp#field::process(SelectOption&)`; EDOPro `gframe/duelclient.cpp#ClientAnalyze(MSG_SELECT_OPTION)` |
| CHAIN layout/domain/validation | ocgcore `playerop.cpp#field::process(SelectChain&)`, `processor.cpp#PointEvent`/`QuickEffect`; EDOPro `gframe/duelclient.cpp#ClientAnalyze(MSG_SELECT_CHAIN)` |
| POSITION layout/domain/validation/order | ocgcore `playerop.cpp#field::process(SelectPosition&)`; EDOPro `gframe/duelclient.cpp#ClientAnalyze(MSG_SELECT_POSITION)` |
| modern nested location | ocgcore `duel.h#duel::duel_message::write`, `card.cpp#card::get_info_location`; EDOPro `gframe/core_utils.cpp#ReadLocInfo` |
| chain/activation source order and mode | ocgcore `field.cpp#chain::chain_operation_sort`, `effect.cpp#effect::get_client_mode`, and the three `playerop.cpp` writers |
| response body and CTOS envelope | EDOPro `gframe/duelclient.h#SetResponseI`, `SendBufferToServer`, `gframe/event_handler.cpp` response handlers, and `gframe/duelclient.cpp#DuelClient::SendResponse`; core `duel.cpp#duel::set_response` and `playerop.cpp` validators |
| selecting-player routing | EDOPro `gframe/generic_duel.cpp#GenericDuel::Sending` and `GetResponse` |

All listed authorities are at EDOPro commit
`30935e847165a9ef0e547fb51a43f36168fab7c7` or ocgcore commit
`46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57` as specified in Section 1.

The exact positive and negative vectors are in
`fixtures/gameplay/v1/i4-flat-prompt-vectors.v1.json`. The current machine
inventory records each frozen layout as `layout_status=FROZEN` and
`support_status=OUT_OF_SCOPE`; this is a contract freeze, not runtime support.

This task does not:

- change `GameplayMessageDecoderV1`, the mirror, locators, or public-state JSON;
- implement prompt decoding, candidate construction, or response sending;
- add `PrivateResponseBinding` production types;
- implement I5 combinatorial prompts or continuations;
- implement model input, model runner, IPC, checkpoint binding, replay, or audit;
- claim OCGForge `PlayerObservation` compatibility or I6 cross-oracle acceptance;
- use WindBot as semantic authority;
- change rules, upstream pins, EDOPro, or ocgcore.

```text
I4_IMPLEMENTED=NO
I5_STARTED=NO
MODEL_INPUT_READY=NO
I6_CROSS_ORACLE_ACCEPTED=NO
PR_CREATED=NO
```
