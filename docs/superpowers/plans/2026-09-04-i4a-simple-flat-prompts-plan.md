# I4A Simple Flat Prompt Runtime Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the three authorized modern flat-prompt families — YESNO, OPTION, and POSITION — as a deterministic, atomic, stale-safe `FlatPromptSessionV1` without changing I3, networking, model input, or the frozen I4A0 contract.

**Architecture:** Keep `GameplayMessageDecoderV1`, `GameplayMirrorSessionV1`, `PerspectiveStateMirrorV1`, and `PublicStateProjectionResultV1` unchanged. Add a single `FlatPromptSessionV1` lifecycle owner, an internal `FlatPromptProjectionV1` parser helper, discriminated public context/candidate values that preserve `ABSENT`, and an internal value-owned binding plus opaque selection handle.

**Tech Stack:** C#/.NET 10, nullable reference types, deterministic Release builds, existing executable Gameplay/Client/Protocol harnesses, `System.Buffers.Binary`, `System.Globalization`, and explicit ordered arrays. No new package or network dependency.

---

## Scope map

The implementation is limited to these five files:

- Create `src/OCGForge.Ignis.Gameplay/FlatPromptTypesV1.cs` for I4 family/error enums, discriminated public values, projection result, and internal binding/selection values.
- Create `src/OCGForge.Ignis.Gameplay/FlatPromptProjectionV1.cs` for the internal exact-wire parser and temporary projection draft. This file has no public façade, ordinal owner, or binding owner.
- Create `src/OCGForge.Ignis.Gameplay/FlatPromptSessionV1.cs` for the sole I4 lifecycle owner, ordinal, current binding, acceptance transaction, selection capture, and private response resolution.
- Create `tests/OCGForge.Ignis.Gameplay.Tests/Tests/I4AFlatPromptProjectionTests.cs` for frozen-vector-derived red/green coverage.
- Modify `tests/OCGForge.Ignis.Gameplay.Tests/Program.cs` only by appending deterministic I4A registrations after the existing 53 registrations.

Do not modify these files:

- `src/OCGForge.Ignis.Gameplay/GameplayMessageDecoderV1.cs`
- `src/OCGForge.Ignis.Gameplay/GameplayMessageTypesV1.cs`
- `src/OCGForge.Ignis.Gameplay/GameplayMirrorSessionV1.cs`
- `src/OCGForge.Ignis.Gameplay/PerspectiveStateMirrorV1.cs`
- `src/OCGForge.Ignis.Gameplay/PublicSemanticLocatorV1.cs`
- `src/OCGForge.Ignis.Gameplay/PublicStateProjectionV1.cs`
- `fixtures/gameplay/v1/i4-flat-prompt-vectors.v1.json`
- `fixtures/gameplay/v1/game-message-support.v1.json`
- `docs/contracts/flat-prompt-projection-v1.md`
- any Protocol, Client, workflow, dependency, I3 contract, or I3 golden file

The frozen source of truth is:

```text
docs/contracts/flat-prompt-projection-v1.md
fixtures/gameplay/v1/i4-flat-prompt-vectors.v1.json
fixtures/gameplay/v1/game-message-support.v1.json
```

The design document is already committed at
`60c738b78540cc3fb4e2483b20c66221b7c49265`. This plan is committed next as a
docs-only commit with subject
`docs: add I4A simple flat prompt implementation plan`. The resulting commit
is `PLAN_HEAD`; the later feature commit must use that exact commit as its
parent. The branch-wide post-feature diff contains seven files: the two
committed design/plan documents plus the five implementation files. The
feature scope itself remains five files.

## Task 1: Reconfirm the exact implementation checkpoint

**Files:** none.

- [ ] **Step 1: Verify the worktree and branch before implementation.**

Run from `C:\Users\chris\Documents\OCGForge-Ignis`:

```powershell
git status --short --branch
git rev-parse --show-toplevel
git rev-parse HEAD
```

Expected:

```text
## chris/i4a-simple-flat-prompts
C:/Users/chris/Documents/OCGForge-Ignis
```

The worktree must be clean before implementation edits begin. `HEAD` is the
current approved design commit; record its value from `git rev-parse HEAD`
instead of hard-coding a future commit identity.

- [ ] **Step 2: Verify that remote `main` still equals the authorized base.**

Run:

```powershell
$base = '13e0831c1364e329b62a1beb11f67d6ae8e682d1'
$remoteMain = (git ls-remote origin refs/heads/main).Split()[0]
if ($remoteMain -ne $base) {
    throw "STATUS=BLOCKED_BASE_MOVED REMOTE_HEAD=$remoteMain EXPECTED_BASE=$base"
}
git merge-base --is-ancestor $base HEAD
if ($LASTEXITCODE -ne 0) {
    throw "The implementation branch must retain the authorized base in its ancestry."
}
Write-Output "BASE=$base"
Write-Output "REMOTE_HEAD=$remoteMain"
```

Expected:

```text
BASE=13e0831c1364e329b62a1beb11f67d6ae8e682d1
REMOTE_HEAD=13e0831c1364e329b62a1beb11f67d6ae8e682d1
```

If the remote value differs, stop with `STATUS=BLOCKED_BASE_MOVED` and do not
create or modify implementation files. If the branch has been altered from
the design commit, stop and report the actual `HEAD` and parent values.

- [ ] **Step 3: Record the pre-change source identity and catalog count.**

Run:

```powershell
git log -2 --oneline --decorate
$base = '13e0831c1364e329b62a1beb11f67d6ae8e682d1'
git diff --name-only $base
```

Expected source diff before implementation is the two committed design/plan
documents. `HEAD` is the exact `PLAN_HEAD` produced by the docs-only commit;
obtain it from `git rev-parse HEAD` rather than guessing it. Count the existing
`tests` array entries in `Program.cs`; the accepted baseline is 53. Do not alter
that existing sequence.

## Task 2: Add frozen-vector-derived red tests and append the harness catalog

**Files:**

- Create: `tests/OCGForge.Ignis.Gameplay.Tests/Tests/I4AFlatPromptProjectionTests.cs`
- Modify: `tests/OCGForge.Ignis.Gameplay.Tests/Program.cs` after its current final test registration

- [ ] **Step 1: Add the deterministic I4A registrations without moving existing tests.**

Append these entries after the existing I3D registrations in the `tests`
array:

```csharp
    ("I4A YESNO exact domain and response values",
        I4AFlatPromptProjectionTests.TestYesNoExactDomain),

    ("I4A YESNO malformed input and ownership",
        I4AFlatPromptProjectionTests.TestYesNoFailuresAndOwnership),

    ("I4A OPTION source order and public values",
        I4AFlatPromptProjectionTests.TestOptionSourceOrderAndValues),

    ("I4A OPTION duplicates and local-key metamorphic identity",
        I4AFlatPromptProjectionTests.TestOptionDuplicatesAndMetamorphicKey),

    ("I4A OPTION invalid domains fail closed",
        I4AFlatPromptProjectionTests.TestOptionFailures),

    ("I4A POSITION valid mask order and private responses",
        I4AFlatPromptProjectionTests.TestPositionValidMasks),

    ("I4A POSITION invalid masks fail closed",
        I4AFlatPromptProjectionTests.TestPositionFailures),

    ("I4A POSITION unbound card code stays absent",
        I4AFlatPromptProjectionTests.TestPositionUnboundCardCode),

    ("I4A private binding resolves exact response values",
        I4AFlatPromptProjectionTests.TestExactResponseBindings),

    ("I4A stale same-looking selection is rejected",
        I4AFlatPromptProjectionTests.TestStaleSelection),

    ("I4A invalid key family and domain bindings fail closed",
        I4AFlatPromptProjectionTests.TestBindingValidationFailures),

    ("I4A failed prompts do not publish or advance state",
        I4AFlatPromptProjectionTests.TestFailureAtomicityAndOrdinal),

    ("I4A public values preserve the privacy boundary",
        I4AFlatPromptProjectionTests.TestPublicApiBoundary),

    ("I4A public values own source data",
        I4AFlatPromptProjectionTests.TestValueOwnership)
```

The 53 existing registrations must remain byte-for-byte ordered before these
new entries. The eventual Gameplay catalog is expected to contain 67 entries.

- [ ] **Step 2: Create the red-test helpers using the frozen vector bytes.**

Use the existing `TestAssert` helpers and `System.Buffers.Binary`. The test
file must use exact payloads from the checked-in I4A0 vectors, not generated or
rewritten fixture data:

```csharp
private static readonly byte[] YesNoDescription =
{
    0x0D, 0x00, 0x08, 0x07, 0x06,
    0x05, 0x04, 0x03, 0x02, 0x01
};

private static readonly byte[] OptionWireOrder =
{
    0x0E, 0x00, 0x03,
    0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11,
    0x11, 0x00, 0xFF, 0xEE, 0xDD, 0xCC, 0xBB, 0xAA
};

private static readonly byte[] OptionDuplicates =
{
    0x0E, 0x00, 0x02,
    0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11,
    0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11
};

private static readonly byte[] PositionThreeBits =
{
    0x13, 0x00, 0xBE, 0xBA, 0xFE, 0xCA, 0x0D
};
```

Keep the vector case IDs in test method comments, for example
`YESNO_DESCRIPTION_U64`, `OPTION_WIRE_ORDER`,
`OPTION_DUPLICATE_DESCRIPTIONS`, and
`POSITION_THREE_BITS_IN_EDOPRO_ORDER`, so each assertion remains traceable to
the frozen fixture.

- [ ] **Step 3: Write the exact YESNO, OPTION, and POSITION assertions before production code exists.**

The red test methods must assert these public values and no others:

```csharp
FlatPromptSessionV1 session = new();
FlatPromptProjectionResultV1 result =
    session.TryAcceptPrompt(YesNoDescription);

True(result.IsSuccess);
Equal(FlatPromptErrorCodeV1.None, result.Error);
NotNull(result.Context);
NotNull(result.Candidates);
Equal(FlatPromptFamilyV1.MsgSelectYesNo, result.Context!.PromptFamily);
Equal((byte)0, result.Context.ActingPlayer);
Equal(2, result.Candidates!.Count);
Equal("MSG_SELECT_YESNO:NO", result.Candidates[0].I4LocalCandidateKey);
Equal("MSG_SELECT_YESNO:YES", result.Candidates[1].I4LocalCandidateKey);
Equal(FlatPromptChoiceKindV1.No, result.Candidates[0].ChoiceKind);
Equal(FlatPromptChoiceKindV1.Yes, result.Candidates[1].ChoiceKind);
```

The OPTION tests must assert the three exact values
`1`, `0x1122334455667788`, and `0xAABBCCDDEEFF0011`, source ordinals
`0, 1, 2`, keys ending in `:0`, `:1`, `:2`, and source order. The duplicate
test must assert two candidates with equal `OptionValue` but different keys
and ordinals. The metamorphic test must compare two prompts with the same
ordinal and different values and assert equal local keys but different
`OptionValue` values.

The POSITION tests must assert mask `0x0d`, candidate order
`FACEUP_ATTACK`, `FACEUP_DEFENSE`, `FACEDOWN_DEFENSE`, position values
`1, 4, 8`, and no `PositionCardCode` member on the unbound context type.

- [ ] **Step 4: Add all required negative and binding assertions.**

Use the exact negative fixture payloads and assert a failed result has
`IsSuccess == false`, `Context == null`, and `Candidates == null` for each:

```text
YESNO_TRUNCATED_DESCRIPTION
YESNO_TRAILING_BYTE
YESNO_UNSUPPORTED_LEGACY_WIDTH
OPTION_ZERO_COUNT
OPTION_COUNT_BODY_MISMATCH
OPTION_TRAILING_BYTE
OPTION_COUNT_U8_MAX_WITHOUT_BODY
OPTION_UNSUPPORTED_LEGACY_WIDTH
POSITION_INVALID_HIGH_BIT
POSITION_ZERO_MASK
POSITION_SINGLETON_MASK
POSITION_TRAILING_BYTE
POSITION_INVALID_PARTICIPANT
```

For `YESNO_UNSUPPORTED_LEGACY_WIDTH`, assert
`result.Error == FlatPromptErrorCodeV1.UnsupportedPromptLayout`. For
`OPTION_UNSUPPORTED_LEGACY_WIDTH`, assert the same concrete error code. Assert
`MalformedPrompt` for the non-legacy truncation/trailing/count mismatches and
`InvalidPositionMask` for the three invalid mask forms.

Assert these specific semantic error codes where the failure is selected by a
binding operation:

```text
invalid local key                  -> InvalidI4LocalCandidateKey
old handle after new prompt        -> StalePromptBinding
handle family mismatch             -> StalePromptBinding
handle complete-domain mismatch    -> StalePromptBinding
missing response map entry         -> InvalidResponseBinding
swapped candidate/key vectors      -> InvalidResponseBinding and no binding
```

The tests must also assert that one OPTION candidate does not resolve
automatically: acceptance returns one candidate and no response resolution
occurs until an explicit internal selection handle is captured. Pass a
`null`, empty, and unknown key to `TryCaptureSelection(string? ...)` and
assert `InvalidI4LocalCandidateKey`, a null handle, and no exception for each.
Construct two valid OPTION descriptors, reverse only the local-key array, and
pass the original response array to `CurrentFlatPromptBindingV1.TryCreate`; the
factory must return `InvalidResponseBinding` with a null binding.

- [ ] **Step 5: Run the red harness before creating production types.**

Run:

```powershell
dotnet run --project tests/OCGForge.Ignis.Gameplay.Tests/OCGForge.Ignis.Gameplay.Tests.csproj --configuration Release
```

Expected: compilation failure because `FlatPromptSessionV1`, the frozen I4A
public value types, and the internal binding API do not yet exist. Do not
interpret this expected red run as an acceptance result and do not add a
fallback implementation to make the harness compile.

- [ ] **Step 6: Confirm the existing friend-test seam before using internal I4A APIs.**

The live repository already contains this line in
`src/OCGForge.Ignis.Gameplay/Properties/AssemblyInfo.cs:3`:

```csharp
[assembly: InternalsVisibleTo("OCGForge.Ignis.Gameplay.Tests")]
```

Use this existing assembly-level grant for the direct tests of
`CurrentFlatPromptBindingV1`, `FlatPromptSelectionHandleV1`,
`TryCaptureSelection`, and `TryResolveSelection`. Verify that the line remains
present and do not add a duplicate attribute to `FlatPromptTypesV1.cs`, add a
new file, or modify the project file.

## Task 3: Implement the discriminated value types and private binding values

**File:** `src/OCGForge.Ignis.Gameplay/FlatPromptTypesV1.cs`

- [ ] **Step 1: Define the exact family and semantic enums.**

Add these public enums and no unrelated family values:

```csharp
public enum FlatPromptFamilyV1 : byte
{
    MsgSelectYesNo = 13,
    MsgSelectOption = 14,
    MsgSelectPosition = 19
}

public enum FlatPromptChoiceKindV1 : byte
{
    No = 0,
    Yes = 1,
    Option = 2,
    FaceupAttack = 3,
    FacedownAttack = 4,
    FaceupDefense = 5,
    FacedownDefense = 6
}

public enum FlatPromptSourceSectionV1 : byte
{
    Options = 0
}
```

Use `FlatPromptFamilyV1` as the exact semantic `prompt_family` value. Do not
add `EffectYn`, `Chain`, `IdleCmd`, or `BattleCmd` to this I4A enum.

- [ ] **Step 2: Define structured I4A error values.**

Add an explicit result error enum containing these members:

```csharp
public enum FlatPromptErrorCodeV1 : byte
{
    None = 0,
    MalformedPrompt = 1,
    UnsupportedPromptLayout = 2,
    UnprovenPublicReference = 3,
    UnprovenCandidateDomain = 4,
    InvalidI4LocalCandidateKey = 5,
    StalePromptBinding = 6,
    InvalidResponseBinding = 7,
    InvalidParticipant = 8,
    InvalidPositionMask = 9,
    ZeroOptionDomain = 10,
    ArithmeticFailure = 11
}
```

The parser will use `MalformedPrompt` for truncation, trailing bytes, and
exact-length mismatch; `UnsupportedPromptLayout` for IDs outside 13, 14, and
19 or legacy widths; `InvalidParticipant` for players outside 0/1;
`ZeroOptionDomain` for OPTION count zero; and `InvalidPositionMask` for zero,
singleton, or high-bit POSITION masks. `UnprovenPublicReference` remains
available for future card-bearing families but is not returned by I4A's
unbound POSITION path.

- [ ] **Step 3: Define discriminated public contexts with no nullable irrelevant fields.**

Implement an abstract immutable `FlatPromptPublicContextV1` with only:

```csharp
public string ContractId { get; }
public FlatPromptFamilyV1 PromptFamily { get; }
public byte ActingPlayer { get; }
```

Use the exact contract ID constant
`ocgforge-ignis.flat-prompt-projection.v1`. Add these immutable variants with
internal constructors:

```csharp
public sealed record FlatPromptYesNoPublicContextV1 : FlatPromptPublicContextV1
{
    public ulong YesNoDescriptionId { get; }
}

public sealed record FlatPromptOptionPublicContextV1 : FlatPromptPublicContextV1
{
}

public sealed record FlatPromptPositionPublicContextV1 : FlatPromptPublicContextV1
{
    public byte PositionAllowedPositionsMask { get; }
}
```

`FlatPromptPositionPublicContextV1` must not declare `PositionCardCode` or a
card locator. This is the structural representation of the unbound I4A
POSITION result. No context variant may declare prompt ordinal, local key,
response, raw bytes, mirror identity, protocol offset, path, or timestamp.

- [ ] **Step 4: Define discriminated public candidates with exact family members.**

Implement an abstract immutable `FlatPublicCandidateDescriptorV1` with only:

```csharp
public string I4LocalCandidateKey { get; }
public FlatPromptChoiceKindV1 ChoiceKind { get; }
```

Add these variants with internal constructors and only the listed members:

```csharp
public sealed record FlatYesNoPublicCandidateDescriptorV1
    : FlatPublicCandidateDescriptorV1
{
}

public sealed record FlatOptionPublicCandidateDescriptorV1
    : FlatPublicCandidateDescriptorV1
{
    public FlatPromptSourceSectionV1 SourceSection { get; }
    public int SourceOrdinal { get; }
    public ulong OptionValue { get; }
}

public sealed record FlatPositionPublicCandidateDescriptorV1
    : FlatPublicCandidateDescriptorV1
{
    public byte PositionValue { get; }
}
```

No variant may declare a nullable catch-all field. Do not add card locators,
card codes, descriptions, response values, raw wire bytes, or public action
keys to these I4A types.

- [ ] **Step 5: Define the public projection result with atomic absence on failure.**

Implement `FlatPromptProjectionResultV1` as an immutable class with:

```csharp
public bool IsSuccess { get; }
public FlatPromptErrorCodeV1 Error { get; }
public FlatPromptPublicContextV1? Context { get; }
public IReadOnlyList<FlatPublicCandidateDescriptorV1>? Candidates { get; }
```

Its success factory must copy the candidate array into an
`Array.AsReadOnly` view. Its failure factory must set both `Context` and
`Candidates` to `null`. It must not expose an ordinal, binding, response, or
input bytes.

- [ ] **Step 6: Define the internal value-owned draft, binding, handle, and response result.**

Add internal types with these exact responsibilities:

```csharp
internal sealed class FlatPromptProjectionDraftV1
{
    private readonly FlatPublicCandidateDescriptorV1[] candidates;
    private readonly string[] localKeys;
    private readonly int[] responses;

    internal FlatPromptPublicContextV1 Context { get; }
    internal int Count => candidates.Length;
    internal FlatPublicCandidateDescriptorV1 GetCandidate(int index) =>
        candidates[index];
    internal string GetLocalKey(int index) => localKeys[index];
    internal int GetResponse(int index) => responses[index];
    internal FlatPublicCandidateDescriptorV1[] CopyCandidates() =>
        candidates.ToArray();
    internal string[] CopyLocalKeys() => localKeys.ToArray();
    internal int[] CopyResponses() => responses.ToArray();
}

internal sealed class CurrentFlatPromptBindingV1
{
    private readonly FlatPublicCandidateDescriptorV1[] candidates;
    private readonly ReadOnlyCollection<FlatPublicCandidateDescriptorV1> candidatesView;
    private readonly string[] localKeys;
    private readonly ReadOnlyCollection<string> localKeysView;
    private readonly Dictionary<string, int> responseByKey;

    internal ulong PromptInstanceOrdinal { get; }
    internal FlatPromptFamilyV1 Family { get; }
    internal IReadOnlyList<FlatPublicCandidateDescriptorV1> Candidates =>
        candidatesView;
    internal IReadOnlyList<string> LocalKeys => localKeysView;
    internal bool TryGetResponse(string key, out int response) =>
        responseByKey.TryGetValue(key, out response);
}

internal sealed class FlatPromptSelectionHandleV1
{
    private readonly FlatPublicCandidateDescriptorV1[] orderedDomain;
    private readonly ReadOnlyCollection<FlatPublicCandidateDescriptorV1> orderedDomainView;

    internal ulong PromptInstanceOrdinal { get; }
    internal FlatPromptFamilyV1 Family { get; }
    internal string I4LocalCandidateKey { get; }
    internal IReadOnlyList<FlatPublicCandidateDescriptorV1> OrderedDomain =>
        orderedDomainView;
}

internal readonly record struct FlatPromptResponseResolutionV1(int ResponseI32);
```

Every constructor must copy incoming arrays and wrap each private array with
`Array.AsReadOnly`; no array may be returned through an internal property. The
binding's private dictionary must be created with `StringComparer.Ordinal` and
exposed only through an internal `TryGetResponse` lookup. Public candidate
order comes from the read-only copied arrays, never dictionary iteration. The
handle must copy the complete domain and expose only a read-only view so
resolution can compare values rather than object identity.

## Task 4: Implement the internal exact-wire projection helper

**File:** `src/OCGForge.Ignis.Gameplay/FlatPromptProjectionV1.cs`

- [ ] **Step 1: Define the parser boundary and fixed constants.**

Create an `internal static class FlatPromptProjectionV1` with one entry point:

```csharp
internal static bool TryProject(
    ReadOnlySpan<byte> bytes,
    out FlatPromptProjectionDraftV1 draft,
    out FlatPromptErrorCodeV1 error)
```

Use `System.Buffers.Binary.BinaryPrimitives` and no network or outer-frame
types. Initialize `draft = null!` and `error = None`; every failure must
return `false` before any draft is published.

Dispatch only on `bytes[0]`:

```text
13 -> TryProjectYesNo
14 -> TryProjectOption
19 -> TryProjectPosition
other -> UnsupportedPromptLayout
```

An empty span returns `MalformedPrompt` without reading index zero. A complete
inner message is required; no parser may consume data outside the supplied
span.

- [ ] **Step 2: Implement YESNO exact parsing and projection.**

Use this exact sequence:

```csharp
if (bytes.Length == 6) return UnsupportedPromptLayout;
if (bytes.Length != 10) return MalformedPrompt;
if (bytes[1] > 1) return InvalidParticipant;
ulong description = BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice(2, 8));
```

The six-byte form is the frozen legacy-width discriminator. A different
non-modern length is malformed, not a legacy layout.

Build one `FlatPromptYesNoPublicContextV1` and exactly two ordered candidates:

```text
MSG_SELECT_YESNO:NO  / No  / response 0
MSG_SELECT_YESNO:YES / Yes / response 1
```

The context stores the exact decoded `ulong`; candidates contain no
description field. Store responses as signed `int` values in the internal
draft only.

- [ ] **Step 3: Implement OPTION exact count/length validation before allocation.**

Use this exact validation order:

```csharp
if (bytes.Length < 3) return MalformedPrompt;
if (bytes[1] > 1) return InvalidParticipant;
byte count = bytes[2];
int expectedLength;
try
{
    expectedLength = checked(3 + checked(8 * count));
}
catch (OverflowException)
{
    return ArithmeticFailure;
}
if (count == 0) return ZeroOptionDomain;
int legacyLength = checked(3 + checked(4 * count));
if (bytes.Length == legacyLength && legacyLength != expectedLength)
{
    return UnsupportedPromptLayout;
}
if (bytes.Length != expectedLength) return MalformedPrompt;
```

Allocate exactly `count` candidate slots only after all checks pass. For each
ordinal `i` from zero through `count - 1`, decode the exact `ulong` at
`bytes.Slice(3 + 8 * i, 8)`, preserve it as `OptionValue`, set
`SourceSection = FlatPromptSourceSectionV1.Options`, and bind private response
`i`.

Construct the local key exactly as:

```csharp
private static string CreateOptionKey(int sourceOrdinal) =>
    "MSG_SELECT_OPTION:OPTION:" +
    sourceOrdinal.ToString(CultureInfo.InvariantCulture);
```

Before returning the key, require `sourceOrdinal >= 0` and ensure the output
contains only ASCII decimal digits after the final colon. Invariant formatting
must produce `0`, `1`, …, `254`: no sign, no localized digits, and no leading
zeros except the single character `0`. Use `StringComparer.Ordinal` for all
later key comparisons.

- [ ] **Step 4: Implement POSITION exact mask validation and explicit order.**

Use this exact validation order:

```csharp
if (bytes.Length != 7) return MalformedPrompt;
if (bytes[1] > 1) return InvalidParticipant;
_ = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(2, 4));
byte mask = bytes[6];
if (mask == 0 || (mask & 0xF0) != 0 ||
    BitOperations.PopCount((uint)mask) < 2)
{
    return InvalidPositionMask;
}
```

Build `FlatPromptPositionPublicContextV1` with only `mask`. Do not copy the
wire `card_code`, do not create a locator, and do not inspect the mirror or
public projection. Iterate this fixed table, in this order, and append only
set bits:

```csharp
private static readonly (byte Bit, FlatPromptChoiceKindV1 Kind, string Key)[] PositionChoices =
{
    (0x01, FlatPromptChoiceKindV1.FaceupAttack,
        "MSG_SELECT_POSITION:FACEUP_ATTACK"),
    (0x02, FlatPromptChoiceKindV1.FacedownAttack,
        "MSG_SELECT_POSITION:FACEDOWN_ATTACK"),
    (0x04, FlatPromptChoiceKindV1.FaceupDefense,
        "MSG_SELECT_POSITION:FACEUP_DEFENSE"),
    (0x08, FlatPromptChoiceKindV1.FacedownDefense,
        "MSG_SELECT_POSITION:FACEDOWN_DEFENSE")
};
```

Each emitted `PositionValue` and private response equals its bit. The public
domain remains complete even though `position_card_code` is absent.

- [ ] **Step 5: Keep the projection helper internal and independent of I3.**

Verify the helper has no public type, constructor, or method accepting
`PerspectiveStateMirrorV1`, `MirrorSnapshotV1`, `PublicStateProjectionResultV1`,
`PublicSemanticLocatorV1`, `GameplayMirrorSessionV1`, or a socket. It may
reference only the I4A value types and BCL parsing primitives. Do not add a
second public projection façade.


## Task 5: Implement session lifecycle, atomic binding, and stale-safe resolution

**File:** `src/OCGForge.Ignis.Gameplay/FlatPromptSessionV1.cs`

- [ ] **Step 1: Define the sole session owner and acceptance API.**

Create a public sealed `FlatPromptSessionV1` with only private lifecycle state:

```csharp
private ulong nextPromptOrdinal;
private CurrentFlatPromptBindingV1? currentBinding;
```

Define the public acceptance method:

```csharp
public FlatPromptProjectionResultV1 TryAcceptPrompt(
    ReadOnlySpan<byte> completeInnerGameMessage)
```

The type must have no public mirror, projection, ordinal, binding, socket,
response, or model property. The method is synchronous and accepts exactly one
complete inner `GAME_MSG` value.

- [ ] **Step 2: Implement acceptance as a two-phase transaction.**

Use this algorithm without mutation between phases:

```text
1. Call FlatPromptProjectionV1.TryProject on the supplied span.
2. On parser failure:
   - set currentBinding = null;
   - do not change nextPromptOrdinal;
   - return a failure result with null context and candidates.
3. Compute `nextPromptOrdinal + 1` with checked arithmetic in a local variable;
   do not mutate session state.
4. Copy the draft context with `CopyCandidates()`, `CopyLocalKeys()`, and
   `CopyResponses()`; never retain a draft-owned array.
5. Call `CurrentFlatPromptBindingV1.TryCreate` using the current ordinal and
   copied values; treat a duplicate key or length mismatch as a failed
   transaction.
6. Build the public result from the copied public values only.
7. Commit both state values together: replace `currentBinding` and assign the
   precomputed next ordinal.
8. Return the public success result.
```

If checked arithmetic or binding construction fails, clear `currentBinding`,
leave `nextPromptOrdinal` unchanged, and return the corresponding structured
error. Do not publish a partially constructed result.

- [ ] **Step 3: Implement the binding factory and invariant OPTION key lookup.**

Give `CurrentFlatPromptBindingV1` an internal static `TryCreate` factory with
the values listed in Task 3 and `out CurrentFlatPromptBindingV1? binding` plus
`out FlatPromptErrorCodeV1 error`. The factory must:

```text
set binding = null and error = None
reject unequal candidate/key/response array lengths
reject a zero candidate count
copy every input array
create Dictionary<string, int>(StringComparer.Ordinal)
for every index, reject null candidate/key values
for every index, require candidate[i].I4LocalCandidateKey == key[i]
  with StringComparison.Ordinal
for every index, require the family matches the descriptor subtype
for every index, require the response equals the descriptor's exact semantic
  response (YESNO 0/1, OPTION SourceOrdinal, POSITION PositionValue)
insert each key/response pair in array order
return InvalidResponseBinding if any key is duplicated
construct the binding only after all inserts succeed
```

When building the private response dictionary, insert each local key with its
corresponding signed response in source order. The authorized families cannot
produce duplicates because OPTION ordinals and YESNO/POSITION semantic keys
are unique. Never normalize keys, trim strings, parse localized digits, or
enumerate the dictionary for public output.

- [ ] **Step 4: Implement internal opaque selection capture.**

Add this internal operation for the later runtime selector and friend tests:

```csharp
internal bool TryCaptureSelection(
    string? i4LocalCandidateKey,
    out FlatPromptSelectionHandleV1? handle,
    out FlatPromptErrorCodeV1 error)
```

If the key is `null` or empty, or if there is no current binding or no exact
ordinal key match, return `false`, set `handle = null`, and use
`InvalidI4LocalCandidateKey`; no exception is allowed. Use
`StringComparer.Ordinal` to find a non-empty key in `currentBinding`. On
success, copy the current binding's ordered candidate domain into a new handle
carrying the current ordinal, family, and exact key. Do not return the binding
itself.

- [ ] **Step 5: Implement internal stale-safe response resolution.**

Add:

```csharp
internal bool TryResolveSelection(
    FlatPromptSelectionHandleV1? handle,
    out FlatPromptResponseResolutionV1 response,
    out FlatPromptErrorCodeV1 error)
```

Return `InvalidResponseBinding` for a null handle or missing response map.
Return `StalePromptBinding` unless all of these checks pass:

```text
currentBinding exists
handle.PromptInstanceOrdinal == currentBinding.PromptInstanceOrdinal
handle.Family == currentBinding.Family
handle.OrderedDomain equals currentBinding.Candidates element-by-element
currentBinding.TryGetResponse(handle.I4LocalCandidateKey, out response)
```

Compare candidate values and types, not array or object identity. On success,
return only the internal signed `ResponseI32`. Do not construct
`CTOS_RESPONSE`, write a stream, or expose the response through a public
property.

- [ ] **Step 6: Verify that failed acceptance invalidates the prior binding.**

Capture a valid YESNO handle, submit a malformed OPTION or POSITION prompt,
and then attempt to resolve the old handle. The resolution must return
`StalePromptBinding`. Submit a new valid prompt afterward and verify its
ordinal is the next successful ordinal, not an ordinal consumed by the
failed prompt.

## Task 6: Run focused green tests and perform the API/privacy review

**Files:** the five implementation-scope files only.

- [ ] **Step 1: Run the Gameplay harness after the first implementation pass.**

Run:

```powershell
dotnet run --project tests/OCGForge.Ignis.Gameplay.Tests/OCGForge.Ignis.Gameplay.Tests.csproj --configuration Release
```

Expected after fixing implementation defects:

```text
RESULT passed=67 failed=0
```

If a test fails, inspect the first failure, make the smallest generic fix,
and rerun the focused harness. Do not add a family-name special case,
first-candidate fallback, automatic one-candidate response, or a retry policy.

- [ ] **Step 2: Verify public API reflection boundaries.**

In `TestPublicApiBoundary`, inspect public properties and methods of the I4A
public types and assert that no public name or value exposes:

```text
prompt_instance_ordinal
public_action_key
response_i32
response_body
raw GAME_MSG bytes
raw response bytes
MirrorEntityIdV1
ModernLocInfoV1
raw controller/location/sequence tuple
protocol offset
socket/session state
path
timestamp
PID
```

Also assert that `FlatPromptProjectionV1` and
`CurrentFlatPromptBindingV1` are not public types and that
`FlatPromptSessionV1` has no public overload requiring an I3 mirror or public
projection. The accepted I3D types and their canonical behavior must remain
unchanged.

- [ ] **Step 3: Verify value ownership.**

For successful OPTION acceptance, mutate the original input byte array after
acceptance and assert all `OptionValue` values and key order remain unchanged.
Capture a selection handle, accept a new prompt, and assert the old handle's
copied domain remains unchanged while resolution returns `StalePromptBinding`.
Attempt to mutate any candidate/result list through `IReadOnlyList` and assert
that no mutable caller-owned collection is retained.

- [ ] **Step 4: Verify the authorized family boundary.**

Submit message IDs 10, 11, 12, and 16 with otherwise plausible bytes and
assert `UnsupportedPromptLayout`, no context, no candidates, no binding, and
no ordinal advance. Do not add those family values to any I4A enum or parser
dispatch table.

## Task 7: Build all six projects and compare two fresh processes per harness

**Files:** none beyond the five implementation-scope files.

- [ ] **Step 1: Build every Release project with zero warnings and errors.**

Run an initial restore/build, then the no-restore gate:

```powershell
$projects = @(
    'src/OCGForge.Ignis.Protocol/OCGForge.Ignis.Protocol.csproj',
    'src/OCGForge.Ignis.Client/OCGForge.Ignis.Client.csproj',
    'src/OCGForge.Ignis.Gameplay/OCGForge.Ignis.Gameplay.csproj',
    'tests/OCGForge.Ignis.Protocol.Tests/OCGForge.Ignis.Protocol.Tests.csproj',
    'tests/OCGForge.Ignis.Client.Tests/OCGForge.Ignis.Client.Tests.csproj',
    'tests/OCGForge.Ignis.Gameplay.Tests/OCGForge.Ignis.Gameplay.Tests.csproj'
)

foreach ($project in $projects) {
    dotnet build $project --configuration Release
    if ($LASTEXITCODE -ne 0) { throw "BUILD_FAILED=$project" }
}

foreach ($project in $projects) {
    dotnet build $project --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw "NO_RESTORE_BUILD_FAILED=$project" }
}
```

Expected: all twelve build invocations — six projects in two modes — exit 0;
build output reports `0 Warning(s)` and `0 Error(s)` for every project. Record
the actual output rather than inferring counts from a prior run.

- [ ] **Step 2: Run each executable harness twice in fresh processes.**

Use the compiled DLLs and capture complete stdout, stderr, and exit code for
two separate process invocations:

```powershell
$harnesses = @(
    'tests/OCGForge.Ignis.Protocol.Tests/bin/Release/net10.0/OCGForge.Ignis.Protocol.Tests.dll',
    'tests/OCGForge.Ignis.Client.Tests/bin/Release/net10.0/OCGForge.Ignis.Client.Tests.dll',
    'tests/OCGForge.Ignis.Gameplay.Tests/bin/Release/net10.0/OCGForge.Ignis.Gameplay.Tests.dll'
)

function Invoke-Harness([string] $dll) {
    $stdoutFile = [IO.Path]::GetTempFileName()
    $stderrFile = [IO.Path]::GetTempFileName()
    try {
        $process = Start-Process -FilePath 'dotnet' `
            -ArgumentList @($dll) `
            -RedirectStandardOutput $stdoutFile `
            -RedirectStandardError $stderrFile `
            -NoNewWindow -PassThru -Wait
        [pscustomobject]@{
            ExitCode = $process.ExitCode
            Stdout = [IO.File]::ReadAllText($stdoutFile)
            Stderr = [IO.File]::ReadAllText($stderrFile)
        }
    }
    finally {
        Remove-Item -LiteralPath $stdoutFile, $stderrFile -Force
    }
}

foreach ($dll in $harnesses) {
    $first = Invoke-Harness $dll
    $second = Invoke-Harness $dll
    if ($first.ExitCode -ne $second.ExitCode) {
        throw "NONDETERMINISTIC_EXIT_CODE=$dll"
    }
    if ($first.Stdout -cne $second.Stdout) {
        throw "NONDETERMINISTIC_STDOUT=$dll"
    }
    if ($first.Stderr -cne $second.Stderr) {
        throw "NONDETERMINISTIC_STDERR=$dll"
    }
    if ($first.ExitCode -ne 0) {
        throw "HARNESS_FAILED=$dll"
    }
    Write-Output "PASS fresh-process deterministic $dll"
    Write-Output $first.Stdout
}
```

Expected baseline prefixes are Protocol `20` existing tests and Client `17`
existing tests. Gameplay must report all 67 tests passed. The exact complete
stdout must be byte-identical between each pair.

- [ ] **Step 3: Run the final focused commands without retries.**

Run:

```powershell
$base = '13e0831c1364e329b62a1beb11f67d6ae8e682d1'
git diff --check
git diff --stat $base
git diff --name-only $base
git status --short
```

Expected: no whitespace errors; exactly the five implementation files plus
the design document and this plan appear in the branch diff; no fixture or
frozen-contract path appears; and `git status --short` lists only the five
uncommitted implementation files. The worktree becomes clean only after the
feature commit in Task 8.

## Task 8: Scope audit, commit, push, and stop for independent review

**Files:** no additional files.

- [ ] **Step 1: Audit the final diff against the authorized scope.**

Run:

```powershell
$allowed = @(
    'src/OCGForge.Ignis.Gameplay/FlatPromptTypesV1.cs',
    'src/OCGForge.Ignis.Gameplay/FlatPromptProjectionV1.cs',
    'src/OCGForge.Ignis.Gameplay/FlatPromptSessionV1.cs',
    'tests/OCGForge.Ignis.Gameplay.Tests/Program.cs',
    'tests/OCGForge.Ignis.Gameplay.Tests/Tests/I4AFlatPromptProjectionTests.cs'
)
$changed = @(git diff --name-only 13e0831c1364e329b62a1beb11f67d6ae8e682d1)
foreach ($path in $changed) {
    if ($path -notin $allowed -and
        $path -ne 'docs/superpowers/specs/2026-09-04-i4a-simple-flat-prompts-design.md' -and
        $path -ne 'docs/superpowers/plans/2026-09-04-i4a-simple-flat-prompts-plan.md') {
        throw "UNAUTHORIZED_FILE=$path"
    }
}

if (@($changed | Where-Object { $_ -like 'fixtures/*' }).Count -ne 0) {
    throw 'FIXTURES_CHANGED=YES'
}
if (@($changed | Where-Object { $_ -eq 'docs/contracts/flat-prompt-projection-v1.md' }).Count -ne 0) {
    throw 'FROZEN_CONTRACT_CHANGED=YES'
}
```

Review the production diff for hidden identity, raw response, protocol
address, public-action-key, network, model, fallback, and I3 authority drift.
The final audit must establish:

```text
I3_SEMANTICS_CHANGED=NO
I3_PUBLIC_CANONICAL_BYTES_CHANGED=NO
I3_PUBLIC_PROJECTION_ID_CHANGED=NO
NETWORK_RESPONSE_SENDING_ADDED=NO
MODEL_INPUT_ADDED=NO
FALLBACK_ADDED=NO
PUBLIC_PRIVATE_SEAM=PASS
```

- [ ] **Step 2: Commit only after every executable gate passes.**

Stage the five implementation-scope files and create the requested feature
commit:

```powershell
git add `
    src/OCGForge.Ignis.Gameplay/FlatPromptTypesV1.cs `
    src/OCGForge.Ignis.Gameplay/FlatPromptProjectionV1.cs `
    src/OCGForge.Ignis.Gameplay/FlatPromptSessionV1.cs `
    tests/OCGForge.Ignis.Gameplay.Tests/Program.cs `
    tests/OCGForge.Ignis.Gameplay.Tests/Tests/I4AFlatPromptProjectionTests.cs
git commit -m 'feat: implement I4A simple flat prompt projection'
```

Do not amend the already committed design document. Do not commit generated
build output or fixture changes.

- [ ] **Step 3: Verify the commit identity and clean worktree.**

Run:

```powershell
git show -s --format='%H%n%P%n%s' HEAD
git status --short --branch
git diff --check HEAD^ HEAD
```

Expected: the feature commit's parent is the docs-only `PLAN_HEAD` commit,
the subject is exact, and the worktree is clean. Record the concrete parent
SHA from `git show`; it must equal the plan commit recorded before
implementation, not the design commit.

- [ ] **Step 4: Push only the requested branch and verify the remote head.**

Run:

```powershell
git push -u origin chris/i4a-simple-flat-prompts
git ls-remote origin refs/heads/chris/i4a-simple-flat-prompts
```

The returned remote SHA must equal the local feature `HEAD`. Do not create a
PR, merge, start I4B, start I5, or start I6.

## Final handoff values to collect

Report only values actually proven by executed commands. The final handoff
must include:

```text
TASK=I4A_SIMPLE_FLAT_PROMPT_RUNTIME_01
BASE=13e0831c1364e329b62a1beb11f67d6ae8e682d1
HEAD=the exact feature commit SHA printed by `git show`
PARENT=PLAN_HEAD
REMOTE_HEAD=the exact SHA printed by `git ls-remote`
BRANCH=chris/i4a-simple-flat-prompts
FILES_CHANGED=7
BRANCH_FILES_CHANGED=7
FEATURE_FILES_CHANGED=5
PRODUCTION_FILES_CHANGED=3
TEST_FILES_CHANGED=2
FIXTURES_CHANGED=NO
FROZEN_CONTRACT_CHANGED=NO
YESNO_IMPLEMENTED=YES
OPTION_IMPLEMENTED=YES
POSITION_IMPLEMENTED=YES
EFFECTYN_IMPLEMENTED=NO
CHAIN_IMPLEMENTED=NO
IDLECMD_IMPLEMENTED=NO
BATTLECMD_IMPLEMENTED=NO
FLAT_PROMPT_PUBLIC_CONTEXT_V1=IMPLEMENTED
FLAT_PUBLIC_CANDIDATE_DESCRIPTOR_V1=IMPLEMENTED
CURRENT_FLAT_PROMPT_BINDING_V1=IMPLEMENTED
POSITION_UNBOUND_CARD_CODE_REJECTS_PROMPT=NO
POSITION_UNBOUND_CARD_CODE_IS_ABSENT=YES
I3D_PUBLIC_PROJECTION_IS_PUBLIC_AUTHORITY=YES
MIRROR_PUBLIC_LOCATOR_AUTHORITY=NO
I4_LOCAL_CANDIDATE_KEY_IS_OCGFORGE_PUBLIC_ACTION_KEY=NO
I4_LOCAL_CANDIDATE_KEY_MODEL_INPUT_AUTHORIZED=NO
PRIVATE_RESPONSE_BINDING_PUBLIC=NO
NETWORK_RESPONSE_SENDING_ADDED=NO
MODEL_INPUT_ADDED=NO
STALE_PROMPT_BINDING_REJECTED=YES
FAILED_PROMPT_CREATES_BINDING=NO
FAILED_PROMPT_ADVANCES_ORDINAL=NO
N1_AUTO_ANSWER=NO
FALLBACK_ADDED=NO
PROTOCOL_TESTS=the passed/total reported by the Protocol harness
CLIENT_TESTS=the passed/total reported by the Client harness
GAMEPLAY_TESTS=the passed/total reported by the Gameplay harness
DETERMINISTIC_STDOUT_PROTOCOL=YES after the two-output comparison passes
DETERMINISTIC_STDOUT_CLIENT=YES after the two-output comparison passes
DETERMINISTIC_STDOUT_GAMEPLAY=YES after the two-output comparison passes
WARNINGS=the count reported by the six Release builds
ERRORS=the count reported by the six Release builds
DIFF_CHECK=PASS
I3_SEMANTICS_CHANGED=NO
I3_PUBLIC_CANONICAL_BYTES_CHANGED=NO
I3_PUBLIC_PROJECTION_ID_CHANGED=NO
I4A_SELF_FINAL_PASS=NO
I4B_STARTED=NO
I5_STARTED=NO
I6_STARTED=NO
PR_CREATED=NO
WORKTREE=CLEAN
STATUS=STOP_FOR_INDEPENDENT_REVIEW
```

The handoff must never self-declare `I4A_FINAL_PASS`; independent review owns
acceptance.
