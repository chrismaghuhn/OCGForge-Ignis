# Protocol Provenance and Clean-Room Ledger

Status: I0 provenance freeze
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

No version fact in this ledger is attributed to gframe/ocgapi_constants.h.
The corrected version facts above are tied only to the exact source paths where
they were verified.

The I1 wire-codec task must add the exact packet facts it implements and must
preserve the I0 prohibition on source copying. I1 is not authorized by this
bootstrap.

## Provenance boundaries

The pinned CardScripts, BabelCDB, and Distribution commits identify a future
runtime bundle; they do not authorize including those assets. Runtime identity
is specified separately in the
[runtime-bundle identity contract](docs/contracts/runtime-bundle-identity-v1.md).
