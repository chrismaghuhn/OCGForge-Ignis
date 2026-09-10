# I6D Cross-Locator Mapping Authority Characterization

Status: test/documentation-only characterization; no mapping implementation is
authorized by this document.

## Frozen authorities

The characterization uses the following sources without changing them:

```text
Ignis public-state source:
  docs/contracts/public-state-projection-v1.md

Ignis I6A mapping boundary:
  docs/superpowers/specs/2026-09-06-i6a-model-contract-cross-oracle-design.md

OCGForge source binding:
  f929de0b4d4157327dba003067d2e21e42f7ad75
```

The pinned OCGForge implementation is the executable authority for its
locator behavior. In `src/observation/observation_builder.cpp`:

```text
sequence_is_visible(HAND, owner, perspective)
    == (owner == perspective)

sequence_is_visible(ExtraDeck, ...)
    == false

non-sequence-visible locator ordinal key
    == (controller, card_code)
```

Therefore an own-hand card is represented by an indexed locator in OCGForge,
while a non-sequence-visible card uses the shared public ordinal counter. The
Ignis I3D/PublicState contract is a separate authority: known Hand and
Extra-Deck public ordinals are scoped by `(absolute_player, zone, card_code)`.
The two locator vocabularies are not aliases.

## Observed forms

The test characterization constructs the I4 `PublicStateProjectionV1` and the
I6C5-consumed I6C3 entity source from the same mirror. It does not manufacture
an accepted full I6C5 frame when the independent I6C5 coverage guard rejects a
fixture. Only public player/zone/code availability, locator form, and match
counts are retained in the diagnostic result; no raw protocol address,
`MirrorEntityIdV1`, hidden identity, or response value is emitted.

| Case | I4 form | I6C5/OCGForge form | Safe current match | Characterization |
| --- | --- | --- | ---: | --- |
| own Hand, unique code | public ordinal | indexed | 1 | unique safe attribute match; not mapping authority |
| own Hand, duplicate same code | public ordinal | indexed | 2 | ambiguous; reject |
| opponent public Hand | public ordinal | public ordinal | 1 exact | exact public token |
| isolated Extra Deck | public ordinal | public ordinal | 1 exact | exact public token |
| same code across Hand + Extra | per-zone ordinal | shared ordinal | 1, no exact token | unique safe attribute match; not mapping authority |
| hidden opponent identity | no safe candidate identity | no safe identity proof | 0 | no safe public proof; reject |
| stale frame binding | any | any | not authoritative | stale; reject |
| missing current entity | any | none | 0 | missing; reject |

The unique-match rows are observations about available safe attributes, not an
approved mapping authority and not an approval to search by guessed sequence
or card identity. The duplicate row
proves that `(player, zone, known public code)` is not sufficient to identify
an own-hand occurrence when multiplicity is greater than one.

## Boundary implications

The current `OcgForgePublicCandidateBridgeV1` exact-token check remains
correct for its current API. It must not silently reinterpret an Ignis
locator as an OCGForge locator.

Any future mapping must be an explicit I6D-owned, frame-local, deterministic
proof boundary. A permissible proof would have to be one of:

```text
1. exact equality with the current I6C5/OCGForge public locator; or
2. a uniquely proven current public attribute match whose resulting OCGForge
   locator is obtained from the accepted current safe frame; this remains a
   characterization result, not an accepted authority; or
3. a separately accepted private frame-local source-occurrence binding whose
   private data never enters the public descriptor, public key, or model input.
```

The current characterization does not establish option 3 as an existing
capability. In particular, it does not authorize using:

```text
MirrorEntityIdV1
raw loc_info
raw hand sequence as a public substitute
prompt-local CardCode as a public field
collection order or first-match behavior
```

Ambiguous, missing, stale, or colliding correspondence must reject the whole
candidate/frame. No candidate may be dropped or replaced with a guessed
reference.

## Evidence gates

The focused Ignis characterization target is:

```text
tests/OCGForge.Ignis.Gameplay.Tests/
  Fixtures/I6DCrossLocatorMappingAuthorityCharacterizationV1.cs
  Tests/I6DCrossLocatorMappingAuthorityCharacterizationTests.cs
```

It proves:

```text
OWN_HAND_NATIVE_OCGFORGE_VECTOR=PASS
I4_OWN_HAND_LOCATOR_FORM=PROVEN
OCGFORGE_OWN_HAND_LOCATOR_FORM=PROVEN

DUPLICATE_SAME_CODE_OWN_HAND_CASE=CHARACTERIZED
PUBLIC_OPPONENT_HAND_CASE=CHARACTERIZED
EXTRA_DECK_CASE=CHARACTERIZED
CROSS_PILE_SHARED_ORDINAL_CASE=CHARACTERIZED

MAPPING_COLLISION_REJECTION_REQUIREMENT=DOCUMENTED
AMBIGUOUS_MAPPING_REJECTION_REQUIREMENT=CHARACTERIZED
MISSING_MAPPING_REJECTION_REQUIREMENT=CHARACTERIZED
STALE_MAPPING_REJECTION_REQUIREMENT=CHARACTERIZED
UNIQUE_MATCH_IS_MAPPING_AUTHORITY=NO

HIDDEN_IDENTITY_USED=NO
MIRROR_ENTITY_ID_USED_AS_PUBLIC_PROOF=NO
RAW_POINTER_OR_PROTOCOL_ADDRESS_EXPOSED=NO
```

The native checks used for source confirmation are the existing pinned
OCGForge observation/action tests. The native source and build checkout remain
read-only; their generated build output is not Ignis acceptance evidence.

## Non-goals and current result

This characterization does not change I4, I6C5, I6D, the locator codec, the
public-state codec, runtime behavior, or the OCGForge contract. It does not
run the Counter scenario, send a response, or admit the result as I6G runtime
acceptance evidence.

```text
PROPOSED_OWNING_LAYER=I6D OcgForgePublicCandidateBridgeV1, pending review
PROPOSED_FRAME_LOCAL_BINDING=REQUIRES_EXPLICIT_ACCEPTED_SOURCE_PROOF
PUBLIC_IDENTITY_IMPLICATIONS=private occurrence data must stay outside public identity
REPLAY_DETERMINISM_IMPLICATIONS=bind only current accepted frame/prompt, reject stale or ambiguous mappings

I6D_CROSS_LOCATOR_MAPPING_IMPLEMENTATION=NOT_IMPLEMENTED
I6G_COUNTER_CAPTURE_RETRY=NOT_AUTHORIZED
I6G_LINK_CAPTURE=NOT_AUTHORIZED
I6G_FRESH_PROCESS_A_B=NOT_AUTHORIZED
I6G_FINAL=NO
I6_FINAL=NO
I7_AUTHORIZED=NO
```
