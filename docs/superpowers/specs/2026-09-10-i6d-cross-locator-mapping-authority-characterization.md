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

Required private correlation transition:
  docs/contracts/flat-prompt-correlation-sidecar-v1.md
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

The current I4 prompt binding remains a negative capability finding.
`CurrentFlatPromptBindingV1` retains the prompt instance/family, public
candidate values, local routing keys, continuation state, and response
bindings. `FlatPromptCardCorrelationResultV1` retains the accepted I4 public
locator and safe public-code result. Neither current prompt-binding type
carries a private source occurrence, `MirrorEntityIdV1`, `ModernLocInfoV1`, or
raw address. Implementation 01 now stores the projection-scoped occurrence
sidecar separately; prompt correlation and I6D consumption still do not read
it.

The gameplay projection does temporarily possess the wire occurrence and the
exact mirror correlation while constructing a public candidate, but that
proof is not currently transported into the I4 binding or across the I6D
interface. This is an observed seam gap, not permission to expose those
values. The I4-to-I6D binding seam remains absent; the following design
freezes the smallest permitted carrier and lifecycle before that enablement
is authorized.

## Frozen private source-occurrence binding design

This section is the accepted design contract. Implementation 01 adds only the
projection-scoped sidecar; it does not enable prompt correlation or implement
the I6D mapping.

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
owned by the Gameplay-side source-proof implementation and never exposed as a
public I4 or I6D member. The Model assembly does not receive a friend view of
Gameplay internals. Instead, one public opaque
`I6DPrivateCrossLocatorBindingHandoffV1` capability crosses the project
reference; it has no public constructor, fields, occurrence properties, or
serialization surface and exposes only the validated safe-target operation
specified below. `FlatPromptProjectionResultV1`,
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
through one opaque capability. The conceptual cross-assembly interface is:

```text
public opaque I6DPrivateCrossLocatorBindingHandoffV1
    TryGetValidatedTarget(
        prompt_instance,
        continuation_step,
        frame_instance,
        projection_id,
        local_key,
        source_section,
        source_ordinal,
        current_frame,
        out safe_target,
        out error)
```

`PrivateCrossLocatorBindingSetV1` remains an internal immutable set, not a
public list. It contains exactly one binding for every candidate that needs a
non-equal locator mapping, no binding for non-card candidates, and no unknown
or duplicate lookup keys. The opaque capability is the only value that can
cross from Gameplay to Model; its operation returns only an already validated
safe target or a structured failure. The existing public
`OcgForgePublicCandidateBridgeV1.TryCreate(acceptedDecision)` remains the only
public consumption interface; it consumes the capability through the accepted
decision boundary's internal member. A caller cannot pass a second list after
the boundary has been accepted. The existing two-argument producer path may
construct an empty capability for exact-token-only candidates, but the bridge
must reject any non-equal mapping that arrives without the complete internal
set.

The internal Gameplay-side capability factory validates the set atomically
against the complete projection before returning the opaque value. The I6D
bridge then uses the exact-token path where possible and otherwise calls the
single safe-target operation, checks the frame/projection coordinates and
candidate cross-checks, verifies the target in the current frame, and only
then emits the existing OCGForge descriptor. No private field is added to
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

## Remediation 02: duplicate own-hand I4 amendment

The real I4 path has an earlier fail-closed boundary than the I6D carrier.
With two current own-hand occurrences that have the same known CardCode but
different source sequences, the complete modern `MSG_SELECT_IDLECMD` prompt
is parsed, but each pile correlation sees two matching I4 public cards. The
current `TryCorrelatePile` / `CompleteCorrelation` path therefore returns
`UnprovenPublicReference`, the prompt result has no context or candidates, and
I6D is not reached. This is an executable characterization, not admitted
Counter-capture evidence.

The design decision is Option A:

```text
OPTION_A = SELECTED
  private source-occurrence -> existing I4 public-ordinal sidecar

OPTION_B = REJECTED
  changing own-Hand public locators to indexed semantics would change the
  frozen public-state contract and its canonical identity

OPTION_C = REJECTED
  continuing to reject duplicate same-code own-Hand occurrences would leave
  a complete legal I4/I6D candidate domain unavailable
```

### Explicit I4 contract amendment

This is an amendment to the internal prompt-correlation contract only. It is
not a change to `ocgforge-ignis.public-state-projection.v1` public semantics:

```text
I4_PUBLIC_LOCATOR_SEMANTICS = UNCHANGED
PUBLICSTATE_BYTES            = UNCHANGED
PUBLICSTATE_IDENTITY         = UNCHANGED
```

The I4 implementation produces an internal immutable
`PrivateI4OccurrencePublicLocatorSidecarV1` alongside the accepted public
projection. For each public known Hand/Extra-Deck occurrence, the sidecar
records the already assigned I4 public ordinal for the exact normalized
source occurrence. It does not add a sequence to `PublicCardStateV1`, its
canonical bytes, or its identity.

Implementation 01 creates this sidecar only through an internal frame-owned
projection overload with a session-supplied `FrameInstanceOrdinal`. The
existing two-argument PublicState projection path remains unchanged and does
not expose or consume a sidecar; prompt-correlation enablement is separate.

The sidecar container and its entries have these exact fields. The first two
are container-scoped; the remaining fields belong to each entry:

```text
container:
  FrameInstanceOrdinal
  AcceptedPublicProjectionId

entry:
AbsoluteController
NormalizedZone
SourceSequence
IsOverlay
OverlayIndex                 # present exactly for overlay occurrences
AcceptedI4PublicLocator
```

Its exact lookup key is:

```text
(FrameInstanceOrdinal,
 AcceptedPublicProjectionId,
 AbsoluteController,
 NormalizedZone,
 SourceSequence,
 IsOverlay,
 OverlayIndex)
```

### Deterministic duplicate pairing rule

The sidecar does not inherit the iteration order of a mirror collection. When
one public pile group contains multiple occurrences with the same known
CardCode, the existing I3D `KnownPileCard.Compare` position ordering is
retained first: an absent position sorts before a present position, then the
numeric position sorts ascending. Equal public sort keys are resolved by this
complete private scalar tie-break, in exactly this order:

```text
(position_presence_and_value,
 absolute_controller,
 normalized_zone,
 source_sequence,
 is_overlay,
 overlay_index)
```

`position_presence_and_value` uses the existing null-before-known rule. The
remaining values use ordinal numeric comparison; `OverlayIndex` is present
only for an overlay. The full key must be unique. A duplicate full key is a
collision and rejects the whole projection/prompt; insertion order, dictionary
iteration, allocation order, CardCode re-search, and first-match behavior are
never tie-breakers.

This rule determines only the private association between a current source
occurrence and the already emitted public ordinal. It does not add the private
tie-break values to `PublicCardStateV1`, canonical bytes, or public identity.
For the same-code/same-position own-Hand fixture, source sequence order is
therefore deterministic in both input orders while the public projection
remains byte-identical.

The sidecar is populated at the same point at which the I3D projection
assigns the public ordinal, so duplicate own-hand occurrences are paired with
the actual public ordinals produced by the accepted projection. A transient
`MirrorEntityIdV1` may join the current mirror card to that assignment while
building the sidecar, but the ID is not stored, serialized, hashed, or
published. The sidecar never searches by CardCode alone, uses collection
order, or guesses a sequence.

When an I4 prompt supplies an exact current source occurrence, the pile
correlator must look up that occurrence in the sidecar and verify the
accepted public card/code facts already required by I4. A missing or
ambiguous sidecar entry fails closed. Existing exact public-token cases remain
unchanged. Hidden opponent Hand still creates no per-card sidecar entry.
Main Deck and other unsupported pile forms remain fail-closed; this amendment
does not make hidden identity or physical continuity observable.

The sidecar is an internal prompt-correlation aid, not a public locator
replacement. In particular:

```text
RAW_HAND_SEQUENCE_IN_PUBLIC_LOCATOR = NO
MIRROR_ENTITY_ID_PUBLIC             = NO
CARDCODE_HEURISTIC                  = NO
COLLECTION_ORDER_HEURISTIC          = NO
```

### Controlled Gameplay-to-Model assembly handoff

The actual project dependency direction is Model -> Gameplay. The
cross-assembly decision therefore avoids a reverse project reference and
avoids granting Model access to Gameplay internals:

```text
Gameplay owns:
  internal immutable PrivateI4OccurrencePublicLocatorSidecarV1
  internal immutable PrivateCrossLocatorBindingV1
  one public opaque I6DPrivateCrossLocatorBindingHandoffV1 capability

Model may receive only:
  the capability's validated accepted-target operation

Model may not receive:
  MirrorSnapshotV1
  MirrorEntityIdV1
  ModernLocInfoV1
  raw loc_info
  private occurrence fields
```

The capability is the only controlled handoff. It has no public constructor,
no public fields or occurrence properties, no serialization, and no method
that returns private source data. It returns only an accepted safe target or
a structured failure after validating the complete lifecycle and candidate
coordinates. `InternalsVisibleTo("OCGForge.Ignis.Model")` is explicitly not
part of this design; Model cannot read private Gameplay internals.

The capability is obtained only from the current prompt session after the
sidecar and complete prompt have been accepted:

```text
FlatPromptSessionV1.TryCreateI6DPrivateBindingHandoff(
    current_frame,
    accepted_public_projection,
    out handoff,
    out error)
```

The future I6D boundary producer receives this opaque value as one trusted
capability argument. `FlatPromptProjectionResultV1` is not extended with
private data, and callers cannot construct the capability or a second binding
list independently.

The only permitted operation across that seam is conceptually:

```text
public opaque TryGetValidatedTarget(
    prompt_instance_ordinal,
    continuation_step,
    frame_instance_ordinal,
    accepted_public_projection_id,
    i4_local_candidate_key,
    source_section,
    source_ordinal,
    current_accepted_frame,
    out accepted_i6c5_target_locator,
    out error)
```

The operation returns only the already accepted safe target or a structured
failure. It does not return the source occurrence, mirror snapshot, mirror
ID, raw address, prompt CardCode, or sidecar storage. The Model bridge must
not call any other Gameplay-internal member. If this opaque capability cannot
be made non-forgeable with an internal-only constructor and safe-target-only
operation, the implementation must stop and request a separate assembly
design rather than adding a friend assembly.

The public `OcgForgePublicCandidateBridgeV1.TryCreate(acceptedDecision)`
interface remains unchanged. Its accepted decision boundary stores the
complete binding set internally; the bridge uses the I4 exact-token path or
the facade's validated target, never an independently supplied mapping list.

### I4-to-I6D flow after the amendment

```text
same committed mirror/frame
    -> I3D creates unchanged public snapshot and private occurrence sidecar
    -> I4 parses complete prompt occurrences
    -> exact sidecar lookup yields existing I4 public ordinal
    -> I4 emits the complete public candidate domain plus internal handoff
    -> I6D combines the same source occurrence with the same-frame I6C5 target
    -> public OCGForge descriptor/key sees only its accepted safe target
```

The following future production requirements remain requirements rather than
current capabilities:

```text
I4_DUPLICATE_OWN_HAND_COMPLETE_DOMAIN = REQUIRED
SIDECAR_MISSING_REJECT                = REQUIRED
SIDECAR_AMBIGUOUS_REJECT              = REQUIRED
SIDECAR_STALE_REJECT                   = REQUIRED
SIDECAR_COLLISION_REJECT               = REQUIRED
I6D_BINDING_IMPLEMENTATION             = NOT_IMPLEMENTED
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

REAL_DUPLICATE_OWN_HAND_I4_FAILURE=CHARACTERIZED
I4_FAILURE_STAGE=TryCorrelatePile/CompleteCorrelation
I6D_BOUNDARY_REACHED=NO
I4_AUTHORITATIVE_CONTRACT_RECONCILED=YES_BY_EXPLICIT_VERSION_TRANSITION
I4_V1_IN_PLACE_AMENDMENT=FORBIDDEN
I4_PRIVATE_SIDECAR_CONTRACT=ocgforge-ignis.flat-prompt-correlation-sidecar.v1
I4_PRIVATE_SIDECAR_AMENDMENT=DESIGNED_PENDING_IMPLEMENTATION
OPTION_A_PRIVATE_OCCURRENCE_TO_I4_LOCATOR=SELECTED
PRIVATE_OCCURRENCE_ORDINAL_RULE=EXACTLY_DEFINED
DUPLICATE_ASSIGNMENT_INSERTION_ORDER_INDEPENDENT=CHARACTERIZED
ACTUAL_DEPENDENCY_DIRECTION=MODEL_TO_GAMEPLAY
BROAD_INTERNALS_VISIBLE_TO=NO
MODEL_CAN_READ_PRIVATE_GAMEPLAY_INTERNALS=NO
SAFE_TARGET_ONLY_HANDOFF=DESIGNED

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
DESIGNED_FRAME_LOCAL_BINDING=internal immutable set behind opaque safe-target capability
DESIGNED_ASSEMBLY_HANDOFF=public opaque capability; no Model friend assembly
PUBLIC_IDENTITY_IMPLICATIONS=private occurrence data must stay outside public identity
REPLAY_DETERMINISM_IMPLICATIONS=bind only current accepted frame/prompt, reject stale or ambiguous mappings

I6D_CROSS_LOCATOR_MAPPING_IMPLEMENTATION=NOT_IMPLEMENTED
I6D_PRIVATE_SOURCE_OCCURRENCE_BINDING_DESIGN=ACCEPTED_DESIGN_FREEZE
I6D_PRIVATE_SOURCE_OCCURRENCE_BINDING_DESIGN_FINAL=YES
I6D_PRIVATE_SOURCE_OCCURRENCE_BINDING_REMEDIATION_02=ACCEPTED_DESIGN_FREEZE
I4_PRIVATE_SIDECAR_CONTRACT_ACCEPTED=YES_FOR_DESIGN_ONLY
I4_PRIVATE_SIDECAR_IMPLEMENTATION=YES_PROJECTION_ONLY
I4_PRIVATE_SIDECAR_CORRELATION_ENABLEMENT=NOT_IMPLEMENTED
I6G_COUNTER_CAPTURE_RETRY=NOT_AUTHORIZED
I6G_LINK_CAPTURE=NOT_AUTHORIZED
I6G_FRESH_PROCESS_A_B=NOT_AUTHORIZED
I6G_FINAL=NO
I6_FINAL=NO
I7_AUTHORIZED=NO
```
