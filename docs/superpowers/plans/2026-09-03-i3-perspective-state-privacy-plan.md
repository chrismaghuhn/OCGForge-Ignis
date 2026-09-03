# OCGForge-Ignis I3 Perspective State and Privacy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Specify and later implement the separately reviewable I3A–I3D gameplay-message, perspective, knowledge, locator, and public-projection slices, with the I3C0/I3D0 byte-codec freezes preceding their implementations, without introducing a second rules authority or hidden-information leak.

**Architecture:** A future platform-neutral Gameplay layer consumes one claimed I2 handoff, processes pending bytes before new reads, decodes only the modern pinned GAME_MSG stream, and owns a single `PerspectiveStateMirror`. A separate projection boundary converts only accepted perspective-safe values to `PublicContractProjectionV1`; private response binding, model input, and audit traces remain outside I3.

**Tech Stack:** C# / .NET 10, deterministic managed byte parsing, accepted `OCGForge.Ignis.Protocol` and I2 Client contracts, custom deterministic executable tests, no third-party runtime dependencies, no embedded ocgcore.

---

Execution status: CONTRACT_FREEZE_ONLY; all implementation tasks remain `NOT_RUN` and unauthorized.
Frozen slice order: `I3A → I3B0 → I3B → I3C0 → I3C → I3D0 → I3D`.

## Frozen inputs and non-negotiable boundaries

- Base contract: `ocgforge-ignis.gameplay.perspective-privacy.v1`.
- Inventory: `ocgforge-ignis.game_message_support.v1`.
- Runtime: EDOPro `30935e847165a9ef0e547fb51a43f36168fab7c7`, ocgcore gitlink `46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57`.
- I2 handoff: `FINAL_GAMEPLAY_PERSPECTIVE=UNRESOLVED_AT_I2_HANDOFF`.
- I3 production is not authorized by this plan. The frozen sequence is `I3A → I3B0 → I3B → I3C0 → I3C → I3D0 → I3D`; each implementation or documentation-freeze slice requires a new explicit task, its own branch/commit/PR, focused gates, independent review, and a stop.
- EDOPro remains the sole rules/legal authority. I3 never recomputes legality, selects a prompt answer, creates candidates, scores a model, or binds a response.
- `ANNOUNCE_CARD`, Tag/Relay, Match/Siding, observer gameplay, reconnect, public servers, IPC, WPF, and model integration remain fail-closed or out of scope.

## Future project boundary

Future implementation slices may create:

- `src/OCGForge.Ignis.Gameplay/OCGForge.Ignis.Gameplay.csproj` targeting `net10.0`;
- `tests/OCGForge.Ignis.Gameplay.Tests/OCGForge.Ignis.Gameplay.Tests.csproj` targeting `net10.0`;
- `fixtures/gameplay/v1/` deterministic byte and paired-world fixtures.

The Gameplay project may reference Protocol and the explicitly accepted I2
handoff boundary. It must not reference OCGForge's implementation, WindBot,
EDOPro binaries, or ocgcore. The current I2 `GameplayTransportHandoffV1` is an
internal implementation detail; the first I3 implementation task must expose
the narrow transfer operation through an explicit assembly boundary rather
than reflection or direct `I2SessionRunner` access. That boundary must provide
exactly-once claim, pending-byte-first access, live transport ownership, and
exactly-once close on failure.

## Task 1: I3A GAME_MSG decoder foundation

**Files:**

- Create future `src/OCGForge.Ignis.Gameplay/GameplayMessageDecoderV1.cs`.
- Create future `src/OCGForge.Ignis.Gameplay/GameplayMessageTypesV1.cs`.
- Create future `src/OCGForge.Ignis.Gameplay/GameplayHandoffConsumerV1.cs`.
- Test future `tests/OCGForge.Ignis.Gameplay.Tests/GameplayMessageDecoderTests.cs`.
- Test future `tests/OCGForge.Ignis.Gameplay.Tests/GameplayHandoffTests.cs`.
- Use `fixtures/gameplay/v1/` for independently constructed message bytes.

- [ ] **Step 1: Write RED decoder tests.** Define a wished-for API returning a typed result from one `StocGameMessagePayload` and assert the exact modern `MSG_START` bytes, modern `loc_info` width, underflow, overflow, trailing-byte rejection, unknown ID rejection, legacy-extra-byte rejection, and count overflow. Assert that no test result contains a socket, endpoint, password, PID, timestamp, task ID, or raw object identity.

- [ ] **Step 2: Run the decoder RED tests.** Run:

```powershell
dotnet run --project tests/OCGForge.Ignis.Gameplay.Tests/OCGForge.Ignis.Gameplay.Tests.csproj --configuration Release
```

Expected result: the future Gameplay project/types do not exist or the wished-for decoder contract is not implemented. Do not treat missing I3 production code as a passing gate.

- [ ] **Step 3: Implement only the strict modern envelope.** The decoder consumes the complete I1-preserved GAME_MSG payload as `u8 message_id + message payload`, uses explicit little-endian reads, and returns `Success`, `UNSUPPORTED_MESSAGE`, or `MALFORMED_GAME_MESSAGE` (or equivalent stable typed values). The inner decoder never returns `NeedMoreData`: underflow in a complete outer `STOC_GAME_MSG` payload is malformed. Only I1 may report `NeedMoreData` while accumulating an incomplete outer frame, and the inner decoder never borrows bytes from a following frame. It must consume exactly one message and never parse an I4/I5 prompt beyond identifying its boundary. It must not repair a length, skip an unknown message, or fall back to a legacy layout.

- [ ] **Step 4: Add exact `MSG_START` establishment.** Accept only an exact 18-byte inner message:

```text
u8  message_id = 4
u8  playertype in {0x00, 0x01}
u32_le lp[0]
u32_le lp[1]
u16_le deck_count[0]
u16_le extra_count[0]
u16_le deck_count[1]
u16_le extra_count[1]
```

Reject `0x10`/`0x11` observers, every other role byte, duplicate/conflicting
markers, and a marker after a perspective-dependent message. Produce exactly
`SelfIsPlayer0` or `SelfIsPlayer1`; never derive it from I2 lobby position,
host flag, RPS outcome, or TP choice.

- [ ] **Step 5: Verify I3A locally.** Run the Gameplay tests, I1 tests, and I2 tests in Release. Repeat each executable in two fresh processes and compare complete output. Verify exact pending bytes are processed before a new transport read and a failed handoff closes once.

- [ ] **Step 6: Stop.** Do not add the PerspectiveStateMirror, card knowledge, semantic locators, public projection, prompt answer, or model input. Commit and review I3A as a separate task only after a new authorization.

## Task 2: I3B0 query-flag union freeze (documentation only)

This is a separate contract-freeze task between I3A and I3B. It creates no
production project or runtime parser.

**Files:**

- Modify the owning I3 design/spec document only.
- Add no C# source, project, or runtime dependency.

- [ ] **Step 1: Consume the outer grammars unchanged.** Use the I3A0-frozen
  `ModernQueryV1` and `ModernQueryStreamV1` definitions as immutable inputs.
  Their `item_size==0`/`ONFIELD_SKIPPED`, `QUERY_END==4`, item-size arithmetic,
  stream total-byte boundary, and prefix-exclusion semantics are not owned by
  I3B0 and must not be changed here.
- [ ] **Step 2: Freeze the flag union.** Record the exact admitted query flags,
  payload types, widths, per-flag bounds, overflow behavior, and unknown-flag
  policy for the pinned modern V1 source. Keep this flag-specific contract
  separate from the already-frozen outer record grammars.
- [ ] **Step 3: Freeze flag-specific validation.** Define the exact relationship
  between each admitted flag payload and its inherited `item_size`, including
  malformed/truncated flag payloads and any count/vector bounds. The outer
  grammar remains an unchanged prerequisite, not a new I3B0 decision.
- [ ] **Step 4: Add evidence and stop.** Add positive and negative fixtures for
  each admitted flag, including inherited zero-size/QUERY_END/stream-boundary
  regression cases without changing their expected semantics. Leave all
  implementation gates `NOT_RUN`; I3B may implement the union only after I3B0
  is independently accepted.

## Task 3: I3B deterministic PerspectiveStateMirror

**Files:**

- Create future `src/OCGForge.Ignis.Gameplay/PerspectiveStateMirrorV1.cs`.
- Create future `src/OCGForge.Ignis.Gameplay/PerspectiveStateTypesV1.cs`.
- Create future `tests/OCGForge.Ignis.Gameplay.Tests/PerspectiveStateMirrorTests.cs`.
- Add only the exact I3B provenance rows for message layouts and transitions.

- [ ] **Step 1: Write RED mirror tests.** Start from an established `GameplayPerspectiveV1` and assert participant roles `Self`/`Opponent`, LP, turn player/count, phase, deck/extra/hand/grave/banished counts, field slots, controller/owner distinction, and chain/public relationship values. Assert that every value has an explicit provenance class and that absent knowledge is not represented by a magic card code.

- [ ] **Step 2: Implement a single-owner reducer.** Consume only I3A validated messages and the accepted I3B0 query codec/union contract. Use fixed participant and zone order, explicit state transitions, exact source/destination validation, and immutable result snapshots. `MSG_UPDATE_CARD` uses the single `ModernQueryV1` suffix; `MSG_UPDATE_DATA` uses the `u32_le total_query_bytes`-prefixed `ModernQueryStreamV1` suffix. Unrecognized flags, incomplete query records, and stream-boundary violations fail closed.

- [ ] **Step 3: Implement required public state families.** Cover the inventory's I3B-required families: `MSG_START`, `MSG_WIN`, `MSG_UPDATE_DATA`, `MSG_UPDATE_CARD`, `MSG_MOVE`, `MSG_POS_CHANGE`, `MSG_SET` as a consume-only presentation event with no mirror mutation, `MSG_SWAP`, `MSG_NEW_TURN`, `MSG_NEW_PHASE`, LP changes, required chain messages, target relations, and equipment relations. Keep prompt families at the I4/I5 boundary and do not apply presentation/summon notifications a second time when MOVE/query messages own the state change.

- [ ] **Step 4: Add RED/green failure coverage.** Reject wrong player mapping, unknown locators, structurally contradictory zone/controller/position references, duplicate transport processing, arithmetic overflow/underflow, unsupported phase values, and state-capacity failure. Do not mutate or publish a partial mirror on any rejected transition. Do not reimplement expected turn alternation, phase sequencing, skipped-phase legality, extra-turn legality, or another Yu-Gi-Oh! rules check; `MSG_NEW_TURN` and `MSG_NEW_PHASE` are authoritative updates.

- [ ] **Step 5: Verify chunking and process determinism.** Feed the same validated message stream as one message, one byte at a time, complete-frame chunks, and fixed irregular chunks. Compare mirror values, event order, and semantic identity; exclude transport chunk counts and timing from all comparisons.

- [ ] **Step 6: Stop.** Do not add knowledge-destroying policy beyond explicit hooks, public projection, candidates, or response selection. Commit and review I3B separately.

## Task 4: I3C0 semantic-locator codec freeze (documentation only)

This is a separate contract-freeze task. It creates no production project or
runtime implementation and must be independently reviewed before I3C.

**Files:**

- Modify the owning I3 design/spec document only.
- Add no C# source, project, fixture, or runtime dependency.

- [ ] **Step 1: Freeze the locator identity domain.** Define the exact locator
  domain tag, schema/version bytes, semantic participant and zone enum codes,
  slot/sequence widths, endian order, creation-ordinal encoding, and explicit
  optional/unknown encoding. Do not use raw protocol addresses as public
  identity.
- [ ] **Step 2: Freeze canonical locator bytes.** Define the complete field
  order, vector count encoding, duplicate-entity handling, lifecycle
  create/move/rebind/destroy representation, and hash algorithm/prefix. Every
  byte must have one normative semantic owner; no map iteration, object
  identity, PID, time, path, or transport metadata is allowed.
- [ ] **Step 3: Add codec-freeze evidence.** Add golden byte vectors and
  metamorphic cases for equal semantic locators, destroyed hidden locators,
  duplicate public card codes, and ordering changes that are not semantic.
  Leave all implementation gates `NOT_RUN`.
- [ ] **Step 4: Stop.** I3C0 does not implement the codec or the knowledge
  reducer. A separate implementation authorization is required after review.

## Task 5: I3C card knowledge and semantic locators

**Files:**

- Create future `src/OCGForge.Ignis.Gameplay/CardKnowledgeV1.cs`.
- Create future `src/OCGForge.Ignis.Gameplay/PublicSemanticLocatorV1.cs`.
- Create future `src/OCGForge.Ignis.Gameplay/KnowledgeBoundaryReducerV1.cs`.
- Create future `tests/OCGForge.Ignis.Gameplay.Tests/CardKnowledgeAndLocatorTests.cs`.
- Add exact provenance for shuffle/reorder/reveal behavior.

- [ ] **Step 1: Write RED knowledge tests.** Assert `UnknownIdentity`, `KnownPrivateIdentity(card_code)`, and `KnownPublicIdentity(card_code)` are distinct typed values. Assert own private cards may retain legitimate identity, opponent hidden cards do not, and `card_code=0` is never the unknown sentinel.

- [ ] **Step 2: Write RED locator lifecycle tests.** Assert deterministic create/retain/move/rebind/destroy/replace behavior, distinct locators for duplicate public card codes, no code-as-locator shortcut, no pointer/object/PID/path/time inputs, and no dependence on unordered iteration.

- [ ] **Step 3: Implement knowledge-destroying transitions.** For `MSG_SHUFFLE_DECK`, `MSG_SHUFFLE_HAND`, `MSG_SHUFFLE_EXTRA`, `MSG_SHUFFLE_SET_CARD`, `MSG_REVERSE_DECK`, `MSG_SWAP_GRAVE_DECK`, ambiguous randomized movement, and ambiguous reorder, destroy stale hidden continuity. `MSG_REFRESH_DECK` is consumed as an exact empty presentation/control signal and supplies no player-scoped mutation. Retain identity only when a complete perspective-legitimate mapping is explicitly proven.

- [ ] **Step 4: Add metamorphic privacy tests.** Mutate hidden opponent identities, hidden deck order, stale post-reveal identities, equal-code duplicate histories, and ambiguous source locators. Require the same public locator table and public meaning where legitimate public knowledge is unchanged; require old hidden locators to disappear after destruction.

- [ ] **Step 5: Verify I3C.** Run all I3B tests plus paired hidden-world fixtures A–E, fresh-process output comparison, no-secret scan, no-control-metadata scan, and explicit resource-failure tests. Do not add probability, archetype inference, elimination, or hidden-deck reconstruction.

- [ ] **Step 6: Stop.** Do not implement the locator codec, `PublicContractProjectionV1`, or any model-facing identity in the same task. I3C consumes only the accepted I3C0 codec contract and is reviewed separately.

## Task 6: I3D0 public-projection identity/codec freeze (documentation only)

This is a separate contract-freeze task after I3C and before I3D. It creates no
production project or runtime implementation.

**Files:**

- Modify the owning I3 design/spec document only.
- Add no C# source, project, fixture, or runtime dependency.

- [ ] **Step 1: Freeze the projection domain.** Define the exact projection
  domain tag, schema/version bytes, participant/zone/entity field order,
  integer widths, endian order, enum codes, and explicit optional/unknown and
  knowledge-union encodings.
- [ ] **Step 2: Freeze canonical projection bytes.** Define vector count and
  ordering rules, relation encoding, the public locator table encoding, and
  the exact public identity hash algorithm and prefix. Exclude raw protocol,
  private-control, hidden-opponent, execution, and model-derived fields.
- [ ] **Step 3: Add codec-freeze evidence.** Add golden projection bytes and
  paired-hidden-world metamorphic cases A–E, including process-restart and
  chunking invariance. Leave all implementation gates `NOT_RUN`.
- [ ] **Step 4: Stop.** I3D0 does not implement projection or identity code. A
  separate implementation authorization is required after review.

## Task 7: I3D PublicContractProjection and paired-world acceptance

**Files:**

- Create future `src/OCGForge.Ignis.Gameplay/PublicContractProjectionV1.cs`.
- Create future `src/OCGForge.Ignis.Gameplay/PublicGameplayIdentityV1.cs`.
- Create future `tests/OCGForge.Ignis.Gameplay.Tests/PublicProjectionPrivacyTests.cs`.
- Use `fixtures/gameplay/v1/paired-hidden-worlds/` for deterministic fixture descriptions and expected hashes.

- [ ] **Step 1: Write RED projection tests.** Assert projection values are copied/value-owned, stable under source mutation, explicitly encode known/unknown knowledge, preserve duplicate public entities, and exclude raw bytes, protocol offsets, socket state, endpoint, password, PID, wall clock, path, object identity, response binding, and model-derived fields.

- [ ] **Step 2: Implement the projection boundary.** Consume only the accepted mirror and locator table. Emit `PublicContractProjectionV1` plus a separate `public_projection_id`; never reuse a transport/provenance digest as gameplay identity. Reject publication when a required mirror field is ambiguous or when an unclassified field would enter the output.

- [ ] **Step 3: Consume the accepted I3D0 codec.** Encode only according to the separately accepted projection domain, field order, optional/knowledge encoding, locator-table encoding, and identity hash/prefix. I3D must not make new byte-level identity decisions. Do not use map iteration or mutable aliases.

- [ ] **Step 4: Add paired-hidden-world acceptance.** Implement fixture classes A–E: different hidden opponent hands, different hidden deck order, reveal-then-hide knowledge destruction, duplicate equal-code public cards, and TCP chunking variants. Require byte-identical projection, public locator table, and public identity for semantically equal public worlds.

- [ ] **Step 5: Verify I3D and boundaries.** Run I1/I2 regression, all I3 tests, fresh-process identity comparison, raw metadata leak scan, and no-candidate/no-legality/no-model scan. Keep `TRAINING_ELIGIBILITY=NO` and `AUTHORITATIVE_MODEL_INPUT=NO` for raw protocol diagnostics.

- [ ] **Step 6: Stop.** Do not implement I4/I5 prompt projection, private response binding, model input, runner IPC, checkpoint binding, or OCGForge cross-oracle.

## Task 8: I3 provenance, CI, and acceptance evidence

**Files:**

- Modify future `.github/workflows/i3-gameplay.yml` or the explicitly accepted shared workflow.
- Modify `PROTOCOL_PROVENANCE.md` only for facts used by the authorized slice.
- Add only the future slice's deterministic fixtures and test executable.

- [ ] **Step 1: Add source-ledger rows.** For every supported family record repository, exact commit, source path, symbol/function/constant, fact, inspection date, and classification. Use the ocgcore pin for message IDs/constants and EDOPro paths for server/client wire behavior. Do not copy source or serialized buffers.

- [ ] **Step 2: Add narrow hosted CI.** Restore, build Release, and run only the authorized I3 slice plus I1/I2 regressions. No EDOPro download, socket connection, public network, public server, model runner, or WPF.

- [ ] **Step 3: Run the frozen gate checklist.** The implementation task may report only gates actually run. Apply I3-G00 through I3-G22 exactly as defined in the contract; all gates are `NOT_RUN` for I3A0.

- [ ] **Step 4: Scope-audit before each slice commit.** Reject `.dll`, `.exe`, CDB, deck, checkpoint, copied upstream source, public endpoint, I4/I5 candidate type, model/IPC/UI code, or a Protocol→Gameplay reverse dependency.

- [ ] **Step 5: Commit one slice at a time.** Use separate branches and PRs for I3A, I3B0, I3B, I3C0, I3C, I3D0, and I3D. Do not combine slices or merge without independent review.

## I3A0 current verification boundary

This documentation-only task must run:

```powershell
git fetch origin
git status --short --branch
git rev-parse HEAD
git ls-remote origin refs/heads/main
Get-Content fixtures/gameplay/v1/game-message-support.v1.json | ConvertFrom-Json
dotnet run --project tests/OCGForge.Ignis.Protocol.Tests/OCGForge.Ignis.Protocol.Tests.csproj --configuration Release
dotnet run --project tests/OCGForge.Ignis.Client.Tests/OCGForge.Ignis.Client.Tests.csproj --configuration Release
git diff --check
```

It must additionally compare the JSON inventory and Markdown matrix, verify
the inventory against the pinned core constants, validate relative Markdown
links, and prove that no production `.cs` file was added. It must not run or
claim any I3 implementation gate.

## Stop conditions

Stop the current slice immediately on any unknown/malformed state-relevant
message, perspective ambiguity, hidden-identity continuity ambiguity,
semantic-locator collision, public-projection leak, incomplete candidate or
prompt boundary, provenance mismatch, failed deterministic comparison, or
resource-capacity failure. No slice may add a fallback or silently widen the
support inventory.
