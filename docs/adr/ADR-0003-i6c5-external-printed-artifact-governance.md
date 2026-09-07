# ADR-0003: I6C5 External Printed Artifact Governance

Status: ACCEPTED REPOSITORY-GOVERNANCE DECISION / IMPLEMENTATION NOT YET AUTHORIZED

Date: 2026-09-07

Decision:

```text
OPTION_B_GOVERNANCE_DECISION=APPROVE_PROVIDER_CODE_ONLY
```

This ADR is a repository-governance decision, not legal advice and not a
statement that any operator has a legal right to acquire, possess, use, or
redistribute BabelCDB or derived data.

## 1. Question and scope

This ADR decides only whether OCGForge-Ignis may later implement generic code
which consumes an explicitly operator-supplied local Printed-provider artifact
conforming to the frozen I6C5 technical contract.

The decision does not approve:

```text
BabelCDB redistribution
derived-data redistribution
BabelCDB acquisition tooling
derived-artifact generation tooling
artifact inclusion in the repository or release
real third-party data in tests
legal use of any operator-supplied artifact
```

The technical contract remains:

[I6C5 Printed Source Provider V1](../contracts/i6c5-printed-source-provider-v1.md)

## 2. Evidence review

| Question | Evidence | Result |
| --- | --- | --- |
| Standalone BabelCDB license at the OCGForge pin? | Root inventory, README, and exact pinned `LICENSE`/`COPYING` lookups at [89ad6837](https://github.com/ProjectIgnis/BabelCDB/tree/89ad6837b0766a52984d8c715a7d5d4f8447946b) | NO |
| Standalone BabelCDB license at the Ignis provenance pin? | Root inventory, README, and exact pinned `LICENSE`/`COPYING` lookups at [2142b4b4](https://github.com/ProjectIgnis/BabelCDB/tree/2142b4b45e7963fd944940f144951177e87eb15c) | NO |
| Explicit redistribution permission found? | No authoritative permission statement in the inspected pinned repository material | UNPROVEN |
| Explicit derived-data permission found? | No authoritative permission statement in the inspected pinned repository material | UNPROVEN |
| Ignis currently redistributes BabelCDB? | Repository inventory and [THIRD_PARTY.md](../../THIRD_PARTY.md) | NO |
| Option B requires Ignis redistribution? | Frozen I6C5 Option-B boundary | NO |
| Option B requires Ignis acquisition tooling? | Frozen I6C5 Option-B boundary | NO |
| Option B requires Ignis generation tooling? | Frozen I6C5 Option-B boundary | NO |
| Generic consumer can be tested synthetically? | Frozen semantic-row, coverage, hashing, privacy, and fail-closed contract | YES |
| Existing policy can represent code-only approval? | Policy requires a separate explicit decision for future source reuse and separates references from vendored assets | YES |

The BabelCDB README describes synchronized database content and contribution
workflow, but it does not supply the explicit redistribution or derived-data
permission required to change the separate redistribution status. This ADR
therefore makes no license conclusion.

## 3. Decision

The repository authorizes implementation of generic Option-B provider code only:

```text
OPTION_B_PROVIDER_CODE_AUTHORIZED=YES

BABELCDB_REDISTRIBUTION_AUTHORIZED=NO
DERIVED_ARTIFACT_REDISTRIBUTION_AUTHORIZED=NO
BABELCDB_ACQUISITION_TOOLING_AUTHORIZED=NO
DERIVED_ARTIFACT_GENERATION_TOOLING_AUTHORIZED=NO
REAL_CARD_DATA_TEST_FIXTURES_AUTHORIZED=NO
```

This decision is limited to code that accepts an explicit operator-supplied
artifact and validates it against the frozen contract. It does not authorize
the provider implementation in this turn:

```text
PRINTED_PROVIDER_TECHNICAL_CONTRACT_READY=YES
PRINTED_PROVIDER_GOVERNANCE_READY=YES
PRINTED_PROVIDER_IMPLEMENTATION_READY=YES
PRINTED_PROVIDER_IMPLEMENTATION_AUTHORIZED=NO
```

Implementation requires a later, separate authorization.

## 4. Allowed boundary

Future implementation MAY contain:

```text
generic manifest validation
canonical semantic-row parsing
source-artifact hash verification
coverage verification
semantic-row digest verification
environment compatibility verification
immutable provider construction
session/run binding
synthetic contract tests
explicit operator-supplied local artifact input
```

The configured artifact path MUST be explicit. Missing configuration or a
missing artifact MUST return a structured fail-closed error.

Runtime provenance validation records what the supplied manifest claims and
whether the bytes satisfy the technical contract. It is not a certification of
legal ownership, acquisition rights, redistribution rights, or licensing.

## 5. Forbidden boundary

Future implementation MUST NOT:

```text
ship BabelCDB
ship cards.cdb
ship generated real card-data rows
ship a provider artifact
download BabelCDB
clone BabelCDB
generate an artifact from a local CDB
copy or port prepare_card_data.py for acquisition/generation
scan EDOPro directories
search Downloads or other ambient directories
perform network acquisition
auto-update static data
fallback to EDOPro DataManager
fallback to an unbound local CDB
provide real BabelCDB rows as test fixtures
```

No artifact discovery is allowed. There is no implicit search path and no
fallback database.

## 6. Test and release consequences

Repository tests MUST use fabricated, synthetic rows only. They MAY cover:

```text
normal-monster mapping
Xyz mapping
Link mapping
Pendulum mapping
combined XYZ|LINK control flow
coverage mismatch
source hash mismatch
semantic-row hash mismatch
unknown-identity no-lookup
missing-artifact failure
```

No real upstream card row may be copied into a fixture merely because its
numeric fields appear simple.

Releases MAY contain the generic provider code and its contract documentation
after a later implementation authorization. Releases MUST NOT contain:

```text
BabelCDB
cards.cdb
generated Printed artifacts
real derived card rows
```

## 7. Privacy, determinism, and replay

The provider remains usable only in this direction:

```text
already-known perspective-safe passcode
    → Printed properties
```

Unknown or redacted identities MUST NOT trigger provider lookup. Reverse lookup,
candidate-passcode enumeration, and inference from Printed or Current
properties remain forbidden.

The provider semantic identity remains exactly the
`PrintedProviderSemanticIdentityV1` defined by the frozen I6C5 contract. The
external artifact path, timestamps, machine identity, and acquisition details
MUST NOT enter gameplay semantic identity.

Missing artifact, invalid manifest, missing coverage, or any digest mismatch
MUST fail closed with no complete I6C5 frame.

## 8. Future governance changes

Changing any of the following requires another explicit decision:

```text
redistributing BabelCDB
redistributing derived card data
including real data in tests
adding acquisition or generation tooling
bundling an artifact in a release
```

This ADR does not supersede the existing third-party policy. It records the
narrow code-only exception authorized by the explicit I6C5 governance review.

## 9. Consequence

The technical governance blocker for implementing a generic external-artifact
consumer is removed. The data itself remains outside the repository and
outside the release. A later implementation task must still be independently
authorized and must preserve the frozen technical contract.
