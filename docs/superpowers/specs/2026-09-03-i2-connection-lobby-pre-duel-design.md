# OCGForge-Ignis I2 Connection, Lobby, and Pre-Duel Design

Status: DESIGN_APPROVED=YES
Latest spec remediation: SPEC_REMEDIATION_02=APPLIED
Independent spec review: SPEC_REVIEWED=NO
Transport decision: I2_TRANSPORT_DECISION=ACCEPTED
I2_RPS_WIN_TERMINAL_AFTER=CTOS_TP_RESULT_SENT
I2_RPS_LOSS_TERMINAL_AFTER=NON_TIED_STOC_HAND_RESULT
Client contract ID: ocgforge-ignis.client.preduel.v1
Accepted main base: fff3269918f7f9120e815b900d8cc2e14e1bc52d
Expected server handshake: EXPECTED_SERVER_HANDSHAKE=4043399681
Date: 2026-09-03

## Goal

I2 provides a headless, deterministic client for one explicitly configured
local or private EDOPro room. It owns TCP lifecycle, pre-duel control traffic,
and the lobby state machine, then hands off before gameplay-message decoding.

I2 does not implement gameplay state, legality, privacy projection, model
inference, WPF, reconnect, public-server automation, or I3 behavior.

## Frozen boundaries

The accepted protocol contract is
`ocgforge-ignis.protocol.wire.v1`. I2 consumes `WireFrameCodec`,
`PacketPayloadValidator`, the accepted packet DTOs, and the opaque
`STOC_GAME_MSG`/`CTOS_RESPONSE` boundaries. It does not duplicate packet
parsing, packet IDs, binary layouts, or I1 validation.

Production transport is `TcpClient` plus `NetworkStream`. Acceptance tests use
an injected deterministic in-memory scripted transport. I2 has no Localhost
test server and no public-network test.

The only runtime endpoint is supplied by `ConnectionConfigurationV1`; the
repository contains no public-server preset, discovery, fallback, retry, or
reconnect behavior.

I2 V1 freezes the room topology to one duelist per side:

```text
I2_ROOM_TOPOLOGY=1V1_ONLY
TEAM1_REQUIRED=1
TEAM2_REQUIRED=1
DUEL_RELAY=FORBIDDEN
OBSERVER_ROLE=UNSUPPORTED
```

After validated `STOC_JOIN_GAME`, the session rejects `HostInfo.team1 != 1`,
`HostInfo.team2 != 1`, and any non-single/relay topology as
`UNSUPPORTED_ROOM_TOPOLOGY`. A joiner that receives observer type `7` is also
rejected; I2 does not operate an observer session.

## Architecture

```text
I2SessionRunner
 ├─ IByteTransport
 │   ├─ TcpClientTransport       production
 │   └─ ScriptedTransport         tests only
 ├─ bounded receive buffer
 ├─ accepted I1 WireFrameCodec
 ├─ accepted I1 PacketPayloadValidator
 └─ PreDuelStateMachine
```

`IByteTransport` is the Client-layer transport seam. It owns connect, byte
reads, byte writes, cancellation, shutdown, and disposal. It does not own
packet parsing, packet construction, lobby semantics, state transitions,
choice logic, retry, or reconnect.

One serialized session owner mutates all I2 state. Transport reads and caller
commands are processed in one deterministic order. Events and state never
contain receive timing, chunk boundaries, callback counts, socket handles,
ephemeral ports, process IDs, task IDs, or wall-clock values.

## Configuration and deck input

`ConnectionConfigurationV1` is immutable and contains only:

- explicit host;
- TCP port;
- display name;
- unsigned protocol game/room ID;
- a dedicated redacted room-password value;
- a positive finite connection-timeout policy.

Validation rejects blank hosts, ports outside 1 through 65535, invalid fixed
UTF-16 display names, display names that exceed the I1 20-code-unit field,
passwords that exceed that field, and invalid timeout values. A typed `uint`
game/room ID is accepted without string parsing or silent conversion; values
that cannot be represented by the protocol are rejected before a session
starts.

The password type exposes its value only to the join-packet construction
boundary. Its `ToString()` is redacted. Passwords never enter exception text,
logs, state snapshots, semantic events, equality/hash inputs, or handoff
values.

`PrevalidatedProtocolDeck` owns copied, ordered main-plus-extra and side card
code sequences. It exposes read-only views, preserves source order exactly,
and performs no file I/O, card-database lookup, legality validation,
deduplication, sorting, or archetype inference. EDOPro remains the deck
acceptance authority.

## State machine

The public state model is explicit:

```text
Created
Connecting
TransportConnected
PlayerInfoSent
JoinRequestSent
LobbyJoined
DeckSubmitted
ReadyRequested
Ready
NotReadyRequested
Starting
DuelStarted
WaitingForHandChoice
WaitingForHandResult
WaitingForTpRequest
WaitingForTpChoice
HandedOff
Closed
Failed
```

`DuelStarted` is deliberately a nonterminal marker. The pinned EDOPro server
sends `STOC_DUEL_START` before `STOC_SELECT_HAND`; I2 must therefore continue
the RPS/turn-preference handshake after the marker. `HandedOff` is the terminal
I2 state.

The normal transitions are:

```text
Created
  → Connecting
  → TransportConnected
  → PlayerInfoSent       [CTOS_PLAYER_INFO]
  → JoinRequestSent      [CTOS_JOIN_GAME]
  → LobbyJoined          [validated STOC_JOIN_GAME]
  → DeckSubmitted        [CTOS_UPDATE_DECK]
  → ReadyRequested       [CTOS_HS_READY]
  → Ready                [own STOC_HS_PLAYER_CHANGE READY]
  ├─→ NotReadyRequested   [CTOS_HS_NOTREADY]
  │   └─→ DeckSubmitted   [own STOC_HS_PLAYER_CHANGE NOTREADY]
  ├─→ Starting            [host-only CTOS_HS_START after both required slots are ready]
  │   └─→ DuelStarted     [validated STOC_DUEL_START]
  └─→ DuelStarted         [validated server start for non-host]

DuelStarted
  → WaitingForHandChoice [validated STOC_SELECT_HAND]
WaitingForHandChoice
  → WaitingForHandResult [one CTOS_HAND_RESULT]
WaitingForHandResult
  → DuelStarted          [STOC_HAND_RESULT tie; await a new STOC_SELECT_HAND]
  → WaitingForTpRequest  [non-tied STOC_HAND_RESULT; this client wins]
  → HandedOff            [non-tied STOC_HAND_RESULT; this client loses]
WaitingForTpRequest
  → WaitingForTpChoice   [validated STOC_SELECT_TP]
  → HandedOff            [one CTOS_TP_RESULT successfully sent]
```

`CTOS_HS_READY` changes the state to `ReadyRequested`, not `Ready`. The server
checks deck size and content after that request. Only the own-position
`PLAYERCHANGE_READY` event establishes `Ready`. An own `PLAYERCHANGE_NOTREADY`
returns to `DeckSubmitted` without ever passing through `Ready`; a deck error
terminates the attempt as `DECK_REJECTED` and cannot be repaired implicitly.
`CTOS_HS_START` is legal only for a proven host whose own status is `Ready` and
whose two required V1 player slots, positions 0 and 1, are occupied and
server-confirmed ready. A host cannot start merely because its own deck was
accepted. `CTOS_HS_NOTREADY` changes the state to `NotReadyRequested`, not
`DeckSubmitted`; only the own-position `PLAYERCHANGE_NOTREADY` event confirms
the return to `DeckSubmitted`. While `NotReadyRequested`, another not-ready
request and host start are both illegal.

If this client wins a non-tied RPS result, the opponent receives no TP request
for this session until the server sends `STOC_SELECT_TP` to the winner. I2
therefore uses the explicit `WaitingForTpRequest` state. If this client loses,
the opponent receives the TP request and I2 creates the terminal handoff
immediately after the validated `STOC_HAND_RESULT`, before the first gameplay
message. It does not wait for an `STOC_TP_RESULT`, because the pinned server
does not send that packet after `CTOS_TP_RESULT`.

Every transition names its source state and trigger. An impossible packet,
duplicate terminal marker, stale choice, illegal command, or packet after a
failure causes a structured failure; I2 never repairs state or retries.

## Commands and packet ownership

The Client API exposes narrow operations rather than a generic packet sender:

- start one configured session, which connects and sends player info then join;
- submit one `PrevalidatedProtocolDeck` only in `LobbyJoined`;
- send ready only after deck submission, entering `ReadyRequested` until the
  server confirms the own position as ready;
- send not-ready only from `Ready`, entering `NotReadyRequested` until the
  server confirms the own position as not-ready;
- request duel start only when the validated type-change state proves this
  client is host, the session is `Ready`, positions 0 and 1 are occupied, and
  both required duelists are server-confirmed ready;
- submit a caller-provided pre-duel choice only while its matching request is
  pending;
- leave explicitly before handoff;
- cancel or close explicitly.

All outgoing packets are constructed with accepted I1 DTO encoders and
`WireFrameCodec`. I2 has no public `SendPacket(byte, byte[])` escape hatch.
`CTOS_SURRENDER` and `CTOS_TIME_CONFIRM` are not exposed because they are not
needed by the V1 pre-duel state machine.

## Lobby and pre-duel events

The deterministic event vocabulary contains only semantic coordination facts:

```text
TransportConnected
PlayerInfoSent
JoinRequestSent
LobbyJoined
OwnTypeChanged
PlayerEntered
PlayerStatusChanged
WatcherCountChanged
DeckSubmitted
ReadyRequested
ReadySent
NotReadySent
NotReadyRequested
DuelStartRequested
DuelStarted
PlayerMoved
RpsRequested
RpsChoiceSent
RpsResultReceived
TurnPreferenceRequested
TurnPreferenceSent
HandedOff
Failed
Closed
```

Lobby occupants use the protocol-visible position as identity. Names are
display metadata and may repeat. Type-change data is decoded as the own
pre-duel lobby position plus the host flag carried by the protocol byte.
Player-change data uses the encoded position/status values verified from the
pinned upstream.
Dictionary or object insertion order is never an identity or ordering source.
The pinned lobby status values are `OBSERVE=0x8`, `READY=0x9`,
`NOTREADY=0xa`, and `LEAVE=0xb`; other non-position status values fail closed.
Low-nibble values `0x0` through `0x5` are valid upstream-defined duelist
destination positions and produce `PlayerMoved`, not malformed-packet errors.
For I2's 1v1 topology, only moves whose source and destination positions are
0 or 1 are representable. A valid upstream move involving another duelist
slot returns `UNSUPPORTED_ROOM_TOPOLOGY`; an unrepresentable move returns the
explicit `UNSUPPORTED_LOBBY_POSITION_MOVE` failure. Neither is classified as a
malformed packet. A low-nibble observer type `7` in the own `STOC_TYPE_CHANGE`
is `UNSUPPORTED_ROOM_TOPOLOGY`.

The validated handlers cover the accepted pre-duel control surface:

- `STOC_ERROR_MSG` maps to structured join, version, deck, or unsupported-side
  failures;
- `STOC_JOIN_GAME` first requires `HostInfo.handshake == 4043399681`,
  `HostInfo.team1 == 1`, `HostInfo.team2 == 1`, and single-duel mode. A
  mismatch fails closed as `SERVER_HANDSHAKE_MISMATCH` or
  `UNSUPPORTED_ROOM_TOPOLOGY` and never enables legacy compatibility mode or
  HostInfo reinterpretation. A matching packet records validated public
  `HostInfo` and enters the lobby;
- `STOC_TYPE_CHANGE`, `STOC_HS_PLAYER_ENTER`,
  `STOC_HS_PLAYER_CHANGE`, and `STOC_HS_WATCH_CHANGE` update lobby facts;
- `STOC_DUEL_START` enters the nonterminal `DuelStarted` marker state;
- `STOC_SELECT_HAND`, `STOC_HAND_RESULT`, and `STOC_SELECT_TP` drive the
  explicit pre-duel choice states;
- `STOC_DUEL_END` and `STOC_LEAVE_GAME` terminate early with structured
  failures where appropriate;
- `STOC_GAME_MSG` is never decoded and is fail-closed before handoff;
- `STOC_TP_RESULT` is not emitted by the pinned server path and is rejected as
  an unexpected packet if it appears before handoff rather than being guessed
  or treated as a required acknowledgement.

For a tied `STOC_HAND_RESULT`, the session returns to the pre-request
`DuelStarted` state and waits for the next `STOC_SELECT_HAND`. That request
creates a new deterministic choice token; the previous token remains stale.

## Pre-duel choices

`PreDuelChoiceRequest` contains a deterministic session-local token, a choice
kind, and an immutable legal domain. The domains are:

```text
RPS              = {1, 2, 3}
turn preference  = {0, 1}
```

The server's RPS result is interpreted only to determine whether this client
must receive the TP request or may hand off after losing. I2 does not choose a
strategy. It sends exactly one `CTOS_HAND_RESULT` or `CTOS_TP_RESULT` for one
currently pending request. A duplicate, stale, wrong-kind, or out-of-domain
choice returns `CHOICE_NOT_PENDING`, `STALE_CHOICE`, or a corresponding stable
I2 error and emits no packet.

## Receive buffering and failures

The bounded receive buffer retains incomplete bytes exactly and repeatedly
calls the accepted I1 validated parser. Complete coalesced frames are consumed
one at a time using I1's exact consumed-byte count. The buffer is bounded by
the largest representable I1 frame plus its length prefix; malformed growth
fails closed before allocation or publication.

Transport errors map to stable I2 codes such as
`CONNECTION_FAILED`, `CONNECTION_TIMEOUT`, `REMOTE_CLOSED`,
`TRUNCATED_STREAM`, and `SEND_FAILED`. I1 validation errors map to
`PROTOCOL_FAILURE`, `VERSION_MISMATCH`, `JOIN_REJECTED`, `DECK_REJECTED`,
`UNSUPPORTED_PACKET`, or `UNEXPECTED_PACKET_FOR_STATE` as appropriate.
`SIDEERROR` fails closed because siding is outside I2 V1.

Cancellation closes the transport, stops pending read/write work, emits one
deterministic `Closed` outcome, and leaves no receive loop alive. Cancellation
does not become a remote error. A failed connection attempt is final for that
session; a caller must create a new session explicitly.

## Terminal handoff

`PreDuelSessionV1` is immutable and contains only validated public pre-duel
facts needed by the next layer: `HostInfo`, own `pre_duel_lobby_position`, host
flag, the public lobby/pre-duel outcome, and the deterministic event sequence.
`FINAL_GAMEPLAY_PERSPECTIVE=UNRESOLVED_AT_I2_HANDOFF`; I2 never treats the
pre-duel lobby position as the final gameplay perspective. I3 must establish
that perspective from the first `MSG_START`.

`PreDuelSessionV1` itself contains no raw hidden gameplay state, card zones,
model state, candidate data, password, socket object, or transport identity.

The public handoff is paired with an internal, one-time
`GameplayTransportHandoffV1` owned by the session runner. The internal value
contains the same live `IByteTransport`, the immutable `PreDuelSessionV1`, and
an exact owned copy of every unread receive-buffer byte. On handoff, I2 stops
parsing immediately, transfers transport ownership exactly once, and neither
consumes, rejects, nor discards a trailing `STOC_GAME_MSG`. The first
`STOC_GAME_MSG` belongs to the future gameplay consumer; I2 never parses it.

The handoff is emitted exactly once. The winning-RPS path emits it only after
the single `CTOS_TP_RESULT` write succeeds. The losing-RPS path emits it after
the non-tied `STOC_HAND_RESULT` proves that the peer owns the TP choice.

## Test design

Tests use a deterministic `ScriptedTransport` implementing `IByteTransport`.
It records framed CTOS bytes and supplies a fixed server transcript as:

- one byte at a time;
- one complete frame at a time;
- all frames coalesced;
- several fixed irregular chunk patterns.

Every pattern must produce identical semantic state/event sequences, CTOS
frame bytes, and terminal result.

The successful transcript covers connect, player-info, join, validated host
info with the expected server handshake, type/lobby events, deck upload,
ready request, server-confirmed ready, server start, duel-start marker, RPS,
hand result, tie-loop coverage, TP choice or loss handoff, and exactly one
terminal handoff.
Failure transcripts cover password rejection, version rejection, deck
rejection, unexpected ordering, truncated stream, remote close, unsupported
STOC, handshake mismatch, invalid commands, duplicate packets, stale choices,
unsupported position moves, cancellation, post-terminal input, and a
coalesced final pre-duel frame plus first `STOC_GAME_MSG` boundary.
They explicitly include `team1 != 1`, `team2 != 1`, relay/single-mode mismatch,
observer self-type `7`, duplicate `STOC_HAND_RESULT` while
`WaitingForTpRequest`, `STOC_SELECT_HAND`/TP results in the wrong states,
`CTOS_HS_NOTREADY` while `NotReadyRequested`, and host start while
`NotReadyRequested`.

Tests assert that passwords never occur in state, events, exception messages,
or test diagnostics; that deck order is byte-for-byte preserved; that no
retry/reconnect occurs; that position moves are either represented or rejected
with `UNSUPPORTED_LOBBY_POSITION_MOVE`; that the public handoff contains
`pre_duel_lobby_position` only; that the internal handoff retains the exact
unconsumed suffix and live transport; and that no inner duel message is parsed.

## Provenance and CI

The implementation updates `PROTOCOL_PROVENANCE.md` only for facts used by
I2, including the pinned EDOPro `duelclient.cpp`, `generic_duel.cpp`, and
`network.h` paths for connection order, server-handshake compatibility, lobby
ready confirmation, player/status/position-move encoding, RPS/TP behavior, and
the `STOC_DUEL_START`/`STOC_SELECT_HAND` ordering. No external source
implementation is copied.

The topology lock specifically records `MODE_SINGLE=0x0`, `MODE_RELAY=0x3`,
duelist positions `0..5`, observer position `7`, and the relay-specific
first-duel readiness rule from `gframe/network.h` and
`gframe/generic_duel.cpp`. These facts authorize I2 to reject non-1v1 rooms;
they do not authorize team or relay implementation.

The hosted workflow runs the accepted I1 build/tests and the I2 Client
build/tests. It uses no EDOPro download, public network, Localhost server,
timing-dependent integration, or runtime asset.

## Explicit non-goals

I2 does not create rooms, discover rooms, access public servers, reconnect,
rematch, catch up, side, parse `STOC_GAME_MSG`, construct gameplay state,
project privacy-safe observations, create decision candidates, load a model,
run a model process, or implement WPF.
