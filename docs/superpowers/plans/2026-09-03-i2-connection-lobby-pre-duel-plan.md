# I2 Connection, Lobby, and Pre-Duel Implementation Plan

Execution status: IMPLEMENTED_PENDING_PR_REVIEW

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the deterministic 1v1 local/private EDOPro pre-duel client on top of the accepted I1 wire codec, ending with an explicit transport-preserving handoff before gameplay-message parsing.

**Architecture:** A single serialized `I2SessionRunner` owns mutable state, an injected `IByteTransport`, and a bounded receive buffer. It sends only I1-encoded packets, feeds every received byte through `PacketPayloadValidator`, applies explicit pre-duel transitions, and returns either a failed/closed terminal or an internal `GameplayTransportHandoffV1` containing the live transport and exact unread suffix.

**Tech Stack:** C# / .NET 10, platform-neutral Client library, production `TcpClient`/`NetworkStream`, deterministic in-memory test executable, no third-party runtime dependencies, and the accepted `OCGForge.Ignis.Protocol` project.

---

## Frozen decisions

- Base: `fff3269918f7f9120e815b900d8cc2e14e1bc52d`.
- Protocol contract: `ocgforge-ignis.protocol.wire.v1`.
- Client contract: `ocgforge-ignis.client.preduel.v1`.
- Topology: exactly one duelist at position `0` and one at position `1`.
- `STOC_JOIN_GAME` enters non-actionable `JoinAccepted`; valid own
  `STOC_TYPE_CHANGE` is required before action-ready `LobbyJoined`.
- Relay gate: reject `(HostInfo.DuelFlagLow & 0x00000080u) != 0`.
- Series gate: reject `HostInfo.BestOf != 1`.
- Server handshake: require `HostInfo.Handshake == 4043399681u`.
- RPS domain: `{1, 2, 3}`; TP domain: `{0, 1}`.
- Choice tokens: request ordinals `0, 1, 2, ...`, checked `ulong` increment.
- `STOC_DUEL_START` is a nonterminal marker. Win handoff follows successful `CTOS_TP_RESULT`; loss handoff follows non-tied recipient-relative `STOC_HAND_RESULT`.
- An incomplete I1 frame blocks the pump until completion/EOF/error/cancellation;
  already-read bytes after a pending choice fail closed.
- Explicit leave closes the owned transport and never sends `CTOS_LEAVE_GAME`.
- Remote readiness invalidation while `Starting` returns to `Ready` when own
  readiness remains authoritative.
- `Failed`/`Closed` close the owned transport exactly once. `HandedOff` transfers it exactly once and does not close it.
- `STOC_GAME_MSG` is never parsed by I2; unread bytes transfer unchanged.

## Planned files

Create:

- `src/OCGForge.Ignis.Client/OCGForge.Ignis.Client.csproj`
- `src/OCGForge.Ignis.Client/ClientContractV1.cs`
- `src/OCGForge.Ignis.Client/ClientErrors.cs`
- `src/OCGForge.Ignis.Client/ConnectionConfigurationV1.cs`
- `src/OCGForge.Ignis.Client/PrevalidatedProtocolDeck.cs`
- `src/OCGForge.Ignis.Client/IByteTransport.cs`
- `src/OCGForge.Ignis.Client/TcpClientTransport.cs`
- `src/OCGForge.Ignis.Client/ReceiveBuffer.cs`
- `src/OCGForge.Ignis.Client/I2SessionState.cs`
- `src/OCGForge.Ignis.Client/I2Events.cs`
- `src/OCGForge.Ignis.Client/LobbyState.cs`
- `src/OCGForge.Ignis.Client/PreDuelChoices.cs`
- `src/OCGForge.Ignis.Client/PreDuelSessionV1.cs`
- `src/OCGForge.Ignis.Client/GameplayTransportHandoffV1.cs`
- `src/OCGForge.Ignis.Client/PreDuelStateMachine.cs`
- `src/OCGForge.Ignis.Client/I2SessionRunner.cs`
- `src/OCGForge.Ignis.Client/Properties/AssemblyInfo.cs`
- `tests/OCGForge.Ignis.Client.Tests/OCGForge.Ignis.Client.Tests.csproj`
- `tests/OCGForge.Ignis.Client.Tests/ScriptedTransport.cs`
- `tests/OCGForge.Ignis.Client.Tests/Program.cs`
- `fixtures/client/v1/README.md`

Modify:

- `README.md` — keep current branch status consistent with the accepted I1 and authorized I2 scope.
- `.github/workflows/i1-protocol.yml`
- `PROTOCOL_PROVENANCE.md`

Never create `src/OCGForge.Ignis.State/`, `src/OCGForge.Ignis.Decisions/`, `src/OCGForge.Ignis.Inference/`, `src/OCGForge.Ignis.App/`, or WPF files.

## Task 1: Client project, configuration, deck, and stable errors

**Files:** the Client project, `ClientContractV1.cs`, `ClientErrors.cs`, `ConnectionConfigurationV1.cs`, `PrevalidatedProtocolDeck.cs`, the Client test project, and the first test group.

- [ ] **Step 1: Write RED tests.** Assert the contract ID, relay flag, best-of, handshake, RPS/TP domains, blank/invalid host and ports, fixed-string limits, embedded NUL rejection, positive finite timeout, redacted `ToString()`, and synthetic-password exclusion from output.
- [ ] **Step 2: Verify RED.** Run:

```powershell
dotnet run --project tests/OCGForge.Ignis.Client.Tests/OCGForge.Ignis.Client.Tests.csproj --configuration Release
```

Expected: missing Client project/types cause a build failure.

- [ ] **Step 3: Implement minimal value-owned types.** The Client project targets `net10.0`, enables nullable, deterministic compilation, analyzers, warnings-as-errors, and references only the Protocol project. `ConnectionConfigurationV1` is a sealed immutable class with host, port, player name, `uint GameId`, a redacted `RoomPasswordV1`, and finite positive timeout. Validate names/passwords through accepted I1 fixed-string encoding without truncation. `PrevalidatedProtocolDeck` copies ordered main-plus-extra and side sequences and rejects only sizes that cannot fit I1 update-deck framing; it performs no legality work. `I2ErrorCode` contains stable codes for invalid state, connection/timeout/cancel/close/truncation, protocol/version/handshake/topology failure, join/deck/side rejection, choice/stale choice, send failure, and ownership failure.
- [ ] **Step 4: Verify green.** Build/run Client tests and rerun the unchanged I1 executable. Both must pass with zero build warnings/errors.

## Task 2: Transport seam and bounded buffer

**Files:** `IByteTransport.cs`, `TcpClientTransport.cs`, `ReceiveBuffer.cs`, `ScriptedTransport.cs`, and transport tests.

- [ ] **Step 1: Write RED tests.** Test deterministic scripted reads/writes, EOF, one close, splitting a scripted chunk when the destination is smaller, one-byte appends, complete/coalesced frames, incomplete retention, exact unread bytes, and capacity rejection.
- [ ] **Step 2: Verify RED.** Run the Client executable; missing seam/buffer symbols must fail the build.
- [ ] **Step 3: Implement the seam exactly.**

```csharp
public interface IByteTransport : IAsyncDisposable
{
    ValueTask ConnectAsync(string host, int port, TimeSpan timeout, CancellationToken cancellationToken);
    ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken);
    ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken);
    ValueTask CloseAsync();
}
```

`TcpClientTransport` owns only `TcpClient`, `NetworkStream`, connect timeout, cancellation, writes, reads, shutdown, and disposal. It knows no packet type or payload. `ReceiveBuffer` is bounded to `ProtocolContractV1.MaxPacketLength + ProtocolContractV1.LengthPrefixSize`, removes only I1 consumed bytes, and returns an owned exact unread copy. `ScriptedTransport` exists only in tests and uses no socket APIs.
- [ ] **Step 4: Verify green.** Run Client and I1 builds/tests. No transport test uses the public internet or a Localhost server.

## Task 3: Immutable state, events, choices, lobby, and handoff values

**Files:** `I2SessionState.cs`, `I2Events.cs`, `LobbyState.cs`, `PreDuelChoices.cs`, `PreDuelSessionV1.cs`, `GameplayTransportHandoffV1.cs`, `AssemblyInfo.cs`, and value tests.

- [ ] **Step 1: Write RED tests.** Assert explicit states, duplicate names keyed by position, immutable legal domains, token ordinals, stale-token inequality, value-only public handoff, exact pending bytes, one-time transport ownership, and absence of password/endpoint/socket/PID/time/chunk metadata from public values.
- [ ] **Step 2: Verify RED.** Run the Client executable; missing value types must fail the build.
- [ ] **Step 3: Implement the models.** Use states `Created`, `Connecting`, `TransportConnected`, `PlayerInfoSent`, `JoinRequestSent`, `JoinAccepted`, `LobbyJoined`, `DeckSubmitted`, `ReadyRequested`, `Ready`, `NotReadyRequested`, `Starting`, `DuelStarted`, `WaitingForHandChoice`, `WaitingForHandResult`, `WaitingForTpRequest`, `WaitingForTpChoice`, `HandedOff`, `Closed`, and `Failed`. Define events exactly once for successful ready/not-ready sends as `ReadyRequested`/`NotReadyRequested`; do not add separate sent-event names. Position-key lobby snapshots are sorted by position. `PreDuelChoiceTokenV1` stores only `ulong Ordinal`. `PreDuelSessionV1` contains `HostInfo`, `PreDuelLobbyPosition`, host flag, public outcome, and copied events; it contains no final gameplay perspective. Internal `GameplayTransportHandoffV1` owns the live transport, public facts, and exact unread bytes and rejects a second transfer.
- [ ] **Step 4: Verify green.** Run value tests and I1 regression.

## Task 4: Pure state-machine reducer

**Files:** `PreDuelStateMachine.cs` and reducer tests.

- [ ] **Step 1: Write RED transition tests.** Cover valid/wrong handshake, `team1/team2`, relay bit with `mode=0`, `best_of`, observer type `7`, `ReadyRequested`/server READY, `NotReadyRequested`/server NOTREADY, host/all-slot start rules, `STOC_DUEL_START`, RPS requests, all tie/win/loss pairs, invalid result values, tie renewal, `WaitingForTpRequest`, TP handoff, position moves, duplicate/out-of-order packets, and stable failures.
- [ ] **Step 2: Verify RED.** Run Client tests; missing reducer symbols must fail the build.
- [ ] **Step 3: Implement a pure reducer.** It receives validated I1 packets or narrow caller commands and returns next state, immutable events, optional choice request, optional public handoff, and one `I2ErrorCode`. It performs no I/O. Require `Team1 == 1`, `Team2 == 1`, relay bit clear, `BestOf == 1`, and `Handshake == 4043399681u`; never enter legacy compatibility mode. Validate both RPS values in `1..3`, then apply tie, win `(1,3)/(2,1)/(3,2)`, or loss `(1,2)/(2,3)/(3,1)` exactly. New requests publish the current ordinal and then checked-increment once; ties invalidate the old token and wait for a new select-hand. Low-nibble `0..5` values are upstream move destinations; only 0/1 moves are represented, while valid moves outside 1v1 fail as `UnsupportedRoomTopology`, not malformed input.
- [ ] **Step 4: Verify green.** Run all reducer tests and I1 regression.

## Task 5: Serialized session runner

**Files:** `src/OCGForge.Ignis.Client/I2SessionRunner.cs` and `tests/OCGForge.Ignis.Client.Tests/Program.cs`.

- [ ] **Step 1: Write RED transcript tests.** With `ScriptedTransport`, assert `StartAsync` connects and emits exactly I1-encoded `CTOS_PLAYER_INFO` then `CTOS_JOIN_GAME`; test winner/loser RPS, topology/version/join/deck/side errors, unsupported packets, close/truncation, invalid order, cancellation, and secret redaction.
- [ ] **Step 2: Verify RED.** Missing runner symbols must fail the Client build.
- [ ] **Step 3: Implement the narrow API.**

```csharp
ValueTask<I2Result> StartAsync(ConnectionConfigurationV1 configuration, CancellationToken cancellationToken);
ValueTask<I2Result> SubmitDeckAsync(PrevalidatedProtocolDeck deck, CancellationToken cancellationToken);
ValueTask<I2Result> RequestReadyAsync(CancellationToken cancellationToken);
ValueTask<I2Result> RequestNotReadyAsync(CancellationToken cancellationToken);
ValueTask<I2Result> RequestDuelStartAsync(CancellationToken cancellationToken);
ValueTask<I2Result> SubmitChoiceAsync(PreDuelChoiceTokenV1 token, byte value, CancellationToken cancellationToken);
ValueTask<I2PumpResult> PumpReadAsync(CancellationToken cancellationToken);
ValueTask<I2Result> CloseAsync();
```

Serialize all calls with one operation gate. `StartAsync` validates configuration, connects, sends player info and join using I1. `PumpReadAsync` repeatedly calls `PacketPayloadValidator.TryReadValidatedStoc`, removes exact consumed frames, waits through incomplete frames, and stops only at a causal choice boundary or handoff boundary. Any bytes already buffered after a published choice fail closed. Every I1 error maps to a stable I2 failure. Failed sessions stop reads/writes and close once; cancellation closes without a remote-error event; EOF with pending bytes is `TruncatedStream`; EOF without pending bytes is `RemoteClosed`; no retry/reconnect exists. The runner never exposes a generic packet sender, and explicit leave closes the transport without sending `CTOS_LEAVE_GAME`.
- [ ] **Step 4: Verify green.** Run runner tests, I1 tests, and forbidden-symbol/secret scans.

## Task 6: Transcript, handoff-boundary, and chunking evidence

**Files:** `tests/OCGForge.Ignis.Client.Tests/Program.cs` and `fixtures/client/v1/README.md`.

- [ ] **Step 1: Add successful 1v1 transcript.** Use valid V1 HostInfo, own type `0x10`, player positions 0/1, deck upload, server-confirmed READY for both, host start, `STOC_DUEL_START`, `STOC_SELECT_HAND`, RPS choice, recipient-relative win `(1,3)`, `STOC_SELECT_TP`, TP choice `0`, and a future `STOC_GAME_MSG` read by the transferred transport. Assert exact CTOS frame bytes and no pre-response game-message suffix.
- [ ] **Step 2: Add loss/tie transcripts.** Loss `(1,2)` hands off after `STOC_HAND_RESULT`; tie `(1,1)` returns to `DuelStarted`, requires a new select-hand, and produces a new token. Assert old-token rejection.
- [ ] **Step 3: Add chunking metamorphisms.** Execute the same byte transcript as one-byte chunks, one-frame chunks, all coalesced, and irregular `[1,2,5,3,8,13,21]`; compare semantic states/events, outbound CTOS bytes, terminal result, and pending suffix, never read counts/timing.
- [ ] **Step 4: Add failure/terminal tests.** Cover topology/relay/best-of/observer, duplicate and reordered packets, incomplete-frame action barriers, pre-response choice bytes, invalid RPS, stale choices, transport-close leave, `Starting` invalidation/retry, `Failed` close-once/no-later-read, `Closed`, cancellation, duplicate terminal markers, and post-handoff bytes belonging only to the next layer.
- [ ] **Step 5: Verify twice.** Run Client and I1 executables in two fresh processes and compare complete outputs.

## Task 7: Provenance, CI, full gates, commit, and PR

**Files:** `PROTOCOL_PROVENANCE.md`, `.github/workflows/i1-protocol.yml`, fixture README, and only planned source/test files.

- [ ] **Step 1: Add provenance.** Record exact pinned paths/facts for `DUEL_RELAY=0x80` (`gframe/ocgapi_constants.h`), creator flag/mode/best-of and recipient orientation (`gframe/duelclient.cpp`), server GenericDuel relay/best-of propagation (`gframe/netserver.cpp`), HostInfo/player/status layout (`gframe/network.h`), and CheckReady/StartDuel/RPS/TP behavior (`gframe/generic_duel.cpp`). Do not copy source.
- [ ] **Step 2: Extend hosted CI.** Retain I1 restore/build/run and add restore/build/run for the Client test project. Use no EDOPro download, public network, Localhost server, or timing-based test.
- [ ] **Step 3: Run full local gates.**

```powershell
dotnet restore tests/OCGForge.Ignis.Client.Tests/OCGForge.Ignis.Client.Tests.csproj
dotnet build src/OCGForge.Ignis.Protocol/OCGForge.Ignis.Protocol.csproj --configuration Release --no-restore
dotnet build src/OCGForge.Ignis.Client/OCGForge.Ignis.Client.csproj --configuration Release --no-restore
dotnet build tests/OCGForge.Ignis.Protocol.Tests/OCGForge.Ignis.Protocol.Tests.csproj --configuration Release --no-restore
dotnet build tests/OCGForge.Ignis.Client.Tests/OCGForge.Ignis.Client.Tests.csproj --configuration Release --no-restore
dotnet run --project tests/OCGForge.Ignis.Protocol.Tests/OCGForge.Ignis.Protocol.Tests.csproj --configuration Release --no-build --no-restore
dotnet run --project tests/OCGForge.Ignis.Client.Tests/OCGForge.Ignis.Client.Tests.csproj --configuration Release --no-build --no-restore
git diff --check
```

Audit changed paths and assert no State/Decisions/Inference/App/WPF code, copied external source, CDB/deck/checkpoint files, public server preset, or gameplay symbols. Confirm I2 uses I1 only and no Protocol dependency points to Client.
- [ ] **Step 4: Self-review I2-G01 through I2-G25.** Report only gates backed by current command/test output.
- [ ] **Step 5: Commit/push/PR/STOP.** Commit `feat: add deterministic pre-duel client state machine`, push `chris/i2-connection-lobby-state-machine`, open `I2: connection and pre-duel state machine` against `main`, wait for exact-head CI, do not merge, and report `I3_IMPLEMENTED=NO`, `I3_AUTHORIZED=NO`.
