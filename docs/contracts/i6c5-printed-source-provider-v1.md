# I6C5 Printed Source Provider Contract V1

Contract status:

```text
FROZEN TECHNICAL CONTRACT
PENDING THIRD-PARTY GOVERNANCE CLEARANCE
IMPLEMENTATION NOT AUTHORIZED
```

Date: 2026-09-07

This contract freezes the technical boundary for a future I6C5 Printed-card
provider. It does not authorize a provider implementation, artifact
generation, BabelCDB redistribution, derived-data redistribution, I6C5 final
acceptance, I6C6, I6D, or I7.

The contract is an Ignis source contract. OCGForge remains the semantic owner
of Printed-card meaning and canonical OCGForge encodings.

## 1. Scope and authority

The accepted authority path is:

```text
OCGForge rules-bundle-derived cards.cdb
    → OCGForge tools/prepare_card_data.py
    → generated static card-data rows
    → CardDataStore
    → CoreHost::static_card_data(passcode)
    → OCGForge card_projection.cpp
    → ObservedCard.printed
```

The current OCGForge semantic authority is:

```text
ocgforge_semantic_commit =
f929de0b4d4157327dba003067d2e21e42f7ad75

rules_bundle_id =
3adfe6b4cfe2c2805e50b389fc0eb4e70a3b0b6107436614d328fddc865e585f

OCGForge database commit =
89ad6837b0766a52984d8c715a7d5d4f8447946b

OCGForge cards.cdb SHA-256 =
7a6570fe313ae0affe4e1e2047c564b669397500777da6c3d76e79aac393726f
```

The relevant OCGForge authority paths are:

```text
third_party/rules_bundle.lock.json
tools/prepare_card_data.py
src/core/card_data_reader.cpp
include/ygo/core/card_data.hpp
include/ygo/core/core_host.hpp
src/observation/card_projection.cpp
src/observation/observation_builder.cpp
```

The semantic field mapping is based on the pinned OCGForge sources, not on the
Ignis EDOPro database. The current Ignis third-party policy remains binding:
[THIRD_PARTY.md](../../THIRD_PARTY.md).

## 2. Normative terminology

The following terms are normative:

```text
MUST       required for acceptance
MUST NOT   forbidden
SHOULD     recommended unless a separately accepted reason exists
MAY        permitted without changing the contract meaning
```

The terms below have distinct meanings and MUST NOT be collapsed:

```text
Printed
    Static card properties from the OCGForge semantic source.

Current
    Runtime/query-derived properties that may be modified during a duel.

Provider semantic identity
    Identity of the semantic passcode-to-Printed mapping and its declared
    coverage.

Environment semantic provenance
    Identity of the OCGForge environment contract with which the provider
    claims compatibility.

Forensic/source provenance
    Evidence describing where and how source bytes were obtained or generated.

Coverage
    The exact passcode set for which the provider claims complete Printed
    rows.
```

## 3. Provider semantic identity

The future provider semantic identity is the fieldwise tuple:

```text
PrintedProviderSemanticIdentityV1 =
(
  provider_contract_id,
  semantic_rows_contract_id,
  field_mapping_contract_id,
  coverage_contract_id,
  coverage_digest_sha256,
  semantic_rows_digest_sha256
)
```

The frozen contract identifiers are:

```text
provider_contract_id =
ocgforge-ignis.i6c5.printed-provider.v1

semantic_rows_contract_id =
ocgforge-ignis.i6c5.printed-semantic-rows.v1

field_mapping_contract_id =
ocgforge-ignis.i6c5.printed-field-mapping.v1

coverage_contract_id =
ocgforge-ignis.i6c5.printed-coverage.v1
```

Two provider semantic identities are equal if and only if all six tuple fields
are equal.

The following MUST NOT be fields of `PrintedProviderSemanticIdentityV1`:

```text
rules_bundle_id
ocgforge semantic commit SHA
BabelCDB repository or commit
cards.cdb SHA
generator repository or file SHA
raw source-artifact SHA
filesystem path
runtime database identity
EDOPro database identity
```

Those values belong to environment compatibility or forensic/source
provenance. They do not independently change the provider's semantic mapping
when the canonical semantic rows, field mapping, and coverage are unchanged.

## 4. Canonical semantic rows

The semantic provider domain is a set of canonical parsed rows. Each row is:

```text
PrintedSemanticRowV1 {
    code        : uint32
    type        : uint32
    level       : uint32
    attribute   : uint32
    race        : uint64
    attack      : int32
    defense     : int32
    left_scale  : uint32
    right_scale : uint32
    link_marker : uint32
}
```

The semantic row deliberately excludes:

```text
alias
setcode
card name
card text
category
localization
scripts
```

Those fields are not part of the OCGForge `ObservedCard.printed` value used by
this I6C5 source contract.

Rows MUST satisfy all of the following:

```text
code != 0
rows are strictly increasing by code
each code occurs exactly once
every declared coverage code has one row
no row exists outside declared coverage
level <= 0xff
if TYPE_LINK is absent: link_marker == 0
if TYPE_LINK is present: defense == 0
if TYPE_PENDULUM is absent: left_scale == 0 and right_scale == 0
if TYPE_PENDULUM is present: left_scale <= 0xff and right_scale <= 0xff
if TYPE_LINK is present: link_marker contains only the eight OCGForge-
    recognized marker bits
```

Malformed input MUST be rejected. A provider MUST NOT silently sort,
deduplicate, drop unexpected rows, or fill missing rows.

These are canonicality rules, not new card-game rules. They mirror the
normalization performed by the pinned OCGForge transformation and ensure that
two rows which project to the same Printed value cannot receive different
semantic-row digests solely through ignored or normalized fields.

## 5. Semantic row hash domain

The semantic row digest is:

```text
semantic_rows_digest_sha256 =
SHA256(canonical semantic-row bytes)
```

The exact canonical byte domain is:

```text
ASCII bytes:
OCGFORGE-IGNIS-I6C5-PRINTED-ROWS-V1\0

row_count : uint32 big-endian

for each row in strictly increasing code order:
    code        : uint32 big-endian
    type        : uint32 big-endian
    level       : uint32 big-endian
    attribute   : uint32 big-endian
    race        : uint64 big-endian
    attack      : int32 big-endian two's-complement
    defense     : int32 big-endian two's-complement
    left_scale  : uint32 big-endian
    right_scale : uint32 big-endian
    link_marker : uint32 big-endian
```

There is no padding, text encoding, delimiter, locale, or JSON property order
in this digest domain. This digest is `SEMANTIC` provider identity input.

## 6. Coverage contract

Coverage is an `ExplicitDeclaredPasscodeSetV1` supplied before the duel/run.
It is immutable for the provider lifetime and MUST be represented as:

```text
strictly increasing uint32 passcodes
nonzero values
unique values
```

The provider manifest MUST reject an unsorted, duplicate, or zero passcode.
This is intentionally stricter than the current OCGForge generator input,
which canonicalizes its combined deck/code input with `sorted(set(...))`.
The distinction is normative:

```text
OCGForge generator input:
    duplicate inputs may be canonicalized

Ignis provider coverage manifest:
    duplicate entries are malformed and are rejected
```

The coverage digest is:

```text
coverage_digest_sha256 =
SHA256(canonical coverage bytes)
```

Its exact byte domain is:

```text
ASCII bytes:
OCGFORGE-IGNIS-I6C5-PRINTED-COVERAGE-V1\0

passcode_count : uint32 big-endian

each passcode in strict ascending order:
    passcode : uint32 big-endian
```

The set of semantic-row `code` values MUST equal the declared coverage set
exactly. A subset, superset, lazy fill, or runtime coverage growth is invalid.

## 7. Source artifact and hash separation

The raw externally provisioned source artifact has a separate integrity hash:

```text
source_artifact_sha256 = SHA256(exact externally provisioned file bytes)
```

The intended OCGForge-generated source format is the existing 12-field,
pipe-delimited representation:

```text
code|alias|setcode|type|level|attribute|race|atk|def|lscale|rscale|link_marker
```

The expected source-format requirements are:

```text
UTF-8
LF line endings
no BOM

source_artifact_format_id =
ocgforge-ignis.i6c5.printed-source-artifact.pipe12.v1
```

`source_artifact_sha256` is `INTEGRITY / FORENSIC PROVENANCE`. It is not the
provider semantic digest. An alias-only or setcode-only source change may
change the raw artifact hash while leaving the provider semantic identity
unchanged when all canonical Printed rows remain equal.

The manifest is metadata and the artifact contains source rows. A future
loader MUST conceptually perform this order:

```text
parse manifest
→ validate manifest contract identifiers
→ read artifact
→ verify source_artifact_sha256
→ parse canonical rows
→ validate row ordering, uniqueness, codes, and coverage
→ compute and compare coverage_digest_sha256
→ compute and compare semantic_rows_digest_sha256
→ only then construct an immutable provider
```

Any mismatch MUST fail closed.

## 8. OCGForge-compatible Printed field mapping

The mapping from `PrintedSemanticRowV1` to
`PerspectiveSafeCardPropertiesV1.Printed` is frozen as follows.

For every card:

```text
Type      = row.type
Attribute = row.attribute
Race      = row.race
Attack    = row.attack
```

The projection follows the OCGForge control flow independently for each type
bit. For non-Link cards:

```text
Defense = row.defense
```

For Link cards:

```text
Defense = ABSENT
```

For Xyz cards:

```text
Rank = row.level
```

For a card which is not Xyz and is not Link:

```text
Level = row.level
```

For Link cards, independently of the Xyz and Pendulum checks:

```text
LinkRating = row.level
LinkMarkers = row.link_marker bits in this order:
    BottomLeft
    Bottom
    BottomRight
    Left
    Right
    TopLeft
    Top
    TopRight
```

For Pendulum cards:

```text
LeftScale  = row.left_scale
RightScale = row.right_scale
```

For non-Pendulum cards:

```text
LeftScale  = ABSENT
RightScale = ABSENT
```

Printed MUST NOT populate:

```text
BaseAttack
BaseDefense
StatusFlags
Counters
```

Those values belong to current/runtime semantics. Printed and Current are not
interchangeable.

The OCGForge transformation semantics are:

```text
left_scale  = (raw_level >> 16) & 0xff
right_scale = (raw_level >> 24) & 0xff
```

The EDOPro `DataManager` interpretation is not authoritative for this
contract. Its direct scale interpretation is the opposite bit assignment.

## 9. External provisioning boundary

The frozen architecture is:

```text
external/local trusted provisioning process
    → OCGForge-semantic source artifact and manifest
    → local files supplied to OCGForge-Ignis
    → manifest/hash/row/coverage verification
    → immutable read-only provider
    → I6C5 source construction
```

This is `OPTION_B_EXTERNAL_PROVISIONING`.

OCGForge-Ignis MUST NOT:

```text
download BabelCDB
clone BabelCDB
open arbitrary cards.cdb
scan EDOPro directories
load expansions/*.cdb
load archive CDBs
perform runtime network fetches
auto-update static data
```

The Ignis repository and release do not contain BabelCDB or the generated
provider artifact unless a later explicit governance decision authorizes that
distribution. This contract does not authorize the provisioning workflow.

## 10. Provider lifetime and ownership

The provider binding MUST be captured no later than run/session construction
and MUST remain immutable for the duel:

```text
one duel/session
→ one Printed provider semantic identity
→ every frame uses that binding
```

A future owner is the Gameplay session or an immediate run owner, conceptually:

```text
GameplayMirrorSessionV1 / run owner
    ├─ PerspectiveSafeMatchContextV1
    └─ immutable Printed provider binding
```

The provider MUST NOT be stored in `PerspectiveStateMirrorV1`. The Mirror
remains the protocol-derived current-state authority. The Printed provider is
a separate static-source seam.

Printed provider identity and bytes MUST NOT be placed inside
`PerspectiveSafeMatchContextV1` merely for convenience.

## 11. Privacy and failure semantics

The only permitted information-flow direction is:

```text
already-known perspective-safe passcode
    → static Printed properties
```

The following are forbidden:

```text
Printed properties → possible passcodes
reverse lookup
runtime enumeration for hidden-card inference
current properties → guessed Printed properties
```

The provider MUST NOT be queried for an unknown or redacted identity.

The privacy and missing-coverage gates are explicitly:

```text
UNKNOWN_IDENTITY_PROVIDER_LOOKUP_ALLOWED=NO
REVERSE_LOOKUP_ALLOWED=NO
RUNTIME_ENUMERATION_FOR_INFERENCE_ALLOWED=NO
MISSING_COVERAGE_BEHAVIOR=FAIL_CLOSED
```

For a known identity:

```text
passcode covered
    → lookup permitted

passcode absent from coverage
    → fail closed; no complete I6C5 frame
```

The provider MUST NOT return `Printed=null`, zero/default properties, an EDOPro
fallback, a local unbound CDB fallback, or a network result to keep a frame
alive.

## 12. Three provenance layers

These layers are separate.

### A. Provider semantic identity

Exactly:

```text
provider_contract_id
semantic_rows_contract_id
field_mapping_contract_id
coverage_contract_id
coverage_digest_sha256
semantic_rows_digest_sha256
```

These fields are `SEMANTIC` and determine Printed-provider semantic equality.

### B. Environment semantic provenance

The environment compatibility envelope includes:

```text
ocgforge_semantic_commit
rules_bundle_id
```

These values bind the provider claim to the OCGForge environment contract. They
do not become fields of `PrintedProviderSemanticIdentityV1`.

### C. Forensic/source provenance

The forensic envelope may include:

```text
babelcdb_repository
babelcdb_commit
babelcdb_checkout_sha256
cards_cdb_sha256
transformation_source_repository
transformation_source_commit
transformation_source_path
transformation_file_sha256
source_artifact_format_id
source_artifact_sha256
```

These fields answer where the rows came from and how they were produced. They
are not independently gameplay-semantic identity inputs once the canonical
semantic rows are fixed.

The currently reproduced transformation-file SHA is retained as forensic
provenance only:

```text
transformation_source_commit =
f929de0b4d4157327dba003067d2e21e42f7ad75

transformation_source_path =
tools/prepare_card_data.py

transformation_file_sha256 =
78ae588acfe82466e23fde6279fa10b62062e1c3514e47ba9e71f41ef3ebd0ca
```

This value is not a provider semantic identity field and does not by itself
authorize third-party reuse.

## 13. Manifest semantics

The future logical manifest identifier is:

```text
manifest_contract_id =
ocgforge-ignis.i6c5.printed-manifest.v1
```

The manifest MUST carry the following logical groups.

Provider semantic identity:

```text
provider_contract_id
semantic_rows_contract_id
field_mapping_contract_id
coverage_contract_id
coverage_digest_sha256
semantic_rows_digest_sha256
```

Environment compatibility/provenance:

```text
ocgforge_semantic_commit
rules_bundle_id
```

Source/forensic provenance:

```text
babelcdb_repository
babelcdb_commit
babelcdb_checkout_sha256
cards_cdb_sha256
transformation_source_repository
transformation_source_commit
transformation_source_path
transformation_file_sha256
source_artifact_format_id
source_artifact_sha256
```

For this V1 manifest, `babelcdb_checkout_sha256` is required whenever the
manifest claims the pinned BabelCDB source. No forensic field in the V1
manifest is implicitly optional. A future manifest revision MAY define an
alternate source and explicit optionality; it MUST NOT do so silently under
this V1 contract. Semantic identity fields MUST NOT be absent.

JSON whitespace, JSON property order, and a raw JSON hash are not frozen by
this contract as gameplay semantics.

## 14. Replay and identity consequences

The minimum Printed gameplay-replay identity is:

```text
PrintedProviderSemanticIdentityV1
```

Forensic audit records MAY additionally retain the environment and source
provenance envelopes. Those values MUST NOT be silently mixed into a semantic
gameplay hash.

The intended equality rule is:

```text
same semantic identity
→ same covered passcodes
→ same canonical Printed rows
→ same PerspectiveSafeCardPropertiesV1.Printed values
```

## 15. Third-party governance status

This technical contract does not clear governance:

```text
TECHNICAL_CONTRACT_FROZEN=YES
THIRD_PARTY_GOVERNANCE_CLEARED=NO
BABELCDB_REDISTRIBUTION_AUTHORIZED=NO
DERIVED_DATA_REDISTRIBUTION_AUTHORIZED=NO
PRINTED_PROVIDER_IMPLEMENTATION_AUTHORIZED=NO
```

The unresolved BabelCDB license declaration and derived-data redistribution
question are governance findings, not legal conclusions. No wording in this
contract asserts that redistribution is lawful or unlawful.

## 16. Non-goals

This contract does not authorize:

```text
BabelCDB inclusion
derived artifact inclusion
provider implementation
manifest parser or artifact loader
database tooling
runtime database hashing
EDOPro DataManager use as authority
general card search or text lookup
artwork, localization, rulings, or deck-building services
MSG_SWAP_GRAVE_DECK
I6C5 final acceptance
I6C6
I6D
I7
```

## 17. Future implementation gates

A later implementation task MUST prove at least:

```text
manifest contract validation
source artifact hash validation
canonical row parsing
strict row ordering and uniqueness
exact coverage equality
coverage digest equality
semantic row digest equality
environment compatibility
unknown-identity no-lookup behavior
missing-coverage fail-closed behavior
provider immutability for the session lifetime
fresh-process semantic determinism
paired-world privacy
```

The implementation remains blocked until the separate third-party/derived-data
governance decision is complete.

```text
PRINTED_PROVIDER_TECHNICAL_CONTRACT_READY=YES
PRINTED_PROVIDER_IMPLEMENTATION_READY=NO
```
