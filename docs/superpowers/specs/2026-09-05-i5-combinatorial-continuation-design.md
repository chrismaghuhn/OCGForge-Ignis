# OCGForge-Ignis I5A0 — Combinatorial Prompt / Continuation Design

Status: DESIGN AND CONTRACT-FREEZE AUDIT ACCEPTED. `SELECT_SUM` is intentionally
fail-closed unsupported for V1; the eleven-family contract passed independent
review and no I5 runtime implementation is authorized.

Date: 2026-09-05

Accepted base:

    d3a340977974260ed9242118eb68fdfb6c0127f8

## 1. Decision summary

The smallest safe I5A0 result is a documentation and evidence slice. It
audits the twelve requested prompt families, records their modern wire
grammars and response consumers, and defines a future continuation contract.
It does not add a parser, continuation state machine, response sender, model
input, or OCGForge compatibility layer.

The research result is:

    I5A0_ARTIFACT_CLASSIFICATION=DOCS_FIXTURE_ONLY
    I5_IMPLEMENTATION_AUTHORIZED=NO
    SELECT_SUM_WIRE_GRAMMAR=RESOLVED
    SELECT_SUM_SUPPORT=FAIL_CLOSED_UNSUPPORTED_V1
    I5A0_CONTRACT_FREEZE=YES_FOR_11_FAMILIES

Eleven families have a closed protocol/domain design in the companion
contract draft. `SELECT_SUM` has a researched wire grammar but is deliberately
outside the admitted V1 family domain because the exact pinned core does not
make its unrestricted legality reconstructable from the prompt. This is an
explicit fail-closed scope decision, not an invitation to choose a convenient
interpretation.

## 2. Scope and authority

I4 is final and remains unchanged. Its seven families are already implemented
and integrated. I5A0 researches exactly:

    SELECT_CARD          15
    SELECT_TRIBUTE       20
    SELECT_SUM           23
    SELECT_PLACE         18
    SELECT_DISFIELD      24
    SELECT_COUNTER       22
    SORT_CARD            25
    SORT_CHAIN           21
    ANNOUNCE_RACE       140
    ANNOUNCE_ATTRIB     141
    ANNOUNCE_NUMBER     143
    SELECT_UNSELECT_CARD 26

`ANNOUNCE_CARD = 142` remains `FAIL_CLOSED_UNSUPPORTED` and is not part of the
twelve-family count.

The exact clean-room source pins are:

| Repository | Commit | Use |
| --- | --- | --- |
| `edo9300/ygopro-core` | `46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57` | Primary producer and response-validation authority |
| `edo9300/edopro` | `30935e847165a9ef0e547fb51a43f36168fab7c7` | Primary modern client reader and response-byte producer |
| `ProjectIgnis/windbot` | `bffe6b62679c8b2fafea8f59740e03a132517da4` | Secondary corroboration only |

The full source ledger is in `PROTOCOL_PROVENANCE.md`. No source
implementation, parser, control flow, or serialized upstream packet is
copied.

## 3. Existing baseline evidence

The baseline was run before any I5A0 document or fixture work.

Commands:

    dotnet build src/OCGForge.Ignis.Protocol/OCGForge.Ignis.Protocol.csproj --configuration Release
    dotnet build src/OCGForge.Ignis.Protocol/OCGForge.Ignis.Protocol.csproj --configuration Release --no-restore
    dotnet build src/OCGForge.Ignis.Client/OCGForge.Ignis.Client.csproj --configuration Release
    dotnet build src/OCGForge.Ignis.Client/OCGForge.Ignis.Client.csproj --configuration Release --no-restore
    dotnet build src/OCGForge.Ignis.Gameplay/OCGForge.Ignis.Gameplay.csproj --configuration Release
    dotnet build src/OCGForge.Ignis.Gameplay/OCGForge.Ignis.Gameplay.csproj --configuration Release --no-restore
    dotnet build tests/OCGForge.Ignis.Protocol.Tests/OCGForge.Ignis.Protocol.Tests.csproj --configuration Release
    dotnet build tests/OCGForge.Ignis.Protocol.Tests/OCGForge.Ignis.Protocol.Tests.csproj --configuration Release --no-restore
    dotnet build tests/OCGForge.Ignis.Client.Tests/OCGForge.Ignis.Client.Tests.csproj --configuration Release
    dotnet build tests/OCGForge.Ignis.Client.Tests/OCGForge.Ignis.Client.Tests.csproj --configuration Release --no-restore
    dotnet build tests/OCGForge.Ignis.Gameplay.Tests/OCGForge.Ignis.Gameplay.Tests.csproj --configuration Release
    dotnet build tests/OCGForge.Ignis.Gameplay.Tests/OCGForge.Ignis.Gameplay.Tests.csproj --configuration Release --no-restore

All twelve build invocations exited zero. The Release output reported zero
warnings and zero errors.

Two independent process runs were executed for each command below:

    dotnet run --project tests/OCGForge.Ignis.Protocol.Tests/OCGForge.Ignis.Protocol.Tests.csproj --configuration Release --no-build --no-restore
    dotnet run --project tests/OCGForge.Ignis.Client.Tests/OCGForge.Ignis.Client.Tests.csproj --configuration Release --no-build --no-restore
    dotnet run --project tests/OCGForge.Ignis.Gameplay.Tests/OCGForge.Ignis.Gameplay.Tests.csproj --configuration Release --no-build --no-restore

The results were:

    Protocol: passed=20 failed=0 on both runs; stdout/stderr/exit identical
    Client:   passed=17 failed=0 on both runs; stdout/stderr/exit identical
    Gameplay: passed=108 failed=0 on both runs; stdout/stderr/exit identical

The Gameplay output included the accepted I4D five groups. These are baseline
facts only. They do not prove an I5 contract or implementation.

## 4. Ownership and module boundary

The design uses the following modules and seams:

    EDOPro / ocgcore
        legality and original response authority

    GameplayMessageDecoder / future I5 parser
        one complete inner GAME_MSG and private wire draft

    PerspectiveStateMirrorV1
        private resolution evidence only

    PublicStateProjectionResultV1
        sole persistent public locator/accepted-snapshot CardCode authority

    future I5 continuation module
        public current-domain projection and private final-response binding

    later network/model layers
        CTOS_RESPONSE transport, OCGForge public_action_key, and model input

The proposed I5 module is a deep module: its public interface is a complete
current semantic domain plus a stale-safe local selection handle; its private
implementation owns source occurrences, feasibility state, and response
serialization. No public API exposes protocol offsets, raw bytes, or the
private response binding.

## 5. Family audit and classification

| Family | Core producer / validator | EDOPro reader / response producer | Classification | Status |
| --- | --- | --- | --- | --- |
| SELECT_CARD | `playerop.cpp#field::process(SelectCard&)`, `parse_response_cards` | `gframe/duelclient.cpp#ClientAnalyze`, `event_handler.cpp#SetResponseSelectedCards` | CONTINUATION_REQUIRED | FROZEN |
| SELECT_TRIBUTE | `playerop.cpp#field::process(SelectTributeP&)`, `parse_response_cards` | `ClientAnalyze`, `SetResponseSelectedCards` | CONTINUATION_REQUIRED | FROZEN |
| SELECT_SUM | `playerop.cpp#field::process(SelectSum&)`, `select_sum_check1` | `ClientAnalyze`, sum selection UI | FAIL_CLOSED_UNSUPPORTED_V1 | UNSUPPORTED |
| SELECT_PLACE | `playerop.cpp#field::process(SelectPlace&)` | `ClientAnalyze`, field response construction | CONTINUATION_REQUIRED | FROZEN |
| SELECT_DISFIELD | `SelectPlace&` with `disable_field` | `ClientAnalyze` same layout | CONTINUATION_REQUIRED | FROZEN |
| SELECT_COUNTER | `playerop.cpp#field::process(SelectCounter&)` | `ClientAnalyze`, counter response construction | CONTINUATION_REQUIRED | FROZEN |
| SORT_CARD | `playerop.cpp#field::process(SortCard&)` | `ClientAnalyze`, sorting response construction | CONTINUATION_REQUIRED | FROZEN |
| SORT_CHAIN | `processor.cpp#field::process(SortChain&)` delegates to `SortCard&` | `ClientAnalyze`, same permutation response | CONTINUATION_REQUIRED | FROZEN |
| ANNOUNCE_RACE | `playerop.cpp#field::process(AnnounceRace&)` | `ClientAnalyze` | CONTINUATION_REQUIRED | FROZEN |
| ANNOUNCE_ATTRIB | `playerop.cpp#field::process(AnnounceAttribute&)` | `ClientAnalyze` | CONTINUATION_REQUIRED | FROZEN |
| ANNOUNCE_NUMBER | `playerop.cpp#field::process(AnnounceNumber&)` | `ClientAnalyze` | FLAT_TERMINAL_DOMAIN_SAFE | FROZEN |
| SELECT_UNSELECT_CARD | `playerop.cpp#field::process(SelectUnselectCard&)` | `ClientAnalyze` | FLAT_TERMINAL_DOMAIN_SAFE | FROZEN |

`ANNOUNCE_NUMBER` is naturally flat because the source already supplies a
bounded option vector and the final response is one option index.
`SELECT_UNSELECT_CARD` is also one terminal action per prompt; higher-level
operations may emit another prompt later, but that is not a continuation of
the same inner message.

## 6. Common continuation model

For a continuation-required family:

    complete source GAME_MSG
      -> private parsed source draft
      -> accepted public initial context
      -> complete current public domain
      -> one externally selected local key
      -> atomic adapter-local state transition
      -> complete next public domain
      -> ...
      -> terminal private original response body

The current domain contains every legal next action that leaves at least one
terminal completion. `FINISH` exists only when the current partial selection
is itself terminal legal. `CANCEL` exists only when the source response grammar
explicitly permits it. A valid singleton current domain is still externally
selected. There is no first-candidate, random, teacher, native-AI, or fallback
policy.

The terminal set is defined over one canonical response codec per family.
Byte-level aliases accepted by the core are equivalent encodings, not extra
semantic decisions. Every canonical terminal response is reachable by at
least one legal path, and every terminal path serializes to a canonical source
response.

Occurrence identity is `(family, source_section, source_ordinal)`. Equal code,
locator, amount, description, counter value, bit, or number does not merge
occurrences. Continuation identity includes original prompt instance, current
step, prior choices, current domain, and terminal status. It never includes a
pointer, allocation address, PID, time, thread, socket, path, random UUID, or
unordered iteration result.

The continuation graph uses a canonical path rule, not arbitrary public order:

| Family semantics | Canonical path |
| --- | --- |
| SELECT_CARD, SELECT_TRIBUTE | pick strictly increasing original source occurrence indexes |
| SELECT_SUM | unsupported V1 family; no continuation path or oracle |
| SELECT_PLACE, SELECT_DISFIELD | pick strictly increasing indexes in the explicit place order |
| ANNOUNCE_RACE, ANNOUNCE_ATTRIB | pick strictly increasing admitted bit indexes |
| SELECT_COUNTER | assign amounts in fixed wire source order, including zero |
| SORT_CARD, SORT_CHAIN | choose any remaining source occurrence for the next destination position |
| ANNOUNCE_NUMBER, SELECT_UNSELECT_CARD | no continuation path; one external terminal choice |

The monotonic rule removes permutation duplicates for set-valued responses while
preserving every legal final set. It is not a candidate sort and does not
change the source order delivered by the core. Ordering families retain every
meaningful permutation.

## 7. Card-bearing authority and privacy

The future authority overload follows I4B exactly:

    capture mirror.Snapshot exactly once
    -> reproject the captured snapshot with accepted duel flags
    -> compare canonical bytes, SHA-256, and PublicProjectionId
    -> resolve source occurrences privately against captured mirror
    -> optionally correlate to exactly one accepted public card
    -> copy persistent locator/accepted-snapshot CardCode only from accepted snapshot

Indexed field references use the existing semantic-zone compatibility rules.
Hand/Extra references use a proven card code and a unique accepted public pile
ordinal only when a persistent locator is needed. Main Deck correlation does
not create a persistent locator. An overlay reference in a source layout
without an overlay index fails before the I4B helper.

The current prompt is itself a disclosure to its acting perspective. When the
pinned EDOPro reader receives a nonzero source CardCode and binds it to the
current selectable item, I5 may publish that exact value in a
prompt-local-card-code variant. This applies to the location-bearing and
zero-location SELECT_CARD paths and to the other card-selection/sorting paths
whose reader stores the prompt code. It is candidate-local only: it expires at
the prompt boundary, never mutates the mirror, never proves a locator, and
never creates physical continuity. A zero code is absent. The SELECT_COUNTER
reader explicitly discards its wire code, so that field is not independently
published by the counter contract.

`CARD_CODE_SAFE` remains a separate accepted-snapshot publication predicate.
A zero or mismatching source code does not destroy a proven persistent locator,
and a prompt-local code does not create one. No raw loc_info, mirror entity ID,
private continuity, first-match choice, or raw protocol address becomes public
identity.

`SELECT_PLACE` and `SELECT_DISFIELD` are field-slot choices, not card
locators. `POSITION` remains governed by its validated mask and must not
inherit card-reference requirements from any I5 family.

## 8. SELECT_SUM explicit V1 fail-closed boundary

The wire layout is resolved as:

    id u8 = 23
    player u8
    mode u8
    target u32_le (writer emits acc & 0xffff)
    min u32_le
    max u32_le
    mandatory_count u32_le
    mandatory entries: code u32 + ModernLocInfoV1 + sum_param u32
    optional_count u32_le
    optional entries: code u32 + ModernLocInfoV1 + sum_param u32

Each entry is 18 bytes and the message length is `23 + 18*(mandatory +
optional)`. The source code proves that mandatory occurrences are always
included and the response contains optional occurrence indexes.

The exact future predicates cannot be admitted for this V1 family. The
producer supplies no source-backed range that excludes the problematic
`sum_param` values, the writer transmits only `acc & 0xffff`, and the final
validator is preceded by pointer sorting of selected `card*` values. The
research witnesses show that a wire-only, pointer-independent adapter cannot
reconstruct the unrestricted legal domain.

Therefore the I5 V1 dispatch boundary rejects every `MSG_SELECT_SUM` before
public context, candidate-domain, continuation, or response-binding
construction:

    error=UnsupportedPromptFamily
    public_context=ABSENT
    public_candidates=ABSENT
    private_binding=ABSENT
    prompt_ordinal=UNCHANGED

This is an explicit fail-closed scope decision. It is not a heuristic oracle,
an unsigned reinterpretation, a narrowed implicit subdomain, or a production
fix for the pinned core. A future contract may re-admit SELECT_SUM only under a
new independently accepted source-backed exact-domain proof.

    SELECT_SUM_CONTRACT=FAIL_CLOSED_UNSUPPORTED_V1
    SELECT_SUM_EXACT_SEMANTICS=NOT_APPLICABLE_DUE_FAIL_CLOSED
    SELECT_SUM_EXACT_ORACLE_CONTRACT=NOT_APPLICABLE_DUE_FAIL_CLOSED
    SELECT_SUM_HEURISTIC_ORACLE_ALLOWED=NO

## 9. Family semantics retained for the eleven-family V1 freeze

The companion contract draft contains the complete formulas. The audit
decisions that must survive a later SELECT_SUM remediation are:

* card selection uses occurrence indexes, min/max counts, source cancellation,
  prompt-local disclosed CardCodes where the reader exposes them, and a shared
  exact index-list codec;
* tribute selection uses a minimum required tribute value, a maximum selected
  card count, release values, and a weighted completion predicate, never two
  simple card-count bounds;
* place/disfield uses the four acting-player-relative field-slot groups and
  emits absolute player/location/sequence triples;
* counter selection assigns a nonnegative amount to every source occurrence,
  including zero, with exact total and per-occurrence capacity;
* card and chain sorting place each remaining source occurrence at the next
  destination position and serialize a source-indexed permutation;
* race and attribute announcements select exactly the requested number of
  admitted bits and serialize one mask;
* number announcement preserves every u64 option and returns its source index;
* select/unselect preserves both sections, distinguishes SELECT from
  UNSELECT, and uses the core's combined-index response;
* all source order and duplicate occurrences remain visible in the semantic
  domain, while unsafe card identity remains absent;
* SELECT_SUM is an explicit unsupported-family boundary and is never silently
  routed into an approximate continuation or oracle.

## 10. Response and network barrier

The future I5 module may construct a private final response body for the
original prompt. It may not construct or send a CTOS network envelope. There
are zero intermediate responses. A failed continuation emits no response and
does not retry or fall back.

The final response body remains distinct from:

    public candidate descriptor
    I5 local candidate key
    OCGForge public_action_key
    LogicalModelInputV1
    EncodedModelInputV1

I5 does not own any of the latter values. `I5_LOCAL_KEY_EQUALS_OCGFORGE_PUBLIC_ACTION_KEY=NO`.

## 11. Future test evidence shape

The future implementation must include exact raw-prompt/response vectors and
test all eleven admitted families plus an explicit SELECT_SUM unsupported
boundary. At minimum the vectors must cover:

    minimal and multi-choice prompts
    duplicate-looking source occurrences
    singleton current domains without auto-answer
    cancel and finish where the source permits them
    malformed/truncated/trailing input
    invalid counts, booleans, masks, locations, and ranges
    every intermediate current domain in representative paths
    every canonical terminal response golden
    invalid continuation instance/step/key/membership
    failure atomicity and source-byte ownership
    paired privacy worlds and public reflection boundary
    fresh-process value-level determinism

The retained SELECT_SUM research vectors are negative unsupported-family
evidence, not an oracle contract. Any future re-admission would require a new
independent bounded brute-force/exact-oracle review for zero optional entries,
singleton, duplicates, equal sums, mandatory plus optional, lower/upper bounds,
no/one/many solutions, and a large bounded vector. Those tests cannot
authorize a heuristic oracle.

## 12. Findings

### RESOLVED BLOCKER — SELECT_SUM is intentionally unsupported for V1

The concrete signed/unsigned `sum_param` discrepancy, truncated internal
accumulator, and pointer-ordered final validation prevent a complete exact
wire-to-domain contract. The authorized V1 scope decision is to reject
`MSG_SELECT_SUM` as `UnsupportedPromptFamily` before any public or private
domain is constructed. This closes the blocker without inventing an oracle;
re-admission belongs to a new contract review.

### Three MAJOR findings from the previous head — remediated here

1. Prompt CardCodes were incorrectly treated as private in all cases. The
   contract now distinguishes prompt-local disclosure to the acting perspective
   from persistent I3D locator/CardCode authority. Prompt-local codes are
   ephemeral candidate fields and never mutate mirror state or establish
   continuity.
2. SELECT_TRIBUTE bounds were incorrectly named as two card counts. The
   contract now names the first bound `minimum_required_tribute_value` and the
   second `maximum_selected_card_count`, and its feasibility rule uses
   `release_value` plus the count bound.
3. Unordered continuation paths were not canonicalized. The contract now
   requires monotonic original source indexes for unordered card/tribute,
   place, and mask choices, fixed source traversal for counter amounts, and
   full remaining-choice permutations for sorting.

These three findings are closed by the docs/fixture remediation only. The
SELECT_SUM scope decision is separate and also does not authorize runtime
implementation.

### NOTE — requested repository governance documents are absent

The task requested `docs/NORMATIVE_HIERARCHY.md`,
`docs/CURRENT_PROJECT_STATE.md`, and `docs/ROADMAP.md`. The live repository
has no files at those paths; the audit used the live `README.md`,
`PROJECT_CHARTER.md`, `ARCHITECTURE.md`, `PRIVACY_THREAT_MODEL.md`, contracts,
and issue #3 as available context. No replacement governance files were
created.

### NOTE — issue #3 historical status is stale

Issue #3 remains roadmap context, not authority. Its historical I4 status and
old main SHA do not override the accepted live base or the current tests.

### NOTE — SELECT_UNSELECT has one wire terminal value for two source flags

When both `finishable` and `cancelable` are true, the core accepts only one
`-1` body. The future public surface uses one closed `FINISH_OR_CANCEL`
variant instead of fabricating two identical response candidates.

### NOTE — low-level counter validator accepts signed negative bytes

The future adapter must reject negative amounts before binding because the
source response is a nonnegative allocation even though the low-level read is
`int16_t`.

## 13. I4D/I5 acceptance boundary

The intended successful matrix is frozen here as a requirement, not as a
claim of success:

    I4_FINAL=YES
    I5A0_TARGET_FAMILY_COUNT=12
    I5A0_SUPPORTED_CONTRACT_FAMILY_COUNT=11
    SELECT_SUM_SUPPORT=FAIL_CLOSED_UNSUPPORTED_V1
    ANNOUNCE_CARD_SUPPORT=FAIL_CLOSED_UNSUPPORTED
    I5_MESSAGE_IDS_FROZEN=PASS
    I5_MODERN_WIRE_GRAMMARS_FROZEN=PASS
    I5_RESPONSE_CODECS_FROZEN=PASS_FOR_11_ADMITTED_FAMILIES
    I5_CONTINUATION_MODEL_FROZEN=PASS
    I5_CURRENT_DOMAIN_COMPLETENESS=PASS_FOR_11_ADMITTED_FAMILIES
    I5_TERMINAL_COMPLETION_REACHABILITY=PASS_FOR_11_ADMITTED_FAMILIES
    I5_DUPLICATE_OCCURRENCE_PRESERVATION=PASS
    I5_FINISH_SEMANTICS_FROZEN=PASS
    I5_CANCEL_SEMANTICS_FROZEN=PASS
    I5_N1_AUTOANSWER=NO
    I5_INTERMEDIATE_PROTOCOL_RESPONSE=ABSENT
    I5_NETWORK_RESPONSE_SENDING=ABSENT
    I5_PUBLIC_PRIVATE_SEAM=PASS
    I5_PUBLICATION_AUTHORITY=I3D
    I5_PROMPT_LOCAL_CARD_DISCLOSURE=ACTING_PERSPECTIVE_CURRENT_PROMPT
    I5_PROMPT_LOCAL_CARD_CODE_PERSISTS=NO
    I5_PRIVATE_RESPONSE_IS_MODEL_INPUT=NO
    I5_CONTINUATION_STATE_DETERMINISTIC=PASS
    I5_UNORDERED_CANONICAL_PATHS=MONOTONIC_SOURCE_INDEXES
    I5_SELECT_TRIBUTE_BOUND_SEMANTICS=MIN_VALUE_PLUS_MAX_COUNT
    I5_LOCAL_KEY_EQUALS_OCGFORGE_PUBLIC_ACTION_KEY=NO
    SELECT_SUM_EXACT_SEMANTICS=NOT_APPLICABLE_DUE_FAIL_CLOSED
    SELECT_SUM_EXACT_ORACLE_CONTRACT=NOT_APPLICABLE_DUE_FAIL_CLOSED
    SELECT_SUM_HEURISTIC_ORACLE_ALLOWED=NO
    I6_AUTHORITY_ACQUIRED=NO
    MODEL_INPUT_AUTHORITY_ACQUIRED=NO
    NETWORK_SEND_AUTHORITY_ACQUIRED=NO

This I5A0 slice passed independent review of the eleven-family freeze. The
accepted final status is:

    BLOCKERS=0
    MAJORS=0
    MINORS=0
    NOTES=4
    CONTRACT_FROZEN_FAMILY_COUNT=11
    I5A0_CONTRACT_FREEZE=YES_FOR_11_FAMILIES
    I5A0_CONTRACT_FREEZE_FINAL_PASS=YES
    I5_IMPLEMENTATION_AUTHORIZED=NO
    I5_IMPLEMENTED=NO
    I5_FINAL=NO
    I6_AUTHORIZED=NO
