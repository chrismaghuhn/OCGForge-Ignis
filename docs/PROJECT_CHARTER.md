# OCGForge-Ignis Project Charter

Status: accepted for I0 bootstrap
Date: 2026-09-03
Owner: OCGForge-Ignis governance

## Purpose

OCGForge-Ignis is an external deterministic evaluation and debugging adapter.
Its purpose is to let an OCGForge policy or model participate as a normal
player in a pinned EDOPro-compatible local or private duel.

The adapter translates a proven current protocol decision. It does not decide
game legality and it does not create a second interpretation of the duel.

## Explicit non-goals

OCGForge-Ignis is not:

- a rules engine;
- a second legality engine;
- a training authority;
- a replacement for OCGForge;
- a WindBot fork;
- an EDOPro fork;
- a public-server automation service;
- a general arbitrary-deck certification system.

No networking, gameplay, model-runner, or protocol implementation is part of
I0.

## Authority and dependency direction

EDOPro is the gameplay and legal authority for the pinned V1 target. It owns:

- game rules and engine semantics;
- the current legal decision prompt;
- the accepted CTOS response for that prompt.

Ignis may only expose a complete, perspective-safe representation of a proven
prompt and route one selected candidate to its original response binding.

The repository boundary is permanently:

~~~text
OCGForge
  produces immutable model contracts and artifacts
        ↓
OCGForge-Ignis
  consumes those contracts and artifacts
~~~

OCGForge must not depend on OCGForge-Ignis. Ignis must not silently replace
OCGForge contracts with an adapter-local approximation.

## Governance rules

Every implementation milestone is one separately authorized task:

~~~text
one task → focused gates → independent review → stop
~~~

The agent implementing a task may not self-authorize the next milestone. A
reported pass is not acceptance until the relevant evidence is independently
reviewed.

The I0 stop boundary is strict:

- no WPF project or UI implementation;
- no src/ production project;
- no C# project or NuGet dependency;
- no network or gameplay class;
- no model-runner or IPC implementation;
- no copied EDOPro, WindBot, ocgcore, CardScripts, BabelCDB, or Distribution
  source or asset;
- no change to the primary OCGForge repository.

## V1 scope

The target and exclusions are defined in the
[EDOPro target contract](contracts/edopro-target-v1.md) and
[architecture](ARCHITECTURE.md). V1 is a single-duel adapter for a
pre-created local or private room. Match/Siding, Tag/Relay, Rematch,
reconnect, and public production servers are outside the accepted boundary.
ANNOUNCE_CARD is unsupported and fail closed.

The executable provides a WPF launcher shell on .NET 10 around a required
headless runtime. The shell is only a configuration and status surface. It
creates a validated LaunchConfigurationV1 for the Application Controller. The
same controller and validation path must remain available through a headless
invocation such as OCGForge-Ignis.exe --headless --config match.json. The UI
never selects a gameplay candidate, sends a protocol packet, invokes a model,
or repairs a failure.

The frozen GUI boundary is:

~~~text
WPF UI
  ↓ validated LaunchConfigurationV1
  ↓ Application Controller
  ↓ Headless Adapter Runtime
      Protocol, Client, State / Privacy, Decisions, Trace
  ↓ separate Model Runner
~~~

The GUI has no gameplay authority, no legality authority, and no model-policy
authority. These are explicit:

~~~text
GUI_GAMEPLAY_AUTHORITY=NONE
GUI_LEGALITY_AUTHORITY=NONE
GUI_MODEL_POLICY_AUTHORITY=NONE
HEADLESS_MODE_REQUIRED=YES
~~~

## Compatibility status

The following states remain separate:

- CHECKPOINT_BINARY_COMPATIBILITY=UNPROVEN
- INPUT_CONTRACT_COMPATIBILITY=UNPROVEN
- RULES_DOMAIN_COMPATIBILITY=DIFFERENT_OR_UNPROVEN
- EVALUATION_COMPARABILITY=UNPROVEN

Task 4B's accepted recovery evidence is only a bounded CUDA/inference smoke.
It is not an accepted playable BC baseline checkpoint. I6 and later require
the evidence listed in the
[runtime-bundle identity contract](contracts/runtime-bundle-identity-v1.md).

## Normative references

- [Architecture](ARCHITECTURE.md)
- [Privacy threat model](PRIVACY_THREAT_MODEL.md)
- [Project invariants](contracts/project-invariants-v1.md)
- [Third-party policy](../THIRD_PARTY.md)
- [Protocol provenance](../PROTOCOL_PROVENANCE.md)
