# ADR-0001: Independent EDOPro Network Client

- Status: Accepted and frozen for I0
- Date: 2026-09-03
- Decision owners: OCGForge-Ignis governance

## Context

OCGForge-Ignis must participate in a pinned EDOPro-compatible duel while
preserving a strict authority boundary. The adapter must not become a second
rules engine, inherit heuristic response behavior, or require modifications to
the EDOPro client.

WindBot and EDOPro are useful protocol references and independent test
oracles. Their source and runtime behavior are not a neutral contract for an
OCGForge model adapter.

## Decision

Freeze V1 as:

~~~text
INTEGRATION_MODE=independent standalone external EDOPro network client
EDOPRO_FORK_REQUIRED=NO
EDOPRO_PLUGIN_REQUIRED=NO
DLL_INJECTION=FORBIDDEN
OCGCORE_EMBEDDING=FORBIDDEN
WINDBOT_CODE_REUSE=FORBIDDEN
~~~

OCGForge-Ignis.exe is a separate process and connects to a local or private
EDOPro instance as a normal player over the EDOPro CTOS/STOC protocol.

The later model-runner is a separate child process connected through framed
anonymous stdin/stdout pipes. That IPC boundary is architectural only in I0;
I0 does not implement it.

## Alternatives rejected

### Fork or derive from WindBot

Rejected because it imports heuristic behavior, fallback decisions, and
unnecessary gameplay assumptions into a boundary that requires complete legal
domains and strict fail-closed behavior. It also increases source-reuse and
license risk.

### EDOPro fork, plugin, hook, or DLL injection

Rejected because it creates invasive version coupling, weakens process
isolation, and is unnecessary for an independent network client.

### ocgcore embedding

Rejected because EDOPro remains the sole gameplay and legality authority. A
second embedded core would create a competing rules path.

## Consequences

The architecture gains a clear process boundary and can be tested against a
pinned local EDOPro instance. The adapter must implement its own clean-room
protocol facts and complete response routing. It must also maintain explicit
compatibility evidence rather than assuming that a network connection proves
checkpoint or rules equivalence.

Public production-server use remains unauthorized and requires a separate
accepted decision.
## Revisit criteria

Changing this decision requires a new explicitly authorized ADR and evidence
covering authority, privacy, determinism, licensing, and compatibility. I0
completion does not authorize I1 or any networking implementation.
