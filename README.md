# OCGForge-Ignis

OCGForge-Ignis is an external deterministic evaluation and debugging adapter.
It allows an OCGForge policy or model to participate as a normal player in a
pinned, local or private EDOPro-compatible duel.

This repository is separate from
[OCGForge](https://github.com/chrismaghuhn/OCGForge). OCGForge produces
immutable model contracts and artifacts; OCGForge-Ignis consumes them. The
dependency direction never points back from OCGForge to this repository.

## Current status

The merged `main` baseline contains the I0 repository bootstrap and the final
I1 deterministic protocol codec. This branch contains the separately
authorized I2 Client implementation for independent review.

- repository charter and governance;
- frozen V1 architecture;
- privacy threat model;
- EDOPro target contract and project invariants;
- runtime-bundle identity contract;
- accepted integration and process ADRs;
- third-party and clean-room protocol provenance records;
- repository hygiene configuration;
- the I1 platform-neutral protocol library and deterministic tests;
- the I2 headless 1v1 local/private connection and pre-duel Client under review.

The original I0 commit contained no production implementation. The current
I2 scope contains only the Protocol and Client projects; it does not contain
model-runner code, WPF/UI code, gameplay state, copied source, or external
runtime assets.

The user-authorized I0 evidence records the accepted OCGForge recovery head as
f13d19cab2b3677149f4caa25ef3c755623ded41 with:

~~~text
ORIGINAL_TASK4B_PASS=false
TASK4B_RECOVERY_PASS=true
TASK4B_FINAL_PASS=true
~~~

Task 4B remains only the bounded CUDA/inference smoke. It is not the first
accepted playable behavioral-cloning checkpoint. I0 through I5 may be developed
independently of that checkpoint; I6 and later remain gated by the accepted
playable checkpoint or frozen final model-contract bundle, a byte-exact
OCGForge-to-EDOPro cross-oracle, and explicit runtime/rules compatibility
evidence.

## Frozen V1 topology

~~~text
WPF UI
  ↓
validated LaunchConfigurationV1
  ↓
Application Controller
  ↓
Headless Adapter Runtime
  ├─ Protocol
  ├─ Client
  ├─ State / Privacy
  ├─ Decisions
  └─ Trace
  ↓
separate Model Runner
~~~

The Headless Adapter Runtime is the external client of the local/private
EDOPro instance over TCP CTOS/STOC. The separate Model Runner is a child
process connected through framed anonymous stdin/stdout pipes and bound to the
exact pinned canonical checkpoint.

EDOPro remains the sole authority for game rules, the current legal prompt, and
the accepted CTOS response. Ignis only translates proven protocol decisions. It
is not a rules engine, a second legality engine, a training authority, an
EDOPro fork, or a WindBot fork.

The frozen integration mode is an independent standalone external EDOPro
network client. An EDOPro fork or plugin is not required. DLL injection,
ocgcore embedding, WindBot source reuse, and client patching are forbidden.

## Launcher and headless contract

The default user experience is:

~~~text
DEFAULT_USER_EXPERIENCE=double-click EXE → choose mode → validate → Start
GUI_TECHNOLOGY=WPF / .NET 10
HEADLESS_MODE_REQUIRED=YES
GUI_AND_HEADLESS_SHARE=LaunchConfigurationV1, Application Controller, Adapter Runtime
GUI_GAMEPLAY_AUTHORITY=NONE
GUI_LEGALITY_AUTHORITY=NONE
GUI_MODEL_POLICY_AUTHORITY=NONE
~~~

The launcher may collect explicit configuration such as mode, deck manifest,
seat, starting-player policy, checkpoint reference, and EDOPro endpoint. It
must validate that versioned configuration before the Application Controller
starts. A button handler must never send socket packets or invoke the model
directly. The UI makes no gameplay decision and cannot repair an invalid
configuration or response.

The same runtime must be startable without a window, for example:

~~~text
OCGForge-Ignis.exe --headless --config match.json
~~~

The design reserves explicit mode values for Human vs Bot, Bot vs Bot,
Protocol Replay, and Diagnostics. These are architecture-level mode
candidates, not I0 implementation claims. The headless invocation and the UI
must use the same LaunchConfigurationV1 validation and controller path.

User-facing status is presentation of structured internal outcomes, for
example checkpoint incompatibility, EDOPro version mismatch, deck manifest
mismatch, model-runner startup failure, unsupported prompt, or duel end.
Control-plane secrets such as passwords remain outside semantic identity and
public traces.

## V1 boundary

V1 targets EDOPro 41.0.2, code name Bagooska, PRO_VERSION 0x1354, and ocgcore
API 11. It is limited to pre-created local or private rooms, one duel, and no
Match/Siding, Tag/Relay, Rematch, reconnect, or public production servers.
ANNOUNCE_CARD is unsupported and must fail closed.

Public production-server automation is not authorized. A separate accepted
OI-N01 decision is required before that boundary can change.

## Contracts and decisions

- [Project charter](docs/PROJECT_CHARTER.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Privacy threat model](docs/PRIVACY_THREAT_MODEL.md)
- [EDOPro target V1](docs/contracts/edopro-target-v1.md)
- [Project invariants V1](docs/contracts/project-invariants-v1.md)
- [Runtime-bundle identity V1](docs/contracts/runtime-bundle-identity-v1.md)
- [ADR-0001: integration mode](docs/adr/ADR-0001-integration-mode.md)
- [ADR-0002: language and processes](docs/adr/ADR-0002-language-and-processes.md)
- [Third-party policy](THIRD_PARTY.md)
- [Protocol provenance](PROTOCOL_PROVENANCE.md)

The candidate completeness and fail-closed rules are normative. A supported
prompt must expose every legal candidate in source order, preserve the
candidate-to-response binding, and produce exactly one final original engine
response. Unsupported or unproven prompts terminate with a structured
diagnostic; they are never guessed or repaired.

## Roadmap boundary

The authorized sequence is:

~~~text
I0   repository charter, threat model, contracts, and pins
I1   deterministic wire codec
I2   connection, lobby, and pre-duel state machine
I3   perspective state mirror and privacy projection
I4   flat prompt families
I5   combinatorial prompt families and continuations
I6   OCGForge model-contract bundle and cross-oracle
I7   model-runner IPC and checkpoint binding
I8   one prompt family end to end
I9   reachable prompt coverage and local duel
I10  audit trace and first-divergence tooling
I11  Windows release and controlled human readiness
~~~

Completion of I0 does not authorize I1. Each later task requires its own
authorization, focused gates, independent review, and stop point.

## I0 bootstrap files

The initial commit is intentionally limited to the files named by the I0
authorization. Production projects, protocol fixtures, decks, databases,
checkpoints, binaries, and copied upstream source are out of scope.
