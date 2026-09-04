# Public State Projection V1

Status: I3C implementation contract
Contract ID: `ocgforge-ignis.public-state-projection.v1`

## Boundary and meaning

`public` means safe to cross the model-facing boundary for the established
player perspective. It does not mean globally public to both duel
participants. Legitimate perspective-private knowledge, such as the
established player's own hidden card identity, may therefore be included.
Unknown opponent hidden identity may not be included.

This contract is an adapter-native reduction of the accepted
`PerspectiveStateMirrorV1`. It is value-owned and immutable. It is not a
second mirror, a rules engine, a protocol decoder, a prompt/candidate model,
or a response binding.

These identities remain separate:

```text
PerspectiveStateMirrorV1 != PublicStateSnapshotV1
PublicStateSnapshotV1 != OCGForge PlayerObservation
I3C DOES NOT CLAIM I6 CROSS-ORACLE ACCEPTANCE.
```

I3C does not implement or claim model input readiness, OCGForge contract
compatibility, or cross-oracle acceptance.

## Projection boundary

The internal projection boundary is:

```text
MirrorSnapshotV1 + PublicStateProjectionContextV1
    -> PublicStateProjectionResultV1
```

The projector is internal to the Gameplay assembly. Public result, snapshot,
participant, card, error, and context types expose no `Mirror*`, protocol,
query-payload, private-response, or control-plane type.

The result is either a complete authoritative `PublicStateSnapshotV1` with
canonical bytes and a lowercase SHA-256 digest, or a structured failure with
no snapshot and no canonical bytes. A partial snapshot is never authoritative.

The projection does not mutate the mirror or create persistent mirror
identity. It does not project chains, relations, events, query payloads, or
private response bytes.

## Snapshot schema

`PublicStateSnapshotV1` contains these fields in this semantic order:

```text
contract_id
perspective_player
duel_flags
turn_count
turn_player?
phase?
terminal
participants
cards
```

`perspective_player` and every participant/card player are absolute `p0` or
`p1` values. The established I3C0 mapping is used exactly:

| Perspective | Role | Absolute player |
| --- | --- | ---: |
| `SelfIsPlayer0` | `Self` | 0 |
| `SelfIsPlayer0` | `Opponent` | 1 |
| `SelfIsPlayer1` | `Self` | 1 |
| `SelfIsPlayer1` | `Opponent` | 0 |

Participants are always emitted in `p0`, then `p1` order. Each participant
contains only:

```text
absolute_player
life_points
main_deck_count
hand_count
extra_deck_count
```

All five values are copied only from known mirror facts. If a required value
is not known, projection fails closed with `UNPROVEN_KNOWLEDGE` (or an
equivalent structured error) and emits no snapshot.

`turn_player` is an absolute player or `null`; `phase` is the proven phase or
`null`; `terminal` is the mirror's terminal boolean. No local turn or phase
legality is inferred.

## Card knowledge and locator rules

Each emitted card contains:

```text
locator
absolute_player
zone
card_code?
position?
```

`card_code` is emitted only when the mirror's code is known with one of the
accepted proven classifications: `PublicProtocolFact`,
`PerspectivePrivateFact`, or `DerivedFromProvenPublicFacts`. An
`UnknownRedacted` value never supplies a code and is never reconstructed from
another field, a deck manifest, a duplicate, a cache, a prior identity, or
inference.

Main Deck contributes its known count only. It never contributes a per-card
entry or a `MAIN_DECK` locator. Unknown Hand and Extra Deck cards contribute
their population counts only; they never produce an `unknown` locator.

Known Hand and Extra Deck cards use the accepted I3C0 public-ordinal forms:

```text
p<player>:HAND:public:<card_code>:<ordinal>
p<player>:EXTRA_DECK:public:<card_code>:<ordinal>
```

For each absolute player, zone, and known code group, ordinal values are
exactly `0..N-1`. The group is formed from semantic multiplicity and duplicate
records are ordered only by their public position value, with unknown position
before known position. Mirror identity, allocation order, collection order,
and physical hidden-card continuity are not used.

Visible indexed locations use the accepted I3C0 forms for
`MONSTER_ZONE`, `SPELL_TRAP_ZONE`, `FIELD_ZONE`,
`PENDULUM_RELEVANT_STATE`, `GRAVEYARD`, and `BANISHED`. An occupied visible
slot may have a `null` code when its physical card identity is unknown.

Overlay cards use only:

```text
p<player>:OVERLAY:<parent_sequence>:<overlay_sequence>
```

Both values are consumed from the mirror's proven semantic positional facts.
`MirrorEntityIdV1`, overlay relations, and relation ordinals are never locator
fallbacks. If a proven overlay locator cannot be created, projection fails
closed with `UNPROVEN_LOCATOR`.

## Pinned SZONE layout mapping

The mapping is based on the pinned `edo9300/ygopro-core` commit
`46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57` and its exact EDOPro target. The
context carries the complete `DuelFlags` value. The relevant pinned constants
are:

```text
DUEL_PZONE          = 0x800
DUEL_SEPARATE_PZONE = 0x1000
DUEL_3_COLUMNS_FIELD = 0x400000
```

For a mirror `SpellTrapZone` card, physical SZONE sequence 5 is always
`FIELD_ZONE`. It is never classified as an ordinary Spell/Trap slot.

The proven physical slot mapping is:

| Layout flags | Ordinary Spell/Trap slots | Pendulum slots | Field slot |
| --- | --- | --- | --- |
| no `PZONE`, no `3_COLUMNS_FIELD` | 0..4 | none | 5 |
| no `PZONE`, with `3_COLUMNS_FIELD` | 1..3 | none | 5 |
| `PZONE` + `SEPARATE_PZONE`, no `3_COLUMNS_FIELD` | 0..4 | 6..7 | 5 |
| `PZONE` + `SEPARATE_PZONE` + `3_COLUMNS_FIELD` | 1..3 | 6..7 | 5 |
| `PZONE`, no `SEPARATE_PZONE`, no `3_COLUMNS_FIELD` | 1..3 plus shared 0/4 | shared 0/4 | 5 |
| `PZONE` + `3_COLUMNS_FIELD`, no `SEPARATE_PZONE` | shared 1/3 plus ordinary 2 | shared 1/3 | 5 |

In a shared PZONE layout, the wire/mirror SZONE address does not itself carry
the internal `pzone` boolean. A shared endpoint is therefore classified only
when the mirror contains a known `Type` query value proving the pinned
PZONE-form type (`TYPE_PENDULUM | TYPE_SPELL`, without `TYPE_MONSTER`). A
missing, redacted, or malformed proof is not guessed: the projection fails
closed with `UNSUPPORTED_LAYOUT` or `UNPROVEN_KNOWLEDGE`. Contradictory flags
such as `SEPARATE_PZONE` without `PZONE`, and out-of-layout sequences, also
fail closed.

No numeric flag is inferred from OCGForge runtime code, a card code, an
archetype, a decklist, or a plausible approximation.

## Canonical bytes and digest

Canonical bytes are compact UTF-8 JSON with no BOM, no whitespace, no trailing
newline, and the fixed property/array order above. All strings use ordinal
semantic values; all numbers are ASCII decimal; booleans are lowercase
`true`/`false`; unknown optional values are JSON `null`.

Participant property order is:

```text
absolute_player
life_points
main_deck_count
hand_count
extra_deck_count
```

Card property order is:

```text
locator
absolute_player
zone
card_code
position
```

Cards are sorted by `PublicSemanticLocatorV1.Value` with ordinal comparison.
The SHA-256 digest is computed over exactly those canonical bytes and is
reported as lowercase hexadecimal. It is an I3C public-state semantic digest
only; it is not `GetHashCode`, build provenance, process identity, replay
identity, or I6 cross-oracle acceptance.

## Fail-closed error categories

The result distinguishes at least these categories:

```text
INVALID_SNAPSHOT
UNSUPPORTED_LAYOUT
UNPROVEN_KNOWLEDGE
UNPROVEN_LOCATOR
CANONICALIZATION_FAILURE
```

Every category returns no authoritative snapshot. No fallback, partial state,
hidden identity reconstruction, relation fallback, allocation-order fallback,
or protocol dump is permitted.

## Privacy and determinism invariants

The canonical bytes and digest contain no raw protocol frame/query payload,
mirror identity, protocol address object, allocation ordinal, relation
ordinal, response binding, host/port/password, socket/PID/process handle,
timestamp, thread/task identity, filesystem path, receive-buffer detail, or
TCP chunk detail. They depend only on proven mirror facts, explicit pinned
semantic layout context, and the fixed canonical encoding.

Required I3C claims remain:

```text
HIDDEN_OPPONENT_IDENTITY_LEAK=NO
RAW_PROTOCOL_STATE_EXPOSED=NO
MIRROR_ENTITY_ID_EXPOSED=NO
STABLE_HIDDEN_LOCATOR_CREATED=NO
PRIVATE_RESPONSE_BINDING_EXPOSED=NO
CONTROL_PLANE_METADATA_EXPOSED=NO
INFERRED_HIDDEN_STATE=NO
MODEL_INPUT_READY=NO
I6_CROSS_ORACLE_ACCEPTED=NO
```
