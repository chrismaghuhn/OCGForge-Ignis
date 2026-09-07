# I6C6 Native OCGForge Oracle Comparison Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Compare the accepted Ignis I6C5 perspective-safe frame with the native OCGForge public-safe-state authority for a bounded, deterministic, privacy-safe corpus without moving I6D or I7 authority into I6C6.

**Architecture:** OCGForge remains the semantic and canonical-byte owner. A later I6C6 test boundary will build a native `PlayerObservation` through OCGForge's `CoreHost`/observation builder, replay an explicitly paired supported transcript into Ignis, normalize both values into a test-only typed comparison model, and use the native `ocgforge.public_safe_state.v1` serializer as the canonical-byte oracle. No production Ignis serializer or second gameplay authority is introduced.

**Tech Stack:** OCGForge C++ observation/oracle tests at semantic commit `f929de0b4d4157327dba003067d2e21e42f7ad75`, pinned rules bundle/core provenance, OCGForge-Ignis .NET 10 Gameplay source, deterministic test fixtures, and fresh-process comparison harnesses.

---

## 1. Current audit result

Fixed points:

```text
IGNIS_MAIN=28f67dfdae622d16b3b040e74e8f7b78e4914661
IGNIS_TREE=687962863205441330487a2dcdb8b7a60364a8cd
OCGFORGE_SEMANTIC_COMMIT=f929de0b4d4157327dba003067d2e21e42f7ad75
OCGFORGE_RULES_BUNDLE=3adfe6b4cfe2c2805e50b389fc0eb4e70a3b0b6107436614d328fddc865e585f
OCGFORGE_CORE=9a0c558c2d686542f7914a6d529fd7aa57746aed
IGNIS_EDOPRO=30935e847165a9ef0e547fb51a43f36168fab7c7
IGNIS_EDOPRO_CORE=46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57
```

```text
I6C6_SOURCE_AUDIT=PASS
OCGFORGE_NATIVE_ORACLE_PATH=PROVEN
IGNIS_I6C5_SOURCE_PATH=PROVEN
FIELD_MAPPING_COMPLETE=YES
PRIVACY_MODEL_COMPLETE=YES
DETERMINISM_MODEL_COMPLETE=YES
IMPLEMENTATION_PLAN_COMPLETE=YES
I6C6_DESIGN_FINAL=NO
I6C6_IMPLEMENTATION_AUTHORIZED=NO
```

The shape and field mapping are sufficiently identified for implementation planning. The two bridge contracts are now closed at the design level, but their future execution evidence is not yet present:

1. `SAME_SCENARIO_REPLAY_EVIDENCE=UNPROVEN`: the later implementation still needs an artifact binding one native `CoreHost` scenario to the same perspective-safe public-event transcript and selected mirror/oracle boundary, including relevant CardScripts behavior provenance.
2. `PRINTED_SOURCE_EVIDENCE=UNPROVEN`: I6C5 consumes an external provider artifact; no real OCGForge static-card rows may be copied into Ignis, and the later implementation still needs to supply the exact native artifact externally.

These are implementation-evidence obligations, not permission to infer equality.

## 2. Native OCGForge authority path

The exact native observation authority is:

```text
CoreHost::query_field()
CoreHost::query_location()
CoreHost::query()
CoreHost::static_card_data(passcode)
        ↓
ygo::observation::build_player_observation(...)
        ↓
ygo::environment::canonical_public_safe_state_bytes(PlayerObservation)
```

Primary source paths at the accepted semantic commit:

- [`include/ygo/observation/player_observation.hpp`](https://github.com/chrismaghuhn/OCGForge/blob/f929de0b4d4157327dba003067d2e21e42f7ad75/include/ygo/observation/player_observation.hpp) defines `PlayerObservation`, including globals, zones, entities, relationships, chain, visible events, decision context, and match context.
- [`include/ygo/observation/observation_builder.hpp`](https://github.com/chrismaghuhn/OCGForge/blob/f929de0b4d4157327dba003067d2e21e42f7ad75/include/ygo/observation/observation_builder.hpp) and [`src/observation/observation_builder.cpp`](https://github.com/chrismaghuhn/OCGForge/blob/f929de0b4d4157327dba003067d2e21e42f7ad75/src/observation/observation_builder.cpp) own query acquisition, visibility, entity retention, locator creation, relation resolution, chain projection, and match-context attachment.
- [`src/observation/zone_projection.cpp`](https://github.com/chrismaghuhn/OCGForge/blob/f929de0b4d4157327dba003067d2e21e42f7ad75/src/observation/zone_projection.cpp) owns engine-location/sequence/duel-flag to semantic-zone projection.
- [`src/observation/card_projection.cpp`](https://github.com/chrismaghuhn/OCGForge/blob/f929de0b4d4157327dba003067d2e21e42f7ad75/src/observation/card_projection.cpp) owns Printed and Current property projection, position mapping, identity visibility, Link markers, Xyz rank, and Pendulum scales.
- [`src/observation/event_projection.cpp`](https://github.com/chrismaghuhn/OCGForge/blob/f929de0b4d4157327dba003067d2e21e42f7ad75/src/observation/event_projection.cpp) owns framed-message to visible-event projection. `engine_step_index` is internal metadata and is not emitted by the public-safe-state bytes.
- [`include/ygo/environment/public_safe_state.hpp`](https://github.com/chrismaghuhn/OCGForge/blob/f929de0b4d4157327dba003067d2e21e42f7ad75/include/ygo/environment/public_safe_state.hpp) and [`src/environment/public_safe_state.cpp`](https://github.com/chrismaghuhn/OCGForge/blob/f929de0b4d4157327dba003067d2e21e42f7ad75/src/environment/public_safe_state.cpp) own `ocgforge.public_safe_state.v1` validation, decoding, and canonical bytes.
- [`include/ygo/environment/public_environment_observation.hpp`](https://github.com/chrismaghuhn/OCGForge/blob/f929de0b4d4157327dba003067d2e21e42f7ad75/include/ygo/environment/public_environment_observation.hpp) and [`src/environment/public_environment_observation.cpp`](https://github.com/chrismaghuhn/OCGForge/blob/f929de0b4d4157327dba003067d2e21e42f7ad75/src/environment/public_environment_observation.cpp) own the outer decision-boundary projection. That layer includes `decision_index` and public decision context and is outside I6C6.

The native public-safe-state schema is:

```text
ocgforge.public_safe_state.v1
```

Its canonical safe-state fields are exactly:

```text
globals
zones
entities
relationships
chain
visible_events
match_context
```

It deliberately excludes top-level `engine_step_index`, internal decision IDs, continuation IDs, and `PlayerObservation.observation_hash`.

## 3. Ignis I6C5 source path

The exact Ignis path is:

```text
GameplayMessageDecoderV1
        ↓
PerspectiveStateMirrorV1.Clone → Apply → Validate → Commit
        ├─ MirrorSnapshotV1
        └─ PerspectiveSafeEventLedgerV1
                ↓
PerspectiveSafePublicFrameSourceV1.TryCreateI6C5
        ├─ PerspectiveSafeMatchContextV1
        ├─ PerspectiveSafePrintedProviderV1
        ├─ I6C2 globals/zones/entities
        ├─ I6C3 relationships/chain
        └─ I6C4 visible events
                ↓
PerspectiveSafeFrameV1
```

Primary local paths:

- `src/OCGForge.Ignis.Gameplay/PerspectiveStateMirrorV1.cs`
- `src/OCGForge.Ignis.Gameplay/PerspectiveSafeEventLedgerV1.cs`
- `src/OCGForge.Ignis.Gameplay/PerspectiveSafePublicFrameSourceV1.cs`
- `src/OCGForge.Ignis.Gameplay/PerspectiveSafeFrameSourceTypesV1.cs`
- `src/OCGForge.Ignis.Gameplay/PerspectiveSafeMatchContextV1.cs`
- `src/OCGForge.Ignis.Gameplay/PerspectiveSafePrintedProviderV1.cs`
- `src/OCGForge.Ignis.Gameplay/GameplayMirrorSessionV1.cs`

Ignis intentionally does not own OCGForge canonical bytes. I6C6 must therefore keep any native serializer invocation in the oracle/test boundary.

## 4. Field-by-field comparison matrix

| Semantic field | Native authority | Ignis I6C5 source | I6C6 rule |
| --- | --- | --- | --- |
| Perspective | `PlayerObservation.perspective_player`; also `MatchContext.perspective_player` | `PerspectiveSafeMatchContextV1.PerspectivePlayer`; absolute player values in frame | Exact equality; mismatch fails closed |
| Duel flags | `ObservedPlayerGlobals.duel_flags`; `MatchContext.duel_flags` | Explicit immutable match context and frame globals | Exact equality |
| Life points | `ObservedPlayerGlobals.life_points` | `PerspectiveSafeGlobalsV1.LifePoints` | Exact ordered vector equality |
| `player_to_act` | Native safe-state globals | I6C5 always represents it as absent | Only compare state-only vectors where native value is absent; present means `BLOCKED_PENDING_I6D` |
| Turn/phase/terminal | Native globals | Ignis globals | Exact optional-presence and value equality |
| Zones | `ObservedZone` from `add_zone_counts` | `PerspectiveSafeZoneV1` from I6C2/I6C5 | Exact tuple values after the frozen enum mapping and canonical zone sort |
| Entity locator | Native `locator_for` / public ordinal creation | Ignis public semantic locator creation | Exact string equality; no MirrorEntityId comparison |
| Entity identity | Native visibility predicate plus query code | Ignis provenance/visibility predicate | Exact known/absent and passcode equality; hidden identity must be absent |
| Entity owner/controller | Native query owner fallback and controller | Ignis query-derived owner/controller | Exact when owner is explicitly proven; fallback-only owner cases fail closed rather than being guessed |
| Entity zone/sequence | Native `project_zone` and sequence visibility | Ignis I6C2/I6C3 projection | Exact values after enum mapping; Extra public ordinals must match |
| Position/face flags | Native `card_projection.cpp` | Ignis frame entity position/flags | Exact enum and boolean equality |
| Printed properties | Native `static_card_data` → `card_projection.cpp` | External immutable Printed provider → frame | Exact field presence/value equality; requires a provenance-bound test artifact bridge |
| Current properties | Native raw query → `set_current_card_properties` | Ignis query fields → current properties | Exact optional presence/value equality; Link/XYZ/Pendulum normalization is explicit |
| Relationships | Native pending relation resolution | Ignis I6C3 relation projection | Exact after sort by `(kind, source, target)` |
| Chain | Native `field.chain` and observation builder | Ignis I6C3 chain projection | Exact link order/index and optional fields; target locators sorted |
| Visible events | Native `project_visible_events` | I6C4 ledger | Exact public event fields and `event_index`; omit native internal `engine_step_index` |
| Match context | Native configured `MatchContext` | Immutable I6C5 match context | Exact semantic fields; decks compare as sorted composition because native safe-state canonicalization sorts them |
| Schema identity | Native `ocgforge.public_safe_state.v1` | Not embedded in `PerspectiveSafeFrameV1` | Comparator pins the native schema externally; do not add an Ignis schema field |
| Decision metadata | Native outer environment layer | Not owned by I6C5 | Outside I6C6; I6D owns it |

## 5. Normalization contract

The later comparator must normalize only representation, never meaning:

1. Use the declaration-order enum mappings frozen by the native public-safe-state contract.
2. Sort zones by `(player, kind, total_count, public_identity_count, hidden_count, player_observable_order)`.
3. Sort entities by locator string using ordinal comparison.
4. Sort relationships by `(kind, source, target)`.
5. Preserve chain-link order by native chain index; sort each target vector lexicographically.
6. Sort visible events by `event_index`; sort each target vector lexicographically.
7. Sort Link markers by native enum code and counters by `(type, count)`.
8. Sort known deck passcodes for the comparison representation; do not reinterpret current Deck order.
9. Preserve optional presence exactly: `PRESENT(0)` is not `ABSENT`.
10. Omit `engine_step_index`, top-level observation metadata, decision context, continuation identity, and observation hashes.
11. Reject unknown enums, duplicate locators, duplicate event indices, hidden identity with properties, and any field that cannot be mapped without inference.

The comparator must not sort source histories or chain links that are semantically ordered. Only the native contract's canonical sort rules may reorder a collection.

## 6. Native canonical-byte strategy

The native serializer remains the byte authority:

```text
ocgforge.public_safe_state.v1
    → canonical_public_safe_state_bytes(PlayerObservation)
```

The later I6C6 implementation must not copy this codec into production Ignis. The preferred test architecture is:

```text
native CoreHost scenario → native PlayerObservation → native safe-state bytes
Ignis I6C5 frame → test-only typed adapter → native oracle-side PlayerObservation/value view
                                             ↓
                              native safe-state serializer
```

If the native adapter cannot accept the Ignis-side normalized values without adding a second semantic authority, I6C6 must compare the typed matrix only and report `SEMANTIC_COMPARISON_DIGEST=UNPROVEN`; it must not invent a parallel gameplay codec.

## 7. Privacy model

The test setup may use CoreHost/EDOPro authority to construct a hidden world, but the compared values and diagnostics are perspective-safe only.

Required paired worlds:

```text
World A and World B differ only in opponent hidden Hand/Deck/face-down Extra identity
native public-safe state(P,A) == native public-safe state(P,B)
Ignis frame(P,A) == Ignis frame(P,B)
comparison result(A) == comparison result(B)
```

The harness must not pass hidden passcodes into the Ignis comparison adapter, provider lookup, diagnostics, or normalized fixture. Unknown/redacted identity must never trigger Printed lookup. Mismatch diagnostics may contain public locator/field paths and public-safe digests, but never hidden passcodes, raw engine pointers, raw omniscient queries, or private continuation IDs.

## 8. Determinism and replay model

Each scenario manifest must bind:

```text
scenario_id
perspective_player
duel_flags
seed words
rules_bundle_id
OCGForge semantic commit
ocgforge_core_commit
ocgforge_core_patchset_id
ocgforge_core_patchset_sha256
ocgforge_cardscripts_commit
ignis_cardscripts_commit
ocgforge_babelcdb_commit
ignis_babelcdb_commit
EDOPro/runtime provenance
explicit fixture/deck configuration
native setup descriptor ID
Ignis replay transcript
Printed-provider semantic identity and coverage digest
```

The semantic comparison digest, if implemented, must cover only the normalized public-safe state. It must exclude paths, timestamps, process IDs, branch names, checkout locations, object IDs, and source/build provenance. Two fresh processes must produce identical normalized bytes, digest, and pass/fail classification.

## 9. Cross-runtime differences

| Difference | Classification | I6C6 consequence |
| --- | --- | --- |
| OCGForge core `9a0c558…` vs EDOPro core `46779fbe…` | Expected provenance difference | Never compare internal state; require behavioral public-frame evidence |
| OCGForge API-hardening patchset | Requires source/provenance binding | Native oracle manifest must include the patchset; unsupported API differences fail closed |
| CardScripts provenance (`ocgforge=f337c870…`, `ignis=00a828b7…`) | Requires scenario behavior binding | Different scripts may change legal transitions, queries, or emitted messages; every I6C6 V1 scenario requires `CARD_SCRIPT_BEHAVIOR_BINDING=PASS` |
| BabelCDB/database provenance (`ocgforge=89ad6837…`, `ignis=2142b4b4…`) | Requires Printed source bridge | Exact Printed values require an externally bound provider artifact and semantic-row/coverage evidence; no database data is copied into Ignis |
| OCGForge query-location builder vs EDOPro wire/mirror | Requires scenario corpus | Compare resulting public semantics, not query payload shape |
| Native event projector vs Ignis ledger | Requires event corpus | Same accepted message subset and event-index rules must be proven; unsupported families remain out of corpus |
| Native `player_to_act`/decision context | Outside I6C6 | State-only vectors require absent `player_to_act`; I6D owns decision-boundary values |

CardScripts and BabelCDB are independent provenance dimensions. A matching
Printed artifact does not prove matching script behavior, and matching
CardScripts behavior does not prove matching Printed rows. The scenario
manifest must carry both dimensions and the comparator must fail closed when a
script-dependent transition is not behaviorally bound.

## 10. Normative bridge contracts

### 10.1 `SameScenarioReplayBridgeV1`

This is a provenance/evidence contract, not a public-frame field. Its canonical
manifest domain is:

```text
OCGFORGE-IGNIS-I6C6-SAME-SCENARIO-REPLAY-V1\0
```

The manifest contains the following fields in this fixed order, encoded with
the existing I6 identity convention of explicit fixed-width integers, ordered
vectors, and length-prefixed UTF-8 strings:

```text
scenario_contract_id
scenario_id
perspective_player
starting_player
duel_flags
seed_words[]
seat0_deck_id
seat0_deck_sha256
seat1_deck_id
seat1_deck_sha256
ocgforge_semantic_commit
rules_bundle_id
ocgforge_core_commit
ocgforge_core_patchset_id
ocgforge_core_patchset_sha256
ocgforge_cardscripts_commit
ignis_edopro_commit
ignis_edopro_core_commit
ignis_cardscripts_commit
native_setup_descriptor_id
native_raw_transcript_sha256
ignis_raw_replay_transcript_sha256
native_public_event_transcript_sha256
ignis_public_event_transcript_sha256
supported_message_family_envelope_id
comparison_boundary.kind
comparison_boundary.public_event_prefix_count
comparison_boundary.state_snapshot_selector
```

The field types and byte encoding are normative:

```text
scenario_contract_id: string
scenario_id: string
perspective_player: u8, constrained to {0,1}
starting_player: u8, constrained to {0,1}
duel_flags: u64be
seed_words: exactly u32be count 4, followed by exactly four ordered u64be words
seat0_deck_id: exact nonempty canonical token from the native `deck.id`
  field used by OCGForge `episode_identity.v1`
seat0_deck_sha256: exact 64 lowercase ASCII hex `deck.sha256` from the same
  native deck-identity contract
seat1_deck_id: exact nonempty canonical token from the native `deck.id`
  field used by OCGForge `episode_identity.v1`
seat1_deck_sha256: exact 64 lowercase ASCII hex `deck.sha256` from the same
  native deck-identity contract
Git commit fields: exactly 40 lowercase ASCII hex characters
SHA-256 fields: exactly 64 lowercase ASCII hex characters
native_setup_descriptor_id: nonempty canonical ASCII token string
supported_message_family_envelope_id: the fixed V1 token
  `ocgforge-ignis.i6c6.supported-message-family-envelope.v1`
comparison_boundary.kind: u8, exactly `1` (`PUBLIC_EVENT_PREFIX_AND_STATE`)
comparison_boundary.public_event_prefix_count: u64be
comparison_boundary.state_snapshot_selector: u8, exactly `1`
  (`POST_PUBLIC_EVENT_PREFIX`)
```

Every `string` is `u32be byte_length || exact UTF-8 bytes`. The manifest
identity bytes are the domain string
`OCGFORGE-IGNIS-I6C6-SAME-SCENARIO-REPLAY-V1\0`, followed by the ordered
fields above. Canonical token strings use lowercase ASCII letters, digits,
`.` , `_` and `-` only; no whitespace, slash, control character, or locale
dependent spelling is permitted. No JSON whitespace, property order, locale,
or platform path representation participates in the identity.

For this V1 manifest, the four deck fields are imported independently from the
native OCGForge deck vector used by `episode_identity.v1`, in fixed seat order
`seat0`, then `seat1`. `seat*_deck_sha256` is not recomputed by this contract,
and there is no combined deck digest. A missing native deck identity, a deck
digest whose source contract cannot be identified, or a seat-order mismatch
fails closed.

`native_setup_descriptor_id` identifies the source-controlled OCGForge
scenario descriptor whose deterministic setup operations are used. It is not a
free-form label: the descriptor registry must resolve it to exactly one
versioned setup definition, and the same descriptor must be selected by the
Ignis replay fixture. V1 deliberately has no separate
`native_setup_transcript_sha256` field; setup is bound by this descriptor ID
plus the fixed scenario fields, rather than by an undefined digest domain. If
the descriptor cannot be resolved or its versioned contents are unavailable,
the scenario is `UNPROVEN`.

`supported_message_family_envelope_id` is likewise a versioned contract ID,
not a caller-defined digest. The V1 value above names the fixed admitted
`GameplayMessageKindV1`/native public-event family table used by this plan.
Changing that table requires a new envelope ID. There is deliberately no
`supported_message_family_envelope_sha256` field without a separately frozen
byte domain.

`ComparisonBoundaryV1` is the shared boundary, not two runtime-specific token
claims. Its V1 encoding is the three fields
`kind:u8 || public_event_prefix_count:u64be || state_snapshot_selector:u8`.
Only `kind=1` and `state_snapshot_selector=1` are accepted. The native and
Ignis sides must each provide the public-event prefix of that exact length and
the corresponding post-prefix public-safe-state snapshot. A boundary that
cannot be selected by this pair is `UNPROVEN`.

For this V1 manifest, Git commit fields are
`ocgforge_semantic_commit`, `ocgforge_core_commit`,
`ocgforge_cardscripts_commit`, `ignis_edopro_commit`,
`ignis_edopro_core_commit`, and `ignis_cardscripts_commit`. SHA-256 fields are
`seat0_deck_sha256`, `seat1_deck_sha256`, `rules_bundle_id`,
`ocgforge_core_patchset_sha256`,
`native_raw_transcript_sha256`, `ignis_raw_replay_transcript_sha256`,
`native_public_event_transcript_sha256`,
and `ignis_public_event_transcript_sha256`.

The complete SameScenario manifest bytes and identity are
`RESTRICTED_EVIDENCE`. They are not a public gameplay identity, public frame
field, semantic comparison digest, model input, or public acceptance artifact.
In particular, raw transcript digests may depend on hidden-dependent transport
bytes and must remain outside all public outputs.

Raw runtime transcripts are restricted forensic evidence only. They are not
required to be byte-equal because OCGForge observes raw core messages while
EDOPro emits player-specific STOC messages, redacts identities, and may add
refresh messages.

Each raw transcript may use the following independent canonical container for
its own provenance hash:

```text
CanonicalGameplayMessageV1 =
    ordinal:u64be
    message_id:u8
    payload_length:u32be
    payload_bytes[payload_length]

RestrictedRawGameplayTranscriptV1 =
    domain string "OCGFORGE-IGNIS-I6C6-GAMEPLAY-TRANSCRIPT-V1\0"
    message_count:u32be
    CanonicalGameplayMessageV1[message_count]
```

For OCGForge, the native event frame is decoded as `u32le frame_length`, then
`message_id:u8 || payload_bytes`; only that native frame-length wrapper is
removed. For Ignis, the already extracted `StocGameMessagePayload.Bytes` is
used as `message_id:u8 || payload_bytes`; no additional gameplay bytes are
removed. The payload bytes are otherwise copied unchanged. The canonical
message ordinal starts at zero and increments by one. The two restricted raw
hashes are never compared as a same-scenario equality criterion and never
enter public comparison artifacts or diagnostics.

The common cross-runtime boundary is the perspective-safe public event
transcript, not the raw packet stream:

```text
CanonicalPublicEventTranscriptV1 =
    domain string "OCGFORGE-IGNIS-I6C6-PUBLIC-EVENT-TRANSCRIPT-V1\0"
    event_count:u32be
    CanonicalPublicEventRecordV1[event_count]

CanonicalPublicEventRecordV1 =
    event_index:u64be
    kind:u8
    player:optional u8
    entity:optional locator string
    public_passcode:optional u32be
    from_zone:optional u8
    to_zone:optional u8
    count:optional u32be
    amount:optional signed i32
    counter_type:optional u32be
    phase:optional u32be
    winner:optional u8
    win_reason:optional u8
    effect_description:optional u64be
    targets:u32be count followed by lexicographically sorted locator strings
```

Every optional field is encoded as `presence:u8`, where `0` means absent and
`1` means present, followed by the value only when present. A signed `i32` is
encoded as its two's-complement bit pattern in `u32be`. Every locator string
is `u32be byte_length || exact UTF-8 bytes`. Target ordering is bytewise
lexicographic ordering of those UTF-8 bytes. Event, zone, position,
relationship, and visible-event kind values use the exact
`ocgforge.public_safe_state.v1` code tables; an unknown code fails closed.

The native `project_visible_events` result and the Ignis I6C4 ledger are
converted to this exact public-event form. `engine_step_index` is omitted.
The native and Ignis public-event transcript hashes must be equal through the
selected comparison boundary. A refresh packet that produces no public event
is represented by the current public-safe-state boundary, not by an invented
event. Message redaction, refresh packets, and raw transport wrappers are
therefore allowed to differ while the perspective-safe public history and
state remain bound.

Semantic inputs are `perspective_player`, `starting_player`, `duel_flags`,
seed words, the seat-ordered deck identity vector, and the structured
`ComparisonBoundaryV1`. Runtime/source commits, patchset IDs, raw transcript
digests, the setup descriptor ID, and the message-family envelope ID are
provenance/evidence fields; they are not public-frame semantic values.

The raw transcript hashes cover restricted runtime evidence; the two
public-event transcript hashes cover the common comparison boundary. The
native setup descriptor ID owns the deterministic fixture/setup definition and
is resolved before replay. A scenario passes only when the manifest values,
fixed message-family envelope, structured boundary, perspective, seed, flags,
seat-ordered deck identities, and canonical public-event transcripts agree
exactly. The selected public-safe-state boundary is then compared separately
by the native oracle contract.
Missing or unmatched transcript evidence is `UNPROVEN`, not a reduced
comparison corpus.

The bridge does not require `ocgforge_core_commit == ignis_edopro_core_commit`.
It requires the two runtime identities to be recorded and the public transcript
and boundary behavior to be evidenced for the scenario.

### 10.2 `CardScriptsBehaviorBindingV1`

This is a sub-contract of `SameScenarioReplayBridgeV1`, never a third bridge
architecture. Every I6C6 V1 scenario is treated as script-dependent and
declares `SCRIPT_DEPENDENCY=CLOSURE`. This label records the scenario's
CardScripts provenance requirement; it does not claim that an external EDOPro
process has exposed its complete runtime load trace. A `NONE` mode is not a
behavior-equivalence bypass in I6C6 V1.

```text
SCRIPT_DEPENDENCY=CLOSURE
```

An empty required-script set alone is insufficient and does not create a
script-free bypass.

`SCRIPT_DEPENDENCY=CLOSURE` requires:

```text
ocgforge_cardscripts_commit
ignis_cardscripts_commit
canonical_public_event_transcript_sha256
```

The two CardScripts commit fields are restricted source provenance. Optional
scenario-declared closure evidence may additionally provide
`ocgforge_script_closure_digest` and `ignis_script_closure_digest`; it is also
restricted provenance and is not required for the behavior gate. When supplied,
the closure entries are canonical sorted relative paths and their file bytes
are hashed in path order. The closure digest grammar is:

```text
OCGFORGE-IGNIS-I6C6-CARDSCRIPT-CLOSURE-V1\0
entry_count:u32be
for each entry in byte-lexicographic path order:
    path_length:u32be
    path_utf8_bytes[path_length]
    file_sha256: exactly 64 lowercase ASCII hex characters
```

Paths are root-relative UTF-8 paths with `/` separators and no `.` or `..`
components. If optional closure evidence is supplied, its declared set must
include the globally required scripts `constant.lua`, `utility.lua`, and
`proc_normal.lua`, together with the externally inspectable scenario-declared
paths. No V1 rule requires an EDOPro script-reader, script-load, or
script-callback trace, and no V1 rule claims that an externally declared set is
an exhaustive runtime execution closure. If supplied closure evidence is
malformed or its digest cannot be reproduced from the declared bytes, the
scenario fails closed; if it is absent, the scenario remains eligible for the
behavior gate without that optional provenance detail. The two script closure
digests are allowed to differ because the runtimes use different CardScripts
commits.

`CARD_SCRIPT_BEHAVIOR_BINDING=PASS` is determined by the exact shared
`CanonicalPublicEventTranscriptV1` and selected public-safe-state boundary,
not by a claim of equal script bytes or equal internal execution. Equal card
passcodes or equal top-level script commits alone never prove equivalent
behavior. The public behavior evidence is the executable binding for this
sub-contract; CardScripts commits and optional closure hashes remain
restricted provenance.

The future acceptance gate is therefore:

```text
CARD_SCRIPT_PROVENANCE=PASS
CARD_SCRIPT_BEHAVIOR_BINDING=PASS
```

`CARD_SCRIPT_PROVENANCE=PASS` requires both runtime commit fields and validates
any supplied optional closure evidence. `CARD_SCRIPT_BEHAVIOR_BINDING=PASS`
requires the canonical public-event transcript and selected public-safe-state
boundary to pass; it is not replaced by `SCRIPT_DEPENDENCY=NONE`.

### 10.3 `PrintedSourceBridgeV1`

The V1 Printed bridge uses the generated semantic artifact, not BabelCDB
commit equality. At the exact OCGForge `CoreHost` boundary,
`RulesBundlePaths.card_data_tsv` identifies the bytes loaded by
`CardDataStore`. The Ignis provider consumes the same frozen pipe12 artifact
format. For I6C6 V1 the strongest and simplest rule is required:

```text
native_card_data_tsv_bytes == ignis_provider_artifact_bytes
```

The artifact equality is byte equality after both sides have opened the exact
external files. The bridge manifest must bind:

```text
source_artifact_format_id
native_card_data_tsv_sha256
ignis_provider_artifact_sha256
coverage_passcodes[]
coverage_digest_sha256
semantic_rows_digest_sha256
ocgforge_semantic_commit
rules_bundle_id
transformation_source_commit
transformation_source_path
transformation_file_sha256
```

The two raw artifact hashes must be equal, the coverage sets/digests must be
equal, and the semantic-row digest must be equal to the Ignis provider
manifest. `ocgforge_babelcdb_commit` and `ignis_babelcdb_commit` remain
forensic provenance fields; they need not be equal and do not define Printed
semantic equality. A source artifact mismatch, absent external artifact, or
manifest claim without exact file/hash evidence fails closed.

This contract does not permit copying the native artifact or real card rows
into Ignis. It binds two externally available files at test execution time;
the repository and release remain free of those bytes.

The Printed bridge gates are:

```text
PRINTED_SOURCE_ARTIFACT_BINDING=PASS
PRINTED_COVERAGE_BINDING=PASS
PRINTED_SEMANTIC_ROWS_BINDING=PASS
PRINTED_PROVIDER_ENVIRONMENT_BINDING=PASS
NO_HIDDEN_PROVIDER_LOOKUP=PASS
```

## 11. Supported acceptance corpus

The later bounded corpus must include, for both perspectives where meaningful:

```text
initial/start and Extra UPDATE_DATA bootstrap
known own Hand/Extra identities
opponent hidden Hand/Deck/face-down Extra paired worlds
public face-up field and Extra identities
Main Deck counts without Main Deck entities
Graveyard and Banished public identity
Monster/SZONE/PZONE/Field-zone projection
normal, Xyz, Link, Pendulum, and combined XYZ|LINK property cases
Xyz-material, Equip, and Target relationships
chain links, source/activation-zone/targets
Move, Shuffle, RandomizationBoundary, and supported visible-event families
MSG_SWAP_GRAVE_DECK plus subsequent MSG_SHUFFLE_DECK
duplicate passcodes and public Extra ordinal assignment
malformed/unsupported source cases that must fail closed
fresh-process repeated runs
```

No real third-party card-data rows may be added to Ignis. Existing OCGForge native fixtures may remain owned and executed by OCGForge; any cross-repository use must be an explicit external fixture/provenance input and must not be copied into this repository.

## 12. Failure and diagnostic model

The comparator returns exactly one structured outcome:

```text
PASS
or
FAIL(schema/field/visibility/order/provenance/determinism)
```

It must fail closed for missing native fields, unsupported enums, ambiguous mappings, cardinality mismatches, visibility mismatches, Printed/Current mismatches, locator collisions, relationship/chain mismatches, event-index mismatches, and unproven scenario pairing. Diagnostics identify a public-safe field path and stable error code; they must not reveal hidden values.

## 13. Exact later implementation file scope

This audit creates no implementation files. After separate authorization, the narrowest expected Ignis-side test scope is:

```text
CREATE  tests/OCGForge.Ignis.Gameplay.Tests/Tests/I6C6NativeOracleTests.cs
CREATE  tests/OCGForge.Ignis.Gameplay.Tests/Fixtures/I6C6ScenarioFixtures.cs
CREATE  tests/OCGForge.Ignis.Gameplay.Tests/Fixtures/I6C6ComparisonFixtures.cs
```

An OCGForge-side native adapter, if required by the proven bridge design, must be test-only and separately authorized in the OCGForge repository, likely under:

```text
tests/observation/i6c6_native_oracle_test.cpp
tests/observation/i6c6_native_oracle_test.hpp
```

No production Ignis file, OCGForge production file, third-party pin, database, or real-data fixture is authorized by this plan.

## 14. Staged implementation plan

### Task I6C6-1: Freeze comparison contract and RED cases

**Files:**

- Create: `tests/OCGForge.Ignis.Gameplay.Tests/Tests/I6C6NativeOracleTests.cs`
- Create: `tests/OCGForge.Ignis.Gameplay.Tests/Fixtures/I6C6ScenarioFixtures.cs`
- Test only: no production source changes

- [ ] Define a test-only normalized safe-state structure with the exact matrix above.
- [ ] Add RED tests for absent `player_to_act`, optional `PRESENT(0)`, enum mapping, locator ordering, hidden identity rejection, and unsupported scenario pairing.
- [ ] Run the Gameplay harness and confirm the new test fails because the comparator/bridge is absent, not because of a malformed fixture.

### Task I6C6-2: Implement typed test-boundary normalization

**Files:**

- Create: `tests/OCGForge.Ignis.Gameplay.Tests/Fixtures/I6C6ComparisonFixtures.cs`
- Modify: `tests/OCGForge.Ignis.Gameplay.Tests/Tests/I6C6NativeOracleTests.cs`
- No production changes

- [ ] Normalize `PerspectiveSafeFrameV1` into the typed comparison model using only public values.
- [ ] Add equivalent native-row ingestion for `PlayerObservation`/`PublicSafeStateView`.
- [ ] Enforce exact optional presence, enum, ordering, privacy, relationship, chain, and event rules.
- [ ] Return structured field-path mismatches without private values.
- [ ] Run focused RED/GREEN tests for synthetic typed rows.

### Task I6C6-3: Add the externally bound native scenario bridge

**Files:**

- Modify/create: OCGForge test-only observation oracle adapter only after separate OCGForge authorization.
- Modify: `tests/OCGForge.Ignis.Gameplay.Tests/Fixtures/I6C6ScenarioFixtures.cs`
- No Ignis production changes

- [ ] Define a scenario manifest binding native setup, Ignis transcript, perspective, seed, duel flags, and provenance.
- [ ] Include separate `ocgforge_cardscripts_commit` and `ignis_cardscripts_commit` fields, plus the independent OCGForge/Ignis BabelCDB identities.
- [ ] Build native `PlayerObservation` with `CoreHost` and `ObservationBuildConfig`.
- [ ] Replay only the explicitly paired supported transcript into `PerspectiveStateMirrorV1`.
- [ ] Record script-dependent scenarios with the two CardScripts commits and any optional, statically declared closure evidence; do not require an unavailable EDOPro runtime script trace or admit a `SCRIPT_DEPENDENCY=NONE` bypass.
- [ ] Require `CANONICAL_PUBLIC_EVENT_TRANSCRIPT_BINDING=PASS` plus selected public-safe-state boundary equality as the script-dependent behavior binding; restricted raw transcript hashes remain provenance only.
- [ ] Reject any scenario whose native and Ignis source histories cannot be bound without inference.
- [ ] Provision Printed rows externally or use an approved synthetic native catalog; do not add real rows to Ignis.

### Task I6C6-4: Native canonical bytes, paired privacy, and determinism

**Files:**

- Modify: `tests/OCGForge.Ignis.Gameplay.Tests/Tests/I6C6NativeOracleTests.cs`
- Modify: test-only native adapter from I6C6-3

- [ ] Compare typed normalized state first.
- [ ] Obtain native `canonical_public_safe_state_bytes` through the OCGForge owner.
- [ ] Compare native-oracle canonical bytes/digest only through the test boundary; do not copy the codec into Ignis production.
- [ ] Add paired hidden worlds with equal public outputs and equal comparison results.
- [ ] Run each scenario twice in fresh processes and compare stdout, stderr, normalized bytes, digest, and exit code.

### Task I6C6-5: Aggregate acceptance

**Files:**

- Modify: `tests/OCGForge.Ignis.Gameplay.Tests/Tests/I6C6NativeOracleTests.cs`
- Add/update only I6C6 acceptance evidence after all previous gates pass

- [ ] Require exact scenario-manifest provenance and supported-corpus coverage.
- [ ] Require zero hidden-value diagnostics.
- [ ] Require no I3/I4/I5/I6C1-I6C5 regressions.
- [ ] Require hosted CI with native and Ignis heads explicitly recorded.
- [ ] Set `I6C6_FINAL` only by independent review; this plan never authorizes implementation or final acceptance.

## 15. Acceptance gates for the future implementation

```text
NATIVE_ORACLE_HEAD_MATCH=PASS
IGNIS_I6C5_HEAD_MATCH=PASS
SCENARIO_PROVENANCE_BINDING=PASS
DECK_IDENTITY_BINDING=PASS
NATIVE_SETUP_DESCRIPTOR_BINDING=PASS
MESSAGE_FAMILY_ENVELOPE_BINDING=PASS
COMPARISON_BOUNDARY_CANONICAL=PASS
CANONICAL_PUBLIC_EVENT_TRANSCRIPT_BINDING=PASS
RAW_TRANSCRIPTS_RESTRICTED_ONLY=PASS
CARD_SCRIPT_PROVENANCE=PASS
CARD_SCRIPT_BEHAVIOR_BINDING=PASS
PLAYER_TO_ACT_ABSENT_OR_I6D_BLOCKED=PASS
TYPED_FIELD_COMPARISON=PASS
NATIVE_SAFE_STATE_BYTES=PASS
PRINTED_SOURCE_ARTIFACT_BINDING=PASS
PRINTED_COVERAGE_BINDING=PASS
PRINTED_SEMANTIC_ROWS_BINDING=PASS
PRINTED_PROVIDER_ENVIRONMENT_BINDING=PASS
NO_HIDDEN_PROVIDER_LOOKUP=PASS
PAIRED_WORLD_PRIVACY=PASS
MISMATCH_DIAGNOSTICS_PUBLIC_SAFE=PASS
FRESH_PROCESS_DETERMINISM=PASS
NO_REAL_THIRD_PARTY_DATA_IN_IGNIS=PASS
NO_I6D_OR_I7_AUTHORITY=PASS
```

Any unproven bridge, field, or runtime difference remains a fail-closed blocker. I6C6 implementation is not authorized by this document.

## 16. Current stop state

```text
I6C6_SOURCE_AUDIT=PASS
OCGFORGE_NATIVE_ORACLE_PATH=PROVEN
IGNIS_I6C5_SOURCE_PATH=PROVEN
FIELD_MAPPING_COMPLETE=YES
PRIVACY_MODEL_COMPLETE=YES
DETERMINISM_MODEL_COMPLETE=YES
IMPLEMENTATION_PLAN_COMPLETE=YES
SAME_SCENARIO_REPLAY_BRIDGE=PROVEN_CONTRACT
CARD_SCRIPT_BEHAVIOR_BINDING_CONTRACT=PROVEN
PRINTED_SOURCE_BRIDGE=PROVEN_CONTRACT
SAME_SCENARIO_REPLAY_EVIDENCE=UNPROVEN
PRINTED_SOURCE_EVIDENCE=UNPROVEN
I6C6_DESIGN_READY_FOR_FINAL_INDEPENDENT_REVIEW=YES
I6C6_DESIGN_FINAL=NO
I6C6_IMPLEMENTATION_AUTHORIZED=NO
I6D_AUTHORIZED=NO
I7_AUTHORIZED=NO
```

No production code, tests, fixtures, third-party pins, OCGForge files, PR, or merge is created by this audit.
