# I2 Client V1 Synthetic Transcripts

These reviewable transcripts use synthetic names, card codes, and room IDs.
They contain no EDOPro runtime data, public server address, password, deck
file, or copied upstream buffer. Every frame is constructed through the
accepted I1 protocol codec.

## Frozen V1 pre-duel assumptions

```text
CLIENT_CONTRACT_ID=ocgforge-ignis.client.preduel.v1
I2_ROOM_TOPOLOGY=1V1_ONLY
TEAM1_REQUIRED=1
TEAM2_REQUIRED=1
DUEL_RELAY_FLAG=0x00000080
BEST_OF_REQUIRED=1
EXPECTED_SERVER_HANDSHAKE=4043399681
FINAL_GAMEPLAY_PERSPECTIVE=UNRESOLVED_AT_I2_HANDOFF
```

`HostInfo.mode` is set to `0` in the modern creator path but is not the relay
detector. The relay gate is the `DUEL_RELAY` bit in `DuelFlagLow`.

## Successful winner transcript

The scripted server stream and caller commands are ordered as follows:

```text
transport connected
CTOS_PLAYER_INFO(name=Ignis)
CTOS_JOIN_GAME(protocol=0x1354, client/core=41.0/11.0)
STOC_JOIN_GAME(team1=1, team2=1, mode=0, duel_flag_low=0,
               best_of=1, handshake=4043399681)
STOC_TYPE_CHANGE(type=0x10)                 own pre-duel position 0, host
STOC_HS_PLAYER_ENTER(pos=0, name=Ignis)
STOC_HS_PLAYER_ENTER(pos=1, name=Opponent)
CTOS_UPDATE_DECK(main+extra=[11223344], side=[aabbccdd])
CTOS_HS_READY
STOC_HS_PLAYER_CHANGE(status=0x09)           own READY confirmation
STOC_HS_PLAYER_CHANGE(status=0x19)           opponent READY confirmation
CTOS_HS_START
STOC_DUEL_START                              nonterminal marker
STOC_SELECT_HAND
CTOS_HAND_RESULT(value=1)
STOC_HAND_RESULT(res1=1, res2=3)              recipient-relative own WIN
STOC_SELECT_TP
CTOS_TP_RESULT(value=0)
STOC_GAME_MSG(opaque bytes)                   next-layer suffix
```

The `STOC_SELECT_TP` frame and the first `STOC_GAME_MSG` frame are delivered
in one scripted read. I2 stops parsing at the choice boundary, then transfers
the exact game-message bytes with the live transport after the successful TP
write.

## Loss and tie transcripts

```text
RPS loss:  STOC_HAND_RESULT(1,2) → HandedOff
           no CTOS_TP_RESULT is emitted by this client

RPS tie:   STOC_HAND_RESULT(1,1) → DuelStarted
           next STOC_SELECT_HAND → new choice token ordinal
           previous token remains stale
```

The recipient-relative result table is:

```text
tie:  (1,1), (2,2), (3,3)
win:  (1,3), (2,1), (3,2)
loss: (1,2), (2,3), (3,1)
```

Values outside `1..3`, duplicate results in `WaitingForTpRequest`, and TP
requests outside `WaitingForTpRequest` fail closed.

## Failure transcripts

The test executable covers:

- `team1 != 1`, `team2 != 1`, `DuelFlagLow & DUEL_RELAY_FLAG != 0` with
  `mode=0`, `best_of != 1`, and wrong server handshake;
- own `STOC_TYPE_CHANGE` observer position `7`;
- duplicate join, type-change before join, duel-start before readiness, and
  packets after `Failed`/`Closed`;
- `ReadyRequested` without server confirmation, `NotReadyRequested` without
  server confirmation, duplicate ready/not-ready commands, and host start
  while not-ready is pending;
- remote password/version/deck/side errors, unsupported STOC, truncated EOF,
  remote close, cancellation, stale choices, and invalid RPS bytes.

Every failure stops the runner, closes the owned transport once, and performs
no retry or reconnect. Post-handoff bytes are not I2 input.

## Chunking evidence

The same server byte transcript is delivered as:

```text
one byte at a time
one complete frame at a time
all frames coalesced
irregular chunks [1, 2, 5, 3, 8, 13, 21]
```

Semantic state sequence, event sequence, framed CTOS output, and terminal
result must be identical. Transport read timing and chunk counts are not
semantic identity. The coalesced handoff case additionally asserts exact
pending-byte preservation.
