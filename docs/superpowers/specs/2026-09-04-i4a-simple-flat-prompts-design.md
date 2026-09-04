# OCGForge-Ignis I4A — Simple Flat Prompt Runtime Design

Status: `DESIGN_APPROVED_BY_USER=YES`; `SPEC_REVIEW_PENDING=YES`; implementation not started

Date: 2026-09-04

Repository: `https://github.com/chrismaghuhn/OCGForge-Ignis`

Base: `13e0831c1364e329b62a1beb11f67d6ae8e682d1`

Branch: `chris/i4a-simple-flat-prompts`

This document records the approved runtime design for I4A. The frozen
I4A0 contract and checked-in vectors remain normative; this document does not
change them.

## 1. Purpose and scope

I4A adds a deterministic adapter projection for exactly these modern inner
`GAME_MSG` families:

```text
MSG_SELECT_YESNO    = 13
MSG_SELECT_OPTION   = 14
MSG_SELECT_POSITION = 19
```

For each supported prompt, the runtime will parse one complete inner message,
publish its complete ordered public candidate domain, and retain a private
current-prompt binding from each local candidate key to the exact original
signed `i32` response value.

The implementation will not add general Yu-Gi-Oh! legality. EDOPro and the
pinned ocgcore remain the authorities for prompt legality and response
semantics.

The following remain outside I4A:

- `MSG_SELECT_EFFECTYN`, `MSG_SELECT_CHAIN`, `MSG_SELECT_IDLECMD`, and
  `MSG_SELECT_BATTLECMD`;
- outer CTOS/STOC framing, TCP reads, stream chunking, and socket writes;
- model input, checkpoints, policy selection, `Teacher`, `RandomLegal`, and
  fallback behavior;
- `OCGForge public_action_key` derivation and byte-exact I6 compatibility;
- response transmission through `CTOS_RESPONSE`;
- changes to I3 decoder, mirror, projection, canonical bytes, or projection
  identity.

## 2. Ownership and dependency boundary

The runtime ownership graph is:

```text
GAME_MSG state/query values
    -> GameplayMessageDecoderV1
    -> GameplayMirrorSessionV1
    -> PerspectiveStateMirrorV1
    -> PublicStateProjectionResultV1

complete supported flat-prompt GAME_MSG value
    -> FlatPromptSessionV1
    -> FlatPromptPublicContextV1
    -> complete ordered FlatPublicCandidateDescriptorV1[]
    -> private CurrentFlatPromptBindingV1
```

`GameplayMessageDecoderV1` remains the I3 state-message decoder. It will not
gain I4 prompt cases. `GameplayMirrorSessionV1` remains the state-lifecycle
owner. `PublicStateProjectionResultV1` remains the sole accepted public
authority for I3 public card locators and card codes.

`FlatPromptSessionV1` owns only the I4 flat-prompt lifecycle:

- synchronous acceptance of exactly one complete inner modern `GAME_MSG`;
- atomic wire validation and semantic projection;
- complete candidate order and local key construction;
- the successful-prompt ordinal;
- replacement or invalidation of the current private binding;
- validation of an opaque stale-safe selection handle; and
- resolution to a private scalar `i32` response value.

I4A has no authorized family that requires a proven public card reference.
YESNO and OPTION have no card reference, and POSITION obtains its complete
domain from the validated emitted mask. The current I3D API does not prove a
POSITION prompt-to-card correlation. Therefore the I4A API will not accept
unused mirror/projection parameters and will not invent a correlation seam.
`position_card_code` remains structurally absent in this slice. A future
family may add an authority input only after its accepted I3D correlation is
proven.

## 3. Value model

### 3.1 Public context

`FlatPromptPublicContextV1` will be a public discriminated value base with
only the common fields:

```text
contract_id   = ocgforge-ignis.flat-prompt-projection.v1
prompt_family = one of the three authorized family values
acting_player = 0 or 1
```

Family-specific immutable variants preserve the frozen inclusion matrix:

```text
YESNO:
    yes_no_description_id = exact decoded u64

OPTION:
    no family-specific fields

POSITION:
    position_allowed_positions_mask = validated mask
    position_card_code               = ABSENT in I4A
```

The context base will not expose `prompt_instance_ordinal`, local candidate
keys, public action identity, raw bytes, response values, or private binding
state. An irrelevant family field will not be represented as a nullable
member on a common DTO.

### 3.2 Public candidate descriptors

`FlatPublicCandidateDescriptorV1` will be a public discriminated value base.
Every descriptor contains only the common fields explicitly authorized by the
matrix:

```text
i4_local_candidate_key
choice_kind
```

Family variants add only their required semantic members:

```text
YESNO:
    no additional fields

OPTION:
    source_section = OPTIONS
    source_ordinal = exact wire ordinal
    option_value   = exact decoded u64

POSITION:
    position_value = one of 1, 2, 4, 8
```

No candidate descriptor will contain a response integer, response bytes,
protocol offsets, raw `GAME_MSG` bytes, `ModernLocInfoV1`, `MirrorEntityIdV1`,
socket state, or `public_action_key`.

The local key is a prompt-local binding selector only:

```text
MSG_SELECT_YESNO:NO
MSG_SELECT_YESNO:YES
MSG_SELECT_OPTION:OPTION:<source_ordinal>
MSG_SELECT_POSITION:FACEUP_ATTACK
MSG_SELECT_POSITION:FACEDOWN_ATTACK
MSG_SELECT_POSITION:FACEUP_DEFENSE
MSG_SELECT_POSITION:FACEDOWN_DEFENSE
```

The key will never be renamed, aliased, hashed, or presented as an OCGForge
public action key. Equal OPTION values remain separate descriptors because
their source ordinals and private response bindings differ.

### 3.3 Private binding and selection handle

`CurrentFlatPromptBindingV1` will remain `internal` and immutable. It owns
independent copies of:

```text
prompt_instance_ordinal
family
complete ordered candidate descriptors
complete ordered local keys
private local-key -> exact i32 response binding
```

The session will create an internal, value-owned
`FlatPromptSelectionHandleV1` from the current binding. The handle carries the
ordinal, family, selected local key, and an exact value-owned copy of the
ordered candidate domain needed for binding validation. It does not rely on
object identity, reference identity, receive-call count, or a hash as proof of
freshness.

Resolution checks, in order, are:

1. the handle ordinal equals the current binding ordinal;
2. the handle family equals the current binding family;
3. the handle's complete ordered domain equals the current binding domain;
4. the selected local key exists in that current binding; and
5. the selected key maps to the exact private `i32` response value.

Any failure returns a structured error and no response value. In particular,
an old handle remains stale even when a later prompt has the same family,
candidate fields, and local key.

## 4. Session API and lifecycle

The production façade will be a bounded synchronous `FlatPromptSessionV1`.
Its acceptance operation is conceptually:

```csharp
FlatPromptProjectionResultV1 TryAcceptPrompt(
    ReadOnlySpan<byte> completeInnerGameMessage)
```

The exact public result exposes only success/error, the typed public context,
and the immutable ordered public candidate view. It does not expose the
current binding or prompt ordinal.

Selection capture and response resolution remain internal runtime operations,
using the opaque value-owned handle rather than a bare string. No public or
internal operation writes a stream or constructs a network packet.

The session maintains one next-ordinal value and at most one current binding:

```text
next ordinal starts at 0

successful accepted prompt:
    build all values first
    check ordinal increment with checked arithmetic
    publish result and binding atomically
    advance ordinal exactly once

malformed/unsupported/unproven prompt:
    publish no context or candidate domain
    invalidate the usable current binding
    do not advance ordinal
```

The input span is read synchronously and never retained. All candidate
collections and decoded values are copied into value-owned storage before a
successful result is returned. The caller must supply GAME_MSG values in
semantic order; TCP chunking and outer framing are lower-layer concerns.

## 5. Exact family behavior

### 5.1 YESNO

The parser accepts only a ten-byte value:

```text
u8     13
u8     player
u64_le description
```

It validates the player before decoding the description and rejects every
other length. It creates exactly two candidates in this order:

```text
NO  -> private i32 0
YES -> private i32 1
```

There is no pass, cancel, third response, or one-candidate shortcut.

### 5.2 OPTION

The parser accepts only the exact modern layout:

```text
u8     14
u8     player
u8     option_count = n
n * u64_le option_description
```

It computes `3 + 8*n` with checked arithmetic and requires exact equality
with the supplied span length before allocating or building candidates.
`n=0` fails closed. Every source entry becomes one candidate in wire order,
with its exact `u64` value and zero-based ordinal. Duplicate values remain
distinct. Candidate ordinal `i` binds privately to signed `i32 i`.

### 5.3 POSITION

The parser accepts only the exact seven-byte layout:

```text
u8     19
u8     player
u32_le card_code
u8     allowed_positions_mask
```

It validates the player, rejects zero masks, singleton masks, and any bit
outside `0x0f`, and preserves only the validated mask in public context. The
candidate order is explicitly fixed by the proven bit-test order:

```text
0x01 FACEUP_ATTACK
0x02 FACEDOWN_ATTACK
0x04 FACEUP_DEFENSE
0x08 FACEDOWN_DEFENSE
```

Each candidate's private response is exactly its position bit as a signed
`i32`. The wire `card_code` is not copied into public context because no
accepted I3D prompt correlation proves it safe. This omission does not remove
any mask-derived candidate.

## 6. Failure and privacy behavior

The implementation will use an I4-specific structured error/result convention
consistent with the existing explicit result style. It will include at least
the following semantic failures:

```text
MALFORMED_PROMPT
UNSUPPORTED_PROMPT_LAYOUT
UNPROVEN_PUBLIC_REFERENCE
UNPROVEN_CANDIDATE_DOMAIN
INVALID_I4_LOCAL_CANDIDATE_KEY
STALE_PROMPT_BINDING
INVALID_RESPONSE_BINDING
```

It may distinguish invalid participants, invalid position masks, zero-option
domains, trailing bytes, count/body mismatch, and arithmetic failure when
that improves diagnostics. Exceptions will not represent partial success.

For every failed acceptance:

```text
public context       = absent
public candidates    = absent
usable current bind  = absent
response transmitted = no
ordinal advanced     = no
```

Malformed input cannot consume bytes from a following message. Legacy or
compatibility widths are unsupported. Candidate construction is transactional:
temporary values are discarded before publication if any validation fails.

The public surface will not expose hidden opponent identity, raw wire
addresses, mirror identities, raw response values/bytes, socket metadata,
paths, timestamps, process IDs, or thread/task identity. `i4_local_candidate_key`
remains explicitly separate from `public_action_key` and model input.

## 7. Test and verification design

Add one dedicated test file:

```text
tests/OCGForge.Ignis.Gameplay.Tests/Tests/I4AFlatPromptProjectionTests.cs
```

Append its registrations to the existing deterministic Gameplay harness
without renaming, removing, or reordering the existing 53 registrations.
Tests will cover:

- exact YESNO decoding, description preservation, candidate order, and
  response values;
- exact OPTION lengths, source order, duplicate preservation, values,
  ordinals, zero-domain rejection, and the same-ordinal/different-value
  metamorphic case;
- exact POSITION mask validation, explicit subset order, bit response values,
  absent unbound card code, and complete-domain preservation;
- stale same-looking handles, family mismatch, domain mismatch, invalid keys,
  failed-prompt invalidation, and exact ordinal advancement;
- public API reflection/privacy boundaries and value ownership after source
  buffer or caller collection mutation;
- deterministic output across fresh processes.

The intended implementation scope is at most five files:

```text
src/OCGForge.Ignis.Gameplay/FlatPromptTypesV1.cs
src/OCGForge.Ignis.Gameplay/FlatPromptProjectionV1.cs
src/OCGForge.Ignis.Gameplay/FlatPromptSessionV1.cs
tests/OCGForge.Ignis.Gameplay.Tests/Program.cs
tests/OCGForge.Ignis.Gameplay.Tests/Tests/I4AFlatPromptProjectionTests.cs
```

The frozen contract and both gameplay fixtures remain unchanged.

## 8. Acceptance and delivery boundary

Implementation begins only after this written spec is reviewed. Verification
will start from the exact base and will separately report local build,
individual harness, fresh-process determinism, privacy, and scope evidence.
All six Release builds and Protocol, Client, and Gameplay harnesses will be
run in two fresh processes, with warnings and errors recorded exactly as
executed. `git diff --check` is required.

The implementation may commit and push the requested branch only after those
gates pass. The commit message is:

```text
feat: implement I4A simple flat prompt projection
```

No PR will be created, no merge will be performed, and I4B, I5, and I6 will
not start. The handoff will stop for independent review and will not claim
`I4A_FINAL_PASS`.
