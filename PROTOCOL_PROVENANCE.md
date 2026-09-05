# Protocol Provenance and Clean-Room Ledger

Status: I3A through I5A0 implementation/provenance ledger
Date inspected: 2026-09-05

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

## I3B implementation facts

I3B uses the following additional clean-room facts. These entries bind only
the independently implemented wire reads and structural mirror behavior to
the exact accepted pins; no upstream source, control flow, or serialized
fixture is copied.

| External repository | Exact commit | Source path / symbol | Fact learned | Date | Classification |
| --- | --- | --- | --- | --- | --- |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | ocgapi_constants.h, `LOCATION_*` and position constants | Modern location bits are deck `0x01`, hand `0x02`, monster `0x04`, spell/trap `0x08`, grave `0x10`, removed `0x20`, extra `0x40`, and overlay `0x80`; the overlay bit modifies a monster-zone parent address | 2026-09-04 | numeric constant/structural rule |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | operations.cpp, `Processors::Draw` | `MSG_DRAW` writes player, a uint32 count, and each drawn card as an interleaved uint32 card code followed by uint32 position; the writer emits the message only for a nonzero draw | 2026-09-04 | wire layout/observed behavior |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/duelclient.cpp, `MSG_DRAW` case | The modern client consumes each draw record as code then position and removes that many cards from the deck into the hand; it does not provide a physical-card identity discriminator for a previously hidden deck entity | 2026-09-04 | wire layout/identity boundary |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/client_field.cpp, `ClientField::Initial`, `AddCard`, `RemoveCard` | The client owns seven monster slots and eight spell/trap slots; pile insertion/removal uses list order and compacts later pile sequence values, while field slots remain addressed by fixed sequence | 2026-09-04 | structural state rule |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/client_field.cpp, `UpdateFieldCard` and `UpdateCard` | `MSG_UPDATE_DATA` applies successive query records to successive entries of the selected client list, including skipped entries; `MSG_UPDATE_CARD` addresses one selected entry by its explicit sequence | 2026-09-04 | observed behavior/wire layout |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/duelclient.cpp, `MSG_MOVE`, `MSG_POS_CHANGE`, `MSG_SWAP` cases | The client uses the complete previous/current loc_info records for structural movement, updates a position change in place, and exchanges two addressed cards for `MSG_SWAP`; presentation behavior does not replace the structural addresses | 2026-09-04 | observed behavior |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/duelclient.cpp, `MSG_POS_CHANGE` case | A face-up to face-down position change clears client target state before applying the new position; I3B applies the same conservative boundary to stale mirror relations and card/query facts without creating a new entity | 2026-09-04 | observed behavior/privacy boundary |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | processor.cpp, movement, relation, chain, and LP message writers | Required I3B messages carry the independently frozen uint32 card/reason or amount fields, modern loc_info records, chain indexes, target/equipment addresses, and authoritative LP updates without embedding a second legality decision | 2026-09-04 | wire layout/authority boundary |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/duelclient.cpp, `MSG_SET`, chain, equip, target, LP, and turn/phase cases | `MSG_SET` is consumed as a presentation signal without changing client card structure; chain, target, equipment, LP, turn, and phase cases update or display the addressed values after complete wire reads | 2026-09-04 | observed behavior/effect classification |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/client_card.cpp, `ClientCard::UpdateInfo` | Query fields are applied only when their corresponding admitted flag is present; I3B retains the complete decoded query fields in wire order and does not reinterpret them as a public projection or semantic locator | 2026-09-04 | observed behavior/privacy boundary |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | operations.cpp and processor.cpp, chain/equip/target writers | Chain lifecycle indexes and relation messages reference protocol-visible card locations; I3B may retain only resolved value-owned internal references and must fail closed when a required reference is absent or ambiguous | 2026-09-04 | wire layout/structural rule |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | field.h, `player_info` | The authoritative LP storage is signed 32-bit and the protocol writes the nonnegative value as uint32; I3B rejects a wire value outside the representable nonnegative authoritative range before applying it | 2026-09-04 | semantic width/arithmetic rule |

## I3B remediation 01 boundaries

The following entries document the remediation's semantic boundary. The
upstream protocol supplies bytes and visibility-related fields; the
perspective-sensitive classification is an Ignis mirror policy applied only
after the established MSG_START perspective and complete query context are
available. It is not a claim that every protocol-delivered code is public.

| External repository | Exact commit | Source path / symbol | Fact learned | Date | Classification |
| --- | --- | --- | --- | --- | --- |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/client_card.cpp, `ClientCard::UpdateInfo`; gframe/duelclient.cpp, query consumers | `QUERY_CODE`, `QUERY_POSITION`, `QUERY_IS_PUBLIC`, and `QUERY_IS_HIDDEN` arrive as separate fields; code visibility requires the complete position/visibility context rather than query-record order | 2026-09-04 | observed behavior/privacy boundary |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/duelclient.cpp, `MSG_MOVE`, `MSG_POS_CHANGE`, and `MSG_DRAW` cases | Addressed message payloads may carry card codes together with a destination/position; an opponent hidden-card code is not thereby a public fact, while a face-up card is publicly established by the protocol state | 2026-09-04 | wire layout/privacy boundary |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | gframe/core_utils.cpp, `ReadLocInfo` and query readers | `loc_info` is a decode-local controller/location/sequence/position address; it is used to resolve an already represented client entity and is not a persistent public identity | 2026-09-04 | wire layout/identity boundary |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | field/query writers for `QUERY_CODE`, `QUERY_POSITION`, `QUERY_IS_PUBLIC`, and `QUERY_IS_HIDDEN` | Query records can provide identity and visibility context in an order independent of the semantic classification pass; the mirror must retain only proven scalar values or resolved internal references | 2026-09-04 | wire layout/privacy boundary |

I3B therefore classifies a nonzero self-held hidden card code as
`PerspectivePrivateFact`, a proven face-up/public code as
`PublicProtocolFact`, and an opponent hidden identity as
`UnknownRedacted`. Query-derived loc_info values are resolved to existing
value-owned internal entity references during the transactional candidate
build; an unresolved required reference fails closed. No raw loc_info,
protocol address object, public locator bytes, locator hash, or public
projection identity is part of the mirror semantic state.

## I4A0 flat prompt contract facts

I4A0 is a documentation/fixture-only freeze. The following facts were
researched against the exact pinned commits on 2026-09-04. They are clean-room
semantic evidence only; no upstream source implementation or serialized
upstream packet is copied.

I4A0 names its adapter-local selector `i4_local_candidate_key`. This is a
current-prompt binding key only. It is not the distinct OCGForge
`public_action_key` from `ocgforge.public_action_identity.v1`; that value is
`public_action.v1.<lowercase hexadecimal canonical descriptor bytes>` and its
derivation and byte-exact compatibility remain I6-owned and unproven.

```text
I4_LOCAL_CANDIDATE_KEY_IS_OCGFORGE_PUBLIC_ACTION_KEY=NO
I4_LOCAL_CANDIDATE_KEY_MODEL_INPUT_AUTHORIZED=NO
I4_LOCAL_CANDIDATE_KEY_I6_COMPATIBILITY_CLAIM=NO
OCGFORGE_PUBLIC_ACTION_KEY_DERIVATION=I6_OWNED
I6_BYTE_EXACT_COMPATIBILITY=UNPROVEN
```

Remediation-02 freezes the public-reference ownership boundary against the
accepted I3C/I3D projection. The mirror resolves a prompt's source reference
privately; it is not allowed to publish a candidate locator or card code. A
future I4 implementation must correlate that private result to a successful
accepted `PublicStateProjectionResultV1` and copy the exact locator and card
code from its `PublicStateSnapshotV1.Cards[]`. The locator codec alone is only
syntax/validation, not publication authority.

```text
I3D_PUBLIC_STATE_PROJECTION_IS_PUBLIC_LOCATOR_AUTHORITY=YES
MIRROR_PUBLIC_LOCATOR_AUTHORITY=NO
PUBLIC_SEMANTIC_LOCATOR_CODEC_ALONE_IS_PUBLICATION_AUTHORITY=NO
CARD_CODE_AUTHORITY=ACCEPTED_PUBLIC_SNAPSHOT
```

For indexed visible cards, the permitted correlation is absolute player,
semantic zone, and semantic sequence. Known HAND/EXTRA_DECK cards use the
accepted public-ordinal snapshot card only when the public correlation is
unique; duplicate ambiguity, missing snapshot cards, Main Deck cards, raw
hand/extra sequence, physical continuity, mirror identity, collection order,
and allocation order fail closed or remain private. POSITION is different:
its validated multi-bit mask is the complete domain authority, and an
unproven card code is absent rather than a reason to reject the prompt.

| Repository | Exact commit | Source path / symbol | Fact learned | Date | Classification |
| --- | --- | --- | --- | --- | --- |
| OCGForge-Ignis | 4a054c3e0f0be10b704a1614ae275d4ce630ddce | `docs/contracts/public-state-projection-v1.md#I3D identity ownership`; `src/OCGForge.Ignis.Gameplay/PublicStateProjectionV1.cs#PublicStateProjectionResultV1`, `PublicCardStateV1`, `PublicStateSnapshotV1` | The accepted successful I3C/I3D result is the value-owned public snapshot authority; its `Cards[]` contains the exact public semantic locators and perspective-safe card codes used by the later I4 boundary | 2026-09-04 | accepted public-state authority |
| OCGForge-Ignis | 4a054c3e0f0be10b704a1614ae275d4ce630ddce | `src/OCGForge.Ignis.Gameplay/PublicSemanticLocatorV1.cs#PublicSemanticLocatorV1`; `docs/contracts/public-semantic-locator-v1.md` | The locator contract validates the syntax of a semantic locator but does not prove that a locator belongs to the current accepted public projection | 2026-09-04 | locator syntax/authority boundary |

| External repository | Exact commit | Source path / symbol | Fact learned | Date | Classification |
| --- | --- | --- | --- | --- | --- |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `ocgapi_constants.h#MSG_SELECT_*`, `POS_*` | The seven I4 message IDs are 10, 11, 12, 13, 14, 16, and 19; position values are 0x01, 0x02, 0x04, and 0x08 | 2026-09-04 | numeric constant |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | `gframe/ocgapi_constants.h#EFFECT_CLIENT_MODE_*` | Client modes are NORMAL=0, RESOLVE=1, and RESET=2 | 2026-09-04 | numeric constant |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `duel.h#duel::duel_message::write`; `duel.cpp#duel::duel_message::write` | Core appends the message ID and typed values in field order; `write(loc_info)` emits controller, location, sequence, and position | 2026-09-04 | wire layout |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `card.cpp#card::get_info_location` | A non-overlay location is current controller/location/sequence/position; an overlay location carries parent location plus overlay bit, parent sequence, and overlay sequence as position | 2026-09-04 | wire layout/identity boundary |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `playerop.cpp#field::process(SelectBattleCmd&)` | BATTLECMD writes player, u32 activatable count and 19-byte entries, u32 attackable count and 8-byte entries, then two u8 transition flags; core validates kinds 0..3 and section bounds | 2026-09-04 | wire layout/response consumer |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `playerop.cpp#field::process(SelectIdleCmd&)` | IDLECMD writes six heterogeneous sections in summon, special-summon, reposition, monster-set, spell/trap-set, activate order, then Battle/End/Shuffle u8 flags; core validates kinds 0..8 | 2026-09-04 | wire layout/response consumer |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `playerop.cpp#field::process(SelectEffectYesNo&)` | EFFECTYN writes player, u32 card code, modern loc_info, and u64 description; legal response integers are exactly 0 and 1 | 2026-09-04 | wire layout/response consumer |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `playerop.cpp#field::process(SelectYesNo&)` | YESNO writes player and u64 description; legal response integers are exactly 0 and 1 | 2026-09-04 | wire layout/response consumer |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `playerop.cpp#field::process(SelectOption&)` | OPTION writes player, a u8 option count, and count u64 descriptions in vector order; an empty vector emits MSG_HINT instead, and responses are zero-based indices | 2026-09-04 | wire layout/response consumer |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `playerop.cpp#field::process(SelectChain&)` | CHAIN writes player, u8 spe_count, u8 forced, two u32 hint timings, u32 count, and 23-byte entries; every entry index is legal and -1 is legal exactly when forced is false | 2026-09-04 | wire layout/response consumer |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `processor.cpp#spe_effect`, `PointEvent`, `QuickEffect` | `spe_count` counts optional trigger/activate-or-quick effects with hints in ordinary paths; 0x7f is passed as a trigger-selection marker and is not an entry count | 2026-09-04 | semantic field/producer |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `field.cpp#chain::chain_operation_sort`; `playerop.cpp#SelectBattleCmd`, `SelectIdleCmd`, `SelectChain` | Core sorts select-chain records before writing the relevant sections; the emitted wire order is the source order to preserve, without exposing the internal effect ID | 2026-09-04 | source ordering |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `playerop.cpp#field::process(SelectPosition&)` | Position zero or a singleton is resolved directly without a prompt; an emitted prompt contains a multi-bit subset of the low four position bits and accepts the selected bit itself | 2026-09-04 | wire layout/response consumer |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `effect.cpp#effect::get_client_mode` | Mode 1 marks field-only effects for resolve, mode 2 marks non-action effects for reset, and mode 0 is the normal action mode | 2026-09-04 | semantic field |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `duel.cpp#duel::set_response`; `playerop.cpp#returns.at<int32_t>` | Flat prompt responses are consumed as a signed 32-bit response value; invalid values cause a retry in the core, which I4 replaces with fail-closed adapter behavior | 2026-09-04 | response consumer |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | `gframe/duelclient.cpp#CompatRead`, `ClientAnalyze` prompt cases | Modern mode reads u32 counts/sequences and u64 descriptions where the compatibility reader is used; legacy narrow alternatives are distinct and excluded from I4 V1 | 2026-09-04 | wire layout |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | `gframe/core_utils.cpp#ReadLocInfo` | Modern loc_info reads u8 controller, u8 location, u32 sequence, and u32 position | 2026-09-04 | wire layout |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | `gframe/duelclient.cpp#ClientAnalyze` | BATTLECMD and IDLECMD are read in their heterogeneous wire section order; SELECT_OPTION preserves received option order; CHAIN reads forced/special-count metadata and entries in wire order | 2026-09-04 | wire layout/source ordering |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | `gframe/duelclient.cpp#ClientAnalyze(MSG_SELECT_POSITION)` | Position candidates are displayed/tested in 0x01, 0x02, 0x04, 0x08 order and the selected response is the bit value | 2026-09-04 | source ordering/response producer |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | `gframe/duelclient.h#SetResponseI`; `gframe/event_handler.cpp#ClientField::SetResponseSelectedOption`, `CancelOrFinish` | EDOPro creates the flat response body as a four-byte int32; option uses the source index, yes/no uses 0/1, and chain cancel uses -1 | 2026-09-04 | response producer |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | `gframe/duelclient.h#SendBufferToServer`; `gframe/duelclient.cpp#DuelClient::SendResponse` | CTOS_RESPONSE wraps the unchanged four-byte response body with a length of packet type plus body and sends it only after the current prompt response is selected | 2026-09-04 | response wire envelope |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | `gframe/generic_duel.cpp#GenericDuel::Sending`, `GetResponse` | The server waits for the selecting player for all seven prompt families and forwards the selected response to the core; prompt payloads are not a public-boundary exemption | 2026-09-04 | routing/authority boundary |

The I4A0 contract refines the old shorthand `N protocol options = N adapter
candidates` to the cardinality of the complete proven source-response domain.
The machine inventory records the seven layouts as frozen while retaining
`support_status=OUT_OF_SCOPE`; no I4 implementation is accepted by these
rows.

## Provenance boundaries

The pinned CardScripts, BabelCDB, and Distribution commits identify a future
runtime bundle; they do not authorize including those assets. Runtime identity
is specified separately in the
[runtime-bundle identity contract](docs/contracts/runtime-bundle-identity-v1.md).

## I5A0 pinned-source audit (read-only, 2026-09-05)

The following ledger records the exact clean-room audit of the twelve proposed
I5A0 message families. `ygopro-core` is the primary authority for message
production and response validation, pinned EDOPro is the primary authority for
the client reader and response construction, and WindBot is secondary
corroboration only. No upstream implementation or serialized packet was copied
into Ignis. The audit used the exact commits in the rows below on 2026-09-05.

| Repository | Exact commit | Source path / symbol | Fact learned | Date | Classification |
| --- | --- | --- | --- | --- | --- |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `ocgapi_constants.h#MSG_SELECT_CARD`, `MSG_SELECT_PLACE`, `MSG_SELECT_TRIBUTE`, `MSG_SORT_CHAIN`, `MSG_SELECT_COUNTER`, `MSG_SELECT_SUM`, `MSG_SELECT_DISFIELD`, `MSG_SORT_CARD`, `MSG_SELECT_UNSELECT_CARD`, `MSG_ANNOUNCE_RACE`, `MSG_ANNOUNCE_ATTRIB`, `MSG_ANNOUNCE_NUMBER` | The audited I5 message IDs are respectively 15, 18, 20, 21, 22, 23, 24, 25, 26, 140, 141, and 143. `MSG_ANNOUNCE_CARD=142` is a separate family and remains outside the requested I5A0 set. | 2026-09-05 | PRIMARY numeric constant |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `duel.h#duel::duel_message::write`; `duel.cpp#duel::duel_message::write(loc_info)`, `generate_buffer` | A core message starts with its one-byte message ID, appends typed fields in writer order, and a `loc_info` is controller `u8`, location `u8`, sequence `u32` and position `u32`; the outer generated stream adds a separate `u32` message length. | 2026-09-05 | PRIMARY wire layout |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `playerop.cpp#field::process(Processors::SelectCard&)` | Modern `MSG_SELECT_CARD` is selecting player `u8`, cancelable `u8`, min `u32`, max `u32`, occurrence count `u32`, then each occurrence as card code `u32` plus 10-byte `loc_info` (14 bytes per entry). The writer clamps max to the supplied occurrence count and min to max, and sorts the card vector before writing. | 2026-09-05 | PRIMARY wire/domain |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `playerop.cpp#field::process(Processors::SelectCardCodes&)` | The same message ID also has a code-only producer: player `u8`, cancelable `u8`, min `u32`, max `u32`, count `u32`, then each card code `u32` plus a zero `loc_info` carrying the selecting player. This is a distinct source variant, not a second message ID. | 2026-09-05 | PRIMARY wire/domain |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `playerop.cpp#field::process(Processors::SelectTributeP&)` | Modern `MSG_SELECT_TRIBUTE` is player `u8`, cancelable `u8`, min `u32`, max `u32`, count `u32`, then each entry as code `u32`, controller `u8`, location `u8`, sequence `u32`, and release value `u8` (11 bytes per entry). The writer sorts the source card vector and clamps max to five and to the accumulated release value. | 2026-09-05 | PRIMARY wire/domain |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `playerop.cpp#field::process(Processors::SelectSum&)` | Modern `MSG_SELECT_SUM` is player `u8`, mode `u8` (`0` when the original max is nonzero, `1` when it is zero), accumulated target `u32` (`acc & 0xffff`), min `u32`, transmitted max `u32`, mandatory count `u32`, mandatory entries of code `u32` + 10-byte `loc_info` + `sum_param u32`, optional count `u32`, and optional entries with the same 18-byte layout. Optional entries are sorted before writing; mandatory entries retain their vector order. | 2026-09-05 | PRIMARY wire layout |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `playerop.cpp#field::process(Processors::SelectPlace&)` | Modern `MSG_SELECT_PLACE` and `MSG_SELECT_DISFIELD` share player `u8`, required count `u8`, and field mask `u32`. A zero count emits no prompt. The response consumer reads exactly count triples of player `u8`, location `u8` (MZONE or SZONE only), and sequence `u8`; MZONE sequences are 0..6, SZONE sequences are 0..7, duplicate mask bits and forbidden bits fail validation. | 2026-09-05 | PRIMARY wire/response |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `playerop.cpp#field::process(Processors::SelectCounter&)` | Modern `MSG_SELECT_COUNTER` is player `u8`, counter type `u16`, requested total `u16`, entry count `u32`, then each entry as code `u32`, controller `u8`, location `u8`, sequence `u8`, and available amount `u16` (9 bytes per entry). The source vector is sorted before writing; the response consumer reads one signed `int16` amount per entry and requires the accumulated total to equal the requested total and each amount not to exceed availability. | 2026-09-05 | PRIMARY wire/response |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `playerop.cpp#field::process(Processors::SelectUnselectCard&)` | Modern `MSG_SELECT_UNSELECT_CARD` is player `u8`, finishable `u8`, cancelable `u8`, min `u32`, max `u32`, selectable count `u32` plus 14-byte code/loc_info entries, then unselectable count `u32` plus the same entries. Selectable entries are sorted; unselectable entries follow in their vector order. The response is `int32` `-1` when finishable or cancelable, otherwise type `1` plus an `int32` combined index into selectable-then-unselectable entries. | 2026-09-05 | PRIMARY wire/response |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `playerop.cpp#field::process(Processors::SortCard&)` | Modern `MSG_SORT_CARD` and `MSG_SORT_CHAIN` are player `u8`, count `u32`, then count entries of code `u32`, controller `u8`, location `u32`, sequence `u32` (13 bytes each). A response beginning with signed byte `-1` leaves the order unchanged; otherwise the core consumes one signed `int8` destination ordinal per source occurrence and requires a permutation. | 2026-09-05 | PRIMARY wire/response |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `playerop.cpp#field::process(Processors::AnnounceRace&)`; `ocgapi_constants.h#RACE_ALL` | Modern `MSG_ANNOUNCE_RACE` is player `u8`, requested bit count `u8`, and available race mask `u64`. The response is a `u64` mask with no bits outside `available` and exactly `count` set bits. `RACE_ALL` contains race bits 0..31 and `RACE_YOKAI` at bit 62. | 2026-09-05 | PRIMARY wire/response |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `playerop.cpp#field::process(Processors::AnnounceAttribute&)`; `ocgapi_constants.h#ATTRIBUTE_ALL` | Modern `MSG_ANNOUNCE_ATTRIB` is player `u8`, requested bit count `u8`, and available attribute mask `u32`. The response is a `u32` mask with no bits outside `available` and exactly `count` set bits. `ATTRIBUTE_ALL` is the seven bits 0x01 through 0x40. | 2026-09-05 | PRIMARY wire/response |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `playerop.cpp#field::process(Processors::AnnounceNumber&)` | Modern `MSG_ANNOUNCE_NUMBER` is player `u8`, count `u8`, and count `u64` values in source order. The final response is a signed `int32` zero-based option index; negative or out-of-range indices retry. The writer does not itself reject an empty option vector. | 2026-09-05 | PRIMARY wire/response |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `libgroup.cpp#LUA_FUNCTION(SelectWithSumEqual)`, `SelectWithSumGreater`, `CheckWithSumEqual`, `CheckWithSumGreater`; `interpreter.cpp#interpreter::get_operation_value` | Sum prompts store a nonzero Lua operation result in `card::sum_param`; the low and high 16-bit portions are treated as alternative operation values by the feasibility helpers. Mandatory cards are separated from optional cards and are always included in sum checks. | 2026-09-05 | PRIMARY semantic producer |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `field.cpp#field::check_with_sum_limit_m`, `check_with_sum_greater_limit_m` | The core has two recursive feasibility predicates. Exact mode explores each mandatory/optional occurrence and its low/high values with min/max card-count bounds; greater mode searches value alternatives while carrying the minimum required operation value. These are the closest pinned source predicates for a future continuation oracle. | 2026-09-05 | PRIMARY continuation oracle |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `playerop.cpp#select_sum_check1`; `playerop.cpp#field::process(Processors::SelectSum&)` | Final `MSG_SELECT_SUM` response validation is not a generic subset count: exact mode recursively chooses one low/high value per selected occurrence and enforces the target and count bounds; original-max-zero mode instead checks aggregate minimum/maximum interval conditions. The response contains selected occurrence indices, not a chosen alternative value. | 2026-09-05 | PRIMARY response semantics |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `card.h#card::sum_param`; `interpreter.cpp#interpreter::get_operation_value`; `field.cpp#field::check_with_sum_limit_m`; `playerop.cpp#select_sum_check1` | `sum_param` is a `uint32_t` populated from an unrestricted `lua_Integer` operation result. Feasibility masks the high half as `& 0xffff`, while `select_sum_check1` promotes the packed value to signed `int32_t` and right-shifts it without the same mask. No pinned core symbol proves that high alternatives are restricted below 0x8000. Therefore the full arbitrary-wire `SELECT_SUM` legality semantics are `UNRESOLVED`; no exact I5A0 freeze may claim otherwise. | 2026-09-05 | PRIMARY UNRESOLVED |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `progressivebuffer.h#ProgressiveBuffer::at`; `duel.cpp#duel::set_response` | The core copies an opaque response byte buffer and its indexed reads return a zero value when the requested storage is short; the shown validators do not perform a general exact-length or trailing-byte check. A future fail-closed adapter must impose exact response lengths before any terminal binding is accepted. | 2026-09-05 | PRIMARY response-boundary fact |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | `gframe/duelclient.cpp#CompatRead`; `gframe/core_utils.cpp#ReadLocInfo` | The pinned reader uses narrow compatibility widths only when `compat_mode` is enabled; modern mode reads `u32` counts/sequences and `u64` descriptions/values, while modern `loc_info` is `u8,u8,u32,u32`. | 2026-09-05 | PRIMARY reader |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | `gframe/duelclient.cpp#DuelClient::ClientAnalyze` cases `MSG_SELECT_CARD`, `MSG_SELECT_UNSELECT_CARD`, `MSG_SELECT_TRIBUTE`, `MSG_SELECT_COUNTER`, `MSG_SELECT_SUM`, `MSG_SORT_CARD`, `MSG_SORT_CHAIN`, `MSG_SELECT_PLACE`, `MSG_SELECT_DISFIELD`, `MSG_ANNOUNCE_RACE`, `MSG_ANNOUNCE_ATTRIB`, `MSG_ANNOUNCE_NUMBER` | The EDOPro reader consumes the twelve messages in the heterogeneous orders above, preserves source indices for card-selection/sort UI state, treats overlay locations with the loc_info position where the reader supports it, and reads modern flags/counts with the modern widths. It does not supply a second semantic legality authority. | 2026-09-05 | PRIMARY reader |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | `gframe/event_handler.cpp#GetSuitableReturn`, `ClientField::SetResponseSelectedCards`, `ClientField::CancelOrFinish` | EDOPro encodes ordinary card subsets as response type `0`, `1`, `2`, or bitset type `3` with source indices; `MSG_SELECT_UNSELECT_CARD` uses a two-`u32` body for a selected combined index; sorting uses one byte per source occurrence; `-1` is used for cancel/finish or unchanged sort order according to the family. | 2026-09-05 | PRIMARY response producer |
| EDOPro | 30935e847165a9ef0e547fb51a43f36168fab7c7 | `gframe/duelclient.h#SetResponseI`, `SetResponseB`; `gframe/duelclient.cpp#DuelClient::SendResponse` | EDOPro stores integer responses as four native `int32` bytes, stores variable response bodies by exact byte copy, and sends the response only through the later CTOS_RESPONSE path after the prompt action is complete. | 2026-09-05 | PRIMARY response path |
| WindBot | bffe6b62679c8b2fafea8f59740e03a132517da4 | `ExecutorBase/YGOSharp.OCGWrapper.Enums/GameMessage.cs#GameMessage`; `Game/GameBehavior.cs#OnSelectBattleCmd`, `OnSelectIdleCmd`, `OnSelectCard`, `OnSelectUnselectCard`, `OnSelectTribute`, `OnSelectCounter`, `OnSelectSum`, `OnSelectDisfield`, `OnSelectPlace`, `OnCardSorting`, `OnChainSorting`, `OnAnnounceRace`, `OnAnnounceAttrib`, `OnAnnounceNumber` | WindBot independently registers and reads all twelve target IDs. Its reader corroborates the modern section orders and widths, but its AI callbacks and fallback behavior are policy code and are not semantic authority for I5A0. | 2026-09-05 | SECONDARY corroboration |
| WindBot | bffe6b62679c8b2fafea8f59740e03a132517da4 | `Game/GamePacketFactory.cs#GamePacketFactory.Create`; `Game/GameBehavior.cs#InternalOnSelectCard`, `InternalOnSelectUnselectCard`, `OnSelectCounter`, `OnSelectSum`, `OnCardSorting` | WindBot constructs CTOS_RESPONSE bodies as the family-specific integer, index-list, amount-list, sum-list, or permutation bytes described by the core consumers. Its behavior includes heuristic/automatic choices and therefore cannot authorize complete-domain or fail-closed semantics. | 2026-09-05 | SECONDARY response corroboration |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `playerop.cpp#field::process(Processors::SelectUnselectCard&)`; `libgroup.cpp#LUA_FUNCTION(SelectUnselect)` | The wire exposes finishable and cancelable as separate flags, but the core accepts the same `-1` response when either flag is true. The source therefore proves the response capability, but does not prove a distinct final response value or distinct wire identity for the two labels when both are true. | 2026-09-05 | PRIMARY continuation-semantics boundary |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `playerop.cpp#field::process(Processors::AnnounceNumber&)`; `libduel.cpp#LUA_STATIC_FUNCTION(AnnounceNumberRange)` | The message count is serialized through `static_cast<uint8_t>(core.select_options.size())`, while the writer loops over the full option vector. A range or caller producing more than 255 values can therefore create a count/trailing-body mismatch; strict I5 parsing must reject that malformed form rather than truncate it. | 2026-09-05 | PRIMARY bounded-source hazard |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `playerop.cpp#field::process(Processors::SelectCounter&)`; `operations.cpp#field::process(Processors::RemoveCounter&)` | Counter response slots are read as signed `int16_t` even though the semantic availability and the EDOPro UI response are nonnegative two-byte amounts; the core path has no explicit negative-value rejection. A future adapter must reject negative or overflowed amounts before terminal binding. | 2026-09-05 | PRIMARY fail-closed boundary |
| ygopro-core | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 | `playerop.cpp#field::process(Processors::SelectCardCodes&)`; `duel.h#duel::duel_message::write(loc_info)` | The code-only `MSG_SELECT_CARD` producer supplies no physical location beyond a zero-location placeholder. Its source occurrence is therefore wire-proven but has no private location correlation proof; a public candidate/card-code policy cannot be inferred from the location-bearing SELECT_CARD path. | 2026-09-05 | PRIMARY public-correlation boundary |

Audit consequence: all twelve transport grammars and their pinned producer/reader
symbols are identified, but `SELECT_SUM` is not fully contract-executable for
the unrestricted wire domain because the pinned source does not close the
packed-value range and its feasibility/final-validation paths disagree for
signed high alternatives. `SELECT_SUM` must remain `UNRESOLVED` until a later
contract decision either narrows the admitted source domain with proof or
freezes the exact behavior of the affected values. No production, test,
fixture, or frozen-contract file was changed by this audit.
