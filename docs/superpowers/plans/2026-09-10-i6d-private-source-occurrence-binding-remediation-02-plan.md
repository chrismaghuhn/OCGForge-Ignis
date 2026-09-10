# I6D Private Source Occurrence Binding Remediation 02 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Characterize the real pre-I6D duplicate-own-hand failure and freeze the Option A private I4 occurrence sidecar plus its controlled Gameplay-to-Model handoff without changing production behavior.

**Architecture:** The public I3D/I4 projection remains byte- and identity-compatible. A future internal I4 sidecar will be produced beside that projection from the same exact mirror occurrence and the same public ordinal assignment, then a private I6D carrier will combine it with the same-frame I6C5 target. The current slice only documents and tests this design; it does not add the sidecar or mapping implementation.

**Tech Stack:** .NET 10, C# console-style Gameplay tests, existing `FlatPromptSessionV1` / `FlatPromptCardCorrelationV1` path, Markdown contract documentation.

---

### Task 1: Characterize the real I4 duplicate-own-hand failure

**Files:**
- Modify: `tests/OCGForge.Ignis.Gameplay.Tests/Tests/I6DCrossLocatorMappingAuthorityCharacterizationTests.cs`
- Modify: `tests/OCGForge.Ignis.Gameplay.Tests/Program.cs`

- [x] **Step 1: Build a duplicate own-hand mirror and accepted public projection**

Use `CreateMirror` and two existing `MoveMessage` values with the same nonzero CardCode and normalized hand locations `(controller=0, location=0x02, sequence=0/1)`. Apply both to the same mirror, then call `PublicStateProjectionV1.TryProject` with the mirror snapshot and accepted duel flags.

- [x] **Step 2: Feed a real `MSG_SELECT_IDLECMD` wire vector through I4**

Construct the complete modern idle wire value with two `SUMMON` entries containing that same CardCode and the two hand sequences, zero entries for the other five sections, and three zero transition flags. Pass it to `new FlatPromptSessionV1().TryAcceptPrompt(message, mirror, acceptedProjection)`.

- [x] **Step 3: Assert the exact pre-I6D failure**

Assert that both public hand cards exist in the accepted I4 snapshot, that direct `FlatPromptCardCorrelationV1.TryCorrelate` calls for sequences 0 and 1 return `false` with `FlatPromptErrorCodeV1.UnprovenPublicReference`, and that the full prompt result is unsuccessful with null context/candidates. This proves `TryCorrelatePile -> CompleteCorrelation`, no complete I4 candidate domain, and no I6D boundary reach. Do not log CardCode, sequence values, or raw prompt bytes.

- [x] **Step 4: Register a separate characterization test**

Register `TestDuplicateOwnHandI4StopsBeforeI6DBoundary` in the Gameplay test runner. Keep it distinct from the already accepted synthetic cross-locator observations.

---

### Task 2: Freeze the Option A I4 sidecar amendment and assembly handoff

**Files:**
- Modify: `docs/superpowers/specs/2026-09-10-i6d-cross-locator-mapping-authority-characterization.md`
- Create: `docs/contracts/flat-prompt-correlation-sidecar-v1.md`
- Modify: `tests/OCGForge.Ignis.Gameplay.Tests/Fixtures/I6DCrossLocatorMappingAuthorityCharacterizationV1.cs`
- Modify: `tests/OCGForge.Ignis.Gameplay.Tests/Tests/I6DCrossLocatorMappingAuthorityCharacterizationTests.cs`

- [x] **Step 1: Document the explicit I4 contract amendment**

Keep the frozen `docs/contracts/flat-prompt-projection-v1.md` unchanged and add the explicit design-only companion contract `ocgforge-ignis.flat-prompt-correlation-sidecar.v1`. State that the amendment is private prompt-correlation provenance only: `public-state-projection.v1` canonical bytes, public locators, and identity remain unchanged. During the same I3D projection, an internal immutable sidecar records exact normalized source occurrence to the already assigned I4 public ordinal. It may use `MirrorEntityIdV1` only as a transient same-snapshot join and never stores or publishes it.

- [x] **Step 2: Freeze sidecar lookup and lifecycle**

Require the sidecar to be scoped by accepted projection/frame, source occurrence `(absolute controller, normalized zone, sequence, overlay index)`, and exact source section/ordinal. Its lookup is exact and one-to-one; missing, ambiguous, stale, or colliding entries fail the complete prompt. No CardCode-only, sequence-as-public-locator, first-match, collection-order, or physical-continuity fallback is allowed.

- [x] **Step 3: Freeze the controlled assembly handoff**

Document the exact current-graph mechanism: Gameplay owns the internal immutable sidecar and `PrivateCrossLocatorBindingV1`; `FlatPromptSessionV1.TryCreateI6DPrivateBindingHandoff(...)` returns one public opaque capability as the only Gameplay-to-Model crossing. It has no public constructor or private data surface and exposes only validated safe-target retrieval. No `InternalsVisibleTo("OCGForge.Ignis.Model")` is added, and Model does not access mirror snapshots, raw loc_info, or private occurrence fields. The public bridge still consumes only `OcgForgeAcceptedDecisionBoundaryV1`, whose private capability is complete and non-detached.

The only cross-assembly operation is the following internal shape:

```text
public opaque TryGetValidatedTarget(
    prompt_instance,
    continuation_step,
    frame_instance,
    projection_id,
    local_key,
    source_section,
    source_ordinal,
    current_frame,
    out target,
    out error)
```

- [x] **Step 4: Record the decision and non-goals**

Freeze `OPTION_A=SELECTED`, `OPTION_B=REJECTED_FOR_PUBLIC_CONTRACT_CHANGE`, and `OPTION_C=REJECTED_FOR_COMPLETE_DOMAIN`. Mark the I4 amendment and production binding implementation as designed but not implemented. Keep Counter retry, Link capture, and fresh-process A/B unauthorized.

- [x] **Step 5: Add a test-only design manifest and assertions**

Extend the existing characterization fixture with a manifest for the sidecar field groups, lookup key, assembly facade, public-identity exclusions, and rejection requirements. Add assertions for the exact Option A choice, the duplicate case, the opponent-public-hand exact-token path, cross-pile source binding, and the absence of a production implementation. Requirements must remain labeled `REQUIRED`, not `PASS`.

---

### Task 3: Verify and deliver the design-only slice

**Files:**
- No production files.

- [x] **Step 1: Run Release Gameplay tests**

Run:

```powershell
dotnet run --project tests/OCGForge.Ignis.Gameplay.Tests/OCGForge.Ignis.Gameplay.Tests.csproj --configuration Release --no-restore
```

Expected: the new characterization and design tests pass, with no production source changes.

- [x] **Step 2: Run Release Model, Protocol, and Client tests**

Run the existing three Release test projects and require zero failures.

- [x] **Step 3: Check scope and hygiene**

Run `git diff --check`, inspect staged names, and reject any `src/` file in the diff. Verify the original dirty checkout remains untouched.

- [x] **Step 4: Commit and push**

```powershell
git add docs/superpowers/plans/2026-09-10-i6d-private-source-occurrence-binding-remediation-02-plan.md docs/contracts/flat-prompt-correlation-sidecar-v1.md docs/superpowers/specs/2026-09-10-i6d-cross-locator-mapping-authority-characterization.md tests/OCGForge.Ignis.Gameplay.Tests/Fixtures/I6DCrossLocatorMappingAuthorityCharacterizationV1.cs tests/OCGForge.Ignis.Gameplay.Tests/Tests/I6DCrossLocatorMappingAuthorityCharacterizationTests.cs tests/OCGForge.Ignis.Gameplay.Tests/Program.cs
git commit -m "test: characterize duplicate hand binding boundary"
git push origin chris/i6g-final-cross-oracle-acceptance
```

Expected delivery:

```text
PRODUCTION_CODE_CHANGED=NO
I6D_PRIVATE_SOURCE_OCCURRENCE_BINDING_IMPLEMENTATION=NO
I6G_COUNTER_CAPTURE_RETRY=NO
I6G_LINK_CAPTURE=NO
I6G_FRESH_PROCESS_A_B=NO
```
