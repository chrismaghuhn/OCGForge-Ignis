# I6D Private Source Occurrence Binding Design Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Freeze the private, frame-local source-occurrence binding contract required to translate I4 locators to the pinned I6C5/OCGForge locator without changing production semantics.

**Architecture:** The semantic owner is the I6D `OcgForgePublicCandidateBridgeV1`. Gameplay/I4 creates a private proof while normalized prompt occurrence and same-frame mirror correlation are available; an internal immutable handoff carries lifecycle coordinates, normalized source occurrence, and the exact target locator derived from the accepted I6C5 frame. The public I4 descriptor, action key, candidate-domain digest, observation, and model input remain unchanged.

**Tech Stack:** .NET 10, C# record/enum test descriptors, repository Markdown specifications, existing console-style Gameplay test runner.

---

### Task 1: Freeze the private binding contract in the design specification

**Files:**
- Modify: `docs/superpowers/specs/2026-09-10-i6d-cross-locator-mapping-authority-characterization.md`

- [x] **Step 1: Add the exact owner and visibility decision**

Document that the semantic owner is `OcgForgePublicCandidateBridgeV1`, source proof acquisition is the transient `FlatPromptProjectionV1`/`FlatPromptCardCorrelationV1` seam, and the carrier is internal-only through a narrow Gameplay-to-Model handoff. The public `FlatPromptProjectionResultV1` and public candidate descriptors do not gain private occurrence members.

- [x] **Step 2: Add the exact field classification**

Freeze these groups and meanings:

```text
lifecycle/frame binding:
  PromptInstanceOrdinal
  ContinuationStep
  FrameInstanceOrdinal
  AcceptedPublicProjectionId

candidate lookup/cross-check:
  I4LocalCandidateKey
  SourceSection
  SourceOrdinal

private source occurrence:
  AbsoluteController
  NormalizedZone
  SourceSequence
  IsOverlay
  OverlayIndex (present only for overlay)

safe target:
  AcceptedI6C5TargetLocator
```

State that `FrameInstanceOrdinal` is session-owned and deterministic, `AcceptedPublicProjectionId` is an existing I3D consistency guard, and neither is public identity. State that `MirrorEntityIdV1`, `ModernLocInfoV1`, prompt CardCode, raw `loc_info`, pointers, and object hashes are not carrier fields.

- [x] **Step 3: Add creation, lookup, and consumption rules**

Freeze the primary lookup key as `(PromptInstanceOrdinal, ContinuationStep, I4LocalCandidateKey)`; require `SourceSection` and `SourceOrdinal` to match the candidate as cross-checks, never as a fallback search. Require creation after exact normalized source resolution and exact same-snapshot I6C3 target lookup, before `CompleteCorrelation` discards the occurrence. Require the complete binding set to be atomically accepted by the decision boundary; the public bridge receives it only through that boundary, never as a detached caller list.

- [x] **Step 4: Add lifecycle, duplicate, collision, and privacy rules**

Specify that each continuation step receives a new binding set, old bindings are invalid after prompt/frame/step replacement, and all missing, stale, ambiguous, or colliding bindings reject the whole boundary. Distinguish duplicate own-hand source occurrences from public identity: distinct normalized sequences may produce distinct I6C5 targets; a single source occurrence may serve multiple different action choices only when its target is identical. Private provenance never enters public descriptor/key/domain/model/observation identity; only the accepted safe target may feed the OCGForge descriptor.

- [x] **Step 5: Correct the public/hidden hand case labels**

Explicitly distinguish `opponent public Hand = exact public-token path` from `hidden opponent Hand = no public per-card entity`, so the design does not overclaim the hidden case.

---

### Task 2: Add executable design characterization without production code

**Files:**
- Modify: `tests/OCGForge.Ignis.Gameplay.Tests/Fixtures/I6DCrossLocatorMappingAuthorityCharacterizationV1.cs`
- Modify: `tests/OCGForge.Ignis.Gameplay.Tests/Tests/I6DCrossLocatorMappingAuthorityCharacterizationTests.cs`
- Modify: `tests/OCGForge.Ignis.Gameplay.Tests/Program.cs`

- [x] **Step 1: Define a test-only contract manifest**

Add `I6DPrivateSourceOccurrenceBindingDesignV1` as a test-only manifest. It must expose the ordered field groups, primary lookup key, owner/acquisition/consumption seam, invalidation requirements, duplicate/collision policy, and public-identity exclusion flags. It must not define or instantiate a production binding type.

- [x] **Step 2: Assert exact field roles and forbidden data**

Add `TestPrivateSourceOccurrenceBindingDesignContract()` and assert the exact ordered fields listed in Task 1, the lifecycle-versus-provenance distinction, and absence of `MirrorEntityIdV1`, `ModernLocInfoV1`, prompt CardCode, raw locator bytes, pointers, and object hashes.

- [x] **Step 3: Assert the four source cases and privacy rule**

Characterize own-hand unique, own-hand duplicate, opponent-public-hand, and cross-pile cases. Assert that duplicate own-hand bindings are distinct by source occurrence, exact-token opponent-public-hand remains unchanged, and private occurrence values do not participate in a paired public identity when the accepted safe target/public semantics are identical.

- [x] **Step 4: Assert requirements are not overclaimed as implemented behavior**

Report stale/missing/ambiguous/collision handling as `REQUIRED`, not `PASS`, because no production mapping implementation exists. Register the design test as a separate focused test.

---

### Task 3: Verify and deliver the design-only slice

**Files:**
- No additional files.

- [x] **Step 1: Run the focused Gameplay test**

Run the Release Gameplay test runner and verify the new design characterization passes without changing production code.

- [x] **Step 2: Run all required regression targets**

Run Release Model, Gameplay, Protocol, and Client tests. Record only executed results.

- [x] **Step 3: Run repository hygiene checks**

Run `git diff --check` and inspect the tracked diff to confirm only documentation and test files changed.

- [x] **Step 4: Commit and push**

```powershell
git add docs/superpowers/plans/2026-09-10-i6d-private-source-occurrence-binding-design-plan.md docs/superpowers/specs/2026-09-10-i6d-cross-locator-mapping-authority-characterization.md tests/OCGForge.Ignis.Gameplay.Tests/Fixtures/I6DCrossLocatorMappingAuthorityCharacterizationV1.cs tests/OCGForge.Ignis.Gameplay.Tests/Tests/I6DCrossLocatorMappingAuthorityCharacterizationTests.cs tests/OCGForge.Ignis.Gameplay.Tests/Program.cs
git commit -m "test: freeze private occurrence binding design"
git push origin chris/i6g-final-cross-oracle-acceptance
```

Expected delivery remains design-only:

```text
PRODUCTION_CODE_CHANGED=NO
I6D_PRIVATE_SOURCE_OCCURRENCE_BINDING_IMPLEMENTATION=NO
I6G_COUNTER_CAPTURE_RETRY=NO
I6G_LINK_CAPTURE=NO
I6G_FRESH_PROCESS_A_B=NO
```
