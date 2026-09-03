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
  EDOPro commit,
  protocol target/version,
  ocgcore commit,
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

## Required inputs

The identity must bind, at minimum:

- the exact EDOPro commit and target protocol version;
- the exact ocgcore commit and API expectation;
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
