# ADR-0002: C# and Separate Model-Runner Process

- Status: Accepted and frozen for I0
- Date: 2026-09-03
- Decision owners: OCGForge-Ignis governance

## Decision

Freeze the initial implementation target as:

~~~text
LANGUAGE=C#
TARGET_FRAMEWORK=net10.0
INITIAL_RELEASE_TARGET=win-x64
DESIGN_TARGET=cross-platform where practical
WINDOWS_LAUNCHER=WPF / .NET 10
HEADLESS_MODE_REQUIRED=YES
HEADLESS_ENTRYPOINT=OCGForge-Ignis.exe --headless --config <path>
GUI_GAMEPLAY_AUTHORITY=NONE
GUI_LEGALITY_AUTHORITY=NONE
GUI_MODEL_POLICY_AUTHORITY=NONE
~~~

Freeze the process topology as:

~~~text
OCGForge-Ignis.exe
  starts
model-runner child process
~~~

V1 runner IPC is framed anonymous stdin/stdout pipes. The runner is not a
network peer and does not receive raw protocol state, private response bytes,
or control-plane metadata.

The executable contains a WPF launcher/application shell and a required
headless adapter-runtime path. Both paths create or consume the same validated,
versioned LaunchConfigurationV1 and pass it to the Application Controller. The
UI is limited to explicit configuration and status presentation. It does not
send protocol packets, call the model runner, select gameplay candidates, or
repair failures.

The default user experience is double-click EXE, choose mode, validate, and
Start. The design reserves explicit mode values for Human vs Bot, Bot vs Bot,
Protocol Replay, and Diagnostics. Those values are not an I0 implementation
claim and do not authorize networking or gameplay.

## Rationale

C# and .NET provide the intended Windows release path while retaining a
practical cross-platform design target. A separate runner makes checkpoint
binding, process failure, timeouts, and model-contract validation explicit.
It also prevents model execution from becoming an alternative gameplay or
legality authority.

## I0 limitation

This ADR documents a future process boundary. I0 must not create:

- C# project files;
- WPF project files or UI code;
- production source;
- model-runner code;
- IPC codec or framing;
- NuGet dependencies;
- networking classes.

The first implementation task remains I1, and I1 is not authorized by I0
completion.

## Revisit criteria

Any change to language, target framework, release target, process ownership,
or IPC boundary requires a separately authorized decision with compatibility,
privacy, failure-mode, and deterministic-replay evidence.
