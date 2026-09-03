# I1 Protocol Golden Vectors V1

These synthetic vectors are reviewable, independently constructed protocol
fixtures. They contain no passwords, account data, external deck material, or
copied upstream buffers.

The outer frame is:

~~~text
uint16_le frame_length
uint8     packet_type
byte[]    payload
~~~

The frame length includes the packet type and payload, but not the two-byte
length prefix. Where a repeated zero sequence is shown, the notation is exact:
N means that the preceding byte sequence is repeated N times.

The packet IDs and payload layouts are sourced from the exact EDOPro commit
30935e847165a9ef0e547fb51a43f36168fab7c7. The independent fixed-string and
join-padding comparison is WindBot commit
bffe6b62679c8b2fafea8f59740e03a132517da4. The complete ledger is in
[PROTOCOL_PROVENANCE.md](../../../PROTOCOL_PROVENANCE.md).

## Vectors

### 1. Zero-payload CTOS response

Direction: CTOS
Packet type: CTOS_RESPONSE, 0x01
Frame hex:

~~~text
01 00 01
~~~

Expected decode: Response with an empty opaque payload.

### 2. Zero-payload STOC duel start

Direction: STOC
Packet type: STOC_DUEL_START, 0x15
Frame hex:

~~~text
01 00 15
~~~

Expected decode: DuelStart with an empty payload.

### 3. Opaque CTOS response

Direction: CTOS
Packet type: CTOS_RESPONSE, 0x01
Frame hex:

~~~text
04 00 01 00 10 ff
~~~

Expected decode: Response with opaque payload 00 10 ff, preserved byte-for-byte.

### 4. Opaque STOC game message

Direction: STOC
Packet type: STOC_GAME_MSG, 0x01
Frame hex:

~~~text
05 00 01 aa bb cc dd
~~~

Expected decode: GameMsg with opaque payload aa bb cc dd. The inner duel
message is not decoded.

### 5. CTOS player information

Direction: CTOS
Packet type: CTOS_PLAYER_INFO, 0x10
Frame prefix and type:

~~~text
29 00 10
~~~

Payload hex:

~~~text
49 00 67 00 6e 00 69 00 73 00 00 00 x 15
~~~

Expected decode: Name Ignis in a 20-code-unit little-endian UTF-16 field,
terminated and zero-filled.

### 6. CTOS join game

Direction: CTOS
Packet type: CTOS_JOIN_GAME, 0x12
Frame prefix and type:

~~~text
35 00 12
~~~

Payload hex:

~~~text
54 13 00 00 44 33 22 11
72 00 6f 00 6f 00 6d 00 2d 00 73 00 65 00 63 00 72 00 65 00 74 00
00 00 x 9
29 00 0b 00
~~~

Expected decode: protocol version 0x1354, game ID 0x11223344, password
room-secret, and client/core version 41.0/11.0. The two alignment bytes are
canonical zero padding.

### 7. CTOS update deck

Direction: CTOS
Packet type: CTOS_UPDATE_DECK, 0x02
Frame hex:

~~~text
15 00 02 02 00 00 00 01 00 00 00
44 33 22 11 88 77 66 55 dd cc bb aa
~~~

Expected decode: two main-plus-extra card codes 0x11223344 and 0x55667788,
followed by one side card code 0xaabbccdd.

### 8. CTOS hand result

Direction: CTOS
Packet type: CTOS_HAND_RESULT, 0x03
Frame hex:

~~~text
02 00 03 7f
~~~

Expected decode: result 0x7f.

### 9. CTOS starting-player result

Direction: CTOS
Packet type: CTOS_TP_RESULT, 0x04
Frame hex:

~~~text
02 00 04 01
~~~

Expected decode: result 0x01.

### 10. STOC error message

Direction: STOC
Packet type: STOC_ERROR_MSG, 0x02
Frame hex:

~~~text
0b 00 02 02 00 00 00 44 33 22 11 55 66
~~~

Expected decode: error type DeckError, raw code 0x11223344, and opaque
additional payload 55 66. The three alignment bytes are canonical zero padding.

### 11. STOC join game / HostInfo

Direction: STOC
Packet type: STOC_JOIN_GAME, 0x12
Frame prefix and type:

~~~text
45 00 12
~~~

Payload hex:

~~~text
04 03 02 01 05 06 07 08 09 00 00 00
10 0f 0e 0d 11 12 14 13 18 17 16 15
1c 1b 1a 19 1d 1e 1f 20
24 23 22 21 28 27 26 25 2c 2b 2a 29
30 2f 2e 2d 34 33 32 31 36 35
38 37 3a 39 3c 3b 3e 3d 40 3f 42 41
00 00
~~~

Expected decode: the exact 68-byte mechanically aligned HostInfo payload,
including canonical zero alignment bytes and the three main/extra/side
deck-size pairs.

### 12. STOC hand result

Direction: STOC
Packet type: STOC_HAND_RESULT, 0x05
Frame hex:

~~~text
03 00 05 01 02
~~~

Expected decode: result values 0x01 and 0x02.

### 13. STOC type change

Direction: STOC
Packet type: STOC_TYPE_CHANGE, 0x13
Frame hex:

~~~text
02 00 13 a5
~~~

Expected decode: raw type 0xa5.

### 14. STOC time limit

Direction: STOC
Packet type: STOC_TIME_LIMIT, 0x18
Frame hex:

~~~text
05 00 18 02 00 34 12
~~~

Expected decode: player 0x02 and remaining time 0x1234. The alignment byte is
canonical zero padding.

### 15. STOC lobby player enter

Direction: STOC
Packet type: STOC_HS_PLAYER_ENTER, 0x20
Frame prefix and type:

~~~text
2b 00 20
~~~

Payload hex:

~~~text
50 00 31 00 00 00 x 18 03 00
~~~

Expected decode: name P1 and position 0x03. The trailing alignment byte is
canonical zero padding.

### 16. STOC lobby player change

Direction: STOC
Packet type: STOC_HS_PLAYER_CHANGE, 0x21
Frame hex:

~~~text
02 00 21 a1
~~~

Expected decode: raw status 0xa1.

### 17. STOC lobby watcher count

Direction: STOC
Packet type: STOC_HS_WATCH_CHANGE, 0x22
Frame hex:

~~~text
03 00 22 34 12
~~~

Expected decode: watcher count 0x1234.

### 18. Maximum representable payload boundary

Direction: CTOS
Packet type: CTOS_RESPONSE, 0x01
Frame prefix and type:

~~~text
ff ff 01
~~~

Payload hex: 11, followed by 65532 zero bytes, followed by ee.

Expected decode: a complete 65534-byte opaque payload; no allocation occurs
until the complete frame is present.
