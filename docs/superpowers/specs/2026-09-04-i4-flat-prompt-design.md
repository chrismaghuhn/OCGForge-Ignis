# OCGForge-Ignis I4A0 — Flat Prompt Wire and Response Design

Status: `I4A0_CONTRACT_FREEZE=YES`; research/design only
Date: 2026-09-04
Accepted base: `4a054c3e0f0be10b704a1614ae275d4ce630ddce`

## 1. Purpose and boundary

I4A0 freezes the facts a later I4 implementation may consume for exactly
seven flat prompt families. It deliberately stops before production decoding,
candidate construction, response sending, model input, or live server use.

```text
accepted PerspectiveStateMirrorV1
        ↓
future I4 prompt boundary
        ├─ exact modern prompt grammar
        ├─ complete source-response domain
        ├─ perspective-safe public candidate projection
        └─ current-prompt-local private response binding
```

The rules engine remains the pinned EDOPro/ocgcore authority. Ignis may not
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
32-bit values.

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
An effect card reference is projected only through a proven public semantic
locator or a proven perspective-private scalar; raw location bytes never
become a candidate field.

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

## 4. Domain and privacy decisions

The exact source-response domain is the cardinality of all repeated entries,
flag-created transitions, position-bit expansions, scalar choices, and the
protocol-defined chain `-1` where legal. The public candidate is built only
after the source prompt has been completely validated and card references have
been reduced through the established perspective mirror and
`PublicSemanticLocatorV1`.

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
encode or derive from internal entity IDs. Public candidate keys are local to
the current prompt; private binding values are never serialized into public
candidate data or model input.

## 5. Negative and metamorphic evidence

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
- unsupported legacy narrow layouts.

The fixture records raw bytes as restricted protocol research evidence. It does
not authorize exposing those bytes or private bindings to a public candidate.

## 6. Resulting implementation boundary

The inventory now records the seven layouts as frozen but retains
`support_status=OUT_OF_SCOPE` and `planned_slice=I4`. This means a later I4
implementation may consume the contract only after independent review. It
must still prove runtime behavior, privacy, complete-domain coverage, and
response submission in a separate authorization.

```text
I4_IMPLEMENTED=NO
I5_STARTED=NO
MODEL_INPUT_READY=NO
I6_CROSS_ORACLE_ACCEPTED=NO
PR_CREATED=NO
```
