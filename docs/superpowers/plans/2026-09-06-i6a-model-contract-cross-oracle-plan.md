# I6A Model-Contract Bundle and Cross-Oracle Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. The current I6A task is documentation-only and stops after its commit; the later runtime slices require fresh authorization.

**Goal:** Freeze an exact consumer bundle and a byte-exact, fail-closed cross-oracle plan for the OCGForge-Ignis I6 model boundary without implementing I6 or I7.

**Architecture:** OCGForge remains the semantic and canonical-byte owner. Ignis will later consume accepted public observation/candidate values, construct an independently validated OCGForge-compatible descriptor, and compare native OCGForge oracle artifacts at each explicit stage. Missing public events, unrepresentable prompt-local CardCode fields, unaccepted vocabulary content, rules mismatch, and unaccepted Task7 data authority remain explicit stops.

**Tech Stack:** Markdown contracts, exact Git source snapshots, C#/.NET 10 future adapter code, OCGForge C++/Python native oracle probes, canonical big-endian byte codecs, SHA-256 identities, and fresh-process deterministic harnesses. No runtime dependency, model framework, checkpoint, dataset, or network code is added by I6A.

---

## 1. Current I6A file map and stop boundary

The current task creates exactly these three Ignis documents:

```text
docs/contracts/ocgforge-model-contract-bundle-v1.md
docs/superpowers/specs/2026-09-06-i6a-model-contract-cross-oracle-design.md
docs/superpowers/plans/2026-09-06-i6a-model-contract-cross-oracle-plan.md
```

Their responsibilities are deliberately separate:

| File | Responsibility |
| --- | --- |
| `docs/contracts/ocgforge-model-contract-bundle-v1.md` | consumer-side registry, exact OCGForge IDs, canonical-byte/identity ownership, vocabulary/config binding, and no-alias rules |
| `docs/superpowers/specs/2026-09-06-i6a-model-contract-cross-oracle-design.md` | live audit, authority model, oracle ladder, field mapping, gaps, privacy/determinism, and acceptance boundary |
| `docs/superpowers/plans/2026-09-06-i6a-model-contract-cross-oracle-plan.md` | future file map, gated slice sequence, commands, tests, mismatch behavior, and delivery protocol |

```text
CURRENT_PRODUCTION_FILES=0
CURRENT_TEST_FILES=0
CURRENT_FIXTURE_FILES=0
CURRENT_OGCFORGE_FILES=0
I6_RUNTIME_IMPLEMENTATION_AUTHORIZED=NO
I7_AUTHORIZED=NO
```

## 2. Start guard for this task

Run from `C:\Users\chris\Documents\OCGForge-Ignis`:

```powershell
git fetch origin --prune
$expectedIgnisMain = 'e54f392d3688a28f2892c02998854349b2007a91'
$main = (git rev-parse origin/main).Trim()
if ($main -cne $expectedIgnisMain) {
    throw "STATUS=BLOCKED_BASE_MOVED ORIGIN_MAIN=$main EXPECTED=$expectedIgnisMain"
}
if (@(git status --short).Count -ne 0) {
    throw 'STATUS=BLOCKED_DIRTY_WORKTREE'
}
git switch main
git pull --ff-only
if ((git rev-parse HEAD).Trim() -cne $expectedIgnisMain) {
    throw 'STATUS=BLOCKED_LOCAL_MAIN_MISMATCH'
}
git switch -c chris/i6a-model-contract-cross-oracle-freeze
```

The OCGForge audit checkout is read-only. Verify its source anchor without
switching or modifying it:

```powershell
git -C C:\yogiohML fetch origin --prune
if ((git -C C:\yogiohML rev-parse origin/main).Trim() -cne
    '3edfcabf51dd914f96adc4df903b1ac2a9d20e5f') {
    throw 'STATUS=BLOCKED_OCGFORGE_BASE_MOVED'
}
```

If the OCGForge checkout is on a branch ahead of `origin/main`, use
`git show origin/main:<path>` for every authority read. Never use a newer
unmerged source commit as a semantic oracle.

## 3. Source-audit execution order

- [ ] **Step 1: Confirm the integrated Ignis prerequisite.**

  Verify PR #28 is merged with merge commit
  `e54f392d3688a28f2892c02998854349b2007a91`, parent 2 contains
  `d56d5a45bc6a858759946bac44a0905c0f4253d2`, and push CI
  `34026325229` is `completed/success` on that exact merge commit. Verify
  `SELECT_SUM=FAIL_CLOSED_UNSUPPORTED_V1` and
  `ANNOUNCE_CARD=FAIL_CLOSED_UNSUPPORTED` in Ignis.

- [ ] **Step 2: Read the OCGForge authority set.**

  Read the exact files listed in the design document at
  `origin/main=3edfcabf…`: ADR-0004 and ADR-0006, public observation/action/
  episodic contracts, all P5 contract/evidence files, model headers/sources,
  Task6 readiness, Task7 materialization contract/source/tests, and Task7
  dataset-authority contract. Record source path, owner, status, and exact
  identity rather than copying a summary claim.

- [ ] **Step 3: Separate authority classes.**

  Produce the fifteen-entry registry in the contract document. Put public
  environment/action/model semantics in the semantic class, canonical codecs
  in the encoding class, `CardVocabulary`/model/environment/action IDs in the
  identity class, batch/materialization tensors in physical/derived classes,
  and Task4 smoke artifacts outside the I6 bundle.

- [ ] **Step 4: Record unresolved source gaps.**

  Preserve these exact stops in the design and plan:

  ```text
  I6_PUBLIC_STATE_ORACLE=BLOCKED
  I6_EVENT_ORACLE=BLOCKED
  I6_PROMPT_LOCAL_CARDCODE_MAPPING=BLOCKED
  I6_FIXED_VOCABULARY_ARTIFACT=REQUIRES_SOURCE_PROOF
  I6_TASK7_FINAL_ACCEPTANCE=NOT_PROVEN
  I6_RULES_DOMAIN_COMPATIBILITY=DIFFERENT_OR_UNPROVEN
  I6_CHECKPOINT_COMPATIBILITY=NOT_AN_I6_FINAL_GATE
  I7_CHECKPOINT_COMPATIBILITY=UNRESOLVED
  ```

  Do not add a test or fallback that hides one of these values.

## 4. Future implementation file map

The following is the smallest proposed runtime shape after the blockers receive
their own decisions. No file below is changed by I6A.

### I6B — consumer bundle preflight

Owning layer: new `OCGForge.Ignis.Model` boundary; semantic changes: none;
purpose: validate the static consumer manifest and exact required OCGForge IDs.

```text
CREATE src/OCGForge.Ignis.Model/OCGForge.Ignis.Model.csproj
CREATE src/OCGForge.Ignis.Model/OcgForgeModelContractBundleV1.cs
CREATE tests/OCGForge.Ignis.Model.Tests/OCGForge.Ignis.Model.Tests.csproj
CREATE tests/OCGForge.Ignis.Model.Tests/I6BBundlePreflightTests.cs
MODIFY tests/OCGForge.Ignis.Model.Tests/Program.cs
```

The project may reference accepted Ignis Gameplay public value assemblies but
must not reference private response state, CoreHost, or a model framework.
The bundle loader rejects missing/duplicate/reordered IDs, floating source
references, invalid config identity, and wrong ownership metadata before
returning a binding.

### I6C — public-state and event source closure

Owning layer: separately authorized Ignis I3/public-state extension or an
accepted OCGForge-compatible frame source. The exact files are not yet
authorized because the current Ignis mirror lacks the full event/safe-state
source. Candidate implementation files, pending that decision, are:

```text
MODIFY src/OCGForge.Ignis.Gameplay/PerspectiveStateMirrorV1.cs
MODIFY src/OCGForge.Ignis.Gameplay/PerspectiveStateTypesV1.cs
MODIFY src/OCGForge.Ignis.Gameplay/PublicStateProjectionV1.cs
CREATE tests/OCGForge.Ignis.Gameplay.Tests/I6PublicEventSourceTests.cs
```

This is a separate I3 authority decision, not an automatic I6 permission. It
must add only accepted public event/state values, preserve I3D locator
authority, and never expose private Mirror IDs or raw protocol data. If the
missing fields are supplied by an external accepted frame instead, no Ignis
I3 production extension is needed and the three candidate files remain
untouched.

### I6D — public candidate and action-key bridge

Owning layer: `OCGForge.Ignis.Model`; semantic changes: exact field mapping
only, with no legality change.

```text
CREATE src/OCGForge.Ignis.Model/OcgForgePublicCandidateV1.cs
CREATE src/OCGForge.Ignis.Model/OcgForgePublicActionIdentityV1.cs
CREATE tests/OCGForge.Ignis.Model.Tests/I6DPublicCandidateKeyTests.cs
```

The implementation constructs one OCGForge descriptor for each accepted
Ignis candidate, calls an exact C# port of the published OCGForge public-action
codec, and compares the resulting descriptor bytes/key to a native OCGForge
oracle. The port is a codec, not a new semantic authority. Any prompt-local
CardCode, key collision, missing field, or ambiguous locator fails the entire
frame. No candidate is removed.

### I6E — logical/encoded model input and vocabulary bridge

Owning layer: `OCGForge.Ignis.Model`; dependencies: I6B, I6C, I6D, and an
explicit immutable vocabulary artifact.

```text
CREATE src/OCGForge.Ignis.Model/OcgForgeLogicalModelInputV1.cs
CREATE src/OCGForge.Ignis.Model/OcgForgeEncodedModelInputV1.cs
CREATE src/OCGForge.Ignis.Model/OcgForgeCardVocabularyV1.cs
CREATE tests/OCGForge.Ignis.Model.Tests/I6EModelInputTests.cs
```

The codec preserves exact public optional presence, integer width, source
order, locator-reference form, visible event order, and N-to-N candidate
pairing. It rejects a known public passcode absent from the supplied
vocabulary and never queries BabelCDB or hidden state.

### I6F — conditional Task7 materialization bridge

Owning layer: `OCGForge.Ignis.Model`; semantic changes: none. This slice is
not schedulable until the Task7 materialization semantics are accepted, the
implementation/source is available, the exact configuration identity is
validated, and native oracle vectors exist. Dataset and checkpoint readiness
are not prerequisites for this materialization-only bridge.

```text
CREATE src/OCGForge.Ignis.Model/OcgForgeTask7MaterializationV1.cs
CREATE tests/OCGForge.Ignis.Model.Tests/I6FTask7MaterializationTests.cs
```

The bridge validates the exact Task7 configuration identity and compares
canonical unpadded materialization bytes/tables. It never downgrades to Task4,
creates a dataset identity, or loads a checkpoint.

`DatasetManifest`, `TrainingDatasetSplitV1`, and a real checkpoint remain
outside I6F. Dataset membership belongs to the separately owned Task7 dataset
authority; checkpoint binding belongs to I7. Their absence must not be used to
block a valid materialization-only cross-oracle vector.

If the later path is supervised, it receives a separately validated
`ModelSupervisionSampleV1`/admission association. The Task7 materializer does
not derive `selected_public_action_key`, candidate ordinal, dataset identity,
or split identity from its physical rows.

### I6G — cross-oracle final acceptance

Owning layer: Model test/oracle boundary; semantic changes: none.

```text
CREATE tests/OCGForge.Ignis.Model.Tests/I6CrossOracleFinalAcceptanceTests.cs
CREATE fixtures/model/v1/i6-cross-oracle-vectors.v1.json
```

The fixture must be generated by a native OCGForge oracle tool from an exact
OCGForge source snapshot. It must not be hand-edited or generated from Ignis
output. If OCGForge has no suitable unified oracle emitter, an OCGForge-owned
tooling decision is required before this slice; Ignis cannot manufacture the
expected bytes.

## 5. Red-test-first future sequence

Every runtime slice follows this order and stops on the first unresolved
authority:

- [ ] **Step 1: Re-run both repository start guards.** Record exact Ignis and
  OCGForge source heads. Do not rebase or silently adopt a newer contract.

- [ ] **Step 2: Add a failing acceptance test.** The red test must fail because
  the intended I6 capability is absent, not because an oracle vector is
  malformed. For I6C/D/E, a still-open blocker is an expected structured stop,
  not permission to implement around it.

- [ ] **Step 3: Implement one owning-layer value seam.** Keep public model code
  in `OCGForge.Ignis.Model`; do not add it to the protocol transport or private
  Mirror reducer unless I6C receives its separate I3 authorization.

- [ ] **Step 4: Validate source authorities before constructing output.** The
  future order is:

  ```text
  validate bundle registry and exact source snapshot
  validate accepted public observation/safe-state bytes
  validate complete Ignis candidate domain and public field coverage
  map locators with exact current/historical rules
  construct OCGForge public candidates in source order
  recompute and compare public_action_key values
  construct logical input and compare canonical bytes
  encode with supplied CardVocabulary and compare bytes
  compute/compare model_input_identity
  optionally validate Task7 config/materialization
  ```

- [ ] **Step 5: Exercise negative cases before widening coverage.** Reject
  unknown IDs, source drift, missing fields, prompt-local CardCode without a
  target field, duplicate keys, reordered keys, missing vocabulary entries,
  hidden identity, invalid references, count/offset overflow, Task4 downgrade,
  and detached logical/encoded/materialization values. The result must contain
  no partial domain or private diagnostic payload.

- [ ] **Step 6: Run paired-world and fresh-process tests.** Compare exact public
  bytes, all model values, identities, routing sidecars, and Task7 bytes where
  applicable. Compare stdout, stderr, and exit code from two independent
  processes. Do not compare PID, path, device, or framework execution data.

- [ ] **Step 7: Commit only the authorized slice and stop.** No PR, merge, I7,
  model runner, checkpoint, training, or network response action is included.

## 6. Cross-oracle test catalog

The future test suite must use small reasoned vectors, not a large arbitrary
corpus:

| Vector | Required proof |
| --- | --- |
| flat scalar decision | public observation, typed choice, key, logical/encoded bytes |
| locator-bearing card decision | exact current locator/reference and accepted CardVocabulary |
| duplicate-looking candidates | N-to-N source order, distinct valid keys, no deduplication |
| I5 intermediate continuation | complete current domain, no private continuation state in model values |
| I5 terminal continuation | terminal status and private response remain outside public/model values |
| N=1 | one externally controlled candidate, no automatic answer |
| redacted/unknown card | ID 1 with real row mask, no hidden passcode |
| no persistent locator | prompt/public candidate remains only if OCGForge field coverage is complete; otherwise whole-frame fail closed |
| visible events | event-index order, historical references without current rebinding |
| multiple candidates | exact count, order, key sidecar, score-slot cardinality |
| large domain | arbitrary N and no fixed global cap where available |
| paired hidden worlds | equal public/model bytes and identities, different hidden source state |
| Task4 collision | old smoke float collision does not collapse exact Task7 limbs |
| optional presence | ABSENT differs from PRESENT(0) for every optional primitive |
| malformed source | structured fail closed with no partial output |

## 7. Future acceptance gates and exact authority

Each gate is closed only by the listed artifact, not by a green compile or a
shape-only comparison:

```text
I6_BUNDLE_REGISTRY_EXACT
    authority: bundle contract + native source snapshot
    proof: exact ordered entries and fixed IDs

I6_PUBLIC_OBSERVATION_BYTES
    authority: OCGForge public observation codec
    proof: native bytes == independent Ignis reconstruction

I6_SAFE_STATE_BYTES
    authority: OCGForge public_safe_state codec
    proof: strict decode/re-encode and byte equality

I6_LOCATOR_ORACLE
    authority: public observation/P5 reference rules
    proof: token table, current ordinals, historical references

I6_EVENT_ORACLE
    authority: public safe-state visible-event source
    proof: exact event fields/order; BLOCKED until Ignis source closure exists

I6_CANDIDATE_N_TO_N_ORDER
    authority: accepted Ignis public domain + OCGForge public candidate contract
    proof: count, source order, multiplicity, field-by-field equality

I6_PUBLIC_ACTION_KEY
    authority: OCGForge public_action_identity.v1 codec
    proof: canonical descriptor bytes and full key equality

I6_LOGICAL_INPUT_BYTES
    authority: OCGForge P5 logical codec
    proof: canonical byte equality and optional-presence equality

I6_ENCODED_INPUT_BYTES
    authority: OCGForge P5 encoded codec + vocabulary artifact
    proof: exact codes, limbs, masks, rows, and sidecar

I6_VOCABULARY_IDENTITY
    authority: explicit CardVocabulary manifest
    proof: canonical bytes and model_card_vocabulary.v1.<sha256>

I6_MODEL_INPUT_IDENTITY
    authority: canonical logical/encoded identity codec
    proof: exact model_input.v1.<sha256> equality

I6_TASK7_MATERIALIZATION
    authority: accepted Task7 contract/configuration
    proof: exact config identity and canonical unpadded sample bytes

I6_BATCH_LAYOUT
    authority: ModelBatchLayoutV1
    proof: ragged/padded/unpadded real-row roundtrip; excluded from model identity

I6_PAIRED_WORLD_PRIVACY
    authority: public observation + P5/Task7 privacy contracts
    proof: hidden-only mutation leaves every public/model oracle value equal

I6_FRESH_PROCESS_DETERMINISM
    authority: two independent native/consumer processes
    proof: bytes, identities, values, sidecars, stdout, stderr, exit codes equal

I6_FAIL_CLOSED
    authority: each owning contract's failure taxonomy
    proof: malformed/mismatch/unsupported input yields no partial output

I6_NO_I7_AUTHORITY
    authority: Ignis layer audit
    proof: no checkpoint loader, runner IPC, inference, scoring, or training path
```

For every gate, a mismatch invalidates the complete frame/batch. There is no
first-match mapping, candidate removal, alternate vocabulary, fallback model,
Task4 downgrade, automatic N=1 resolution, or retry under a different
authority.

## 8. Build and repository gates for later runtime slices

The exact test count is intentionally not frozen by I6A because no I6 test
harness exists yet. A later implementation must report actual counts, not reuse
the I5 count as an I6 claim. At minimum it must retain the current Ignis
regressions and add named I6 groups for bundle, source closure, action mapping,
model input, materialization, and final oracle acceptance.

Run from a clean Ignis checkout:

```powershell
dotnet build src/OCGForge.Ignis.Protocol/OCGForge.Ignis.Protocol.csproj --configuration Release
dotnet build src/OCGForge.Ignis.Client/OCGForge.Ignis.Client.csproj --configuration Release
dotnet build src/OCGForge.Ignis.Gameplay/OCGForge.Ignis.Gameplay.csproj --configuration Release
dotnet build tests/OCGForge.Ignis.Protocol.Tests/OCGForge.Ignis.Protocol.Tests.csproj --configuration Release
dotnet build tests/OCGForge.Ignis.Client.Tests/OCGForge.Ignis.Client.Tests.csproj --configuration Release
dotnet build tests/OCGForge.Ignis.Gameplay.Tests/OCGForge.Ignis.Gameplay.Tests.csproj --configuration Release
```

Run every applicable harness twice in independent processes and compare complete
stdout, stderr, and exit code. Run `git diff --check` and a tracked-plus-
untracked scope audit. For OCGForge native oracle changes, run the exact CMake/
CTest/Python command recorded by the OCGForge authority artifact; do not call
historical P5 or Task7 text a fresh pass.

## 9. Current-task validation and delivery

The current I6A task has no runtime tests to execute. After writing the three
documents, run:

```powershell
git diff --check
$base = 'e54f392d3688a28f2892c02998854349b2007a91'
$tracked = @(git diff --name-only $base)
$untracked = @(git ls-files --others --exclude-standard)
$changed = @($tracked + $untracked | Sort-Object -Unique)
$expected = @(
    'docs/contracts/ocgforge-model-contract-bundle-v1.md',
    'docs/superpowers/specs/2026-09-06-i6a-model-contract-cross-oracle-design.md',
    'docs/superpowers/plans/2026-09-06-i6a-model-contract-cross-oracle-plan.md'
)
if (@($changed).Count -ne 3 -or
    (Compare-Object $expected $changed).Count -ne 0) {
    throw 'STATUS=BLOCKED_SCOPE_MISMATCH'
}
git status --short
```

No Markdown/link checker is configured in Ignis. Validate internal links by
checking every relative Markdown target in the three new files against the
Ignis tree; external OCGForge URLs are references and are not downloaded into
the repository. This is documentation validation, not I6 runtime evidence.

Commit exactly:

```text
docs: freeze I6A model contract bundle and cross oracle
```

Push exactly:

```text
chris/i6a-model-contract-cross-oracle-freeze
```

Then stop. Do not create a PR, merge, switch to OCGForge, change OCGForge,
begin I6B, begin I7, or claim I6 runtime acceptance.

## 10. Required later handoff states

The future implementation agent must report actual values for every gate and
must preserve the following barriers:

```text
I6A_DESIGN_REVIEW=INDEPENDENT_REVIEW_REQUIRED
I6_RUNTIME_IMPLEMENTATION_AUTHORIZED=NO_UNLESS_SEPARATELY_AUTHORIZED
I6_FINAL=NO
I7_AUTHORIZED=NO
I7_STARTED=NO
```

Current known unresolved states remain:

```text
I6_EVENT_ORACLE=BLOCKED
I6_PROMPT_LOCAL_CARDCODE_MAPPING=BLOCKED
I6_FIXED_VOCABULARY_ARTIFACT=REQUIRES_SOURCE_PROOF
I6_TASK7_FINAL_ACCEPTANCE=NOT_PROVEN
I6_RULES_DOMAIN_COMPATIBILITY=DIFFERENT_OR_UNPROVEN
I6_CHECKPOINT_COMPATIBILITY=NOT_AN_I6_FINAL_GATE
I7_CHECKPOINT_COMPATIBILITY=UNRESOLVED
```
