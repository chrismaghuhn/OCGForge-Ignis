# I6C Public-State and Visible-Event Source Closure Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Close the blocked Ignis source seam for the complete OCGForge public safe state and visible-event ledger without changing I3 V1 bytes or introducing I6D/I7 authority.

**Architecture:** Extend the existing transactional `PerspectiveStateMirrorV1` implementation with source facts and an event ledger that commit atomically with a successful typed-message application. Expose a new versioned perspective-safe public-frame source seam; leave `PublicStateSnapshotV1`, its canonical bytes, and its identity unchanged. OCGForge remains the semantic and canonical-byte owner.

**Tech Stack:** Existing C#/.NET 10 Ignis Gameplay code, immutable/read-only value types, pinned EDOPro message facts, explicit source provenance, and native OCGForge oracle vectors. No model framework, network response, checkpoint, inference, or OCGForge modification.

---

## 1. Current-slice boundary and future file map

I6C0 itself creates only these two documents:

```text
CREATE docs/superpowers/specs/2026-09-06-i6c0-public-state-event-source-design.md
CREATE docs/superpowers/plans/2026-09-06-i6c-public-state-event-source-plan.md
```

No new source contract is created because the complete public-state/event
source is currently blocked. A later contract may be proposed only after the
field matrix has no blocked required constituent.

The future production/test file map is intentionally explicit:

| Future file | Responsibility |
| --- | --- |
| `src/OCGForge.Ignis.Gameplay/PerspectiveSafeFrameSourceTypesV1.cs` | Immutable source-side globals, zones, entities, relations, chain, events, match context, and structured failure values. It is not an OCGForge byte codec. |
| `src/OCGForge.Ignis.Gameplay/PerspectiveSafeEventLedgerV1.cs` | Atomic semantic event records and monotonic event-index allocation. It receives typed source facts; it does not parse raw buffers independently. |
| `src/OCGForge.Ignis.Gameplay/PerspectiveSafePublicFrameSourceV1.cs` | Deep public source module: validates complete source closure and returns one immutable frame or fail-closed result. |
| `src/OCGForge.Ignis.Gameplay/PerspectiveSafeMatchContextV1.cs` | Explicit duel-layout/knowledge/deck configuration; no reveal-derived deck knowledge. |
| `src/OCGForge.Ignis.Gameplay/PerspectiveStateMirrorV1.cs` | Modify only to retain required source facts and commit the source ledger in the same successful transaction. Existing V1 behavior and snapshot bytes remain unchanged. |
| `src/OCGForge.Ignis.Gameplay/GameplayMessageTypesV1.cs` | Modify only if a currently decoded typed message needs a missing source field preserved; no raw payload becomes public. |
| `src/OCGForge.Ignis.Gameplay/GameplayMessageDecoderV1.cs` | Add narrowly typed source messages only for pinned event forms required by the accepted matrix; unsupported forms remain fail closed. |
| `src/OCGForge.Ignis.Gameplay/GameplayMirrorSessionV1.cs` | Feed successful typed applications to the source ledger exactly once; no public source on failed application. |
| `tests/OCGForge.Ignis.Gameplay.Tests/Tests/I6CPublicFrameSourceTests.cs` | Focused source, privacy, ordering, history, and I3 V1 regression tests. |
| `tests/OCGForge.Ignis.Gameplay.Tests/Program.cs` | Register only the accepted I6C test groups for the current slice. |
| `tests/OCGForge.Ignis.Gameplay.Tests/Tests/I6CNativeOracleTests.cs` | Future native OCGForge safe-state/event oracle comparison after an accepted oracle-vector import boundary exists. |
| `fixtures/model/v1/i6c-public-frame-vectors.v1.json` | Future generated evidence only, emitted by an OCGForge-owned/native oracle; never hand-authored from Ignis output. |

No future slice may modify OCGForge, I3 V1 canonical bytes, I4/I5 candidate
semantics, prompt-local CardCode mapping, public action keys, LogicalModelInput,
EncodedModelInput, Task7 materialization, checkpoint loading, or network sends.

## 2. Future slice sequence

Every slice below is separately authorized, red-tested, committed, pushed, and
stopped for independent review. A passing slice never self-authorizes the next.

### Task I6C1: Source value types and fail-closed container

**Owning layer:** `OCGForge.Ignis.Gameplay`, new source module.

**Files:**

- Create `src/OCGForge.Ignis.Gameplay/PerspectiveSafeFrameSourceTypesV1.cs`.
- Create `src/OCGForge.Ignis.Gameplay/PerspectiveSafePublicFrameSourceV1.cs`.
- Create `tests/OCGForge.Ignis.Gameplay.Tests/Tests/I6CPublicFrameSourceTests.cs`.
- Modify `tests/OCGForge.Ignis.Gameplay.Tests/Program.cs` only for the first I6C group.

**Semantic work:**

- Define immutable/read-only source values for every safe-state section listed in the I6C0 matrix.
- Define structured source errors without raw bytes, paths, Mirror IDs, or private response data.
- Make the source result either a complete immutable frame or no frame; no partial public state.
- Keep the source-side value distinct from OCGForge canonical safe-state bytes.

**Red test and focused gates:**

- A failing test constructs a missing-globals/missing-events source and proves no partial frame is accepted.
- Valid immutable values survive caller collection mutation.
- Unknown enum, invalid optional presence, duplicate locator, and duplicate event-index values fail closed.
- No reference to Protocol, Client, private response bindings, prompt continuation state, model frameworks, or OCGForge is introduced.

**Privacy/determinism/replay:**

- Public types contain semantic values only.
- All lists are copied and exposed read-only.
- No ordering is derived from a map or allocation order.
- Error tokens are stable and omit operational/private metadata.

**Stop boundary:** Stop if the types require a new meaning for an OCGForge field,
an I3 V1 extension, or any prompt-local/private value.

### Task I6C2: Globals, zones, entities, and locator source closure

**Owning layer:** Existing transactional Mirror plus new public-frame source.

**Files:**

- Modify `src/OCGForge.Ignis.Gameplay/PerspectiveStateMirrorV1.cs` for missing atomic source facts.
- Create/modify `src/OCGForge.Ignis.Gameplay/PerspectiveSafeFrameSourceTypesV1.cs`.
- Create/modify `src/OCGForge.Ignis.Gameplay/PerspectiveSafePublicFrameSourceV1.cs`.
- Extend `tests/OCGForge.Ignis.Gameplay.Tests/Tests/I6CPublicFrameSourceTests.cs`.

**Semantic work:**

- Close the gameplay-state globals (life points, turn player/count, phase, terminal, and proven terminal values) without aliases; `duel_flags` remains explicit I6C5 configuration and `player_to_act` remains outside I6C under I6D.
- Close ordinary OCGForge zones and layout-derived field/pzone values only where their source is explicit and proven.
- Do not claim overlay/Xyz-dependent zone or entity closure in I6C2; leave those constituents fail-closed pending the relation/material proof owned by I6C3.
- Preserve total/public/hidden counts and observable-order values per zone.
- Convert current entity facts to public locators and optional values under I3 knowledge rules.
- Never produce a Main Deck entity or hidden unknown locator.
- Leave printed properties blocked unless an independently accepted perspective-safe source exists; do not use card databases.

**Red test and focused gates:**

- each global field mutation changes exactly its public source value;
- field/pzone layout ambiguity fails closed;
- overlay/Xyz-dependent values remain blocked rather than being fabricated before I6C3;
- counts and hidden/public populations remain exact for both perspectives;
- current locators are unique and stable under dictionary/container reorder;
- knowledge-destroying transitions remove stale identity;
- the existing I3 V1 golden bytes and IDs remain byte-identical.

**Privacy/determinism/replay:**

- Run paired worlds A-F for state values.
- Use explicit zone/entity sorting rules from OCGForge.
- Keep source snapshots immutable and value-owned.
- Retain private source diagnostics separately from public frame values.

**Stop boundary:** Stop on any required printed/current field whose source is
not accepted, any hidden-card leakage, any attempt to claim overlay/Xyz
closure before I6C3, or any proposal to alter `PublicStateProjectionV1.cs` V1
encoding.

### Task I6C3: Relationship and chain source closure

**Owning layer:** Existing Mirror relation/chain transaction plus public-frame source.

**Files:**

- Modify `src/OCGForge.Ignis.Gameplay/PerspectiveStateMirrorV1.cs` to retain missing chain metadata at successful ingest.
- Modify `src/OCGForge.Ignis.Gameplay/GameplayMessageTypesV1.cs` if triggering-player/source fields are missing from stored typed values.
- Modify `src/OCGForge.Ignis.Gameplay/GameplayMirrorSessionV1.cs` only for atomic propagation.
- Extend `src/OCGForge.Ignis.Gameplay/PerspectiveSafeFrameSourceTypesV1.cs` and `PerspectiveSafePublicFrameSourceV1.cs`.
- Extend `tests/OCGForge.Ignis.Gameplay.Tests/Tests/I6CPublicFrameSourceTests.cs`.

**Semantic work:**

- Close the overlay/Xyz-dependent zone and entity constituents deliberately
  left blocked by I6C2, but only after the current public locator seam and the
  parent/material source are accepted.
- Map internal target/equipment/overlay relation facts to OCGForge `Target`, `Equip`, and `XyzMaterial` only when both public endpoints are proven.
- Preserve exact current relation removal/retargeting semantics.
- Preserve complete chain link order, triggering player, source, activation zone, description, and targets.
- Reject chain links/targets that cannot be tied to a public locator without hidden continuity.
- Keep relation ordinals and Mirror IDs private.

**Red test and focused gates:**

- one witness per relationship kind with distinct source/target locators;
- relation removal and retargeting events preserve the correct current state;
- a chain target disappearing causes fail-closed source output, not silent target deletion;
- chain link order is preserved and not sorted;
- unknown activation player/source/zone fails closed;
- paired worlds with hidden-only relation differences remain equal.

**Stop boundary:** Stop on any chain field whose `ChainSize` mapping remains
ambiguous, any relation endpoint that is hidden/ambiguous, or any requirement
to derive relation meaning from card text or board plausibility.

### Task I6C4: Visible-event ledger and event-index closure

**Owning layer:** Existing Gameplay decoder/session plus the new internal event ledger.

**Files:**

- Create `src/OCGForge.Ignis.Gameplay/PerspectiveSafeEventLedgerV1.cs`.
- Modify `src/OCGForge.Ignis.Gameplay/GameplayMessageTypesV1.cs` only for missing typed source fields.
- Modify `src/OCGForge.Ignis.Gameplay/GameplayMessageDecoderV1.cs` only for pinned event forms required by the accepted matrix.
- Modify `src/OCGForge.Ignis.Gameplay/GameplayMirrorSessionV1.cs` to submit successful typed messages once.
- Modify `src/OCGForge.Ignis.Gameplay/PerspectiveStateMirrorV1.cs` to commit ledger state atomically with Mirror state.
- Extend `tests/OCGForge.Ignis.Gameplay.Tests/Tests/I6CPublicFrameSourceTests.cs`.

**Semantic work:**

- Implement the source-backed mapping for all 22 emitted event kinds only after entity/locator and relation/chain closure has passed.
- Preserve OCGForge's one-message-to-zero/one/many event multiplicity.
- Allocate `event_index` from a monotonic semantic counter, starting at zero for a fresh source stream.
- Append events only after typed decode, mirror application, and mirror validation succeed.
- Preserve event-time public visibility and historical locators; never reconstruct from the current board.
- Keep unsupported or insufficiently typed messages fail closed.

**Red test and focused gates:**

- `MSG_DRAW` produces a draw event plus only the source-proven reveal events;
- accepted shuffle produces `Shuffle` and `RandomizationBoundary`, and destroys hidden continuity;
- malformed/unsupported confirm, summon, counter, or shuffle forms produce no event and no partial commit;
- a failed mirror application leaves event count and next index unchanged;
- the same semantic messages split at every TCP boundary produce identical event values/indexes.

**Privacy/determinism/replay:**

- Event codes/passcodes are redacted at event creation, not after public serialization.
- The ledger stores typed semantic fields, not raw buffers or internal object IDs.
- Event indexes depend only on successful semantic emission order.
- Private audit metadata remains a separate restricted record.

**Stop boundary:** Stop if a required event field exists only in raw private
bytes, if a historical locator cannot be retained safely, or if the source
would need a second independent reducer.

### Task I6C5: Match context and outer public-frame source

**Owning layer:** Gameplay source configuration and public-frame module.

**Files:**

- Create `src/OCGForge.Ignis.Gameplay/PerspectiveSafeMatchContextV1.cs`.
- Extend `src/OCGForge.Ignis.Gameplay/PerspectiveSafePublicFrameSourceV1.cs`.
- Extend `tests/OCGForge.Ignis.Gameplay.Tests/Tests/I6CPublicFrameSourceTests.cs`.

**Semantic work:**

- Require explicit duel flags and explicit deck/knowledge configuration.
- Keep opponent decklist knowledge false unless the runtime configuration explicitly supplies the complete list as known.
- Return a complete frame only when all safe-state/event constituents are present.
- Leave `decision_index` and `public_decision_context` to the separately owned I6D decision/environment integration; I6C5 must not read or mutate `FlatPromptSessionV1` continuation state.

**Red test and focused gates:**

- missing configuration rejects the whole frame;
- a known deck with an unsorted list rejects;
- an unknown deck with nonempty passcodes rejects;
- `PRESENT(0)` and `ABSENT` remain distinct;
- the frame source rejects any request that attempts to supply private continuation or response data.

**Privacy/determinism/replay:**

- Configuration identity is explicit and immutable for a run.
- Deck passcodes are sorted only by OCGForge's contract and never discovered from a database.
- Outer values are derived from the accepted frame, never from transport metadata.

**Stop boundary:** Stop if match configuration is not explicit or if source
construction attempts to implement `decision_index` or public decision-context
mapping, which are outside I6C and belong to I6D.

### Task I6C6: Native OCGForge safe-state/event oracle comparison

**Owning layer:** Model/oracle test boundary; no change to OCGForge.

**Files:**

- Create `tests/OCGForge.Ignis.Gameplay.Tests/Tests/I6CNativeOracleTests.cs`.
- Create `fixtures/model/v1/i6c-public-frame-vectors.v1.json` only from an OCGForge-owned/native oracle generator.
- Modify no OCGForge files.

**Semantic work:**

- Compare native OCGForge safe-state bytes and decoded field values against independently constructed Ignis source values only for vectors whose `player_to_act` is legitimately absent and that carry no decision context.
- Compare event count/order/index/optional presence and historical references.
- Do not claim full Decision-Boundary safe-state byte equality: vectors with `player_to_act=PRESENT` are `BLOCKED_PENDING_I6D` until I6D supplies the accepted decision source.
- Compare outer observation bytes only after the separately owned I6D decision composition is accepted.
- Keep prompt-local CardCode blocker outside this slice; affected frames fail closed.

**Gates:**

- complete field-by-field byte equality;
- paired worlds A-F;
- native event multiplicity and order;
- exact locator-token/ordinal behavior;
- malformed/mismatch fail closed with no partial source;
- fresh-process values, bytes, and identities equal.

**Stop boundary:** Stop if no native oracle emitter can produce the expected
bytes, if the fixture would need hand editing, or if Task4 smoke output is used
as a substitute for accepted OCGForge safe-state semantics.

### Task I6C7: I6C source-closure final acceptance

**Owning layer:** Gameplay/model oracle acceptance boundary.

**Files:**

- Modify `tests/OCGForge.Ignis.Gameplay.Tests/Program.cs` to register the final I6C group.
- Extend `tests/OCGForge.Ignis.Gameplay.Tests/Tests/I6CNativeOracleTests.cs`.
- No production change unless an earlier authorized source slice explicitly owns it.

**Current I6C0 preflight classification:**

```text
GLOBALS_SOURCE=BLOCKED
I6C_STATE_GLOBALS_SOURCE=BLOCKED
I6C_PLAYER_TO_ACT_SOURCE=OUTSIDE_I6C_PENDING_I6D
I6_DECISION_BOUNDARY_SAFE_STATE_BYTES=BLOCKED_PENDING_I6D
ZONES_SOURCE=BLOCKED
ENTITIES_SOURCE=BLOCKED
RELATIONSHIPS_SOURCE=BLOCKED_PENDING_LOCATOR_CLOSURE
CHAIN_SOURCE=BLOCKED
VISIBLE_EVENTS_SOURCE=BLOCKED
EVENT_INDEX_SOURCE=BLOCKED
MATCH_CONTEXT_SOURCE=BLOCKED_PENDING_EXPLICIT_CONFIGURATION
OUTER_OBSERVATION_CONTEXT_SOURCE=OUTSIDE_I6C
```

These are current source-closure values, not acceptance claims. The following
is the required matrix for a later I6C final review after the preceding slices
have closed their dependencies:

**Required future I6C-owned state-only final matrix:**

```text
I6C_STATE_GLOBALS_SOURCE=PROVEN
I6C_STATE_ONLY_SAFE_STATE_BYTES=PROVEN
I6C_ZONES_SOURCE=PROVEN
I6C_ENTITIES_SOURCE=PROVEN
I6C_RELATIONSHIPS_SOURCE=PROVEN
I6C_CHAIN_SOURCE=PROVEN
I6C_VISIBLE_EVENTS_SOURCE=PROVEN
I6C_EVENT_INDEX_SOURCE=PROVEN
I6C_MATCH_CONTEXT_SOURCE=PROVEN_WITH_EXPLICIT_CONFIGURATION
I6C_OUTER_OBSERVATION_CONTEXT_SOURCE=OUTSIDE_I6C

I3_V1_CANONICAL_BYTES_UNCHANGED=PASS
I3_V1_IDENTITY_UNCHANGED=PASS
I6_PAIRED_WORLD_PRIVACY=PASS
I6_FRESH_PROCESS_DETERMINISM=PASS
I6_MALFORMED_SOURCE_FAIL_CLOSED=PASS
I6_HISTORICAL_LOCATOR_NO_REBIND=PASS
I6_NO_PRIVATE_CONTINUATION_LEAK=PASS
I6_NO_I6D_OR_I7_AUTHORITY=PASS
```

The decision-boundary composition is a separate deferred matrix and is not an
I6C final prerequisite:

```text
I6C_PLAYER_TO_ACT_SOURCE=OUTSIDE_I6C
I6_DECISION_BOUNDARY_SAFE_STATE_BYTES=BLOCKED_PENDING_I6D
I6C_DECISION_CONTEXT_SOURCE=OUTSIDE_I6C
```

I6C6 may compare only state-only vectors whose OCGForge
`player_to_act` is semantically absent. Full decision-boundary safe-state or
outer-observation equality becomes eligible only after I6D supplies and
accepts the decision source; I6C must not close that gap by dropping a
present field.

The exact test count is intentionally UNKNOWN until this final slice is
authorized; the implementation must record the actual live harness count and
must not reuse an I5 count.

**Stop boundary:** A missing field, hidden-state dependency, event-index
ambiguity, native-oracle mismatch, I3 V1 regression, or determinism divergence
means `I6C_FINAL=NO` and stops the slice. I6D is not automatically authorized.

## 3. Cross-slice acceptance rules

Every future slice must run the narrowest relevant RED test first, then its
focused tests, then the existing Protocol/Client/Gameplay regressions when
Gameplay code changes. Fresh-process comparisons must include stdout, stderr,
exit code, public values, event indexes, and canonical bytes where available.

No future slice may claim full I6 compatibility from source presence alone.
The accepted OCGForge source snapshot remains:

```text
3edfcabf51dd914f96adc4df903b1ac2a9d20e5f
```

The known project blockers remain explicit:

```text
I6_PROMPT_LOCAL_CARDCODE_MAPPING=BLOCKED
I6_RULES_DOMAIN_COMPATIBILITY=DIFFERENT_OR_UNPROVEN
I6_TASK7_FINAL_ACCEPTANCE=NOT_PROVEN
I6_CHECKPOINT_COMPATIBILITY=NOT_AN_I6_FINAL_GATE
```

No source closure work may change those statuses by implication.

## 4. Current I6C0 delivery protocol

For this docs-only slice:

```text
FILES_CHANGED=2
PRODUCTION_CODE_CHANGED=NO
TEST_CODE_CHANGED=NO
CI_CHANGED=NO
OCGFORGE_CHANGED=NO
I3_V1_CHANGED=NO
```

Required local checks are `git diff --check`, exact tracked-plus-untracked
scope, source-path existence for every cited local file, and no unresolved template token
contract language. Runtime tests are not evidence for this documentation-only
slice.

Commit and push exactly once, then stop. Do not create a PR, merge, implement
I6C1, begin I6D, or begin I7 from this plan.
