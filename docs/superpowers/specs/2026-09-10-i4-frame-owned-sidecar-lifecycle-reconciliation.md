# I4 Frame-Owned Sidecar Lifecycle Reconciliation

Status: `DESIGN_ONLY`; characterization accepted pending independent review
Date: 2026-09-10
Base: `65d059e5b7dc618704b02ac16389baf682bbe145`

This document characterizes the missing frame-lifecycle authority exposed by
the I4 private occurrence sidecar implementation. It does not change
production code, the public-state contract, I5, I6D, or I6G runtime
acceptance.

## Current characterization

The current modules have these responsibilities:

```text
GameplayMirrorSessionV1
    owns and mutates the current Mirror
    owns no FrameInstanceOrdinal

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

The existing frame authority is therefore:

```text
EXISTING_FRAME_AUTHORITY=ABSENT
GAMEPLAY_MIRROR_SESSION_OWNS_CURRENT_MIRROR=YES
GAMEPLAY_MIRROR_SESSION_OWNS_FRAME_ORDINAL=NO
FLAT_PROMPT_SESSION_OWNS_FRAME_ORDINAL=NO
PERSPECTIVE_SAFE_FRAME_OWNS_FRAME_ORDINAL=NO
PROJECTION_ORDINAL_IS_CALLER_SUPPLIED=YES
CURRENT_FRAME_INSTANCE_MATCH=NOT_PROVEN
STALE_FRAME_REJECTION=NOT_PROVEN
```

The executable characterization also demonstrates the current weakness: a
sidecar with the same public projection and entries but a different ordinal
is accepted when the consumer reads that ordinal back and uses it for its own
re-projection. This proves that projection identity and same-mirror
re-projection are not an independent current-frame check.

## Minimal future private frame authority

The next implementation should introduce one narrow, internal Gameplay seam;
the following is a design contract, not an implemented type:

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

The owner creates ordinal `0` when the initial `MSG_START` mirror state is
committed. Each successful state-message `Mirror.Apply` commit creates the
next checked ordinal exactly once. Presentation-only packets, failed applies,
projection reads, prompt acceptance, response encoding, and response writes
do not create a new frame. Overflow or any failed boundary rejects the
operation and does not reuse an ordinal.

The frame authority is immutable and is valid only until the next successful
mirror commit, a failed gameplay/projection boundary, or session disposal.
It is never derived from wall time, PID, process order, object identity,
allocation order, TCP chunking, dictionary iteration, or a random UUID.

## Required consumer seam

The frame owner must pass the current frame authority independently of the
sidecar. A future frame-owned prompt entry point should conceptually be:

```text
FlatPromptSessionV1.TryAcceptPrompt(
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

The first comparison is the missing independent check. The sidecar must never
be allowed to supply the expected current ordinal. If a caller provides only a
mirror, public projection, or sidecar without the current frame authority,
the duplicate-occurrence path fails closed.

The frame owner and prompt session remain in Gameplay. No broad
`InternalsVisibleTo("OCGForge.Ignis.Model")` is introduced. Any later I6D
handoff remains a separate opaque safe-target capability and cannot expose the
frame authority, MirrorSnapshot, MirrorEntityId, ModernLocInfo, or private
occurrence fields.

## Lifecycle acceptance matrix

The future implementation must prove:

```text
INITIAL_MSG_START_COMMIT_ORDINAL=0
SUCCESSFUL_STATE_COMMIT=NEXT_ORDINAL_EXACTLY_ONCE
PRESENTATION_PACKET_DOES_NOT_ADVANCE=PASS
FAILED_APPLY_DOES_NOT_ADVANCE=PASS
PROJECTION_READ_DOES_NOT_ADVANCE=PASS
PROMPT_ACCEPTANCE_DOES_NOT_ADVANCE=PASS
NEXT_FRAME_INVALIDATES_PREVIOUS=PASS
SESSION_DISPOSAL_INVALIDATES_CURRENT=PASS

STALE_FRAME_ORDINAL=FAIL_CLOSED
FUTURE_FRAME_ORDINAL=FAIL_CLOSED
SAME_PUBLIC_BYTES_DIFFERENT_FRAME=FAIL_CLOSED
SIDECAR_CANNOT_SELF_AUTHENTICATE_FRAME=PASS
```

No private frame coordinate enters public observation, public action bytes,
public action keys, candidate-domain digests, logical/encoded model input, or
Task7 materialization.

## I5 isolation finding

The current implementation commit also passes the sidecar through I5 prompt
projection paths. That behavior is outside this authorization and must be
removed by the next implementation remediation:

```text
I4_SIDECAR_ENABLEMENT=AUTHORIZED
I5_SIDECAR_ENABLEMENT=OUT_OF_SCOPE
I5_BEHAVIOR_MUST_BE_RESTORED_TO=65ccf707c802f42a942423e4e6fd0939fd6d342a
I5_PUBLIC_DOMAIN_BYTES_BEFORE_AFTER=EXACT_REQUIRED
I5_SIDE_CAR_ENABLEMENT=NO_REQUIRED
```

The I4 sidecar correlation path may remain available internally, but only I4
call sites may supply it until a separate authorization changes the I5
contract. I5 must not gain duplicate-pile acceptance, sidecar-dependent
locators, or changed continuation semantics from this design.

## Current non-effects and next gate

```text
PRODUCTION_CODE_CHANGED=NO
I4_PUBLIC_CONTRACT_V1=UNCHANGED
PUBLICSTATE_BYTES=UNCHANGED
PUBLICSTATE_IDENTITY=UNCHANGED
I5_IMPLEMENTATION_REMEDIATION=NOT_DONE
I6D_CHANGED=NO
I6G_CAPTURE=NOT_RUN
FRESH_PROCESS_A_B=NOT_RUN
```

The next production implementation is not authorized by this document. It
must first receive the independent frame authority described above, then
restore I5 behavior and re-prove duplicate I4 correlation without permitting
the sidecar to authenticate its own frame coordinate.
