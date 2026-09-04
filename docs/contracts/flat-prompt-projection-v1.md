# OCGForge-Ignis I4A0 — Flat Prompt Projection V1

Status: `I4A0_CONTRACT_FREEZE=YES`; contract only
Contract ID: `ocgforge-ignis.flat-prompt-projection.v1`
Implementation status: `I4_IMPLEMENTED=NO`
Date: 2026-09-04

```text
I4A0=AUTHORIZED
I4_IMPLEMENTATION=NO
I5=NOT_AUTHORIZED
MODEL_INPUT=NOT_AUTHORIZED
LIVE_SERVER_USE=FORBIDDEN
```

This document freezes the modern flat-prompt wire, candidate-domain, source
ordering, privacy, current-prompt binding, and response contracts for exactly
seven message families. It does not implement a decoder, candidate DTO,
response sender, model input, or live server path.

## 1. Authority and exact target

The authoritative sources are the following exact commits:

| Repository | Commit | Role |
| --- | --- | --- |
| https://github.com/edo9300/edopro | `30935e847165a9ef0e547fb51a43f36168fab7c7` | pinned client prompt readers, modern/legacy width selector, response construction, and CTOS routing |
| https://github.com/edo9300/ygopro-core | `46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57` | pinned runtime prompt producers and response validation |

The EDOPro commit's `ocgcore` gitlink is exactly
`46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57`. The V1 target is the modern
EDOPro 41.0.2 / Bagooska, ocgcore API 11.0 path. Legacy/compatibility widths
are not accepted by this contract.

The sources are clean-room semantic authorities only. No upstream parser,
control flow, source implementation, or serialized upstream packet is copied
into Ignis. The machine inventory and the independently constructed vectors
under `fixtures/gameplay/v1/i4-flat-prompt-vectors.v1.json` are the checked-in
research evidence.

The seven and only seven I4 families are:

```text
MSG_SELECT_BATTLECMD   = 10
MSG_SELECT_IDLECMD     = 11
MSG_SELECT_EFFECTYN     = 12
MSG_SELECT_YESNO        = 13
MSG_SELECT_OPTION       = 14
MSG_SELECT_CHAIN        = 16
MSG_SELECT_POSITION     = 19
```

`MSG_SELECT_CARD`, `MSG_SELECT_TRIBUTE`, `MSG_SELECT_SUM`,
`MSG_SELECT_PLACE`, `MSG_SELECT_DISFIELD`, `MSG_SELECT_COUNTER`,
`MSG_SORT_CARD`, `MSG_SORT_CHAIN`, `MSG_ANNOUNCE_RACE`,
`MSG_ANNOUNCE_ATTRIB`, `MSG_ANNOUNCE_NUMBER`, `MSG_SELECT_UNSELECT_CARD`,
and `MSG_ANNOUNCE_CARD` are outside this freeze. They remain unimplemented,
unfrozen, or fail-closed according to the existing inventory.

## 2. Common wire rules

Each value below is the complete inner modern `GAME_MSG` value, beginning with
the one-byte message ID. It is not a TCP frame and has no adapter length
prefix. The existing I1 outer protocol remains authoritative for framing.

The only accepted numeric forms in these seven layouts are:

```text
u8       one unsigned byte
u32_le   four-byte unsigned little-endian integer
u64_le   eight-byte unsigned little-endian integer
i32_le   four-byte signed little-endian response integer
```

The pinned upstream writes native C++ integer objects through
`duel_message::write`/`BufferIO` and reads them through the matching client
functions. The accepted Ignis V1 target fixes those multi-byte wire values to
little-endian forms above. A future implementation must reject a legacy
`u8`/`u32_le` compatibility alternative rather than selecting a width from
available bytes.

The modern nested location value is exactly:

```text
ModernLocInfoV1 =
    u8       controller
    u8       location
    u32_le   sequence
    u32_le   position
```

It is exactly 10 bytes. For an overlay card, the pinned core's
`card::get_info_location` supplies the parent location with the overlay bit,
the parent sequence, and the overlay sequence in `position`. A location is a
protocol-local address. A future I4 implementation may use it only for private
source resolution and semantic correlation. It must not publish a new locator
from that address: the published locator must be copied from the successful
accepted I3D public-state projection described below.

Every `player`/`controller` field that identifies a duel player must be 0 or 1
in V1. Other values fail closed as an invalid participant. The accepted
location bits are the pinned core values `0x01` deck, `0x02` hand, `0x04`
monster, `0x08` spell/trap, `0x10` graveyard, `0x20` banished, `0x40` extra,
and `0x80` overlay. An unknown or structurally impossible card reference is
decoded only as private wire data; it never crosses the public candidate
boundary.

The complete inner value must be consumed exactly. A future decoder must:

1. check every fixed-width field before reading it;
2. compute every counted length with checked arithmetic before allocation or iteration;
3. require the computed length to equal the supplied complete-message length;
4. reject truncation, count/body mismatch, overflow, and every trailing byte; and
5. publish no partial candidate domain after any failure.

No following `GAME_MSG` value is consumed to repair a short prompt.

## 3. Exact public context and candidate descriptor

I4A0 freezes exactly two public decision values and one private binding value:

```text
FlatPromptPublicContextV1
FlatPublicCandidateDescriptorV1
CurrentFlatPromptBindingV1       # private to the current prompt instance
```

These are semantic contract values, not wire DTOs. Every field in the public
context and descriptor is governed by the inclusion matrices in this section.
`REQUIRED` means the member is present with the stated semantic value.
`ABSENT` means the member is not a contract member for that row; it is not a
null, zero, empty string, or another sentinel. `CONDITIONAL(predicate)` means
the member is present exactly when that predicate is true and is absent when
it is false. A false predicate never authorizes a substitute field.

No public context or candidate field exists outside the matrices below.

### Exact predicates and publication authority

The following predicates are normative. `PublicSemanticLocatorV1` is the
accepted locator syntax/validation contract, not a publication authority. The
only publication authority for a candidate card locator or card code is the
successful accepted `PublicStateProjectionResultV1` produced by I3D's accepted
I3C projection:

```text
MIRROR_PUBLIC_LOCATOR_AUTHORITY=NO
PUBLIC_SEMANTIC_LOCATOR_CODEC_ALONE_IS_PUBLICATION_AUTHORITY=NO
I3D_PUBLIC_STATE_PROJECTION_IS_PUBLIC_LOCATOR_AUTHORITY=YES
CANDIDATE_LOCATORS_COPIED_FROM_ACCEPTED_PUBLIC_SNAPSHOT=YES
SYNTHETIC_MIRROR_DERIVED_PUBLIC_LOCATOR=NO
POSITION_DOMAIN_AUTHORITY=VALIDATED_POSITION_MASK
POSITION_CARD_LOCATOR=ABSENT
POSITION_UNBOUND_CARD_CODE_REJECTS_PROMPT=NO

accepted PublicStateProjectionResultV1
    -> Snapshot
    -> Snapshot.Cards[]
    -> exact existing PublicCardStateV1.Locator and CardCode values
```

The mirror is a private resolution authority only. It may establish which
current source entity/context a protocol reference addresses, but it cannot
publish a locator or card code independently.

`accepted_public_projection` is a private authority input to the future I4
projector; it is not a public context member and is not duplicated into a
candidate. A successful result is required. The projector copies the exact
`PublicCardStateV1.Locator` and, when present, the exact `CardCode` from the
correlated snapshot card. It never calls a locator factory to create a second
published identity. If the accepted snapshot has no corresponding card, or if
more than one snapshot card could match without hidden physical continuity,
the card-bearing prompt fails closed.

```text
PUBLIC_CARD_REFERENCE_PROVEN(
    source_reference,
    mirror,
    accepted_public_projection
) =
    accepted_public_projection.IsSuccess
    AND accepted_public_projection.Snapshot is present
    AND source_reference privately resolves to exactly one current mirror
        entity/context
    AND exactly one current PublicCardStateV1 in
        accepted_public_projection.Snapshot.Cards[] correlates to that source
        through only the permitted perspective-safe semantic facts below
    AND the candidate's published locator is an exact copy of that
        PublicCardStateV1.Locator
    AND no new locator is created by the mirror or by the locator codec

INDEXED_VISIBLE_CORRELATION =
    exact absolute player + semantic zone + semantic sequence,
    only for an indexed visible family admitted by the public-state contract

HAND_OR_EXTRA_PUBLIC_ORDINAL_CORRELATION =
    exact absolute player + zone + known public card code, with exactly one
    matching PublicCardStateV1 in the accepted public snapshot
    AND no raw hand/extra sequence, physical continuity, mirror identity,
        collection order, or allocation order is used

OVERLAY_CORRELATION =
    exact accepted public overlay semantic components, copied from the
    matching PublicCardStateV1; no OverlayRelations or mirror identity fallback

MAIN_DECK_CORRELATION =
    not available in V1 because Main Deck has no per-card public locator

CARD_CODE_SAFE(source_code, accepted_public_card) =
    source_code != 0
    AND accepted_public_card.CardCode is present
    AND accepted_public_card.CardCode.Value == source_code

POSITION_CARD_CODE_SAFE(source_code, accepted_position_context) =
    source_code != 0
    AND an accepted public-state context explicitly correlates this position
        prompt to exactly one current public card without deriving a locator
    AND that PublicCardStateV1.CardCode is present
    AND that public card code == source_code
```

For every card-bearing BATTLECMD, IDLECMD, EFFECTYN, and CHAIN entry,
`PUBLIC_CARD_REFERENCE_PROVEN` is required. If it is false, the entire
card-bearing prompt projection fails closed and no candidate list is published.
The public locator is copied from the accepted snapshot, never reconstructed.
`CARD_CODE_SAFE` controls only whether the separate `card_code` member is
included; a false `CARD_CODE_SAFE` does not create an unknown code value.

`POSITION_CARD_CODE_SAFE` never controls whether a valid POSITION domain is
published. POSITION's domain authority is the validated emitted bitmask and
its card locator is always absent. If `POSITION_CARD_CODE_SAFE` is false,
`position_card_code` is absent and the complete legal position domain is still
published. The wire card code is never republished as an unbound identity.

### Context schema

The common context members are fixed:

| Field | Inclusion | Exact value |
| --- | --- | --- |
| `contract_id` | REQUIRED | `ocgforge-ignis.flat-prompt-projection.v1` |
| `prompt_family` | REQUIRED | exact symbolic family name |
| `acting_player` | REQUIRED | absolute player 0 or 1 from the prompt's player byte |
| `prompt_instance_ordinal` | ABSENT | private binding only; never public context or OCGForge identity |
| `public_action_key` | ABSENT | owned by OCGForge/I6, never an I4 local value |
| `i4_local_candidate_key` | ABSENT | candidate member, not shared context |

Family-specific shared context members are:

| Family | Required shared fields | Conditional shared fields | Absent shared fields |
| --- | --- | --- | --- |
| `MSG_SELECT_BATTLECMD` | none beyond common context | none | `description_or_effect_id`, `client_mode`, `position_value`, `transition_token`, `option_value`, chain metadata |
| `MSG_SELECT_IDLECMD` | none beyond common context | none | `description_or_effect_id`, `client_mode`, `position_value`, `transition_token`, `option_value`, chain metadata |
| `MSG_SELECT_EFFECTYN` | `effect_card_locator` copied from the successful accepted public snapshot via `PUBLIC_CARD_REFERENCE_PROVEN`; `effect_description_id` as the u64 prompt description | `effect_card_code` copied from that same accepted public card via `CARD_CODE_SAFE` | `position_value`, `transition_token`, `option_value`, chain metadata |
| `MSG_SELECT_YESNO` | `yes_no_description_id` as the u64 prompt description | none | card fields, `position_value`, `transition_token`, `option_value`, chain metadata |
| `MSG_SELECT_OPTION` | none beyond common context | none | card fields, description/effect field, `position_value`, `transition_token`, chain metadata |
| `MSG_SELECT_CHAIN` | `chain_spe_count`, `chain_forced`, `chain_hint_timing_for_player`, `chain_hint_timing_for_other_player` | none | card fields, `position_value`, `transition_token`, `option_value` |
| `MSG_SELECT_POSITION` | `position_allowed_positions_mask` after exact mask validation | `position_card_code` via `POSITION_CARD_CODE_SAFE`; false predicate means ABSENT and does not reject the valid domain | `position_card_locator`, description/effect field, `transition_token`, `option_value`, chain metadata |

`chain_spe_count` is the exact normalized u8 field. `chain_forced` is the
normalized boolean. The two hint timings are normalized u32 values. None of
these shared fields is a response binding or a raw protocol dump.

### Candidate descriptor schema

The fixed descriptor member set is:

```text
i4_local_candidate_key
choice_kind
source_section
source_ordinal
public_semantic_card_locator
card_code
description_or_effect_id
client_mode
direct_attackable
position_value
transition_token
option_value
```

The exact inclusion matrix is below. `REQUIRED=value` denotes the status
`REQUIRED` with that fixed value. A required card locator has the
`PUBLIC_CARD_REFERENCE_PROVEN` precondition defined above; a false predicate
fails the card-bearing prompt. The locator is the exact value copied from the
accepted public snapshot. `CONDITIONAL(CARD_CODE_SAFE)` always means the exact
predicate defined above; it never means “include if useful”. For POSITION,
`CONDITIONAL(POSITION_CARD_CODE_SAFE)` is absent when false and never removes
the validated position domain.

#### BATTLECMD candidates

| Candidate kind | `i4_local_candidate_key` | `choice_kind` | `source_section` | `source_ordinal` | `public_semantic_card_locator` | `card_code` | `description_or_effect_id` | `client_mode` | `direct_attackable` | `position_value` | `transition_token` | `option_value` |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| activatable entry | REQUIRED | REQUIRED=`ACTIVATE` | REQUIRED=`ACTIVATABLE` | REQUIRED | REQUIRED | CONDITIONAL(`CARD_CODE_SAFE`) | REQUIRED | REQUIRED | ABSENT | ABSENT | ABSENT | ABSENT |
| attackable entry | REQUIRED | REQUIRED=`ATTACK` | REQUIRED=`ATTACKABLE` | REQUIRED | REQUIRED | CONDITIONAL(`CARD_CODE_SAFE`) | ABSENT | ABSENT | REQUIRED | ABSENT | ABSENT | ABSENT |
| M2 transition | REQUIRED | REQUIRED=`TO_M2` | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | REQUIRED=`MAIN_PHASE_2` | ABSENT |
| End transition | REQUIRED | REQUIRED=`TO_EP` | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | REQUIRED=`END_PHASE` | ABSENT |

#### IDLECMD candidates

| Candidate kind | `i4_local_candidate_key` | `choice_kind` | `source_section` | `source_ordinal` | `public_semantic_card_locator` | `card_code` | `description_or_effect_id` | `client_mode` | `direct_attackable` | `position_value` | `transition_token` | `option_value` |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| summon entry | REQUIRED | REQUIRED=`SUMMON` | REQUIRED=`SUMMON` | REQUIRED | REQUIRED | CONDITIONAL(`CARD_CODE_SAFE`) | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT |
| special-summon entry | REQUIRED | REQUIRED=`SPECIAL_SUMMON` | REQUIRED=`SPECIAL_SUMMON` | REQUIRED | REQUIRED | CONDITIONAL(`CARD_CODE_SAFE`) | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT |
| reposition entry | REQUIRED | REQUIRED=`REPOSITION` | REQUIRED=`REPOSITION` | REQUIRED | REQUIRED | CONDITIONAL(`CARD_CODE_SAFE`) | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT |
| monster-set entry | REQUIRED | REQUIRED=`MSET` | REQUIRED=`MSET` | REQUIRED | REQUIRED | CONDITIONAL(`CARD_CODE_SAFE`) | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT |
| spell/trap-set entry | REQUIRED | REQUIRED=`SSET` | REQUIRED=`SSET` | REQUIRED | REQUIRED | CONDITIONAL(`CARD_CODE_SAFE`) | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT |
| activatable entry | REQUIRED | REQUIRED=`ACTIVATE` | REQUIRED=`ACTIVATE` | REQUIRED | REQUIRED | CONDITIONAL(`CARD_CODE_SAFE`) | REQUIRED | REQUIRED | ABSENT | ABSENT | ABSENT | ABSENT |
| Battle transition | REQUIRED | REQUIRED=`TO_BP` | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | REQUIRED=`BATTLE_PHASE` | ABSENT |
| End transition | REQUIRED | REQUIRED=`TO_EP` | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | REQUIRED=`END_PHASE` | ABSENT |
| hand-shuffle transition | REQUIRED | REQUIRED=`SHUFFLE_HAND` | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | REQUIRED=`SHUFFLE_HAND` | ABSENT |

#### EFFECTYN and YESNO candidates

The card locator and description are shared context for EFFECTYN; the
description is shared context for YESNO. They are not duplicated into each
candidate descriptor.

| Family/kind | `i4_local_candidate_key` | `choice_kind` | `source_section` | `source_ordinal` | `public_semantic_card_locator` | `card_code` | `description_or_effect_id` | `client_mode` | `direct_attackable` | `position_value` | `transition_token` | `option_value` |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| EFFECTYN NO | REQUIRED | REQUIRED=`NO` | ABSENT | ABSENT | ABSENT; context `effect_card_locator` REQUIRED | ABSENT; context `effect_card_code` CONDITIONAL(`CARD_CODE_SAFE`) | ABSENT; context `effect_description_id` REQUIRED | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT |
| EFFECTYN YES | REQUIRED | REQUIRED=`YES` | ABSENT | ABSENT | ABSENT; context `effect_card_locator` REQUIRED | ABSENT; context `effect_card_code` CONDITIONAL(`CARD_CODE_SAFE`) | ABSENT; context `effect_description_id` REQUIRED | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT |
| YESNO NO | REQUIRED | REQUIRED=`NO` | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT; context `yes_no_description_id` REQUIRED | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT |
| YESNO YES | REQUIRED | REQUIRED=`YES` | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT; context `yes_no_description_id` REQUIRED | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT |

#### OPTION candidates

| Candidate kind | `i4_local_candidate_key` | `choice_kind` | `source_section` | `source_ordinal` | `public_semantic_card_locator` | `card_code` | `description_or_effect_id` | `client_mode` | `direct_attackable` | `position_value` | `transition_token` | `option_value` |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| option entry | REQUIRED | REQUIRED=`OPTION` | REQUIRED=`OPTIONS` | REQUIRED | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | REQUIRED=`u64 option_description` |

`option_value` is the exact decoded u64 option value. It is required because
two options with different values must remain semantically distinguishable;
the local ordinal does not replace the public option value.

#### CHAIN candidates

| Candidate kind | `i4_local_candidate_key` | `choice_kind` | `source_section` | `source_ordinal` | `public_semantic_card_locator` | `card_code` | `description_or_effect_id` | `client_mode` | `direct_attackable` | `position_value` | `transition_token` | `option_value` |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| chain entry | REQUIRED | REQUIRED=`CHAIN_ENTRY` | REQUIRED=`CHAIN_CHOICES` | REQUIRED | REQUIRED | CONDITIONAL(`CARD_CODE_SAFE`) | REQUIRED | REQUIRED | ABSENT | ABSENT | ABSENT | ABSENT |
| no-chain entry | REQUIRED | REQUIRED=`NO_CHAIN` | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT | ABSENT |

The chain metadata is required once in shared context for every row. It is not
duplicated into the candidate descriptor.

#### POSITION candidates

| Candidate kind | `i4_local_candidate_key` | `choice_kind` | `source_section` | `source_ordinal` | `public_semantic_card_locator` | `card_code` | `description_or_effect_id` | `client_mode` | `direct_attackable` | `position_value` | `transition_token` | `option_value` |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| face-up attack | REQUIRED | REQUIRED=`FACEUP_ATTACK` | ABSENT | ABSENT | ABSENT; no locator exists in this wire layout | ABSENT; context `position_card_code` CONDITIONAL(`POSITION_CARD_CODE_SAFE`) | ABSENT | ABSENT | ABSENT | REQUIRED=`1` | ABSENT | ABSENT |
| face-down attack | REQUIRED | REQUIRED=`FACEDOWN_ATTACK` | ABSENT | ABSENT | ABSENT; no locator exists in this wire layout | ABSENT; context `position_card_code` CONDITIONAL(`POSITION_CARD_CODE_SAFE`) | ABSENT | ABSENT | ABSENT | REQUIRED=`2` | ABSENT | ABSENT |
| face-up defense | REQUIRED | REQUIRED=`FACEUP_DEFENSE` | ABSENT | ABSENT | ABSENT; no locator exists in this wire layout | ABSENT; context `position_card_code` CONDITIONAL(`POSITION_CARD_CODE_SAFE`) | ABSENT | ABSENT | ABSENT | REQUIRED=`4` | ABSENT | ABSENT |
| face-down defense | REQUIRED | REQUIRED=`FACEDOWN_DEFENSE` | ABSENT | ABSENT | ABSENT; no locator exists in this wire layout | ABSENT; context `position_card_code` CONDITIONAL(`POSITION_CARD_CODE_SAFE`) | ABSENT | ABSENT | ABSENT | REQUIRED=`8` | ABSENT | ABSENT |

### Local binding and OCGForge identity boundary

The I4-local ASCII selector is exactly:

```text
MSG_SELECT_BATTLECMD:ACTIVATE:<source_ordinal>
MSG_SELECT_BATTLECMD:ATTACK:<source_ordinal>
MSG_SELECT_BATTLECMD:TO_M2
MSG_SELECT_BATTLECMD:TO_EP

MSG_SELECT_IDLECMD:SUMMON:<source_ordinal>
MSG_SELECT_IDLECMD:SPECIAL_SUMMON:<source_ordinal>
MSG_SELECT_IDLECMD:REPOSITION:<source_ordinal>
MSG_SELECT_IDLECMD:MSET:<source_ordinal>
MSG_SELECT_IDLECMD:SSET:<source_ordinal>
MSG_SELECT_IDLECMD:ACTIVATE:<source_ordinal>
MSG_SELECT_IDLECMD:TO_BP
MSG_SELECT_IDLECMD:TO_EP
MSG_SELECT_IDLECMD:SHUFFLE_HAND

MSG_SELECT_EFFECTYN:NO
MSG_SELECT_EFFECTYN:YES
MSG_SELECT_YESNO:NO
MSG_SELECT_YESNO:YES
MSG_SELECT_OPTION:OPTION:<source_ordinal>
MSG_SELECT_CHAIN:CHAIN_ENTRY:<source_ordinal>
MSG_SELECT_CHAIN:NO_CHAIN

MSG_SELECT_POSITION:FACEUP_ATTACK
MSG_SELECT_POSITION:FACEDOWN_ATTACK
MSG_SELECT_POSITION:FACEUP_DEFENSE
MSG_SELECT_POSITION:FACEDOWN_DEFENSE
```

The name of every value in that grammar is `i4_local_candidate_key`. It is an
adapter-local, current-prompt candidate selector/binding identity. It is not
the already accepted OCGForge `public_action_key`.

```text
I4_LOCAL_CANDIDATE_KEY_IS_OCGFORGE_PUBLIC_ACTION_KEY=NO
I4_LOCAL_CANDIDATE_KEY_MODEL_INPUT_AUTHORIZED=NO
I4_LOCAL_CANDIDATE_KEY_I6_COMPATIBILITY_CLAIM=NO
OCGFORGE_PUBLIC_ACTION_KEY_DERIVATION=I6_OWNED
I6_BYTE_EXACT_COMPATIBILITY=UNPROVEN
```

The OCGForge contract `ocgforge.public_action_identity.v1` owns the distinct
future value `public_action.v1.<lowercase hexadecimal canonical descriptor
bytes>`. I4A0 references that boundary but does not reimplement it, create a
second translation identity, or alias the local selector to it. I6 will later
prove how an accepted semantic candidate descriptor maps to the OCGForge
descriptor and exact `public_action_key`.

Equal descriptions, equal card codes, and equal visible references do not
justify deduplication. A source entry at ordinal 0 and a source entry at
ordinal 1 remain two candidates and have distinct local keys. For OPTION,
their required `option_value` fields must also preserve their semantic values.

The private current-prompt binding is a value-owned record containing:

```text
prompt_instance_ordinal : u64
message_id/family       : exact one of the seven families
ordered candidate descriptors : complete current domain
ordered i4_local_candidate_keys : complete current domain
private key-to-response binding for this prompt only
```

The first accepted I4 prompt in semantic GAME_MSG order has ordinal 0. The
ordinal increments once, with checked arithmetic, for each subsequently
accepted I4 prompt. TCP reads, chunk boundaries, threads, allocation order,
and wall time never advance it. A selected local key is valid only when its
prompt ordinal, family, and complete current domain match the current binding.
Thus a selection from prompt A is rejected even when prompt B has an identical
local key and identical-looking candidate fields. A failed or unsupported
prompt creates no usable binding.

## 4. Response envelope and no fallback

All seven flat families use the same original scalar response path:

```text
CTOS_RESPONSE frame:
    u16_le   length = 5                 # packet type plus four body bytes
    u8       packet_type = 0x01         # CTOS_RESPONSE
    i32_le   response_body
```

EDOPro's `DuelClient::SetResponseI` stores an `int32_t` body, and
`SendBufferToServer(CTOS_RESPONSE, ...)` sends the body unchanged. The core
copies that body into its response buffer and validates it through
`returns.at<int32_t>(0)`. A future implementation must produce exactly four
body bytes and must not submit an alternative width or an unsigned sentinel.

These rules are absolute:

```text
N=1_AUTO_ANSWER=FORBIDDEN
TEACHER_FALLBACK=NO
RANDOM_LEGAL_FALLBACK=NO
FIRST_CANDIDATE_FALLBACK=NO
PASS_FALLBACK=NO
RETRY_WITH_OTHER_POLICY=NO
```

When a source prompt contains one legal candidate, the future adapter still
publishes one candidate and waits for the agent/model selection. The pinned
core internally resolves a position or simple-AI path without sending a
prompt; that upstream behavior is not an adapter permission to auto-answer an
I4 prompt that was received. Unsupported, stale, unbound, malformed, or
ambiguous values fail closed and never submit a replacement response.

## 5. MSG_SELECT_BATTLECMD (10)

### Exact modern request grammar

```text
u8       message_id = 10
u8       player
u32_le   activatable_count = a
repeat a times:
    u32_le card_code
    u8     controller
    u8     location
    u32_le sequence
    u64_le description
    u8     client_mode
u32_le   attackable_count = b
repeat b times:
    u32_le card_code
    u8     controller
    u8     location
    u8     sequence
    u8     direct_attackable
u8       to_main_phase_2
u8       to_end_phase
```

The activatable entry is 19 bytes. The attackable entry is 8 bytes. The exact
complete-message length is:

```text
payload_bytes = 11 + 19*a + 8*b
total_bytes   = 12 + 19*a + 8*b
```

The syntactic minimum is 12 bytes. Both flags and `direct_attackable` are
boolean bytes and must be exactly 0 or 1. `client_mode` is one of the pinned
EDOPro values `0=EFFECT_CLIENT_MODE_NORMAL`,
`1=EFFECT_CLIENT_MODE_RESOLVE`, or `2=EFFECT_CLIENT_MODE_RESET`; another
value is unproven and fails closed. The source count fields are modern
`u32_le`; a one-byte compatibility count or narrow sequence is not V1.

### Complete domain and order

The legal response domain has exactly:

```text
N = a + b + (to_main_phase_2 == 1 ? 1 : 0)
                    + (to_end_phase == 1 ? 1 : 0)
```

The domain must be non-empty. A syntactically valid message with no
activatable entries, no attackable entries, and both transition flags zero has
no legal response and fails closed as a prohibited zero-option state.

The deterministic order is the wire/source order:

1. every activatable entry in received order;
2. every attackable entry in received order;
3. `TO_M2` if its flag is 1;
4. `TO_EP` if its flag is 1.

The pinned core sorts `core.select_chains` with its
`chain::chain_operation_sort` before writing the activatable section. Ignis
preserves the resulting wire order and never exposes or recomputes the core's
internal effect ID. The attackable section is preserved as emitted. Duplicate
entries remain distinct by section ordinal.

The low response selector is the section kind and the high 16 bits are the
zero-based ordinal within that section:

| Candidate | Response integer | Body bytes |
| --- | ---: | --- |
| activatable ordinal `i` | `(i << 16) \| 0` | `i32_le` |
| attackable ordinal `i` | `(i << 16) \| 1` | `i32_le` |
| `TO_M2` | `2` | `02 00 00 00` |
| `TO_EP` | `3` | `03 00 00 00` |

The core validates kind 0..3, section bounds, and transition flags. The
`card_code` and coordinates are not public identity; an entry requiring a
card reference is resolved privately through the current perspective mirror
and then correlated to, and copied from, the accepted public-state snapshot.

## 6. MSG_SELECT_IDLECMD (11)

### Exact modern request grammar

```text
u8       message_id = 11
u8       player
u32_le   summon_count = s
repeat s times:
    u32_le card_code
    u8     controller
    u8     location
    u32_le sequence
u32_le   special_summon_count = ss
repeat ss times: same 10-byte card entry
u32_le   reposition_count = r
repeat r times:
    u32_le card_code
    u8     controller
    u8     location
    u8     sequence
u32_le   monster_set_count = m
repeat m times: same 10-byte card entry
u32_le   spell_trap_set_count = st
repeat st times: same 10-byte card entry
u32_le   activatable_count = a
repeat a times:
    u32_le card_code
    u8     controller
    u8     location
    u32_le sequence
    u64_le description
    u8     client_mode
u8       to_battle_phase
u8       to_end_phase
u8       shuffle_hand
```

The first, second, fourth, fifth, and sixth entry sections are 10 bytes per
entry. The reposition section is 7 bytes per entry. The activation section is
19 bytes per entry. The exact length is:

```text
payload_bytes = 28 + 10*s + 10*ss + 7*r + 10*m + 10*st + 19*a
total_bytes   = 29 + 10*s + 10*ss + 7*r + 10*m + 10*st + 19*a
```

The syntactic minimum is 29 bytes. All three final flags and every activation
`client_mode` are validated as described for BATTLECMD. Every count is a
modern `u32_le`; legacy one-byte counts and narrow sequences are forbidden.

### Complete domain and order

The complete legal domain has exactly:

```text
N = s + ss + r + m + st + a
      + (to_battle_phase == 1 ? 1 : 0)
      + (to_end_phase == 1 ? 1 : 0)
      + (shuffle_hand == 1 ? 1 : 0)
```

The domain must be non-empty. A zero count with all three flags zero is not a
pass candidate; it is a fail-closed zero-option state.

The exact concatenation order is:

1. `SUMMON` entries;
2. `SPECIAL_SUMMON` entries;
3. `REPOSITION` entries;
4. `MSET` entries;
5. `SSET` entries;
6. `ACTIVATE` entries;
7. `TO_BP` when flagged;
8. `TO_EP` when flagged;
9. `SHUFFLE_HAND` when flagged.

Within each repeated section, preserve wire order. The source entries are
identified by `(section, source_ordinal)`, so equal card codes and equal
coordinates do not collapse. Exact response bindings are:

| Candidate | Response integer |
| --- | ---: |
| `SUMMON` ordinal `i` | `(i << 16) \| 0` |
| `SPECIAL_SUMMON` ordinal `i` | `(i << 16) \| 1` |
| `REPOSITION` ordinal `i` | `(i << 16) \| 2` |
| `MSET` ordinal `i` | `(i << 16) \| 3` |
| `SSET` ordinal `i` | `(i << 16) \| 4` |
| `ACTIVATE` ordinal `i` | `(i << 16) \| 5` |
| `TO_BP` | `6` |
| `TO_EP` | `7` |
| `SHUFFLE_HAND` | `8` |

The transition flags are the core's already computed legal choices. Ignis
does not infer a phase transition or add a generic pass. A card-bearing
candidate cannot be public until its card reference is proven by a unique
correlation to the accepted public-state snapshot. The mirror only performs
private source resolution, and `PublicSemanticLocatorV1` only validates or
compares the copied syntax.

## 7. MSG_SELECT_EFFECTYN (12)

### Exact modern request grammar

```text
u8       message_id = 12
u8       player
u32_le   card_code
ModernLocInfoV1 card_location       # 10 bytes
u64_le   description
```

The exact payload is 23 bytes and the exact complete-message length is 24
bytes. `description` is not a legacy `u32` in V1. The location is the exact
10-byte modern form, including the overlay convention described above.

The complete legal domain is exactly two scalar choices. Because the source
has no repeated choice array, I4 V1 fixes the explicit semantic order
`NO(0)`, then `YES(1)`; this is a deterministic adapter order, not a claim
that a UI button order is a wire list. The bindings are:

```text
MSG_SELECT_EFFECTYN:NO  -> i32 0 -> 00 00 00 00
MSG_SELECT_EFFECTYN:YES -> i32 1 -> 01 00 00 00
```

The core accepts only response integers 0 and 1. A public candidate carries
the normalized card reference copied from the accepted public-state snapshot
and a proven description identifier only after the private source reference
has been uniquely correlated to that snapshot card. Raw location bytes and
raw response bytes stay private. An unproven required card reference fails
closed.

## 8. MSG_SELECT_YESNO (13)

### Exact modern request grammar

```text
u8       message_id = 13
u8       player
u64_le   description
```

The exact payload is 9 bytes and the exact complete-message length is 10
bytes. The complete legal domain is ordered `NO(0)`, then `YES(1)` and uses
the same exact four-byte bindings:

```text
MSG_SELECT_YESNO:NO  -> i32 0 -> 00 00 00 00
MSG_SELECT_YESNO:YES -> i32 1 -> 01 00 00 00
```

The pinned core validates only 0 and 1. The description is a normalized prompt
semantic value; it is not a raw response or an identity. No extra pass,
cancel, or third response exists.

## 9. MSG_SELECT_OPTION (14)

### Exact modern request grammar

```text
u8       message_id = 14
u8       player
u8       option_count = n
repeat n times:
    u64_le option_description
```

The exact complete-message length is:

```text
payload_bytes = 2 + 8*n
total_bytes   = 3 + 8*n
0 <= n <= 255
```

`option_count` is intentionally `u8`; it is not a modern `u32`. The pinned
core casts the option-vector size to `uint8_t`. If the source vector exceeds
255, its count byte cannot describe the complete emitted vector and the V1
exact-length rule rejects the resulting value rather than silently truncating
or accepting trailing options. A `n=0` `MSG_SELECT_OPTION` is not the source
path: the pinned core emits `MSG_HINT` for an empty option vector. Therefore
`n=0` fails closed as a prohibited zero-option prompt.

The complete domain is exactly the `n` options in wire order, with duplicate
description values preserved as separate source ordinals `0..n-1`. The
response binding is:

```text
MSG_SELECT_OPTION:OPTION:i -> i32 i -> four-byte i32_le(i)
```

The core accepts exactly `0 <= i < n`. No description sorting, deduplication,
or lexical ordering is permitted.

## 10. MSG_SELECT_CHAIN (16)

### Exact modern request grammar

```text
u8       message_id = 16
u8       player
u8       special_effect_count_or_trigger_marker = spe_count
u8       forced
u32_le   hint_timing_for_player
u32_le   hint_timing_for_other_player
u32_le   chain_count = c
repeat c times:
    u32_le card_code
    ModernLocInfoV1 card_location       # 10 bytes
    u64_le description
    u8     client_mode
```

The chain entry is 23 bytes. The exact complete-message length is:

```text
payload_bytes = 15 + 23*c
total_bytes   = 16 + 23*c
```

The syntactic minimum is 16 bytes. `forced` is exactly boolean 0 or 1.
`client_mode` is the same exact 0/1/2 semantic enum as the other activation
sections. `hint_timing_for_player` and `hint_timing_for_other_player` are
four-byte engine-provided timing masks; they are prompt context, not extra
candidates and not permission to infer a missing candidate.

For `spe_count`, values other than `0x7f` are the u8 representation of the
pinned core's optional special-effect/trigger count context. `0x7f` is the
explicit trigger-selection marker used by the pinned `PointEvent` and
`QuickEffect` paths. It is not a count of 127 entries and never adds a
candidate. If an ordinary source count aliases the marker or cannot be
represented unambiguously by this byte, the prompt is unproven and fails
closed. No public candidate contains a raw pointer or internal effect ID.

### Complete domain, cancel semantics, and order

Every chain entry is a legal source response candidate. The core's response
validator accepts an entry index `0..c-1`. If `forced == 0`, it also accepts
exactly the protocol-defined no-chain sentinel `-1`; if `forced == 1`, `-1` is
illegal. Consequently:

```text
forced == 0: N = c + 1; the final candidate is NO_CHAIN
forced == 1: N = c;     c must be >= 1
```

The optional empty chain (`forced=0`, `c=0`) therefore has one candidate,
`MSG_SELECT_CHAIN:NO_CHAIN`, and is not an invented pass. A forced empty chain
has no legal source response and fails closed. The no-chain candidate is
placed after the wire entries because the `-1` choice has no wire-array
position; this is the explicit V1 canonical placement, not a fabricated source
entry.

The chain entries preserve the exact wire order after the pinned core's
`core.select_chains.sort(chain::chain_operation_sort)`. The future public
domain contains entry source ordinals 0..c-1 and then `NO_CHAIN` when allowed.
Duplicate descriptions, codes, locators, and modes remain distinct.

Bindings are:

```text
MSG_SELECT_CHAIN:CHAIN_ENTRY:i -> i32 i  -> four-byte i32_le(i)
MSG_SELECT_CHAIN:NO_CHAIN   -> i32 -1 -> FF FF FF FF
```

The `forced` bit is the sole source authority for whether `NO_CHAIN` is legal.
The future implementation must not fabricate no-chain because the wire count
is zero, and must not omit it when `forced` is zero.

## 11. MSG_SELECT_POSITION (19)

### Exact modern request grammar

```text
u8       message_id = 19
u8       player
u32_le   card_code
u8       allowed_positions_mask
```

The exact payload is 6 bytes and the exact complete-message length is 7
bytes. The only allowed position bits are the pinned values:

| Bit/value | Semantic position |
| ---: | --- |
| `0x01` | `FACEUP_ATTACK` |
| `0x02` | `FACEDOWN_ATTACK` |
| `0x04` | `FACEUP_DEFENSE` |
| `0x08` | `FACEDOWN_DEFENSE` |

The valid emitted-prompt mask is a nonzero subset of `0x0f` with at least two
bits. The pinned core resolves `positions == 0` to face-up attack and resolves
a singleton mask directly without emitting `MSG_SELECT_POSITION`; neither
shortcut is a prompt candidate or an adapter auto-answer. If a one-bit or
zero-bit prompt is received, or if any bit outside `0x0f` is present, the V1
adapter fails closed as an unsupported/unproven prompt instead of repairing it.

The complete domain has one candidate per set bit. EDOPro's pinned
`ClientAnalyze` presents and handles them in this exact order:

```text
FACEUP_ATTACK (0x01)
FACEDOWN_ATTACK (0x02)
FACEUP_DEFENSE (0x04)
FACEDOWN_DEFENSE (0x08)
```

This order is proven by the pinned reader's four ordered bit tests; it is not a
generic numeric sort selected for convenience. The response body is the
position value itself:

```text
MSG_SELECT_POSITION:FACEUP_ATTACK    -> i32 1 -> 01 00 00 00
MSG_SELECT_POSITION:FACEDOWN_ATTACK  -> i32 2 -> 02 00 00 00
MSG_SELECT_POSITION:FACEUP_DEFENSE   -> i32 4 -> 04 00 00 00
MSG_SELECT_POSITION:FACEDOWN_DEFENSE -> i32 8 -> 08 00 00 00
```

The card code is a wire fact, not automatically a public identity. Since this
layout carries no card locator, `position_card_code` is present exactly when
`POSITION_CARD_CODE_SAFE` is true. When the predicate is false, the field is
absent; the validated multi-bit mask still produces its complete ordered
position domain. No raw coordinate or locator is invented, and an unbound card
code does not reject the position prompt.

## 12. Complete-domain invariant and duplicates

The old shorthand “N protocol options = N adapter candidates” is refined for
these heterogeneous families to mean:

```text
N = cardinality of the complete proven legal current source-response domain
```

Repeated sections contribute their entries exactly once in source order.
Boolean transition flags contribute one candidate when and only when the
source flag is 1. Position bits expand to one candidate per set bit in the
proven EDOPro order. `MSG_SELECT_CHAIN` contributes the protocol-defined
`NO_CHAIN` response exactly when `forced == 0`. Scalar yes/no prompts have two
fixed response values. There are no hidden pass/cancel choices beyond these
proven rules.

Every proven legal source response maps to exactly one candidate and every
candidate maps to exactly one response. A malformed or semantically unproven
prompt publishes no domain, not a partial domain. Duplicate source entries are
preserved; only their source section/ordinal distinguishes them when public
fields otherwise compare equal.

## 13. Privacy and determinism exclusions

Public candidates and public action keys must never contain or depend on:

```text
MirrorEntityIdV1
ModernLocInfoV1 raw values
raw controller/location/sequence/position tuples
raw GAME_MSG or CTOS_RESPONSE bytes
raw response indices when not the explicitly normalized public choice token
protocol offsets
pointer/object identity
allocation order
relation ordinals
hidden opponent card identity or hidden deck order
stale identity after knowledge destruction
socket/session identity
host, port, room password
PID, process handle, timestamp, wall clock, thread/task ID
filesystem path
TCP chunk boundaries or receive-buffer identity
dictionary/hash-map iteration order
model output, teacher output, archetype inference, beliefs, or probabilities
```

The seven prompt messages are delivered by the pinned server only to the
selecting player. That routing does not waive the Ignis public boundary: a
future projection resolves source references privately through the established
perspective mirror, but copies published card locators and card codes only from
the accepted successful I3D public-state projection. If a required card
reference is absent or ambiguous in that snapshot, the card-bearing candidate
domain fails closed. A false POSITION card-code predicate only omits that
conditional field; it does not remove a valid mask-derived position domain.

The semantic candidate order, keys, prompt ordinal, and private binding
association depend only on the complete accepted prompt and semantic GAME_MSG
order. They are invariant under TCP segmentation, machine, culture, locale,
timezone, process restart, scheduling, allocation order, and filesystem
location.

## 14. Failure contract

The future implementation must distinguish at least these categories:

```text
MALFORMED_PROMPT
UNSUPPORTED_PROMPT_LAYOUT
UNPROVEN_PUBLIC_REFERENCE
UNPROVEN_CANDIDATE_DOMAIN
INVALID_I4_LOCAL_CANDIDATE_KEY
STALE_PROMPT_BINDING
INVALID_RESPONSE_BINDING
```

Every failure returns no authoritative candidate domain and sends no response.
Count arithmetic, source-section bounds, boolean/enum values, participant and
required card-reference proof, position masks, chain cancel rules, and
legacy-width rejection are all fail-closed checks. A missing conditional
POSITION card code is not a failure. No retry policy, teacher, random action,
first candidate, generic pass, or partial answer is allowed.

## 15. Evidence and non-goals

The source authority for each frozen category is explicit:

| Frozen category | Exact pinned authority |
| --- | --- |
| message IDs and position/mode constants | ocgcore `ocgapi_constants.h#MSG_SELECT_*`, `POS_*`; EDOPro `gframe/ocgapi_constants.h#POS_*`, `EFFECT_CLIENT_MODE_*` |
| BATTLECMD layout/domain/validation | ocgcore `playerop.cpp#field::process(SelectBattleCmd&)`; EDOPro `gframe/duelclient.cpp#ClientAnalyze(MSG_SELECT_BATTLECMD)` |
| IDLECMD layout/domain/validation | ocgcore `playerop.cpp#field::process(SelectIdleCmd&)`; EDOPro `gframe/duelclient.cpp#ClientAnalyze(MSG_SELECT_IDLECMD)` |
| EFFECTYN layout/domain/validation | ocgcore `playerop.cpp#field::process(SelectEffectYesNo&)`; EDOPro `gframe/duelclient.cpp#ClientAnalyze(MSG_SELECT_EFFECTYN)` |
| YESNO layout/domain/validation | ocgcore `playerop.cpp#field::process(SelectYesNo&)`; EDOPro `gframe/duelclient.cpp#ClientAnalyze(MSG_SELECT_YESNO)` |
| OPTION layout/domain/validation | ocgcore `playerop.cpp#field::process(SelectOption&)`; EDOPro `gframe/duelclient.cpp#ClientAnalyze(MSG_SELECT_OPTION)` |
| CHAIN layout/domain/validation | ocgcore `playerop.cpp#field::process(SelectChain&)`, `processor.cpp#PointEvent`/`QuickEffect`; EDOPro `gframe/duelclient.cpp#ClientAnalyze(MSG_SELECT_CHAIN)` |
| POSITION layout/domain/validation/order | ocgcore `playerop.cpp#field::process(SelectPosition&)`; EDOPro `gframe/duelclient.cpp#ClientAnalyze(MSG_SELECT_POSITION)` |
| modern nested location | ocgcore `duel.h#duel::duel_message::write`, `card.cpp#card::get_info_location`; EDOPro `gframe/core_utils.cpp#ReadLocInfo` |
| chain/activation source order and mode | ocgcore `field.cpp#chain::chain_operation_sort`, `effect.cpp#effect::get_client_mode`, and the three `playerop.cpp` writers |
| response body and CTOS envelope | EDOPro `gframe/duelclient.h#SetResponseI`, `SendBufferToServer`, `gframe/event_handler.cpp` response handlers, and `gframe/duelclient.cpp#DuelClient::SendResponse`; core `duel.cpp#duel::set_response` and `playerop.cpp` validators |
| selecting-player routing | EDOPro `gframe/generic_duel.cpp#GenericDuel::Sending` and `GetResponse` |

All listed authorities are at EDOPro commit
`30935e847165a9ef0e547fb51a43f36168fab7c7` or ocgcore commit
`46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57` as specified in Section 1.

The exact positive and negative vectors are in
`fixtures/gameplay/v1/i4-flat-prompt-vectors.v1.json`. The current machine
inventory records each frozen layout as `layout_status=FROZEN` and
`support_status=OUT_OF_SCOPE`; this is a contract freeze, not runtime support.

This task does not:

- change `GameplayMessageDecoderV1`, the mirror, locators, or public-state JSON;
- implement prompt decoding, candidate construction, or response sending;
- add `PrivateResponseBinding` production types;
- implement I5 combinatorial prompts or continuations;
- implement model input, model runner, IPC, checkpoint binding, replay, or audit;
- claim OCGForge `PlayerObservation` compatibility or I6 cross-oracle acceptance;
- use WindBot as semantic authority;
- change rules, upstream pins, EDOPro, or ocgcore.

```text
I4_IMPLEMENTED=NO
I5_STARTED=NO
MODEL_INPUT_READY=NO
I6_CROSS_ORACLE_ACCEPTED=NO
PR_CREATED=NO
```
