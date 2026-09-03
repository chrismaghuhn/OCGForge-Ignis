# OCGForge-Ignis I3A0 — Gameplay Perspective, State, and Privacy Contract

Status: I3A0_CONTRACT_FREEZE=AUTHORIZED
Production code authorization: NO
I3A_AUTHORIZED: NO
I3B_AUTHORIZED: NO
I3C_AUTHORIZED: NO
I3D_AUTHORIZED: NO
Date: 2026-09-03

Accepted main base: `8badf37a4c3645060b05935236143f0cbd683dec`
Accepted I2 implementation: `3e2e319112a1237a9f42c33f3d974ba05e944e8e`

Contract ID: `ocgforge-ignis.gameplay.perspective-privacy.v1`
Message inventory schema: `ocgforge-ignis.game_message_support.v1`

This document freezes the design boundary only. It does not implement a
GAME_MSG decoder, a gameplay state mirror, prompt projection, model input, or
any later I3 slice.

## 1. Authority and purpose

I3 owns the boundary after the accepted I2 gameplay transport handoff:

```text
accepted I2 handoff
  → validated GAME_MSG decoding
  → PerspectiveStateMirror
  → PublicContractProjection
```

EDOPro and its exact V1 ocgcore runtime remain the sole authorities for game
rules, engine state, legal prompts, and accepted CTOS responses. I3 may only
reconstruct facts proven by the protocol stream visible to this client. I3 is
not a rules engine, a legality engine, a prompt-answering layer, a candidate
generator, a model adapter, or a second OCG core.

The priority order is:

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

Unproven gameplay semantics, visibility, continuity, identity, or message
ordering fail closed.

## 2. Frozen layer separation

The following layers are distinct contracts, not aliases of one state object:

| Layer | Owner | Allowed content | Forbidden content |
| --- | --- | --- | --- |
| `RawProtocolState` | transport/protocol boundary | exact accepted I2 handoff, raw GAME_MSG bytes, transport progress | model input, public semantic identity |
| `PerspectiveStateMirror` | I3 | only perspective-legitimate gameplay facts and explicit unknowns | omniscient opponent identity, engine pointers, response bytes |
| `PublicContractProjection` | I3 | deterministic, value-owned, perspective-safe model-facing state | raw offsets, socket/control metadata, private response binding |
| `PrivateResponseBinding` | I4/I5 later | selected semantic key to original CTOS response | I3 state or model feature |
| `ModelRunnerBoundary` | I7 later | accepted model contract and scores | raw protocol or gameplay transport |
| `PublicAuditTrace` | I10 later | redacted semantic evidence | hidden identity, password, raw socket data |
| `PrivateProtocolTrace` | I10 later | restricted raw diagnostics when explicitly enabled | training input or authoritative model input |

The information flow is one-way:

```text
received protocol bytes
  → validated gameplay messages
  → perspective-owned mirror
  → privacy projection
  → public value objects
```

No lower-layer object, buffer, address, or reference becomes a public semantic
locator merely because it is convenient to expose.

## 3. I2 handoff and ownership

I2 freezes the gameplay perspective at:

```text
FINAL_GAMEPLAY_PERSPECTIVE=UNRESOLVED_AT_I2_HANDOFF
```

The accepted internal `GameplayTransportHandoffV1` contains the live byte
transport, public pre-duel facts, and an exact copy of causally valid unread
bytes. I3 must:

1. claim the handoff exactly once;
2. become the sole transport owner after a successful claim;
3. process the pending-byte copy before issuing a new transport read;
4. retain incomplete pending bytes exactly until completion or terminal failure;
5. never call I2 again for parsing, reading, or closing;
6. close/dispose the transferred transport exactly once on I3 failure;
7. transfer no socket, password, endpoint, PID, task, or object identity into
   public state.

If the handoff claim fails, I3 returns a stable typed ownership failure and
performs no read. Pending bytes are not reinterpreted according to TCP chunking.
The transport is not closed by I3 after a successful handoff merely because
I2 has reached its terminal state; ownership has transferred.

## 4. Modern GAME_MSG envelope and decoding boundary

I1 preserves the exact `STOC_GAME_MSG` payload opaquely. I3A will interpret
that payload as a modern V1 core message stream:

```text
u8     message_id
byte[] message_payload
```

The first byte is the `MSG_*` identifier. The payload has no second TCP frame
or adapter framing. A strict cursor must consume exactly the message payload;
underflow, overflow, unknown IDs, and trailing bytes are typed failures.

The locked V1 target is the modern `41.0.2` / ocgcore API `11.0` path. Legacy
compatibility layouts are not silently accepted. In particular, modern
`CompatRead`-wide fields use their wide forms, and modern `loc_info` is:

```text
u8     controller
u8     location
u32_le sequence
u32_le position
```

The modern `MSG_START` payload is frozen exactly as:

```text
u8     message_id = 4
u8     playertype
u32_le lp[0]
u32_le lp[1]
u16_le deck_count[0]
u16_le extra_count[0]
u16_le deck_count[1]
u16_le extra_count[1]
```

The complete inner message is exactly 18 bytes. No legacy duel-rule byte is
accepted in this V1 contract.

## 5. Final gameplay perspective

`MSG_START.playertype` is the only establishment evidence for final gameplay
perspective. I3 does not choose perspective from lobby position, host flag,
RPS outcome, TP choice, or pre-duel position.

Accepted V1 values are:

| `playertype` | Meaning | I3 V1 result |
| --- | --- | --- |
| `0x00` | self is canonical gameplay player 0 | establish `SelfIsPlayer0` |
| `0x01` | self is canonical gameplay player 1 | establish `SelfIsPlayer1` |
| `0x10`/`0x11` | observer variants used by the pinned server | reject `UNSUPPORTED_PERSPECTIVE` |
| any other value | unproven or ambiguous role | fail closed |

After establishment, the perspective is immutable. A second `MSG_START`, a
conflicting perspective marker, or a `MSG_START` in an impossible mirror state
fails closed; it never resets or silently corrects the mirror.

Before perspective is established, only a valid `MSG_START` may establish the
gameplay mirror. A state-relevant message that requires a player mapping before
that point fails with `PERSPECTIVE_NOT_ESTABLISHED`. UI-only data is not a
substitute for perspective evidence.

The two LP and deck/extra-count pairs in `MSG_START` are indexed by canonical
gameplay player `0` and `1`. After the marker is accepted, I3 maps those values
to `Self` and `Opponent` through the established perspective. The I2 lobby
position is retained only as provenance and is never used to override the
marker. If a future cross-check proves an irreconcilable protocol conflict, it
fails closed rather than selecting one interpretation.

`MSG_START` initializes the semantic turn count to `0`. Each accepted
`MSG_NEW_TURN` carries the new turn player and increments that semantic count
exactly once in message order. The count is derived from semantic messages,
not transport reads or packet offsets; a duplicate or out-of-order turn event
fails closed.

## 6. PerspectiveStateMirror V1

The conceptual mirror is a value-owned semantic model with explicit unknowns.
It contains no raw engine object or protocol buffer reference.

### Participants

Exactly two V1 gameplay roles exist after `MSG_START`:

```text
Self
Opponent
```

Each participant may carry LP, turn-relevant public status, zone summaries,
and public card entities. Canonical participant order is `Self`, then
`Opponent`; no dictionary or allocation order is semantic.

### Zones and slots

The mirror can represent:

- Main Deck;
- Extra Deck;
- Hand;
- Monster Zone;
- Spell/Trap Zone, including proven special positions;
- Graveyard;
- Banished/Removed;
- Overlay relationships when the protocol proves them.

Each zone has explicit count/occupancy semantics. A count is not a list of
identities. Field slots are ordered by their protocol position only after the
corresponding participant perspective is established. Unknown or unoccupied
slots remain explicit rather than being omitted or filled with a fabricated
card.

### Card/entity fields

A card entity may carry only fields proven by the current message history:

- a semantic locator;
- participant role, controller, and owner where each is proven;
- zone and sequence relationship;
- position/face-up/face-down/public visibility;
- card knowledge state;
- public card properties such as type, attack, defense, counters, or link
  data only when the query/reveal contract proves them;
- equipment, target, overlay, or chain relationship only when the relationship
  is currently proven.

The mirror does not add fields because ocgcore has them internally. Every field
has one of these provenance classifications:

```text
PUBLIC_PROTOCOL_FACT
PERSPECTIVE_PRIVATE_FACT
DERIVED_FROM_PROVEN_PUBLIC_FACTS
UNKNOWN_REDACTED
```

Derived values must have a deterministic source path and may not smuggle a
hidden identity through a feature or relation.

## 7. Card knowledge model

`card_code=0` is not the unknown representation. The conceptual V1 union is:

```text
UnknownIdentity
KnownPrivateIdentity(card_code)
KnownPublicIdentity(card_code)
```

`KnownPrivateIdentity` is allowed only for information legitimately known to
this player, such as the bot's own hand. `KnownPublicIdentity` is allowed for
an opponent card only while the current protocol history proves that identity
is public. `UnknownIdentity` carries no stale card code.

When a previously known identity enters a hidden, unordered, or otherwise
ambiguous population, the old identity and locator association are destroyed.
The mirror may retain only the count, zone, and other proven public facts.
It must not preserve the old code in a hidden side field, diagnostic alias, or
hash input.

Duplicate card codes are distinct entities when the protocol proves distinct
public entities. A card code is a property, never a unique object locator.

## 8. Knowledge-destroying transitions

The following message families are knowledge boundaries for affected hidden
entities:

| Transition/message | Frozen effect |
| --- | --- |
| `MSG_SHUFFLE_DECK` | invalidate hidden deck ordering and locator-to-card associations; retain only proven counts/public facts |
| `MSG_SHUFFLE_HAND` | invalidate hidden hand ordering and associations; do not infer identities from the prior hand order |
| `MSG_SHUFFLE_EXTRA` | invalidate hidden Extra Deck ordering and associations |
| `MSG_SHUFFLE_SET_CARD` | invalidate affected hidden set-card continuity unless a complete independent mapping proves the same entity |
| `MSG_REVERSE_DECK` | invalidate hidden ordering continuity unless the mirror has an independently proven complete mapping; never expose a guessed mapping |
| `MSG_REFRESH_DECK` / `MSG_SWAP_GRAVE_DECK` | invalidate any hidden locator relationship that the new representation does not prove |
| hidden randomized movement | destroy continuity when source-to-destination identity is not proven |
| hidden reorder with ambiguity | destroy the old association and create explicit unknowns as needed |

For own private cards, a deterministic operation may retain an identity only if
the protocol and current private knowledge prove the complete mapping. For
opponent hidden cards, the default is destruction. No continuity may be
recovered from prior ordering, array index, packet timing, move order, unique
card code, deck composition, elimination, probability, or later model output.

## 9. Semantic public locators

I3 distinguishes three address classes:

| Address class | Use |
| --- | --- |
| protocol-local address | raw controller/location/sequence/position used only to decode a message |
| mirror locator | internal perspective-owned entity reference with explicit lifecycle |
| public semantic locator | deterministic, perspective-safe value exposed by the public projection |

Protocol-local addresses are not automatically public identity. Public
locators must be reproducible from accepted semantic history and independent
of pointers, object allocation, PID, wall time, task/thread scheduling,
filesystem paths, TCP segmentation, receive-buffer boundaries, and hash-map
iteration.

The lifecycle is explicit:

```text
create
→ retain while continuity is proven
→ move while the same entity is proven
→ rebind only with an explicit proven mapping
→ destroy at a knowledge-destroying or ambiguous transition
→ replace with a fresh locator when a new semantic entity is established
```

Locator allocation order may use canonical semantic message order, but never
transport chunk order. The canonical public locator table is ordered by the
public locator's deterministic creation ordinal, with participant, zone, and
slot fields encoded in fixed contract order. A destroyed hidden locator never
reappears under the same public identity.

Two equal-code public cards receive two distinct locators when they are two
distinct proven entities. If a message cannot distinguish two possible source
entities, the transition fails closed instead of choosing one.

## 10. PublicContractProjection V1

The projection is the only future model-facing gameplay state input. It is
value-owned, deterministic, perspective-safe, and explicit about unknowns.
It may contain:

- the established `Self`/`Opponent` perspective role;
- LP, turn player, turn count, and phase when protocol-proven;
- public zone counts and proven field slot occupancy;
- own private card identities legitimately known to the bot;
- opponent card identities only while currently public;
- `UnknownIdentity` for hidden opponent cards and ambiguous continuity;
- proven controller/owner relationships;
- proven public chain/target/equipment/overlay relationships;
- semantic public locators and their canonical lifecycle state;
- a versioned state/projection schema identifier.

It must not contain:

- raw GAME_MSG bytes or packet offsets;
- raw protocol controller/location/sequence as an identity shortcut;
- socket, endpoint, port, password, PID, task/thread, process handle, wall
  clock, receive count, chunk boundary, absolute path, or object reference;
- private response bindings or CTOS response bytes;
- hidden opponent decklist/order or hidden card codes;
- inferred archetypes, probabilities, beliefs, or model-derived facts;
- incomplete or fabricated legal candidate data.

The I3 projection is not declared byte-equal to OCGForge `PlayerObservation`.
I6 owns byte-exact cross-oracle and compatibility evidence.

## 11. Normative information-flow table

The default for an unlisted field is fail closed, not public.

| Category | `RAW_ONLY` | `PRIVATE_MIRROR_ALLOWED` | `PUBLIC_PROJECTION_ALLOWED` | `MODEL_ALLOWED_LATER` | `PUBLIC_AUDIT_ALLOWED_LATER` |
| --- | --- | --- | --- | --- | --- |
| card code | yes | only when perspective-known | only while visible/public or own-private by contract | only through projection | only if public/approved |
| zone | raw protocol form | yes | proven zone/count/slot | yes through projection | redacted semantic zone |
| sequence | raw protocol address | ephemeral mirror input | no raw sequence; canonical locator only | canonical locator only | canonical locator only |
| controller | raw protocol field | yes when proven | yes when public/proven | yes through projection | redacted public relation |
| owner | raw protocol field | yes when proven | yes when public/proven | yes through projection | redacted public relation |
| position | raw protocol field | yes when proven | yes when public/proven | yes through projection | public position only |
| LP | no | yes | yes | yes | yes |
| turn/phase | no | yes | yes when protocol-proven | yes | yes |
| chain index | raw message field | yes as mirror relation | only canonical public relation | yes through projection | public relation only |
| raw packet bytes | yes | no semantic use | no | no | private trace only |
| raw protocol card locator | yes | transient decode input | no | no | no |
| socket state | operational only | no semantic mirror | no | no | no |
| endpoint/port | operational only | no semantic mirror | no | no | no |
| room password | private configuration only | no | no | no | no |
| host flag | I2 public pre-duel fact | optional mirror provenance | only if a future projection explicitly owns it | only if contractually useful | redacted public fact |
| pre-duel position | I2 public pre-duel fact | provenance only | not a gameplay perspective selector | not a gameplay identity | optional redacted fact |
| TCP chunk boundary | transport diagnostic only | no | no | no | no |
| read count | transport diagnostic only | no | no | no | no |
| timestamp/wall clock | operational only | no | no | no | no |
| PID | operational only | no | no | no | no |
| thread/task ID | operational only | no | no | no | no |
| absolute path | build/operation provenance only | no | no | no | no |
| internal object identity | runtime only | no semantic identity | no | no | no |
| private response binding | private I4/I5 only | no | no | no | no |
| knowledge state | no raw form | yes | explicit known/unknown projection | yes through projection | redacted knowledge status |

Raw protocol diagnostics remain:

```text
TRAINING_ELIGIBILITY=NO
AUTHORITATIVE_MODEL_INPUT=NO
```

## 12. Paired-hidden-world contract

The future I3D acceptance harness must construct paired histories with equal
legitimate public knowledge and different hidden opponent information. It must
require byte-identical:

```text
PublicContractProjection
public semantic locator table
public state identity
```

Required fixture classes are:

| Fixture | Difference allowed | Required equality |
| --- | --- | --- |
| A | opponent hidden hand identities | projection, locators, public identity |
| B | opponent hidden deck order | projection, locators, public identity |
| C | a revealed card becomes hidden and continuity is destroyed | old public identity absent; resulting unknown projection equal |
| D | duplicate equal-code cards have different internal histories | distinct current public locators where entities are public; no history leak |
| E | same semantic stream with different TCP chunking | mirror, projection, locators, identity |

Internal values may differ only where they are legitimately perspective-known
and are excluded from the projection. Hidden opponent differences alone are not
legitimate private knowledge.

## 13. Deterministic identity hierarchy

These identities are separate domains:

```text
gameplay_semantic_state_id
public_projection_id
transport_protocol_provenance_id
```

`public_projection_id` is a versioned digest of the canonical public
projection. A future gameplay semantic identity may include the canonical
perspective mirror, but only fields allowed by the mirror contract. The
transport/provenance identity contains pins and execution facts and is never
used as gameplay identity.

Canonical order is fixed as:

1. schema/domain identifier;
2. participants `Self`, `Opponent`;
3. zones in the declared enum order;
4. slots and card/entity relationships by canonical numeric position;
5. card knowledge/property fields in declared field order;
6. chain/public relationships in protocol semantic order;
7. public locator table in locator creation order.

No raw packet buffer, unread count, TCP chunk, endpoint, password, PID, wall
clock, task/thread ID, path, address, mutable alias, or unordered iteration
participates in these identities.

## 14. Fail-closed GAME_MSG handling

I3 must return a stable typed failure for:

- unknown or malformed GAME_MSG;
- unsupported required message;
- message illegal for the current mirror state;
- impossible zone/controller/position transition;
- illegal duplicate semantic message;
- perspective-dependent message before `MSG_START`;
- unknown card locator/reference;
- knowledge-continuity ambiguity;
- semantic locator collision;
- state-capacity/resource failure.

I3 must not skip unknown messages, guess payload lengths, continue with partial
state, invent cards/entities, repair malformed relationships, or silently
discard a state-relevant message. Prompt messages are recognized only enough
to stop at the I4/I5 boundary; I3 does not answer them.

## 15. Message-support status vocabulary

The machine-readable inventory uses exactly these statuses:

| Status | Meaning in I3A0 |
| --- | --- |
| `REQUIRED` | Required for the planned minimum V1 mirror claim; each has an assigned I3 slice. It is not implemented by I3A0. |
| `OPTIONAL` | Useful public/knowledge behavior that may be added for a locked path; arrival before accepted support fails closed. |
| `UNSUPPORTED_FAIL_CLOSED` | Explicitly not accepted in I3 V1 or not semantically safe to pass through; arrival always fails closed. |
| `OUT_OF_SCOPE` | Owned by I4/I5 or a presentation/legacy boundary; I3 does not decode or mutate state and fails/forwards only at a future explicit boundary. |

The matrix is intentionally not a claim of complete EDOPro coverage. The
inventory includes every `MSG_*` identifier defined by the pinned core so that
an omitted message cannot be mistaken for supported coverage.

## 16. GAME_MSG support matrix

The full machine-readable matrix is
[`game-message-support.v1.json`](../../../fixtures/gameplay/v1/game-message-support.v1.json).
The following identifier/status ledger is normative for agreement checking;
entries are ordered by numeric message ID.

| ID | Symbol | Status | Planned slice |
| ---: | --- | --- | --- |
| 1 | `MSG_RETRY` | `UNSUPPORTED_FAIL_CLOSED` | I3A |
| 2 | `MSG_HINT` | `OPTIONAL` | I3B |
| 3 | `MSG_WAITING` | `OPTIONAL` | I3B |
| 4 | `MSG_START` | `REQUIRED` | I3A |
| 5 | `MSG_WIN` | `REQUIRED` | I3B |
| 6 | `MSG_UPDATE_DATA` | `REQUIRED` | I3B/I3C |
| 7 | `MSG_UPDATE_CARD` | `REQUIRED` | I3B/I3C |
| 8 | `MSG_REQUEST_DECK` | `UNSUPPORTED_FAIL_CLOSED` | I3A |
| 10 | `MSG_SELECT_BATTLECMD` | `OUT_OF_SCOPE` | I4 |
| 11 | `MSG_SELECT_IDLECMD` | `OUT_OF_SCOPE` | I4 |
| 12 | `MSG_SELECT_EFFECTYN` | `OUT_OF_SCOPE` | I4 |
| 13 | `MSG_SELECT_YESNO` | `OUT_OF_SCOPE` | I4 |
| 14 | `MSG_SELECT_OPTION` | `OUT_OF_SCOPE` | I4 |
| 15 | `MSG_SELECT_CARD` | `OUT_OF_SCOPE` | I5 |
| 16 | `MSG_SELECT_CHAIN` | `OUT_OF_SCOPE` | I4 |
| 18 | `MSG_SELECT_PLACE` | `OUT_OF_SCOPE` | I5 |
| 19 | `MSG_SELECT_POSITION` | `OUT_OF_SCOPE` | I4 |
| 20 | `MSG_SELECT_TRIBUTE` | `OUT_OF_SCOPE` | I5 |
| 21 | `MSG_SORT_CHAIN` | `OUT_OF_SCOPE` | I5 |
| 22 | `MSG_SELECT_COUNTER` | `OUT_OF_SCOPE` | I5 |
| 23 | `MSG_SELECT_SUM` | `OUT_OF_SCOPE` | I5 |
| 24 | `MSG_SELECT_DISFIELD` | `OUT_OF_SCOPE` | I5 |
| 25 | `MSG_SORT_CARD` | `OUT_OF_SCOPE` | I5 |
| 26 | `MSG_SELECT_UNSELECT_CARD` | `OUT_OF_SCOPE` | I5 |
| 30 | `MSG_CONFIRM_DECKTOP` | `OPTIONAL` | I3C |
| 31 | `MSG_CONFIRM_CARDS` | `OPTIONAL` | I3C |
| 32 | `MSG_SHUFFLE_DECK` | `REQUIRED` | I3C |
| 33 | `MSG_SHUFFLE_HAND` | `REQUIRED` | I3C |
| 34 | `MSG_REFRESH_DECK` | `OPTIONAL` | I3C |
| 35 | `MSG_SWAP_GRAVE_DECK` | `OPTIONAL` | I3B/I3C |
| 36 | `MSG_SHUFFLE_SET_CARD` | `REQUIRED` | I3C |
| 37 | `MSG_REVERSE_DECK` | `REQUIRED` | I3C |
| 38 | `MSG_DECK_TOP` | `OPTIONAL` | I3C |
| 39 | `MSG_SHUFFLE_EXTRA` | `REQUIRED` | I3C |
| 40 | `MSG_NEW_TURN` | `REQUIRED` | I3B |
| 41 | `MSG_NEW_PHASE` | `REQUIRED` | I3B |
| 42 | `MSG_CONFIRM_EXTRATOP` | `OPTIONAL` | I3C |
| 50 | `MSG_MOVE` | `REQUIRED` | I3B/I3C |
| 53 | `MSG_POS_CHANGE` | `REQUIRED` | I3B/I3C |
| 54 | `MSG_SET` | `REQUIRED` | I3B |
| 55 | `MSG_SWAP` | `REQUIRED` | I3B/I3C |
| 56 | `MSG_FIELD_DISABLED` | `OPTIONAL` | I3B |
| 60 | `MSG_SUMMONING` | `OPTIONAL` | I3B |
| 61 | `MSG_SUMMONED` | `OPTIONAL` | I3B |
| 62 | `MSG_SPSUMMONING` | `OPTIONAL` | I3B |
| 63 | `MSG_SPSUMMONED` | `OPTIONAL` | I3B |
| 64 | `MSG_FLIPSUMMONING` | `OPTIONAL` | I3B |
| 65 | `MSG_FLIPSUMMONED` | `OPTIONAL` | I3B |
| 70 | `MSG_CHAINING` | `REQUIRED` | I3B |
| 71 | `MSG_CHAINED` | `REQUIRED` | I3B |
| 72 | `MSG_CHAIN_SOLVING` | `REQUIRED` | I3B |
| 73 | `MSG_CHAIN_SOLVED` | `REQUIRED` | I3B |
| 74 | `MSG_CHAIN_END` | `REQUIRED` | I3B |
| 75 | `MSG_CHAIN_NEGATED` | `REQUIRED` | I3B |
| 76 | `MSG_CHAIN_DISABLED` | `REQUIRED` | I3B |
| 80 | `MSG_CARD_SELECTED` | `OPTIONAL` | I3B |
| 81 | `MSG_RANDOM_SELECTED` | `OPTIONAL` | I3C |
| 83 | `MSG_BECOME_TARGET` | `REQUIRED` | I3B |
| 90 | `MSG_DRAW` | `REQUIRED` | I3B/I3C |
| 91 | `MSG_DAMAGE` | `REQUIRED` | I3B |
| 92 | `MSG_RECOVER` | `REQUIRED` | I3B |
| 93 | `MSG_EQUIP` | `REQUIRED` | I3B/I3C |
| 94 | `MSG_LPUPDATE` | `REQUIRED` | I3B |
| 95 | `MSG_UNEQUIP` | `REQUIRED` | I3B/I3C |
| 96 | `MSG_CARD_TARGET` | `REQUIRED` | I3B |
| 97 | `MSG_CANCEL_TARGET` | `REQUIRED` | I3B |
| 100 | `MSG_PAY_LPCOST` | `REQUIRED` | I3B |
| 101 | `MSG_ADD_COUNTER` | `OPTIONAL` | I3B |
| 102 | `MSG_REMOVE_COUNTER` | `OPTIONAL` | I3B |
| 110 | `MSG_ATTACK` | `OPTIONAL` | I3B |
| 111 | `MSG_BATTLE` | `OPTIONAL` | I3B |
| 112 | `MSG_ATTACK_DISABLED` | `OPTIONAL` | I3B |
| 113 | `MSG_DAMAGE_STEP_START` | `OPTIONAL` | I3B |
| 114 | `MSG_DAMAGE_STEP_END` | `OPTIONAL` | I3B |
| 120 | `MSG_MISSED_EFFECT` | `OPTIONAL` | I3B |
| 121 | `MSG_BE_CHAIN_TARGET` | `OPTIONAL` | I3B |
| 122 | `MSG_CREATE_RELATION` | `OPTIONAL` | I3B |
| 123 | `MSG_RELEASE_RELATION` | `OPTIONAL` | I3B |
| 130 | `MSG_TOSS_COIN` | `OPTIONAL` | I3B |
| 131 | `MSG_TOSS_DICE` | `OPTIONAL` | I3B |
| 132 | `MSG_ROCK_PAPER_SCISSORS` | `UNSUPPORTED_FAIL_CLOSED` | I3A |
| 133 | `MSG_HAND_RES` | `UNSUPPORTED_FAIL_CLOSED` | I3A |
| 140 | `MSG_ANNOUNCE_RACE` | `OUT_OF_SCOPE` | I5 |
| 141 | `MSG_ANNOUNCE_ATTRIB` | `OUT_OF_SCOPE` | I5 |
| 142 | `MSG_ANNOUNCE_CARD` | `UNSUPPORTED_FAIL_CLOSED` | I3A |
| 143 | `MSG_ANNOUNCE_NUMBER` | `OUT_OF_SCOPE` | I5 |
| 160 | `MSG_CARD_HINT` | `OPTIONAL` | I3C |
| 161 | `MSG_TAG_SWAP` | `UNSUPPORTED_FAIL_CLOSED` | I3A |
| 162 | `MSG_RELOAD_FIELD` | `OPTIONAL` | I3B/I3C |
| 163 | `MSG_AI_NAME` | `OUT_OF_SCOPE` | UI_ONLY |
| 164 | `MSG_SHOW_HINT` | `OUT_OF_SCOPE` | UI_ONLY |
| 165 | `MSG_PLAYER_HINT` | `OPTIONAL` | I3B |
| 170 | `MSG_MATCH_KILL` | `UNSUPPORTED_FAIL_CLOSED` | I3A |
| 180 | `MSG_CUSTOM_MSG` | `UNSUPPORTED_FAIL_CLOSED` | I3A |
| 190 | `MSG_REMOVE_CARDS` | `REQUIRED` | I3C |

For every `REQUIRED` or `OPTIONAL` entry, the future owner must freeze the
exact modern field layout and strict length/count behavior before implementing
it. An entry marked `OUT_OF_SCOPE` or `UNSUPPORTED_FAIL_CLOSED` is never
silently skipped.

## 17. Ordered I3 implementation slices

The following are separate future tasks. I3A0 does not authorize any of them.

### I3A — GAME_MSG foundation and perspective establishment

Owner: I3 gameplay decoder boundary.

Inputs:

- claimed `GameplayTransportHandoffV1`;
- exact pending bytes first, then live transport bytes;
- accepted I1 `StocGameMessagePayload` bytes;
- modern V1 core pin and this support inventory.

Outputs:

- strict validated message values for the I3A-supported subset;
- exactly one immutable `GameplayPerspectiveV1` established by valid
  `MSG_START`;
- typed fail-closed result for unknown/malformed/pre-perspective messages;
- no public state mirror beyond the perspective seed.

Required negative tests include duplicate/conflicting `MSG_START`, observer
playertype, invalid lengths, legacy-extra-byte forms, perspective-dependent
messages before `MSG_START`, unknown IDs, trailing bytes, pending-byte-first
processing, one-time handoff claim, and transport close-on-failure.

Non-goals: card-zone mirror, prompt decoding, candidate generation, privacy
projection, model input, and response selection.

Stop condition: any unsupported or ambiguous state-relevant message fails the
I3A session; no fallback parser or legacy mode is attempted.

### I3B — deterministic PerspectiveStateMirror

Owner: I3 state mirror.

Inputs: I3A validated messages and immutable perspective; no raw transport
address or external engine query.

Outputs: canonical participant/zone/LP/turn/phase/field/chain/public-relation
mirror values with explicit unknowns and deterministic state transitions.

Initial message families: `MSG_START`, `MSG_UPDATE_DATA`, `MSG_UPDATE_CARD`,
`MSG_MOVE`, `MSG_POS_CHANGE`, `MSG_SET`, `MSG_SWAP`, `MSG_NEW_TURN`,
`MSG_NEW_PHASE`, LP families, required chain families, and required public
target/equipment families listed in the inventory.

Required negative tests include wrong perspective role, impossible source or
destination, unknown locator, duplicate semantic transitions, incomplete query
records, count overflow, zone capacity failure, and chunking metamorphisms.

Non-goals: hidden-card continuity policy beyond the state hooks, prompt answer,
candidate construction, model input, and legality recomputation.

Stop condition: the mirror is not published when a required transition is
unproven or ambiguous.

### I3C — card knowledge and semantic locators

Owner: I3 knowledge/identity subsystem.

Inputs: I3B mirror transitions, visible query/reveal facts, and knowledge-boundary
messages such as shuffle, reverse, remove, and confirm families.

Outputs: explicit card knowledge union, lifecycle-safe mirror locators, and
canonical invalidation/replacement events.

Required negative tests include hidden opponent identity injection, stale
identity after every shuffle family, ambiguous `MSG_MOVE`/`MSG_SHUFFLE_SET_CARD`,
duplicate equal-code cards, locator collision, inferred deck order, and
knowledge equality across paired hidden worlds.

Non-goals: probability, archetype inference, hidden-deck reconstruction,
prompt projection, model input, and response binding.

Stop condition: any identity continuity that is not independently proven is
destroyed and represented as unknown; no best-effort rebind is allowed.

### I3D — PublicContractProjection and privacy acceptance

Owner: I3 public projection/identity boundary.

Inputs: only the accepted perspective-safe mirror and canonical locator table.

Outputs: versioned public projection bytes/value objects, public locator table,
and separate public projection identity.

Required negative tests include all information-flow rows marked non-public,
paired hidden worlds A–E, raw metadata scan, projection mutation/aliasing,
unordered iteration, process restart, and canonical identity comparison.

Non-goals: OCGForge byte compatibility, model scoring, candidate selection,
training admission, public audit implementation, and IPC.

Stop condition: any hidden, private-control, raw-address, or non-deterministic
field prevents projection publication and returns a typed privacy failure.

## 18. Frozen I3 acceptance gates

The numbered I3 gate set is frozen for later implementation. All gates are
`NOT_RUN` after this documentation-only I3A0 task:

```text
I3-G00  accepted I0/I1/I2 boundaries unchanged = NOT_RUN
I3-G01  I2 handoff consumed exactly once = NOT_RUN
I3-G02  pending bytes processed exactly before new reads = NOT_RUN
I3-G03  MSG_START establishes final perspective exactly = NOT_RUN
I3-G04  perspective does not depend on lobby inference = NOT_RUN
I3-G05  supported GAME_MSG decoding is deterministic under chunking = NOT_RUN
I3-G06  unknown/malformed state-relevant messages fail closed = NOT_RUN
I3-G07  state transition legality/invariants hold = NOT_RUN
I3-G08  hidden opponent identities do not enter public projection = NOT_RUN
I3-G09  knowledge destruction removes stale hidden identity continuity = NOT_RUN
I3-G10  semantic locators are deterministic and perspective-safe = NOT_RUN
I3-G11  duplicate card codes remain distinct when distinct entities are public = NOT_RUN
I3-G12  paired-hidden-world public byte equality holds = NOT_RUN
I3-G13  public state identity is deterministic = NOT_RUN
I3-G14  raw packet/control metadata is absent from public identity = NOT_RUN
I3-G15  no prompt answer/candidate generation exists in I3 = NOT_RUN
I3-G16  no gameplay legality recomputation exists = NOT_RUN
I3-G17  I1 regression passes = NOT_RUN
I3-G18  I2 regression passes = NOT_RUN
I3-G19  fresh-process deterministic fixture output passes = NOT_RUN
I3-G20  provenance is complete for every supported message family = NOT_RUN
I3-G21  no public-server use exists = NOT_RUN
I3-G22  no I4/I5/model/IPC implementation exists = NOT_RUN
```

I3A0 itself additionally requires exact-base verification, JSON parse/order and
ID agreement, relative-link validation, `git diff --check`, no production
`.cs` gameplay implementation, no copied upstream source, no public endpoint,
and no behavior change.

## 19. Provenance boundary

The exact source facts used for this freeze are recorded in
`PROTOCOL_PROVENANCE.md`. They come from the pinned EDOPro commit
`30935e847165a9ef0e547fb51a43f36168fab7c7` and its exact ocgcore gitlink
`46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57`. WindBot remains an independent
reference only at `bffe6b62679c8b2fafea8f59740e03a132517da4`.

No source implementation, parser control flow, or serialized upstream fixture
is copied. The inventory is an independently authored contract index, not an
upstream source extract.

## 20. Explicit non-goals

I3A0 does not authorize or implement:

- a GAME_MSG decoder;
- a gameplay state mirror;
- prompt answering, flat candidates, or continuations;
- `ANNOUNCE_CARD` support;
- model input, model scoring, checkpoint binding, or IPC;
- EDOPro compatibility/cross-oracle work;
- WPF/UI, installer, or public-server automation;
- Match/Tag/Relay/observer/reconnect expansion;
- training-data admission or audit trace persistence.

I3A0 ends after its documentation commit, push, PR creation, and stop for
independent review. I3A production remains unauthorized.
