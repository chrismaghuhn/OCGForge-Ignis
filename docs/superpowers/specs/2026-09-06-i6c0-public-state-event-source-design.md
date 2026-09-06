# I6C0 Public-State and Visible-Event Source Closure Design

Status: SOURCE-CLOSURE DESIGN / IMPLEMENTATION BLOCKED. This document does not
authorize I6C runtime work.

Audit date: 2026-09-06

```text
IGNIS_SOURCE_COMMIT=144322702a0d885e71ff2b578d134e104ffb2969
OCGFORGE_SOURCE_COMMIT=3edfcabf51dd914f96adc4df903b1ac2a9d20e5f
I3_V1_CANONICAL_BYTES_CHANGED=NO
I3_V1_IDENTITY_CHANGED=NO
```

## 1. Purpose and decision boundary

I6C0 audits whether OCGForge-Ignis can provide one complete,
perspective-safe, deterministic source for the OCGForge
`public_safe_state.v1` and `public_environment_observation.v1` contracts. It
freezes field provenance, privacy rules, ordering rules, versioning rules, and
the later implementation sequence. It does not create model-input bytes and
does not reopen I4/I5 prompt legality.

The result is intentionally not a runtime-ready source contract. The current
Ignis implementation has no accepted source containing the complete OCGForge
state and event surface. A future contract file is therefore omitted from this
slice; the design and plan record the exact blockers and the conditions under
which a new source contract may be proposed.

The ownership direction is:

```text
EDOPro protocol and pinned runtime
        ↓ typed, perspective-safe Ignis source facts
Ignis public-frame source seam
        ↓ source-closure validation
OCGForge PlayerObservation/public-safe-state semantics
        ↓ OCGForge-owned canonical bytes
I6 model-facing consumer
```

OCGForge owns the meaning and canonical encoding. Ignis may own only the
validated source/provenance boundary. No I6C source value may contain private
response bindings, raw protocol buffers, Mirror identities, or hidden-state
inference.

## 2. Audited authority set

The OCGForge authority was read from `origin/main` at
`3edfcabf51dd914f96adc4df903b1ac2a9d20e5f`. The relevant primary surfaces are:

| Authority | Exact source | Finding for I6C0 |
| --- | --- | --- |
| Safe-state meaning and bytes | `docs/contracts/public-environment-observation-v1.md`; `include/ygo/environment/public_safe_state.hpp`; `src/environment/public_safe_state.cpp` | The target contains globals, zones, entities, relationships, chain, visible events, and match context. OCGForge owns their field order, optional presence, validation, and bytes. |
| Player-observation source shape | `include/ygo/observation/player_observation.hpp`; `include/ygo/observation/observed_*.hpp` | `PlayerObservation` carries all target state plus internal decision metadata. It is not itself the policy-facing value. |
| State construction | `src/observation/observation_builder.cpp` | OCGForge builds zone counts, visible entities, relations, chain state, and match context from a `CoreHost` plus explicit configuration. |
| Visible-event construction | `src/observation/event_projection.cpp`; `src/observation/observation_session.cpp` | One framed-message batch can produce zero, one, or multiple semantic events. `ObservationSession` assigns a monotonic event index to emitted events. |
| Public outer observation | `include/ygo/environment/public_environment_observation.hpp`; `src/environment/public_environment_observation.cpp` | The outer value contains decision index, safe-state bytes, safe decision context, and the OCGForge-owned observation digest. |
| Canonical state validation | `src/environment/public_safe_state.cpp` functions `validate_observation`, `append_*`, `read_safe_state` | Counts, optional presence, enums, unique locators/event indexes, and canonical ordering are validated before bytes are accepted. |

The Ignis authority was read at the integrated I6B main commit:

| Ignis source | Exact current capability |
| --- | --- |
| `src/OCGForge.Ignis.Gameplay/PerspectiveStateTypesV1.cs` | `MirrorSnapshotV1` has current participants, seven mirror zones, cards, current chain snapshots, and internal relation snapshots. Relations and some identities remain internal. |
| `src/OCGForge.Ignis.Gameplay/PerspectiveStateMirrorV1.cs` | The mirror applies typed gameplay messages transactionally. `MirrorState` stores life points, seven-zone counts, current entities, chains, and relation lists, but no visible-event history. |
| `src/OCGForge.Ignis.Gameplay/PublicStateProjectionV1.cs` | I3 V1 publishes only the frozen contract ID, perspective, flags, turn/phase/terminal, participants, and cards. It explicitly does not publish relations, chains, events, or a locator table. |
| `src/OCGForge.Ignis.Gameplay/PublicSemanticLocatorV1.cs` | I3 locator grammar is immutable and separate from protocol addresses and `MirrorEntityIdV1`. |
| `src/OCGForge.Ignis.Gameplay/GameplayMessageDecoderV1.cs` | Typed decoding exists for the accepted I3B message subset, including movement, turn/phase, life points, chain, target, and equipment messages. Shuffle, confirm/reveal variants, summon presentation variants, and counter-change events are not all admitted by this decoder. |
| `src/OCGForge.Ignis.Gameplay/GameplayMirrorSessionV1.cs` | `PumpAsync` decodes/applies one message and returns one current snapshot. It has no event ledger or public-frame source. |
| `src/OCGForge.Ignis.Gameplay/FlatPrompt*` | I4/I5 continuation state and private response bindings are adapter-local and must remain outside I6C. |

The existing I3 contract at `docs/contracts/public-state-projection-v1.md`
freezes the V1 bytes and identity. No I6C design may extend that byte surface.

## 3. Target field ownership and source status

The following status vocabulary is used exactly once per target field:

```text
PROVEN_EXISTING_SOURCE
PROVEN_DERIVED_FROM_PUBLIC_FACTS
REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER
REQUIRES_EXPLICIT_RUNTIME_CONFIGURATION
NOT_AVAILABLE_FROM_PINNED_RUNTIME
SEMANTICS_AMBIGUOUS
OUTSIDE_I6C
```

The OCGForge source references used below are abbreviated as follows:

```text
O1 = OCGForge public-environment contract and public-safe-state headers
O2 = OCGForge public_safe_state.cpp validation/encoding
O3 = OCGForge observation_builder.cpp
O4 = OCGForge event_projection.cpp and observation_session.cpp
I1 = Ignis PerspectiveStateTypesV1.cs / PerspectiveStateMirrorV1.cs
I2 = Ignis PublicStateProjectionV1.cs / PublicSemanticLocatorV1.cs
I3 = Ignis GameplayMessageDecoderV1.cs / GameplayMirrorSessionV1.cs
P  = Ignis PROTOCOL_PROVENANCE.md and pinned EDOPro message facts
```

### 3.1 Safe-state envelope and globals

| OCGForge field | Required semantics | Ignis source | Pinned producer | Knowledge classification | Persistence/history needed | Ordering rule | Status | Owning future slice | Failure behavior |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| safe-state domain/schema | Exact `ocgforge.public_safe_state.v1` pair | None; fixed contract value | O1/O2 | Contract constant | No runtime history | First two safe-state fields | PROVEN_EXISTING_SOURCE | I6C1 | Reject if altered or absent |
| `globals.duel_flags` | Exact duel-layout/flag value | I2 `PublicStateProjectionContextV1.DuelFlags`; not retained by `MirrorState` | O3 receives explicit flags | Configuration value, not inferred state | Run configuration | Scalar | REQUIRES_EXPLICIT_RUNTIME_CONFIGURATION | I6C5 | Reject if absent or inconsistent |
| `globals.life_points[]` | Ordered current LP values, one per absolute player | I1 `MirrorState.LifePoints`; start and LP message payloads | O3 `field.players[].life_points`; P `MSG_START`, LP messages | Perspective-safe public scalar | Current state only | Absolute player order | PROVEN_EXISTING_SOURCE | I6C2 | Reject unknown/count mismatch |
| `globals.player_to_act?` | Optional current acting player, not a turn-player alias | OCGForge derives it from attached decision context; current Ignis has no accepted general field | O1 `DecisionContext.player` | Decision/context value, not gameplay state | Decision integration | Optional presence bit | OUTSIDE_I6C | I6D | Omit unless the separately owned decision source supplies it; never infer |
| `globals.turn_player?` | Optional absolute player whose turn is current | I1 `MirrorState.TurnPlayer`, `ApplyNewTurn` | O3 event-global projection and `MSG_NEW_TURN` | Public protocol fact | Current state | Optional presence bit | PROVEN_EXISTING_SOURCE | I6C2 | Reject invalid player/provenance |
| `globals.turn_count?` | Optional semantic turn count | I1 `MirrorState.TurnCount`, incremented by `ApplyNewTurn` | O3 derives from turn-start events | Public derived scalar | Current state plus turn history | Scalar | PROVEN_DERIVED_FROM_PUBLIC_FACTS | I6C2 | Reject if equivalence cannot be proved |
| `globals.phase?` | Optional exact phase value | I1 `MirrorState.Phase`, `ApplyNewPhase` | O3 phase events / `MSG_NEW_PHASE` | Public protocol fact | Current state | Optional presence bit | PROVEN_EXISTING_SOURCE | I6C2 | Reject invalid provenance |
| `globals.chain_length` | Current chain length, distinct from link count | I1 chains plus pending chain; not exposed by accepted I3 V1 | O3 `field.chain.size()` | Current mirror fact requiring a new public source | Current chain state | Scalar | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C3 | Reject inconsistent chain/pending state |
| `globals.winner?` | Optional absolute winner | I1 `MirrorTerminalSnapshotV1.Winner` is omitted by accepted I3 V1 | O1/O3 win source | Current mirror fact requiring a new public source | Terminal state | Optional presence bit | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C2 | Reject invalid player |
| `globals.win_reason?` | Optional exact win reason/type | I1 `MirrorTerminalSnapshotV1.WinType` | O1 `MSG_WIN` payload | Public terminal fact, exact name mapping still required | Terminal state | Optional presence bit | SEMANTICS_AMBIGUOUS | I6C2 | Reject until native equivalence is proven |
| `globals.terminal` | Terminal boolean | I1 `MirrorTerminalSnapshotV1.IsTerminal` | O3 Win projection | Public current fact | Current state | Scalar | PROVEN_EXISTING_SOURCE | I6C2 | Reject terminal/state contradiction |

`player_to_act` is not interchangeable with `turn_player`. `decision_index`,
`PromptInstanceOrdinal`, continuation steps, network message counts, and
GAME_MSG counts are not accepted substitutes for either global turn field.

I6C2 owns the gameplay-state globals above except for `player_to_act`. The
configured `duel_flags` value is owned by I6C5 because it is explicit runtime
configuration rather than reducer state; `player_to_act` remains outside I6C
and is owned by I6D.

### 3.2 Zone records

The OCGForge target zone set is `MAIN_DECK`, `HAND`, `MONSTER_ZONE`,
`SPELL_TRAP_ZONE`, `GRAVEYARD`, `BANISHED`, `EXTRA_DECK`, `FIELD_ZONE`,
`PENDULUM_RELEVANT`, and `OVERLAY`. Ignis currently has seven
`MirrorZoneV1` values; field/pzone/overlay are derived or represented through
card flags rather than a complete zone vector.

| Zone field / coverage | Required semantics | Ignis source | Pinned producer | Knowledge classification | Persistence/history needed | Ordering rule | Status | Owning future slice | Failure behavior |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `zone.player` | Absolute player 0/1 | I1 controller/perspective mapping | O1/O3 | Public | Current state | Zone tuple order | PROVEN_DERIVED_FROM_PUBLIC_FACTS | I6C2 | Reject invalid player |
| `zone.kind` for MAIN_DECK/HAND/MZONE/SZONE/GRAVE/BANISHED/EXTRA | Exact semantic zone enum | I1 `MirrorZoneV1` | O3 zone projection | Public | Current state | OCGForge zone tuple sort | PROVEN_DERIVED_FROM_PUBLIC_FACTS | I6C2 | Reject unknown enum |
| `zone.kind` for FIELD_ZONE/PENDULUM_RELEVANT | SZONE layout-dependent semantic classification | I2 `TryClassifySpellTrap`, duel flags, proven Type query | O3 `project_zone` | Public only with flags and Type proof | Current state/config | OCGForge zone tuple sort | REQUIRES_EXPLICIT_RUNTIME_CONFIGURATION | I6C2 | Reject missing/contradictory layout proof |
| `zone.kind` for OVERLAY | Separate overlay population only after parent/material proof | I1 `IsOverlay`, `OverlayRelations` | O3 overlay traversal | Perspective-safe only with parent/material proof | Current relation/state | Zone tuple sort | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C3 | Reject unproven parent/continuity |
| `zone.total_count` for existing seven zones | Total population, including hidden cards | I1 `ZoneCounts` and participant zones | O3 `add_zone_counts` | Public count | Current state | Zone tuple sort | PROVEN_EXISTING_SOURCE | I6C2 | Reject count underflow/overflow |
| `zone.total_count` for field/pzone | Total population of derived zone | I1 slots; no complete public zone vector | O3 layout traversal | Derived public fact | Current state/config | Zone tuple sort | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C2 | Reject if composition is incomplete |
| `zone.total_count` for overlay | Total overlay population under proven parent relations | I1 overlay flags/relations | O3 overlay traversal | Derived public fact | Current relation/state | Zone tuple sort | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C3 | Reject if relation composition is incomplete |
| `zone.public_identity_count` | Count of identities legally public to perspective | I1 nullable card codes and counts, but no accepted zone summary | O3 visibility rules | Zone-level visibility semantics differ at current seam | Current state | Zone tuple sort | SEMANTICS_AMBIGUOUS | I6C2 | Reject if visibility cannot be proven |
| `zone.hidden_count` | Exact hidden population, not guessed as a residual | I1 total counts plus incomplete public-card projection | O3 computes from zone-specific visibility | Zone-level residual semantics are not yet equivalent | Current state | Zone tuple sort | SEMANTICS_AMBIGUOUS | I6C2 | Reject if residual derivation is not valid for zone |
| `zone.player_observable_order` | Whether order is observable and semantically meaningful | No equivalent field in I1/I2; own-Hand semantics differ | O3 sets explicit per-zone value | Public semantic policy | Current state plus zone policy | Zone tuple sort | SEMANTICS_AMBIGUOUS | I6C2 | Reject instead of container-order inference |
| Main Deck coverage | Count only; no per-card entity/locator | I1 count exists; public I3 excludes cards | O3 `keep_entity` excludes Main Deck | Hidden/public population rule | Current state | Main Deck zone record | PROVEN_EXISTING_SOURCE | I6C2 | Never create per-card Main Deck locator |
| Hand coverage | Own order/identity may be perspective-private; opponent hidden identities omitted | I1 hand count/cards/provenance | O3 visibility and sequence rules | Perspective-safe | Current state; shuffle destroys hidden continuity | Zone tuple sort | PROVEN_DERIVED_FROM_PUBLIC_FACTS | I6C2 | Reject hidden identity leakage |
| MZONE/SZONE coverage | Field slots and public/hidden identity | I1 field entities/positions; SZONE mapping in I2 | O3 field slots/layout | Public with layout proof | Current state/config | Zone tuple sort | PROVEN_DERIVED_FROM_PUBLIC_FACTS | I6C2 | Reject unsupported layout |
| Graveyard/Banished coverage | Population and public identities only when proven | I1 counts/entities/query provenance | O3 visibility rules | Perspective-safe | Current state | Zone tuple sort | PROVEN_DERIVED_FROM_PUBLIC_FACTS | I6C2 | Reject unproven identity |
| Extra Deck coverage | Count; known public cards only when exposed | I1 count/entities/provenance | O3 visibility rules | Perspective-safe | Current state; hidden continuity destroyed | Zone tuple sort | PROVEN_DERIVED_FROM_PUBLIC_FACTS | I6C2 | Never create unknown locator |

`hidden_count` must not be implemented as “total minus rows currently emitted”
unless the zone-specific visibility rule and all omitted categories are proven.

I6C2 closes ordinary zone/entity facts and any layout-derived field/pzone
facts whose source is independently proven. It does not claim complete
ten-zone closure: overlay population, overlay entity zone, and
`overlay_sequence` remain explicitly blocked until I6C3 proves the parent and
material relationships. A future I6C2 implementation must preserve those
constituents as fail-closed rather than fabricating an overlay projection.

### 3.3 Entity records and nested card properties

OCGForge `ObservedCard` is perspective-safe only after its identity and
properties pass the OCGForge validation rule. In particular, a card with
`identity_known=false` must have no passcode, printed properties, or current
properties. Ignis query facts are stored in `MirrorQueryFieldSnapshotV1` and
are not yet a complete accepted entity-property source.

| Entity field | Required semantics | Ignis source | Pinned producer | Knowledge classification | Persistence/history needed | Ordering rule | Status | Owning future slice | Failure behavior |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `entity.locator` | Non-empty unique public semantic locator | I2 `PublicSemanticLocatorV1` forms, but own-Hand mapping differs from OCGForge | O1/O2 locator validation | Public token semantics are not fully equivalent | Current frame only | Locator value | SEMANTICS_AMBIGUOUS | I6C2 | Reject missing/colliding/unproven locator |
| `entity.identity_known` | Whether passcode/properties are known to perspective | I1 `MirrorValueV1.IsKnown` and provenance | O3 visibility rule | Perspective-safe knowledge classification | Knowledge-destroying transitions | Entity record | PROVEN_EXISTING_SOURCE | I6C2 | Reject inconsistent identity fields |
| `entity.passcode?` | Optional public passcode | I1 `CardCode` when proven | O3 query/protocol visibility | Public or legitimate perspective-private fact | Current knowledge only | Optional presence | PROVEN_EXISTING_SOURCE | I6C2 | Omit unknown; never reconstruct |
| `entity.owner?` | Optional absolute owner | I1 owner query/provenance is not emitted by I3 V1 cards | O3 card query | Public only when proven | Current state | Optional presence | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C2 | Reject invalid player/provenance |
| `entity.controller?` | Optional absolute controller | I1 address/controller mapping | O3 current field source | Public current fact | Current state | Optional presence | PROVEN_EXISTING_SOURCE | I6C2 | Reject invalid player |
| `entity.zone` for ordinary zones | Exact semantic zone | I1 zone/address plus I2 SZONE mapping | O3 zone projection | Public with layout proof | Current state/config | Entity locator order | PROVEN_DERIVED_FROM_PUBLIC_FACTS | I6C2 | Reject unknown/layout ambiguity |
| `entity.zone` for OVERLAY | Overlay zone only under proven parent/material relation | I1 `IsOverlay`, `OverlayRelations` | O3 overlay projection | Public only after relation/parent proof | Relation/current state | Entity locator order | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C3 | Reject unproven parent/continuity |
| `entity.sequence?` | Optional semantic sequence, not hidden physical identity | I1 address sequence; I2 uses code-group ordinals for pile cards | O3 sequence visibility rule | Own-Hand/current locator semantics differ | Current state | Entity locator order | SEMANTICS_AMBIGUOUS | I6C2 | Omit when not observable |
| `entity.overlay_sequence?` | Optional overlay position under proven parent | I1 overlay index/address and I2 overlay locator | O3 overlay traversal | Derived public positional fact when parent is proven | Current relation/state | Entity locator order | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C3 | Reject hidden continuity ambiguity |
| `entity.position` | Exact public position code, including Unknown | I1 position provenance | O3 query position | Public/known fact | Current state | Entity locator order | PROVEN_EXISTING_SOURCE | I6C2 | Reject unproven non-unknown value |
| `entity.face_up` | Position-derived face-up flag | I1 position plus local mapping | O3 card projection | Derived public fact | Current state | Entity record | PROVEN_DERIVED_FROM_PUBLIC_FACTS | I6C2 | Reject both face flags true |
| `entity.face_down` | Position-derived face-down flag | I1 position plus local mapping | O3 card projection | Derived public fact | Current state | Entity record | PROVEN_DERIVED_FROM_PUBLIC_FACTS | I6C2 | Reject both face flags true |
| `entity.printed?` | Static printed properties for known public identity | No Ignis accepted static-card-data source; I1 stores query facts, not printed records | O3 uses `CoreHost.static_card_data` | Must be perspective-safe and explicitly sourced | Current known identity | Property field order | NOT_AVAILABLE_FROM_PINNED_RUNTIME | I6C2 | Reject complete frame if required |
| `entity.current?` | Current public/proven dynamic properties | I1 query fields with provenance | O3 current query projection | Public/known facts only | Current state | Property field order | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C2 | Omit only when contract permits; never guess |
| `printed.type?` | Printed type bitset | No accepted Ignis printed source | O3 static data | Not derivable from protocol alone | Current known identity | Optional field | NOT_AVAILABLE_FROM_PINNED_RUNTIME | I6C2 | Reject if required |
| `printed.attribute?` | Printed attribute bitset | No accepted Ignis printed source | O3 static data | Not derivable from protocol alone | Current known identity | Optional field | NOT_AVAILABLE_FROM_PINNED_RUNTIME | I6C2 | Reject if required |
| `printed.race?` | Printed race bitset | No accepted Ignis printed source | O3 static data | Not derivable from protocol alone | Current known identity | Optional field | NOT_AVAILABLE_FROM_PINNED_RUNTIME | I6C2 | Reject if required |
| `printed.attack?` | Printed signed attack | No accepted Ignis printed source | O3 static data | Not derivable from protocol alone | Current known identity | Optional field | NOT_AVAILABLE_FROM_PINNED_RUNTIME | I6C2 | Reject if required |
| `printed.defense?` | Printed signed defense | No accepted Ignis printed source | O3 static data | Not derivable from protocol alone | Current known identity | Optional field | NOT_AVAILABLE_FROM_PINNED_RUNTIME | I6C2 | Reject if required |
| `printed.base_attack?` | Printed base attack if contract supplies it | No accepted Ignis printed source | O3 static data | Not derivable from protocol alone | Current known identity | Optional field | NOT_AVAILABLE_FROM_PINNED_RUNTIME | I6C2 | Reject if required |
| `printed.base_defense?` | Printed base defense if contract supplies it | No accepted Ignis printed source | O3 static data | Not derivable from protocol alone | Current known identity | Optional field | NOT_AVAILABLE_FROM_PINNED_RUNTIME | I6C2 | Reject if required |
| `printed.level?` | Printed level | No accepted Ignis printed source | O3 static data | Not derivable from protocol alone | Current known identity | Optional field | NOT_AVAILABLE_FROM_PINNED_RUNTIME | I6C2 | Reject if required |
| `printed.rank?` | Printed rank | No accepted Ignis printed source | O3 static data | Not derivable from protocol alone | Current known identity | Optional field | NOT_AVAILABLE_FROM_PINNED_RUNTIME | I6C2 | Reject if required |
| `printed.link_rating?` | Printed link rating | No accepted Ignis printed source | O3 static data | Not derivable from protocol alone | Current known identity | Optional field | NOT_AVAILABLE_FROM_PINNED_RUNTIME | I6C2 | Reject if required |
| `printed.link_markers[]` | Ordered marker enum codes | No accepted Ignis printed source | O3 static data | Not derivable from protocol alone | Current known identity | Sorted by enum code | NOT_AVAILABLE_FROM_PINNED_RUNTIME | I6C2 | Reject if required |
| `printed.left_scale?` | Printed left scale | No accepted Ignis printed source | O3 static data | Not derivable from protocol alone | Current known identity | Optional field | NOT_AVAILABLE_FROM_PINNED_RUNTIME | I6C2 | Reject if required |
| `printed.right_scale?` | Printed right scale | No accepted Ignis printed source | O3 static data | Not derivable from protocol alone | Current known identity | Optional field | NOT_AVAILABLE_FROM_PINNED_RUNTIME | I6C2 | Reject if required |
| `printed.status_flags?` | Printed status flags if defined | No accepted Ignis printed source | O3 static data | Not derivable from protocol alone | Current known identity | Optional field | NOT_AVAILABLE_FROM_PINNED_RUNTIME | I6C2 | Reject if required |
| `printed.counters[]` | Printed counters `(type,count)` | No accepted Ignis printed source | O3 static data | Not derivable from protocol alone | Current known identity | Sorted `(type,count)` | NOT_AVAILABLE_FROM_PINNED_RUNTIME | I6C2 | Reject if required |
| `current.type?` | Current type bitset | I1 query `QueryFlagV1.Type` when proven, omitted by I3 V1 | O3 query projection | Public/proven perspective fact after new source closure | Current state | Optional field | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C2 | Reject hidden/unproven value |
| `current.attribute?` | Current attribute | I1 query `Attribute` when proven, omitted by I3 V1 | O3 query projection | Public/proven perspective fact after new source closure | Current state | Optional field | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C2 | Reject hidden/unproven value |
| `current.race?` | Current race | I1 query `Race` when proven, omitted by I3 V1 | O3 query projection | Public/proven perspective fact after new source closure | Current state | Optional field | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C2 | Reject hidden/unproven value |
| `current.attack?` | Current signed attack | I1 query `Attack` when proven, omitted by I3 V1 | O3 query projection | Public/proven perspective fact after new source closure | Current state | Optional field | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C2 | Reject hidden/unproven value |
| `current.defense?` | Current signed defense | I1 query `Defense` when proven, omitted by I3 V1 | O3 query projection | Public/proven perspective fact after new source closure | Current state | Optional field | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C2 | Reject hidden/unproven value |
| `current.base_attack?` | Current base attack | I1 query `BaseAttack` when proven, omitted by I3 V1 | O3 query projection | Public/proven perspective fact after new source closure | Current state | Optional field | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C2 | Reject hidden/unproven value |
| `current.base_defense?` | Current base defense | I1 query `BaseDefense` when proven, omitted by I3 V1 | O3 query projection | Public/proven perspective fact after new source closure | Current state | Optional field | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C2 | Reject hidden/unproven value |
| `current.level?` | Current level | I1 query `Level` when proven, omitted by I3 V1 | O3 query projection | Public/proven perspective fact after new source closure | Current state | Optional field | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C2 | Reject hidden/unproven value |
| `current.rank?` | Current rank | I1 query `Rank` when proven, omitted by I3 V1 | O3 query projection | Public/proven perspective fact after new source closure | Current state | Optional field | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C2 | Reject hidden/unproven value |
| `current.link_rating?` | Current link rating | I1 query `Link` when proven, omitted by I3 V1 | O3 query projection | Public/proven perspective fact after new source closure | Current state | Optional field | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C2 | Reject hidden/unproven value |
| `current.link_markers[]` | Current marker values in canonical enum order | I1 `ModernQueryLinkPayloadV1`, omitted by I3 V1 | O3 query projection | Public/proven perspective fact after new source closure | Current state | Sorted enum code | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C2 | Reject unproven/malformed mapping |
| `current.left_scale?` | Current left scale | I1 query `LScale`, omitted by I3 V1 | O3 query projection | Public/proven perspective fact after new source closure | Current state | Optional field | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C2 | Reject hidden/unproven value |
| `current.right_scale?` | Current right scale | I1 query `RScale`, omitted by I3 V1 | O3 query projection | Public/proven perspective fact after new source closure | Current state | Optional field | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C2 | Reject hidden/unproven value |
| `current.status_flags?` | Current status flags | I1 query `Status`, omitted by I3 V1 | O3 query projection | Public/proven perspective fact after new source closure | Current state | Optional field | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C2 | Reject hidden/unproven value |
| `current.counters[]` | Current `(type,count)` values | I1 query `Counters`, omitted by I3 V1 | O3 query projection | Public/proven perspective fact after new source closure | Current state | Sorted `(type,count)` | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C2 | Reject unproven/malformed mapping |

The printed-property rows are deliberately `NOT_AVAILABLE_FROM_PINNED_RUNTIME`
for the current Ignis source, not an invitation to use BabelCDB, CardScripts,
CardCode lookup, decklists, archetypes, or prior revealed entities. A future
explicit public source may change that status only through a separately
reviewed source decision.

### 3.4 Relationships

OCGForge accepts exactly `XyzMaterial`, `Equip`, and `Target`, sorted by
`(kind, source.value, target.value)`. Ignis currently has four internal relation
lists and relation ordinals, but I3 V1 does not publish them.

| Relationship field | Required semantics | Ignis source | Pinned producer | Knowledge classification | Persistence/history needed | Ordering rule | Status | Owning future slice | Failure behavior |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `relationship.kind` | Exact OCGForge relationship enum | I1 relation lists are internal and absent from I3 V1 | O2/O3; `ApplyBecomeTarget`, `ApplyEquip`, overlay updates | Current relation fact requiring public source closure | Current state | Kind first | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C3 | Reject unknown/ambiguous kind |
| `relationship.source` | Current public locator of source | I1 internal `MirrorEntityIdV1` relation source; no public relation vector | O2 requires locator string | Requires current exact entity mapping | Current state | Kind/source/target | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C3 | Reject hidden/missing/ambiguous mapping |
| `relationship.target` | Current public locator of target | I1 internal `MirrorEntityIdV1` relation target | O2 requires locator string | Requires current exact entity mapping | Current state | Kind/source/target | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C3 | Reject hidden/missing/ambiguous mapping |

Internal relation ordinals are not public order. The future source must use the
OCGForge tuple sort and must not expose `NextRelationOrdinal`.

### 3.5 Chain state

OCGForge requires `chain.length` and the authoritative ordered link vector. The
Ignis mirror stores chain size, card identity/description/status, and internal
targets, but it does not retain the triggering player in `ChainState` and does
not expose chain records through I3 V1.

| Chain field | Required semantics | Ignis source | Pinned producer | Knowledge classification | Persistence/history needed | Ordering rule | Status | Owning future slice | Failure behavior |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `chain.length` | Current chain length | I1 `Chains` plus `PendingChain` | O3 field chain size | Current derived fact | Current chain state | Scalar | PROVEN_DERIVED_FROM_PUBLIC_FACTS | I6C3 | Reject mismatch with links |
| `chain.links[]` | Complete ordered chain links | I1 current chain list only | O3 `field.chain` | Public current chain source incomplete | Current chain state | Authoritative vector order | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C3 | Reject partial chain |
| `link.index` | Exact link index | I1 `ChainSize` is not retained as the same field shape | O1/O2 chain link index | Mapping not proven | Chain history | Vector order | SEMANTICS_AMBIGUOUS | I6C3 | Reject until native vector proves mapping |
| `link.activating_player?` | Optional absolute activating player | Input `GameplayChainingPayloadV1.TriggeringController` is consumed but not stored in `ChainState` | O3 chain source | Public source exists at ingest, not retained | Chain event history | Link order | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C3 | Reject if missing when required |
| `link.source?` | Optional current public source locator | I1 internal pending/card entity ID and card code | O3 source locator | Requires exact current public mapping | Chain history/current entities | Link order | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C3 | Never use CardCode as locator |
| `link.activation_zone?` | Optional semantic activation zone | I1 address available before internal ID conversion | O3 zone projection | Layout/provenance dependent | Chain history/config | Link order | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C3 | Reject unproven layout |
| `link.effect_description?` | Optional exact description ID | I1 `MirrorChainSnapshotV1.Description` is not emitted by I3 V1 | O3 chain description | Public only when source is known | Chain history | Link order | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C3 | Reject unproven value |
| `link.targets[]` | Complete target locator vector | I1 internal target IDs and relation lists | O3 target relations | Requires public mapping and history | Chain history/current entities | OCGForge target sort | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C3 | Reject missing/ambiguous target |

Current-chain state is not a substitute for historical chain events. A source
that only inspects the current board cannot recreate resolved links or their
past targets.

### 3.6 Visible event ledger and event fields

OCGForge `VisibleEventKind` has 23 enum values including `Unknown` (which is a
validation value, not a valid emitted semantic event) and 22 named semantic
events. Enum completeness is separate from producer reachability under a
particular pinned runtime profile. The event projector at O4 proves that one
message can produce multiple events:
`MSG_DRAW` can emit `Draw` plus `CardRevealed` events, and shuffle messages emit
`Shuffle` plus `RandomizationBoundary`. Unsupported message families emit no
event. Ignis currently has no persistent event ledger.

| Event kind | Exact pinned EDOPro message/source candidates | Current Ignis tracking | Visibility / history rule | Status | Future slice | Failure behavior |
| --- | --- | --- | --- | --- | --- | --- |
| `Unknown` | No admitted producer; O2 rejects unknown event code | None | Must never be emitted | OUTSIDE_I6C | I6C4 | Reject unknown event |
| `TurnStarted` | `MSG_NEW_TURN` | I3 decodes/applies current turn only | Emit from typed public player field | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C4 | Reject invalid player/order |
| `PhaseChanged` | `MSG_NEW_PHASE` | I3 decodes/applies current phase only | Emit exact phase value | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C4 | Reject malformed phase |
| `CardMoved` | `MSG_MOVE` when no destroy/banish/return specialization applies | I3 applies movement, discards reason from snapshot history | Preserve from/to and visibility at event time | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C4 | Reject if event source not complete |
| `CardRevealed` | `MSG_DRAW` reveal branch and `MSG_CONFIRM_CARDS`, `MSG_CONFIRM_DECKTOP`, `MSG_CONFIRM_EXTRATOP` | Confirm variants not admitted; query state has no ledger | Reveal only exact acting-perspective-visible code | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C4 | Omit hidden code; reject missing required field |
| `Summoned` | `MSG_SUMMONING`, `MSG_SPSUMMONING`, `MSG_FLIPSUMMONING` and corresponding completion signals | Not all variants admitted by I3 decoder | Preserve exact message-to-event multiplicity | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C4 | Reject unsupported producer form |
| `Set` | `MSG_SET` | I3 decodes set but state ownership is movement/query | Presentation event must not double-mutate state | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C4 | Reject duplicate semantic application |
| `Draw` | `MSG_DRAW` | I3 decodes draw records | Emit one draw event with exact count | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C4 | Reject count/body mismatch |
| `Shuffle` | `MSG_SHUFFLE_DECK`, `MSG_SHUFFLE_HAND`, `MSG_SHUFFLE_EXTRA`, `MSG_SHUFFLE_SET_CARD`, `MSG_REVERSE_DECK` | Not admitted by I3 decoder | Identity-destroying boundary; no hidden codes | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C4 | Reject unknown shape; destroy stale continuity |
| `RandomizationBoundary` | Derived only from an accepted shuffle event | None | Paired semantic event, not invented from board change | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C4 | Emit only with source shuffle |
| `LifePointsChanged` | `MSG_LPUPDATE`, `MSG_DAMAGE`, `MSG_RECOVER`; `MSG_PAY_LPCOST` is deferred/not copied by the frozen event oracle | I3 decodes/applies LP, including a typed PayLpCost message that is not an admitted event source | Preserve signed amount and player only for the three admitted producers | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C4 | Reject deferred/unsupported event form |
| `ChainActivated` | `MSG_CHAINING`, `MSG_CHAINED` | I3 stores current chain but no history | Preserve link order/player/source/description when visible | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C4 | Reject incomplete link |
| `ChainResolved` | `MSG_CHAIN_SOLVING`, `MSG_CHAIN_SOLVED` | I3 stores status only | Preserve semantic resolution event | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C4 | Reject order/status mismatch |
| `ChainEnded` | `MSG_CHAIN_END` | I3 clears chain state | Emit before history is discarded | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C4 | Reject lost terminal chain event |
| `CardDestroyed` | `MSG_MOVE` with proven destroy reason into Graveyard | I3 applies move but discards reason history | Reason-dependent specialization must be recorded at ingest | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C4 | Reject missing reason/source |
| `CardBanished` | `MSG_MOVE` into Banished | I3 applies move only | Preserve to-zone and visible identity | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C4 | Reject incomplete move |
| `CardReturned` | `MSG_MOVE` from Graveyard/Banished | I3 applies move only | Preserve from/to zones | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C4 | Reject incomplete move |
| `PositionChanged` | `MSG_POS_CHANGE` | I3 applies current position | Preserve old/new public semantics | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C4 | Reject missing old/new |
| `CounterChanged` | `MSG_ADD_COUNTER`, `MSG_REMOVE_COUNTER` | Not admitted by I3 decoder | Preserve counter type and signed/unsigned meaning | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C4 | Reject unsupported counter form |
| `Equipped` | `MSG_EQUIP` | I3 applies current equipment relation | Preserve source/target public locators | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C4 | Reject hidden/ambiguous relation |
| `Unequipped` | `MSG_UNEQUIP` parser-compatibility form (two locations; no producer in the current pinned runtime) | I3 removes the current equipment relation and retains an I3 wire-compatibility decoder | The I6C4-certified source must not admit an unproven 95 packet or synthesize the historical target | NOT_AVAILABLE_FROM_PINNED_RUNTIME | I6C4 profile classification | I6C4 source admission fails closed; the I3 compatibility decoder may remain |
| `Targeted` | `MSG_BECOME_TARGET`, `MSG_CARD_TARGET`, `MSG_CANCEL_TARGET` | I3 applies current target relations | Preserve exact event action and visible targets | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C4 | Reject hidden/ambiguous target |
| `Win` | `MSG_WIN` | I3 applies terminal snapshot | Preserve winner/win reason at event time | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | I6C4 | Reject invalid terminal payload |

The event-kind table is complete for the OCGForge enum, but it is not a claim
that every enum member has a reachable producer under every runtime pin. I6C4
must close every source-backed producer reachable under the exact pinned
runtime profile and must explicitly classify enum members that are unreachable.
For the current profile, `MSG_UNEQUIP` is such an enum member: the OCGForge
parser accepts a two-location compatibility shape, while the pinned runtime
has no producer. The existing Ignis one-location decoder remains an I3 wire
compatibility path and is not I6C4 source authority.

The event field matrix is:

| Event field | Required source rule | Current Ignis source | Status | Failure behavior |
| --- | --- | --- | --- | --- |
| `event_index` | Monotonic semantic event index assigned once per emitted event, starting at zero for a fresh perspective stream; never engine step/TCP index | No ledger | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | Reject if event sequence cannot be reconstructed |
| `kind` | Exact OCGForge enum code | Message-to-kind mapping exists only in external provenance | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | Reject unknown/ambiguous mapping |
| `player?` | Optional absolute player only when source carries/proves it | Typed payloads for some messages | PROVEN_DERIVED_FROM_PUBLIC_FACTS | Omit only when absent; reject invalid player |
| `entity?` | Optional public locator at event time | I1 address/current entity, no history | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | Never bind historical slot to current entity |
| `public_passcode?` | Optional code only when visible to perspective | I1 query/move payload at ingest, no event retention | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | Hidden code absent; no lookup |
| `from_zone?` | Optional exact semantic source zone | `MSG_MOVE` payload; I1 discards event history | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | Reject unproven layout |
| `to_zone?` | Optional exact semantic destination zone | `MSG_MOVE`/typed address | PROVEN_DERIVED_FROM_PUBLIC_FACTS | Reject unproven layout |
| `count?` | Optional exact unsigned count | Draw/chain payloads for supported messages | PROVEN_DERIVED_FROM_PUBLIC_FACTS | Reject body/count mismatch |
| `amount?` | Optional signed i32 amount | LP payloads; counter events unsupported | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | Reject signedness ambiguity |
| `counter_type?` | Optional exact counter type | Counter messages not admitted | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | Reject unsupported source |
| `phase?` | Optional exact phase value | `MSG_NEW_PHASE` | PROVEN_EXISTING_SOURCE | Reject malformed value |
| `winner?` | Optional absolute winner | `MSG_WIN` | PROVEN_EXISTING_SOURCE | Reject invalid player |
| `win_reason?` | Optional exact reason/type | `MSG_WIN.WinType`, naming mapping not frozen | SEMANTICS_AMBIGUOUS | Reject until mapping proof |
| `effect_description?` | Optional exact effect ID | Chain payload stores description | PROVEN_EXISTING_SOURCE | Reject unproven value |
| `targets[]` | Complete public locator vector, OCGForge target sort | I1 internal relations/current target payloads, no event history | REQUIRES_NEW_PERSPECTIVE_SAFE_TRACKER | Reject hidden/missing/ambiguous target |

Optional presence is semantic. `ABSENT` is not `PRESENT(0)`, and no source may
populate an absent event field merely because a default value is available.

### 3.7 Match context

The OCGForge match context is configuration/knowledge policy, not a running
inference about what cards have happened to be revealed.

| Match-context field | Required semantics | Ignis source | Status | Owning future slice | Failure behavior |
| --- | --- | --- | --- | --- | --- |
| `perspective_player` | Same absolute perspective as safe state | I1 `GameplayPerspectiveV1.PlayerType` | PROVEN_EXISTING_SOURCE | I6C5 | Reject mismatch |
| `duel_flags` | Exact configured flags used for layout | I2 projection context only | REQUIRES_EXPLICIT_RUNTIME_CONFIGURATION | I6C5 | Reject missing/mismatch |
| `knowledge.own_decklist_known` | Explicit policy/configuration bit | No current match config | REQUIRES_EXPLICIT_RUNTIME_CONFIGURATION | I6C5 | Reject inferred value |
| `knowledge.opponent_decklist_known` | Explicit policy/configuration bit | No current match config | REQUIRES_EXPLICIT_RUNTIME_CONFIGURATION | I6C5 | Do not infer from reveals |
| `own_deck.known` | Explicit known-deck state | Start message has only counts | REQUIRES_EXPLICIT_RUNTIME_CONFIGURATION | I6C5 | Reject missing list when known |
| `own_deck.main_deck[]` | Exact configured passcodes, sorted by OCGForge rule | No current list source | REQUIRES_EXPLICIT_RUNTIME_CONFIGURATION | I6C5 | Reject absent/unsorted known list |
| `own_deck.extra_deck[]` | Exact configured passcodes, sorted by OCGForge rule | No current list source | REQUIRES_EXPLICIT_RUNTIME_CONFIGURATION | I6C5 | Reject absent/unsorted known list |
| `opponent_deck.known` | Explicit known-deck policy, never reveal-derived | No current list source | REQUIRES_EXPLICIT_RUNTIME_CONFIGURATION | I6C5 | Reject inferred true |
| `opponent_deck.main_deck[]` | Exact configured passcodes only when known | No current list source | NOT_AVAILABLE_FROM_PINNED_RUNTIME | I6C5 | Unknown deck must have empty vector |
| `opponent_deck.extra_deck[]` | Exact configured passcodes only when known | No current list source | NOT_AVAILABLE_FROM_PINNED_RUNTIME | I6C5 | Unknown deck must have empty vector |

### 3.8 Outer public observation fields

| Outer field | Required semantics | Ignis source | Status | Owning future slice | Failure behavior |
| --- | --- | --- | --- | --- | --- |
| `perspective_player` | Absolute public perspective | I1 perspective | PROVEN_EXISTING_SOURCE | I6C6 | Reject mismatch |
| `decision_index` | OCGForge public decision sequence, not prompt ordinal | OCGForge environment/decision integration owns it; no accepted Ignis equivalent | OUTSIDE_I6C | I6D | Do not substitute prompt ordinal |
| `canonical_safe_state_bytes` | OCGForge-owned bytes from complete safe source | Current I3 V1 bytes are narrower and incompatible | BLOCKED by missing fields | I6C6 | No empty/default bytes |
| `public_decision_context.kind?` | Optional admitted request-family token | OCGForge decision integration derives it from the public request; I4/I5 values are not an I6C state source | OUTSIDE_I6C | I6D | Copy only accepted public kind |
| `public_decision_context.player?` | Optional acting player | OCGForge decision integration derives it from the public request | OUTSIDE_I6C | I6D | Reject invalid player |
| `public_decision_context.referenced_entities[]` | Sorted safe locator tokens only | OCGForge decision integration supplies request references; current prompt references/private continuation are not an I6C state source | OUTSIDE_I6C | I6D | Reject hidden/private references |
| `public_observation_digest` | OCGForge SHA-256 of outer canonical bytes | No complete input to hash | PROVEN_DERIVED_FROM_PUBLIC_FACTS only after closure | I6C6 | No digest on failed projection |

`public_decision_context` is not allowed to carry continuation IDs, private
prompt ordinals, raw prompt bytes, private response bindings, or internal
decision IDs. Prompt-local CardCode remains the separate known I6 blocker and
is not changed by I6C0.

## 4. Architecture decision

### 4.1 Selected architecture: A, with one transactional source owner

```text
extend private MirrorState facts and transaction metadata
        +
append a perspective-safe event ledger in the same successful Apply transaction
        +
compose a NEW versioned public-frame source
        +
leave PublicStateSnapshotV1 and its bytes/identity untouched
```

The future source module should be a deep module with a small interface: a
caller supplies an accepted perspective/configuration and receives either one
complete immutable public frame or one structured fail-closed result. The
implementation may use internal source facts, event records, and validation
helpers, but those are not public interfaces.

The critical transaction rule is:

```text
decode typed message
→ clone/apply mirror candidate
→ validate mirror candidate
→ append exactly the corresponding public-event records
→ validate source closure
→ commit mirror + ledger together
```

Any decode, mirror, visibility, relation, chain, event, or source-closure
failure leaves neither a new public frame nor a partially committed event.

### 4.2 Rejected alternatives

| Option | Decision | Reason |
| --- | --- | --- |
| B: parallel perspective-safe tracker/reducer | Rejected | It duplicates movement, relation, chain, and visibility interpretation and can diverge from the accepted Mirror transaction. A separate ledger may exist as an internal data structure, but it must be fed by the one successful reducer transaction rather than independently reinterpreting the stream. |
| C: external accepted OCGForge-compatible frame source | Rejected for current work | No available accepted external source is present in Ignis. OCGForge's `PlayerObservation` builder is an upstream authority, not a runtime feed available to this adapter. |
| Modify I3 V1 in place | Forbidden | `ocgforge-ignis.public-state-projection.v1`, its canonical bytes, golden vectors, and identity are frozen. |

Architecture A does not claim that all fields are currently sourced. It is the
least-duplicative place to close the missing source gaps once each gap receives
its own authorization and tests.

### 4.3 Versioning rule

The future source may propose a new Ignis source contract/version, for example
`ocgforge-ignis.public-frame-source.v1`, but this token is not accepted or
frozen by I6C0. It is a provenance contract for the source seam, not a
replacement for either OCGForge semantic identity and not a second safe-state
codec. OCGForge continues to own `public_safe_state.v1` and
`public_environment_observation.v1` bytes.

If a later source change alters meaning, it requires a new source version or a
new OCGForge mapping decision. It may not silently extend I3 V1.

## 5. Locator and identity rules

The existing `PublicSemanticLocatorV1` remains the Ignis locator authority for
current public facts. It is not a physical-card identity. The future mapping
must distinguish:

```text
current entity reference
    = frame-local public locator token plus optional exact current ordinal

historical event reference
    = locator token captured at event time, never current-entity rebound

chain reference / relationship reference
    = current public locator only after exact source/target proof

prompt-local disclosure
    = current decision value with prompt lifetime only
```

The future OCGForge token table follows OCGForge's unsigned UTF-8 ordering and
frame-local ordinal rule. Equal token strings may share an ordinal because the
ordinal means token equality only. A zone/sequence match, CardCode, collection
order, `MirrorEntityIdV1`, or internal relation ordinal is not proof of physical
continuity.

If a historical locator cannot be retained without hidden continuity, the
event/frame fails closed. It is never rebound to the current slot occupant.

## 6. Visible-event index and ordering design

The source-backed future rule is:

```text
event_index starts at 0 for a fresh perspective-visible source stream
for each accepted typed protocol semantic message, in message order:
    emit the exact source-backed event sequence for that message
    assign the next consecutive index to each emitted event
commit the next index only with the successful mirror transaction
```

This mirrors OCGForge `ObservationSession::next_event_index_` and its
`project_visible_events` emission order, without exposing
`engine_step_index`. The index is not a TCP frame number, receive-chunk number,
task ordinal, object allocation order, or wall-clock value. If a message's
semantic event multiplicity or ordering is not source-proven, the frame fails
closed rather than assigning an arbitrary index.

Required canonical ordering at the future public-source seam:

```text
zones          = OCGForge zone tuple order
entities       = ordinal UTF-8 locator value
relationships  = (kind, source locator, target locator)
chain links    = authoritative chain.links order
chain targets  = OCGForge target ordering
events         = strictly increasing event_index
event targets  = OCGForge target ordering
deck passcodes  = OCGForge explicit ascending order
```

No `Dictionary`, `HashSet`, filesystem order, or reflection order may define a
semantic vector. The underlying Mirror may use dictionaries internally, but the
source output must be explicit and validated before publication.

## 7. Privacy and paired-world design

The source tracker is allowed to retain private diagnostics internally, but its
public frame must be computed only from accepted perspective-safe facts. It
must not contain raw `GAME_MSG` bytes, raw query payloads, private response
bytes, socket state, host/port/password, PID, process identity, object address,
`MirrorEntityIdV1`, or adapter continuation state.

The required paired-world matrix is:

| Pair | Hidden-only difference | Values that MUST remain equal | Required privacy rule |
| --- | --- | --- | --- |
| A | Opponent hidden Hand identities | safe state, zone counts, unknown entity rows, locators, event values, outer bytes/digest | Hidden hand identities never enter the source ledger or public frame. |
| B | Opponent Main Deck identities/order | safe state, Main Deck count, all public entities/events, outer values | Main Deck is count-only unless explicit configuration says a decklist is known; order is never inferred. |
| C | A revealed identity later becomes hidden | post-boundary unknown representation, valid counts, future events, locators/outer values | Knowledge-destroying transition removes stale identity/locator continuity. |
| D | Equal-code cards with different private histories | equal public values produce equal public values; distinct current public slots retain distinct semantic locators | Internal history and Mirror IDs cannot choose public identity or order. |
| E | Same semantic message history with different TCP chunking | event sequence/index, state, locators, outer bytes/digest | Chunking is transport detail; typed semantic processing must be chunk-invariant. |
| F | Opponent hidden Extra Deck identities | counts and all perspective-safe state/events/outer values | Unknown Extra Deck identity is absent, not catalog-looked-up or inferred. |

Any hidden-only mutation that changes a proposed public value is a BLOCKER. A
public reveal is different: once the protocol explicitly reveals a value to the
acting perspective, it may be recorded as a public event fact with prompt/event
lifetime, but it does not create persistent physical continuity.

## 8. Replay and audit implications

The future source must support first-divergence analysis without promoting raw
transport into public semantics. A private audit record may retain:

```text
accepted semantic message ordinal
typed message kind
source transaction result
emitted event_index range
public redaction/knowledge classification
source-closure diagnostic code
```

The public audit record may retain only event indexes, public event kinds,
public locators, and structured outcomes. It must not retain raw bytes or
private identifiers.

The minimum replay questions are:

```text
which accepted typed semantic input first differed?
which perspective-safe source fact first differed?
which public-state field or event first differed?
which event_index was first divergent?
```

A private protocol trace can answer transport diagnostics separately. It cannot
be included in the canonical public frame to make debugging easier.

## 9. Current blockers and boundaries

```text
I6_PUBLIC_STATE_ORACLE=BLOCKED
I6_EVENT_ORACLE=BLOCKED
I6_EVENT_INDEX_SOURCE=BLOCKED
I6_PROMPT_LOCAL_CARDCODE_MAPPING=BLOCKED   # outside I6C0
I6_RULES_DOMAIN_COMPATIBILITY=DIFFERENT_OR_UNPROVEN
```

The two I6C0 blockers are:

1. no accepted Ignis source currently contains the complete OCGForge safe-state
   field surface, including complete zones, entity property records, relation
   locator pairs, chain link metadata, and explicit match configuration; and
2. no accepted Ignis visible-event ledger currently preserves the source-backed
   event sequence, event multiplicity, historical locators, optional fields,
   and semantic event indexes.

These are source-closure findings, not permission to synthesize data from the
private mirror or current board. The prompt-local CardCode gap and rules/runtime
compatibility gap remain unchanged and outside this task.

### 9.1 Runtime-profile reachability resolution for `MSG_UNEQUIP`

The semantic enum member `Unequipped` remains valid OCGForge vocabulary, but
the current pinned runtime profile has no source-backed producer for
`MSG_UNEQUIP` (message id 95). The OCGForge event projector's two-location
case and Ignis' one-location decoder are therefore compatibility/parser
surfaces, not evidence that the current runtime can emit the event.

The I3 decoder may continue accepting its historical one-location form for
wire compatibility. That acceptance must not promote the packet into the
I6C4-certified event source. An unexpected `MSG_UNEQUIP` under this profile
must fail I6C4 source admission closed:

```text
MUST NOT be accepted as a source-backed Unequipped event
MUST NOT synthesize a target from current or prior mirror state
MUST NOT change the I3 compatibility parser contract
```

I6C4 acceptance therefore requires reachable-producer closure plus an explicit
unreachable-enum classification; it does not require a reachable producer for
every enum member.

## 10. Future acceptance evidence

I6C source closure cannot be accepted from compilation, current-state snapshots,
one event, one public card, or a shape-only comparison. The future evidence
must include:

```text
complete field matrix with no BLOCKED constituent
typed source transaction atomicity
native OCGForge safe-state oracle equality
native event multiplicity/order/index equality
historical-locator non-rebinding witness
relationship and chain source witnesses
explicit match-context configuration witness
paired-world A-F equality
TCP-chunking equality
fresh-process byte/value determinism
malformed/unknown/hidden-source fail-closed cases
I3 V1 golden-byte regression unchanged
```

The evidence is sufficient for `I6C_SOURCE_CLOSURE=PASS` only when every
required source field is either proven or deliberately excluded by an accepted
OCGForge scope decision. It is insufficient if any target field is silently
omitted, defaulted, inferred, or represented by current-board reconstruction.

The native safe-state oracle has two explicit scopes:

```text
I6C state-only safe-state equality
    = eligible only when player_to_act is ABSENT
      and no decision context is attached

decision-boundary safe-state equality
    = BLOCKED_PENDING_I6D
      because OCGForge attaches player_to_act from the public DecisionContext
```

`ABSENT` here is a semantic OCGForge value, not an assertion that Ignis failed
to observe a required player. A decision-boundary vector must not be converted
to the state-only vector by dropping that field.

## 11. I6C0 status

```text
SOURCE_AUDIT=COMPLETE_FOR_CURRENT_PIN
FIELD_PROVENANCE_MATRIX=COMPLETE
EVENT_KIND_MATRIX=COMPLETE
I3_V1_NON_INTERFERENCE=PASS
ARCHITECTURE_A_SELECTED=YES
SOURCE_CLOSURE=BLOCKED
NEW_CONTRACT_FILE=NOT_CREATED_DUE_SOURCE_BLOCKERS

GLOBALS_SOURCE=BLOCKED
ZONES_SOURCE=BLOCKED
ENTITIES_SOURCE=BLOCKED
RELATIONSHIPS_SOURCE=BLOCKED_PENDING_LOCATOR_CLOSURE
CHAIN_SOURCE=BLOCKED
VISIBLE_EVENTS_SOURCE=BLOCKED
EVENT_INDEX_SOURCE=BLOCKED
MATCH_CONTEXT_SOURCE=BLOCKED_PENDING_EXPLICIT_CONFIGURATION
OUTER_OBSERVATION_CONTEXT_SOURCE=OUTSIDE_I6C
I6C_STATE_GLOBALS_SOURCE=BLOCKED
I6C_PLAYER_TO_ACT_SOURCE=OUTSIDE_I6C_PENDING_I6D
I6_DECISION_BOUNDARY_SAFE_STATE_BYTES=BLOCKED_PENDING_I6D

PROMPT_LOCAL_CARDCODE_BLOCKER_CHANGED=NO
RULES_RUNTIME_COMPATIBILITY_CHANGED=NO
I6D_AUTHORITY_INTRODUCED=NO
I7_AUTHORITY_INTRODUCED=NO

I6C_RUNTIME_AUTHORIZED=NO
I6C_FINAL=NO
```
