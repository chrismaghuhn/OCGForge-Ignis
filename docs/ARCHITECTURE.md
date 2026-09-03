# OCGForge-Ignis Architecture

Status: V1 architecture frozen for I0
Date: 2026-09-03

## Frozen topology

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

The Headless Adapter Runtime is the independent external TCP CTOS/STOC client
of the local/private EDOPro instance. The separate Model Runner is a child
process connected through framed anonymous stdin/stdout pipes. It is bound to
the exact pinned canonical checkpoint.

The process and protocol components shown above are future architecture only.
I0 creates no production project and implements no IPC.

## Integration decision

The V1 integration mode is:

~~~text
INTEGRATION_MODE=independent standalone external EDOPro network client
EDOPRO_FORK_REQUIRED=NO
EDOPRO_PLUGIN_REQUIRED=NO
DLL_INJECTION=FORBIDDEN
OCGCORE_EMBEDDING=FORBIDDEN
WINDBOT_CODE_REUSE=FORBIDDEN
~~~

Ignis is started as its own executable. It connects to the pinned EDOPro
instance as a normal external client. EDOPro does not need to be patched,
forked, or loaded into the Ignis process. WindBot and EDOPro may inform
protocol research and independent oracle tests, but neither is a runtime
dependency and no source is vendored or copied.

## Launcher and application-controller boundary

The initial Windows shell is WPF on .NET 10. Headless mode is required. This is
a presentation and configuration choice, not a gameplay authority:

~~~text
DEFAULT_USER_EXPERIENCE=double-click EXE → choose mode → validate → Start
GUI_TECHNOLOGY=WPF / .NET 10
HEADLESS_MODE_REQUIRED=YES
GUI_AND_HEADLESS_SHARE=LaunchConfigurationV1, Application Controller, Adapter Runtime
GUI_GAMEPLAY_AUTHORITY=NONE
GUI_LEGALITY_AUTHORITY=NONE
GUI_MODEL_POLICY_AUTHORITY=NONE
~~~

The runtime must also start without a window:

~~~text
OCGForge-Ignis.exe --headless --config match.json
  → parse and validate LaunchConfigurationV1
  → Application Controller
  → Headless Adapter Runtime
~~~

The WPF path and the headless path consume the same versioned
LaunchConfigurationV1 and the same validation/controller boundary. The
configuration may contain explicit values for mode, bot deck or deck
manifest, seat, starting-player policy, checkpoint reference, and EDOPro
endpoint. A password, if needed, is a secret input and is not a semantic
identity field or public trace field.

The design reserves Human vs Bot, Bot vs Bot, Protocol Replay, and Diagnostics
as explicit mode values. The modes are not implemented or accepted as
reachable gameplay paths by I0. The UI may display structured runtime outcomes
such as checkpoint incompatibility, EDOPro version mismatch, deck manifest
mismatch, model-runner startup failure, unsupported prompt, and duel end.
Those labels are presentation mappings; the underlying error codes remain
structured and fail closed.

No button handler may send CTOS/STOC packets, invoke the model runner, choose a
candidate, reorder a domain, or repair an error. The Application Controller is
the only future orchestration boundary between the shell and the adapter
runtime. The GUI has no gameplay authority, no legality authority, and no
model-policy authority.

## Authority boundary

| Concern | Sole authority | Ignis responsibility |
| --- | --- | --- |
| Game rules and engine semantics | EDOPro and its pinned ocgcore | Preserve and report the authoritative result |
| Current legal prompt | EDOPro | Decode a proven supported family completely |
| Candidate ordering | EDOPro prompt semantics | Preserve source order exactly |
| Candidate-to-response mapping | Original EDOPro response contract | Keep a private binding; emit one original response |
| Model input semantics | Accepted OCGForge contract bundle | Consume only after I6 cross-oracle acceptance |
| Model score count | Model-runner contract | Return exactly one score per model candidate row |
| Privacy projection | Ignis contract, bounded by OCGForge input contract | Exclude hidden opponent identity and control-plane data |

Ignis is not allowed to invent legality, infer missing legal candidates, repair
an invalid response, or use a fallback policy.

## V1 target

| Field | Frozen value |
| --- | --- |
| EDOPro release | 41.0.2, Bagooska |
| EDOPro commit | 30935e847165a9ef0e547fb51a43f36168fab7c7 |
| Protocol version | PRO_VERSION=0x1354 |
| ocgcore API | 11 |
| Room type | pre-created local or private room |
| Duel mode | single duel only |
| Match/Siding | unsupported |
| Tag/Relay | unsupported |
| Rematch | unsupported |
| Player reconnect | unsupported |
| Public production server | unauthorized |
| ANNOUNCE_CARD | unsupported; fail closed |

Legacy compatibility mode is rejected. A version or capability mismatch is a
structured failure, not an invitation to guess a compatible layout.

## Process boundary

The future OCGForge-Ignis.exe owns the external protocol session, the
perspective-safe state mirror, complete prompt domains, private response
bindings, and public/private trace separation.

The future model-runner child process owns only the model-facing contract
projection and checkpoint-bound inference. It receives no raw protocol state,
socket metadata, process metadata, or private response bytes. In I0 the
process boundary is documented but not implemented.

The exact checkpoint relationship remains unproven. I6 must establish a
byte-exact cross-oracle for state, locators, events, candidate keys, domains,
logical inputs, encoded inputs, numeric rows, and model-input identities before
I7 can claim checkpoint binding.

## Deterministic data flow

For a supported prompt, the future data flow is:

~~~text
EDOPro prompt
  → complete candidates in authoritative source order
  → perspective-safe public contract projection
  → accepted model-contract input
  → exactly N model scores for N rows
  → selected semantic candidate key
  → private response binding
  → exactly one original CTOS response
~~~

Semantic gameplay identity is distinct from execution and build provenance.
Gameplay identity cannot depend on pointers, PIDs, wall time, scheduling, TCP
segmentation, receive-buffer boundaries, or absolute filesystem paths.
Transport sequence numbers may order a trace but are not gameplay identity.

## Roadmap and authorization

| Milestone | Scope | I0 status |
| --- | --- | --- |
| I0 | charter, contracts, threat model, and pins | current task |
| I1 | deterministic wire codec | NOT AUTHORIZED |
| I2 | connection, lobby, and pre-duel state machine | not authorized |
| I3 | perspective state mirror and privacy projection | not authorized |
| I4 | flat prompt families | not authorized |
| I5 | combinatorial prompt families and continuations | not authorized |
| I6 | OCGForge contract bundle and cross-oracle | blocked on accepted prerequisites |
| I7 | runner IPC and checkpoint binding | blocked on I6 |
| I8 | one end-to-end model prompt | blocked on I7 |
| I9 | reachable local-duel prompt coverage | blocked on I8 |
| I10 | audit and first-divergence tooling | not authorized |
| I11 | release and human readiness | not authorized |

Completion of I0 never authorizes I1. I6 and later cannot claim exact OCGForge
checkpoint compatibility without the accepted playable checkpoint or frozen
final model-contract bundle, byte-exact cross-oracle evidence, and explicit
runtime/rules compatibility evidence.
