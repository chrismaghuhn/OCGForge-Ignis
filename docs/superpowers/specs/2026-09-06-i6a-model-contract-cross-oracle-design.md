# OCGForge-Ignis I6A — Model-Contract Bundle and Cross-Oracle Design

Status: DESIGN / CONTRACT-FREEZE CANDIDATE. This document does not authorize
I6 runtime implementation or I7.

Audit date: 2026-09-06

```text
IGNIS_MAIN=e54f392d3688a28f2892c02998854349b2007a91
OCGFORGE_MAIN=3edfcabf51dd914f96adc4df903b1ac2a9d20e5f
```

## 1. Scope and decision boundary

I6A freezes the architecture and consumer binding for the OCGForge model
contracts. It does not add model-input code, a runner, a checkpoint, a
cross-oracle executable, a vocabulary artifact, a dataset, or a new gameplay
authority.

```text
I6A = source audit + bundle registry + oracle design + future plan
I6B+ = separately authorized runtime/evidence slices
I7  = separately authorized model-runner and checkpoint boundary
```

OCGForge remains the sole owner of the model/environment semantics. Ignis
remains the external EDOPro adapter and consumer of accepted public values.

## 2. Live audit result

The Ignis `main` anchor is live at `e54f392d…`; PR #28 is merged and its
post-merge CI run `34026325229` succeeded on that merge commit. The accepted
I5 boundary remains:

```text
SELECT_SUM   = FAIL_CLOSED_UNSUPPORTED_V1
ANNOUNCE_CARD = FAIL_CLOSED_UNSUPPORTED
```

The OCGForge `origin/main` anchor is live at `3edfcabf…`. Its checked-out
branch was `chris/phase6-task7-run-a-failure-localization` at `a32d336…`, two
commits ahead of `origin/main`; those diagnostic commits were excluded from
this audit. OCGForge was not modified.

The Ignis repository does not contain `docs/NORMATIVE_HIERARCHY.md` or
`docs/CURRENT_PROJECT_STATE.md`; the authoritative versions inspected for
OCGForge model questions are in OCGForge itself. This is an informational
repository-topology note, not a request to copy those documents into Ignis.

## 3. Findings and readiness classification

### BLOCKER 1 — Ignis has no complete OCGForge public-state/event source

OCGForge P5 requires the complete perspective-safe observation state: globals,
zones, entities, relationships, chain state, visible events, match context,
public locator tokens, and public decision context. Ignis I3D currently
exposes a public participant/card snapshot with turn, phase, terminal, and
locator values, but it does not expose one accepted value containing the full
OCGForge field set. In particular, the current Ignis mirror does not retain an
accepted `PublicSafeVisibleEvent` history or the OCGForge match-context deck
vectors.

No I6 bridge may reconstruct these values from raw protocol bytes, private
Mirror state, current slot continuity, pointer identity, or timing. Until an
accepted source-closure decision supplies the missing public fields:

```text
I6_PUBLIC_STATE_ORACLE=BLOCKED
I6_EVENT_ORACLE=BLOCKED
I6_LOGICAL_INPUT_ORACLE=BLOCKED_FOR_FULL_STATE
```

Owning correction layer: an explicitly authorized Ignis I3/public-state
extension or an accepted OCGForge-compatible public-frame source. It is not
implemented by I6A.

### BLOCKER 2 — prompt-local CardCode has no current OCGForge candidate field

Ignis I4/I5 candidate variants legitimately carry a prompt-local CardCode in
some current prompts, including zero-location/code-only and no-persistent-
locator cases. OCGForge `EnvironmentActionCandidate`, `LogicalCandidate`, and
`EncodedCandidate` expose references, choices, source index, amount, and other
public fields, but no candidate-local CardCode field.

The bridge cannot safely solve this by putting the value into a locator,
`PublicChoice`, `source_index`, `public_action_key`, CardVocabulary, or public
state. Each would change an accepted meaning or create an unauthorized
identity path. Dropping the field would not be value-preserving.

```text
I6_PROMPT_LOCAL_CARDCODE_MAPPING=BLOCKED
I6_CANDIDATE_VALUE_PRESERVATION=BLOCKED_FOR_AFFECTED_VARIANTS
```

Owning correction layer: OCGForge public model contract, or a separately
accepted decision that the prompt-local field is outside model semantics. I6A
does neither.

### MAJOR 1 — rules/runtime compatibility is not proven

The OCGForge rules bundle records core commit `9a0c558c…`, resolved core hash
`161849049…`, CardScripts commit `f337c870…`, BabelCDB commit `89ad6837…`,
format `TCG_ADVANCED_2026_05_18`, `DUEL_MODE_MR5`, and duel flags `190464`.
Ignis targets EDOPro commit `30935e847…`, its ocgcore gitlink
`46779fbe…`, PRO_VERSION `0x1354`, API 11, and an externally configured deck /
resource set. The exact CardScripts, database, patchset, format/flag, and
locked-deck equivalence is not established.

This does not prevent comparing explicitly supplied equal public vectors. It
does prevent a claim that an Ignis runtime and an OCGForge checkpoint share the
same rules-domain trajectory or checkpoint compatibility.

```text
I6_RULES_DOMAIN_COMPATIBILITY=DIFFERENT_OR_UNPROVEN
I6_CHECKPOINT_COMPATIBILITY=NOT_AN_I6_FINAL_GATE
I7_CHECKPOINT_COMPATIBILITY=UNRESOLVED
```

Owning correction layer: a later explicit runtime-bundle compatibility audit;
no pin changes are permitted in I6A.

### Task7 materialization status — separate conditional dependency

The OCGForge Task7 non-smoke materialization contract is accepted and the
materialization implementation/tests are present on live `main` through the
Task7 materialization commits. The configuration KAT is:

```text
ocgforge.phase6.task7.input_materialization.v1
phase6_task7_input_materialization_config.v1.20f394c888e959446fa263c3520f3dd3b1f48b3a23e58373da7153a691ab1e7a
```

However, the live Task7 records still state `TASK7_READINESS=BLOCKED`,
`TASK7_AUTHORIZED=NO`, and no accepted dataset authority / meaningful
checkpoint exists. The Task7 materializer is therefore a conditional physical
oracle stage, not evidence that I6 can bind a checkpoint or trusted dataset
today. This does not block a materialization-only I6F vector once the accepted
Task7 semantics, implementation/source, configuration identity, and native
oracle vectors are available.

Its source association is also physical/materialization-only: it does not
contain a `ModelSupervisionSampleV1`, selected public key, candidate ordinal,
dataset identity, or split identity. When labels are in scope, that admitted
supervision value must be validated alongside the materialized sample as a
separate source association. A materialized row or sample digest cannot create
dataset membership or a training label.

```text
I6_TASK7_CONTRACT=ACCEPTED
I6_TASK7_SOURCE_IMPLEMENTATION=PRESENT_ON_MAIN
I6_TASK7_FINAL_ACCEPTANCE=NOT_PROVEN
I6_TASK7_DATASET_AUTHORITY=PROPOSED_NOT_READY
I6F_DATASET_CHECKPOINT_DEPENDENCY=NO
```

## 4. Authority model

The authority direction is fixed:

```text
EDOPro legal prompt / accepted Ignis I3-I5 values
        ↓
OCGForge public-environment descriptor
        ↓
OCGForge ygo::model semantic input
        ↓
OCGForge encoded/layout/materialization views
        ↓
later scorer/runner
```

| Concern | Authority | I6 role |
| --- | --- | --- |
| Game legality and prompt domain | EDOPro and its pinned runtime | Consume accepted Ignis public domain; never reopen legality |
| Ignis persistent public locator proof | Ignis I3D accepted snapshot | Supply only already-proven public locator values |
| Public observation/safe state/event meaning | OCGForge public contracts | Reconstruct/validate exact source values; fail on missing fields |
| `public_action_key` | OCGForge public action identity codec | Construct from the OCGForge public descriptor, never alias I4/I5 local keys |
| Logical/encoded model input | OCGForge P5 `ygo::model` | Reproduce/compare exact canonical values |
| CardVocabulary | explicit OCGForge immutable artifact | Consume and validate exact list/identity |
| Batch/materialization layout | OCGForge physical contracts | Validate lossless derivation only |
| Private response binding | Ignis/EDOPro adapter | Never crosses the model boundary |
| Selection/scoring/checkpoint | later environment/model layers | Not owned by I6 |

## 5. Exact contract registry

The static registry is specified in
`docs/contracts/ocgforge-model-contract-bundle-v1.md`. It contains
`I6_BUNDLE_ENTRIES=15` ordered entries across mixed authority classes. The
registry's consumer-manifest identity is an Ignis binding identity and is never
substituted for an OCGForge semantic identity.

The OCGForge-owned registry is:

```text
ocgforge.public_environment_observation.v1
ocgforge.public_safe_state.v1
ocgforge.public_action_identity.v1
ocgforge.public_candidate_domain.v1
ocgforge.public_semantic_decision_identity.v1
ocgforge.episodic_environment.v2
ocgforge.environment_identity.v2
ocgforge.model_logical_input.v1
ocgforge.model_encoded_input.v1
ocgforge.model_card_vocabulary.v1
ocgforge.model_input_identity.v1
ocgforge.model_batch_layout.v1
ocgforge.model_supervision_sample.v1
ocgforge.phase6.task7.input_materialization.v1
ocgforge.phase6.task7.input_materialization_config.v1
```

The following are retained compatibility dependencies, not model feature
values:

```text
ocgforge.decision_protocol.v1
ygo.player_observation.v1
ocgforge.action_identity.v1
ocgforge.candidate_domain.v1
ocgforge.semantic_decision_identity.v1
ocgforge.episode_identity.v1
ygo.engine_trace.v2
ocgforge.seed_derivation.v1
ocgforge.script_resolution.v1
ocgforge.required_script_closure.v1
```

`ocgforge.dataset_manifest.v1`,
`ocgforge.phase6.dataset_split.v1`, Task7 collection schedule/job identities,
and checkpoint identities are not part of the online I6 model-contract bundle.
They require separately accepted Task7 dataset/checkpoint authority.

## 6. Cross-oracle comparison ladder

The comparison ladder uses native OCGForge output as the independent oracle
and an Ignis reconstruction as the consumer under test. A copied Ignis output
is never used as its own expected value.

| Stage | Meaning and source | Canonical comparison | Allowed differences | Failure / privacy rule |
| --- | --- | --- | --- | --- |
| B0 | bundle preflight | exact registry, source snapshot, contract IDs, config identity | none in required fields | reject before frame construction; no floating version |
| O0 | public observation | exact `canonical_public_environment_observation_bytes` | none | public-only source; reject invalid/caller-supplied state |
| O1 | safe state and locator table | exact safe-state bytes, exact token vector, exact frame-local ordinals | physical object IDs and allocation excluded | current ordinals only on exact current proof; historical refs never rebind |
| O2 | visible event stream | exact event count/order/fields and historical references | no engine-step/pointer/path metadata | missing event source is currently `BLOCKED`; never synthesize |
| O3 | complete candidate domain | N, runtime semantic fields, order, multiplicity, public descriptor pairing | no private response/local key | no drop, sort, dedup, truncation, top-K, or N=1 auto-answer |
| O4 | public-action routing | exact OCGForge descriptor bytes and full `public_action.v1.` key | Ignis local key differs | duplicate/unrepresentable mapping rejects whole frame |
| O5 | logical model input | exact `canonical_logical_model_input_bytes` and field presence | none in public semantics | no `PlayerObservation`, internal key, response, or continuation view |
| O6 | encoded model input | exact encoded bytes, codes, limbs, optional masks, routing sidecar | none in encoded meaning | vocabulary/key/count mismatch rejects all rows |
| O7 | CardVocabulary | exact canonical vocabulary bytes and identity | no catalog order/path/runtime differences | missing public passcode or identity mismatch fails closed |
| O8 | model-input identity | exact `canonical_model_input_identity_bytes` and `model_input.v1.` value | batch/layout differences excluded | logical/encoded/vocabulary detachment rejects |
| O9 | Task7 non-smoke materialization | exact configuration identity and canonical unpadded sample bytes/tables | tensor device, padding width, batch composition excluded | conditional until Task7 acceptance; no Task4 downgrade |
| O10 | physical batch layout | ragged/padded/unpadded real-row equality, masks, offsets, N/order | padding representation and backend/device | W<N, offset, mask, or roundtrip error rejects; layout never changes O8 |

Semantic equality, canonical-byte equality, identity equality, provenance
equality, and physical execution equality are different predicates. The oracle
must label which predicate it is checking; it must not require PID, path, wall
clock, process count, TCP segmentation, allocation order, device, batch
composition, or framework metadata to match semantic inputs.

## 7. Candidate and key mapping

The bridge's input is one accepted Ignis public candidate vector. It creates
one OCGForge `EnvironmentActionCandidate` per source occurrence and then
recomputes the OCGForge public key from the OCGForge descriptor. The mapping
is not an alias:

```text
Ignis FlatPrompt candidate
        ↓ field-by-field public mapping
OCGForge EnvironmentActionCandidate
        ↓ OCGForge canonical_public_action_key_bytes
OCGForge public_action_key
```

The OCGForge key descriptor contains:

```text
action_kind
optional typed choice
optional source/target public reference
optional phase / position / source_index / signed amount
continuation_operation
```

It does not contain `submits_engine_response`, private response bytes, Ignis
local key, source section, private continuation instance, or prompt-local
CardCode. The complete OCGForge candidate vector must nevertheless preserve
all public candidate fields; two candidates that become indistinguishable
under the OCGForge key descriptor are a whole-frame collision and fail closed.

Representative mappings are:

| Ignis value | OCGForge value | Rule |
| --- | --- | --- |
| scalar YESNO/EFFECTYN | `PublicChoice` YesNo/EffectYesNo | value 0/1 only; no response bytes |
| OPTION/ANNOUNCE_NUMBER | typed choice plus response selector | selector is source response selector, never candidate-vector index |
| CHAIN/BATTLE/IDLE | family action kind plus public fields/reference | exact source vector proof required |
| CARD/TRIBUTE/UNSELECT | card-selection/pick/finish/cancel fields | continuation state is not independent model input |
| PLACE/DISFIELD | place action and public semantic fields | exact mapping vector must prove any family distinction |
| RACE/ATTRIB | announcement/pick plus public bit/source-index representation | no raw mask or private index; exact OCGForge meaning must be proven |
| COUNTER | assign amount, source index, signed public amount | amount is public semantic value, private binding absent |
| SORT_CARD/SORT_CHAIN | pick/cancel and source index/reference | source order and multiplicity preserved |
| prompt-local CardCode | no current target field | `BLOCKED`, never dropped or repurposed |

The public key remains a routing sidecar. It is not a learned string feature,
candidate ordinal, vocabulary ID, physical locator, or model input feature.
The bridge must not construct an OCGForge `EnvironmentContinuationView` from
Ignis private continuation state. Only public candidate fields already admitted
by OCGForge P5 may cross.

## 8. Locator semantics

The mapping is frame-local and proof-based:

```text
Ignis accepted public locator
        ↓ native oracle vector proves exact token semantics
OCGForge PublicCardReference(kind, observation_locator)
        ↓ unsigned UTF-8 token table
public_locator_ordinal
        ↓ exact current public entity match, if any
optional current_entity_ordinal
```

Equal token strings may share a frame-local ordinal. That ordinal is only a
token-equality feature. It is not a physical card ID, persistent identity,
replay identity, or hidden continuity.

Current candidate/chain/relationship references may receive a current entity
ordinal only when exactly one current public entity matches. Historical visible
event references receive no current entity ordinal even when the token text is
reused by a later card. Ignis `MirrorEntityIdV1`, raw loc_info, source
sequence, collection order, and prompt-local CardCode cannot decide an
ambiguous mapping.

## 9. Event semantics and current source closure

OCGForge P5 serializes visible events in canonical event-index order with
public event kind, optional acting player, historical entity/target locators,
optional public passcode vocabulary ID, zones, amount, counter, phase, winner,
and effect description. Historical references are locator-only.

Ignis currently has no accepted equivalent event-history collection in its I3D
public projection. Its mirror reducer has current cards, participants,
turn/phase/chain/relations, but no OCGForge-compatible visible-event log.
Therefore:

```text
EVENTS_FROM_CURRENT_SNAPSHOT=NO
EVENTS_FROM_PRIVATE_MIRROR=NO
EVENTS_FROM_RAW_PROTOCOL_RECONSTRUCTION=NO
I6_EVENT_ORACLE=BLOCKED
```

The future source-closure slice must either add an independently accepted
public event source or define a narrower model contract with OCGForge review.
No missing event is represented by an empty vector or fabricated default.

## 10. Card vocabulary and identity

The exact CardVocabulary source is an immutable explicit ascending public
passcode list. OCGForge owns its bytes and identity:

```text
0 = PAD
1 = PUBLIC_UNKNOWN_OR_REDACTED
2 + ascending rank = known public passcode
```

The bundle freezes the schema and mapping rule only. A future oracle vector
must carry the exact vocabulary manifest and verify:

```text
canonical_card_vocabulary_bytes
→ model_card_vocabulary.v1.<lowercase SHA-256>
```

There is no accepted global vocabulary artifact on live OCGForge main. The
Task7 dataset-authority contract proposes deriving one from accepted public
values, but that authority is not ready. Ignis must never scan BabelCDB,
CardScripts, a deck database, or hidden state to fill a vocabulary.

## 11. Task4, Task7, and physical execution

The Task4 numeric projection is intentionally lossy smoke infrastructure. Its
fixed float rows and provisional backend/checkpoint identities are not valid
I6 semantic inputs and cannot be used to close a Task7 or checkpoint oracle.

Task7 non-smoke materialization consumes validated Phase-5 logical/encoded
inputs and a validated ragged batch. Its selected representation uses typed
tables, exact base-2^16 limbs, explicit optional presence, row masks, and
control sidecars. `globals.chain_length` and `chain_state.length` remain
separate fields. The materializer must reject any mismatch without dropping
rows or downgrading to Task4.

PyTorch `torch.int64`/`torch.bool`, device, CUDA, batch width, padding, worker
count, and allocation are physical execution values. They are not semantic
authority and do not enter `model_input_identity` or the canonical unpadded
Task7 sample identity.

## 12. Paired-world privacy oracle

Every available public/model stage must be run on two worlds with:

```text
same public perspective-safe observation
same complete public candidate semantics
different hidden opponent hand/deck identity or hidden physical history
```

Required equal results are:

```text
public observation bytes/digest
public locator tokens and frame-local ordinals
visible public event values
candidate count/order/public fields
public_action_key vector and candidate-domain digest
logical bytes
encoded bytes and vocabulary identity
model_input_identity
Task7 canonical bytes/tables/masks when available
```

Required unequal values may exist only in private source context that is not
fed to the bridge. No private Mirror ID, raw protocol address, hidden
passcode, prompt-local physical continuity, or private response may appear in
the public/model values or their identities.

## 13. Candidate N-to-N and continuation rules

For every accepted current Ignis domain:

```text
N Ignis public candidates
→ N OCGForge public candidates
→ N logical candidate records
→ N encoded candidate rows
→ N routing keys
→ N Task7 real candidate rows when O9 is available
→ N score slots in a later scorer
```

Source order and multiplicity are preserved at every arrow. I5 continuation
state stays adapter-local: the engine remains paused, intermediate actions
produce no final response, and the model sees only the complete current
candidate vector. A terminal response remains private adapter authority.

## 14. Runtime, replay, and I7 barriers

I6 may own:

```text
contract bundle validation
public frame/model projection
canonical byte comparison
public candidate/key routing validation
compatibility diagnostics
paired-world and determinism evidence
```

I6 may not own:

```text
CTOS_RESPONSE sending
OCGForge engine advancement
checkpoint loading or compatibility handshake
model runner process/IPC
PyTorch inference or scoring
training, dataset membership, split issuance, or policy selection
fallback policy or candidate pruning
```

I7 begins only after I6 final acceptance and separately authorized runner/
checkpoint work. A checkpoint identity cannot be issued from a model-input
identity, Task4 smoke artifact, Task7 configuration identity, or runtime path.

## 15. Exact future I6 acceptance matrix

No row is PASS evidence for the current documentation task. These are the
future machine-readable gates:

```text
I6_BUNDLE_REGISTRY_EXACT=PASS
I6_BUNDLE_SOURCE_SNAPSHOT_PINNED=PASS
I6_NO_FLOATING_CONTRACT_REFERENCE=PASS

I6_PUBLIC_OBSERVATION_BYTES=PASS
I6_PUBLIC_SAFE_STATE_BYTES=PASS
I6_LOCATOR_TOKEN_ORACLE=PASS
I6_VISIBLE_EVENT_ORACLE=PASS
I6_PUBLIC_ACTION_KEY_ORACLE=PASS
I6_CANDIDATE_DOMAIN_N_TO_N=PASS
I6_CANDIDATE_ORDER_PRESERVED=PASS
I6_DUPLICATE_OCCURRENCES_PRESERVED=PASS
I6_LOGICAL_INPUT_BYTES=PASS
I6_ENCODED_INPUT_BYTES=PASS
I6_CARD_VOCABULARY_IDENTITY=PASS
I6_MODEL_INPUT_IDENTITY=PASS
I6_TASK7_CANONICAL_MATERIALIZATION=PASS_OR_NOT_APPLICABLE
I6_BATCH_ROUNDTRIP=PASS_OR_NOT_APPLICABLE

I6_PUBLIC_ACTION_KEY_NOT_IGNIS_LOCAL_ALIAS=PASS
I6_PRIVATE_RESPONSE_NOT_MODEL_INPUT=PASS
I6_NO_PROMPT_LOCAL_IDENTITY_PROMOTION=PASS
I6_PUBLICATION_AUTHORITY_I3D_PRESERVED=PASS
I6_NO_HIDDEN_CONTINUITY=PASS
I6_PAIRED_WORLD_EQUALITY=PASS
I6_FRESH_PROCESS_DETERMINISM=PASS
I6_MALFORMED_MISMATCH_FAIL_CLOSED=PASS
I6_UNSUPPORTED_FAMILY_FAIL_CLOSED=PASS

I6_RULES_RUNTIME_COMPATIBILITY=PASS
I6_CHECKPOINT_COMPATIBILITY=NOT_AN_I6_FINAL_GATE
I6_I7_CHECKPOINT_PREREQUISITES=DOCUMENTED_ONLY
I6_NETWORK_RESPONSE_SENDING=ABSENT
I6_MODEL_SCORING=ABSENT
I6_CHECKPOINT_LOADING=ABSENT
I6_I7_AUTHORITY=ABSENT
```

`PASS_OR_NOT_APPLICABLE` for Task7 means only that the selected I6 scope
explicitly excludes the conditional physical stage; it cannot be used to
claim Task7 materialization success. Dataset membership, split issuance, a
real checkpoint, checkpoint loading, and runner freshness are not I6F
prerequisites and are owned by Task7/I7. Any required stage with missing
source proof is `BLOCKED`, not `PASS`.

I6 may document the input and rules prerequisites that a later I7 checkpoint
must satisfy. I6 does not wait for, create, load, or handshake with that
checkpoint. The I7 checkpoint artifact, loader, binary binding, runner
handshake, and freshness gates remain outside I6 final acceptance.

## 16. Evidence that is insufficient

None of the following establishes I6 final acceptance by itself:

```text
green Ignis compilation
P5 documentation status without the native/independent oracle comparison
Task4 smoke output or a PyTorch checkpoint
one public frame or one candidate family
same shape tensors with different semantic values
same final score vector with a different candidate order
matching counts without key/field pairing
one-process determinism
filesystem artifact presence
Task7 source code presence without Task7 acceptance evidence
matching runtime labels or core API number alone
PR mergeability or hosted CI without I6-specific oracle evidence
```

## 17. Current I6A status

```text
I6A_LIVE_SOURCE_AUDIT=PASS
I6_BUNDLE_DESIGN=PASS
I6_TASK4_TASK7_SEPARATION=PASS
I6_AUTHORITY_MODEL=PASS
I6_ORACLE_LADDER=DESIGNED

I6_PUBLIC_STATE_ORACLE=BLOCKED
I6_EVENT_ORACLE=BLOCKED
I6_PROMPT_LOCAL_CARDCODE_MAPPING=BLOCKED
I6_RULES_DOMAIN_COMPATIBILITY=DIFFERENT_OR_UNPROVEN
I6_CHECKPOINT_COMPATIBILITY=NOT_AN_I6_FINAL_GATE
I7_CHECKPOINT_COMPATIBILITY=UNRESOLVED
I6_TASK7_FINAL_ACCEPTANCE=NOT_PROVEN

I6_RUNTIME_IMPLEMENTATION_AUTHORIZED=NO
I7_AUTHORIZED=NO
I8_AUTHORIZED=NO
```
