# OCGForge-Ignis I4A0 Flat Prompt Contract Freeze Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Freeze the exact modern pinned-protocol wire layouts, complete flat legal-choice domains, deterministic source order, public/private separation, and response bindings for the seven authorized I4 prompt families without implementing I4.

**Architecture:** The contract is a clean-room semantic freeze over the pinned ocgcore prompt producers, EDOPro prompt readers, and EDOPro/core response path. A future I4 implementation will decode one complete prompt, project only perspective-safe candidate values using `PublicSemanticLocatorV1`, retain a current-prompt-local private response binding, and emit exactly one original `CTOS_RESPONSE`; this slice creates no runtime seam.

**Tech Stack:** Markdown contracts, strict deterministic JSON fixtures, PowerShell validation, pinned EDOPro `30935e847165a9ef0e547fb51a43f36168fab7c7`, pinned ocgcore `46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57`, existing .NET 10 Release harnesses.

---

## Scope and files

The authorized change is documentation, provenance, and deterministic fixture
data only. The following files are the complete planned scope:

- Create `docs/contracts/flat-prompt-projection-v1.md` as the normative I4A0 contract.
- Create `docs/superpowers/specs/2026-09-04-i4-flat-prompt-design.md` as the research/design record.
- Create this plan at `docs/superpowers/plans/2026-09-04-i4-flat-prompt-plan.md`.
- Modify `fixtures/gameplay/v1/game-message-support.v1.json` only for the seven proven I4 layout catalog entries and provenance references.
- Create `fixtures/gameplay/v1/i4-flat-prompt-vectors.v1.json` with independently derived positive, duplicate, binding, and malformed vectors.
- Modify `PROTOCOL_PROVENANCE.md` only with the exact I4A0 source ledger and pin-verification date.

Production C#, test C#, project/dependency files, workflows, upstream pins,
locator semantics, and existing I3 fixtures remain unchanged.

### Task 1: Exact-base and primary-source evidence

**Files:** none.

- [x] **Step 1: Verify the repository checkpoint.** Run `git fetch origin`, `git status --short --branch`, `git rev-parse HEAD`, `git rev-parse origin/main`, and `git ls-remote origin refs/heads/main` in `C:\Users\chris\Documents\OCGForge-Ignis`. Continue only when all three refs equal `4a054c3e0f0be10b704a1614ae275d4ce630ddce` and the worktree is clean.
- [x] **Step 2: Verify the research pins.** In a disposable directory, check out EDOPro `30935e847165a9ef0e547fb51a43f36168fab7c7` and ocgcore `46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57`. Confirm that the pinned EDOPro `ocgcore` gitlink resolves to the pinned runtime core.
- [x] **Step 3: Read the authority paths.** Inspect the exact message constants, `field::process` prompt writers, `duel_message::write`, `card::get_info_location`, EDOPro `CompatRead`, `ClientAnalyze`, `SetResponseI`, `SendBufferToServer`, event-handler response paths, and core prompt response validators. Record repository, SHA, path, symbol, and role for each fact.

### Task 2: Normative flat-prompt contract

**File:** `docs/contracts/flat-prompt-projection-v1.md`.

- [x] **Step 1: Freeze common boundaries.** State the contract ID, exact pins, modern-only widths, explicit little-endian interpretation for the pinned target, complete-message/trailing-byte policy, checked count arithmetic, fail-closed behavior, and the distinction between contract freeze and implementation.
- [x] **Step 2: Freeze public/private seams.** Define the exact `FlatPromptPublicContextV1` and `FlatPublicCandidateDescriptorV1` inclusion matrices, the accepted public semantic card-reference predicates, private current-prompt response binding, source ordinals, deterministic `i4_local_candidate_key` shape, the I6-owned OCGForge `public_action_key` boundary, no-auto-answer rule, and all forbidden metadata.
- [x] **Step 3: Freeze all seven families.** For message IDs 10, 11, 12, 13, 14, 16, and 19 record exact fields, formulas, flags, complete domains, source order, duplicate treatment, exact response values/body width, and malformed/legacy policies. Explicitly account for command sections, chain `-1`, position bit expansion, and zero-option states.
- [x] **Step 4: Freeze future acceptance boundaries.** State that I4 is not implemented, I5/model input/I6 remain out of scope, no raw response or `ModernLocInfoV1` reaches public candidates, and any unproven reference/domain fails closed without a partial candidate list.

### Task 3: Machine inventory reconciliation

**File:** `fixtures/gameplay/v1/game-message-support.v1.json`.

- [x] **Step 1: Add exact layout catalog entries.** Add only the seven modern V1 layouts with explicit field sequences, widths, formulas, validation rules, domain rules, source order, and response contract IDs.
- [x] **Step 2: Update only the seven entries.** Change `layout_status` to `FROZEN`, assign versioned `wire_layout_id` values, preserve `support_status=OUT_OF_SCOPE`, preserve `planned_slice=I4`, and attach only the new exact provenance reference IDs.
- [x] **Step 3: Preserve all other inventory entries byte-for-byte semantically.** Do not change I3 statuses, I5 assignments, query contracts, message IDs, or upstream pins.

### Task 4: Deterministic protocol vectors

**File:** `fixtures/gameplay/v1/i4-flat-prompt-vectors.v1.json`.

- [x] **Step 1: Add positive vectors.** Include at least one exact raw inner payload for every family, with decoded typed fields, complete ordered `FlatPublicCandidateDescriptorV1` fields, exact shared context, source ordinals, candidate counts, and exact four-byte private response bodies. Include a duplicate-option vector, an optional empty-chain vector, and a forced `spe_count=0x7f` chain vector.
- [x] **Step 2: Add negative vectors.** Cover truncation, trailing bytes, count/body mismatch, checked count overflow, invalid flag/bitmask, prohibited zero-option state, invalid participant, invalid card reference, illegal response index, and unsupported legacy layout. Record that malformed input yields no partial domain.
- [x] **Step 3: Add binding vectors.** Cover stale same-looking keys from different prompt occurrences, current-prompt family mismatch, duplicate source entries remaining distinct, and public/private storage separation without including private bindings in public candidate records.

### Task 5: Design and provenance records

**Files:** `docs/superpowers/specs/2026-09-04-i4-flat-prompt-design.md`, `PROTOCOL_PROVENANCE.md`.

- [x] **Step 1: Write the design record.** Summarize the upstream trace, the seven-family decisions, the source-order rules, the public/private boundary, the fail-closed decisions, and the explicit non-goal that no I4 runtime behavior exists.
- [x] **Step 2: Add the I4A0 provenance ledger.** Record exact source paths/symbols for identifiers, field layouts, loc_info, counts, ordering, response generation, and response consumption. Keep this clean-room and do not copy upstream source.

### Task 6: Validation and delivery

**Files:** all files above.

- [x] **Step 1: Validate strict JSON.** Parse both JSON fixtures with strict duplicate-key detection, require UTF-8 without BOM, reject generated timestamps/absolute paths/duplicate keys, and verify deterministic property order in the checked-in text.
- [x] **Step 2: Independently recompute vectors.** Rebuild the positive raw payloads from the documented field values, confirm every byte length and response body, and compare the resulting hex with the fixture without using future production code.
- [x] **Step 3: Run the existing Release gates twice.** Run all six requested builds and fresh-process Protocol 20/20, Client 17/17, and Gameplay 53/53 harnesses twice; compare complete stdout byte-for-byte and record only actually executed results.
- [x] **Step 4: Audit scope.** Run `git diff --check`, inspect `git diff --stat`, verify no `src/` or `tests/` file changed, search the new documents for stale I4 implementation claims, then commit exactly the authorized files with `docs: separate I4 local bindings from public action identity`.
- [x] **Step 5: Push and stop.** Push `chris/i4a0-flat-prompt-contract-freeze`, verify the remote head and clean worktree, and stop for independent review. Do not create a PR or begin I4/I5.
