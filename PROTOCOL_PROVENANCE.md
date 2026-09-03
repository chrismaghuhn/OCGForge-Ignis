# Protocol Provenance and Clean-Room Ledger

Status: I3A implementation/provenance ledger
Date inspected: 2026-09-03

## Clean-room discipline

Upstream repositories are references and independent test oracles only. This
repository does not copy source implementations, parser code, control-flow
code, protocol DTOs, binaries, databases, scripts, or decks.

For every protocol constant or behavior implemented in a later task, the
ledger must record:

- external repository;
- exact commit;
- source file or path;
- fact learned;
- date inspected;
- classification as numeric constant, wire layout, or observed behavior.

The fact must then be independently implemented and tested. A source path is
provenance, not a permission to copy its implementation.

## Exact upstream pin verification

The six researched pins were checked on 2026-09-03. Each is a lowercase
40-character commit SHA, and each resolved to a commit object in the intended
repository. Current default-branch HEADs were collected separately with
git ls-remote; they are informational only.

| Repository | Exact URL | Frozen commit/reference | Current default HEAD | Result |
| --- | --- | --- | --- | --- |
| EDOPro | https://github.com/edo9300/edopro.git | 30935e847165a9ef0e547fb51a43f36168fab7c7 | 30935e847165a9ef0e547fb51a43f36168fab7c7 | VERIFIED; current HEAD equals pin |
| WindBot | https://github.com/ProjectIgnis/windbot.git | bffe6b62679c8b2fafea8f59740e03a132517da4 | bffe6b62679c8b2fafea8f59740e03a132517da4 | VERIFIED; current HEAD equals pin |
| ygopro-core | https://github.com/edo9300/ygopro-core.git | e747e1771fcf91dd7c53a5950f030012229e66e4 | e747e1771fcf91dd7c53a5950f030012229e66e4 | VERIFIED; research reference only, not the EDOPro gitlink |
| CardScripts | https://github.com/ProjectIgnis/CardScripts.git | 00a828b79303d047d6905f528857cc287ad3a84e | e6fdc75f131d00904db1f96bd79ff0f9ffe66c84 | VERIFIED; current HEAD differs and is not adopted |
| BabelCDB | https://github.com/ProjectIgnis/BabelCDB.git | 2142b4b45e7963fd944940f144951177e87eb15c | f37312b2bff3c8ba575f19a1f0a1da3c2d44ccf4 | VERIFIED; current HEAD differs and is not adopted |
| Distribution | https://github.com/ProjectIgnis/Distribution.git | 54a6e2395c532648ff762540e9615319fac4f51b | 54a6e2395c532648ff762540e9615319fac4f51b | VERIFIED; current HEAD equals pin |

Verification used exact-commit object resolution plus the primary GitHub
commit endpoint. The endpoint returned the requested SHA for every pin; no
researched pin was silently replaced by a current HEAD.

## EDOPro and ocgcore identity coherence

The pinned EDOPro commit contains this exact tree entry:

~~~text
EDOPRO_SOURCE_COMMIT=30935e847165a9ef0e547fb51a43f36168fab7c7
EDOPRO_OCGCORE_GITLINK=46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57
YGOPRO_CORE_RESEARCH_REFERENCE=e747e1771fcf91dd7c53a5950f030012229e66e4
~~~

The EDOPro tree entry was queried directly at the pinned EDOPro commit and
returned path ocgcore with type commit and SHA
46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57. Both the gitlink and the separate
ygopro-core research reference resolve as commit objects in the intended
ygopro-core repository. Only the gitlink is the V1 runtime core identity.

The research reference is retained to preserve the original six-pin research
record. It must not be silently treated as the core used by EDOPro. A future
core override requires a separate explicit identity and acceptance decision.

## Initial protocol-fact register

These are research facts to be rechecked by the authorized implementation task
that uses them. The register intentionally contains no copied implementation.

| External repository | Exact commit | Source path | Fact learned | Date | Classification |
| --- | --- | --- | --- | --- | --- |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | tree entry ocgcore | The pinned EDOPro source records ocgcore at gitlink 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | 2026-09-03 | identity/provenance |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/network.h | CTOS and STOC packet type constants and names define the client/server message families | 2026-09-03 | numeric constant |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/game.cpp | PRO_VERSION=0x1354 is defined for the pinned client | 2026-09-03 | numeric constant |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/config.h | EDOPro version 41.0.2, codename Bagooska, and CLIENT_VERSION composition are defined for the pinned client | 2026-09-03 | numeric constant |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/ocgapi_types.h | OCG_VERSION_MAJOR=11 and OCG_VERSION_MINOR=0 define the ocgcore API version | 2026-09-03 | numeric constant |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/duelclient.h and gframe/duelclient.cpp | The client-side duel transport and message dispatch are protocol references, not code to copy | 2026-09-03 | observed behavior |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/generic_duel.cpp | Duel-message handling and response timing provide an independent behavior oracle | 2026-09-03 | observed behavior |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/client_field.cpp, gframe/client_card.cpp, gframe/event_handler.cpp | Client-visible state and event handling are references for later perspective mapping | 2026-09-03 | observed behavior |
| WindBot | bffe6b62679c8b2fafea8f59740e03a132517da4 | YGOSharp.Network/BinaryClient.cs | An independent external client uses a framed TCP byte-stream transport | 2026-09-03 | wire layout |
| WindBot | bffe6b62679c8b2fafea8f59740e03a132517da4 | YGOSharp.Network/YGOClient.cs | Client connection and receive/send orchestration are external-client references | 2026-09-03 | observed behavior |
| WindBot | bffe6b62679c8b2fafea8f59740e03a132517da4 | Game/GameClient.cs | Game-level packet dispatch is a reference for later independent implementation | 2026-09-03 | observed behavior |
| WindBot | bffe6b62679c8b2fafea8f59740e03a132517da4 | Game/GamePacketFactory.cs | CTOS packet construction provides a response-layout oracle | 2026-09-03 | wire layout |
| WindBot | bffe6b62679c8b2fafea8f59740e03a132517da4 | Game/GameBehavior.cs | Existing bot behavior contains heuristics and fallbacks and is not an authority or implementation source | 2026-09-03 | observed behavior |

## I1 implemented facts

Protocol contract ID: ocgforge-ignis.protocol.wire.v1

The following facts are the only additional upstream facts used by the I1
implementation. They were independently reimplemented; no upstream source
implementation was copied.

| External repository | Exact commit | Source path | Fact learned | Date | Classification |
| --- | --- | --- | --- | --- | --- |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/netserver.cpp | A frame begins with a uint16 length, and the server consumes length plus the two-byte prefix; the length includes the packet type | 2026-09-03 | wire layout |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/duelclient.h | Client frame writers encode length as one packet-type byte plus payload bytes | 2026-09-03 | wire layout |
| WindBot | bffe6b62679c8b2fafea8f59740e03a132517da4 | YGOSharp.Network/BinaryClient.cs | The independent client uses a two-byte maximum 0xffff frame header and retains incomplete receive data across arbitrary segmentation | 2026-09-03 | wire layout |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/network.h | The frozen CTOS and STOC packet IDs and the explicit unsupported STOC IDs are defined as direction-specific numeric constants | 2026-09-03 | numeric constant |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/network.h | CTOS_PlayerInfo, CTOS_JoinGame, CTOS_HandResult, and CTOS_TPResult define the implemented CTOS payload fields and alignment | 2026-09-03 | wire layout |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/network.h | JoinError, DeckError, STOC_ErrorMsg/SIDEERROR, VersionError/VERERROR2, STOC_HandResult, STOC_JoinGame/HostInfo, STOC_TypeChange, STOC_TimeLimit, and lobby payload structs define the implemented STOC fields and alignment | 2026-09-03 | wire layout |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/network.h | ERROR_TYPE discriminates JoinError (8 bytes: error), DeckError (24 bytes: type, count.current, count.minimum, count.maximum, code), STOC_ErrorMsg for SIDEERROR/legacy VERERROR (8 bytes: code), and VersionError/VERERROR2 (8 bytes: three padding bytes plus ClientVersion) | 2026-09-03 | wire layout |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/network.h | JoinError.JERR values are 0..2 (unable/password/refused), and DeckError.DERR values are 0..13 (NONE through TOOMANYSKILLS); these inner discriminators are validated before typed publication | 2026-09-03 | numeric constant |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/deck_manager.cpp | CheckCards initializes a DeckError with NONE, assigns the card code before card/content checks, and returns card-code errors with that code; CheckDeckSize initializes count fields only for MAINCOUNT, EXTRACOUNT, and SIDECOUNT; CheckDeckContent can also return EXTRACOUNT and type-only errors without those fields initialized | 2026-09-03 | observed behavior |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/generic_duel.cpp | A DeckError is sent only when its type is not NONE; NONE is therefore not an emitted STOC_ERROR_MSG subtype, and EXTRACOUNT has no unambiguous wire-level source-path discriminator | 2026-09-03 | observed behavior |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/network.h and gframe/generic_duel.cpp | Raw C++ payload structs include alignment bytes, and the assigned semantic fields do not guarantee initialization of those bytes | 2026-09-03 | observed behavior |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/generic_duel.cpp | Join, version, deck, and side failures send different error structs; the error discriminator therefore selects an exact payload layout | 2026-09-03 | observed behavior |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/game.cpp, gframe/config.h, and gframe/ocgapi_types.h | The validated V1 join boundary accepts only PRO_VERSION 0x1354 and client/core version 41.0/11.0; incompatible CTOS_JOIN_GAME or STOC_JOIN_GAME version fields are UnsupportedVersion | 2026-09-03 | numeric constant/validation behavior |
| WindBot | bffe6b62679c8b2fafea8f59740e03a132517da4 | Game/GameClient.cs | Join-game serialization makes the two alignment bytes after the protocol version explicit and writes a fixed UTF-16 password and client version | 2026-09-03 | wire layout |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/bufferio.h | Fixed text fields use UTF-16 code units and write a terminating code unit; the caller-owned remainder of the containing C++ struct is not guaranteed to be zero-filled | 2026-09-03 | wire layout |
| WindBot | bffe6b62679c8b2fafea8f59740e03a132517da4 | YGOSharp.Network/Utils/BinaryExtensions.cs | Fixed text fields use explicit little-endian UTF-16 with a fixed code-unit width | 2026-09-03 | wire layout |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/duelclient.cpp | STOC_GAME_MSG forwards the inner bytes to duel analysis and CTOS_RESPONSE forwards opaque response bytes; I1 preserves both payloads without semantic decoding | 2026-09-03 | observed behavior |

I1 treats C/C++ alignment bytes as transport-only: the raw frame payload
retains them, the typed semantic DTO consumes and ignores them, and canonical
encoding emits zero padding. They do not participate in DTO equality or typed
identity.

DeckError has the same 24-byte physical layout for every DERR_TYPE, but its
initialized semantic fields are subtype-dependent. The typed projection emits
only CardCode for LFLIST, OCGONLY, TCGONLY, UNKNOWNCARD, CARDCOUNT, and
UNOFFICIALCARD; only Current/Minimum/Maximum for MAINCOUNT and SIDECOUNT; and
only the DERR_TYPE for EXTRACOUNT, FORBTYPE, INVALIDSIZE, TOOMANYLEGENDS, and
TOOMANYSKILLS. The remaining raw bytes are transport-only. EXTRACOUNT uses the
type-only projection because the pinned upstream has both count-bearing and
type-only construction paths without a wire discriminator. DERR_TYPE NONE is
rejected as a non-emitted error.

No version fact in this ledger is attributed to gframe/ocgapi_constants.h.
The corrected version facts above are tied only to the exact source paths where
they were verified.

The I1 wire-codec task must add the exact packet facts it implements and must
preserve the I0 prohibition on source copying. I1 is not authorized by this
bootstrap.

## I2 implemented facts

The I2 implementation uses only the following additional facts from the pinned
EDOPro commit. They are independently implemented; no upstream source,
parser, or control flow is copied.

| External repository | Exact commit | Source path | Fact learned | Date | Classification |
| --- | --- | --- | --- | --- | --- |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/ocgapi_constants.h | DUEL_RELAY is the modern duel flag 0x80 | 2026-09-03 | numeric constant |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/network.h | HostInfo carries team1, team2, best_of, duel_flag_low, handshake, and the public duelist/observer position and lobby status encodings | 2026-09-03 | wire layout/numeric constant |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/duelclient.cpp | On a successful TCP connection the current client sends CTOS_PLAYER_INFO before CTOS_JOIN_GAME; the modern creator sets mode independently from duel_flag_low and best_of | 2026-09-03 | observed behavior/wire layout |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/duelclient.cpp | A modern HostInfo relay indication is read from duel_flag_low & DUEL_RELAY; mode is not used as the current relay detector | 2026-09-03 | observed behavior |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/netserver.cpp | The server passes the relay flag from duel_flag_low and best_of independently into GenericDuel construction | 2026-09-03 | observed behavior |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/netserver.h | ReSendToPlayer resends the most recently sent packet to the other player, so the initial STOC_SELECT_HAND reaches both 1v1 participants | 2026-09-03 | observed behavior |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/generic_duel.cpp | CheckReady, PlayerReady, and StartDuel require authoritative server readiness; current 1v1 I2 scope has both positions occupied and ready before host start | 2026-09-03 | observed behavior |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/generic_duel.cpp | HandResult validates values 1..3, retries on ties, sends recipient-relative hand results, and sends STOC_SELECT_TP only to the winner | 2026-09-03 | observed behavior |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/generic_duel.cpp | TPResult uses 0/1, enters the active duel path, and is followed by gameplay-message generation rather than a required STOC_TP_RESULT acknowledgement | 2026-09-03 | observed behavior |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/generic_duel.cpp | Player-change status low nibbles 0..5 encode duelist position moves while 0x8..0xb encode observe/ready/not-ready/leave | 2026-09-03 | wire layout/observed behavior |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/netserver.cpp | EOF/error closes an unjoined player through DisconnectPlayer and routes an already-joined player through LeaveGame; an explicit CTOS_LEAVE_GAME is dispatched to LeaveGame when a duel mode exists | 2026-09-03 | observed behavior |

## I3A0 researched gameplay-message facts

I3A0 uses the following clean-room facts for the gameplay-message contract and
inventory. They are references only. No parser, state-mutation routine,
serialized upstream packet, or client implementation is copied.

| External repository | Exact commit | Source path / symbol | Fact learned | Date | Classification |
| --- | --- | --- | --- | --- | --- |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | ocgapi_constants.h, MSG_* definitions | The pinned runtime defines the complete numeric MSG_* identifier set indexed in `game-message-support.v1.json`; the same file defines location, position, and query flag constants used by later decoding | 2026-09-03 | numeric constant |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/generic_duel.cpp, `GenericDuel::TPResult` start buffer | Modern gameplay start messages contain message ID 4, playertype, two uint32 LP values, and two uint16 deck/extra count pairs; the server sends 18 total bytes and uses playertype 0/1 for duelists and 0x10/0x11 for observers | 2026-09-03 | wire layout/observed behavior |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/duelclient.cpp, `STOC_GAME_MSG` dispatch and `MSG_START` case | The client receives the I1-preserved GAME_MSG payload as an inner message stream, reads modern MSG_START fields in canonical player order, and maps player-relative values through the established gameplay perspective | 2026-09-03 | wire layout/observed behavior |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/duelclient.cpp, `MSG_NEW_TURN` and `MSG_NEW_PHASE` cases | The client initializes turn count at MSG_START, increments it on each semantic MSG_NEW_TURN, reads the turn player as uint8, and reads phase as uint16 | 2026-09-03 | wire layout/observed behavior |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/core_utils.h, `loc_info` | A protocol card locator carries controller, location, sequence, and position; it is a protocol-local address, not a public semantic identity | 2026-09-03 | wire layout/identity boundary |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/core_utils.cpp, `ReadLocInfo` and `Query::Parse` | Modern loc_info uses two uint8 fields followed by two uint32 fields; modern query data is a length/flag stream with a terminating query-end item and flag-specific values, including relation lists and counters | 2026-09-03 | wire layout |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/core_utils.cpp, `Query::Parse` | The query envelope and terminator are proven, but the complete flag-specific V1 union is not frozen by I3A0; `MSG_UPDATE_DATA` and `MSG_UPDATE_CARD` therefore publish only their proven prefixes and remain `UNFROZEN` for typed layout purposes | 2026-09-03 | wire layout/scope boundary |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/core_utils.cpp, `Query::Parse` | A single `ModernQueryV1` reads a uint16 item size; size zero means `ONFIELD_SKIPPED` and no query flag follows, while a nonzero record has at least four bytes for the uint32 query flag plus exactly the remaining flag payload; the query-end record is size four with zero flag payload and there is no leading total byte count | 2026-09-03 | wire layout |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/core_utils.cpp, `QueryStream::Parse` | `ModernQueryStreamV1` begins with a uint32 total query-byte count and then parses complete `ModernQueryV1` records within exactly those following bytes; the prefix is excluded from the count | 2026-09-03 | wire layout |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/client_card.cpp, `ClientCard::UpdateInfo` and `SetCode` | Client-visible card properties are updated only for present query flags; code and public/hidden transitions affect what the client knows and must not be treated as stable identity without a visibility proof | 2026-09-03 | observed behavior/privacy interpretation |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | field.cpp, shuffle/move/reload message creation | Core emits movement, shuffle, reload, and zone messages from semantic card transitions; shuffle families are explicit knowledge-destruction boundaries for hidden locator continuity | 2026-09-03 | observed behavior/privacy interpretation |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | operations.cpp, processor.cpp, playerop.cpp | Core message families cover state, movement, LP, battle, chain, relation, random-result, and prompt messages; prompt families are not gameplay-state answers and are assigned to I4/I5 or fail-closed scope | 2026-09-03 | observed behavior/scope classification |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/duelclient.cpp, `MSG_SHUFFLE_DECK`, `MSG_SHUFFLE_HAND`, `MSG_SHUFFLE_EXTRA`, `MSG_SHUFFLE_SET_CARD`, `MSG_REVERSE_DECK` | The client clears or reassigns hidden card codes/positions during shuffle and reorder handling; I3 must destroy stale continuity rather than expose those process-local associations | 2026-09-03 | observed behavior/privacy interpretation |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/duelclient.cpp, `MSG_UPDATE_DATA` and `MSG_UPDATE_CARD` cases | Modern `MSG_UPDATE_DATA` reads player and location as uint8 values before the query stream; modern `MSG_UPDATE_CARD` reads player, location, and sequence as uint8 values before the query stream; no uint32 sequence is present | 2026-09-03 | wire layout |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/duelclient.cpp, `MSG_SPSUMMONING` case | The modern client reads exactly one uint32 card code followed by one modern loc_info; it does not read an optional second source-code field | 2026-09-03 | wire layout |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | operations.cpp, special-summon writers | The core writer emits exactly one uint32 card code (real code or zero for a hidden card) followed by one modern loc_info for `MSG_SPSUMMONING` | 2026-09-03 | wire layout |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/duelclient.cpp, `MSG_SHUFFLE_SET_CARD` case | The client reads ordered previous records followed by ordered current records and uses the same record ordinal `i`; `previous[i]` maps to `current[i]` by protocol evidence, not by inferred zone/index continuity | 2026-09-03 | wire layout/identity boundary |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/generic_duel.cpp, `MSG_WIN` handling | The server reads player and win type as uint8 values; player 0/1 is recorded as that canonical winner, player values greater than 1 are recorded as match-result draw value 2, and win type is not locally decoded into a narrower enum | 2026-09-03 | wire layout/observed behavior |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/duelclient.cpp, `MSG_SET` and summon cases | `MSG_SET`, `MSG_SUMMONING`, `MSG_SUMMONED`, `MSG_SPSUMMONING`, `MSG_SPSUMMONED`, `MSG_FLIPSUMMONING`, and `MSG_FLIPSUMMONED` are consumable event/presentation messages at the client boundary; `MSG_SET` does not update the client card structure, and the mirror must not apply these signals a second time when MOVE/query messages own state changes | 2026-09-03 | observed behavior/effect classification |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | operations.cpp, summon writers | Core emits the physical summon payloads; the inventory separates their wire consumption from semantic mirror mutation owned by later movement/query facts | 2026-09-03 | wire layout/effect classification |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | operations.cpp, `MSG_SET` writer | The physical `MSG_SET` payload is one uint32 card code followed by one modern loc_info; the code can be redacted by the server before broadcast, so it is not a basis for a second mirror mutation | 2026-09-03 | wire layout/effect classification |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/duelclient.cpp, `MSG_REFRESH_DECK` case | The modern client does not read a player byte for `MSG_REFRESH_DECK`; the legacy player read is commented out, so I3 cannot derive a player mapping or player-scoped mutation from this exact empty message | 2026-09-03 | wire layout/effect classification |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/duelclient.cpp, `MSG_NEW_TURN` and `MSG_NEW_PHASE` cases | The client consumes the authoritative player/phase fields and updates display state; it does not reconstruct expected Yu-Gi-Oh! turn alternation or phase legality, which is outside I3 authority | 2026-09-03 | observed behavior/authority boundary |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | processor.cpp, `MSG_REMOVE_CARDS` writer | `MSG_REMOVE_CARDS` carries a uint32 count followed by that many loc_info records; no leading card-code field is part of the modern V1 layout | 2026-09-03 | wire layout |

## I3A implementation boundary

I3A uses the already recorded I1 outer-frame contract, modern loc_info layout,
modern GAME_MSG envelope, and exact MSG_START facts above. The implementation
adds no query-flag union, state-mirror, semantic-locator, or public-projection
protocol claim. No upstream parser, control flow, source implementation, or
serialized packet is copied.

## Provenance boundaries

The pinned CardScripts, BabelCDB, and Distribution commits identify a future
runtime bundle; they do not authorize including those assets. Runtime identity
is specified separately in the
[runtime-bundle identity contract](docs/contracts/runtime-bundle-identity-v1.md).
