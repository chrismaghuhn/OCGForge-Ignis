# OCGForge-Ignis I6A — OCGForge Model-Contract Bundle V1

Status: DESIGN / CONTRACT-FREEZE CANDIDATE. I6 runtime implementation is not
authorized by this document.

Audit date: 2026-09-06

Audited OCGForge source snapshot:

```text
repository = https://github.com/chrismaghuhn/OCGForge
main       = 3edfcabf51dd914f96adc4df903b1ac2a9d20e5f
```

Audited OCGForge-Ignis source snapshot:

```text
repository = https://github.com/chrismaghuhn/OCGForge-Ignis
main       = e54f392d3688a28f2892c02998854349b2007a91
```

The OCGForge checkout used during the audit was on
`chris/phase6-task7-run-a-failure-localization`, two commits ahead of its
remote `main`. Those two diagnostic commits were not used as authority. Every
OCGForge claim below is resolved against `origin/main` at the exact commit
above.

## 1. Purpose and ownership

OCGForge owns the model/environment semantic contracts and their canonical
encodings. OCGForge-Ignis is a consumer. This document defines only the
consumer-side declaration needed to bind Ignis to those already-owned values.
It does not redefine an OCGForge field, assign a new meaning to an OCGForge
identity, or create a second legality, observation, action, vocabulary, or
model authority.

```text
EDOPro / pinned runtime
        ↓
OCGForge-Ignis I3/I4/I5 public semantic values
        ↓
OCGForge public-environment contracts
        ↓
OCGForge ygo::model contracts
        ↓
optional Task7 physical materialization
        ↓
later I7 model runner
```

I6 owns the bridge, validation, and comparison boundary only. It does not own
network response sending, model policy, checkpoint loading, inference,
training, or candidate selection.

## 2. Audited authority inventory

The following sources were inspected on the OCGForge `main` snapshot. The
classification is about authority, not whether an implementation currently
has final milestone acceptance.

| Surface | Source | Classification | I6 consequence |
| --- | --- | --- | --- |
| Public environment observation | `docs/contracts/public-environment-observation-v1.md`; `include/ygo/environment/public_environment_observation.hpp`; `src/environment/public_environment_observation.cpp` | AUTHORITATIVE_SEMANTIC + AUTHORITATIVE_ENCODING | Sole source for policy-facing observation bytes and digest |
| Public safe state | `docs/contracts/public-environment-observation-v1.md`; `include/ygo/environment/public_safe_state.hpp`; `src/environment/public_safe_state.cpp` | AUTHORITATIVE_SEMANTIC + AUTHORITATIVE_ENCODING | Decode through the existing public-safe decoder; never parse private observation data |
| Public action identity | `docs/contracts/public-action-identity-v1.md`; `include/ygo/environment/public_action_identity.hpp`; `src/environment/public_action_identity.cpp` | AUTHORITATIVE_IDENTITY + AUTHORITATIVE_ENCODING | Owns `public_action_key`; Ignis local keys are not aliases |
| Public candidate domain | same action-identity sources | AUTHORITATIVE_IDENTITY + AUTHORITATIVE_ENCODING | Owns ordered public-key digest |
| Public semantic decision identity | same action-identity sources | AUTHORITATIVE_IDENTITY + AUTHORITATIVE_ENCODING | Binds public observation/domain identity, not private prompt data |
| Episodic V2 environment | `docs/contracts/episodic-environment-v2.md`; `include/ygo/environment/episodic_environment.hpp`; `src/environment/episodic_environment.cpp` | AUTHORITATIVE_SEMANTIC | Owns public frame lifecycle and advancement boundary |
| Environment semantic identity | `include/ygo/environment/episodic_environment.hpp`; `src/environment/episodic_environment.cpp` | AUTHORITATIVE_IDENTITY | Compatibility identity includes OCGForge rules/runtime inputs |
| Logical model input | `docs/p5/P5_MODEL_CONTRACT.md`; `include/ygo/model/logical_model_input.hpp`; `src/model/logical_model_input.cpp` | AUTHORITATIVE_SEMANTIC + AUTHORITATIVE_ENCODING | Owns structured public model meaning and exact candidate pairing |
| Encoded model input | `docs/p5/P5_MODEL_CONTRACT.md`; `include/ygo/model/encoded_model_input.hpp`; `src/model/encoded_model_input.cpp` | AUTHORITATIVE_ENCODING | Owns fixed codes, limbs, optional presence, and routing sidecar |
| Card vocabulary | `docs/p5/P5_MODEL_CONTRACT.md`; `include/ygo/model/card_vocabulary.hpp`; `src/model/card_vocabulary.cpp` | AUTHORITATIVE_SEMANTIC + AUTHORITATIVE_IDENTITY | OCGForge owns the immutable passcode list and vocabulary identity |
| Model-input identity | `docs/p5/P5_MODEL_CONTRACT.md`; `include/ygo/model/encoded_model_input.hpp`; `src/model/encoded_model_input.cpp` | AUTHORITATIVE_IDENTITY + AUTHORITATIVE_ENCODING | Owns identity over logical bytes, encoded bytes, and vocabulary identity |
| Model batch layout | `docs/p5/P5_MODEL_CONTRACT.md`; `include/ygo/model/model_batch_layout.hpp`; `src/model/model_batch_layout.cpp` | PHYSICAL_EXECUTION_ONLY | Lossless derived ragged/padded view; excluded from model-input identity |
| Model supervision sample | `docs/p5/P5_MODEL_CONTRACT.md`; `include/ygo/model/model_supervision_sample.hpp`; `src/model/model_supervision_sample.cpp` | DERIVED_EVIDENCE | Optional admitted-data output, not online I6 semantic input |
| P5 acceptance plan/evidence | `docs/p5/P5_ACCEPTANCE_PLAN.md`; `docs/p5/P5_ACCEPTANCE_EVIDENCE.md` | DERIVED_EVIDENCE | The plan defines gates; the evidence records P5 final status and historical execution heads |
| Task7 materialization | `docs/p6/P6_TASK7_NONSMOKE_INPUT_MATERIALIZATION_CONTRACT.md`; `include/ygo/phase6/task7_input_materialization.hpp`; `src/phase6/task7_input_materialization.cpp` | AUTHORITATIVE_ENCODING + PHYSICAL_EXECUTION_ONLY | Conditional downstream stage; never replaces Phase-5 semantic authority |
| Task7 materialization configuration | same Task7 sources | AUTHORITATIVE_IDENTITY | Fixed configuration identity is required when this stage is used |
| Task7 dataset authority | `docs/p6/P6_TASK7_DATASET_AUTHORITY_CONTRACT.md` | DESIGN_ONLY / NOT_READY | Not part of the I6 online bundle; no dataset identity may be inferred |
| Task4 numeric projection | `docs/p6/P6_TASK4A_NUMERIC_AND_PROVENANCE_CONTRACT.md`; `src/phase6/task4_numeric_projection.cpp` | HISTORICAL_SMOKE_ONLY | Explicitly excluded from non-smoke I6 compatibility |
| Task4 backend/checkpoint evidence | `docs/p6/P6_TASK6_PYTORCH_READINESS.md`; Task4B artifacts | HISTORICAL_SMOKE_ONLY + DERIVED_EVIDENCE | Never promoted to Task7 or I6 checkpoint authority |
| P5 acceptance evidence | `docs/p5/P5_ACCEPTANCE_EVIDENCE.md` | DERIVED_EVIDENCE | Records P5 final gates; it does not replace P5 source contracts |

The OCGForge `docs/NORMATIVE_HIERARCHY.md` rule is decisive: accepted ADRs,
versioned contracts, public implementations, and executable evidence have
different authority. Summary documents and historical plans cannot promote an
unverified capability.

## 3. Contract bundle contents

The I6 consumer bundle has exactly two layers:

1. a static contract registry, frozen against an exact OCGForge source
   snapshot; and
2. per-frame/per-run bindings for values whose content is intentionally
   supplied by the current accepted public input, such as a CardVocabulary
   artifact and a `model_input_identity`.

The static registry does not invent a universal card vocabulary, candidate
domain, model input, dataset, or checkpoint.

### 3.1 Static semantic registry

The following fifteen entries are the frozen I6 bundle registry, in this
order. They span mixed authority classes; `I6_BUNDLE_ENTRIES=15` must not be
reported as fifteen same-class authoritative semantic contracts:

```text
01 ocgforge.public_environment_observation.v1
02 ocgforge.public_safe_state.v1
03 ocgforge.public_action_identity.v1
04 ocgforge.public_candidate_domain.v1
05 ocgforge.public_semantic_decision_identity.v1
06 ocgforge.episodic_environment.v2
07 ocgforge.environment_identity.v2
08 ocgforge.model_logical_input.v1
09 ocgforge.model_encoded_input.v1
10 ocgforge.model_card_vocabulary.v1
11 ocgforge.model_input_identity.v1
12 ocgforge.model_batch_layout.v1
13 ocgforge.model_supervision_sample.v1
14 ocgforge.phase6.task7.input_materialization.v1
15 ocgforge.phase6.task7.input_materialization_config.v1
```

Entries 01–07 are public-environment and compatibility authorities. Entries
08–13 are the Phase-5 model-facing authorities. Entries 14–15 are conditional
Task7 physical/configuration authorities. Entry 13 is derived and is required
only for a supervised-data path; it is not required for online scoring.

The following identifiers are compatibility dependencies of the OCGForge
environment identity, but are not independent learner inputs and are not
duplicated as new I6 model contracts:

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

An implementation must validate the exact dependency set required by the
selected OCGForge environment identity. It must not copy private values from
those layers into model input.

### 3.2 Entry-level identity and byte ownership

| Entry | Canonical byte owner | Identity construction | Ignis responsibility |
| --- | --- | --- | --- |
| `public_environment_observation.v1` | `canonical_public_environment_observation_bytes` | lowercase SHA-256 of outer observation bytes | Reproduce/compare exact bytes from an accepted public frame |
| `public_safe_state.v1` | `canonical_public_safe_state_bytes` and strict decoder | nested schema; no independent public digest | Validate canonical round-trip and carry exact safe-state bytes |
| `public_action_identity.v1` | `canonical_public_action_key_bytes` | full `public_action.v1.` plus lowercase hex canonical descriptor bytes | Reconstruct only from safe candidate fields and compare with native output |
| `public_candidate_domain.v1` | `canonical_public_candidate_domain_bytes` | lowercase SHA-256 over request kind and ordered public keys | Preserve exact N/order/key vector |
| `public_semantic_decision_identity.v1` | `canonical_public_semantic_decision_identity_bytes` | lowercase SHA-256 over public episode/frame/domain inputs | Validate only after public observation/domain are accepted |
| `episodic_environment.v2` | public environment contract and frame codec | environment/lifecycle contract ID | Consume frame values; never advance the environment |
| `environment_identity.v2` | `canonical_environment_identity_bytes` | lowercase SHA-256 of the exact environment identity fields | Compare compatibility metadata; never equate it to Ignis runtime identity |
| `model_logical_input.v1` | `canonical_logical_model_input_bytes` | no standalone model-input ID | Reconstruct exact structured values and source order |
| `model_encoded_input.v1` | `canonical_encoded_model_input_bytes` | no standalone model-input ID | Reconstruct exact codes/limbs/presence and sidecars |
| `model_card_vocabulary.v1` | `canonical_card_vocabulary_bytes` | `model_card_vocabulary.v1.` plus lowercase SHA-256 | Require an explicit immutable artifact; never scan a database implicitly |
| `model_input_identity.v1` | `canonical_model_input_identity_bytes` | `model_input.v1.` plus lowercase SHA-256 | Compare exact logical/encoded/vocabulary binding |
| `model_batch_layout.v1` | `make_ragged_model_batch_v1`, pad/unpad APIs | schema only; no semantic layout identity | Validate lossless physical view; exclude layout from semantic identity |
| `model_supervision_sample.v1` | `canonical_model_supervision_sample_bytes` | derived bytes only; no independent authority | Carry only after admitted trajectory/sample validation |
| `phase6.task7.input_materialization.v1` | Task7 canonical sample materializer | schema plus source identities; no replacement semantic ID | Validate/consume only when Task7 acceptance is established |
| `phase6.task7.input_materialization_config.v1` | `canonical_task7_materialization_config_bytes` | `phase6_task7_input_materialization_config.v1.` plus lowercase SHA-256 | Require exact configuration identity `...20f394c888e959446fa263c3520f3dd3b1f48b3a23e58373da7153a691ab1e7a` |

The current OCGForge Task7 implementation exposes the exact configuration KAT
and materialization APIs on `main` through `c05fd79` and `517ddf6`. However,
the accepted Task7 contract still records `TASK7_READINESS=BLOCKED` and the
dataset-authority contract remains `PROPOSED / TASK7_DATASET_AUTHORITY_READY=NO`.
Therefore the source implementation is present, but Task7 is not treated as a
final accepted checkpoint/data path by I6A.

The Task7 materialization source association also does not carry a
`ModelSupervisionSampleV1`, selected public key, candidate ordinal, dataset
identity, or split identity. That is correct for online materialization, but a
future supervised-data path must validate the admitted supervision value as a
separate source association. It must not infer a label from a materialized row
or promote a Task7 sample digest to dataset membership.

### 3.3 Consumer manifest identity

The static consumer manifest has its own explicitly non-OCGForge identity:

```text
I6_BUNDLE_MANIFEST_SCHEMA=ocgforge-ignis.i6.model-contract-bundle.v1
I6_BUNDLE_MANIFEST_IDENTITY=
  ocgforge-ignis.i6.model-contract-bundle.v1.<lowercase SHA-256>
```

This is a binding-manifest identity, not a model input identity, public action
identity, candidate-domain identity, environment identity, dataset identity,
or checkpoint identity. It cannot be used by OCGForge as a substitute for any
of those values.

The future canonical manifest bytes are an explicit ordered, value-owned
encoding:

```text
string(identity_domain)
string(schema_id)
string(ocgforge_source_commit)
string(p5_acceptance_execution_head)
vector<registry_entry>
string(task7_materialization_config_identity)
string(ignis_runtime_contract_id)
string(compatibility_profile_token)
```

The first two fields are not placeholders and are not inferred from one
another:

```text
identity_domain = ocgforge-ignis.i6.model-contract-bundle.v1
schema_id       = ocgforge-ignis.i6.model-contract-bundle.v1
ocgforge_source_commit =
    3edfcabf51dd914f96adc4df903b1ac2a9d20e5f
p5_acceptance_execution_head =
    3c99e86c487361fc4e0f5f12678b4867e59232b7
ignis_runtime_contract_id = ocgforge-ignis.runtime-bundle-identity.v1
compatibility_profile_token =
    ocgforge-ignis.i6.compatibility-profile.v1
```

Each `registry_entry` is:

```text
string(contract_id)
string(owner_repository_id)
u8 authority_class
u8 runtime_required
u8 canonical_bytes_required
u8 identity_required
```

The registry field codes are frozen as follows:

```text
authority_class:
  0 = AUTHORITATIVE_SEMANTIC
  1 = AUTHORITATIVE_ENCODING
  2 = AUTHORITATIVE_IDENTITY
  3 = PHYSICAL_EXECUTION_ONLY
  4 = HISTORICAL_SMOKE_ONLY
  5 = DERIVED_EVIDENCE
  6 = NOT_RELEVANT_TO_I6

owner_repository_id:
  "ocgforge" = the only accepted owner token in this registry

runtime_required, canonical_bytes_required, identity_required:
  0 = false
  1 = true
```

The complete fifteen-entry flag table is:

| Order | Contract ID | Owner token | Authority code | Runtime | Canonical bytes | Concrete identity |
| ---: | --- | --- | ---: | ---: | ---: | ---: |
| 01 | `ocgforge.public_environment_observation.v1` | `ocgforge` | 0 | 1 | 1 | 1 |
| 02 | `ocgforge.public_safe_state.v1` | `ocgforge` | 0 | 1 | 1 | 0 |
| 03 | `ocgforge.public_action_identity.v1` | `ocgforge` | 2 | 1 | 1 | 1 |
| 04 | `ocgforge.public_candidate_domain.v1` | `ocgforge` | 2 | 1 | 1 | 1 |
| 05 | `ocgforge.public_semantic_decision_identity.v1` | `ocgforge` | 2 | 1 | 1 | 1 |
| 06 | `ocgforge.episodic_environment.v2` | `ocgforge` | 0 | 1 | 0 | 1 |
| 07 | `ocgforge.environment_identity.v2` | `ocgforge` | 2 | 1 | 1 | 1 |
| 08 | `ocgforge.model_logical_input.v1` | `ocgforge` | 0 | 1 | 1 | 0 |
| 09 | `ocgforge.model_encoded_input.v1` | `ocgforge` | 1 | 1 | 1 | 0 |
| 10 | `ocgforge.model_card_vocabulary.v1` | `ocgforge` | 2 | 1 | 1 | 1 |
| 11 | `ocgforge.model_input_identity.v1` | `ocgforge` | 2 | 1 | 1 | 1 |
| 12 | `ocgforge.model_batch_layout.v1` | `ocgforge` | 3 | 0 | 1 | 0 |
| 13 | `ocgforge.model_supervision_sample.v1` | `ocgforge` | 5 | 0 | 1 | 0 |
| 14 | `ocgforge.phase6.task7.input_materialization.v1` | `ocgforge` | 3 | 0 | 1 | 0 |
| 15 | `ocgforge.phase6.task7.input_materialization_config.v1` | `ocgforge` | 2 | 0 | 1 | 1 |
```

`runtime_required=0` means conditional for the I6 path, not unsupported. A
concrete identity is required only where the owning contract defines one;
`public_safe_state.v1`, logical input, encoded input, batch layout,
supervision, and Task7 sample materialization have no separate per-value
identity beyond the explicitly listed bytes/source associations.

Strings use exact UTF-8 with a `u32be` byte length. Integers are unsigned
big-endian. Every canonical vector uses the following primitive grammar:

```text
vector<T> = u32be element_count || T[0] || ... || T[element_count - 1]
```

Therefore this manifest encodes its registry as:

```text
vector<registry_entry>
    = u32be(15)
    || registry_entry[0]
    || ...
    || registry_entry[14]
```

The `element_count` is part of the canonical bytes and MUST equal `15` for
this manifest. An entries-only encoding, an omitted count, any other count,
or trailing bytes is invalid; knowledge of the fixed registry size does not
permit omitting the count. The registry vector order above is semantic and
must not be derived from directory, reflection, map, or enum ordering.
Repository-relative
source paths are audit references only and are not manifest identity inputs.
No filesystem path, timestamp, PID, process topology, allocation order, or
framework/device value is included.

The manifest is invalid if an entry is missing, duplicated, reordered,
unknown, assigned to the wrong owner/class, or carries a floating source
reference such as `latest`, `main`, or an environment-selected version.

## 4. Card vocabulary binding

`ygo::model::CardVocabularyV1` owns the mapping. Its source is an explicit
strictly ascending list of already-public nonzero passcodes:

```text
0 = PAD                         # physical padding only
1 = PUBLIC_UNKNOWN_OR_REDACTED # real redacted public row
2 + rank(passcode)             # known public passcode
```

The static I6 bundle freezes the schema and mapping rule, not a global list.
The exact list is a required immutable per-run/per-vector artifact with:

```text
canonical bytes =
    string(ocgforge.model_card_vocabulary.v1)
    string(ocgforge.model_card_vocabulary.v1)
    string(ascending_public_passcode_rank_plus_two)
    u32be passcode_count
    u32be passcode[0..n-1] in strictly ascending order

identity = model_card_vocabulary.v1.<lowercase SHA-256(canonical bytes)>
```

OCGForge must provide this artifact to an I6 oracle vector or an explicitly
validated runtime configuration. Ignis must accept the exact artifact and
verify its identity; it must not construct one from database traversal,
catalog contents, hidden deck state, or filesystem discovery. A known public
passcode absent from the selected vocabulary rejects the complete model input.

No fixed test vocabulary is accepted yet. A future oracle-vector slice must
generate one from the finite public passcodes in that vector, record its
canonical bytes and identity, and use the same immutable artifact on both
sides. The exact list and resulting identity are `REQUIRES_SOURCE_PROOF` until
that vector is created.

## 5. Field ownership and prohibited aliases

| Value | Owner | I6 handling |
| --- | --- | --- |
| public safe state and visible events | OCGForge public observation contract | Reconstruct only from accepted perspective-safe values; missing fields fail closed |
| Ignis persistent semantic locator | Ignis I3D accepted snapshot for Ignis-side proof | Map to an OCGForge public locator token only after byte-exact vector proof |
| OCGForge public locator token | OCGForge P5 reference contract | Use as current-frame equality token, never physical identity |
| current entity ordinal | OCGForge P5 logical/encoded contract | Derive only from exact current public entity match |
| historical event reference | OCGForge P5 logical/encoded contract | Public locator ordinal only; never rebind to current entity |
| Ignis prompt-local CardCode | Ignis I4/I5 prompt disclosure | No current OCGForge candidate field represents it; whole frame fails closed until an accepted OCGForge mapping exists |
| OCGForge `public_action_key` | OCGForge public action identity codec | Construct from the OCGForge public descriptor, never from the Ignis local key |
| Ignis `I4_LOCAL_CANDIDATE_KEY` | Ignis prompt-instance binding | Private adapter identity only; never sent to model input or OCGForge identity |
| private response bytes/binding | Ignis/EDOPro adapter | Never model input, oracle public value, or routing feature |
| candidate ordinal | local derived coordinate | May label a supervised row; never identity or key replacement |
| CardVocabulary ID | OCGForge vocabulary artifact | Validate exact artifact; do not use as a physical card identity |
| model-input identity | OCGForge logical+encoded+vocabulary identity | Carry/validate exact value; never derive from layout |
| batch offsets, masks, padding, device, dtype | OCGForge physical layout contract | Validate lossless execution only; excluded from model identity |
| Task7 canonical materialization bytes | accepted Task7 materialization contract | Conditional derived bytes; no dataset/checkpoint authority |

### 5.1 Prompt-local CardCode gap

Ignis I4/I5 correctly exposes a prompt-local nonzero CardCode for some legal
current-prompt occurrences, including code-only and no-persistent-locator
cases. The current OCGForge `EnvironmentActionCandidate` and P5
`LogicalCandidate`/`EncodedCandidate` structures have no CardCode field. Their
public card fields are references, not a candidate-local passcode field.

Consequently I6 MUST NOT silently drop, place, hash, or reinterpret that
CardCode. For any frame where the CardCode is semantically required by the
accepted Ignis candidate descriptor and cannot be represented by an accepted
OCGForge public field, the complete model projection fails closed with a
structured `UNREPRESENTABLE_PUBLIC_CANDIDATE_FIELD` diagnostic. Resolving this
requires an OCGForge-owned contract decision or an independently proven rule
that the field is outside the model-facing semantic boundary. I6A does not
make that decision.

## 6. Locator and identity mapping

The intended mapping is:

```text
Ignis accepted PublicSemanticLocatorV1
        ↓ exact current public-reference proof
OCGForge PublicCardReference
        ↓ frame-local token table
OCGForge public_locator_ordinal
        ↓ exact current entity proof where available
OCGForge current_entity_ordinal (optional)
```

The token table is the unique non-empty locator-token set sorted by unsigned
UTF-8 byte order. Equal token text receives equal frame-local ordinals. This
means token equality only; it is not physical-card identity, persistent
identity, replay identity, or hidden continuity.

Current candidate/chain/relationship references may carry a current entity
ordinal only when the OCGForge public safe state contains exactly one current
entity with the same public locator. Zone, sequence, source ordinal, CardCode,
Ignis MirrorEntityId, or collection order is not proof. Historical visible-event
references never receive a current entity ordinal.

Ignis and OCGForge locator grammars are not aliased merely because existing
examples look identical. Each admitted mapping needs a native OCGForge oracle
vector. A mapping collision, missing proof, or hidden-continuity ambiguity
rejects the complete frame.

## 7. Supported candidate mapping

For every accepted Ignis candidate, the future bridge constructs one OCGForge
`EnvironmentActionCandidate` from public semantic fields only:

| Ignis semantic family | OCGForge public descriptor treatment |
| --- | --- |
| I4 YESNO / EFFECTYN | `action_kind` and typed `PublicChoice` with value 0/1 |
| I4 OPTION / ANNOUNCE_NUMBER | typed public value plus exact response selector; selector is not candidate-vector ordinal |
| I4 CHAIN / BATTLE / IDLE | family-specific public action kind, choice, references, phase/position, and safe metadata |
| I5 SELECT_CARD / TRIBUTE / UNSELECT | `pick`, `finish`, `cancel`, or public card-selection descriptor fields as contractually representable |
| I5 PLACE / DISFIELD | `place`/`position` public fields and exact semantic reference where one exists |
| I5 RACE / ATTRIB | `announcement`/`pick` public fields; private mask continuation is not copied |
| I5 COUNTER | `assign_amount`, source index, and public amount; private binding is not copied |
| I5 SORT_CARD / SORT_CHAIN | `pick`/`cancel`, source index, and prompt-local/public reference fields as representable |
| prompt-local-only CardCode variant | no accepted P5 destination field; fail closed as specified in section 5.1 |

The actual action-kind token and typed-choice construction must be validated
against `EnvironmentActionKind`, `PublicChoiceKind`, and
`PublicActionKeyInput` from OCGForge. The bridge must not use a nullable broad
record to invent fields, and it must not force unrelated Ignis candidate kinds
into a convenient OCGForge kind.

After constructing the descriptor, the bridge calls the exact OCGForge public
action identity algorithm (or an independently validated byte-equivalent
implementation) to produce:

```text
public_action_key =
    public_action.v1.<lowercase hexadecimal canonical descriptor bytes>
```

The exact key is then stored only in the OCGForge routing sidecar. Ignis local
keys remain available only to a private binding owned by the adapter. If two
Ignis candidates map to one public key, if one candidate cannot be mapped, or
if the key is not valid under the OCGForge codec, the whole frame fails closed;
no candidate is dropped or disambiguated by order.

## 8. Canonical model layers

The OCGForge model sequence is fixed:

```text
PublicEnvironmentObservation + complete EnvironmentActionCandidate[N]
    → LogicalModelInputV1
    → EncodedModelInputV1 + routing sidecar[N]
    → ModelBatchLayoutV1 (derived physical view)
    → optional Task7 materialized tables
```

At every available semantic boundary:

```text
N source candidates = N logical candidates = N encoded rows
= N routing keys = N real materialized candidate rows = N score slots
```

Source order is copied exactly. No sort, deduplication, truncation, top-K,
fixed width, first-match resolution, fallback candidate, or automatic N=1
answer is permitted.

Logical input preserves public values and explicit optional presence. Its
canonical primitives are unsigned big-endian integers, length-prefixed UTF-8
strings/bytes, booleans `0/1`, and exact signed `i32` two's-complement bits.
Encoded input maps only fixed public categories and the supplied immutable
CardVocabulary. `public_action_key` is control/routing metadata, not learned
string feature data. Task7 uses exact base-2^16 limbs and masks; it does not
repair the lossy Task4 float representation.

`ModelBatchLayoutV1` is a lossless ragged/padded execution view. Padding row
mask `0` and CardVocabulary ID `0` are physical only; a real unknown/redacted
row uses mask `1` and ID `1`. Layout width, batch composition, row offsets,
device, dtype, framework, and allocation order do not enter
`model_input_identity`.

`ModelSupervisionSampleV1` is derived from an admitted trajectory record. Its
zero-based candidate ordinal is a training label coordinate only; the exact
selected public key remains routing/audit metadata.

## 9. Task4 versus Task7

```text
ocgforge.phase6.task4.numeric_projection.v1
    = historical bounded smoke, lossy float rows, not I6 semantic authority

ocgforge.phase6.task7.input_materialization.v1
    = accepted non-smoke physical materialization contract/configuration
```

Task4's fixed float rows may collapse distinct valid `u32` values and are not
reused as a Task7 or I6 model-input identity. Task7 consumes validated Phase-5
logical/encoded values and a validated ragged layout, then emits typed tables
and exact limbs. PyTorch is physical execution only.

The Task7 materialization contract and implementation are present in the
audited OCGForge `main` tree, including the configuration KAT:

```text
CONFIG_CANONICAL_BYTES_LENGTH=8133
CONFIG_CANONICAL_BYTES_SHA256=20f394c888e959446fa263c3520f3dd3b1f48b3a23e58373da7153a691ab1e7a
CONFIGURATION_IDENTITY=phase6_task7_input_materialization_config.v1.20f394c888e959446fa263c3520f3dd3b1f48b3a23e58373da7153a691ab1e7a
```

This is not a claim that Task7 readiness or a meaningful dataset/checkpoint
exists. The dataset-authority contract is still proposed/not ready and the
Task7 readiness record remains blocked. I6 must treat the materialization
stage as conditional until its own accepted implementation/evidence status is
stamped.

## 10. Failure and privacy rules

I6 rejects the complete projection on any of these conditions:

```text
unknown or incompatible bundle/OCGForge contract ID
floating or mismatched source identity
invalid public observation or safe-state canonical bytes
missing visible state/event field required by the selected OCGForge input
unrepresentable prompt-local public candidate field
invalid, duplicate, reordered, missing, or detached public action key
candidate count/order/key mismatch
locator token without accepted public proof
historical reference assigned a current entity ordinal
known passcode absent from the selected immutable vocabulary
unknown/redacted identity presented as known
logical/encoded/model-input identity mismatch
ragged/padded count, offset, mask, presence, or roundtrip mismatch
Task7 configuration/materialization mismatch when that stage is requested
any private response, Mirror identity, raw protocol, pointer, PID, path,
    socket, model-input, or hidden-state value entering semantic bytes
```

No error diagnostic may include hidden identity, private response bytes,
private locators, raw buffers, or filesystem paths. A failure invalidates the
complete I6 frame/binding; it does not publish a reduced domain or fall back to
Task4, native AI, Teacher, RandomLegal, or a guessed mapping.

For paired hidden worlds with equal accepted public observation and public
candidate semantics, all available semantic stages must be byte/equality
identical. Different hidden opponent hand/deck identities, face-down card
identity, private Mirror IDs, prompt-local physical addresses, or allocation
order must not affect them.

## 11. Current readiness and explicit gaps

The following are deliberately not silently resolved by I6A:

```text
I6_PUBLIC_STATE_SOURCE=BLOCKED
I6_EVENT_ORACLE=BLOCKED
I6_PROMPT_LOCAL_CARDCODE_MAPPING=BLOCKED
I6_FIXED_VOCABULARY_ARTIFACT=REQUIRES_SOURCE_PROOF
I6_TASK7_FINAL_ACCEPTANCE=NOT_PROVEN
I6_RULES_DOMAIN_COMPATIBILITY=DIFFERENT_OR_UNPROVEN
I6_CHECKPOINT_COMPATIBILITY=NOT_AN_I6_FINAL_GATE
I6_I7_CHECKPOINT_PREREQUISITES=DOCUMENTED_ONLY
```

### 11.1 Ignis public-state/event gap — BLOCKER

The accepted Ignis I3D projection currently exposes participants, public card
snapshots, turn/phase/terminal values, and public locators. It does not expose
the OCGForge `PublicEnvironmentObservation`/`PublicSafeStateView` field set in
full: canonical public zones, relationships, chain-state fields, visible event
history, static public deck vectors, and the exact OCGForge observation
decision context are not one accepted Ignis source value. In particular,
Ignis has no accepted visible-event history equivalent to OCGForge's
`PublicSafeVisibleEvent` sequence.

No I6 implementation may synthesize those events from raw protocol buffers,
private Mirror state, current slots, or wall-clock/process order. A future
I3/I6 source-closure decision must provide the missing accepted public source
or explicitly narrow the I6 model scope. Until then O0/O2/O5 full equality is
blocked.

### 11.2 Candidate-local CardCode gap — BLOCKER

The accepted Ignis I5 public descriptors can contain a prompt-local CardCode,
while the audited OCGForge public candidate and Phase-5 logical/encoded
candidate structures contain no candidate-local CardCode field. Dropping it
would make the I6 bridge not value-preserving; putting it in a reference,
choice, key, or vocabulary without an accepted rule would invent semantics.
The whole affected frame must fail closed until OCGForge owns a compatible
field or a reviewed source decision excludes the field from the model boundary.

### 11.3 Runtime/rules/checkpoint compatibility — known blocked dependency

OCGForge's canonical rules bundle on its `main` contract is:

```text
bundle_id = 3adfe6b4cfe2c2805e50b389fc0eb4e70a3b0b6107436614d328fddc865e585f
format = TCG_ADVANCED_2026_05_18
duel_mode = DUEL_MODE_MR5
duel_flags = 190464
core_api = 11.0
core_commit = 9a0c558c2d686542f7914a6d529fd7aa57746aed
core_resolved_checkout_sha256 = 161849049d34de7ea60b2f370cc35f903262c14769e399d0bf43a381d295d7f3
core_patchset = ocgforge.ocgcore.api_hardening.v1
cardscripts_commit = f337c87018ca723c1aded5143e616bb649555273
database_commit = 89ad6837b0766a52984d8c715a7d5d4f8447946b
```

Ignis targets EDOPro `30935e847165a9ef0e547fb51a43f36168fab7c7`, its ocgcore
gitlink `46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57`, PRO_VERSION `0x1354`, API
11, and a different externally configured/runtime resource set. The pinned
core commits, patch identities, scripts/database, format/flags, and locked
decks are not proven equivalent. Therefore I6 may compare explicitly supplied
public semantic vectors, but no I6A document may claim identical rules
trajectories or checkpoint binary compatibility.

## 12. Acceptance boundary

This bundle is accepted for design review only when every static entry above
is present, owned by OCGForge, and bound to an exact source snapshot. It does
not authorize I6 runtime code. A later I6 final acceptance additionally needs
the oracle gates in the design and plan documents, including closure of the
two blockers, exact vocabulary artifacts, native cross-oracle vectors, and
explicit runtime compatibility evidence. A Task7 materialization-only bridge
does not wait for `DatasetManifest`, `TrainingDatasetSplitV1`, or a checkpoint;
those are separate Task7/I7 authorities.

```text
I6A_RUNTIME_IMPLEMENTATION_AUTHORIZED=NO
I6_FINAL=NO
I7_AUTHORIZED=NO
```
