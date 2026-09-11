# ADR-0003: I6C5 External Printed Artifact Governance

Status: ACCEPTED REPOSITORY-GOVERNANCE DECISION

Subsequent implementation status:

```text
OPTION_B_PROVIDER_CODE_AUTHORIZED=YES
OPTION_B_OPERATOR_LOCAL_PROVISIONING_GOVERNANCE=APPROVED_BOUNDARY_ONLY
OPTION_B_OPERATOR_LOCAL_PROVISIONING_EXECUTION_AUTHORIZED=NO
PRINTED_PROVIDER_IMPLEMENTATION_AUTHORIZED=YES
PRINTED_PROVIDER_IMPLEMENTATION=FINAL_PASS
```

Date: 2026-09-07

Additive governance update: 2026-09-11 (Task 27A)

Decision:

```text
OPTION_B_GOVERNANCE_DECISION=APPROVE_PROVIDER_CODE_PLUS_OPERATOR_LOCAL_BOUNDARY
```

This ADR is a repository-governance decision, not legal advice and not a
statement that any operator has a legal right to acquire, possess, use, or
redistribute BabelCDB or derived data.

## 1. Question and scope

This ADR records the governance decision on whether OCGForge-Ignis may
implement generic code which consumes an explicitly operator-supplied local
Printed-provider artifact conforming to the frozen I6C5 technical contract.

At the time of this decision, provider implementation was intentionally left
to a later separate authorization. That later authorization and implementation
acceptance are recorded above; they do not retroactively change this ADR's
original decision or its redistribution and acquisition boundaries.

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
artifact and validates it against the frozen contract. The decision-time
status was:

```text
PRINTED_PROVIDER_TECHNICAL_CONTRACT_READY=YES
PRINTED_PROVIDER_GOVERNANCE_READY=YES
PRINTED_PROVIDER_IMPLEMENTATION_READY=YES
PRINTED_PROVIDER_IMPLEMENTATION_AUTHORIZED=NO
```

The separate implementation authorization has since been granted and the
provider implementation has independently reached `FINAL_PASS`. This does not
authorize any BabelCDB or derived-data redistribution, acquisition, generation,
or real-data fixture path.

This additive governance decision defines a narrow future operator-local
provisioning boundary without authorizing its execution. It does not change
the redistribution, acquisition, generation, release, or test-fixture
prohibitions above.

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

A separately authorized operator-local provisioning operation MAY use the
following already-present local inputs and sequence:

```text
explicitly selected local OCGForge source inputs
    → exact pinned tools/prepare_card_data.py
    → local pipe12 artifact and complete manifest outside Ignis
    → exact hash/coverage/environment validation
    → PerspectiveSafePrintedProviderV1.TryCreate()
```

This is a procedural boundary, not an execution authorization. The operator
must provide explicit paths and verify the exact source, checkout,
transformation, artifact, manifest, OCGForge semantic, and rules-bundle
identities before the provider can be supplied to a run. The operation must
fail closed on any missing input or mismatch.

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

In particular, this decision does not authorize an Ignis-owned generator,
acquisition process, network access, ambient source search, or execution of a
provisioning operation. Those remain separately gated and are not part of the
27A documentation slice.

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

Executing the operator-local provisioning procedure is also a separate
operational authorization. This ADR records its allowed shape only; it does
not authorize execution in the current task.

This ADR does not supersede the existing third-party policy. It records the
narrow provider-code exception and the bounded no-data operator-local
provisioning boundary authorized by the explicit I6C5 governance review.

## 9. Consequence

The technical governance blocker for implementing a generic external-artifact
consumer is removed. At the time of this ADR, the later implementation task
still required separate authorization; that authorization has since been
granted and independently accepted. The data itself remains outside the
repository and outside the release, and the frozen technical contract and its
third-party prohibitions remain binding.
