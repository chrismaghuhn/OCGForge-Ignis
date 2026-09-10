# I6D Frame-Owned Cross-Locator Integration Reconciliation

Status: design/characterization-only; no I6D mapping implementation is
authorized by this document.
Base: `6ffc68dbdd5c2619daffbb4d0d9fa35ede5f3c71`
Task: `I6D_FRAME_OWNED_CROSS_LOCATOR_INTEGRATION_RECONCILIATION_01`

This document synchronizes the I6D cross-locator design with the accepted I4
frame lifecycle. It does not change I4, I6C2, I6C3, I6C5, the public-state
contract, the OCGForge model contract, or the public decision-boundary API.

## Accepted lifecycle authority

The private frame coordinate is owned by `GameplayMirrorSessionV1`:

```text
PerspectiveStateMirrorV1.TryCreate(MSG_START)
    -> initialized mutable mirror

GameplayMirrorSessionV1 construction/bind
    -> FRAME_0

successful owner-capability Mirror.Apply
    -> invalidate FRAME_N
    -> FRAME_N+1 with the committed snapshot
```

`TryCreateI6C5Frame()` consumes the current frame. It does not create or
advance a frame. Presentation packets, `STOC_TIME_LIMIT`, projection reads,
prompt acceptance, response encoding, and response writes do not advance the
coordinate. A failed apply, failed frame-owned projection boundary, or session
disposal invalidates the current authority without advancing its ordinal.

```text
FRAME_ORDINAL_CREATION = session bind / successful owner Apply
FRAME_ORDINAL_CREATION != I6C5 projection acceptance
```

The private coordinate and its lifecycle capability remain outside public
observation, action identity, candidate-domain identity, model input, and
replay identity.

## Current same-snapshot I6C3 finding

`PerspectiveSafePublicFrameSourceV1.TryCreateI6C3()` currently creates the
following private builder-local relationship while composing entities,
relationships, and chain values:

```text
MirrorEntityIdV1 -> accepted PublicSemanticLocatorV1
```

The same builder keeps the mirror-card lookup needed to resolve relationship
endpoints. The locator map is used during that single source composition, then
disappears before the public `PerspectiveSafeI6C3StateSourceV1`/
`PerspectiveSafeFrameV1` result is returned. The accepted I6C3/I6C5 source
exposes safe entity locators, but no `MirrorEntityIdV1` map or private
occurrence table.

The current boundary finding is:

```text
I6C3_TRANSIENT_OCCURRENCE_TO_LOCATOR_MAP = PRESENT_IN_BUILDER
I6C3_MAP_RETAINED_BY_PUBLIC_SOURCE       = NO
I6C5_MAP_RETAINED_BY_PUBLIC_FRAME        = NO
I6D_PRIVATE_HANDOFF                       = ABSENT
```

The absence is intentional at the public source boundary. It is not
permission to reconstruct a target later from CardCode, sequence, collection
order, or a second locator factory.

## Required frame-owned target proof

The future implementation must consume the builder-local map before it is
discarded and produce only the already accepted safe target in the private I6D
binding. The exact private join is:

```text
current frame-owned I4 source occurrence
    -> exactly one current mirror occurrence (private join)
    -> its MirrorEntityIdV1 (private, transient)
    -> locatorById[MirrorEntityIdV1]
    -> exact accepted I6C5/OCGForge locator
```

`MirrorEntityIdV1` is never stored in the I6D binding or exposed across the
assembly boundary. The binding stores the source-occurrence proof fields and
the resulting safe target locator described by the accepted sidecar design.
The target must occur exactly once in the same accepted I6C5 frame. Missing,
ambiguous, stale, or colliding results reject the whole decision boundary.

The exact-token path remains a fast path and may omit the private handoff. A
non-equal I4/I6C5 locator form requires the private proof; it may not be
accepted through a public-attribute-only match.

## One owner-guarded composition boundary

The single I6D same-snapshot composition owner is
`GameplayMirrorSessionV1`. It owns the current frame authority,
`boundMatchContext`, `boundPrintedProvider`, and the I6C5 composition path.
`FlatPromptSessionV1` remains the owner of the prompt/binding lifetime only;
it is not the I6C5 composition owner.

```text
I6D_COMPOSITION_OWNER=GameplayMirrorSessionV1
FLATPROMPT_SESSION_ROLE=PROMPT_LIFETIME_AUTHORITY_OWNER_ONLY
```

The future Gameplay-side producer is one owner-guarded operation over the
current `PrivateGameplayFrameAuthorityV1` lease:

```text
acquire current FRAME_N lease
    -> read current immutable authority snapshot
    -> compose I6C3/I6C5 public source and retain builder-local locatorById
    -> consume the accepted frame-owned I4 sidecar/prompt occurrences
    -> create and validate the complete private target-binding set
    -> create one opaque I6D handoff
    -> release lease
```

The source builder may use an internal companion result or callback to make
the builder-local map available to this operation. It must not make the map a
field of `PerspectiveSafeI6C3StateSourceV1`, `PerspectiveSafeFrameV1`, or a
public observation type. The operation must use the immutable snapshot from
the current frame authority, not the mutable mirror through an unbound caller
reference.

The complete operation binds these values:

```text
current FrameInstanceOrdinal
accepted I4 PublicProjectionId
accepted frame-owned I4 prompt/binding
transient exact I6C3 occurrence-to-locator map
accepted I6C5 public frame
```

No detached sidecar list or caller-supplied map may be attached after the
boundary has been accepted.

The prompt-side authority is a separate private revocable capability owned by
`FlatPromptSessionV1` and its current frame-bound binding. It is created only
for the accepted frame-owned I4 binding and is invalidated when the current
prompt binding is replaced, its continuation step changes, terminal selection
consumes it, or the boundary fails. The frame and prompt authorities are
acquired in this fixed order:

```text
FRAME lifetime lease
    -> PROMPT/binding lifetime lease
    -> validate and consume
    -> release PROMPT/binding lease
    -> release FRAME lease
```

No implementation may acquire them in the reverse order. A handoff with a
matching integer coordinate but an unavailable stored authority is stale and
must fail closed.

## Controlled Gameplay-to-Model handoff

The project dependency remains:

```text
OCGForge.Ignis.Model -> OCGForge.Ignis.Gameplay
```

The future crossing is one public opaque value created by Gameplay:

```text
public opaque I6DPrivateCrossLocatorBindingHandoffV1
    internal constructor
    no public fields or private-occurrence properties
    no serialization
    no MirrorSnapshot, MirrorEntityIdV1, or raw loc_info exposure
```

The opaque handoff has two public operations, both safe-only.

First, boundary acceptance obtains an opaque acceptance lease:

```text
TryAcquireBoundaryAcceptanceLease(
    accepted_public_frame,
    accepted_public_projection,
    out I6DBoundaryAcceptanceLeaseV1 lease,
    out structured_error)
```

`I6DBoundaryAcceptanceLeaseV1` has no public data or lifecycle coordinates.
While it is held, it keeps the stored FRAME and PROMPT lifetime authorities
acquired. The Model producer must hold it across accepted boundary
construction and `nextDecisionIndex` consumption, then release it. Second,
target retrieval after a boundary exists is:

```text
TryGetValidatedTarget(
    accepted_public_candidate,
    current_accepted_public_frame,
    out accepted_i6c5_target_locator,
    out structured_error)
```

The capability privately retains the exact revocable
`PrivateGameplayFrameAuthorityV1` (or an explicitly derived equivalent) and
a separate revocable prompt/binding lifetime capability. Both operations
acquire those authorities in the fixed order FRAME, then PROMPT. The
acceptance operation validates the complete public frame/projection and
binding set before returning its lease. Target retrieval validates the
accepted public candidate/frame while both leases are held and returns no
target if either authority is stale. `FrameInstanceOrdinal`,
`PromptInstanceOrdinal`, and `ContinuationStep` are private
diagnostic/cross-check values only; they are not caller-supplied proof. The
operations return no source occurrence, CardCode, sequence, MirrorEntityId,
raw address, or sidecar storage. The handoff is invalid after frame
replacement, prompt/continuation mismatch, terminal selection, session
disposal, or boundary failure.

The existing `OcgForgeAcceptedDecisionBoundaryV1` remains the Model-side
owner of the accepted decision. A later implementation may add one narrowly
bound producer overload that accepts this one opaque capability and stores it
privately in the boundary:

```text
Gameplay frame-owned composition
    -> public PerspectiveSafeFrameV1
    -> accepted FlatPromptProjectionResultV1
    -> one opaque I6D handoff
    -> OcgForgeAcceptedDecisionBoundaryV1 private capability member
    -> existing OcgForgePublicCandidateBridgeV1.TryCreate(acceptedDecision)
```

The producer validates the opaque handoff atomically against the supplied
accepted public frame and projection before constructing the accepted
decision boundary. Thus `FRAME_A + PROJECTION_A + HANDOFF_B` and stale prompt
or continuation combinations are rejected before a boundary exists. The
existing producer overload without the handoff remains unchanged. The public
bridge constructs descriptors only from OCGForge-safe fields; only the
validated target locator may feed the existing reference mapping. There is no
public lifecycle-coordinate argument, public binding-list argument, or broad
`InternalsVisibleTo("OCGForge.Ignis.Model")`.

The producer-side linearization is one guarded transaction:

```text
OcgForgeAcceptedDecisionBoundaryProducerV1 acceptanceGate
    -> handoff.TryAcquireBoundaryAcceptanceLease(public frame, projection)
    -> using acceptance lease:
           construct OcgForgeAcceptedDecisionBoundaryV1
           increment nextDecisionIndex exactly once
    -> release PROMPT lease
    -> release FRAME lease
```

If lease acquisition or validation fails, the boundary remains null and
`nextDecisionIndex` is unchanged. Validation may not complete, release both
authorities, and then construct the accepted boundary in a separate operation.

The acceptance rule is therefore:

```text
MISMATCHED_FRAME_HANDOFF       -> reject before accepted boundary
MISMATCHED_PROJECTION_HANDOFF  -> reject before accepted boundary
STALE_FRAME_HANDOFF            -> reject
STALE_PROMPT_HANDOFF           -> reject
STALE_CONTINUATION_HANDOFF     -> reject
DETACHED_BINDING_LIST          -> not accepted
CALLER_PRIVATE_MAPPING         -> not accepted
```

## Frozen non-alias and privacy rules

```text
I4 locator string == I6C5 locator string       -> not assumed
CardCode search                                 -> forbidden
first/collection-order match                    -> forbidden
I4 ordinal arithmetic to make I6C5 target       -> forbidden
source sequence copied into public locator      -> forbidden
MirrorEntityIdV1 across boundary                -> forbidden
raw loc_info across boundary                    -> forbidden

private occurrence in public descriptor        -> NO
private occurrence in public action key        -> NO
private occurrence in domain digest            -> NO
private occurrence in model input               -> NO
private occurrence in replay identity           -> NO
```

The handoff's private lifetime authorities are mandatory:

```text
HANDOFF_STORES_FRAME_LIFETIME_AUTHORITY       = YES
HANDOFF_STORES_PROMPT_LIFETIME_AUTHORITY     = YES
FRAME_ORDINAL_CALLER_AUTHORITY                = NO
PROMPT_ORDINAL_CALLER_AUTHORITY               = NO
CONTINUATION_STEP_CALLER_AUTHORITY            = NO
PUBLIC_CONSUMER_NEEDS_PRIVATE_COORDINATES     = NO
BOUNDARY_VALIDATES_HANDOFF_BEFORE_CREATION    = YES
```

Paired-world equality is required whenever the accepted safe target and all
other public semantics are equal. If frozen OCGForge public semantics make
the safe target itself different, the public result may differ; private source
data still may not become an additional public identity field.

## Characterization evidence and deferred implementation

The focused design-only tests characterize the current seam:

```text
FRAME_ORDINAL_IS_SESSION_OWNED            = PASS
I6C5_PROJECTION_DOES_NOT_CREATE_FRAME     = PASS
I6C3_MAP_IS_TRANSIENT                      = PASS
PUBLIC_SOURCE_HAS_NO_PRIVATE_MAP           = PASS
CURRENT_I6D_HANDOFF_IS_ABSENT              = PASS
CURRENT_MODEL_BOUNDARY_HAS_NO_HANDOFF      = PASS
PUBLIC_CONSUMER_NEEDS_PRIVATE_COORDINATES  = PASS
BOUNDARY_ACCEPTANCE_GUARD_INTERFACE        = PASS
BOUNDARY_ACCEPTANCE_LEASE_LIFETIME         = PASS
FAILED_ACCEPTANCE_CONSUMES_DECISION_INDEX  = NO
PUBLICSTATE_BYTES_CHANGED                  = NO
PUBLICSTATE_IDENTITY_CHANGED               = NO
PRODUCTION_MAPPING_IMPLEMENTATION          = NO
```

The tests do not create an I6D capability, alter the Model boundary, rerun
Counter or Link capture, send a response, or authorize fresh-process A/B.

```text
I6D_FRAME_OWNED_CROSS_LOCATOR_RECONCILIATION = DESIGN_ONLY_PENDING_REVIEW
I6D_MAPPING_IMPLEMENTATION                   = NOT_IMPLEMENTED
I6G_COUNTER_CAPTURE                           = NOT_AUTHORIZED
I6G_LINK_CAPTURE                              = NOT_AUTHORIZED
I6G_FRESH_PROCESS_A_B                         = NOT_AUTHORIZED
I7_AUTHORIZED                                 = NO
```
