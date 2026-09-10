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
| opponent public Hand (known public identity) | public ordinal | public ordinal | 1 exact | exact public token |
| isolated Extra Deck | public ordinal | public ordinal | 1 exact | exact public token |
| same code across Hand + Extra | per-zone ordinal | shared ordinal | 1, no exact token | unique safe attribute match; not mapping authority |
| hidden opponent Hand | no per-card public entity | no safe identity proof | 0 | no safe public proof; reject |
| stale frame binding | any | any | not authoritative | stale; reject |
| missing current entity | any | none | 0 | missing; reject |

The unique-match rows are observations about available safe attributes, not an
approved mapping authority and not an approval to search by guessed sequence
or card identity. The duplicate row
proves that `(player, zone, known public code)` is not sufficient to identify
an own-hand occurrence when multiplicity is greater than one.

## Existing private seam inventory

The current I4 binding is intentionally recorded as a negative capability
finding. `CurrentFlatPromptBindingV1` retains the prompt instance/family,
public candidate values, local routing keys, continuation state, and response
bindings. `FlatPromptCardCorrelationResultV1` retains the accepted I4 public
locator and safe public-code result. Neither current type carries a private
source occurrence, `MirrorEntityIdV1`, `ModernLocInfoV1`, or raw address.

The gameplay projection does temporarily possess the wire occurrence and the
exact mirror correlation while constructing a public candidate, but that
proof is not currently transported into the I4 binding or across the I6D
interface. This is an observed seam gap, not permission to expose those
values. The production seam remains absent; the following design freezes the
smallest permitted carrier and lifecycle before any implementation is
authorized.

## Frozen private source-occurrence binding design

This section is a design contract only. It does not add a production type or
authorize the I6D mapping implementation.

### Owner, acquisition seam, and visibility

The semantic owner is the I6D `OcgForgePublicCandidateBridgeV1`. Source-proof
acquisition belongs at the internal Gameplay/I4 correlation seam, while the
normalized prompt occurrence and the same-snapshot I6C5 locator are both
available. Concretely, the future handoff is created by the
`FlatPromptProjectionV1` / `FlatPromptCardCorrelationV1` path after exact
`ModernLocInfoV1` normalization and exact current-mirror resolution, but
before `CompleteCorrelation` reduces the result to `AcceptedLocator` and
`SafeCardCode`.

The carrier is an internal immutable `PrivateCrossLocatorBindingV1` value,
owned by the Gameplay-to-Model handoff and never exposed as a public I4 or
I6D member. Because the semantic consumer is in the Model assembly, the
implementation must use one narrow internal friend/handoff seam rather than
making the occurrence fields public. `FlatPromptProjectionResultV1`,
`FlatPublicCandidateDescriptorV1`, and the public
`OcgForgePublicCandidateBridgeV1.TryCreate(acceptedDecision)` interface do not
gain private occurrence fields or a detached binding-list argument.

The accepted decision boundary owns the complete private binding set
internally. The public bridge continues to receive only the accepted decision
boundary. A boundary cannot be accepted with a separately supplied binding
set that was assembled by the caller after the projection.

### Exact carrier fields and field roles

The conceptual carrier has the following exact fields. The categories are
semantic: lifecycle coordinates and lookup checks are not public identity;
source-occurrence fields are private provenance; only the target locator is
allowed to feed the already accepted OCGForge public descriptor.

```text
lifecycle / frame binding:
  PromptInstanceOrdinal       : ulong
  ContinuationStep             : int
  FrameInstanceOrdinal         : ulong
  AcceptedPublicProjectionId   : existing I3D PublicProjectionId string

candidate lookup / cross-check:
  I4LocalCandidateKey          : exact current local key
  SourceSection                : FlatPromptSourceSectionV1
  SourceOrdinal                : int

private exact source occurrence:
  AbsoluteController           : byte, 0 or 1
  NormalizedZone               : MirrorZoneV1
  SourceSequence               : uint
  IsOverlay                    : bool
  OverlayIndex                 : uint?; present exactly when IsOverlay=true

safe target:
  AcceptedI6C5TargetLocator    : exact PublicSemanticLocatorV1 from the
                                 same accepted I6C5 frame
```

`FrameInstanceOrdinal` is assigned by the owning gameplay/frame session only
after a complete I6C5 frame is accepted. It is monotonic within that session,
derived from committed frame order, never caller-supplied, never serialized,
and never included in a public digest. `AcceptedPublicProjectionId` is an
existing I3D consistency guard; it is not a new semantic identity.

The carrier contains no prompt-local CardCode, `MirrorEntityIdV1`, raw
`ModernLocInfoV1`, raw `loc_info`, object address, pointer, runtime hash, or
collection-order marker. A transient `MirrorEntityIdV1` may be used only
inside the same-snapshot builder to join the exact resolved mirror card to
the existing I6C3 locator table; it is not stored, not used as the authority
by itself, and never published.

### Creation and target proof

For each card-bearing candidate that does not already have exact I4/I6C5
token equality, creation is transactional:

```text
accepted complete I4 projection + accepted current I6C5 frame
    -> normalize the prompt address
    -> resolve exactly one current mirror occurrence
    -> obtain that occurrence's exact I6C3/I6C5 locator from the same snapshot
    -> verify the target locator occurs exactly once in the accepted frame
    -> bind candidate key/section/ordinal to the occurrence and target
    -> validate the complete binding set before accepting the decision boundary
```

The target is obtained from the same current I6C3 occurrence-to-locator
mapping that produced the accepted I6C5 frame. It is not created by a second
public locator factory, searched by `(player, zone, CardCode)`, selected by
first match, or inferred from collection order. The normalized source
occurrence is the private join proof; its source sequence is never copied as
a replacement for an I4 public locator. An OCGForge indexed hand target is
public only because it is the accepted OCGForge locator produced by this
same-frame mapping.

Exact I4/I6C5 token equality remains the existing fast path. It may omit a
private binding. If a binding is present for that path, its target must equal
the exact token or the whole boundary fails. A non-equal locator form requires
the private binding; missing it fails closed.

### Exact I6D consumption interface

The private set is created before the accepted decision boundary and carried
through one internal producer overload. The conceptual construction seam is:

```text
internal TryAccept(
    frame,
    completeProjection,
    PrivateCrossLocatorBindingSetV1 completeBindings,
    out OcgForgeAcceptedDecisionBoundaryV1 boundary,
    out error)
```

`PrivateCrossLocatorBindingSetV1` is an internal immutable set, not a public
list. It contains exactly one binding for every candidate that needs a
non-equal locator mapping, no binding for non-card candidates, and no unknown
or duplicate lookup keys. The existing public
`OcgForgePublicCandidateBridgeV1.TryCreate(acceptedDecision)` remains the only
public consumption interface; it reads the set from the accepted decision
boundary's internal member. A caller cannot pass a second list after the
boundary has been accepted. The existing two-argument producer path may
construct an empty set for exact-token-only candidates, but the bridge must
reject any non-equal mapping that arrives without the complete internal set.

The internal producer overload must validate the set atomically against the
complete projection before constructing the boundary. The I6D bridge then
uses the exact-token path where possible and otherwise resolves by the
primary binding key, checks the frame/projection coordinates and candidate
cross-checks, verifies the target in the current frame, and only then emits
the existing OCGForge descriptor. No private field is added to
`FlatPromptProjectionResultV1` or to a public candidate type.

### Lookup, consumption, and lifecycle

The primary lookup key is exactly:

```text
(PromptInstanceOrdinal, ContinuationStep, I4LocalCandidateKey)
```

`SourceSection` and `SourceOrdinal` must equal the accepted candidate and are
cross-checks, never a fallback search key. Candidate-array index is not a
binding authority. The future I6D bridge consumes the binding through the
internal member of `OcgForgeAcceptedDecisionBoundaryV1`; it does not accept a
detached caller-supplied list.

The lifecycle is:

```text
same committed frame + accepted prompt
    -> create immutable binding set
    -> consume only while prompt instance and continuation step match
prompt replacement or continuation transition
    -> discard old set; create a new set from the new accepted frame/prompt
frame replacement, terminal selection, session disposal, or boundary failure
    -> invalidate/discard the set
```

An old binding is never reused for a later continuation step, even when the
prompt instance ordinal remains the same. No binding is persisted, cached
across duels, serialized, or used as a replay/public identity. Any invalid
member causes atomic rejection of the complete candidate boundary; no partial
candidate list is accepted.

### Duplicate and collision semantics

Duplicate own-hand cards are paired by exact source occurrence, not by public
CardCode. Two current own-hand occurrences with the same code but different
source sequences create two distinct private occurrences and two distinct
I6C5 indexed targets; both may be accepted when all other checks pass. A
single source occurrence may back multiple distinct action choices only when
each binding has the same source occurrence and the same target, while the
existing public action keys remain distinct.

The following are whole-boundary failures:

```text
duplicate binding lookup key
one binding key with conflicting source occurrence or target
two distinct source occurrences mapping to one target locator
target locator missing or non-unique in the current I6C5 frame
source occurrence missing or non-unique in the current mirror snapshot
source/section/ordinal mismatch
stale prompt, continuation, frame, or accepted-projection binding
```

Opponent public Hand remains on the exact public-token path. Hidden opponent
Hand produces no per-card public I4 entity and therefore no binding. Cross-
pile same-code cases use the exact source occurrence to obtain the OCGForge
shared-ordinal target; they must not recompute a per-zone ordinal.

### Public-identity and privacy rules

The private source occurrence and every lifecycle coordinate are excluded from
`public_action.v1` descriptor bytes, `public_action.v1` keys,
`public_candidate_domain.v1` digests, `PlayerObservation`, logical/encoded
model input, and Task7 materialization. Only the accepted safe target locator
may feed the existing public descriptor mapping. The binding itself is not a
new public semantic value.

Paired-world equality applies when the accepted safe target and all other
public semantics are identical: different private occurrence data must then
produce identical public descriptor/key/domain results. If the accepted
OCGForge target itself differs because the frozen OCGForge public semantics
make the difference observable, the public result may differ; that is not a
private-data leak.

The design therefore freezes these requirements:

```text
PRIVATE_BINDING_IN_PUBLIC_DESCRIPTOR = NO
PRIVATE_BINDING_IN_PUBLIC_ACTION_KEY = NO
PRIVATE_BINDING_IN_DOMAIN_DIGEST     = NO
PRIVATE_BINDING_IN_MODEL_INPUT       = NO
PRIVATE_BINDING_IN_OBSERVATION       = NO

MIRROR_ENTITY_ID_PUBLIC              = NO
RAW_LOC_INFO_PUBLIC                  = NO
HAND_SEQUENCE_PUBLIC_SUBSTITUTE      = NO
PROMPT_CARDCODE_PUBLIC_SUBSTITUTE    = NO

STALE_BINDING      = WHOLE_BOUNDARY_REJECT
MISSING_BINDING    = WHOLE_BOUNDARY_REJECT
AMBIGUOUS_BINDING  = WHOLE_BOUNDARY_REJECT
COLLIDING_BINDING = WHOLE_BOUNDARY_REJECT
```

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
CURRENT_PRIVATE_OCCURRENCE_SEAM=ABSENT

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
DESIGNED_OWNING_LAYER=I6D OcgForgePublicCandidateBridgeV1
DESIGNED_SOURCE_PROOF_ACQUISITION=Gameplay/I4 correlation seam before CompleteCorrelation
DESIGNED_FRAME_LOCAL_BINDING=internal immutable set carried by accepted decision boundary
PUBLIC_IDENTITY_IMPLICATIONS=private occurrence data must stay outside public identity
REPLAY_DETERMINISM_IMPLICATIONS=bind only current accepted frame/prompt, reject stale or ambiguous mappings

I6D_CROSS_LOCATOR_MAPPING_IMPLEMENTATION=NOT_IMPLEMENTED
I6D_PRIVATE_SOURCE_OCCURRENCE_BINDING_DESIGN=FROZEN_PENDING_REVIEW
I6G_COUNTER_CAPTURE_RETRY=NOT_AUTHORIZED
I6G_LINK_CAPTURE=NOT_AUTHORIZED
I6G_FRESH_PROCESS_A_B=NOT_AUTHORIZED
I6G_FINAL=NO
I6_FINAL=NO
I7_AUTHORIZED=NO
```
