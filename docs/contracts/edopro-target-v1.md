# EDOPro Target Contract V1

Contract ID: ocgforge-ignis.edopro-target.v1
Status: frozen for I0
Date: 2026-09-03

## Target identity

| Field | Frozen value |
| --- | --- |
| Repository | https://github.com/edo9300/edopro |
| EDOPro commit | 30935e847165a9ef0e547fb51a43f36168fab7c7 |
| EDOPro ocgcore gitlink | 46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57 |
| ygopro-core research reference | e747e1771fcf91dd7c53a5950f030012229e66e4 |
| Release label | 41.0.2, Bagooska |
| PRO_VERSION | 0x1354 |
| ocgcore API | 11 |
| Rules/client mode | pinned target only |
| Legacy compatibility mode | rejected |
| Initial platform target | win-x64 |

The commit is a lowercase 40-character SHA and was independently resolved in
the intended repository at bootstrap. The current upstream default-branch HEAD
was also checked and is recorded as provenance in
[PROTOCOL_PROVENANCE.md](../../PROTOCOL_PROVENANCE.md); it never replaces this
pin automatically.

The V1 runtime core is the ocgcore gitlink recorded by the pinned EDOPro tree:
46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57. The separately researched
ygopro-core commit e747e1771fcf91dd7c53a5950f030012229e66e4 is a research
reference only. It is not the EDOPro gitlink and must not be silently
substituted as the V1 runtime core. An intentional core override requires a
separate explicit identity and acceptance decision.

## Integration scope

Ignis is an independent standalone external EDOPro network client. It joins a
pre-created local or private room and participates as one normal player.

V1 includes only:

- one duel;
- pre-created rooms;
- the pinned client/protocol target;
- exact version recognition;
- fail-closed handling of unsupported messages.

V1 excludes:

- Match and Siding;
- Tag and Relay;
- Rematch;
- player reconnect;
- observer catch-up as a gameplay path;
- public production servers;
- authentication or anti-bot bypass;
- ANNOUNCE_CARD.

ANNOUNCE_CARD is explicitly UNSUPPORTED and FAIL_CLOSED. A database scan or
inferred card domain may not be used to make it appear supported.

## Authority rule

EDOPro and its pinned ocgcore remain the sole authority for:

- rules;
- current engine state and legal prompt;
- accepted final CTOS response.

Ignis may translate only a prompt family whose complete legal candidate domain,
semantic identity, source ordering, visibility, and response binding are
proven. It is not a second legality engine.

## Future transport boundary

The later wire-codec task may independently implement the pinned CTOS/STOC
transport. The target research records a TCP byte stream with explicit
little-endian framing and bounded packet lengths. I0 does not implement,
decode, or test that transport.

No source implementation is copied from EDOPro or WindBot. Future protocol
facts must be recorded using the clean-room ledger in
[PROTOCOL_PROVENANCE.md](../../PROTOCOL_PROVENANCE.md).

## LaunchConfigurationV1

The future launcher and headless entry point share one versioned start
contract, LaunchConfigurationV1. Its explicit configuration surface includes,
at minimum:

- mode;
- bot deck manifest and expected deck role;
- seat;
- starting-player policy;
- checkpoint reference;
- pinned EDOPro endpoint and target identity.

The WPF shell only edits and displays this configuration. It passes the
validated value to the Application Controller. The headless path accepts the
same contract from a configuration file. Neither path may select a gameplay
candidate or bypass the controller. Passwords are secret inputs and are
excluded from semantic identity and public traces.

The design reserves Human vs Bot, Bot vs Bot, Protocol Replay, and Diagnostics
as explicit mode values. I0 does not claim that any mode is implemented.

## Authorization boundary

This contract does not authorize I1. I1 remains NOT AUTHORIZED after I0.
Network connection, lobby, prompt decoding, response submission, and gameplay
remain separate future tasks.
