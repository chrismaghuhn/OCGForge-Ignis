# I4 Frame-Owned Sidecar Lifecycle Reconciliation

Status: `IMPLEMENTATION_PENDING_INDEPENDENT_REVIEW`
Date: 2026-09-10
Base: `65d059e5b7dc618704b02ac16389baf682bbe145`

This document records the frame-lifecycle authority added for the I4 private
occurrence sidecar. It does not change the public-state contract, I5's
contract, I6D, or I6G runtime acceptance.

## Pre-implementation characterization

Before this implementation, the modules had these responsibilities:

```text
GameplayMirrorSessionV1
    owned and mutated the current Mirror
    owned no FrameInstanceOrdinal

FlatPromptSessionV1
    owns prompt ordinals and prompt bindings
    owns no current frame coordinate

PerspectiveSafeFrameV1
    carries the public-safe frame values
    carries no FrameInstanceOrdinal

PublicStateProjectionV1
    has a frame-owned overload accepting a caller-supplied ulong
    does not own or validate the current frame coordinate

PrivateI4OccurrencePublicLocatorSidecarV1
    stores FrameInstanceOrdinal from its caller
```

The pre-implementation characterization was:

```text
EXISTING_FRAME_AUTHORITY=ABSENT_AT_BASE_65d059e
GAMEPLAY_MIRROR_SESSION_OWNS_CURRENT_MIRROR=YES
GAMEPLAY_MIRROR_SESSION_OWNS_FRAME_ORDINAL=NO
FLAT_PROMPT_SESSION_OWNS_FRAME_ORDINAL=NO
PERSPECTIVE_SAFE_FRAME_OWNS_FRAME_ORDINAL=NO
PROJECTION_ORDINAL_IS_CALLER_SUPPLIED=YES
CURRENT_FRAME_INSTANCE_MATCH=NOT_PROVEN
STALE_FRAME_REJECTION=NOT_PROVEN
```

The executable characterization at the base demonstrated the weakness: a
sidecar with the same public projection and entries but a different ordinal
was accepted when the consumer read that ordinal back and used it for its own
re-projection. The implementation now rejects that input unless an
independently session-owned current authority matches it.

## Implemented private frame authority

The implementation introduces one narrow, internal Gameplay seam:

```text
PrivateGameplayFrameAuthorityV1

owner:
    GameplayMirrorSessionV1

coordinate:
    ulong FrameInstanceOrdinal

paired private state:
    the immutable MirrorSnapshotV1 committed for that ordinal

public exposure:
    none
serialization:
    none
```

`PerspectiveStateMirrorV1.TryCreate(MSG_START)` consumes the initial start
message and returns an already initialized mirror. `GameplayMirrorSessionV1`
then binds that existing mirror at construction and establishes private frame
ordinal `0`. Each successful subsequent state-message `Mirror.Apply` commit
creates the next checked ordinal exactly once. Presentation-only packets,
failed applies, projection reads, prompt acceptance, response encoding, and
response writes do not create a new frame. Overflow or any failed boundary
rejects the operation and does not reuse an ordinal.

The frame authority is immutable and is valid only until the next successful
mirror commit, a failed gameplay/projection boundary, or session disposal; the
previous in-memory authority token is explicitly invalidated at each such
boundary. It is never derived from wall time, PID, process order, object identity,
allocation order, TCP chunking, dictionary iteration, or a random UUID.

The authority also owns a private non-async lifecycle lease. A frame-owned
prompt holds that lease through projection validation and prompt binding, while
the session acquires the same lease before applying a state message and
replacing the current frame. This makes the validation-to-commit sequence
atomic with respect to a concurrent mirror advance; a second current check is
not used as a substitute for the transaction.

## Implemented consumer seam

The frame owner must pass the current frame authority independently of the
sidecar. The frame-owned prompt entry point is:

```text
FlatPromptSessionV1.TryAcceptFrameOwnedPrompt(
    complete_inner_game_message,
    current_frame_authority,
    accepted_public_projection)
```

The consumer must require all of the following before using a sidecar:

```text
current_frame_authority.FrameInstanceOrdinal
    == sidecar.FrameInstanceOrdinal

current_frame_authority.MirrorSnapshot
    == the snapshot used to create the accepted projection

accepted projection canonical bytes / SHA / PublicProjectionId
    == a re-projection of that current frame

recomputed sidecar
    == the accepted frame-owned sidecar
```

The first comparison is the independent current-frame check. The sidecar is
never allowed to supply the expected current ordinal. The legacy prompt entry
point rejects a sidecar projection without the current authority, while the
frame-owned entry point rejects stale or future ordinals.

The frame owner and prompt session remain in Gameplay. No broad
`InternalsVisibleTo("OCGForge.Ignis.Model")` is introduced. Any later I6D
handoff remains a separate opaque safe-target capability and cannot expose the
frame authority, MirrorSnapshot, MirrorEntityId, ModernLocInfo, or private
occurrence fields.

## Lifecycle acceptance matrix

The implementation must prove:

```text
INITIALIZED_MIRROR_BOUND_AT_SESSION_CONSTRUCTION=FRAME_0
MIRROR_APPLY_START_AFTER_CREATION=DuplicatePerspective
SUCCESSFUL_STATE_COMMIT=NEXT_ORDINAL_EXACTLY_ONCE
PRESENTATION_PACKET_DOES_NOT_ADVANCE=PASS
FAILED_APPLY_DOES_NOT_ADVANCE=PASS
PROJECTION_READ_DOES_NOT_ADVANCE=PASS
PROMPT_ACCEPTANCE_DOES_NOT_ADVANCE=PASS
NEXT_FRAME_INVALIDATES_PREVIOUS=PASS
SESSION_DISPOSAL_INVALIDATES_CURRENT=PASS

STALE_FRAME_ORDINAL=PASS
FUTURE_FRAME_ORDINAL=PASS
SAME_PUBLIC_BYTES_DIFFERENT_FRAME=PASS
SIDECAR_CANNOT_SELF_AUTHENTICATE_FRAME=PASS
```

No private frame coordinate enters public observation, public action bytes,
public action keys, candidate-domain digests, logical/encoded model input, or
Task7 materialization.

## I5 isolation finding

The prior implementation commit passed the sidecar through I5 prompt
projection paths. That out-of-scope behavior is removed by this implementation:

```text
I4_SIDECAR_ENABLEMENT=AUTHORIZED
I5_SIDECAR_ENABLEMENT=NO
I5_BEHAVIOR_RESTORED_TO=65ccf707c802f42a942423e4e6fd0939fd6d342a
I5_PUBLIC_DOMAIN_BYTES_BEFORE_AFTER=EXACT
I5_SIDE_CAR_ENABLEMENT=NO
```

The I4 sidecar correlation path may remain available internally, but only I4
call sites may supply it until a separate authorization changes the I5
contract. I5 must not gain duplicate-pile acceptance, sidecar-dependent
locators, or changed continuation semantics from this design.

An accepted frame-owned I4 binding retains the private frame-lifetime
authority used to create it. `TryCaptureSelection`, `TryResolveSelection`,
and `TryApplySelection` operate under that same authority lease through handle,
response, or continuation creation. I5 bindings retain no frame authority and
remain on the restored baseline path.

## Current non-effects and next gate

```text
PRODUCTION_CODE_CHANGED=YES
I4_PUBLIC_CONTRACT_V1=UNCHANGED
PUBLICSTATE_BYTES=UNCHANGED
PUBLICSTATE_IDENTITY=UNCHANGED
I4_FRAME_AUTHORITY_IMPLEMENTATION=IMPLEMENTED_PENDING_INDEPENDENT_REVIEW
I4_MIRROR_CLAIM_IMPLEMENTATION=IMPLEMENTED_PENDING_INDEPENDENT_REVIEW
I4_FRAME_BOUND_BINDING_LIFETIME=IMPLEMENTED_PENDING_INDEPENDENT_REVIEW
I5_SCOPE_RESTORATION=IMPLEMENTED_PENDING_INDEPENDENT_REVIEW
I6D_CHANGED=NO
I6G_CAPTURE=NOT_RUN
FRESH_PROCESS_A_B=NOT_RUN
```

The production implementation is present pending independent review. It must
be independently reviewed before Counter/Link capture or fresh-process A/B;
those later gates remain outside this slice.
