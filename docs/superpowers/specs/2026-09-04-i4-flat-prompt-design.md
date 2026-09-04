# OCGForge-Ignis I4A0 — Flat Prompt Wire and Response Design

Status: `I4A0_CONTRACT_FREEZE=YES`; research/design only
Date: 2026-09-04
Accepted base: `4a054c3e0f0be10b704a1614ae275d4ce630ddce`

## 1. Purpose and boundary

I4A0 freezes the facts a later I4 implementation can consume for exactly
seven flat prompt families. It deliberately stops before production decoding,
candidate construction, response sending, model input, or live server use.

```text
accepted PerspectiveStateMirrorV1
        ↓
future I4 prompt boundary
        ├─ exact modern prompt grammar
        ├─ complete source-response domain
        ├─ private source resolution through the mirror
        ├─ public candidate projection from the accepted I3D result
        └─ current-prompt-local private response binding
```

The mirror is a private resolution authority only. The accepted successful
`PublicStateProjectionResultV1` from I3D is the public locator and card-code
authority. `PublicSemanticLocatorV1` validates or compares copied locator
syntax; it is not a second publication authority.

The rules engine remains the pinned EDOPro/ocgcore authority. Ignis must not
recompute legality, pick the first action, infer a missing option, or repair an
invalid response. A prompt is publishable only when both directions are
proven:

```text
every legal source response → exactly one candidate
every candidate → exactly one legal source response
```

The normative result is
`docs/contracts/flat-prompt-projection-v1.md`. This document records the
research path and the decisions that led to that contract.

## 2. Exact pinned-source review

The research checkout was detached at the required commits, not at a default
branch:

```text
EDOPRO_COMMIT=30935e847165a9ef0e547fb51a43f36168fab7c7
OCGCORE_COMMIT=46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57
EDOPRO_OCGCORE_GITLINK=46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57
```

The exact source roles are:

| Source | Relevant symbol/case | Role in this freeze |
| --- | --- | --- |
| ocgcore `ocgapi_constants.h` | `MSG_SELECT_*`, `POS_*`, `EFFECT_CLIENT_MODE_*` | numeric constants |
| ocgcore `duel.h`, `duel.cpp` | `duel::duel_message::write`, `duel::set_response` | integer byte writes and response copy |
| ocgcore `playerop.cpp` | `field::process(SelectBattleCmd&)` | BATTLECMD producer and response validator |
| ocgcore `playerop.cpp` | `field::process(SelectIdleCmd&)` | IDLECMD producer and response validator |
| ocgcore `playerop.cpp` | `field::process(SelectEffectYesNo&)` | EFFECTYN producer and 0/1 validator |
| ocgcore `playerop.cpp` | `field::process(SelectYesNo&)` | YESNO producer and 0/1 validator |
| ocgcore `playerop.cpp` | `field::process(SelectOption&)` | OPTION producer, count, and index validator |
| ocgcore `playerop.cpp` | `field::process(SelectChain&)` | CHAIN producer, forced/-1 rule, and index validator |
| ocgcore `playerop.cpp` | `field::process(SelectPosition&)` | POSITION producer, bitmask, and position validator |
| ocgcore `processor.cpp` | `PointEvent`, `QuickEffect`, `spe_effect` paths | chain special-count meaning and `0x7f` marker use |
| ocgcore `field.cpp` | `chain::chain_operation_sort` | source ordering of chain/activation entries |
| ocgcore `effect.cpp` | `effect::get_client_mode` | activation mode values and meaning |
| EDOPro `gframe/duelclient.cpp` | `CompatRead`, `ClientAnalyze` prompt cases | modern widths, prompt reading, position display order |
| EDOPro `gframe/core_utils.cpp` | `ReadLocInfo` | modern 10-byte nested location |
| EDOPro `gframe/duelclient.h` | `SetResponseI`, `SendBufferToServer` | response body width and CTOS envelope |
| EDOPro `gframe/event_handler.cpp` | command/yes-no/chain response handlers | UI-to-response mapping corroboration |
| EDOPro `gframe/generic_duel.cpp` | `Sending`, `GetResponse` | player routing and original response handoff |

No source implementation or serialized upstream packet was copied. The
checked-in vectors are independently authored from these facts.

## 3. Research findings

### Common modern path

The core writer appends the message ID and writes typed C++ values in field
order. Its `get_info_location()` returns controller, location, sequence, and
position; for an overlay it returns the parent location with the overlay bit,
the parent sequence, and the overlay sequence as position. The EDOPro modern
reader uses 32-bit counts/sequences/descriptions where `CompatRead` selects
the modern type, and it reads modern `loc_info` as two bytes followed by two
32-bit values. These locations remain private source-resolution facts. A
future I4 implementation may publish only the exact locator and card code
copied from the accepted successful I3D public-state snapshot.

Therefore I4 V1 is modern-only:

```text
u8
u32_le
u64_le
ModernLocInfoV1 = u8 controller; u8 location; u32_le sequence; u32_le position
```

The old compatibility path uses narrow counts, narrow sequences, narrow
descriptions, and legacy chain fields. It is not a second accepted grammar.
An implementation must reject a legacy-looking value even when it could be
made to fit by looking at remaining bytes.

### Response path

The pinned client stores scalar responses using `SetResponseI`, which copies
an `int32_t`. `SendResponse` wraps that exact four-byte body as
`CTOS_RESPONSE`; `SendBufferToServer` sets the frame length to packet type plus
body. The core copies the body and its prompt processors inspect
`returns.at<int32_t>(0)`. The accepted V1 representation is consequently one
`i32_le` body per flat choice, with no response-specific width variation.

### BATTLECMD

`field::process(SelectBattleCmd&)` writes an activatable count and entries,
then an attackable count and entries, followed by two transition bytes. The
core validates a low 16-bit kind `0..3` and a high 16-bit section index. The
client preserves the two sections separately, and the event handler confirms
the response values `kind=0` for activation, `kind=1` for attack, and the
scalar values `2`/`3` for phase transitions.

The exact complete length is `12 + 19*a + 8*b`. No generic pass is present.
The source order is the exact wire concatenation. Activatable chains have
already been sorted by the pinned core before writing; the adapter preserves
that order without exposing the internal effect ID.

### IDLECMD

`field::process(SelectIdleCmd&)` writes six sections in this exact order:

```text
summonable
spsummonable
repositionable
msetable
ssetable
activatable
```

The first, second, fourth, fifth, and sixth entries contain a 32-bit sequence
and are 10 bytes. Reposition entries contain an 8-bit sequence and are 7
bytes. The three final bytes are the source-computed transitions to Battle
Phase, End Phase, and hand shuffle. The core response validator uses low kinds
0 through 8 in the same section/transition order.

The exact complete length is
`29 + 10*s + 10*ss + 7*r + 10*m + 10*st + 19*a`. This makes a heterogeneous
section concatenation mandatory; flattening only card arrays would omit legal
phase or shuffle responses.

### EFFECTYN and YESNO

`SelectEffectYesNo` writes player, card code, modern `loc_info`, and a 64-bit
description. `SelectYesNo` writes player and a 64-bit description. Their core
processors accept exactly response values 0 and 1. There is no wire array that
defines an order, so the contract fixes the scalar semantic order as
`NO(0)`, then `YES(1)`. This is an explicit adapter ordering rule, not an
assertion that a UI layout is a wire list.

The exact complete lengths are 24 bytes for EFFECTYN and 10 bytes for YESNO.
An effect card reference is projected only by privately resolving the source
and uniquely correlating it to a `PublicCardStateV1` in the accepted successful
I3D snapshot. The candidate copies that snapshot card's exact public locator;
raw location bytes never become a candidate field.

### OPTION

`SelectOption` writes player, one `uint8_t` count, then each option as a
64-bit description in vector order. The core emits a hint instead of this
message when the vector is empty. The core casts the vector size to `uint8_t`,
so a source vector over 255 cannot be represented by one exact V1 message;
the adapter rejects any resulting count/body mismatch rather than accepting a
wrapped count and trailing options.

The exact complete length is `3 + 8*n`, `0 <= n <= 255`, with `n > 0` for a
valid SELECT_OPTION prompt. The response is the zero-based option index as a
four-byte signed integer. Descriptions remain in wire order and duplicates
remain separate source ordinals.

### CHAIN

`SelectChain` writes player, `spe_count`, `forced`, two 32-bit hint-timing
values, a 32-bit entry count, and 23-byte entries containing card code,
modern `loc_info`, description, and client mode. The exact complete length is
`16 + 23*c`.

The `spe_count` field is sourced from `core.spe_effect`; the pinned processor
comments it as the number of optional trigger effects/activate or quick
effects with hints. The value `0x7f` is explicitly passed by trigger-selection
paths and is a marker, not an additional entry count. An ordinary source
count that aliases this marker or cannot be represented unambiguously is
unproven and must fail closed. `forced` is the actual source authority for
cancel legality. The processor accepts entry index
`0..c-1`, and accepts `-1` exactly when `forced` is false.

The public domain is the wire entries followed by one `NO_CHAIN` candidate
when permitted. The optional zero-entry case therefore has one real source
response, `-1`; a forced zero-entry case has no legal response and fails
closed. This accounts for the protocol sentinel without fabricating a pass.

### POSITION

`SelectPosition` writes player, card code, and one byte of position flags. The
core masks the local value to the low four position bits before writing a
prompt. The four pinned values are `0x01` face-up attack, `0x02` face-down
attack, `0x04` face-up defense, and `0x08` face-down defense. The core directly
resolves zero or a singleton and does not emit a prompt in those cases.

When a prompt is emitted with multiple bits, EDOPro tests the four buttons in
the order `0x01`, `0x02`, `0x04`, `0x08`. The response is the selected bit,
not a zero-based bit index. I4 V1 rejects a received zero/singleton prompt or
any bit outside `0x0f`; it never reproduces the core's direct-resolution
shortcut as an adapter auto-answer.

## 4. Exact candidate schema and identity ownership

The accepted public decision surface is a fixed split, not an open-ended bag
of useful fields:

```text
FlatPromptPublicContextV1
FlatPublicCandidateDescriptorV1
```

The common context always contains exactly:

```text
contract_id   = ocgforge-ignis.flat-prompt-projection.v1
prompt_family = one of the seven frozen I4 family names
acting_player = absolute player 0 or 1
```

Family-specific context is fixed as follows:

| Family | Required context | Conditional context | Context fields absent |
| --- | --- | --- | --- |
| BATTLECMD | common context | none | effect description, mode, position, transition, option, chain metadata |
| IDLECMD | common context | none | effect description, mode, position, transition, option, chain metadata |
| EFFECTYN | common context, proven effect-card public locator copied from the accepted I3D snapshot, effect description ID | effect card code copied from that snapshot card under `CARD_CODE_SAFE` | position, transition, option, chain metadata |
| YESNO | common context, yes/no description ID | none | all card fields, position, transition, option, chain metadata |
| OPTION | common context | none | all card fields, description/effect, position, transition, option-shared metadata |
| CHAIN | common context, spe count, forced, both hint timings | none | position, transition, option |
| POSITION | common context, validated position mask | position card code under `POSITION_CARD_CODE_SAFE`; false proof makes the field ABSENT and does not reject the mask-derived domain | card locator, description/effect, transition, option, chain metadata |

Each candidate descriptor has exactly these possible members:

```text
i4_local_candidate_key
choice_kind
source_section
source_ordinal
public_semantic_card_locator
card_code
description_or_effect_id
client_mode
direct_attackable
position_value
transition_token
option_value
```

Section 3 of the normative contract contains the complete required/absent/
conditional matrix for every candidate kind. `ABSENT` is actual absence, not
null, zero, an empty string, or a caller-selected omission. Every conditional
member follows its named predicate exactly. Card-bearing BATTLECMD, IDLECMD,
and CHAIN entries require a locator copied from a uniquely correlated public
snapshot card; EFFECTYN requires that copied locator in shared context.
POSITION has no wire locator and a false safe-code predicate only makes its
conditional card-code field absent. OPTION requires its decoded u64 option
value, so a different public option value remains distinguishable even when
its ordinal is the same in another prompt.

The I4 local ASCII selector is named `i4_local_candidate_key`:

```text
I4_LOCAL_CANDIDATE_KEY_IS_OCGFORGE_PUBLIC_ACTION_KEY=NO
I4_LOCAL_CANDIDATE_KEY_MODEL_INPUT_AUTHORIZED=NO
I4_LOCAL_CANDIDATE_KEY_I6_COMPATIBILITY_CLAIM=NO
OCGFORGE_PUBLIC_ACTION_KEY_DERIVATION=I6_OWNED
I6_BYTE_EXACT_COMPATIBILITY=UNPROVEN
```

The already accepted OCGForge contract
`ocgforge.public_action_identity.v1` owns the separate
`public_action.v1.<lowercase hexadecimal canonical descriptor bytes>` value.
I4A0 neither aliases nor derives that value. I6 owns the future mapping from
the exact Ignis candidate descriptor to the OCGForge descriptor and the
byte-exact `public_action_key` proof.

The private current-prompt binding contains `prompt_instance_ordinal`, the
family, the complete ordered public descriptors and local keys, and the exact
response body. A repeated local key across prompt instances is valid only
with the matching ordinal, family, and complete current domain. The ordinal
never enters the future OCGForge identity or model input.

## 5. Domain and privacy decisions

The exact source-response domain is the cardinality of all repeated entries,
flag-created transitions, position-bit expansions, scalar choices, and the
protocol-defined chain `-1` where legal. The public candidate is built only
after the source prompt has been completely validated. For card-bearing
choices, the source is privately resolved through the established perspective
mirror and then correlated to the accepted successful I3D public-state
snapshot; the published locator and card code are copied from that snapshot.
The mirror and `PublicSemanticLocatorV1` cannot create a second public identity.

The correlation rules are exact:

```text
indexed visible card:
    absolute player + semantic zone + semantic sequence
    -> exactly one accepted snapshot card -> copy its locator

known HAND/EXTRA_DECK card:
    absolute player + zone + known public card code
    -> exactly one accepted public-ordinal card -> copy its locator
    -> any duplicate ambiguity without public semantic proof fails closed

overlay card:
    accepted public overlay components
    -> exactly one accepted snapshot card -> copy its locator

MAIN_DECK card:
    no per-card V1 locator -> fail closed when a candidate requires one
```

Raw hand/extra sequence, physical continuity, mirror identity, collection
order, allocation order, and relation ordinals never select a public ordinal.
If the accepted snapshot has no matching card or more than one permitted
correlation, the complete card-bearing prompt fails closed. For POSITION, the
validated multi-bit mask remains the complete domain authority; an unproven
card code is simply absent and does not reject the prompt.

The following source data stays private even when it was required to decode a
candidate:

```text
raw GAME_MSG bytes
raw CTOS response bytes
raw ModernLocInfoV1
raw protocol offsets
MirrorEntityIdV1
effect/allocation/object identity
socket, host, port, password, PID, timestamp, thread ID
hidden opponent identity or deck order
```

Source section/entry ordinals are public semantic disambiguators. They do not
encode or derive from internal entity IDs. I4 local candidate keys are local
to the current prompt; private binding values are never serialized into public
candidate data or model input. The OCGForge `public_action_key` name and
format remain an I6-owned, unproven boundary.

## 6. Negative and metamorphic evidence

The vector fixture covers all seven families with independently constructed
positive payloads and includes:

- fixed-field truncation and exact-length trailing-byte rejection;
- counted body mismatch and checked arithmetic overflow;
- invalid participants, boolean/enum flags, position masks, and card references;
- zero-option BATTLE/IDLE/OPTION and forced-empty CHAIN;
- optional-empty CHAIN with the real `-1` response;
- illegal option/chain/position response values;
- duplicate option descriptions retained as two candidates;
- stale current-prompt selections rejected by semantic prompt ordinal/family;
- indexed visible, unique known-hand, absent-locator, duplicate-hand ambiguity,
  and paired internal-history projection correlations;
- valid multi-bit POSITION masks retained when the unbound card code is absent;
- unsupported legacy narrow layouts.

The fixture records raw bytes as restricted protocol research evidence. It does
not authorize exposing those bytes or private bindings to a public candidate.

## 7. Resulting implementation boundary

The inventory now records the seven layouts as frozen but retains
`support_status=OUT_OF_SCOPE` and `planned_slice=I4`. This means a later I4
implementation can consume the contract only after independent review. It
must still prove runtime behavior, privacy, complete-domain coverage, and
response submission in a separate authorization.

```text
I4_IMPLEMENTED=NO
I5_STARTED=NO
MODEL_INPUT_READY=NO
I6_CROSS_ORACLE_ACCEPTED=NO
PR_CREATED=NO
```
