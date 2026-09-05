# OCGForge-Ignis Combinatorial Prompt Continuation V1

Status: I5A0 eleven-family contract-freeze accepted; `SELECT_SUM` is intentionally
fail-closed unsupported for V1 after the exact research blocker was confirmed.
This document is not an implementation authorization and is not an accepted
replacement for the I4 flat-prompt contract.

Date: 2026-09-05

Contract ID:

    ocgforge-ignis.combinatorial-prompt-continuation.v1

The contract is intentionally separate from
`ocgforge-ignis.flat-prompt-projection.v1`. I4 remains final and unchanged.
The twelve requested families are audited below; eleven are contract-supported
and `SELECT_SUM` is a deliberately unsupported V1 boundary.

## 1. Authority and scope

The exact clean-room authorities are:

| Authority | Commit | Authority in this document |
| --- | --- | --- |
| `https://github.com/edo9300/ygopro-core` | `46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57` | Primary prompt producer and response validator |
| `https://github.com/edo9300/edopro` | `30935e847165a9ef0e547fb51a43f36168fab7c7` | Primary modern client reader and response-byte producer |
| `https://github.com/ProjectIgnis/windbot` | `bffe6b62679c8b2fafea8f59740e03a132517da4` | Secondary corroboration only; never legality authority |

The accepted I4 public-state boundary remains:

    captured MirrorSnapshotV1
        = private source-resolution authority only

    accepted PublicStateProjectionResultV1.Snapshot
        = sole persistent public locator and accepted-snapshot CardCode authority

    private response binding
        = original-protocol response authority

I5A0 covers contract and future adapter semantics for exactly these twelve
message families:

| Family | ID | I5A0 classification | Freeze status |
| --- | ---: | --- | --- |
| `MSG_SELECT_CARD` | 15 | `CONTINUATION_REQUIRED` | FROZEN |
| `MSG_SELECT_TRIBUTE` | 20 | `CONTINUATION_REQUIRED` | FROZEN |
| `MSG_SELECT_SUM` | 23 | `FAIL_CLOSED_UNSUPPORTED_V1` | UNSUPPORTED |
| `MSG_SELECT_PLACE` | 18 | `CONTINUATION_REQUIRED` | FROZEN |
| `MSG_SELECT_DISFIELD` | 24 | `CONTINUATION_REQUIRED` | FROZEN |
| `MSG_SELECT_COUNTER` | 22 | `CONTINUATION_REQUIRED` | FROZEN |
| `MSG_SORT_CARD` | 25 | `CONTINUATION_REQUIRED` | FROZEN |
| `MSG_SORT_CHAIN` | 21 | `CONTINUATION_REQUIRED` | FROZEN |
| `MSG_ANNOUNCE_RACE` | 140 | `CONTINUATION_REQUIRED` | FROZEN |
| `MSG_ANNOUNCE_ATTRIB` | 141 | `CONTINUATION_REQUIRED` | FROZEN |
| `MSG_ANNOUNCE_NUMBER` | 143 | `FLAT_TERMINAL_DOMAIN_SAFE` | FROZEN |
| `MSG_SELECT_UNSELECT_CARD` | 26 | `FLAT_TERMINAL_DOMAIN_SAFE` | FROZEN |

`MSG_ANNOUNCE_CARD = 142` is explicitly outside this contract and remains
`FAIL_CLOSED_UNSUPPORTED`. It is not counted as one of the twelve families.

No I5A0 value is an OCGForge `public_action_key`, a model input, a network
packet, a socket identity, or a continuation implementation. The final
contract status is:

    I5A0_CONTRACT_FREEZE=YES_FOR_11_FAMILIES
    SELECT_SUM_CONTRACT=FAIL_CLOSED_UNSUPPORTED_V1
    I5_IMPLEMENTATION_AUTHORIZED=NO

## 2. Common wire and parsing rules

An input is exactly one complete inner `GAME_MSG` value. TCP framing,
segmentation, receive buffers, and the outer message-length field are outside
this contract. The first byte is the message ID.

All modern V1 multi-byte fields use explicit little-endian encoding:

| Token | Meaning |
| --- | --- |
| `u8` | one unsigned byte |
| `u16_le` | two unsigned little-endian bytes |
| `u32_le` | four unsigned little-endian bytes |
| `u64_le` | eight unsigned little-endian bytes |
| `i8_le` | one signed response byte where explicitly stated |
| `i16_le` | two signed response bytes where explicitly stated |
| `i32_le` | four signed little-endian response bytes |

`ModernLocInfoV1` is exactly ten bytes:

    u8       controller
    u8       location
    u32_le   sequence
    u32_le   position

The modern reader's compatibility alternatives are not part of this
contract. In particular, a future parser must not choose a narrow count or
sequence width from the number of remaining bytes.

Every future parser must:

1. validate the message ID and family-specific primitive values;
2. compute every count-derived length with checked arithmetic before reading;
3. require the supplied length to equal the computed complete length;
4. reject truncation, overflow, underflow, trailing bytes, invalid boolean
   values, invalid enum/location values, and semantically unproven source
   states;
5. retain no caller-owned byte span after the call; and
6. publish no partial context, domain, continuation, or response binding on
   failure.

The core sometimes creates a hint instead of a prompt for an empty or already
resolved source. A raw message that is structurally parseable but cannot have
been emitted as a legal prompt is rejected as `UnprovenPromptSemantics`; it
is never repaired into a fabricated domain.

## 3. Public and private contract values

The future implementation uses closed, family-specific records rather than a
large nullable DTO. The exact C# names are implementation detail, but the
public shape is fixed conceptually:

### Public context variants

`CombinatorialPromptPublicContextV1` is an abstract record with sealed family
variants for:

* `CardSelection` — acting player, minimum, maximum, effective cancellation;
* `TributeSelection` — acting player, minimum required tribute value, maximum
  selected-card count, effective cancellation;
* `SELECT_SUM` has no admitted public context variant in V1. Its researched
  wire fields are retained only in the explicit fail-closed unsupported
  section below;
* `PlaceSelection` — acting player, required place count, disabled-field mode,
  and an ordered semantic eligible-place list;
* `CounterSelection` — acting player, counter type, required total, and an
  ordered list of capacity-bearing source descriptors;
* `SortSelection` — acting player, sort kind, source count, and ordered source
  descriptors;
* `AnnouncementMaskSelection` — acting player, mask kind, required bit count,
  and the admitted public mask;
* `AnnouncementNumber` — acting player and ordered number-option count;
* `SelectUnselectCard` — acting player, finishable, cancelable, minimum,
  maximum, and both source-section counts.

The public context may expose a semantic value explicitly carried by the
prompt, including a prompt-local CardCode when the reader exposes it to the
acting perspective. It never exposes raw protocol offsets, raw loc_info, raw
sum_param, private response bytes, mirror entity IDs, or a private continuation
ordinal.

### Public candidate variants

The closed candidate hierarchy contains only variants justified by the
families:

| Variant | Public identity and fields |
| --- | --- |
| `PickCard` | source section, source ordinal, optional accepted public locator, optional prompt-local disclosed CardCode, optional accepted-snapshot CardCode |
| `Finish` | no card or response fields; present only when the current partial selection is terminal legal |
| `Cancel` | no card or response fields; present only when the source response grammar permits cancellation |
| `PlaceField` | absolute player, semantic field zone, sequence |
| `AssignAmount` | source ordinal and nonnegative assigned amount |
| `PlaceInOrder` | source ordinal and next destination position |
| `PickMaskBit` | semantic bit index and bit value |
| `NumberOption` | source ordinal and public u64 number value |
| `SelectOccurrence` | source ordinal and optional accepted public card reference |
| `UnselectOccurrence` | source ordinal and optional accepted public card reference |
| `FinishOrCancel` | one wire-equivalent terminal candidate for the case where `SELECT_UNSELECT_CARD` exposes both flags but only one `-1` response exists |

`source_ordinal` is the ordinal in its named wire section, not a public
physical-card identity. Equal visible values remain separate candidates.

Card-bearing source occurrences have two independent disclosure channels. A
persistent/public-state locator is copied only from the accepted I3D snapshot.
The exact nonzero CardCode carried by the current prompt may additionally be
published as a prompt-local disclosure when the pinned reader exposes it to
the acting perspective. This value expires with the prompt and never proves a
locator, mutates the mirror, creates physical continuity, or becomes a public
state CardCode. The accepted-snapshot CardCode remains governed by
`CARD_CODE_SAFE` and is a separate structural variant.

The code-only `SELECT_CARD` producer therefore uses a prompt-local code
variant when its nonzero code is present. Its zero-location placeholder still
cannot establish a public locator. A zero code has no public CardCode and uses
the anonymous source-occurrence variant. Main Deck entries follow the same
rule: a prompt-local disclosed code may be visible without creating a Main
Deck locator.

### Local keys and private state

Keys are canonical ASCII strings. Decimal components use invariant ASCII
decimal with no sign and no leading zero except `0`. Examples are:

    MSG_SELECT_CARD:PICK:3
    MSG_SELECT_CARD:FINISH
    MSG_SELECT_CARD:CANCEL
    MSG_SELECT_COUNTER:ASSIGN_AMOUNT:2:1
    MSG_SORT_CARD:PLACE:4
    MSG_ANNOUNCE_RACE:PICK:62

Keys are current-step binding keys only. They are not OCGForge action keys.
The private state owns the original parsed source, source occurrence indexes,
accepted prior choices, the current step, the current complete domain, and the
private final-response codec state. It is value-owned and invalidated on a
new prompt or failed action.

## 4. Continuation semantics

For a continuation-required prompt, the adapter creates one private
continuation instance. It exposes a complete current domain, accepts one
externally selected local key, and atomically derives the next state. It does
not pre-expand all terminal combinations.

At every nonterminal state:

    current_domain
      = every source-consistent next action that leaves at least one legal
        terminal completion, plus Finish exactly when the current partial
        selection is terminal legal, plus Cancel exactly when the source
        response grammar permits it.

For the monotonic unordered families (`SELECT_PLACE`, `SELECT_DISFIELD`,
`ANNOUNCE_RACE`, and `ANNOUNCE_ATTRIB`), the completion predicate is explicit.
Let `k` be the required final selection count, `c` the number already selected,
`j` the candidate's canonical semantic index, `last` the last selected canonical
index (or `-1` initially), and `L` the ordered list of currently unselected
eligible canonical indexes. Candidate `j` is in the current domain exactly when:

    j > last
    c + 1 <= k
    count(index in L where index > j) >= k - (c + 1)

The third condition means that selecting `j` must leave enough higher canonical
indexes for at least one monotonic terminal completion. A candidate that leaves
too few such indexes is not part of the current domain, even when it is itself
currently eligible. This is a semantic predicate, not a greedy selection
algorithm, candidate cap, or public exposure of the canonical index.

For flat-terminal families, the initial domain contains every legal terminal
action and one selected action completes the original prompt. `N=1` still
requires external selection. A producer-side state that would not emit a
prompt is not an N=1 auto-answer.

Every source occurrence is distinct even when its public values are equal.
No candidate is sorted, deduplicated, capped, top-K filtered, fabricated, or
chosen by first-match logic. A source order that is semantic remains wire
order. A response codec order that is an explicit protocol canonicalization
(for example the field-place response order) is documented by that family
below and is not an unordered-container order.

Terminal reachability is defined over the canonical response codec, not over
byte-level aliases that the core happens to accept. Multiple raw encodings of
the same semantic response are one codec-equivalence class. Every canonical
terminal response is reachable by at least one legal path, and every terminal
path serializes to one canonical member of the source response domain.

There are no intermediate CTOS responses. Prior choices update only the
adapter-local continuation. Network envelope construction and transmission
remain later-layer responsibilities:

    I5_INTERMEDIATE_PROTOCOL_RESPONSE=ABSENT
    I5_NETWORK_RESPONSE_SENDING=ABSENT

Continuation path canonicalization is family-specific and frozen:

| Source semantics | Path rule |
| --- | --- |
| unordered card/tribute selection | monotonic increasing original source occurrence indexes |
| `SELECT_SUM` | unsupported V1 family; no continuation path or response codec |
| unordered multi-place selection | monotonic increasing indexes in the explicit place order |
| unordered race/attribute mask selection | monotonic increasing bit indexes |
| counter allocation | fixed source traversal order; every feasible amount including zero |
| card/chain sorting | ordered remaining-source traversal; each meaningful permutation remains reachable |
| flat terminal number and select/unselect | no continuation path; one external terminal action |

The monotonic rules remove permutation duplicates without removing any legal
terminal set. A lower source index is not offered after a higher index has
been picked in an unordered family; every legal set has its unique ascending
path. This rule is not a public identity and does not alter wire source order.

## 5. Card-reference authority

For source entries that contain a physical card address, the future call
captures `PerspectiveStateMirrorV1.Snapshot` exactly once. All private source
resolution uses that captured value. It then validates the caller-supplied
successful `PublicStateProjectionResultV1` by reprojecting the captured mirror
with the accepted snapshot's duel flags and comparing canonical bytes, SHA-256,
and `PublicProjectionId` exactly. The recomputed result is a consistency proof
only.

The existing `FlatPromptCardCorrelationV1` semantics are reused for persistent
locator proof:

    captured mirror card
        -> permitted private normalized address facts
        -> exactly one accepted public snapshot card
        -> copy accepted locator

The raw source CardCode has a second, prompt-local disclosure role. If the
exact current prompt delivered a nonzero code to the acting perspective, a
candidate may carry that code in a prompt-local structural variant. This does
not establish a public locator, update `PerspectiveStateMirrorV1`, survive a
prompt boundary, or authorize hidden continuity. A zero source code is absent.

For persistent locator publication, a zero or mismatching wire code never
destroys an otherwise proven locator. Main Deck references have no persistent
locator authority, but they may still expose the prompt-local disclosed code.
Hand/Extra references use the accepted I4B rule when a persistent public
ordinal is needed; duplicate known codes remain ambiguous for that purpose.
Overlay requires a proven overlay index; source layouts without that index
reject an overlay reference before calling the existing helper. No raw
sequence, physical continuity, mirror identity, collection order, or first
matching public card may disambiguate a persistent reference.

Source entries with a zero-location code-only form have no public card
reference. Their nonzero code is prompt-local disclosure only; their zero code
candidate is anonymous. This does not create a second publication authority.

## 6. Shared response codec for card-index selections

`SELECT_CARD` and `SELECT_TRIBUTE` use the core's card-index
response grammar. A terminal non-cancel response is encoded by the exact
EDOPro compact selection rule for the selected source indexes in accepted
pick order. The body contains no outer packet envelope.

For a selected index list of length `k` and maximum index `m`,
`GetSuitableReturn(m,k)` is:

| Condition | Type |
| --- | ---: |
| `m < 255` and `m >= k*8` | 2, followed by `u32_le k` and `k` `u8` indexes |
| `m < 65535` and `m >= k*16`, after the previous condition is false | 1, followed by `u32_le k` and `k` `u16_le` indexes |
| `m < 0xffffffff` and `m >= k*32`, after the previous conditions are false | 0, followed by `u32_le k` and `k` `u32_le` indexes |
| otherwise | 3, followed by a bitset whose bit 32+i denotes selected index i |

The type prefix is an `i32_le`; type `-1` is a four-byte cancel response when
the source permits cancellation. Type `3` is sized through the highest set
selected bit and starts at bit 32. A zero-length selected list uses the exact
EDOPro rule (`type 2`, count zero). Indexes are unique and in-range before
codec selection. The core accepts additional byte-level aliases; the adapter
uses one deterministic canonical encoding and never treats aliases as new
semantic candidates.

## 7. MSG_SELECT_CARD (15)

The modern complete message is:

    u8       message_id = 15
    u8       acting_player
    u8       cancelable
    u32_le   minimum_count
    u32_le   maximum_count
    u32_le   occurrence_count = n
    repeat n:
        u32_le card_code
        ModernLocInfoV1 location

Length is `15 + 14*n`. The core's emitted values are normalized from its
uint8 minimum/maximum fields: `minimum <= maximum`, `maximum <= n`, and
`cancelable` is the effective wire permission (`original_cancelable ||
minimum == 0`). A zero maximum or zero source vector produces a hint instead
of this prompt in the normal producer path and is rejected as an un-emitted
prompt. Modern count and location widths are mandatory.

The source domain is the n wire occurrences in wire order. The location-bearing
producer writes its card vector after the core's card-operation ordering; the
wire order is authoritative. The separate `SelectCardCodes` producer writes
the same layout with a zero location placeholder. A nonzero code in either
form is prompt-local disclosed data for the acting perspective; it is not a
persistent locator or mirror fact. Its candidate has no locator and uses the
prompt-local CardCode variant.

The continuation starts with no selected occurrences and uses monotonic source
occurrence indexes to remove permutation duplicates. `PICK:i` is admitted
when occurrence i is greater than the current last picked index, the count
does not exceed maximum, and at least one completion remains. `FINISH` is
admitted when the current count is between minimum and maximum. `CANCEL` is
admitted when the wire cancelable flag is true. Selecting an occurrence does
not remove any other occurrence.
The final response is the shared card-index codec; cancel is `i32_le(-1)`.

An addressed card occurrence carries its prompt-local code whenever it is
nonzero and uses the accepted I4B correlation policy for an optional
persistent locator. A safe locator-bearing variant is added only when proof
succeeds. Lack of a persistent Main Deck locator does not reject a prompt when
the prompt-local occurrence can be represented by source section, ordinal, and
the disclosed code. If the code is zero, the occurrence remains anonymous.
No raw address is exposed and no occurrence is discarded solely because a
persistent locator cannot be proven.

## 8. MSG_SELECT_TRIBUTE (20)

The modern complete message is:

    u8       message_id = 20
    u8       acting_player
    u8       cancelable
    u32_le   minimum_required_tribute_value
    u32_le   maximum_selected_card_count
    u32_le   occurrence_count = n
    repeat n:
        u32_le card_code
        u8       controller
        u8       location
        u32_le   sequence
        u8       release_value

Length is `15 + 11*n`. The first bound is a weighted tribute-value threshold,
not a card count. The second bound is the maximum number of selected card
occurrences. The core normalizes the internal values and caps the emitted
maximum at five and at its accumulated release total. The wire flag is the
effective `cancelable || minimum == 0` value. A zero maximum or empty source is
a hint path, not a valid emitted selection prompt.

The wire order is the complete source order. Equal code, location, sequence,
or release values remain separate occurrences. `release_value` is a private
weight used for legality; it is not automatically public card identity or a
public raw protocol field.

The continuation starts with an empty selected set and uses monotonic source
occurrence indexes to remove permutation duplicates. `PICK:i` is legal exactly
when the selected count remains within the maximum and some completion can
reach the minimum required tribute value. `FINISH` is present when selected
count is at most the maximum selected card count and the sum of selected
release values is at least the minimum required tribute value.
`CANCEL` is present when the effective wire flag is true. No count-only
substitution for the release weights is permitted. The final selected indexes
use the shared card-index codec; cancellation is `i32_le(-1)`.

Every entry carries its nonzero prompt-local CardCode, while persistent locator
publication remains optional and uses the accepted I4B card-reference
authority. This layout carries no overlay index, so an overlay location is
rejected before correlation. If no persistent locator can be proven, the
source occurrence may remain an anonymous prompt-local candidate identified
only by its section and ordinal; no raw address is exposed and no legal source
occurrence is removed solely because I3D has no persistent pile locator.

## 9. MSG_SELECT_SUM (23) — explicit V1 fail-closed unsupported boundary

The modern wire grammar is identifiable:

    u8       message_id = 23
    u8       acting_player
    u8       mode              # 0 equal, 1 greater in the writer
    u32_le   target_sum        # writer emits original acc & 0xffff
    u32_le   minimum_optional_count
    u32_le   maximum_optional_count
    u32_le   mandatory_count = m
    repeat m:
        u32_le card_code
        ModernLocInfoV1 location
        u32_le sum_param
    u32_le   optional_count = n
    repeat n:
        u32_le card_code
        ModernLocInfoV1 location
        u32_le sum_param

Length is `23 + 18*(m+n)`. Mandatory entries are always included in final
sum validation; optional entries are selected by the response. Optional
entries are emitted after the core's card-operation ordering. The source
writer does not emit this message when the optional source vector is empty.
Mode zero is emitted when the original maximum is nonzero; mode one is
emitted when it is zero. The writer transmits only the low sixteen bits of the
original `acc`.

The exact wire fields remain documented for inventory and fail-closed
dispatch, but `MSG_SELECT_SUM` is not an admitted I5 public-domain family in
V1. Every occurrence of message ID 23 is rejected before public context,
candidate-domain, continuation, or response-binding construction:

    error=UnsupportedPromptFamily
    public_context=ABSENT
    public_candidates=ABSENT
    private_binding=ABSENT
    prompt_ordinal=UNCHANGED

This is a deliberate scope decision, not a parser shortcut. The pinned core
accepts unrestricted Lua operation results into `uint32_t sum_param`, loses
upper accumulator bits when writing `acc & 0xffff`, and uses a different
signed/unsigned interpretation of packed alternatives in feasibility versus
final validation. `parse_response_cards` additionally sorts selected `card*`
values by private object address before the final validator; for admitted
zero-valued alternatives this can make validity depend on pointer order that
is absent from the prompt bytes. No exact, pointer-independent wire-to-domain
oracle can therefore be claimed for the unrestricted family.

The following are explicitly not permitted as a substitute:

    unsigned reinterpretation of all packed halves
    narrowing the source domain without complete producer proof
    heuristic or greedy completion search
    pointer/address reproduction
    candidate truncation or first-match selection

`SELECT_SUM` remains a researched message with a frozen fail-closed boundary.
A future contract version may re-admit it only after an independently accepted
source-backed domain/codec proof. This contract does not authorize such work:

    SELECT_SUM_CONTRACT=FAIL_CLOSED_UNSUPPORTED_V1
    SELECT_SUM_EXACT_SEMANTICS=NOT_APPLICABLE_DUE_UNSUPPORTED
    SELECT_SUM_EXACT_ORACLE_CONTRACT=NOT_APPLICABLE
    SELECT_SUM_HEURISTIC_ORACLE_ALLOWED=NO

## 10. MSG_SELECT_PLACE (18) and MSG_SELECT_DISFIELD (24)

Both modern messages have the same seven-byte layout:

    u8       message_id = 18 or 24
    u8       acting_player
    u8       required_place_count = k
    u32_le   field_flag

`MSG_SELECT_DISFIELD` is the same `SelectPlace` process with its
`disable_field` mode set. Length is exactly seven bytes. A zero count is a
producer-side no-prompt path. The flag is a four-byte blocked/available field
mask; it is not a card locator and is not published as a raw field.

The semantic slot groups, relative to the acting player, are:

| Group | Bit offset | Location | Sequence |
| --- | ---: | --- | --- |
| acting player's monster zones | 0 | `LOCATION_MZONE` | 0..6 |
| acting player's spell/trap zones | 8 | `LOCATION_SZONE` | 0..7 |
| other player's monster zones | 16 | `LOCATION_MZONE` | 0..6 |
| other player's spell/trap zones | 24 | `LOCATION_SZONE` | 0..7 |

A semantic place is eligible when its known slot bit is clear in the wire
flag. Unused mask bits are never candidate slots and are not emitted as raw
public data. The current public domain is all and only currently unselected
eligible semantic places in the explicit acting-player-relative group order
above that satisfy the monotonic completion predicate in section 4. The prompt
is unproven if `k` is zero or exceeds the number of eligible slots.

`PICK:player:zone:sequence` selects one not-yet-selected place. There is no
cancel or early finish; terminal state is exactly k distinct places. The
canonical final response contains k triples in the same explicit group/slot
order, with absolute wire player, `LOCATION_MZONE` or `LOCATION_SZONE`, and
the u8 sequence. Its body is `3*k` bytes. The core rejects duplicate places,
wrong players, wrong locations, out-of-range sequences, and blocked slots.

The only public difference between the two message IDs is the closed
`disable_field` context variant and the corresponding message family/key
prefix. No card code or locator is involved.

## 11. MSG_SELECT_COUNTER (22)

The modern message is:

    u8       message_id = 22
    u8       acting_player
    u16_le   counter_type
    u16_le   required_total = q
    u32_le   occurrence_count = n
    repeat n:
        u32_le card_code
        u8       controller
        u8       location
        u8       sequence
        u16_le   available_amount

Length is `10 + 9*n`. The source vector is the ordered wire vector after the
core's source ordering. A zero required total, no source, or a one-card source
is resolved by the core without this prompt. For an emitted prompt, each
available amount is positive and q is no greater than the total capacity.

The public context exposes counter type, q, and safe source descriptors. The
capacity is a public semantic amount; the raw card code is not. Field source
entries are correlated through the accepted I3D snapshot. This layout has no
overlay index, so overlays fail closed. A source reference that cannot be
proven uniquely fails the whole prompt.

The continuation processes source occurrences in wire order. For the next
source ordinal, `ASSIGN_AMOUNT:i:a` is present for each `a` from zero through
that occurrence's capacity that leaves an exact completion for the remaining
occurrences. Zero is a real response value and must not be omitted. There is
no greedy allocation and no separate cancel or finish action. After all n
assignments the terminal body is n `u16_le` nonnegative amounts in source
order, with exact sum q. Negative `i16` values and values above capacity fail
closed even though the core's low-level reader is signed and does not reject
every negative value itself.

## 12. MSG_SORT_CARD (25) and MSG_SORT_CHAIN (21)

Both messages use this modern layout:

    u8       message_id = 25 or 21
    u8       acting_player
    u32_le   source_count = n
    repeat n:
        u32_le card_code
        u8       controller
        u32_le   location
        u32_le   sequence

Length is `6 + 13*n`. `MSG_SORT_CHAIN` is produced by the `SortChain` process,
which delegates to the same `SortCard` response process. `MSG_SORT_CARD`
sorts the source vector as supplied by the producer. There is no overlay index
in this layout; an overlay source is rejected. A source count of zero is a
no-prompt path, and the response validator's signed-byte permutation makes a
count above 255 unproven and fail closed.

The source occurrence order is the wire order and is never reordered by the
adapter. A public `PLACE:i` candidate means “put source occurrence i at the
next destination position.” The next position is adapter-local semantic state,
not a public prompt ordinal. Every not-yet-placed source occurrence remains a
candidate. After n placements the private response body contains n `u8`
destination positions indexed by source ordinal. It is a permutation of
0..n-1. `CANCEL` is the four-byte `i32_le(-1)` response and is admitted at
every nonterminal step because the core explicitly accepts -1 to leave the
order unchanged. There is no early finish.

The public card descriptor carries the prompt-local CardCode when the source
prompt discloses one, and carries a persistent locator/accepted-snapshot
CardCode only where accepted I3D correlation proves it. Anonymous
source-occurrence descriptors preserve the sorting domain when no code is
disclosed, without publishing hidden deck identity.

## 13. MSG_ANNOUNCE_RACE (140)

The modern message is:

    u8       message_id = 140
    u8       acting_player
    u8       required_bit_count = k
    u64_le   available_race_mask

Length is exactly eleven bytes. The core validates the response as a subset
of the available mask with exactly k set bits. The admitted `RACE_ALL` values
are bits 0..32 and bit 62 (`RACE_YOKAI`). The writer clamps k to the available
popcount; zero k or an empty mask is a no-prompt path and is rejected for an
emitted I5 prompt.

The current public domain contains one `PICK:<bit_index>` candidate for every
available, not-yet-selected admitted bit that satisfies the monotonic completion
predicate in section 4. The continuation is terminal after exactly k distinct
bits; there is no finish or cancel. The final private body is one `u64_le` mask.
Unknown bits, duplicate picks, candidates that leave no monotonic completion,
and wrong popcount fail closed.

## 14. MSG_ANNOUNCE_ATTRIB (141)

The modern message is:

    u8       message_id = 141
    u8       acting_player
    u8       required_bit_count = k
    u32_le   available_attribute_mask

Length is exactly seven bytes. The admitted `ATTRIBUTE_ALL` values are
`0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40`. The writer clamps k to the
available popcount. An empty mask or zero k is a no-prompt path.

The continuation and public domain are the race rules with the u32 attribute
mask and bit indexes 0..6, including the monotonic completion predicate in
section 4. The final private body is one `u32_le` mask. No unknown bit,
duplicate pick, candidate that leaves no monotonic completion, early finish, or
cancel exists.

## 15. MSG_ANNOUNCE_NUMBER (143)

The modern message is:

    u8       message_id = 143
    u8       acting_player
    u8       option_count = n
    repeat n:
        u64_le   number_value

Length is `3 + 8*n`. The count is serialized through an u8 cast in the core;
the strict V1 source domain therefore requires `1 <= n <= 255`. A count above
255 or a trailing body caused by that cast is malformed, not truncated. The
ordinary source APIs do not emit an empty option prompt; n=0 is rejected as
an unusable prompt.

This is a flat terminal domain, not a continuation. The public candidates are
the n values in wire order, including duplicate values. The local key is
`MSG_ANNOUNCE_NUMBER:OPTION:i`. A selected option terminates with the private
`i32_le` zero-based source index. The selected u64 value is not substituted
for the response index. There is no finish or cancel candidate and N=1 still
requires external selection.

## 16. MSG_SELECT_UNSELECT_CARD (26)

The modern message is:

    u8       message_id = 26
    u8       acting_player
    u8       finishable
    u8       cancelable
    u32_le   minimum_count
    u32_le   maximum_count
    u32_le   selectable_count = a
    repeat a:
        u32_le card_code
        ModernLocInfoV1 location
    u32_le   unselectable_count = b
    repeat b:
        u32_le card_code
        ModernLocInfoV1 location

Length is `20 + 14*(a+b)`. At least one source occurrence is required for the
prompt path. The first section is the unselected/selectable source list and
the second section is the currently selected/unselectable source list. The
source order is section order followed by each section's wire order. The
core's response index is the combined index `i` in the first section or
`a+i` in the second section.

The initial flat terminal domain contains `SELECT:i` for every selectable
occurrence and `UNSELECT:i` for every unselectable occurrence. Each preserves
its own source occurrence even when the two visible values match. A response
`{u32_le 1, u32_le combined_index}` is the exact private body for either card
operation. A response of `i32_le(-1)` is legal when finishable or cancelable.

When exactly one of the two permissions is present, the public terminal
candidate is respectively `FINISH` or `CANCEL`. When both are true, the wire
has only one indistinguishable `-1` body; the public contract emits one
`FINISH_OR_CANCEL` candidate and retains both source flags in private context.
It does not fabricate two candidates with the same response. When neither is
true, no terminal candidate exists. `minimum_count` and `maximum_count` are
context constraints from the source operation; the core's prompt response
consumer does not turn them into an additional card-count selection protocol.

Addressed occurrences use the accepted I4B correlation rules. A source layout
with a valid overlay index can use the overlay path; an ambiguous or hidden
unproven reference rejects the prompt. Raw loc_info never reaches the public
candidate.

## 17. Failure atomicity and staleness

An accepted continuation instance owns its original prompt identity, current
step, prior choices, domain, and private response state. Each action must
validate:

* the instance identity;
* the current step;
* exact current local-key grammar;
* membership in the complete current domain; and
* family-specific feasibility and response constraints.

The transition is computed in value-owned temporary state and committed only
after the next complete domain and private state are valid. A failed action
does not mutate the current state, emit a response, retry under another policy,
or advance any external prompt ordinal. It invalidates the old usable handle
when the session contract requires failure invalidation. A successful action
stales every handle from the previous continuation step. A new external
prompt invalidates the entire old continuation regardless of family or textual
key similarity.

Terminal completion produces exactly one private original-protocol response
body. I5A0 itself never wraps or sends that body. No public value contains the
body, response index encoding, source bytes, or raw continuation state.

## 18. Determinism and privacy

For equal source bytes, accepted projection, perspective, and continuation
history, the future implementation must produce equal success/failure,
context type and fields, candidate count/order/types/fields, local keys,
terminal state, and final private response bytes. This must hold in independent
processes.

Semantic order comes only from explicit wire order or the explicit family
canonical order above. No pointer, object allocation, PID, wall time, thread,
socket, TCP segmentation, filesystem path, random UUID, hash iteration, or
dictionary order participates.

Paired privacy worlds with equal perspective-safe state and equal prompt-visible
semantics must produce equal public contexts, domains, transitions, local keys,
and terminal status. Hidden opponent identity, private physical continuity,
and mirror entity IDs cannot branch the public continuation. Private known
source facts may affect private response binding only through an already
authorized I3D/mirror correlation.

The public surface must not expose:

    raw ModernLocInfoV1
    raw sum_param or response bytes
    MirrorSnapshotV1 or MirrorEntityIdV1
    protocol offsets or source backing objects
    socket/session/room/password/process/PID/time data
    OCGForge public_action_key
    LogicalModelInputV1 or EncodedModelInputV1

## 19. Error taxonomy

Future runtime errors are structured and fail closed. Names may map to the
existing error enum, but their meaning is fixed:

| Error | Meaning |
| --- | --- |
| `MalformedPrompt` | length, primitive, count, endian, boolean, or trailing-byte failure |
| `UnsupportedPromptLayout` | legacy layout or unadmitted layout/source form |
| `UnsupportedPromptFamily` | a researched family that is deliberately outside the admitted V1 implementation scope |
| `UnprovenPromptSemantics` | structurally parseable but producer/legality semantics are not proven |
| `UnprovenPublicReference` | a required persistent private/public card correlation is zero or multiple, or a required overlay proof is missing |
| `InvalidContinuationInstance` | stale or unknown continuation identity |
| `StaleContinuationStep` | action belongs to a prior step or prior prompt |
| `InvalidContinuationAction` | key is malformed, not in the current complete domain, or semantically illegal |
| `InvalidFinalResponseBinding` | private state cannot serialize one exact legal response |

Every error publishes no partial domain and sends no network response.

## 20. Acceptance boundary

The intended final matrix is machine-readable in meaning:

    I4_FINAL=YES
    I5A0_TARGET_FAMILY_COUNT=12
    I5A0_SUPPORTED_CONTRACT_FAMILY_COUNT=11
    SELECT_SUM_SUPPORT=FAIL_CLOSED_UNSUPPORTED_V1
    ANNOUNCE_CARD_SUPPORT=FAIL_CLOSED_UNSUPPORTED
    I5_MESSAGE_IDS_FROZEN=PASS
    I5_MODERN_WIRE_GRAMMARS_FROZEN=PASS
    I5_RESPONSE_CODECS_FROZEN=PASS_FOR_11_ADMITTED_FAMILIES
    I5_CONTINUATION_MODEL_FROZEN=PASS
    I5_CURRENT_DOMAIN_COMPLETENESS=PASS_FOR_11_ADMITTED_FAMILIES
    I5_TERMINAL_COMPLETION_REACHABILITY=PASS_FOR_11_ADMITTED_FAMILIES
    I5_DUPLICATE_OCCURRENCE_PRESERVATION=PASS
    I5_FINISH_SEMANTICS_FROZEN=PASS
    I5_CANCEL_SEMANTICS_FROZEN=PASS
    I5_N1_AUTOANSWER=NO
    I5_INTERMEDIATE_PROTOCOL_RESPONSE=ABSENT
    I5_NETWORK_RESPONSE_SENDING=ABSENT
    I5_PUBLIC_PRIVATE_SEAM=PASS
    I5_PUBLICATION_AUTHORITY=I3D
    I5_PROMPT_LOCAL_CARD_DISCLOSURE=ACTING_PERSPECTIVE_CURRENT_PROMPT
    I5_PROMPT_LOCAL_CARD_CODE_PERSISTS=NO
    I5_PRIVATE_RESPONSE_IS_MODEL_INPUT=NO
    I5_CONTINUATION_STATE_DETERMINISTIC=PASS
    I5_UNORDERED_CANONICAL_PATHS=MONOTONIC_SOURCE_INDEXES
    I5_SELECT_TRIBUTE_BOUND_SEMANTICS=MIN_VALUE_PLUS_MAX_COUNT
    I5_LOCAL_KEY_EQUALS_OCGFORGE_PUBLIC_ACTION_KEY=NO
    SELECT_SUM_EXACT_SEMANTICS=NOT_APPLICABLE_DUE_FAIL_CLOSED
    SELECT_SUM_EXACT_ORACLE_CONTRACT=NOT_APPLICABLE_DUE_FAIL_CLOSED
    SELECT_SUM_HEURISTIC_ORACLE_ALLOWED=NO
    I6_AUTHORITY_ACQUIRED=NO
    MODEL_INPUT_AUTHORITY_ACQUIRED=NO
    NETWORK_SEND_AUTHORITY_ACQUIRED=NO

The twelve-message audit and the eleven-family contract passed independent
review. The accepted final status is:

    I5A0_CONTRACT_FREEZE=YES_FOR_11_FAMILIES
    SELECT_SUM_EXACT_SEMANTICS=NOT_APPLICABLE_DUE_FAIL_CLOSED
    SELECT_SUM_EXACT_ORACLE_CONTRACT=NOT_APPLICABLE_DUE_FAIL_CLOSED
    CONTRACT_FROZEN_FAMILY_COUNT=11
    ANNOUNCE_CARD_SUPPORT=FAIL_CLOSED_UNSUPPORTED
    I5A0_CONTRACT_FREEZE_FINAL_PASS=YES
    I5_IMPLEMENTED=NO
    I5_FINAL=NO

The following are insufficient even if green: compilation; a single family;
the pre-I5 108/108 aggregate; one deterministic process; fixture existence;
successful parsing; N=1 output; no crash; PR mergeability; or hosted CI with no
I5-specific contract/domain evidence for all eleven admitted families and the
explicit SELECT_SUM unsupported boundary. A future re-admission of SELECT_SUM
would additionally require a new source-backed exact-oracle review; that work
is not part of this contract.
