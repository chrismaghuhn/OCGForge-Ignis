# Project Invariants V1

Contract ID: ocgforge-ignis.project-invariants.v1
Status: frozen for I0
Date: 2026-09-03

## Fail-closed baseline

~~~text
FAIL_CLOSED_ON_UNPROVEN_PROMPT=true
~~~

The adapter must prefer a structured unsupported or unproven failure over a
plausible response whose legality or completeness has not been established.

## Forbidden behavior

The implementation must never:

- truncate legal candidates;
- fabricate candidates;
- deduplicate candidates;
- reorder candidates;
- repair invalid responses;
- automatically select when N=1;
- invent Pass or Cancel;
- use Action 0 as a fallback;
- invoke Teacher or random-legal fallback;
- retry with another policy;
- use a fixed global action vocabulary as a legality source;
- substitute a heuristic subset for a complete legal domain.

These restrictions apply to protocol decoding, state projection, candidate
construction, model inference, response selection, and error recovery.

## Candidate completeness

For a flat one-to-one prompt:

~~~text
N protocol options
= N adapter candidates
= N model candidate rows
= N model scores
~~~

For an adapter-local combinatorial continuation:

~~~text
N current semantic continuation actions
= N model candidate rows
= N model scores
~~~

Every current continuation domain must be complete for the current continuation
state. Prior picks may legitimately constrain the next current domain. No legal
terminal completion consistent with the choices already made may be silently
truncated, fabricated, deduplicated, or made unreachable.

Candidates preserve the authoritative source order and retain the semantic
identity needed for deterministic selection. Candidate rows do not contain
private response bytes or hidden object identity.

An adapter-local continuation may expose a sequence of intermediate domains.
Each intermediate domain is evaluated under the same current-domain rule; the
continuation does not invent an action or hide a legal completion.

## Response routing

Every final selected candidate routes to exactly one original legal EDOPro
response. A continuation may emit no intermediate CTOS response. Only the
terminal continuation action emits the final response for the original engine
prompt.

Stale, duplicate, late, unbound, or otherwise invalid candidate identities
fail closed. No lower layer may repair a response or select a replacement.

## Launcher and headless invariants

The WPF launcher and the required headless command line are two presentation
paths over the same LaunchConfigurationV1 validator and Application
Controller. The UI must never:

- send CTOS/STOC packets directly;
- invoke the model runner directly;
- select, reorder, deduplicate, truncate, or hide gameplay candidates;
- repair an invalid configuration, prompt, candidate, or response;
- replace a structured error with a fallback action.

User-facing status text is only a presentation mapping of structured internal
error codes. A visible START action does not authorize an unvalidated or
unsupported runtime path.

## Authority and privacy

EDOPro owns legality and the accepted response. Ignis owns only proven
translation and private response binding.

Authoritative model input is derived only from the perspective-safe public
projection. It excludes:

- opponent hidden card identities;
- reconstructed hidden hands or decklists;
- inferred beliefs, probabilities, or archetypes;
- raw pointers or addresses;
- control-plane metadata;
- private response bytes and packet offsets.

Knowledge-destroying transitions destroy stale hidden locator identity. See
the [privacy threat model](../PRIVACY_THREAT_MODEL.md).

## Identity and determinism

Semantic gameplay identity is separate from execution/build provenance.
Semantic identity must not depend on:

- pointers or addresses;
- PID;
- wall-clock time;
- thread scheduling;
- TCP segmentation or receive-buffer boundaries;
- absolute filesystem paths.

Transport sequence numbers may support ordered diagnostics but are not semantic
gameplay identity.

## Unsupported target

ANNOUNCE_CARD is unsupported in V1 and must fail closed. Public production
server automation is unauthorized. Match/Siding, Tag/Relay, Rematch, player
reconnect, and other out-of-scope modes must not be silently downgraded to a
supported mode.

## I0 boundary

I0 creates only contracts and governance. It does not authorize:

- I1 deterministic wire-codec implementation;
- networking;
- gameplay;
- prompt handling;
- model-runner IPC;
- checkpoint binding;
- training or optimizer steps.
