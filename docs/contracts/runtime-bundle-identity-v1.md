# Runtime-Bundle Identity Contract V1

Contract ID: ocgforge-ignis.runtime-bundle-identity.v1
Status: conceptual identity frozen for I0
Date: 2026-09-03

## Identity definition

The runtime bundle identity is a content identity for the exact external
runtime and evaluation configuration:

~~~text
ignis_runtime_bundle_id = H(
  identity_schema,
  EDOPro source commit,
  EDOPro ocgcore gitlink,
  protocol target/version,
  CardScripts commit,
  all CDB identities,
  banlist identity,
  duel flags,
  room settings,
  ordered deck manifests
)
~~~

The final canonical encoding, hash algorithm, field ordering, and digest
domain must be versioned before an implementation claims a concrete digest.
I0 defines the input set but does not publish a runtime-bundle digest.

## EDOPro and ocgcore coherence

For V1 the runtime core is the exact submodule gitlink recorded by the pinned
EDOPro source commit:

~~~text
EDOPRO_SOURCE_COMMIT=30935e847165a9ef0e547fb51a43f36168fab7c7
EDOPRO_OCGCORE_GITLINK=46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57
YGOPRO_CORE_RESEARCH_REFERENCE=e747e1771fcf91dd7c53a5950f030012229e66e4
~~~

The research reference is a separately resolved ygopro-core commit. It is not
the EDOPro V1 runtime core and is not a substitute for the gitlink. It may
remain in provenance records, but it must not be treated as the runtime core
identity. Any deliberate core override requires a separate versioned identity
and acceptance decision.

## Required inputs

The identity must bind, at minimum:

- the exact EDOPro source commit and target protocol version;
- the exact ocgcore gitlink used by that EDOPro source commit and its API
  expectation;
- the exact CardScripts commit;
- every CDB identity or content digest used by the runtime;
- banlist identity;
- duel flags;
- room settings that affect gameplay;
- ordered main, extra, and side deck manifests;
- the identity schema and canonical encoding version.

A mutable latest, best, floating branch, or equivalent checkpoint reference
cannot be an authoritative input.

## Identity-domain separation

The runtime-bundle identity is not a transport identity and not a process
identity. Host, port, password, socket identity, packet offset, PID, wall
clock, process handle, scheduling, and absolute filesystem paths are excluded
from semantic gameplay identity.

Build and execution provenance may be recorded in a separate provenance
envelope. It must not be substituted for or mixed into semantic gameplay
identity without an explicit versioned contract.

## No historical OCGForge equality claim

ignis_runtime_bundle_id must not be presented as equal to the historical
OCGForge rules-bundle identity. Rules equivalence requires later, explicit,
field-level evidence. A shared card name, commit family, or similar label is
not proof of rules equality.

## Compatibility vocabulary

These states are independent and must never be collapsed into one generic
compatible boolean:

| State | Initial I0 value | Meaning |
| --- | --- | --- |
| CHECKPOINT_BINARY_COMPATIBILITY | UNPROVEN | The pinned checkpoint binary binding has not been accepted |
| INPUT_CONTRACT_COMPATIBILITY | UNPROVEN | Byte-exact model input equivalence has not been accepted |
| RULES_DOMAIN_COMPATIBILITY | DIFFERENT_OR_UNPROVEN | Runtime rules are not proven equal to the historical OCGForge bundle |
| EVALUATION_COMPARABILITY | UNPROVEN | Comparable evaluation evidence does not yet exist |

## Evidence gate

I6 and later may advance only after the relevant OCGForge side has an accepted
playable BC checkpoint or frozen final model-contract bundle, and the adapter
has accepted:

- byte-exact state, event, locator, and candidate cross-oracle vectors;
- canonical input and domain identities;
- explicit runtime/rules compatibility evidence;
- fresh-process checkpoint binding evidence;
- privacy and no-fallback evidence.
