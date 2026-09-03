# OCGForge-Ignis Privacy Threat Model

Status: accepted for I0 boundary freeze
Date: 2026-09-03

## Scope and security objective

The adapter may observe more information at its protocol boundary than a model
is allowed to consume. The privacy objective is to make the model-facing
projection a strict information boundary:

> Opponent hidden card identities never enter authoritative model input.

The model must not receive hidden identities through direct fields, derived
features, stable hidden locators, diagnostics, control-plane metadata, or
debug-only payloads.

This document is a contract boundary, not a claim that the future
implementation is already complete.

## Trust and data layers

The layers are deliberately separate:

| Layer | Classification | Allowed role |
| --- | --- | --- |
| RawProtocolState | private, omniscient-at-boundary transport state | Decode and retain only inside the protocol boundary |
| PerspectiveStateMirror | perspective-safe state | Track what the player is contractually allowed to know |
| PublicContractProjection | authoritative model-facing semantic input | Supply only accepted, visible, deterministic fields |
| PrivateResponseBinding | private control data | Map a selected semantic candidate to one original response |
| ModelRunnerBoundary | process and data boundary | Accept only the public model contract; expose no protocol internals |
| PublicAuditTrace | shareable semantic evidence | Record redacted semantic identities and outcomes only |
| PrivateProtocolTrace | restricted diagnostics | Retain raw frames and response bytes only when explicitly enabled |

No layer may silently collapse into another. In particular, a response binding
or private trace is never a model feature.

## Threats and mitigations

### T1 — Hidden opponent card identity leakage

Threat: raw engine or protocol data exposes an opponent hand, face-down deck,
face-down Extra Deck, or hidden field identity to the model.

Mitigation: model input is produced only from PerspectiveStateMirror through
PublicContractProjection. Hidden opponent identities are excluded, not
redacted after feature construction. Known evaluation opponent decklists remain
unknown to the bot unless a future explicit contract changes that rule.

### T2 — Stale identity after a knowledge-destroying transition

Threat: a physical hidden-card identity survives a shuffle or other
knowledge-destroying transition and is later treated as the identity of a slot
or locator.

Mitigation: shuffle/randomization boundaries destroy stale hidden locator
identity. A later slot locator is not the same physical object. Hidden engine
identity is never reused as an observation locator.

### T3 — Control-plane metadata becomes semantic identity

Threat: host, port, password, socket identity, packet offset, PID, wall clock,
process handle, receive-buffer boundaries, or scheduling influence model input
or gameplay identity.

Mitigation: control-plane fields are excluded from PublicContractProjection
and semantic digests. They may exist only in restricted operational
diagnostics. Transport sequence numbers may order transport events but are not
gameplay identity.

### T4 — Private protocol trace becomes training data

Threat: raw frames, private response bytes, or diagnostics are ingested as
training input or authoritative model input.

Mitigation: raw protocol diagnostics are marked:

~~~text
TRAINING_ELIGIBILITY=NO
AUTHORITATIVE_MODEL_INPUT=NO
~~~

PublicAuditTrace and PrivateProtocolTrace have separate schemas and retention
policies. Human or raw-protocol logs are not automatically training data.

### T5 — Private response binding leaks through candidates

Threat: a model candidate contains response bytes, packet offsets, object
addresses, or another private locator that reveals hidden state or execution
details.

Mitigation: model candidates carry only safe semantic candidate data and a
stable semantic key. PrivateResponseBinding remains outside the model
contract. The final selected key must resolve to exactly one original legal
response.

### T6 — Debug or runner boundary bypass

Threat: a child process, teacher, search adapter, or debug endpoint requests
raw omniscient state or silently adds inferred beliefs, probabilities,
archetypes, or reconstructed hidden hands.

Mitigation: all model-facing consumers use the public projection. The
authoritative observation schema contains no beliefs or inferred hidden state.
Unsupported access fails closed.

### T7 — Network identity and public audit correlation

Threat: private network locators or object references become public semantic
locators, allowing cross-duel or cross-process correlation.

Mitigation: public semantic locators are canonical and scoped to the accepted
observation contract. Host, port, password, socket identity, PID, and raw
object references never participate in public semantic identity.

## Paired-world privacy property

For a later privacy gate, two worlds may differ only in hidden opponent
identity. If their visible protocol observations are equal, their
PublicContractProjection bytes, semantic identities, candidate domains, and
model-facing inputs must be equal. A difference in hidden identity alone must
not change authoritative model input.

This property includes knowledge-destroying transitions and prevents
post-shuffle locator reuse.

## Fail-closed requirements

The adapter must fail closed when the visibility contract, source ordering,
candidate completeness, response binding, or semantic identity cannot be
proven. It must not repair missing visibility with guesses, inferred beliefs,
or raw protocol access.

See the [project invariants](contracts/project-invariants-v1.md) and
[architecture](ARCHITECTURE.md) for the corresponding decision and process
boundaries.
