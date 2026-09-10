# OCGForge-Ignis I4 Private Prompt Correlation Sidecar V1

Status: `ACCEPTED_IMPLEMENTATION`; projection, correlation, and frame-lifetime implementation accepted
Contract ID: `ocgforge-ignis.flat-prompt-correlation-sidecar.v1`
Parent public contract: `ocgforge-ignis.flat-prompt-projection.v1`
Date: 2026-09-10

```text
I4_PUBLIC_CONTRACT_V1=FROZEN
I4_PUBLICSTATE_BYTES=UNCHANGED
I4_PUBLICSTATE_IDENTITY=UNCHANGED
I4_AUTHORITATIVE_CONTRACT_RECONCILED=YES_BY_EXPLICIT_VERSION_TRANSITION
I4_V1_IN_PLACE_AMENDMENT=FORBIDDEN
I4_PRIVATE_CORRELATION_SIDECAR=IMPLEMENTED_AND_ACCEPTED
I4_PRIVATE_CORRELATION_SIDECAR_ACCEPTED=YES_IMPLEMENTED
I4_PRIVATE_CORRELATION_SIDECAR_IMPLEMENTED=YES_PROJECTION_AND_CORRELATION
I4_PRIVATE_CORRELATION_ENABLEMENT=IMPLEMENTED_AND_ACCEPTED
I4_FRAME_AUTHORITY=IMPLEMENTED_AND_ACCEPTED
I4_FRAME_BOUND_BINDING_LIFETIME=IMPLEMENTED_AND_ACCEPTED
I4_MIRROR_CLAIM=IMPLEMENTED_AND_ACCEPTED
I5_SIDECAR_ENABLEMENT=NO
I6B_BUNDLE_ENTRY=NO
```

This document defines the required version transition for the duplicate
own-Hand correlation problem. It must not be read as an in-place amendment to
`docs/contracts/flat-prompt-projection-v1.md`. That V1 public contract remains
the authority for public context, candidate descriptors, public locators,
canonical bytes, and public identity.

## 1. Purpose and narrow scope

The current V1 prompt path can resolve a current mirror card but cannot choose
one public ordinal when a known public Hand/Extra-Deck group contains multiple
cards with the same CardCode. The current fail-closed result is therefore
correct for the frozen V1 correlation rule, but it prevents a complete legal
domain from reaching I6D.

This companion contract authorizes only a private proof carrier for prompt
correlation. It does not add a public field, a public locator grammar, a
public-state row, an I6B contract entry, or a model input value.

The sidecar is limited to a current, known-public Hand/Extra-Deck occurrence
for which the same accepted public projection has already emitted the public
ordinal. Main Deck, hidden opponent Hand, unknown CardCode, and any source
occurrence without an exact current mirror resolution remain fail-closed.

The sidecar is built only through an internal frame-owned projection path that
receives the session's `FrameInstanceOrdinal`. The existing two-argument
public-state projection path remains unchanged and does not expose or consume
a sidecar. The separately authorized correlation implementation consumes the
sidecar only when the independently current session frame, its projection
identity, and same-mirror re-projection all match.
After acceptance, an I4 frame-bound prompt binding retains the same private
frame-lifetime authority; its capture, response, and continuation consumers
must acquire that authority lease before operating. I5 bindings remain
frame-unbound.

## 2. Sidecar record

The internal immutable sidecar container
`PrivateI4OccurrencePublicLocatorSidecarV1` and its entries have exactly
these fields:

```text
container:
  FrameInstanceOrdinal
  AcceptedPublicProjectionId

entry:
AbsoluteController
NormalizedZone
SourceSequence
IsOverlay
OverlayIndex                 # present exactly when IsOverlay=true
AcceptedI4PublicLocator
```

The two container fields apply uniformly to every entry and are not repeated
as public-state fields.

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

`MirrorEntityIdV1` may be used only as a transient same-snapshot join while
the sidecar is built. It is not a sidecar field, public proof, serialized
value, digest input, replay identity, or fallback. The sidecar contains no
prompt CardCode; existing I4 CardCode safety checks still apply independently.

## 3. Creation authority

The sidecar is created at the same I3D projection operation that assigns the
existing public pile ordinal. The builder retains a private association from
the exact current mirror occurrence to the public `PublicCardStateV1.Locator`
that it has already emitted. It never reconstructs the public locator later.

The public pile order remains the existing `KnownPileCard.Compare` order:

```text
absent position before present position
present numeric position ascending
```

For equal public sort keys, the private total tie-break is exactly:

```text
(position_presence_and_value,
 absolute_controller,
 normalized_zone,
 source_sequence,
 is_overlay,
 overlay_index)
```

The first element uses the existing null-before-known rule. Remaining values
use ordinal numeric comparison; `OverlayIndex` is present only for overlay
occurrences. An equal complete key is a collision and rejects the complete
projection/prompt. Insertion order, dictionary iteration, allocation order,
CardCode re-search, and first-match behavior are not permitted.

The private tie-break only chooses which exact source occurrence corresponds
to an already indistinguishable public ordinal. It is never added to
`PublicCardStateV1`, public canonical bytes, or public identity. Equal
same-code/same-position duplicate groups therefore have deterministic private
pairing while retaining byte-identical public projection output.

Sidecar creation is transactional. If any occurrence has no exact source
resolution, more than one resolution, an invalid overlay shape, a duplicate
full key, or no unique emitted public target, no sidecar or prompt candidate
domain is accepted.

## 4. I4 prompt-correlation use

When a prompt supplies an exact current occurrence, the
`FlatPromptCardCorrelationV1` pile path consumes the sidecar entry and returns
its existing accepted I4 public locator. It also preserves the existing
accepted-snapshot CardCode/provenance checks. The sidecar does not make a new
locator and does not make a hidden card public.

```text
exact sidecar key -> exactly one existing I4 public locator -> candidate
missing/ambiguous/stale/colliding sidecar -> whole prompt fails closed
```

Existing exact public-token cases remain unchanged. A sidecar is not created
for a hidden opponent-Hand card, and no CardCode-only or public-attribute-only
fallback is allowed for a duplicate group.

## 5. Controlled assembly handoff

The actual project dependency is `OCGForge.Ignis.Model ->
OCGForge.Ignis.Gameplay`. No reverse project reference and no broad
`InternalsVisibleTo("OCGForge.Ignis.Model")` is permitted.

Gameplay owns the private sidecar and the internal
`PrivateCrossLocatorBindingV1`. If the I6D target must cross the project
reference, it crosses only as the opaque
`I6DPrivateCrossLocatorBindingHandoffV1` capability. The capability has:

```text
no public constructor
no public fields or occurrence properties
no serialization
no MirrorSnapshot / MirrorEntityId / ModernLocInfo exposure
no private source-data operation
```

`GameplayMirrorSessionV1` is the single owner of complete same-snapshot I6D
composition. It owns the current frame authority, bound I6C5 runtime inputs,
the transient I6C3 `locatorById` map during composition, and construction of
the complete private binding set. `FlatPromptSessionV1` owns only the
revocable prompt/binding lifetime authority and supplies the accepted
frame-owned prompt binding; it is not the I6C5 composition owner.

The capability is created by the current Gameplay session after the sidecar
and complete frame-owned prompt have been accepted:

```text
GameplayMirrorSessionV1.TryCreateFrameOwnedI6DPrivateBindingHandoff(
    accepted_frame_owned_prompt,
    accepted_public_projection,
    out handoff,
    out error)
```

That operation holds the current FRAME lease, obtains the PROMPT/binding lease,
composes I6C3/I6C5 from the immutable FRAME snapshot while the transient map
is available, joins exact I4 occurrences, validates the complete binding set,
and only then creates the opaque handoff. The transient map is never detached
or returned to the Model assembly.

The future I6D boundary producer receives that opaque value as a single
trusted capability argument. `FlatPromptProjectionResultV1` is not extended
with private data, and callers cannot construct the capability or a second
binding list independently.

The opaque handoff exposes two safe-only operations. First, boundary creation
obtains a separate opaque acceptance lease:

```text
TryAcquireBoundaryAcceptanceLease(
    accepted_public_frame,
    accepted_public_projection,
    out I6DBoundaryAcceptanceLeaseV1 lease,
    out structured_error)
```

The Model producer holds that lease through construction of
`OcgForgeAcceptedDecisionBoundaryV1` and the single `nextDecisionIndex`
increment. It releases PROMPT and then FRAME in reverse order. Failure to
acquire or validate leaves the boundary null and does not consume the
decision index.

The producer's acceptance gate surrounds the lease acquisition, boundary
construction, and decision-index increment as one linearizable transaction.
It never validates the handoff, releases both authorities, and then creates an
accepted boundary in a separate operation.

Second, target retrieval after a boundary exists is:

```text
TryGetValidatedTarget(
    accepted_public_candidate,
    current_accepted_public_frame,
    out accepted_i6c5_target_locator,
    out structured_error)
```

The operation returns only the safe target locator or a structured failure.
All lifecycle coordinates remain private inside the capability: it retains the
exact revocable `PrivateGameplayFrameAuthorityV1` (or an equivalent derived
revocation capability) and a revocable current-binding lifetime capability.
`FrameInstanceOrdinal`, `PromptInstanceOrdinal`, and `ContinuationStep` are
private diagnostics/cross-checks, never caller authority. The capability
acquires the stored frame lease and then the prompt-binding lease, validates
the public candidate/frame while both are held, releases them in reverse
order, and returns no target after either lease is stale. The prompt-binding
lease is owned by `FlatPromptSessionV1`/the current frame-bound binding and is
revoked on prompt replacement, continuation transition, terminal selection, or
boundary failure.

The public `OcgForgePublicCandidateBridgeV1.TryCreate(acceptedDecision)` entry
remains unchanged as the final consumer and receives no detached mapping list.

## 6. Lifecycle and rejection

The sidecar and opaque capability are in-memory, immutable, and scoped to one
accepted frame and prompt instance. The opaque capability privately retains
both revocable lifetime authorities. It is discarded or becomes unusable on
prompt replacement, continuation-step transition, frame replacement,
terminal selection, session disposal, or any boundary failure. A new
continuation step requires a new sidecar/binding set from the new accepted
frame.

The complete candidate boundary rejects on:

```text
missing sidecar entry
ambiguous sidecar entry
stale frame/projection/prompt/continuation coordinates
stored frame lifetime unavailable
stored prompt lifetime unavailable
source section or source ordinal mismatch
duplicate lookup key
duplicate source-occurrence key
two distinct source occurrences mapping to one target locator
target missing or non-unique in the current I6C5 frame
```

No candidate is dropped, substituted, sorted by policy, or repaired after a
sidecar failure.

Boundary creation is atomic with respect to the opaque handoff. The future
Model producer receives only the accepted public frame, accepted public
projection, and one opaque handoff. Before constructing
`OcgForgeAcceptedDecisionBoundaryV1`, it asks the handoff to validate those
public values against its privately retained frame/prompt authorities. A
mismatched frame, projection, prompt, continuation, or binding set therefore
rejects before an accepted boundary exists. The producer does not accept a
public coordinate argument as proof of currentness.

## 7. Explicit transition and non-effects

The required transition is:

```text
frozen flat-prompt-projection.v1
    + explicit private flat-prompt-correlation-sidecar.v1
    -> authorized I4 correlation implementation
```

The correlation implementation is present and accepted as part of the final
I4 frame-owned sidecar path.
Duplicate same-code own-Hand prompts use the current frame-owned sidecar;
missing or detached sidecars still fail closed. This transition changes
neither public-state canonical bytes nor public-state identity:

```text
NEW_GAMEPLAY_SEMANTICS=NO
NEW_LEGALITY_SEMANTICS=NO
NEW_OBSERVATION_SEMANTICS=NO
NEW_MODEL_SEMANTICS=NO
PUBLIC_CANDIDATE_SCHEMA_CHANGED=NO
PUBLIC_CANDIDATE_IDENTITY_SEMANTICS_CHANGED=NO
I4_CORRELATION_CAPABILITY_CHANGED=YES
PUBLICSTATE_BYTES_CHANGED=NO
PUBLICSTATE_IDENTITY_CHANGED=NO
```

This implementation does not authorize I6D mapping implementation, Counter
retry, Link capture, or fresh-process A/B.
