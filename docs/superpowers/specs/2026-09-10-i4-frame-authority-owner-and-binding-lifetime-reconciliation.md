# I4 Frame Authority Owner and Binding Lifetime Reconciliation

Status: `DESIGN_PENDING_INDEPENDENT_REVIEW`
Date: 2026-09-10
Base: `aa3d14e4170a503fe6712ed40ea48bbdf1084340`

This document characterizes two remaining lifecycle gaps after the frame-owned
sidecar atomicity implementation. It defines the next owning-layer decisions
without changing production code, I5, I6D, or I6G runtime acceptance.

## 1. Scope and characterization

The review scope is limited to:

```text
PerspectiveStateMirrorV1 mutation ownership after GameplayMirrorSessionV1 bind
FRAME_N prompt-binding lifetime after FRAME_N -> FRAME_N+1
deterministic concurrency-test design for the existing private frame lease
```

No public frame coordinate, private mirror identity, raw protocol address, or
model-facing field is introduced by this characterization.

## 2. Mutable-mirror inventory at the base

The source inventory at `aa3d14e4170a503fe6712ed40ea48bbdf1084340` is:

```text
PRODUCTION_PERSPECTIVE_STATE_MIRROR_APPLY_CALLERS:
    GameplayMirrorSessionV1.PumpAsync only

PRODUCTION_GAMEPLAY_MIRROR_GETTER_CONSUMERS:
    none outside GameplayMirrorSessionV1 itself

TEST_AND_FIXTURE_APPLY_CALLERS:
    direct mirror tests and accepted test fixtures

POST_BIND_EXTERNAL_APPLY:
    technically possible through the public Mirror getter and the constructor's
    original mirror reference

POST_BIND_EXTERNAL_APPLY_INTENDED_SUPPORTED_API:
    no production caller or accepted contract use is identified; the public
    surface nevertheless permits it today and therefore is not fail-closed

POST_BIND_EXTERNAL_APPLY_PROTECTED_BY_FRAME_AUTHORITY:
    NO
```

The current public `PerspectiveStateMirrorV1.Apply` remains callable after a
`GameplayMirrorSessionV1` is constructed. A base characterization therefore
performs a successful direct Apply through `session.Mirror` and observes that
the live mirror changes while the session-owned frame ordinal remains at its
previous value. This is an ownership escape, not accepted behavior.

The current production mutation inventory is narrow, but the mutable object is
not exclusive. A caller promise not to call `Apply` is not an authority
boundary and is rejected.

## 3. Selected post-bind ownership semantics

The implementation decision to carry forward is an exclusive mirror claim with
an owner-only mutation capability:

```text
standalone mirror before claim
    -> existing public Apply behavior remains available

GameplayMirrorSessionV1 construction
    -> atomically claims the exact mirror instance
    -> binds the already initialized snapshot as FRAME_0
    -> retains the private owner capability

after claim:
    GameplayMirrorSessionV1 owner capability
        -> the only successful Apply path

    public PerspectiveStateMirrorV1.Apply
        -> structured InvalidState failure
        -> no state mutation
        -> no frame-ordinal transition
```

The claim and public-Apply check use one mirror-owned synchronization seam. If
claim and an external Apply race, exactly one linearized pre-claim Apply may
finish before the claim; after the claim, the external Apply fails closed. The
session owner applies only with its private capability and advances the frame
authority in the same owner transaction.

The public `GameplayMirrorSessionV1.Mirror` getter may remain for existing read
compatibility only if the claimed mirror's public mutation path is guarded as
above. `Snapshot` remains an immutable read value. No caller promise, mutable
copy silently diverging from the supplied mirror, or broad
`InternalsVisibleTo` is an accepted substitute.

The later implementation must prove:

```text
MIRROR_CLAIM_IS_ATOMIC=REQUIRED
POST_BIND_PUBLIC_APPLY=FAIL_CLOSED
POST_BIND_PUBLIC_APPLY_MUTATES_STATE=NO
POST_BIND_PUBLIC_APPLY_ADVANCES_FRAME=NO
OWNER_CAPABILITY_APPLY=ONLY_SUCCESSFUL_MUTATION_PATH
SUCCESSFUL_OWNER_APPLY=EXACTLY_ONE_FRAME_ADVANCE
```

The current base status is intentionally recorded separately:

```text
MUTABLE_MIRROR_ESCAPE_AT_BASE=PROVEN
POST_BIND_MIRROR_MUTATION_POLICY=EXCLUSIVE_CLAIM_OWNER_CAPABILITY
FRAME_OWNER_CAN_BE_BYPASSED_AT_BASE=YES
FRAME_OWNER_CAN_BE_BYPASSED_AFTER_IMPLEMENTATION=NO
```

## 4. Frame-bound prompt lifetime

At the base, a successful frame-owned prompt stores a normal
`CurrentFlatPromptBindingV1` containing prompt and response data but no
frame-lifetime binding. The executable characterization is:

```text
session-owned FRAME_N authority
    -> accept frame-owned prompt
    -> capture an old selection handle
    -> successfully commit FRAME_N -> FRAME_N+1
    -> old TryCaptureSelection remains accepted
    -> old TryResolveSelection remains accepted
```

Therefore:

```text
STALE_FRAME_PROMPT_BINDING_CURRENTLY_ACCEPTED=YES
```

The required future implementation is narrow and I4-only:

```text
frame-owned I4 CommitProjection
    -> stores the private current-frame lifetime token in the binding

TryCaptureSelection / TryResolveSelection
    -> validate that token is still current
    -> on invalid token clear the binding
    -> return existing StalePromptBinding
```

I5 bindings remain frame-unbound and retain the exact behavior of the accepted
`65ccf707c802f42a942423e4e6fd0939fd6d342a` baseline. The private token is not
part of candidates, local keys, response bytes, public action identity, domain
digests, model input, or replay identity.

The later implementation must prove:

```text
FRAME_N_BINDING_ACCEPTED=PASS
FRAME_N_TO_FRAME_N+1_INVALIDATES_BINDING=PASS
STALE_CAPTURE_REJECTED=PASS
STALE_RESOLVE_REJECTED=PASS
FAILED_FRAME_BOUNDARY_INVALIDATES_BINDING=PASS
SESSION_DISPOSAL_INVALIDATES_BINDING=PASS
I5_BEHAVIOR_EXACTLY_BASELINE=PASS
```

## 5. Deterministic concurrency-test design

The concurrency test must not infer ordering from sleeps, wall time, scheduler
luck, or `Task.IsCompleted` polling. The accepted design is a barrier-driven
two-party test seam around the existing private frame lease:

```text
case A: prompt wins
    prompt transaction enters the FRAME_N lease
    PromptEntered barrier is released
    pump is allowed to start and reaches the same lease
    assert no mirror Apply / frame replacement can pass the lease
    release PromptTransaction
    prompt commits FRAME_N atomically
    pump then applies and creates FRAME_N+1
    old binding is stale and rejected

case B: pump wins
    pump enters the FRAME_N lease before Apply
    PumpEntered barrier is released
    prompt attempts the same FRAME_N transaction
    pump commits FRAME_N+1 and invalidates FRAME_N
    prompt acquisition fails closed
    no stale prompt binding or response is committed
```

The test coordinator uses explicit `TaskCompletionSource`/barrier events and a
bounded cancellation token only for cleanup. It records only ordinal/result
state and never logs private mirror state. The implementation seam must expose
no test-only production bypass; if deterministic entry/exit coordination cannot
be achieved at an approved internal seam, the test remains `UNPROVEN` rather
than becoming timing-based.

```text
DETERMINISTIC_CONCURRENCY_TEST_DESIGNED=YES
SLEEP_BASED_CONCURRENCY_ASSERTION=NO
RAW_PRIVATE_STATE_LOGGED=NO
```

## 6. Assembly and privacy constraints

The actual project direction remains:

```text
OCGForge.Ignis.Model -> OCGForge.Ignis.Gameplay
```

This design does not add a reverse reference or a broad friend assembly. Any
future I6D handoff remains an opaque safe-target capability. Mirror snapshots,
mirror entity IDs, `ModernLocInfo`, frame tokens, source sequences, and private
occurrence data do not cross into the public model-facing surface.

## 7. Acceptance status

```text
MUTABLE_MIRROR_ESCAPE_INVENTORY_COMPLETE=YES
POST_BIND_MIRROR_MUTATION_POLICY=EXACTLY_DEFINED
FRAME_OWNER_CAN_BE_BYPASSED=PROVEN_AT_BASE

STALE_BINDING_AFTER_FRAME_ADVANCE_CHARACTERIZED=YES
FRAME_BOUND_BINDING_LIFETIME=EXACTLY_DEFINED

NO_PUBLIC_FRAME_COORDINATE=YES
NO_MODEL_FRAME_COORDINATE=YES
NO_REPLAY_IDENTITY_CHANGE=YES
I5_SCOPE_RESTORATION_REMAINS_FINAL=YES

DETERMINISTIC_CONCURRENCY_TEST_DESIGNED=YES
NO_SLEEP_BASED_CONCURRENCY_ASSERTION=YES

PRODUCTION_CODE_CHANGED=NO
OWNERSHIP_IMPLEMENTATION=NOT_AUTHORIZED
BINDING_LIFETIME_IMPLEMENTATION=NOT_AUTHORIZED
COUNTER_CAPTURE=NOT_AUTHORIZED
LINK_CAPTURE=NOT_AUTHORIZED
FRESH_PROCESS_A_B=NOT_AUTHORIZED
```

This design must receive independent review before the selected ownership and
binding-lifetime implementation is authorized.
