# I6C6-3A Current-Property Evidence Architecture Decision

## 1. Executive conclusion

This document decides how the remaining Current-property evidence must be
completed under the frozen independent standalone EDOPro TCP-client model. It
does not implement a query change, a counter reducer, an EDOPro patch, or an
ADR revision.

The pinned source audit yields two different results:

* Link markers cannot be reconstructed completely from the existing normal
  TCP evidence. QUERY_LINK is absent from Extra, Hand, and Grave refresh
  masks, and the pinned core applies dynamic marker effects and
  position-dependent rotation that no existing public gameplay message exposes
  completely.
* Counter evidence is present in the normal public gameplay stream.
  MSG_ADD_COUNTER and MSG_REMOVE_COUNTER are forwarded to clients, decoded by
  Ignis, and recorded by the event ledger. A complete Counter Current value can
  be reconstructed from a fresh-duel empty state plus those events and every
  public movement/reset boundary, but the current mirror does not do so.

The selected future architecture is therefore:

~~~text
FINAL_ARCHITECTURE_DECISION=
HYBRID_COUNTER_RECONSTRUCTION_PLUS_LINK_QUERY_EVIDENCE
~~~

This is an architecture decision, not implementation authorization. The
counter half is a future Ignis state-reducer slice. The Link-marker half
requires richer evidence owned by a separately authorized pinned local/private
EDOPro server change. That change is outside the current standalone-client
contract and requires an ADR-0001 revision before implementation.

The current safe status remains:

~~~text
I6C6_3A_IMPLEMENTATION_AUTHORIZED=NO
ADR_REVISION_AUTHORIZED=NO
I6C6_3_FINAL=NO
~~~

## 2. Fixed source and provenance points

| Authority | Exact point | Role |
| --- | --- | --- |
| OCGForge-Ignis base | 4cdc5290af0a0dfc5dacf8f3dde04d636bbd6e9d | Current-property contract base |
| OCGForge-Ignis main | 28f67dfdae622d16b3b040e74e8f7b78e4914661 | Integrated I6C5 reference |
| OCGForge semantic authority | 2913345ecd7bc4fffce192a1eb31b95ec354b178 | Native Current and public-state source |
| EDOPro source | 30935e847165a9ef0e547fb51a43f36168fab7c7 | Pinned external server/client |
| EDOPro ocgcore | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | Pinned runtime core |
| Separate ygopro-core reference | e747e1771fcf91dd7c53a5950f030012229e66e4 | Research-only; not used as runtime authority |

The exact Ignis source base is clean on the new branch. The existing dirty
I6C6-3 worktrees are separate and were not modified. The external EDOPro
source checkout and the separately verified ocgcore checkout are used only as
read-only source evidence.

## 3. Frozen ADR-0001 constraints

ADR-0001 at
docs/adr/ADR-0001-integration-mode.md:18-35 freezes:

~~~text
INTEGRATION_MODE=independent standalone external EDOPro network client
EDOPRO_FORK_REQUIRED=NO
EDOPRO_PLUGIN_REQUIRED=NO
DLL_INJECTION=FORBIDDEN
OCGCORE_EMBEDDING=FORBIDDEN
WINDBOT_CODE_REUSE=FORBIDDEN
~~~

The ADR records the alternatives and their rejection at lines 38-55. A
normal Ignis process may connect to a local or private EDOPro server using
CTOS/STOC, but it cannot alter the server, inject into the client, embed
ocgcore, or rely on WindBot code.

This document does not modify ADR-0001. Any server-side mask expansion is
classified separately below as ADR_REVISION_REQUIRED.

## 4. Link-marker core semantics

The pinned ocgcore source emits QUERY_LINK as a pair containing Link rating and
Link-marker bits when the requested mask includes it
(card.cpp:198-205). The dynamic implementation at card.cpp:1239-1277:

* returns zero for a facedown on-field card;
* honors ASSUME_LINKMARKER;
* returns zero for a non-Link current type;
* applies EFFECT_ADD_LINKMARKER;
* applies EFFECT_REMOVE_LINKMARKER;
* applies EFFECT_CHANGE_LINKMARKER; and
* rotates the marker bits when the card is in defense position on the field.

The source therefore separates:

~~~text
static printed marker bits
vs
dynamic Current.link_markers
~~~

Printed.link_markers cannot be used as a general Current fallback.

AssumeProperty is not a persistent value at the external query boundary. It
stores the assumption in the card map at libcard.cpp:2117-2124, and the core
restores assumptions at outer interpreter completion
(interpreter.cpp:365-372 and duel.cpp:113-117). A future derivation may rely
on the post-Lua query boundary only after this lifecycle premise is evidenced.

## 5. Existing TCP and query-evidence audit

### 5.1 CTOS/STOC inventory

The pinned EDOPro network inventory defines CTOS_RESPONSE,
CTOS_UPDATE_DECK, handshake/lobby packets, CTOS_SURRENDER,
CTOS_TIME_CONFIRM, CTOS_CHAT, and rematch/lobby packets
(gframe/network.h:274-292). The STOC inventory contains game, selection,
duel, replay, time-limit, chat, lobby, and catch-up packets
(gframe/network.h:294-320).

There is no CTOS query, refresh, query-mask, or card-state request. The server
handler at netserver.cpp:302-440 dispatches the listed CTOS families and has
no arbitrary query-mask handler. The only normal client-to-server gameplay
submission is CTOS_RESPONSE, plus the non-gameplay/control packets.

Therefore:

~~~text
CLIENT_CAN_REQUEST_ARBITRARY_QUERY_MASK=NO
NORMAL_TCP_CLIENT_QUERY_EXPANSION_PATH=NONE
~~~

No undocumented request is admitted.

### 5.2 Existing server refresh sources

The GenericDuel declarations at generic_duel.h:42-52 define the refresh
defaults. The helpers at generic_duel.cpp:1354-1368 call RefreshLocation,
which queries ocgcore and emits MSG_UPDATE_DATA for the owner and a filtered
public version for the other recipient (1369-1395).

The normal masks are:

| Source | Mask | Link-marker evidence |
| --- | --- | --- |
| RefreshExtra | 0x00381fff | QUERY_LINK absent |
| RefreshHand | 0x03781fff | QUERY_LINK absent |
| RefreshGrave | 0x00381fff | QUERY_LINK absent |
| RefreshMzone | 0x03981fff | QUERY_LINK present |
| RefreshSzone | 0x03f81fff | QUERY_LINK present |
| RefreshSingle | 0x03f81fff | QUERY_LINK present |
| PseudoRefreshDeck | 0x01181fff | replay-only; QUERY_LINK absent |

The mechanically verified query constants are:

~~~text
QUERY_LINK   = 0x00800000
QUERY_LSCALE = 0x00200000
QUERY_RSCALE = 0x00400000
QUERY_COUNTERS = 0x00020000
QUERY_STATUS = 0x00080000
~~~

The exact RefreshExtra result is:

~~~text
REFRESH_EXTRA_MASK=0x00381fff
QUERY_LINK_INCLUDED=NO
~~~

### 5.3 Existing gameplay message sources

The EDOPro server's AfterParsing paths refresh state after the following
messages (generic_duel.cpp:1143-1257):

| Message family | Existing evidence |
| --- | --- |
| MSG_MOVE | Message locators plus RefreshSingle when the destination is eligible |
| MSG_POS_CHANGE | Position transition plus RefreshSingle when a facedown card becomes face-up |
| MSG_SET | Public/owner-specific move/set payload; no marker bits |
| MSG_FLIPSUMMONING | RefreshSingle; no marker bits in the message |
| MSG_SUMMONING / MSG_SPSUMMONING / MSG_FLIPSUMMONED | Event/locator state; no marker bits |
| MSG_EQUIP / target / chain families | Relationship or chain payload; no marker bits |
| MSG_UPDATE_DATA | Only the fields contained in the selected refresh mask |
| MSG_UPDATE_CARD | Only the fields contained in the selected RefreshSingle mask |

The pinned EDOPro adapter contains Link-marker storage and query parsing in
client_card.cpp and core_utils.cpp, but no separate gameplay message payload
for Link-marker bits. A source search across the pinned gframe tree finds
QUERY_LINK only in query parsing/generation and ClientCard update code, not in
Move, position, summon, equip, target, chain, or other message payloads.

The conclusions for candidate sources are:

| Candidate | Result | Reason |
| --- | --- | --- |
| Existing QUERY_LINK records | PARTIAL | Present only on selected field/single masks |
| Other existing STOC/GAME_MSG fields | INSUFFICIENT | No marker-bit payload exists outside query records |
| Public event-history reconstruction | INSUFFICIENT | Events expose transitions, not marker-changing effects |
| Public Current/static reconstruction | INSUFFICIENT | Dynamic marker effects and rotation are not determined by Printed data |
| Later normal refresh | REJECTED | No guarantee before the selected comparison boundary |
| CTOS richer-query request | NONE | No request family or handler exists |
| Other public server response | NONE | No separate marker response exists |

Accordingly:

~~~text
LINK_MARKER_EXISTING_TCP_SOURCE_AUDITED=PASS
LINK_MARKER_EXISTING_TCP_EVIDENCE=PARTIAL
~~~

## 6. Link-marker dynamic-modifier matrix

| Modifier or premise | Existing public evidence | Classification |
| --- | --- | --- |
| TYPE_LINK gate | QUERY_TYPE where identity/current data is public | DIRECT_EVENT_EVIDENCE |
| facedown on-field result | Position in MSG_MOVE/MSG_POS_CHANGE or query; core returns zero | DERIVABLE_FROM_PUBLIC_STATE |
| ASSUME_LINKMARKER | Cleared after the outermost Lua call; no persistent query-boundary value | DERIVABLE_FROM_PUBLIC_STATE with lifecycle gate |
| EFFECT_ADD_LINKMARKER | No public effect or marker-delta message; value appears only through QUERY_LINK | NOT_OBSERVABLE |
| EFFECT_REMOVE_LINKMARKER | No public effect or marker-delta message; value appears only through QUERY_LINK | NOT_OBSERVABLE |
| EFFECT_CHANGE_LINKMARKER | No public effect or marker-delta message; value appears only through QUERY_LINK | NOT_OBSERVABLE |
| current position rotation | Position is public where the entity is public | DERIVABLE_FROM_PUBLIC_STATE only when base marker bits are available |
| marker ordering/projection | Native and Ignis map bits only after marker value exists | PROVEN only after Link evidence |

At least one supported legal dynamic modifier is not observable through the
existing TCP evidence. Reconstructing the marker vector from events would
require simulating internal ocgcore effects or reading hidden runtime state,
both of which are forbidden.

~~~text
LINK_MARKER_DYNAMIC_MODIFIER_MATRIX=PASS
LINK_MARKER_EVENT_RECONSTRUCTION=INSUFFICIENT
~~~

## 7. Client-request capability audit

The CTOS and STOC inventories at gframe/network.h:274-320 contain no
query-state request. The netserver.cpp:302-440 dispatcher has no handler that
accepts a query mask, and CTOS_RESPONSE is the only normal gameplay response
that can advance a duel. A normal TCP client therefore cannot ask the server
to rerun DuelQueryLocation with an arbitrary mask.

~~~text
CLIENT_CAN_REQUEST_ARBITRARY_QUERY_MASK=NO
NORMAL_TCP_CLIENT_QUERY_EXPANSION_PATH=NONE
~~~

The absence is architectural, not a permission failure. A future client
cannot turn QUERY_LINK or QUERY_COUNTERS on by sending an undocumented packet.
The only direct-query option identified by the pinned source is a change in
the server-owned GenericDuel refresh masks.

## 8. Later-refresh and boundary analysis

The pinned GenericDuel paths at generic_duel.cpp:1143-1257 refresh different
locations at different message boundaries. RefreshSingle uses a rich mask, but
RefreshExtra, RefreshGrave, and RefreshHand use masks without QUERY_LINK.
PseudoRefreshDeck writes replay data and is not a client query response.

There is no source guarantee that a later RefreshSingle or another
QUERY_LINK-bearing refresh will occur before the selected I6C6 public-event
prefix and post-prefix state boundary. A later refresh may be absent, may
follow another gameplay mutation, or may refer to a different visibility
condition. It therefore cannot repair the missing value without changing the
meaning of the comparison boundary.

~~~text
LATER_REFRESH_REPAIR=REJECTED
BOUNDARY_DODGE=REJECTED
~~~

The future bridge must compare the selected supported boundary, including
Link and Pendulum cases. It may not remove those cases or move the boundary
only until the missing field is no longer visible.

## 9. Counter event source

The pinned ocgcore counter implementation is public at card.cpp:2220-2277.
Adding and removing counters update the card counter map and emit
MSG_ADD_COUNTER and MSG_REMOVE_COUNTER. The payload is counter type,
controller, location, sequence, and count. The GenericDuel server has no
counter-specific suppression branch; its default message forwarding at
generic_duel.cpp:1133-1137 sends the gameplay message to clients.

The pinned EDOPro client recognizes both messages at duelclient.cpp:3712-3749.
Ignis decodes them in ModernQueryDecoderV1.cs:945-966 and represents their
typed payload in GameplayMessageTypesV1.cs:364-369. The event ledger records a
public CounterChanged event at PerspectiveSafeEventLedgerV1.cs:653-685.
PerspectiveStateMirrorV1.cs:156-174 currently treats both messages as a
state no-op, so evidence is present but not integrated.

This distinction is normative:

~~~text
COUNTER_QUERY_SOURCE=ABSENT
COUNTER_EVENT_SOURCE=PRESENT
RUNTIME_EVIDENCE_ABSENT=NO
RUNTIME_EVIDENCE_PRESENT_BUT_NOT_INTEGRATED=YES
~~~

Counter events contain no passcode and do not require reverse lookup. They can
be used only for a known public entity or an otherwise valid public
controller/location/sequence association. If the association is unavailable,
the future reducer must fail closed.

## 10. Counter reset and mutation matrix

The counter map is not equivalent to an add/remove event accumulator. The
core clears it on reset masks in card.cpp:2010-2013, while other paths emit
explicit removals. The future reducer must apply the exact message order and
reset boundary, rather than infer a reset from a missing event.

| Reset or mutation | Core counter consequence | Explicit counter message | Other public GAME_MSG | Entity identity association | Exact reconstruction |
| --- | --- | --- | --- | --- | --- |
| Add counter | Increment or create the typed counter entry | MSG_ADD_COUNTER | None required | Controller/location/sequence payload | YES for a resolvable public entity |
| Remove counter | Decrement/remove the typed entry | MSG_REMOVE_COUNTER | None required | Controller/location/sequence payload | YES for a resolvable public entity |
| RemoveAllCounters | Emits a removal for each current counter, then clears | MSG_REMOVE_COUNTER per entry | None required | Current public entity | YES |
| reset permission or effect state | Removes affected counters in the core reset path | Explicit removals from card.cpp:1877-1884 | The surrounding effect/message boundary | Current public entity | YES when the removal messages are in the replay |
| reset disable state | Removes counters in the disable reset path | Explicit removals from card.cpp:2027-2040 | Surrounding public message | Current public entity | YES when the removal messages are in the replay |
| TODECK | Clears counters without a matching remove event | No guaranteed remove | MSG_MOVE; shuffle/reindex messages may follow | Source entity must be resolved before knowledge destruction | YES for tracked source; otherwise fail closed |
| TOHAND | Clears counters without a matching remove event | No guaranteed remove | MSG_MOVE | Source entity must be resolved before move | YES for tracked source; otherwise fail closed |
| TOGRAVE | Clears counters without a matching remove event | No guaranteed remove | MSG_MOVE | Source entity must be resolved before move | YES for tracked source; otherwise fail closed |
| REMOVE | Clears counters without a matching remove event | No guaranteed remove | MSG_MOVE | Source entity must be resolved before move | YES for tracked source; otherwise fail closed |
| TEMP_REMOVE | Clears counters without a matching remove event | No guaranteed remove | MSG_MOVE | Source entity must be resolved before move | YES for tracked source; otherwise fail closed |
| OVERLAY | Clears the moved card counters without a matching remove event | No guaranteed remove | MSG_MOVE or overlay transition | Source entity must be resolved before overlay | YES for tracked source; otherwise fail closed |
| MSCHANGE | Clears counters without a matching remove event | No guaranteed remove | MSG_MOVE | Source entity must be resolved across the zone change | YES for tracked source; otherwise fail closed |
| TOFIELD | Clears counters without a matching remove event | No guaranteed remove | MSG_MOVE | Source entity must be resolved before move | YES for tracked source; otherwise fail closed |
| TURN_SET | Clears counters without a matching remove event | No guaranteed remove | MSG_POS_CHANGE or the enclosing public transition | Entity must be resolved at the transition | YES for tracked source; otherwise fail closed |
| CONTROL | Does not belong to the counter-clear mask; counters are preserved | No counter message required | Control/move public transition | Existing entity association follows the public controller/location change | YES for tracked public entity |
| destruction or card leave field | Uses the applicable destination/reset class | Depends on reset path | MSG_MOVE and enclosing destruction/move messages | Clear before any knowledge-destroying replacement | YES for tracked source; otherwise fail closed |

The reset rows are grounded in card.cpp:2010-2013 and the public movement
paths at operations.cpp:4793-4810, 4547-4563, and 1001-1012. Position and
turn-set behavior is represented by the public paths at operations.cpp:5278-
5312. Control paths at libduel.cpp:1142, 1300-1305, and 1414-1431 do not
clear counters, so a control change is an association update, not a counter
reset.

The table deliberately distinguishes a missing remove event from an empty
counter vector. A future reducer must clear state on the public movement
boundary when the source entity is known, and must never carry a counter
across a knowledge-destroying transition.

~~~text
COUNTER_EVENT_SOURCE_AUDITED=PASS
COUNTER_RESET_MATRIX_COMPLETE=PASS
~~~

## 11. Counter initial state

Fresh card objects start with an empty counter map. The creation path in
ocgapi.cpp:62-83, duel.cpp:75-82, and card.cpp:91-94 establishes this for a
fresh duel. The selected counter reconstruction therefore has a proven
initial condition only when the replay begins at fresh-duel construction or
at an equivalent directly evidenced empty-counter bootstrap.

An arbitrary mid-duel transcript has no such premise. It must not be accepted
by the event reducer without a counter bootstrap source.

~~~text
COUNTER_INITIAL_STATE_PROVEN=YES
COUNTER_INITIAL_STATE_SCOPE=FRESH_DUEL_ONLY
~~~

## 12. Counter locator and identity association

The counter payload's controller, location, and sequence are the same
perspective-safe address dimensions used by Ignis. MirrorAddressNormalizationV1.cs:3-60
and PerspectiveStateMirrorV1.cs:2395-2415 provide deterministic mapping
without pointers, passcodes, or runtime object identity.

The association is valid only if the address resolves to the currently
tracked public entity at the message ordinal. A counter payload that points
to no entity, an invalid location, an ambiguous address, or a
knowledge-destroyed hidden object is not repaired by searching prior state.
The reducer must return a structured failure and leave the mirror unchanged.

PZONE and other SZONE public event projections may intentionally omit a
stable ledger locator, as documented at PerspectiveSafeEventLedgerV1.cs:725-
736. That does not authorize a guessed locator: the future state reducer may
use the typed protocol controller/location/sequence only when the mirror
state proves the corresponding public entity. If it cannot, the Current
counter value is unavailable for that boundary.

~~~text
COUNTER_LOCATOR_ASSOCIATION=PROVEN
COUNTER_LOCATOR_FAILURE=FAIL_CLOSED
~~~

## 13. Counter reconstruction decision

A complete future counter source is available through event reconstruction,
subject to the fresh-duel initial condition, complete supported transcript,
exact reset matrix, and public entity association above. This is not a claim
that the current Ignis mirror already reconstructs counters.

~~~text
COUNTER_STATE_SOURCE=COMPLETE_EVENT_RECONSTRUCTION
~~~

The owning implementation is a PerspectiveStateMirrorV1 successor or
state-reducer component within Ignis. The event ledger remains the
public-history projection; it is not the authoritative Current-state owner.
The reducer must process counter messages and movement/reset transitions in
protocol order, use checked arithmetic, reject malformed counts and
addresses, clear before knowledge-destroying replacement, and commit
atomically.

Required future acceptance evidence includes:

* fresh-duel empty counters for both perspectives;
* multiple counter types and multiple increments;
* partial and complete removals;
* RemoveAllCounters and effect/disable reset removals;
* every destination reset class in the matrix;
* control changes preserving counters;
* same-zone moves, overlay, re-entry, and address reindexing;
* both acting perspectives;
* malformed counter payload and unresolved-address zero-mutation failures;
* hidden opponent entities never queried or reconstructed;
* Current counter equality against native public evidence where available;
* paired hidden worlds and fresh-process deterministic replay.

Until that reducer is integrated, counters remain unavailable at the current
Ignis frame boundary and I6C6 must not claim equality for a scenario that
requires them.

## 14. Query-expansion architecture

The minimum direct-evidence change for Link markers is owned by the pinned
EDOPro GenericDuel server path. The RefreshLocation and RefreshSingle helpers
at generic_duel.cpp:1354-1437 choose the masks passed to DuelQueryLocation or
DuelQuery. RefreshMzone, RefreshSzone, and RefreshSingle already include
QUERY_LINK. RefreshExtra, RefreshGrave, and RefreshHand do not.

The narrow future server change would add QUERY_LINK to the relevant
RefreshExtra, RefreshGrave, and RefreshHand masks, while preserving the
existing owner/private query and public filtered-query split. It must also
cover every other public comparison-boundary refresh that can carry a public
Link entity. This is a server-side EDOPro change, not a CTOS client option and
not an Ignis query setting.

QUERY_COUNTERS would be the corresponding direct-query option for counters,
but the event stream already provides a narrower source. Selecting a
counter-query patch would add mask coverage, privacy obligations, and
association checks without removing the need to handle movement resets. It is
not the selected counter architecture.

The query owner and minimum change are therefore:

~~~text
QUERY_EXPANSION_OWNER=
EDOPro GenericDuel server-side RefreshLocation/RefreshSingle mask patch

MINIMUM_LINK_QUERY_CHANGE=add QUERY_LINK to relevant missing public refresh masks
MINIMUM_COUNTER_QUERY_CHANGE=add QUERY_COUNTERS to all relevant refresh masks
SELECTED_COUNTER_QUERY_CHANGE=NONE
~~~

Changing GenericDuel, the EDOPro server, or its pinned runtime is outside the
frozen standalone-client architecture. It requires a separately accepted
ADR-0001 revision. No query mask is changed by this document.

The future ADR question is narrow:

~~~text
May one explicitly pinned local/private EDOPro server patch expand
perspective-safe query masks for oracle/model-state evidence without changing
legality, decisions, RNG, deck state, response handling, or public-server
operation?
~~~

That ADR must bind authority, patch provenance, privacy tests, replay
determinism, compatibility, licensing, distribution restrictions, CI
reproducibility, and rules-equivalence evidence. It must not authorize a
public-server modification by implication.

## 15. Privacy analysis

The pinned query path creates an owner query and a public filtered query in
GenericDuel::RefreshLocation and RefreshSingle. core_utils.cpp:143-232
constructs only requested fields and applies the public/private visibility
rules; core_utils.cpp:350-358 emits the filtered public buffer. This proves
the existing split, but it does not by itself prove that a newly expanded
mask is safe for every hidden state.

For the existing TCP event architecture:

| Evidence path | Privacy result |
| --- | --- |
| QUERY_LINK on an already public face-up card | Candidate direct source; public filtering still required |
| QUERY_LINK on a hidden hand, deck, or face-down card | Must remain identity-free and cannot be admitted without recipient-specific tests |
| MSG_ADD_COUNTER or MSG_REMOVE_COUNTER | Payload has no passcode; association is allowed only for a proven public tracked entity |
| Printed provider | Never queried for an unknown or redacted identity |
| Prior hidden entity state | Forbidden as a reconstruction source |

A richer query mask may expose dynamic current modifiers or other fields that
are not safe for a hidden recipient even when the query record itself is
well-formed. The public buffer must be tested for code, type, Link data,
scales, status, counters, owner/controller, and all face-down/hidden zone
cases. Until those tests exist:

~~~text
QUERY_EXPANSION_PRIVACY=REQUIRES_ADDITIONAL_EVIDENCE
~~~

The future counter reducer must use the typed counter payload only after the
public address resolves. It must not enumerate hidden cards, use provider
coverage to infer a hidden identity, or preserve a private entity across a
knowledge-destroying transition.

## 16. Determinism and replay

Counter reconstruction is deterministic when all of the following are bound:

* the pinned runtime and scenario;
* fresh-duel empty counter state;
* the ordered public gameplay message stream;
* the exact counter payload bytes;
* the exact movement/reset semantics;
* the public controller/location/sequence address;
* the selected post-transition boundary.

The reducer must use protocol/message ordinal order, checked arithmetic, and
explicit vectors. It must not use dictionary iteration, pointers, timing,
socket arrival timing, process identity, paths, or machine identity.

Link query evidence must be bound to the separately pinned server patch and
the selected public refresh boundary. Its runtime/provenance identity is not
part of gameplay semantic identity. Only the recipient-filtered public query
or the typed public state derived from it may enter the comparison.

No raw query payload containing hidden information may become a public
comparison digest. A missing, reordered, truncated, or ambiguous public
message sequence fails closed. Fresh-process replay must reproduce the same
normalized Current value and the same semantic comparison result.

The design preserves the three distinct states:

~~~text
SEMANTICALLY_ABSENT
EVIDENCE_AVAILABLE(value)
EVIDENCE_UNAVAILABLE
~~~

EVIDENCE_UNAVAILABLE is not a null/zero/empty default and is not a
semantic-absence marker.

## 17. Complete Current-property matrix

The native Current authority is the fifteen-field CardProperties set in
observed_card.hpp:30-45 and set_current_card_properties at
card_projection.cpp:71-100. OCGForge public/model projection treats Current
and Printed as separate properties. All fifteen fields reach the model-facing
logical and encoded paths at logical_model_input.hpp:64-118 and
encoded_model_input.cpp:1208-1230, 1281-1304, subject to the public
identity/visibility gate.

| Current field | Native authority | Existing EDOPro evidence | Semantic absence condition | Current admissible classification | Future route |
| --- | --- | --- | --- | --- | --- |
| type | RawCardQuery type, QUERY_TYPE | Generic refresh masks include QUERY_TYPE for public records | No Current value for hidden/redacted identity | DIRECT_PUBLIC_RUNTIME_SOURCE | Preserve recipient filtering |
| attribute | RawCardQuery attribute, QUERY_ATTRIBUTE | Generic refresh masks include QUERY_ATTRIBUTE for public records | No Current value for hidden/redacted identity | DIRECT_PUBLIC_RUNTIME_SOURCE | Preserve recipient filtering |
| race | RawCardQuery race, QUERY_RACE | Generic refresh masks include QUERY_RACE for public records | No Current value for hidden/redacted identity | DIRECT_PUBLIC_RUNTIME_SOURCE | Preserve recipient filtering |
| attack | RawCardQuery attack, QUERY_ATTACK | Generic refresh masks include QUERY_ATTACK for public records | No Current value for hidden/redacted identity | DIRECT_PUBLIC_RUNTIME_SOURCE | Compare optional presence and value |
| defense | RawCardQuery defense, QUERY_DEFENSE | Generic refresh masks include QUERY_DEFENSE | Native projection omits it for Link type | DIRECT_PUBLIC_RUNTIME_SOURCE | Preserve Link absence |
| base_attack | RawCardQuery base attack, QUERY_BASE_ATTACK | Generic refresh masks include QUERY_BASE_ATTACK | No Current value for hidden/redacted identity | DIRECT_PUBLIC_RUNTIME_SOURCE | Compare optional presence and value |
| base_defense | RawCardQuery base defense, QUERY_BASE_DEFENSE | Generic refresh masks include QUERY_BASE_DEFENSE | Native projection omits it for Link type | DIRECT_PUBLIC_RUNTIME_SOURCE | Preserve Link absence |
| level | RawCardQuery level, QUERY_LEVEL | Generic refresh masks include QUERY_LEVEL | Non-XYZ/non-Link projection rules may omit it when type does not select Level | DIRECT_PUBLIC_RUNTIME_SOURCE | Apply native type mapping |
| rank | RawCardQuery rank, QUERY_RANK | Generic refresh masks include QUERY_RANK | Non-XYZ projection rules may omit it | DIRECT_PUBLIC_RUNTIME_SOURCE | Apply native type mapping |
| link_rating | RawCardQuery link rating, QUERY_LINK; core non-MZONE rule at card.cpp:989-1030 | Direct on QUERY_LINK-bearing MZONE/SZONE/Single paths; absent from Extra/Hand/Grave defaults | Non-Link or STATUS_NO_LEVEL yields the native zero/absence representation | PROVEN_BOUNDARY_DERIVATION | Use data.type, status, assumption lifetime, data.level outside MZONE; direct query in MZONE |
| link_markers | RawCardQuery Link-marker pair, QUERY_LINK; dynamic core at card.cpp:1239-1277 | Direct only on QUERY_LINK-bearing paths | Non-Link/facedown result is semantically zero/absent only when the native projection proves that condition | UNAVAILABLE_AT_BOUNDARY | Requires direct query expansion or another complete public source |
| left_scale | RawCardQuery left scale, QUERY_LSCALE; core rule at card.cpp:1185-1208 | Direct in PZONE-capable SZONE/Single paths; missing in Extra/Grave/MZONE defaults | Non-Pendulum projection omits scales | PROVEN_BOUNDARY_DERIVATION | Outside PZONE use bound static data only under the frozen proof |
| right_scale | RawCardQuery right scale, QUERY_RSCALE; core rule at card.cpp:1209-1237 | Direct in PZONE-capable SZONE/Single paths; missing in Extra/Grave/MZONE defaults | Non-Pendulum projection omits scales | PROVEN_BOUNDARY_DERIVATION | Outside PZONE use bound static data only under the frozen proof |
| status_flags | RawCardQuery status, QUERY_STATUS | All normal public refresh masks include QUERY_STATUS | No Current value for hidden/redacted identity | DIRECT_PUBLIC_RUNTIME_SOURCE | Preserve query presence and public filtering |
| counters | RawCardQuery counters, QUERY_COUNTERS; core events at card.cpp:2220-2277 | QUERY_COUNTERS absent from normal masks; Add/Remove events are public | No Current value for hidden/redacted identity | EVIDENCE_UNAVAILABLE at current Ignis boundary | Future complete event reconstruction |

The classifications are boundary- and visibility-aware. A property that is
semantically absent must compare as absent. A property whose source is missing
at the selected boundary must remain EVIDENCE_UNAVAILABLE. In particular:

~~~text
Printed -> Current without a field-specific proof = FORBIDDEN
omitted query field -> unchanged value without a field-specific proof = FORBIDDEN
unavailable Current field -> null/zero/empty/Printed fallback = FORBIDDEN
~~~

Counters have a proven future event route but are not yet integrated into the
current Ignis mirror. Link markers have no proven existing TCP route. Neither
fact permits the comparator to suppress a model-facing field.

## 18. Static derivation owner

The already accepted derivations for Link rating outside MZONE and scales
outside PZONE belong in I6C6 test-only Current evidence normalization. The
normalizer may use the field-specific core proof, the public type/status and
the immutable Printed-source semantic identity only under the exact
boundary conditions stated by that proof.

This is not a general Current-from-Printed rule. It does not apply to Link
markers, counters, attack, defense, status, or any dynamic property without
its own proof. Production PerspectiveSafeFrame generation remains the owner
of actual runtime state and is not changed merely to satisfy an oracle.

~~~text
DERIVATION_OWNER=I6C6 test-only Current evidence normalization
~~~

## 19. Current-field availability owner

The three-state availability required for I6C6 belongs to the test/evidence
normalization boundary, where native public rows and Ignis public frames are
converted into one typed comparison input. The existing production mirror
continues to own the state it can legitimately observe; this task does not
add a generic availability abstraction to production.

The future counter reducer is a separate production-state capability and may
publish a counter value only after its own fail-closed evidence checks. The
future Link query path is a server/runtime capability, not a Printed-provider
feature.

~~~text
CURRENT_FIELD_AVAILABILITY_OWNER=I6C6 test-only evidence normalization
~~~

## 20. Architecture option comparison

The options are evaluated against the pinned source and the frozen
standalone-client architecture. This is qualitative architecture evidence;
no performance claim is made.

| Option | Correctness | Privacy | Determinism | Replayability | ADR-0001 compatibility | Maintenance |
| --- | --- | --- | --- | --- | --- | --- |
| A. Existing TCP evidence only | Reject: Link-marker effects are not observable | Good for existing messages | Good for existing messages | Good only for a reduced field set | YES | Low, but leaves an unprovable model field |
| B. Hybrid counter reconstruction plus Link query evidence | Accept as target: counters have a complete public event/reset route; Link receives direct evidence | Counter path is narrow; Link patch needs recipient tests | Strong when message order and patch identity are bound | Counter replay is explicit; Link query is boundary-bound | NO for the Link half | Narrowest complete route |
| C. Query evidence for both | Technically possible only through server mask changes | Requires both Link and counter privacy proof | Strong after patch binding | Direct query replay plus movement handling | NO | Broader than required; movement/reset semantics still matter |
| D. Remain blocked | Safe current posture | Strong | Strong | No false equivalence | YES | No progress and no implementation authorization |

Option A cannot satisfy the full Current matrix. Option C is not needed for
counters because the public event source is sufficient in principle. Option D
describes the current operational status until the authorized future slices
are completed; it is not the selected architecture for the eventual
capability.

## 21. Final architecture decision

The existing standalone TCP evidence is sufficient for the counter half after
a future Ignis state-reducer implementation. It is not sufficient for exact
Current.link_markers because the pinned server does not place QUERY_LINK in
all required refresh paths and exposes no compensating public marker delta.

The final architecture decision is:

~~~text
FINAL_ARCHITECTURE_DECISION=
HYBRID_COUNTER_RECONSTRUCTION_PLUS_LINK_QUERY_EVIDENCE
ADR_REVISION_REQUIRED=YES
~~~

The Link portion requires a future, separately pinned local/private EDOPro
server change. Under ADR-0001 that is not a normal standalone client
capability. The query expansion must remain outside the public repository
runtime and must not be treated as an Ignis production protocol feature until
its new ADR is accepted.

The counter portion is a future Ignis reducer. It does not require a query
mask change, a provider change, or a server patch. It remains fail-closed
until the reducer proves the full reset and identity matrix.

## 22. Exact next-slice decomposition

The next work is deliberately separated:

~~~text
NEXT_SLICE_A=I6C6_3A1_COUNTER_STATE_RECONSTRUCTION
NEXT_SLICE_B=I6C6_3A2_LINK_MARKER_EVIDENCE_ARCHITECTURE
~~~

I6C6-3A1 may modify only the narrowly authorized Ignis state-reducer and
test/evidence owners after a separate implementation authorization. It must
not change Printed semantics or infer hidden identities.

I6C6-3A2 first requires an ADR-0001 revision decision for a pinned
local/private EDOPro server mask patch. If that ADR is accepted, the later
implementation must change the server-owned refresh masks, preserve
recipient-specific filtering, bind the patch separately from gameplay
identity, and add privacy/replay evidence. If the ADR is rejected, Link
markers remain unavailable for the selected I6C6 boundary and the comparison
must remain UNPROVEN.

Neither slice authorizes I6C6-4, I6C6-5, I6D, I7, provider changes, real card
data, public-server automation, or third-party acquisition.

## 23. Acceptance gates

The following gates are satisfied by this source-backed architecture audit:

~~~text
LINK_MARKER_EXISTING_TCP_SOURCE_AUDITED=PASS
LINK_MARKER_DYNAMIC_MODIFIER_MATRIX=PASS
CLIENT_QUERY_REQUEST_CAPABILITY_AUDITED=PASS
LATER_REFRESH_REPAIR_AUDITED=PASS

COUNTER_EVENT_SOURCE_AUDITED=PASS
COUNTER_RESET_MATRIX_COMPLETE=PASS
COUNTER_INITIAL_STATE_AUDITED=PASS
COUNTER_LOCATOR_ASSOCIATION_AUDITED=PASS

QUERY_EXPANSION_OWNER_AUDITED=PASS
ADR_0001_COMPATIBILITY_AUDITED=PASS
QUERY_EXPANSION_PRIVACY_AUDITED=PASS

STATIC_DERIVATION_OWNER_DECIDED=PASS
CURRENT_FIELD_AVAILABILITY_OWNER_DECIDED=PASS
~~~

The privacy gate being audited as PASS means that the required question and
existing recipient filtering were located and classified. The future
expanded-mask implementation itself remains
REQUIRES_ADDITIONAL_EVIDENCE, as recorded above; no privacy authorization is
implied.

## 24. Final status

~~~text
OCGFORGE_CURRENT_FIELD_SET_AUDITED=PASS
PUBLIC_CURRENT_FIELD_MATRIX=PASS
COUNTER_ARCHITECTURE=COMPLETE_EVENT_RECONSTRUCTION
LINK_MARKER_ARCHITECTURE=QUERY_EVIDENCE_REQUIRED

CLIENT_CAN_REQUEST_ARBITRARY_QUERY_MASK=NO
NORMAL_TCP_CLIENT_QUERY_EXPANSION_PATH=NONE
LINK_MARKER_EXISTING_TCP_EVIDENCE=PARTIAL
LINK_MARKER_EVENT_RECONSTRUCTION=INSUFFICIENT
QUERY_EXPANSION_PRIVACY=REQUIRES_ADDITIONAL_EVIDENCE

FINAL_ARCHITECTURE_DECISION=
HYBRID_COUNTER_RECONSTRUCTION_PLUS_LINK_QUERY_EVIDENCE
ADR_REVISION_REQUIRED=YES

I6C6_3_CAN_RESUME_WITH_EXISTING_EVIDENCE=NO
I6C6_3A_IMPLEMENTATION_AUTHORIZED=NO
ADR_REVISION_AUTHORIZED=NO
I6C6_3_FINAL=NO
I6C6_4_AUTHORIZED=NO
I6C6_5_AUTHORIZED=NO
I6D_AUTHORIZED=NO
I7_AUTHORIZED=NO
~~~

This document is a capability and architecture decision only. It does not
claim that the current Ignis runtime can yet compare counters or Link
markers, and it does not claim that a future EDOPro patch is authorized.

## 25. Reproducibility commands

The audit used exact-head, read-only source inspection. The essential
commands were:

~~~powershell
git -C C:\Users\chris\.config\superpowers\worktrees\OCGForge-Ignis\i6c6-3a-current-property-evidence-architecture rev-parse HEAD
git -C C:\Users\chris\.config\superpowers\worktrees\OCGForge-Ignis\i6c6-3a-current-property-evidence-architecture status --short
git -C C:\Users\chris\.config\superpowers\worktrees\OCGForge-Ignis\i6c6-3a-current-property-evidence-architecture show 4cdc5290af0a0dfc5dacf8f3dde04d636bbd6e9d:docs/contracts/i6c6-current-property-observability-contract.md
git -C C:\yogiohML show 2913345ecd7bc4fffce192a1eb31b95ec354b178:src/observation/card_projection.cpp
git -C C:\yogiohML show 2913345ecd7bc4fffce192a1eb31b95ec354b178:include/ygo/observation/observed_card.hpp
git -C C:\yogiohML show 2913345ecd7bc4fffce192a1eb31b95ec354b178:src/model/encoded_model_input.cpp
git -C C:\Users\chris\AppData\Local\Temp\edopro rev-parse HEAD
git -C C:\Users\chris\AppData\Local\Temp\edopro show 30935e847165a9ef0e547fb51a43f36168fab7c7:gframe/generic_duel.h
git -C C:\Users\chris\AppData\Local\Temp\edopro show 30935e847165a9ef0e547fb51a43f36168fab7c7:gframe/generic_duel.cpp
git -C C:\Users\chris\AppData\Local\Temp\edopro show 30935e847165a9ef0e547fb51a43f36168fab7c7:gframe/network.h
git -C C:\Users\chris\AppData\Local\Temp\edopro show 30935e847165a9ef0e547fb51a43f36168fab7c7:gframe/netserver.cpp
git -C C:\Users\chris\AppData\Local\Temp\edopro show 30935e847165a9ef0e547fb51a43f36168fab7c7:gframe/ocgapi_constants.h
git -C C:\Users\chris\AppData\Local\Temp\edopro-ocgcore-i6c5-source-audit-46779 rev-parse HEAD
git -C C:\Users\chris\AppData\Local\Temp\edopro-ocgcore-i6c5-source-audit-46779 show 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57:card.cpp
rg -n "QUERY_LINK|RefreshExtra|RefreshHand|RefreshMzone|RefreshSzone|RefreshSingle|MSG_ADD_COUNTER|MSG_REMOVE_COUNTER" C:\Users\chris\AppData\Local\Temp\edopro C:\Users\chris\AppData\Local\Temp\edopro-ocgcore-i6c5-source-audit-46779
~~~

The numeric mask decomposition was performed from the pinned constants, not
by visual inclusion. The relevant result is:

~~~text
REFRESH_EXTRA_MASK_HEX=0x00381fff
REFRESH_EXTRA_MASK_BITS=
Code, Position, Alias, Type, Level, Rank, Attribute, Race, Attack, Defense,
BaseAttack, BaseDefense, Reason, Status, IsPublic, LScale

QUERY_LINK_HEX=0x00800000
QUERY_LINK_INCLUDED=NO
~~~

The surrounding normal masks were mechanically checked as follows:

~~~text
RefreshHand = 0x03781fff: QUERY_LINK absent
RefreshMzone = 0x03981fff: QUERY_LINK present
RefreshSzone = 0x03f81fff: QUERY_LINK present
RefreshGrave = 0x00381fff: QUERY_LINK absent
RefreshSingle = 0x03f81fff: QUERY_LINK present
PseudoRefreshDeck = 0x01181fff: replay-only and QUERY_LINK absent
~~~

No command in this task modified OCGForge, EDOPro, ocgcore, the Ignis
runtime, tests, fixtures, query masks, or ADR-0001.
