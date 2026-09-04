# OCGForge-Ignis I3 Perspective State and Privacy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Record the separately reviewable I3A–I3D gameplay-message, perspective, knowledge, locator, and public-projection slices, including the accepted I3C0/I3C history and the I3D0 freeze, without introducing a second rules authority or hidden-information leak.

**Architecture:** The platform-neutral Gameplay layer consumes one claimed I2 handoff, processes pending bytes before new reads, decodes only the modern pinned GAME_MSG stream, and owns a single `PerspectiveStateMirror`. The accepted I3C boundary reduces that mirror to `PublicStateSnapshotV1` and canonical compact UTF-8 JSON; I3D0 freezes those existing bytes and the versioned `public_projection_id`. Private response binding, model input, and audit traces remain outside I3.

**Tech Stack:** C# / .NET 10, deterministic managed byte parsing, accepted `OCGForge.Ignis.Protocol` and I2 Client contracts, custom deterministic executable tests, no third-party runtime dependencies, no embedded ocgcore.

---

Execution status: I3A/I3B0/I3B/I3C0/I3C `FINAL_PASS`; I3D0 is the current documentation-only freeze; I3D remains `NOT_AUTHORIZED`.
Frozen slice order: `I3A → I3B0 → I3B → I3C0 → I3C → I3D0 → I3D`.

The unchecked checklist items in the historical task sections preserve the
original execution plan. Current acceptance status is authoritative from the
summary above: I3A, I3B0, I3B, I3C0, and I3C are accepted; I3D0 is this
documentation-only reconciliation; I3D and all later slices remain
unauthorized.

## Frozen inputs and non-negotiable boundaries

- Base contract: `ocgforge-ignis.gameplay.perspective-privacy.v1`.
- Inventory: `ocgforge-ignis.game_message_support.v1`.
- Accepted public-state contract: `ocgforge-ignis.public-state-projection.v1`.
- Accepted locator contract: `ocgforge-ignis.public-semantic-locator.v1`.
- Runtime: EDOPro `30935e847165a9ef0e547fb51a43f36168fab7c7`, ocgcore gitlink `46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57`.
- I2 handoff: `FINAL_GAMEPLAY_PERSPECTIVE=UNRESOLVED_AT_I2_HANDOFF`.
- Each slice requires its own explicit authorization, focused gates, independent review, and stop. The accepted I3C implementation is evidence for the current I3D0 reconciliation; I3D remains separately unauthorized.
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

Status: `FINAL_PASS` on accepted main; checklist retained as historical
execution detail.

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

Status: `FINAL_PASS` on accepted main; checklist retained as historical
execution detail.

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

Status: `FINAL_PASS` on accepted main; checklist retained as historical
execution detail.

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

Status: `FINAL_PASS` on accepted main; checklist retained as historical
execution detail.

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

## Task 5: I3C accepted perspective-safe public state projection

Status: `FINAL_PASS` on accepted main. This task supersedes the original
card-knowledge-only description; its implementation history remains in the
repository commits and is not rewritten by I3D0.

**Accepted files:**

- `src/OCGForge.Ignis.Gameplay/PublicStateProjectionV1.cs`;
- `tests/OCGForge.Ignis.Gameplay.Tests/Program.cs`;
- `tests/OCGForge.Ignis.Gameplay.Tests/Tests/I3CPublicStateProjectionTests.cs`;
- `docs/contracts/public-state-projection-v1.md`.

- [x] **Step 1: Reduce the accepted mirror.** Project only proven mirror facts into immutable `PublicStateSnapshotV1` values with absolute p0/p1 participants, perspective-safe card knowledge, semantic locators, and explicit fail-closed errors.
- [x] **Step 2: Freeze accepted I3C behavior as evidence.** Emit compact UTF-8 JSON with fixed property order, ordinal card ordering, JSON `null` for absent optionals, and the raw lowercase SHA-256. Do not add relations, chains, events, query dumps, response binding, candidates, model input, or an I6 claim.
- [x] **Step 3: Verify privacy and determinism.** Cover paired hidden worlds, internal allocation-order independence, culture/order determinism, SZONE mapping, public API boundaries, and canonical-byte storage isolation. The accepted result is `43/43` Gameplay tests with no model-input or cross-oracle acceptance claim.
- [x] **Step 4: Stop.** I3C does not authorize I3D0 retroactively or authorize I4/I5.

## Task 6: I3D0 public-projection identity/codec freeze (documentation only)

Status: current documentation-only reconciliation task. It creates no
production project, runtime implementation, test code, fixture, or dependency.

**Files:**

- `docs/contracts/public-state-projection-v1.md`;
- `docs/superpowers/specs/2026-09-03-i3-perspective-state-privacy-design.md`;
- `docs/superpowers/plans/2026-09-03-i3-perspective-state-privacy-plan.md`.

- [x] **Step 1: Reconcile history.** Record that I3 implemented canonical bytes before I3D0, that I3D0 does not retroactively authorize that implementation, and that this task freezes the already accepted I3C V1 bytes.
- [x] **Step 2: Freeze the existing JSON codec.** Freeze `CANONICAL_ENCODING=UTF8_COMPACT_JSON`, exact top-level/participant/card field order, p0/p1 and ordinal locator ordering, JSON `null` optional encoding, and the absence of relation, chain, event, and separate locator-table vectors.
- [x] **Step 3: Freeze digest and identity definitions.** Freeze raw SHA-256 over the exact canonical bytes and `public_projection_id = ocgforge-ignis.public-state-projection.v1.` plus the 64-character lowercase digest; do not add a production identity property.
- [x] **Step 4: Freeze evidence and future acceptance.** Document the independently verified golden vectors, privacy/determinism exclusions, immutability rule, and paired-world A–E requirements. Keep `I3D_PAIRED_WORLD_ACCEPTANCE=NOT_RUN`.
- [x] **Step 5: Stop.** I3D0 does not implement I3D or authorize I4/I5.

## Task 7: I3D remaining privacy/acceptance closure

Status: `NOT_AUTHORIZED`; I3D0 does not start this task.

**Files:**

- Extend the accepted `src/OCGForge.Ignis.Gameplay/PublicStateProjectionV1.cs` only if a separately authorized I3D design requires an identity-binding seam.
- Create future I3D identity-boundary tests under `tests/OCGForge.Ignis.Gameplay.Tests/`.
- Create future `tests/OCGForge.Ignis.Gameplay.Tests/PublicProjectionPrivacyTests.cs`.
- Use `fixtures/gameplay/v1/paired-hidden-worlds/` for deterministic fixture descriptions and expected hashes.

- [ ] **Step 1: Add I3D acceptance tests.** Assert the accepted I3C `PublicStateProjectionResultV1` and future `public_projection_id` binding remain value-owned, stable under source mutation, perspective-safe, and free of raw/control/model data.

- [ ] **Step 2: Implement the remaining identity boundary.** Consume only a successful accepted I3C `PublicStateProjectionResultV1` (or an exactly equivalent non-separable bound result carrying its snapshot, canonical bytes, and raw digest) and bind `PUBLIC_PROJECTION_ID_PREFIX + exact result.Sha256`; do not consume the mirror as a second projection authority, accept caller-supplied bytes/digest, change canonical JSON, add a second codec, or create a second projection type.

- [ ] **Step 3: Preserve the accepted codec.** I3D must use the I3D0 field order, null encoding, ordinal sorting, raw digest, and identity prefix exactly; it must not use map iteration or mutable aliases.

- [ ] **Step 4: Add paired-hidden-world acceptance.** Implement fixture classes A–E: different hidden opponent hands, different hidden deck order, reveal-then-hide knowledge destruction, duplicate equal-code public cards, and TCP chunking variants. Require byte-identical projection, embedded locators, raw digest, and `public_projection_id` for semantically equal public worlds.

- [ ] **Step 5: Verify I3D and boundaries.** Run I1/I2 regression, all I3 tests, fresh-process identity comparison, raw metadata leak scan, and no-candidate/no-legality/no-model scan. Keep `MODEL_INPUT_READY=NO` and `I6_CROSS_ORACLE_ACCEPTED=NO`.

- [ ] **Step 6: Stop.** Do not implement I4/I5 prompt projection, private response binding, model input, runner IPC, checkpoint binding, or OCGForge cross-oracle.

## Task 8: I3 provenance, CI, and acceptance evidence

**Files:**

- Modify future `.github/workflows/i3-gameplay.yml` or the explicitly accepted shared workflow.
- Modify `PROTOCOL_PROVENANCE.md` only for facts used by the authorized slice.
- Add only the future slice's deterministic fixtures and test executable.

- [ ] **Step 1: Add source-ledger rows.** For every supported family record repository, exact commit, source path, symbol/function/constant, fact, inspection date, and classification. Use the ocgcore pin for message IDs/constants and EDOPro paths for server/client wire behavior. Do not copy source or serialized buffers.

- [ ] **Step 2: Add narrow hosted CI.** Restore, build Release, and run only the authorized I3 slice plus I1/I2 regressions. No EDOPro download, socket connection, public network, public server, model runner, or WPF.

- [ ] **Step 3: Run the frozen gate checklist.** Each implementation task may report only gates actually run. I3C is accepted on main; I3D0 adds no runtime gate, and I3D must run its separately authorized privacy/identity gates.

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
