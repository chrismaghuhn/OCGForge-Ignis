# I5A0 Combinatorial Prompt / Continuation Contract Freeze Plan

> **For agentic workers:** This plan is an audit and future-planning artifact. The current I5A0 result is blocked by `SELECT_SUM`; do not implement any I5 runtime task until an independent review resolves that blocker and explicitly authorizes a later implementation slice.

**Goal:** Freeze a source-grounded combinatorial prompt/continuation contract for the twelve planned I5 families, or stop with a precise contract-executability finding.

**Architecture:** A future adapter will own one private continuation state per original prompt and expose only the complete current semantic domain. It will reuse the I4B mirror/public-snapshot authority seam. I5 will not own network sending, model input, OCGForge action identity, or continuation authority outside the current prompt.

**Tech Stack:** Markdown contracts, strict UTF-8 JSON fixtures, C#/.NET 10 future runtime slices, explicit little-endian codecs, value-owned continuation state, and executable deterministic harnesses. No new dependency is authorized by this plan.

## 1. Current status and hard stop

The accepted implementation base is:

    BASE=d3a340977974260ed9242118eb68fdfb6c0127f8
    BRANCH=chris/i5a0-combinatorial-continuation-contract-freeze

The current task changes exactly six repository files: four documentation
artifacts and two gameplay fixtures. No runtime, test, network, model, or
workflow file is part of this task.

The current audit has:

    TARGET_FAMILY_COUNT=12
    CONTRACT_FROZEN_FAMILY_COUNT=11
    SELECT_SUM_CONTRACT=UNRESOLVED
    I5A0_CONTRACT_FREEZE=NO
    I5_IMPLEMENTATION_AUTHORIZED=NO

The future worker must stop before any code change if the contract file does
not contain an independently accepted `I5A0_CONTRACT_FREEZE=YES` and
`SELECT_SUM_CONTRACT=FROZEN`. A green build, a plausible sum algorithm, or a
fixture-only workaround is not authorization.

## 2. Future start guard

This guard is for a later implementation branch after the contract blocker has
been independently resolved. It is not run as a permission to start I5 on the
current blocked branch.

Run from `C:\Users\chris\Documents\OCGForge-Ignis`:

    git fetch origin --prune
    $branch = (git branch --show-current).Trim()
    $head = (git rev-parse HEAD).Trim()
    $contractPath = 'docs/contracts/combinatorial-prompt-continuation-v1.md'
    $contractText = Get-Content $contractPath -Raw

    if ($branch -notmatch '^chris/i5') {
        throw "WRONG_BRANCH BRANCH=$branch"
    }
    if (@(git status --short).Count -ne 0) {
        throw "STATUS=BLOCKED_DIRTY_WORKTREE"
    }
    if ($contractText -notmatch '(?m)^\s*I5A0_CONTRACT_FREEZE=YES\s*$') {
        throw "STATUS=BLOCKED_CONTRACT_NOT_ACCEPTED"
    }
    if ($contractText -match '(?m)^\s*SELECT_SUM_CONTRACT=UNRESOLVED\s*$') {
        throw "STATUS=BLOCKED_SELECT_SUM_UNRESOLVED"
    }

    The later authorization message must supply the exact accepted main SHA,
    accepted contract/plan commit SHA, target branch, and feature parent. The
    worker must compare those exact values before switching or editing. It may
    not infer a newer main, rebase, amend, or silently adopt a different
    contract revision.

## 3. Current six-file documentation/evidence scope

The current task is limited to:

    MODIFY  PROTOCOL_PROVENANCE.md
    CREATE  docs/contracts/combinatorial-prompt-continuation-v1.md
    CREATE  docs/superpowers/specs/2026-09-05-i5-combinatorial-continuation-design.md
    CREATE  docs/superpowers/plans/2026-09-05-i5-combinatorial-continuation-plan.md
    MODIFY  fixtures/gameplay/v1/game-message-support.v1.json
    CREATE  fixtures/gameplay/v1/i5-combinatorial-prompt-vectors.v1.json

The current task must finish with:

    FILES_CHANGED=6
    PRODUCTION_CODE_CHANGED=NO
    TEST_CODE_CHANGED=NO
    WORKFLOW_CHANGED=NO

If any additional path is needed to make the contract executable, stop with
`STATUS=BLOCKED_SCOPE_MISMATCH`; do not expand the scope.

## 4. Future implementation file map

The following is a sequence of separate future slices. None is authorized by
this plan. Each slice gets its own exact base, feature parent, review, and
stop boundary.

### I5A1 — bounded card selection and flat terminal choices

Families: `SELECT_CARD`, `SELECT_TRIBUTE`, `SELECT_UNSELECT_CARD`,
`ANNOUNCE_NUMBER`.

    MODIFY
    src/OCGForge.Ignis.Gameplay/FlatPromptTypesV1.cs
    src/OCGForge.Ignis.Gameplay/FlatPromptProjectionV1.cs
    src/OCGForge.Ignis.Gameplay/FlatPromptSessionV1.cs
    src/OCGForge.Ignis.Gameplay/FlatPromptCardCorrelationV1.cs
    tests/OCGForge.Ignis.Gameplay.Tests/Program.cs

    CREATE
    tests/OCGForge.Ignis.Gameplay.Tests/Tests/I5A1SelectionPromptTests.cs

Use anonymous source-occurrence variants for code-only SELECT_CARD entries.
Use the existing I4B authority overload for addressed entries. Preserve
weighted tribute values privately and use one canonical `FinishOrCancel`
variant where SELECT_UNSELECT's two flags share the one `-1` response.

### I5A2 — field-place and mask continuations

Families: `SELECT_PLACE`, `SELECT_DISFIELD`, `ANNOUNCE_RACE`,
`ANNOUNCE_ATTRIB`.

    MODIFY
    src/OCGForge.Ignis.Gameplay/FlatPromptTypesV1.cs
    src/OCGForge.Ignis.Gameplay/FlatPromptProjectionV1.cs
    src/OCGForge.Ignis.Gameplay/FlatPromptSessionV1.cs
    tests/OCGForge.Ignis.Gameplay.Tests/Program.cs

    CREATE
    tests/OCGForge.Ignis.Gameplay.Tests/Tests/I5A2PlaceAndMaskPromptTests.cs

Derive semantic field places from the explicit acting-player-relative mask
groups, and serialize the final triples in the fixed family order. Do not
expose raw mask offsets as card locators.

### I5A3 — exact counter allocation

Family: `SELECT_COUNTER`.

    MODIFY
    src/OCGForge.Ignis.Gameplay/FlatPromptTypesV1.cs
    src/OCGForge.Ignis.Gameplay/FlatPromptProjectionV1.cs
    src/OCGForge.Ignis.Gameplay/FlatPromptSessionV1.cs
    src/OCGForge.Ignis.Gameplay/FlatPromptCardCorrelationV1.cs
    tests/OCGForge.Ignis.Gameplay.Tests/Program.cs

    CREATE
    tests/OCGForge.Ignis.Gameplay.Tests/Tests/I5A3CounterPromptTests.cs

Use ordered `ASSIGN_AMOUNT` steps. Generate every amount that preserves an
exact feasible completion, including zero, and reject negative signed response
values before private binding.

### I5A4 — sequential ordering

Families: `SORT_CARD`, `SORT_CHAIN`.

    MODIFY
    src/OCGForge.Ignis.Gameplay/FlatPromptTypesV1.cs
    src/OCGForge.Ignis.Gameplay/FlatPromptProjectionV1.cs
    src/OCGForge.Ignis.Gameplay/FlatPromptSessionV1.cs
    src/OCGForge.Ignis.Gameplay/FlatPromptCardCorrelationV1.cs
    tests/OCGForge.Ignis.Gameplay.Tests/Program.cs

    CREATE
    tests/OCGForge.Ignis.Gameplay.Tests/Tests/I5A4SortPromptTests.cs

Choose one unplaced source occurrence for each next destination position. The
private codec writes the source-indexed permutation bytes. Do not enumerate an
N-factorial terminal domain and do not use a card identity to disambiguate
duplicate-looking occurrences.

### I5A5 — SELECT_SUM exact oracle

Family: `SELECT_SUM` only.

    MODIFY
    src/OCGForge.Ignis.Gameplay/FlatPromptTypesV1.cs
    src/OCGForge.Ignis.Gameplay/FlatPromptProjectionV1.cs
    src/OCGForge.Ignis.Gameplay/FlatPromptSessionV1.cs
    tests/OCGForge.Ignis.Gameplay.Tests/Program.cs

    CREATE
    src/OCGForge.Ignis.Gameplay/CombinatorialSumOracleV1.cs
    tests/OCGForge.Ignis.Gameplay.Tests/Tests/I5A5SelectSumOracleTests.cs

Do not begin this slice until the exact packed-value/sign and accumulator
contract is separately accepted. The optimized oracle must be compared with
an independent brute-force oracle over bounded generated inputs before any
runtime acceptance. A heuristic, greedy, or unsigned reinterpretation is not
an implementation of this slice.

### I5A6 — cross-family I5 acceptance and I5 FINAL

All twelve families after their individual acceptance:

    MODIFY
    tests/OCGForge.Ignis.Gameplay.Tests/Program.cs

    CREATE
    tests/OCGForge.Ignis.Gameplay.Tests/Tests/I5CrossFamilyFinalAcceptanceTests.cs

This final test-only slice proves cross-family continuation lifecycle, stale
instances, failure atomicity, complete-domain preservation, public/private
reflection, paired-world privacy, value-level determinism, and absence of I6/
I8 authority. It does not add another parser or response sender.

Across all future slices the unique planned file totals are:

    FUTURE_PRODUCTION_FILES=6
    FUTURE_TEST_FILES=7
    FUTURE_TOTAL_FILES=13
    FUTURE_NEW_GAMEPLAY_TEST_GROUPS=12
    CURRENT_GAMEPLAY=108
    FUTURE_GAMEPLAY_EXPECTED=120/120

The twelve proposed top-level groups are:

    1  card selection domain and codec
    2  weighted tribute domain and feasibility
    3  select/unselect and number terminal choices
    4  place/disfield exact mask domain
    5  race/attribute mask domain
    6  counter exact allocation
    7  sequential card/chain sorting
    8  SELECT_SUM exact oracle and generated cross-check
    9  malformed wire and final-response codec boundaries
    10 continuation lifecycle, staleness, and failure atomicity
    11 authority, privacy, ownership, and hidden-information pairs
    12 cross-family deterministic regression and I5/I6 barrier

## 5. Future red-test-first execution order

No red test is added in the current blocked I5A0 task. After independent
contract acceptance, each future slice follows this order:

1. Re-run the exact start guard and record the accepted contract/feature base.
2. Add a failing test for the smallest contract rule in the slice. The test
   must fail because the capability is absent, not because the fixture is
   malformed accidentally.
3. Implement only the private parser/draft, typed public variant, complete
   current-domain derivation, and private binding needed for that slice.
4. Add negative tests before broadening positive coverage: truncation,
   trailing bytes, overflow, invalid flags/ranges, no completion, ambiguity,
   and stale actions.
5. Verify the exact response golden from source occurrence state. Never let a
   public key or candidate ordinal stand in for a private response body.
6. Run the focused group, then the complete Gameplay/Protocol/Client gates in
   fresh processes, and inspect the exact scope diff.
7. Commit, push, and stop for independent review. No PR or merge is part of a
   slice unless separately authorized.

## 6. Authority-validation order for future card-bearing slices

The implementation order is fixed:

    parse one complete private wire draft
    validate successful accepted projection and snapshot
    capture mirror.Snapshot exactly once
    reproject captured mirror with accepted snapshot duel flags
    compare canonical bytes, SHA-256, and PublicProjectionId
    resolve every required source occurrence against captured mirror only
    correlate to exactly one accepted snapshot card
    copy locator/CardCode only from accepted snapshot
    build the complete public current domain
    independently validate candidate/type/section/key/response binding
    commit continuation state atomically

After capture there is no live mirror access. The recomputed projection is a
consistency proof only. POSITION's validated mask remains independent of card
correlation and an unproven position CardCode remains structurally absent.

## 7. Candidate and continuation binding checks

For every future public candidate, validation must reconstruct the expected:

    family
    exact sealed runtime type
    choice kind
    source section
    source ordinal or semantic field/bit/amount token
    canonical local key
    exact private response fragment

The binding owns copied arrays and private response state. Public candidates
never contain response integers, variable response bytes, raw loc_info,
sum_param, mirror IDs, or source object references. A successful transition
stales every previous-step handle. A failed transition leaves no partially
mutated continuation or response and does not emit a network packet.

## 8. Future acceptance gates

The twelve future Gameplay registrations are fixed to these coherent groups;
they are not added by the current documentation task:

| Group | Families | Required evidence |
| ---: | --- | --- |
| 1 | SELECT_CARD | modern exact wire, code-only anonymous entries, min/max, duplicates, finish/cancel, index-list codec |
| 2 | SELECT_TRIBUTE | weighted release values, count bounds, feasibility, duplicate occurrences, exact response body |
| 3 | SELECT_UNSELECT_CARD, ANNOUNCE_NUMBER | both toggle sections, shared -1 terminal rule, duplicate numbers, flat N=1 external choice |
| 4 | SELECT_PLACE, SELECT_DISFIELD | relative field-mask groups, player transform, disabled-field distinction, canonical triples |
| 5 | ANNOUNCE_RACE, ANNOUNCE_ATTRIB | admitted bit universes, exact popcount, duplicate-free bit steps, mask codec |
| 6 | SELECT_COUNTER | every zero-through-capacity assignment, exact remaining-capacity oracle, nonnegative amount codec |
| 7 | SORT_CARD, SORT_CHAIN | sequential source occurrences, duplicate preservation, source-indexed permutation, unchanged-order cancel |
| 8 | SELECT_SUM | exact resolved oracle only, independent brute-force cross-check, all bounded edge vectors |
| 9 | all applicable families | malformed/truncated/trailing lengths, overflow, invalid flags/ranges, exact response-size rejection |
| 10 | all continuation families | instance/step/key membership, atomic transitions, stale prior-step handles, no intermediate response |
| 11 | card-bearing families | I3D publication authority, mirror snapshot once, ambiguity, Main Deck/overlay/hidden privacy, source ownership |
| 12 | all twelve families | paired privacy worlds, value-level fresh-process determinism, I4 regression, I5/I6 layer barrier |

Groups 1--7 and 9--12 may be implemented before group 8 only if the final
contract explicitly excludes SELECT_SUM from their accepted family set until
its blocker is resolved. No slice may claim all-twelve I5 support before group
8 passes the exact-oracle gate.

Every future I5 slice must preserve the existing regression harnesses:

    Protocol=20/20
    Client=17/17
    Gameplay=the exact expected count for that slice

The final I5 acceptance additionally requires:

    I4_FINAL=YES
    I5A0_TARGET_FAMILY_COUNT=12
    I5A0_SUPPORTED_CONTRACT_FAMILY_COUNT=12
    ANNOUNCE_CARD_SUPPORT=FAIL_CLOSED_UNSUPPORTED
    I5_MESSAGE_IDS_FROZEN=PASS
    I5_MODERN_WIRE_GRAMMARS_FROZEN=PASS
    I5_RESPONSE_CODECS_FROZEN=PASS
    I5_CONTINUATION_MODEL_FROZEN=PASS
    I5_CURRENT_DOMAIN_COMPLETENESS=PASS
    I5_TERMINAL_COMPLETION_REACHABILITY=PASS
    I5_DUPLICATE_OCCURRENCE_PRESERVATION=PASS
    I5_FINISH_SEMANTICS_FROZEN=PASS
    I5_CANCEL_SEMANTICS_FROZEN=PASS
    I5_N1_AUTOANSWER=NO
    I5_INTERMEDIATE_PROTOCOL_RESPONSE=ABSENT
    I5_NETWORK_RESPONSE_SENDING=ABSENT
    I5_PUBLIC_PRIVATE_SEAM=PASS
    I5_PUBLICATION_AUTHORITY=I3D
    I5_PRIVATE_RESPONSE_IS_MODEL_INPUT=NO
    I5_CONTINUATION_STATE_DETERMINISTIC=PASS
    I5_LOCAL_KEY_EQUALS_OCGFORGE_PUBLIC_ACTION_KEY=NO
    SELECT_SUM_EXACT_SEMANTICS=PASS
    SELECT_SUM_EXACT_ORACLE_CONTRACT=PASS
    SELECT_SUM_HEURISTIC_ORACLE_ALLOWED=NO
    I6_AUTHORITY_ACQUIRED=NO
    MODEL_INPUT_AUTHORITY_ACQUIRED=NO
    NETWORK_SEND_AUTHORITY_ACQUIRED=NO

Run each applicable harness twice in independent processes and compare complete
stdout, stderr, and exit code. Run the six Release projects both normally and
with `--no-restore`, for twelve build invocations, and require zero warnings
and errors. Also require strict JSON validation, independent reconstruction of
every vector and terminal response, `git diff --check`, and an exact tracked+
untracked scope audit.

## 9. Current-task final gate and stop boundary

The current blocked audit task must use these checks after the six artifacts
are written:

    $base = 'd3a340977974260ed9242118eb68fdfb6c0127f8'
    git diff --check
    git status --short
    $tracked = @(git diff --name-only $base)
    $untracked = @(git ls-files --others --exclude-standard)
    $changed = @($tracked + $untracked | Sort-Object -Unique)

The expected six paths are exactly the four named Markdown files and the two
named JSON fixtures. The current task must not claim a final contract pass:

    I5A0_CONTRACT_FREEZE_FINAL_PASS=NO
    I5_IMPLEMENTATION_AUTHORIZED=NO
    I5_IMPLEMENTED=NO
    I5_FINAL=NO
    I6_AUTHORIZED=NO
    PR_CREATED=NO

If the blocked research result is retained, a commit may record the audit and
the six-file evidence scope using the requested message. That commit is not a
contract acceptance and does not authorize a future implementation. If any
future reviewer requires the unresolved artifact not be committed, stop with
the worktree clean-up decision explicitly requested; never hide the finding.

## 10. Commit/push protocol

For a completed six-file audit artifact, the only permitted commit message is:

    docs: freeze I5 combinatorial continuation contracts

Push only:

    chris/i5a0-combinatorial-continuation-contract-freeze

Do not create a PR, merge, begin I5A/I5B/I5C/I5D/I5E, or authorize I6. Stop
after the push for independent review of the blocked `SELECT_SUM` finding.
