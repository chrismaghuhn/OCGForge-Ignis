# I6C6 Current-Property Observability Contract

## 1. Executive conclusion

This document records a source-backed capability classification for the
Current properties required by the I6C6 public-state comparison. It does not
change a runtime, a query mask, a model contract, or the I6C6 comparator.

The native OCGForge observation builder requests the complete current-property
query surface. The normal pinned EDOPro external server path does not provide
that same surface at every public refresh boundary. In particular,
RefreshExtra uses 0x00381fff, which does not include QUERY_LINK,
QUERY_RSCALE, or QUERY_COUNTERS. Other normal refresh masks omit different
current fields.

Two of those omissions have a narrow source-proven derivation. Outside
PZONE, the pinned core returns the static card-data scale values, and outside
MZONE it returns the static Link rating after the status and assumption
preconditions are checked. Those are boundary-specific derivations, not a
generic Current = Printed rule. Link markers remain dynamically dependent,
and counter events are present in the public stream but are not yet
reconstructed into the Ignis Current property state.

The observed Current.link_rating mismatch is therefore not by itself proof
that link_rating is unavailable everywhere; it is proof that the missing
query record must be replaced by the exact non-MZONE derivation or the
comparison must fail closed. The remaining unclosed Current capability is
link markers, together with counter-state reconstruction at the current
Ignis boundary.

The normative consequence is:

~~~text
semantic absence -> compare absence
evidence available -> compare the exact normalized value
evidence unavailable -> comparison is UNPROVEN
~~~

EVIDENCE_UNAVAILABLE must never be converted to null, an empty vector, a zero,
a Printed value, or an older carried value.

The resulting decision is:

~~~text
FINAL_DECISION=IGNIS_RUNTIME_CURRENT_PROPERTY_CAPABILITY_GAP
I6C6_3_CAN_RESUME_WITH_EXISTING_EVIDENCE=NO
~~~

The minimum separately proposed follow-up is
I6C6_3A_CURRENT_PROPERTY_EVIDENCE_COMPLETION. That name is a proposal only. It
must decide and prove a privacy-safe source for the missing fields before
I6C6-3 resumes; this document does not authorize that work.

## 2. Fixed points and source quality

The audit used the following exact committed points:

| Authority | Fixed point | Role |
| --- | --- | --- |
| OCGForge-Ignis | 8a225756a9097397ac58909a42362115ae62b599 | Authorized Ignis source base |
| OCGForge | 2913345ecd7bc4fffce192a1eb31b95ec354b178 | Current native source authority requested for this audit |
| OCGForge I6C6 diagnostic | f2c20d2e09c436ffd68a1efea83967cb7f48c2ad | Committed native diagnostic head; not modified here |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | Pinned external server/client source |
| EDOPro ocgcore gitlink | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | Pinned EDOPro runtime core |
| Separate core research reference | e747e1771fcf91dd7c53a5950f030012229e66e4 | Not used as EDOPro authority |

The EDOPro source was read from the checkout whose HEAD is
30935e847165a9ef0e547fb51a43f36168fab7c7. Its ocgcore gitlink resolves to
46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57; the core source used for the
query-emission audit was separately verified at that commit. The EDOPro
checkout reported only its pre-existing deleted submodule working-tree entry;
no source was changed.

The existing I6C6-3 Ignis worktree and OCGForge worktree were not modified.
Their later runtime observations are treated only as corroborating diagnostic
evidence. This contract relies on the committed sources above.

## 3. Exact source/provenance table

| Source path | Exact authority | Finding |
| --- | --- | --- |
| include/ygo/observation/observed_card.hpp:30-45 | OCGForge 2913345... | Complete CardProperties field set |
| include/ygo/observation/observed_card.hpp:48-61 | OCGForge 2913345... | ObservedCard.current is distinct from printed |
| src/observation/query_decoder.hpp:35-64 | OCGForge 2913345... | Raw query fields, including Link, scales, status, and counters |
| src/observation/card_projection.cpp:43-100 | OCGForge 2913345... | Static/current projection and type-dependent normalization |
| src/observation/observation_builder.cpp:24-29 | OCGForge 2913345... | Native full query mask includes every query flag needed for Current |
| src/observation/observation_builder.cpp:326-364 | OCGForge 2913345... | Current projection is gated by perspective-safe identity visibility |
| src/observation/observation_builder.cpp:572-705 | OCGForge 2913345... | Location queries for Hand, field, Grave, Banished, and Extra |
| include/ygo/environment/public_safe_state.hpp:43-80 | OCGForge 2913345... | Public-safe state owns globals, zones, entities, relationships, chain, events, and match context |
| src/environment/public_safe_state.cpp:251-275 | OCGForge 2913345... | Hidden entities cannot carry identity-derived properties |
| src/environment/public_safe_state.cpp:393-431 | OCGForge 2913345... | Exact Current field order, optional presence, and vector encoding |
| include/ygo/model/logical_model_input.hpp:64-118 | OCGForge 2913345... | Logical model state contains ObservedCard, including Current |
| include/ygo/model/encoded_model_input.hpp:20-40 | OCGForge 2913345... | Encoded model Current field set is complete |
| src/model/encoded_model_input.cpp:1208-1230 and 1281-1304 | OCGForge 2913345... | Current properties are copied into model encoding |
| ModernQueryDecoderV1.cs:6-35 | Ignis 8a225... | Ignis represents the same query flag values |
| ModernQueryDecoderV1.cs:238-259 and 377-498 | Ignis 8a225... | Fields exist only when records decode; skipped records are distinct |
| PerspectiveStateMirrorV1.cs:1162-1255 | Ignis 8a225... | Present fields are applied; omitted fields remain in QueryFields |
| PerspectiveStateMirrorV1.cs:1455-1469 | Ignis 8a225... | Public protocol fields and identity-provenance handling |
| PerspectiveSafeFrameSourceTypesV1.cs:515-588 | Ignis 8a225... | Frame Current type contains all native Current fields |
| PerspectiveSafePublicFrameSourceV1.cs:2411-2632 | Ignis 8a225... | Query fields become Current properties; missing fields become null/empty representation |
| PerspectiveSafePublicFrameSourceV1.cs:3641-3655 | Ignis 8a225... | Exact Ignis Current query-field admission list |
| PublicStateProjectionV1.cs:55-80 | Ignis 8a225... | Legacy projection exposes only code/position and is not full Current authority |
| gframe/generic_duel.h:42-52 | EDOPro 30935... | Server refresh defaults and their masks |
| gframe/generic_duel.cpp:722-728 | EDOPro 30935... | Startup refresh order |
| gframe/generic_duel.cpp:1143-1211 and 1223-1257 | EDOPro 30935... | Event-driven refresh paths and masks |
| gframe/generic_duel.cpp:1354-1422 | EDOPro 30935... | Bulk and single refresh wire construction |
| gframe/core_utils.cpp:11-85 and 143-232 | EDOPro 30935... | Query parsing, field emission, and public filtering |
| gframe/ocgapi_constants.h:168-194 | EDOPro 30935... | Query flag values |
| gframe/ocgapi_constants.h:274-275 | EDOPro 30935... | MSG_ADD_COUNTER=101 and MSG_REMOVE_COUNTER=102 |
| card.cpp:120-209 | EDOPro ocgcore 46779... | Core emits requested current values, including Link/scales/counters |
| ocgapi.cpp:180-247 | EDOPro ocgcore 46779... | DuelQuery and DuelQueryLocation source behavior |
| card.cpp:989-1030, 1185-1237, and 1239-1277 | EDOPro ocgcore 46779... | Dynamic and static-location derivation of Link, scales, and markers |
| card.cpp:1773-1788, 1877-1884, 2010-2040, and 2220-2277 | EDOPro ocgcore 46779... | Counter mutations, emitted events, and silent movement reset |
| duel.cpp:113-117 and interpreter.cpp:365-372 | EDOPro ocgcore 46779... | Assumption restoration at the outer Lua-call boundary |
| generic_duel.cpp:1133-1137 | EDOPro 30935... | Unspecialized gameplay messages are forwarded to all recipients |
| duelclient.cpp:3712-3749 | EDOPro 30935... | Client consumes public counter messages |
| GameplayMessageDecoderV1.cs:60-61, 238-245, and 945-966 | Ignis 8a225... | Ignis decodes counter messages and their payload |
| PerspectiveSafeEventLedgerV1.cs:284-286 and 653-685 | Ignis 8a225... | Counter messages become public CounterChanged events |
| PerspectiveStateMirrorV1.cs:162-174 | Ignis 8a225... | Counter messages are currently state no-ops |
| client_card.cpp:25-115 | EDOPro 30935... | Client updates only fields whose query flags are present |

## 4. OCGForge Current semantic authority

### 4.1 Complete field set

At observed_card.hpp:30-45, native CardProperties contains exactly:

~~~text
type:u32?
attribute:u32?
race:u64?
attack:i32?
defense:i32?
base_attack:i32?
base_defense:i32?
level:u32?
rank:u32?
link_rating:u32?
link_markers:vector<LinkMarker>
left_scale:u32?
right_scale:u32?
status_flags:u32?
counters:vector<Counter>
~~~

ObservedCard.current is a separate optional property object at
observed_card.hpp:48-61. It is not a catalog alias for
ObservedCard.printed.

The public-safe state carries the same entity property object. Its canonical
encoder writes property presence separately from property values and writes
the fixed vector fields in a count-prefixed form
(public_safe_state.cpp:393-431). A present empty vector is therefore a
valid empty vector; it is not proof that the source query flag was absent.

### 4.2 Native current projection

card_projection.cpp:66-100 is the native Current authority:

* type, attribute, race, and attack copy from the raw query;
* defense and base_defense are projected only when the query type is not
  Link;
* base_attack copies from the raw query;
* rank is selected when the query type contains XYZ; otherwise level is
  selected;
* link_rating and Link markers are copied from the raw Link query fields;
* left_scale, right_scale, status_flags, and counters copy from their
  raw query fields.

The Link and XYZ conditions above are the proven Current semantic-absence
rules. They are not a general rule that every static card field is a Current
value.

The current projection does not put a type guard around link_rating,
left_scale, or right_scale. Consequently, I6C6 must compare the native
Current object produced by this function rather than invent a stronger
type-based nulling rule. A separately proven CoreHost derivation may reproduce
the value for a boundary where the pinned core is statically determined; that
derivation is defined in Section 11 and is not a projection rewrite. For Link
markers, a known non-Link projection may have an empty marker vector; a Link
projection requires dynamic Link-marker evidence.

### 4.3 Native acquisition and visibility

observation_builder.cpp:24-29 defines kCardQueryFlags as the union of the
full native query surface, including:

~~~text
QUERY_CODE
QUERY_POSITION
QUERY_ALIAS
QUERY_TYPE
QUERY_LEVEL
QUERY_RANK
QUERY_ATTRIBUTE
QUERY_RACE
QUERY_ATTACK
QUERY_DEFENSE
QUERY_BASE_ATTACK
QUERY_BASE_DEFENSE
QUERY_REASON
QUERY_REASON_CARD
QUERY_EQUIP_CARD
QUERY_TARGET_CARD
QUERY_OVERLAY_CARD
QUERY_COUNTERS
QUERY_OWNER
QUERY_STATUS
QUERY_IS_PUBLIC
QUERY_LSCALE
QUERY_RSCALE
QUERY_LINK
QUERY_IS_HIDDEN
QUERY_COVER
~~~

The same full mask is used for the ordinary location queries and the Extra
location query (observation_builder.cpp:572-705). The native builder sets
current_features_visible to the perspective-safe identity visibility result
(observation_builder.cpp:326-364). A hidden entity therefore does not receive
Current properties in the public observation.

The native public-safe contract validates that a redacted entity has no
passcode, Printed properties, or Current properties
(public_safe_state.cpp:251-275). This makes a hidden entity's missing Current
properties a privacy-semantic absence at the public entity boundary. It does
not make a missing field on a known entity equal to semantic absence.

### 4.4 Model-facing consequence

The model logical state stores ObservedCard directly
(logical_model_input.hpp:64-118). The encoded model has the complete
EncodedCardProperties field set (encoded_model_input.hpp:20-40), and
encoded_model_input.cpp:1208-1230 plus 1281-1304 copies both Printed and
Current properties into the encoded entity.

The current OCGForge main documents Task7 as unauthorized. Even so, the
accepted model-facing path already treats Current properties as semantic input.
I6C6 may not suppress a disputed Current field merely to obtain a match.
If native and Ignis can produce different model-facing bytes, exact equivalence
is not proven.

## 5. Ignis Current state and query capability

### 5.1 Decoder surface

ModernQueryDecoderV1.cs:6-35 admits the complete wire flag vocabulary,
including:

~~~text
Code=0x00000001
Position=0x00000002
Alias=0x00000004
Type=0x00000008
Level=0x00000010
Rank=0x00000020
Attribute=0x00000040
Race=0x00000080
Attack=0x00000100
Defense=0x00000200
BaseAttack=0x00000400
BaseDefense=0x00000800
Reason=0x00001000
ReasonCard=0x00002000
EquipCard=0x00004000
TargetCard=0x00008000
OverlayCard=0x00010000
Counters=0x00020000
Owner=0x00040000
Status=0x00080000
IsPublic=0x00100000
LScale=0x00200000
RScale=0x00400000
Link=0x00800000
IsHidden=0x01000000
Cover=0x02000000
~~~

ModernQueryV1.Fields contains only records actually present in a decoded
message (ModernQueryDecoderV1.cs:238-259). A zero-size on-field record is
represented separately as IsOnFieldSkipped; it is not a query carrying
default Current values.

### 5.2 Mirror retention and redaction

PerspectiveStateMirrorV1.ApplyQuery builds semantic fields only from the
incoming query records (PerspectiveStateMirrorV1.cs:1162-1195). It then
replaces an existing field with the same flag or appends a new field
(1205-1255). An omitted flag is therefore retained in the current mirror
snapshot if it was previously retained.

When the query's identity provenance is UnknownRedacted, private fields are
removed and only always-public query fields are kept
(1198-1203). QueryProvenance marks position, owner, public, and hidden flags
as public protocol facts; other fields inherit the identity visibility
provenance (1455-1469).

This is a mirror retention behavior, not proof that a later upstream omission
means the property is unchanged. The oracle contract may not rely on it without
message-family evidence for that exact omission.

### 5.3 Frame projection

PerspectiveSafeCardPropertiesV1 exposes all fifteen Current fields
(PerspectiveSafeFrameSourceTypesV1.cs:515-588). The frame source scans the
retained query fields and maps every admitted Current flag
(PerspectiveSafePublicFrameSourceV1.cs:2411-2632 and 3641-3655).

The frame source skips a value marked as an unknown redaction and leaves a
missing scalar as null; list-valued markers and counters remain empty when no
accepted record populated them. It still constructs a Current property object
for an identity-visible, known-code entity even when no Current property record
was retained (2411-2632).

That representation cannot state why a field is absent. It can mean semantic
absence, no source record, or a source capability that was not present at that
boundary. The distinction is required by this contract and is not currently
represented by the I6C5 frame type.

PublicStateProjectionV1.cs:55-80 is not a substitute authority: it publishes
only public card code and position and contains no Current property object.

## 6. Pinned EDOPro refresh and query audit

### 6.1 Query flag authority

The pinned EDOPro constants at gframe/ocgapi_constants.h:168-194 are:

| Flag | Hex |
| --- | --- |
| QUERY_CODE | 0x00000001 |
| QUERY_POSITION | 0x00000002 |
| QUERY_ALIAS | 0x00000004 |
| QUERY_TYPE | 0x00000008 |
| QUERY_LEVEL | 0x00000010 |
| QUERY_RANK | 0x00000020 |
| QUERY_ATTRIBUTE | 0x00000040 |
| QUERY_RACE | 0x00000080 |
| QUERY_ATTACK | 0x00000100 |
| QUERY_DEFENSE | 0x00000200 |
| QUERY_BASE_ATTACK | 0x00000400 |
| QUERY_BASE_DEFENSE | 0x00000800 |
| QUERY_REASON | 0x00001000 |
| QUERY_REASON_CARD | 0x00002000 |
| QUERY_EQUIP_CARD | 0x00004000 |
| QUERY_TARGET_CARD | 0x00008000 |
| QUERY_OVERLAY_CARD | 0x00010000 |
| QUERY_COUNTERS | 0x00020000 |
| QUERY_OWNER | 0x00040000 |
| QUERY_STATUS | 0x00080000 |
| QUERY_IS_PUBLIC | 0x00100000 |
| QUERY_LSCALE | 0x00200000 |
| QUERY_RSCALE | 0x00400000 |
| QUERY_LINK | 0x00800000 |
| QUERY_IS_HIDDEN | 0x01000000 |
| QUERY_COVER | 0x02000000 |

The required mechanical mask decomposition was performed by a PowerShell
script that represented each flag as a uint32 bit and selected every named
flag for which (mask -band flag) -ne 0. The exact output for the normal
server masks is below.

### 6.2 Normal GenericDuel server masks

The default declarations are at generic_duel.h:42-52. The normal server
refresh helper calls are at generic_duel.cpp:1354-1368, and all helpers use
RefreshLocation at 1369-1395.

| Path | Mask | Current-related flags included | Current-related flags excluded |
| --- | --- | --- | --- |
| RefreshExtra | 0x00381fff | Type, Level, Rank, Attribute, Race, Attack, Defense, BaseAttack, BaseDefense, Status, LScale | Counters, RScale, Link |
| RefreshGrave | 0x00381fff | Type, Level, Rank, Attribute, Race, Attack, Defense, BaseAttack, BaseDefense, Status, LScale | Counters, RScale, Link |
| RefreshHand | 0x03781fff | Type, Level, Rank, Attribute, Race, Attack, Defense, BaseAttack, BaseDefense, Status, LScale, RScale | Counters, Link |
| RefreshMzone | 0x03981fff | Type, Level, Rank, Attribute, Race, Attack, Defense, BaseAttack, BaseDefense, Status, Link | Counters, LScale, RScale |
| RefreshSzone | 0x03f81fff | Type, Level, Rank, Attribute, Race, Attack, Defense, BaseAttack, BaseDefense, Status, LScale, RScale, Link | Counters |
| RefreshSingle | 0x03f81fff | Type, Level, Rank, Attribute, Race, Attack, Defense, BaseAttack, BaseDefense, Status, LScale, RScale, Link | Counters |
| PseudoRefreshDeck | 0x01181fff | Type, Level, Rank, Attribute, Race, Attack, Defense, BaseAttack, BaseDefense, Status | Counters, LScale, RScale, Link |

All rows also include the non-Current flags selected by the mask, such as
QUERY_CODE and QUERY_POSITION. The table is limited to the Current fields
because those are the comparison subject.

The exact bit decomposition confirms:

~~~text
REFRESH_EXTRA_MASK_HEX=0x00381fff
QUERY_LINK_HEX=0x00800000
QUERY_LINK_INCLUDED=NO
QUERY_COUNTERS_HEX=0x00020000
QUERY_COUNTERS_INCLUDED_IN_NORMAL_GENERICDUEL_MASKS=NO
~~~

### 6.3 Explicit server call-site masks

The server uses additional explicit masks in generic_duel.cpp:

| Call site | Mask | Current consequence |
| --- | --- | --- |
| MSG_DRAW / MSG_SHUFFLE_HAND, generic_duel.cpp:1148-1152 | 0x03781fff | Hand scales are requested; Link and counters are not |
| MSG_SHUFFLE_EXTRA, 1154-1157 | default RefreshExtra | Extra Link, right scale, and counters are not requested |
| MSG_SWAP_GRAVE_DECK, 1159-1162 | default RefreshGrave | Grave Link, right scale, and counters are not requested |
| MSG_SHUFFLE_SET_CARD, 1169-1172 | 0x03181fff | No scales, Link, or counters |
| MSG_CHAIN_END and related field refresh, 1188-1201 | defaults | Mzone lacks both scales; Szone has both scales and Link; counters are absent |
| MSG_MOVE, 1205-1211 | RefreshSingle default when applicable | Link and both scales can be requested; counters are absent |
| MSG_POS_CHANGE, 1213-1220 | RefreshSingle default when a facedown card becomes face-up | Link and both scales can be requested; counters are absent |
| MSG_SWAP, 1223-1229 | two RefreshSingle calls | Link and both scales can be requested; counters are absent |
| MSG_TAG_SWAP, 1232-1253 | Mzone 0x03181fff, Szone 0x03781fff | Mzone loses Link and scales; Szone loses Link but retains scales |
| MSG_RELOAD_FIELD, 1255-1257 | default RefreshExtra | Extra Link, right scale, and counters are not requested |

PseudoRefreshDeck at generic_duel.cpp:1424-1437 writes an
MSG_UPDATE_DATA representation to the replay stream and does not send a
client refresh. It is not an admissible public Current source.

The pinned local replay/single-player code also contains masks such as
0x02f81fff and 0x02ffdfff; the latter includes QUERY_COUNTERS. Those paths
are local replay/single-mode refreshes, not the normal external TCP
server/client boundary. They cannot be used to claim that a normal Ignis
session receives counters. They are therefore classified OUTSIDE_I6C6 for
this capability decision.

### 6.4 EDOPro recipient and public filtering

GenericDuel::RefreshLocation constructs MSG_UPDATE_DATA with player and
location (generic_duel.cpp:1369-1376). It generates a private owner-side
query first and a public query for the other side/observers
(1379-1394).

Query::GenerateBuffer emits only flags requested by the query and can emit a
zero-size record for an on-field skipped entry
(core_utils.cpp:143-166). Query::IsPublicQuery defines the private set as
including all identity and Current fields, including status, both scales, and
Link (224-232). A public query retains those private fields only when the
card is public or face-up; otherwise they are omitted. This is a visibility
filter, not a Current-field absence declaration.

The location helper returns without sending a refresh when the core location
query length is zero (generic_duel.cpp:1374-1378). For a nonempty location,
the core can still emit zero-size records for empty/skipped entries
(ocgapi.cpp:211-217); individual field records can also be omitted by the
public filter or by a query-specific zero relation target
(core_utils.cpp:153-163). Therefore “no record” has several source meanings
and must be classified from the message and visibility context, not replaced
with a Current default.

The exact native core source confirms that requested fields are available to
the query emitter:

* card.cpp:120-134 emits the scalar identity/current fields;
* card.cpp:182-190 emits counters and status when requested;
* card.cpp:198-205 emits both scales and the Link pair when requested;
* ocgapi.cpp:207-247 applies the requested mask to every card in a location.

Therefore the absence of a flag from a server mask is an actual source
capability limitation at that refresh boundary, not a decoder limitation.

### 6.5 Counter-event source audit

The pinned core defines MSG_ADD_COUNTER as 101 and MSG_REMOVE_COUNTER as 102
(ocgapi_constants.h:274-275). A successful card::add_counter call updates
the counter map and emits type, controller, location, sequence, and count
(card.cpp:2220-2255). card::remove_counter emits the corresponding removal
payload (2257-2277). RemoveAllCounters and selected reset paths also emit
remove messages (libcard.cpp:1773-1788, card.cpp:1877-1884, and
2027-2040).

GenericDuel has no special counter redaction branch. The default Sending path
forwards otherwise-unhandled gameplay messages to all recipients
(generic_duel.cpp:1133-1137). The pinned client consumes the same public
counter payload and updates its local counter map
(duelclient.cpp:3712-3749). The counter event is therefore present as a
potential public runtime source, subject to the ordinary public locator and
visibility checks.

Ignis already decodes the two messages into GameplayCounterPayloadV1
(GameplayMessageDecoderV1.cs:60-61, 238-245, and 945-966). Its event ledger
creates a perspective-safe CounterChanged event with controller, locator when
resolvable, amount, and counter type
(PerspectiveSafeEventLedgerV1.cs:284-286 and 653-685). The state mirror
currently accepts both message kinds as a no-op
(PerspectiveStateMirrorV1.cs:162-174). The precise classification is:

~~~text
COUNTER_QUERY_SOURCE=ABSENT
COUNTER_EVENT_SOURCE=PRESENT
RUNTIME_EVIDENCE_ABSENT=NO
RUNTIME_EVIDENCE_PRESENT_BUT_NOT_INTEGRATED=YES
IGNIS_COUNTER_STATE_RECONSTRUCTION=NOT_IMPLEMENTED
~~~

The event source is not yet a complete Current-state proof. The core clears
all counters on several movement/reset masks without emitting a corresponding
remove message (card.cpp:2010-2013), while other reset paths do emit
removals. A future reconstruction must bind every such reset to the accepted
Move/Set/position transition and must preserve the correct public entity
association. The Ignis event ledger intentionally declines to expose a
guessed ordinary public locator for non-Field SZONE/PZONE meanings
(PerspectiveSafeEventLedgerV1.cs:725-736), so PZONE counter association also
needs an explicit safe source. Until those conditions are integrated and
accepted, Current.counters remains unavailable at the I6C5 frame boundary.

## 7. Complete Current-property observability matrix

The matrix uses the following interpretation:

* Directly observable means the field is present in the public runtime
  evidence for a known, identity-visible entity when the listed mask includes
  its query flag and public filtering permits it.
* Derivable means a value can be determined from already public authorities
  at the exact boundary without reading Printed data as a substitute.
* The final classification is for the full I6C6 Current surface across the
  supported external runtime boundaries. A field may have a direct source on
  one path while still being UNAVAILABLE_AT_BOUNDARY overall.

| Current field | OCGForge authoritative? | Model-facing? | EDOPro query flag/source | RefreshExtra | Other proven source | Semantic absence conditions | Directly observable? | Derivable? | Final classification |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| type | Yes, raw query projection | Yes | QUERY_TYPE; all normal masks | Yes | Hand, Mzone, Szone, Grave, Extra, RefreshSingle | No field-level type absence is established for a known Current object | Yes for visible identity | No Printed fallback | DIRECT_PUBLIC_RUNTIME_SOURCE |
| attribute | Yes | Yes | QUERY_ATTRIBUTE; all normal masks | Yes | All normal location refreshes and RefreshSingle | None established for a known Current object | Yes for visible identity | No | DIRECT_PUBLIC_RUNTIME_SOURCE |
| race | Yes | Yes | QUERY_RACE; all normal masks | Yes | All normal location refreshes and RefreshSingle | None established for a known Current object | Yes for visible identity | No | DIRECT_PUBLIC_RUNTIME_SOURCE |
| attack | Yes, dynamic query value | Yes | QUERY_ATTACK; all normal masks | Yes | All normal location refreshes and RefreshSingle | None established for a known Current object | Yes for visible identity | No | DIRECT_PUBLIC_RUNTIME_SOURCE |
| defense | Yes when projected | Yes | QUERY_DEFENSE; all normal masks | Yes | All normal location refreshes and RefreshSingle | Known Link type: native projection omits Defense | Yes when non-Link and visible | No | DIRECT_PUBLIC_RUNTIME_SOURCE |
| base_attack | Yes, dynamic query value | Yes | QUERY_BASE_ATTACK; all normal masks | Yes | All normal location refreshes and RefreshSingle | None established for a known Current object | Yes for visible identity | No | DIRECT_PUBLIC_RUNTIME_SOURCE |
| base_defense | Yes when projected | Yes | QUERY_BASE_DEFENSE; all normal masks | Yes | All normal location refreshes and RefreshSingle | Known Link type: native projection omits BaseDefense | Yes when non-Link and visible | No | DIRECT_PUBLIC_RUNTIME_SOURCE |
| level | Yes when projected | Yes | QUERY_LEVEL; all normal masks | Yes | All normal location refreshes and RefreshSingle | Known XYZ type: native projection selects Rank instead | Yes for known non-XYZ visible entity | No | DIRECT_PUBLIC_RUNTIME_SOURCE |
| rank | Yes when projected | Yes | QUERY_RANK; all normal masks | Yes | All normal location refreshes and RefreshSingle | Known non-XYZ type: native projection selects Level instead | Yes for known XYZ visible entity | No | DIRECT_PUBLIC_RUNTIME_SOURCE |
| link_rating | Yes, raw dynamic Link query | Yes | QUERY_LINK; Mzone/Szone/RefreshSingle only in normal server path | No (0x00381fff) | Mzone/Szone/RefreshSingle direct; non-MZONE static CoreHost derivation; local replay masks are not external evidence | No general Current type-null rule; known Link requires either direct Link evidence or the exact non-MZONE derivation | Yes directly on Link masks; derived outside MZONE under Section 11.3 | Yes, only under the frozen core preconditions | PROVEN_BOUNDARY_DERIVATION |
| link_markers | Yes, raw dynamic Link query mapped to typed markers | Yes | QUERY_LINK; Mzone/Szone/RefreshSingle only in normal server path | No (0x00381fff) | Mzone, Szone, RefreshSingle; local replay masks are not external evidence | Known non-Link may have an empty marker vector; Link requires Link evidence | Only on masks with Link and permitted visibility | No; marker bits depend on effects and position | UNAVAILABLE_AT_BOUNDARY |
| left_scale | Yes, raw query value | Yes | QUERY_LSCALE; absent from Mzone, deck pseudo-refresh, and some explicit masks | Yes | Direct on Hand/Szone/RefreshSingle; static-data derivation outside PZONE under Section 11.2 | No generic Pendulum nulling; outside PZONE the pinned core returns data.lscale | Yes when mask includes LScale and visibility permits | Yes outside PZONE with the exact static-data binding; direct in PZONE | PROVEN_BOUNDARY_DERIVATION |
| right_scale | Yes, raw query value | Yes | QUERY_RSCALE; absent from Extra, Grave, Mzone, deck pseudo-refresh, and some explicit masks | No (0x00381fff) | Direct on Hand/Szone/RefreshSingle; static-data derivation outside PZONE under Section 11.2 | No generic Pendulum nulling; outside PZONE the pinned core returns data.rscale | Yes when mask includes RScale and visibility permits | Yes outside PZONE with the exact static-data binding; direct in PZONE | PROVEN_BOUNDARY_DERIVATION |
| status_flags | Yes, dynamic query value | Yes | QUERY_STATUS; all normal masks, then public-filtered | Yes | All normal refreshes and RefreshSingle when identity/current data is public | Hidden/redacted entity has no Current object; no field-level default is valid | Yes for visible/public known identity | No; status is dynamic | DIRECT_PUBLIC_RUNTIME_SOURCE |
| counters | Yes, raw query vector | Yes | QUERY_COUNTERS; absent from all normal GenericDuel network masks | No | MSG_ADD_COUNTER/MSG_REMOVE_COUNTER are present public events; Ignis decodes and records them but does not reconstruct Current counters | Empty vector is semantic only when the counter query was present; event-derived state requires complete add/remove/reset association | Event fields are present; Current vector is not yet reconstructed | Potentially, from complete events plus reset/move semantics | UNAVAILABLE_AT_BOUNDARY |

The direct and derived classifications are conditional on public visibility.
A hidden
opponent Hand or Extra identity is normally omitted as an entity by the native
public builder; a hidden opponent field entity may remain as a redacted slot
without Current properties. Those are privacy-semantic entity outcomes, not
permission to compare hidden Current fields.

The UNAVAILABLE_AT_BOUNDARY rows are the material gap. They are not made
available by the fact that Ignis can decode a QueryFlagV1 value or that the
same flag exists in ocgcore. For link markers, the external EDOPro server must
request and deliver the flag at the selected boundary. For counters, public
event evidence exists but the current Ignis state does not consume it as a
complete counter vector.

## 8. Zone, visibility, and boundary combinations

| Zone / condition | Native public entity behavior | Pinned EDOPro public source | Current classification |
| --- | --- | --- | --- |
| Main Deck, either participant | No current entity; only aggregate zone counts are public (observation_builder.cpp:58-65) | PseudoRefreshDeck is replay-only and no Main Deck entity is sent | Current fields are not applicable because no entity is compared |
| Own Hand, known identity | Entity and Current may be public to the perspective | RefreshHand 0x03781fff | Direct for mask-covered fields; Link rating is static-derived outside MZONE; markers and counters need other evidence |
| Opponent hidden Hand | Entity identity is omitted from the public state | Public query removes private identity/current fields | No Current lookup or comparison; hidden identity is privacy-absent |
| Opponent public/face-up Hand card | Entity may be retained if the public predicate proves it | RefreshHand 0x03781fff; public filtering retains private fields only when public/face-up | Direct for mask-covered fields; Link rating is static-derived; markers unavailable; counters need event reconstruction |
| Own Monster Zone, known identity | Entity and Current are available | RefreshMzone 0x03981fff or RefreshSingle 0x03f81fff | Link direct; scales are static-derived because MZONE is not PZONE; counters need event reconstruction |
| Opponent public/face-up Monster Zone | Entity and Current are public | Same Mzone/Single paths and public filtering | Same mask-dependent result |
| Opponent face-down field slot | Redacted slot may remain without identity-derived Current | Public filtering removes private Current fields | Current is not compared for the hidden identity |
| Spell/Trap, Field, or Pendulum-relevant SZONE | Native zone projection may retain a known public entity | Default Szone 0x03f81fff; Tag Swap explicit Szone 0x03781fff | Default Szone covers scales and Link; Tag Swap Szone lacks Link but Link rating is static-derived outside MZONE; markers need Link evidence; counters need event reconstruction |
| Graveyard or Banished, public identity | Known entity may be retained | RefreshGrave 0x00381fff is the audited Grave path; generic location refresh has same mask if used | Link rating and both scales are static-derived outside MZONE/PZONE; markers unavailable; counters need event reconstruction |
| Graveyard or Banished, hidden opponent identity | Redacted/omitted under native privacy predicate | Public query removes private fields unless public | No hidden Current lookup |
| Own Extra Deck, known identity | Own known Extra entities may be retained | RefreshExtra 0x00381fff | Link rating and right scale are static-derived outside MZONE/PZONE; Link markers unavailable; counters need event reconstruction |
| Opponent face-up/public Extra | Public entity may be retained | Same Extra mask; public filtering can preserve private fields for face-up/public query | Link rating and right scale are static-derived; Link markers unavailable; counters need event reconstruction |
| Opponent face-down Extra | Hidden identity is not a public entity | Public query removes private fields | No hidden Current lookup |
| Overlay material | Native documentation records that the pinned per-material overlay query does not provide a general public identity source | Normal GenericDuel refresh paths do not establish a separate Current-bearing overlay refresh | Any Current-bearing overlay case requires separate evidence; no blanket derivation is admitted |

The accepted I6C6 corpus must not drop Link, Pendulum, or Extra cases to avoid
these rows. A boundary change that only removes the failing cases is
BOUNDARY_DODGE=REJECTED.

## 9. Three field states

### 9.1 SEMANTICALLY_ABSENT

This state is established by the owning semantic projection, not by a missing
wire record:

* a known Link Current projection omits defense and base_defense;
* a known XYZ Current projection selects rank and omits level;
* a known non-XYZ Current projection selects level and omits rank;
* a hidden/redacted entity has no public Current property object at all.

For a known entity, link markers, status, and counters must not be declared
absent solely because a particular query flag was omitted. The native Current
projection reads those values from query fields without a relevant
type-based fallback. Link rating and scales have the narrower derivations in
Section 11, but those derivations require their exact location and runtime
preconditions.

### 9.2 EVIDENCE_AVAILABLE

This state requires a source-controlled query record, a proven event-derived
state, or an already proven public boundary derivation at or before the
selected comparison boundary. A present zero is still present. A present
empty counter or marker vector is still present when its query record was
emitted. An event payload is evidence of a state transition, not by itself
the complete Current vector.

### 9.3 EVIDENCE_UNAVAILABLE

This state applies when a Current property is semantically relevant but the
selected public runtime evidence did not provide its query field and no
complete derivation has been proven. Examples are Link markers after
RefreshExtra and counters before Ignis event-state reconstruction is complete.

EVIDENCE_UNAVAILABLE != SEMANTICALLY_ABSENT.

The current I6C5 frame model has no independent availability marker. A future
I6C6 input capability must carry at least a field-level state distinct from
the value:

~~~text
CurrentFieldState = SEMANTICALLY_ABSENT
                  | EVIDENCE_AVAILABLE(value)
                  | EVIDENCE_UNAVAILABLE
~~~

This is the minimum concept required; no replacement architecture is defined
here.

## 10. Direct-source classifications

The only accepted per-field source classes are:

~~~text
DIRECT_PUBLIC_RUNTIME_SOURCE
PROVEN_BOUNDARY_DERIVATION
UNAVAILABLE_AT_BOUNDARY
~~~

The matrix in Section 7 assigns exactly one final class to every Current
field. The direct class is conditional on identity visibility and the relevant
query mask. The unavailable class records a real failure of coverage for the
full external-boundary matrix.

Link rating outside MZONE and both scales outside PZONE have a valid
PROVEN_BOUNDARY_DERIVATION under Section 11. No generic derivation repairs
Link markers or counters.

## 11. Boundary-derivation proofs and rejection

### 11.1 Printed-to-Current substitution is rejected

The I6C5 Printed provider owns static catalog properties. It is not a source
for dynamic Current state. Native set_current_card_properties reads the
Current object from RawCardQuery, while set_static_card_properties reads a
separate static card record (card_projection.cpp:43-100).

A generic rule current = printed would be false in legal runtime states.
The pinned core demonstrates why:

* card::get_link() checks Link type and status, supports an ASSUME_LINK
  override, returns a location-dependent value, and applies Link-changing
  effects (card.cpp:989-1030);
* card::get_lscale() and get_rscale() depend on PZONE location and scale
  effects (1185-1237);
* card::get_link_marker() handles face-down field cards, assumptions,
  marker-changing effects, and position-based rotation (1239-1277);
* card::get_status() and the counter map are dynamic core state
  (card.h:129-163, card.cpp:182-190).

Printed data therefore cannot generically replace Current values. It may
participate only in the exact static-data derivations below, when the frozen
Printed source bridge proves that the provider row is byte-identical to the
native card data used by the core.

### 11.2 Scale derivation

The pinned core's exact scale rule is:

~~~text
get_lscale():
    if current location is not LOCATION_PZONE:
        return data.lscale
    otherwise evaluate PZONE scale effects

get_rscale():
    if current location is not LOCATION_PZONE:
        return data.rscale
    otherwise evaluate PZONE scale effects
~~~

This is source-proven by ocgcore card.cpp:1185-1237. Therefore:

* default Extra, Grave, Hand, and Mzone paths with a missing scale flag may
  use the matching native static card-data scale, because those locations are
  not PZONE;
* default Szone and RefreshSingle paths request both scale flags and provide
  direct evidence, including the dynamically relevant PZONE case;
* the explicit MSG_SHUFFLE_SET_CARD mask 0x03181fff is admitted to the static
  derivation only when the scenario proves that every affected card is not
  PZONE. The core operation requires MZONE or SZONE and facedown cards
  (libduel.cpp:1378-1415), but a future fixture must still bind its exact
  symbolic location. If the PZONE exclusion is not proven, scale comparison
  fails closed at that boundary.

The premises for this derivation are:

~~~text
native and Ignis Printed source artifact binding = PASS
known public card identity = PASS
exact native static row type and lscale/rscale = PASS
current location is proven non-PZONE when a scale query flag is absent = PASS
or QUERY_LSCALE / QUERY_RSCALE is present for the PZONE case = PASS
~~~

Under these premises:

~~~text
Current.left_scale  = native data.lscale
Current.right_scale = native data.rscale
~~~

No effect can violate the derivation outside PZONE because the pinned core
returns before evaluating scale effects. PZONE effects are the explicit
negative case and require direct query evidence.

### 11.3 Link-rating derivation

The pinned core's non-MZONE Link rule is:

~~~text
if data.type does not contain TYPE_LINK:
    link_rating = 0
else if status contains STATUS_NO_LEVEL:
    link_rating = 0
else if current location is not LOCATION_MZONE:
    link_rating = data.level
else:
    evaluate ASSUME_LINK and Link-changing effects
~~~

This is source-proven by ocgcore card.cpp:989-1030. The static data type and
level are supplied by the native static row; status is directly queried by
the normal Extra, Hand, and Grave masks. The native full query includes
QUERY_LINK, but the external RefreshExtra/Hand/Grave masks do not; for those
non-MZONE cases the formula is the admissible replacement for the absent
query record.

The derivation requires:

* the public identity and matching Printed/native static row are already
  proven;
* the native static data type, not only a potentially changed current query
  type, is used for the first condition;
* STATUS_NO_LEVEL is obtained from the public QUERY_STATUS value;
* the current location is proven not to include LOCATION_MZONE;
* no active AssumeProperty override exists at the query boundary.

AssumeProperty writes into the card assumption map
(libcard.cpp:2117-2124). The core restores those assumptions after the
outermost interpreter call (interpreter.cpp:365-372), and duel-level
restoration clears the map (duel.cpp:113-117). A query taken after that
boundary may use the formula; evidence of an active in-call assumption would
invalidate the derivation and fail closed.

MZONE has direct QUERY_LINK evidence in the normal 0x03981fff and
0x03f81fff paths. SZONE can use direct evidence when requested, or the
non-MZONE formula when an explicit SZONE mask omits QUERY_LINK. Thus:

~~~text
Current.link_rating
    = DIRECT_PUBLIC_RUNTIME_SOURCE on a permitted QUERY_LINK path
    = PROVEN_BOUNDARY_DERIVATION on a proven non-MZONE path
    = UNAVAILABLE_AT_BOUNDARY if any premise is missing
~~~

### 11.4 Link-marker derivation is rejected

The pinned get_link_marker implementation has no equivalent static-location
shortcut. It can return zero for a facedown on-field card, honor an
ASSUME_LINKMARKER override, apply add/remove/change effects, and rotate
markers based on current position (ocgcore card.cpp:1239-1277). No Printed
row plus public type/status premise determines this value for every legal
non-MZONE state.

Therefore Link markers remain UNAVAILABLE_AT_BOUNDARY whenever QUERY_LINK is
not present and public filtering does not supply it. They are not repaired by
the link-rating derivation.

### 11.5 Counter-event derivation status

Counter events are a real alternative evidence source, but the current Ignis
path does not yet turn them into a complete Current counter vector.

The core emits MSG_ADD_COUNTER after a successful add with counter type,
controller, location, sequence, and count (card.cpp:2220-2255). It emits
MSG_REMOVE_COUNTER for direct removals (2257-2277), RemoveAllCounters
(libcard.cpp:1773-1788), effect-permit reset (card.cpp:1877-1884), and the
negated-counter reset path (2027-2040). GenericDuel forwards these messages to
all recipients through its default send branch (generic_duel.cpp:1133-1137).

The source is perspective-safe for a public counter event when its location
resolves to a public entity. The core's ordinary add path requires a face-up
on-field card when no explicit field location is supplied
(card.cpp:2279-2305); the event contains no passcode. Ignis validates the
location and records a CounterChanged event, but its mirror currently treats
AddCounter and RemoveCounter as state no-ops
(PerspectiveStateMirrorV1.cs:162-174). PZONE/SZONE locator handling is also
deliberately conservative in the event ledger
(PerspectiveSafeEventLedgerV1.cs:725-736).

The core additionally clears counters on several movement/reset masks without
emitting a matching remove message (card.cpp:2010-2013). A complete
event-derived proof would therefore need:

~~~text
counter state starts known and empty
all supported add/remove messages are present in the replay
all movement/reset counter clears are bound to accepted transitions
public controller/location/sequence resolves to the correct entity
PZONE/SZONE association is proven without a guessed locator
unsupported transitions fail closed
~~~

Those premises are not satisfied by the current Ignis mirror. The correct
classification is:

~~~text
COUNTER_QUERY_SOURCE=ABSENT
COUNTER_EVENT_SOURCE=PRESENT
RUNTIME_EVIDENCE_ABSENT=NO
RUNTIME_EVIDENCE_PRESENT_BUT_NOT_INTEGRATED=YES
IGNIS_COUNTER_STATE_RECONSTRUCTION=NOT_IMPLEMENTED
COUNTERS=UNAVAILABLE_AT_BOUNDARY
~~~

This is a capability/integration gap, not a claim that the EDOPro runtime
contains no counter evidence.

### 11.6 Structural absence derivation is narrow

The native projection's structural choices remain the following:
Link excludes Defense/BaseDefense, XYZ selects Rank, and non-XYZ selects
Level. Those derivations require the public Current type value itself to be
available and unambiguous.

The scale and non-MZONE Link-rating derivations in Sections 11.2 and 11.3 are
separate exact CoreHost proofs. No type bit proves Link markers, status, or
counters. A missing query field for those values remains
EVIDENCE_UNAVAILABLE.

### 11.7 Prior-value carry is not a proof

The EDOPro UI client uses CHECK_AND_SET at client_card.cpp:25-36 and
37-115, so fields are updated only when their query flags are present.
This shows that a local client object can retain an older value when a later
message omits a flag. It does not prove that the omission means the value is
unchanged in the cross-runtime public contract.

Ignis has the analogous retention behavior: ApplyQuery replaces or appends
only present query fields (PerspectiveStateMirrorV1.cs:1205-1255).
Without a message-family rule proving omission-as-unchanged for the exact
boundary, carry-forward is forbidden for oracle equality.

## 12. Privacy analysis

The native builder's identity predicate is source-controlled at
observation_builder.cpp:39-67. Main Deck entities are omitted; hidden Hand
and Extra identities are not retained; a hidden field entity may remain as a
slot without identity-derived properties. The public-safe validator enforces
the same boundary at public_safe_state.cpp:251-275.

The EDOPro public path has a separate filter. core_utils.cpp:224-232 treats
identity and Current fields as private. GeneratePublicBuffer invokes that
filter (350-358), while the owner-side path uses the private query path
through RefreshLocation (1379-1391).

A future query-mask expansion may be privacy-safe only if it proves all of the
following for the relevant recipient:

* Link, scale, status, and counter fields do not reveal a hidden identity;
* redaction removes identity-derived values for hidden cards;
* public Current modifiers are preserved only when the native public predicate
  permits them;
* unknown/redacted identities never trigger Printed-provider lookup.

The fact that ocgcore can calculate a value is not enough to admit it as
public evidence. Any technically available but privacy-unsafe source is
treated as unavailable for the authoritative public bridge.

## 13. Replay and determinism analysis

The native comparison boundary remains the selected perspective-safe public
state. Query-mask provenance, message timing, and refresh ordering are
evidence inputs; they are not permission to invent omitted field values.

Current-field evidence must be bound to:

~~~text
perspective
zone and locator
recipient/public visibility
message family
refresh mask
boundary position
query-record presence
~~~

Any evidence digest must be computed from canonical public-safe values and
field-availability states only. It must not contain paths, timestamps, process
IDs, object addresses, local EDOPro installation identity, or hidden card
values.

The normal EDOPro server can emit multiple refreshes for the same semantic
event: startup uses PseudoRefreshDeck(0/1) followed by RefreshExtra(0/1)
(generic_duel.cpp:722-725), and later event paths refresh field, Hand, or
Grave independently. Therefore a later message cannot be assumed to repair an
earlier boundary unless the selected comparison contract explicitly binds that
later evidence.

Fresh-process determinism does not turn unavailable evidence into available
evidence. It only proves repeatability of the evidence that was actually
provided.

## 14. Model and comparator implications

The current I6C6-2 test comparator has nullable scalar properties and
counted marker/counter vectors
(I6C6ComparisonFixtures.cs:636-744, 1788-1832, and 2529-2561). It
compares optional presence and values, but it has no reason-coded distinction
between semantic absence and source unavailability.

Therefore:

* a semantically absent native field may compare absent normally;
* an available field must compare exact presence and value;
* an unavailable field must stop comparison as UNPROVEN;
* null, empty, zero, or a carried value may not be used to make an
  unavailable field look absent or equal.

The minimum successor capability is a versioned availability state for each
Current field at the selected boundary, as defined in Section 9.3. I6C6-2 is
not modified by this document.

## 15. Consequence for I6C6-3

### 15.1 Current evidence status

The committed source path supports a partial comparison:

~~~text
known visible entity
+ refresh mask covers field
+ public filter permits field
-> direct Current evidence
~~~

It does not support the complete accepted I6C6 corpus at the Extra boundary.
The observed missing query record at entities[0].current.link_rating is
repaired only by the non-MZONE derivation:

~~~text
RefreshExtra mask = 0x00381fff
QUERY_LINK      = 0x00800000
QUERY_LINK_INCLUDED=NO
~~~

At that same boundary, link_rating and right_scale are derivable only after
the Printed artifact binding and non-PZONE/non-MZONE premises pass. Link
markers remain unavailable. Counter events are present, but Current counters
are not reconstructed by the current mirror. The full field matrix therefore
still is not covered by the external runtime adapter.

### 15.2 Resume decision

~~~text
I6C6_3_CAN_RESUME_WITH_EXISTING_EVIDENCE=NO
NEXT_REQUIRED_SLICE=I6C6_3A_CURRENT_PROPERTY_EVIDENCE_COMPLETION
~~~

The proposed follow-up must complete two independent evidence obligations:

1. Link markers need an explicitly authorized EDOPro/runtime query expansion
   or another source-controlled public source that provides the exact marker
   value at every relevant boundary, followed by a privacy review.
2. Counters need a complete event-state reconstruction from
   MSG_ADD_COUNTER/MSG_REMOVE_COUNTER plus every proven movement/reset clear,
   including safe PZONE/SZONE association. If that proof cannot be integrated,
   an authorized QUERY_COUNTERS source is the alternative.

The scale and non-MZONE Link-rating derivations may be integrated as
boundary-specific source rules, but each use must pass the static artifact,
location, status, and assumption preconditions in Sections 11.2 and 11.3.

The follow-up must not remove Link, Pendulum, counters, Extra, or model-facing
coverage from the corpus. It must not use Printed data as a Current fallback.
It must fail closed if a requested field is still unavailable.

No query-mask expansion, provider change, boundary change, or runtime fix is
implemented here.

## 16. Future remediation options

The following are capability options, not approvals:

* Expand the normal EDOPro refresh masks for Link markers or counters only
  where the corresponding event/field source remains necessary, then prove
  public redaction for each recipient.
* Integrate the proven non-PZONE scale and non-MZONE Link-rating derivations
  without changing the general Printed/Current separation.
* Reconstruct counters from the public counter events and all movement/reset
  boundaries, or provide an explicitly authorized QUERY_COUNTERS source.
* Add a separately authorized evidence seam that records query-field presence
  and public filtering at the exact boundary. Such a seam must remain
  test-only if it observes internals.
* Version the I6C6 comparison input to carry the three-state Current-field
  availability marker.

A future implementation may combine these only with an explicit contract
update and acceptance evidence. Moving the comparison boundary solely to
avoid a missing field is rejected.

## 17. Acceptance gates

This document satisfies the research gates with a capability-gap outcome:

~~~text
OCGFORGE_CURRENT_FIELD_SET_AUDITED=PASS
PUBLIC_SAFE_STATE_FIELD_SET_AUDITED=PASS
MODEL_INPUT_FIELD_SET_AUDITED=PASS

EDOPRO_REFRESH_EXTRA_MASK_AUDITED=PASS
EDOPRO_ALL_RELEVANT_REFRESH_MASKS_AUDITED=PASS
QUERY_FLAG_TABLE_AUDITED=PASS

LINK_RATING_CLASSIFIED=PASS
LINK_RATING_DERIVATION_AUDITED=PASS
LINK_MARKERS_CLASSIFIED=PASS
PENDULUM_SCALES_CLASSIFIED=PASS
PENDULUM_SCALE_DERIVATION_AUDITED=PASS
STATUS_CLASSIFIED=PASS
COUNTERS_CLASSIFIED=PASS
COUNTER_EVENT_SOURCE_AUDITED=PASS
COUNTER_STATE_RECONSTRUCTION=NOT_IMPLEMENTED
ALL_CURRENT_FIELDS_CLASSIFIED=PASS

SEMANTIC_ABSENCE_VS_UNAVAILABLE=EXPLICIT
PRINTED_TO_CURRENT_WITHOUT_PROOF=FORBIDDEN
OMISSION_MEANS_UNCHANGED_WITHOUT_PROOF=FORBIDDEN
BOUNDARY_DODGE=FORBIDDEN
MODEL_FIELD_SUPPRESSION=FORBIDDEN

PRIVACY_ANALYSIS=PASS
DETERMINISM_ANALYSIS=PASS
REPLAY_ANALYSIS=PASS

FINAL_DECISION=IGNIS_RUNTIME_CURRENT_PROPERTY_CAPABILITY_GAP
~~~

The PASS values above mean that the audit and classification were completed.
They do not mean that the unavailable fields are covered by the current
runtime.

## 18. Final status

~~~text
TASK=I6C6_CURRENT_PROPERTY_OBSERVABILITY_CONTRACT

IGNIS_BASE=8a225756a9097397ac58909a42362115ae62b599
OCGFORGE_AUTHORITY_COMMIT=2913345ecd7bc4fffce192a1eb31b95ec354b178
EDOPRO_COMMIT=30935e847165a9ef0e547fb51a43f36168fab7c7
EDOPRO_OCGCORE_GITLINK=46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57

OCGFORGE_CURRENT_FIELD_SET_AUDITED=PASS
MODEL_FACING_CURRENT_FIELDS_AUDITED=PASS
EDOPRO_QUERY_FLAGS_AUDITED=PASS
REFRESH_EXTRA_MASK_AUDITED=PASS
ALL_RELEVANT_REFRESH_MASKS_AUDITED=PASS

REFRESH_EXTRA_MASK_HEX=0x00381fff
QUERY_LINK_HEX=0x00800000
QUERY_LINK_INCLUDED=NO

LINK_RATING_CLASSIFICATION=DERIVED
LINK_MARKERS_CLASSIFICATION=UNAVAILABLE
LEFT_SCALE_CLASSIFICATION=DERIVED
RIGHT_SCALE_CLASSIFICATION=DERIVED
STATUS_CLASSIFICATION=DIRECT
COUNTERS_CLASSIFICATION=UNAVAILABLE

COUNTER_QUERY_SOURCE=ABSENT
COUNTER_EVENT_SOURCE=PRESENT
RUNTIME_EVIDENCE_ABSENT=NO
RUNTIME_EVIDENCE_PRESENT_BUT_NOT_INTEGRATED=YES
IGNIS_COUNTER_STATE_RECONSTRUCTION=NOT_IMPLEMENTED

ALL_CURRENT_FIELDS_CLASSIFIED=PASS

SEMANTIC_ABSENCE_VS_UNAVAILABLE=EXPLICIT
PRINTED_TO_CURRENT_UNPROVEN_FALLBACK=FORBIDDEN
PRIOR_VALUE_UNPROVEN_CARRY=FORBIDDEN
BOUNDARY_DODGE=FORBIDDEN
MODEL_FIELD_SUPPRESSION=FORBIDDEN

PRIVACY_ANALYSIS=PASS
DETERMINISM_ANALYSIS=PASS
REPLAY_ANALYSIS=PASS

FINAL_DECISION=IGNIS_RUNTIME_CURRENT_PROPERTY_CAPABILITY_GAP

I6C6_3_CAN_RESUME_WITH_EXISTING_EVIDENCE=NO
NEXT_REQUIRED_SLICE=I6C6_3A_CURRENT_PROPERTY_EVIDENCE_COMPLETION

IGNIS_PRODUCTION_CODE_CHANGED=NO
IGNIS_TEST_CODE_CHANGED=NO
OCGFORGE_CHANGED=NO
EDOPRO_CHANGED=NO
QUERY_MASK_CHANGED=NO
COMPARATOR_CHANGED=NO
REAL_CARD_DATA_ADDED=NO

I6C6_3_FINAL=NO
I6C6_4_AUTHORIZED=NO
I6C6_5_AUTHORIZED=NO
I6D_AUTHORIZED=NO
I7_AUTHORIZED=NO
~~~

## 19. Reproducibility commands

The exact-head and source checks used for this audit were:

~~~powershell
git -C C:\Users\chris\Documents\OCGForge-Ignis fetch origin
git -C C:\Users\chris\Documents\OCGForge-Ignis rev-parse origin/main
git -C C:\Users\chris\Documents\OCGForge-Ignis rev-parse 8a225756a9097397ac58909a42362115ae62b599
git -C C:\Users\chris\Documents\OCGForge-Ignis worktree list --porcelain
git -C C:\yogiohML fetch origin
git -C C:\yogiohML rev-parse 2913345ecd7bc4fffce192a1eb31b95ec354b178
git -C C:\Users\chris\AppData\Local\Temp\edopro rev-parse HEAD
git -C C:\Users\chris\AppData\Local\Temp\edopro submodule status
git -C C:\Users\chris\AppData\Local\Temp\edopro-ocgcore-i6c5-source-audit-46779 rev-parse HEAD
git -C C:\yogiohML show 2913345ecd7bc4fffce192a1eb31b95ec354b178:<path>
rg -n "RefreshExtra|RefreshHand|RefreshMzone|RefreshSzone|RefreshGrave|RefreshSingle|PseudoRefreshDeck" C:\Users\chris\AppData\Local\Temp\edopro\gframe
rg -n "^#define QUERY_" C:\Users\chris\AppData\Local\Temp\edopro\gframe\ocgapi_constants.h
rg -n "get_link|get_lscale|get_rscale|get_link_marker" C:\Users\chris\AppData\Local\Temp\edopro-ocgcore-i6c5-source-audit-46779\card.cpp
~~~

The mask decomposition was a deterministic PowerShell calculation over the
named constants:

~~~powershell
$names = $flags.GetEnumerator() |
  Where-Object { ([uint32]$mask.Value -band [uint32]$_.Value) -ne 0 } |
  ForEach-Object Key
~~~

No source checkout, query mask, production file, test, fixture, provider, or
third-party artifact was changed by the audit.
