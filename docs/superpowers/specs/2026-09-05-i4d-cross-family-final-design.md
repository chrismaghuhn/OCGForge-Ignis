# OCGForge-Ignis I4D — Cross-Family Acceptance / I4 FINAL

Status: DESIGN ONLY; I4D implementation is not authorized

Date: 2026-09-05

Accepted current main:

    6a98eefe980fc43d01b221a5704e45a52a5f9b5f

This document records the architecture audit and the smallest acceptance
design needed to close I4 as a whole. It does not change production code,
tests, fixtures, frozen contracts, workflows, model input, networking, I5,
or I6.

## 1. Scope, non-goals, and audit result

I4D introduces no prompt family. The frozen I4 family set remains exactly:

    MSG_SELECT_BATTLECMD = 10
    MSG_SELECT_IDLECMD   = 11
    MSG_SELECT_EFFECTYN  = 12
    MSG_SELECT_YESNO     = 13
    MSG_SELECT_OPTION    = 14
    MSG_SELECT_CHAIN     = 16
    MSG_SELECT_POSITION  = 19

The accepted ownership is:

    I4A  YESNO, OPTION, POSITION
    I4B  EFFECTYN, CHAIN
    I4C  BATTLECMD, IDLECMD

The upstream provenance remains the already accepted pair:

    EDOPro=30935e847165a9ef0e547fb51a43f36168fab7c7
    OCGCORE=46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57

I4D non-goals are any new prompt decoder, public-state projection, candidate
type redesign, response sender, model-input path, OCGForge identity mapping,
continuation state machine, I5 implementation, or I6 compatibility code.

I4D owns cross-family acceptance, not another decoder. It proves that the
already accepted seven family implementations compose through one current
prompt lifecycle without changing their family-specific semantics.

The existing runtime has two distinct acceptance surfaces that must both be
closed over the same seven-family set:

    TryProject(...)
        I4A one-argument surface for YESNO, OPTION, POSITION

    TryParseWireDraft(...) followed by authority projection
        I4B/I4C per-call surface for EFFECTYN, CHAIN, BATTLECMD, IDLECMD

I4_NO_EXTRA_FAMILY is proven only when every byte ID outside
{10, 11, 12, 13, 14, 16, 19} returns UnsupportedPromptLayout with null public
output through both surfaces. A single dispatch-table check is insufficient.

The live audit found:

    BLOCKERS=0
    MAJORS=0
    MINORS=0
    NOTES=1

The one NOTE is provenance only: roadmap issue #3 still contains its historic
2026-09-04 implementation status, while current main, merged I4A/I4B/I4C
history, and the current harness are authoritative. Issue #3 explicitly says
it is a roadmap/reminder tracker rather than task authorization.

No semantic production defect was found that requires an I4D code change.
Therefore:

    I4D_IMPLEMENTATION_CLASSIFICATION=TEST_ONLY
    FUTURE_PRODUCTION_FILES=0

## 2. Fresh baseline evidence

The baseline was executed from the clean branch created at the accepted main
head. These commands were run independently:

    dotnet run --project tests/OCGForge.Ignis.Protocol.Tests/OCGForge.Ignis.Protocol.Tests.csproj --configuration Release
    dotnet run --project tests/OCGForge.Ignis.Client.Tests/OCGForge.Ignis.Client.Tests.csproj --configuration Release
    dotnet run --project tests/OCGForge.Ignis.Gameplay.Tests/OCGForge.Ignis.Gameplay.Tests.csproj --configuration Release

Observed results:

    BASELINE_PROTOCOL_EXIT=0
    BASELINE_PROTOCOL_RESULT=RESULT passed=20 failed=0
    BASELINE_CLIENT_EXIT=0
    BASELINE_CLIENT_RESULT=RESULT passed=17 failed=0
    BASELINE_GAMEPLAY_EXIT=0
    BASELINE_GAMEPLAY_RESULT=RESULT passed=103 failed=0

Each compiled harness was then started twice in separate fresh dotnet processes.
The compared outputs were complete stdout, complete stderr, and exit code.
The exact fresh-process commands were:

    dotnet tests/OCGForge.Ignis.Protocol.Tests/bin/Release/net10.0/OCGForge.Ignis.Protocol.Tests.dll
    dotnet tests/OCGForge.Ignis.Client.Tests/bin/Release/net10.0/OCGForge.Ignis.Client.Tests.dll
    dotnet tests/OCGForge.Ignis.Gameplay.Tests/bin/Release/net10.0/OCGForge.Ignis.Gameplay.Tests.dll

Each command was executed twice.
The result was:

    Protocol  run 1/2 exit 0, stderr empty, stdout identical
    Client    run 1/2 exit 0, stderr empty, stdout identical
    Gameplay  run 1/2 exit 0, stderr empty, stdout identical
    BASELINE_FRESH_PROCESS_DETERMINISM=PASS

These are baseline facts. They do not constitute I4D or I4 FINAL acceptance
by themselves.

## 3. I4 ownership and authority

I4 remains one deep module with one lifecycle owner:

    FlatPromptSessionV1
        parses/accepts one complete prompt
        publishes one complete public flat domain
        owns the current prompt ordinal
        owns the private current-prompt binding
        captures and resolves internal selection handles

The family-specific parser and public records remain inside the existing I4
module. I4D adds no public facade and no second session.

For EFFECTYN, CHAIN, BATTLECMD, and IDLECMD, the accepted I4B authority
transaction remains unchanged:

    captured MirrorSnapshotV1
        private source-resolution evidence only

    accepted PublicStateProjectionResultV1.Snapshot
        sole public locator/CardCode publication authority

    recomputed projection
        consistency proof only

I4D must not construct a public locator from raw ModernLocInfoV1, a mirror
address, a mirror entity identity, a raw pile sequence, or a codec value. The
existing I4C/I4B candidates are already copied from accepted snapshot cards.

POSITION is the deliberate exception already accepted by I4A:

    POSITION_DOMAIN_AUTHORITY=VALIDATED_POSITION_MASK
    POSITION_CARD_LOCATOR=ABSENT
    POSITION_UNBOUND_CARD_CODE_REJECTS_PROMPT=NO
    POSITION_UNBOUND_CARD_CODE_IS_ABSENT=YES

I4D must prove this rule remains separate rather than imposing card-reference
requirements on every family.

The local key remains an I4 control-plane selector:

    I4_LOCAL_CANDIDATE_KEY_EQUALS_OCGFORGE_PUBLIC_ACTION_KEY=NO
    OCGFORGE_PUBLIC_ACTION_KEY_DERIVATION=I6_OWNED
    I4_MODEL_INPUT_AUTHORITY=ABSENT

I4D does not add response sending, CTOS_RESPONSE, model input, model runner,
policy scoring, continuation state, or OCGForge compatibility.

## 4. Cross-family lifecycle semantics

One FlatPromptSessionV1 instance must safely accept family changes. The required
lifecycle sequence is:

    YESNO
      → OPTION
      → POSITION
      → EFFECTYN
      → CHAIN
      → BATTLECMD
      → IDLECMD
      → YESNO

The first three values use the existing one-argument I4A interface. The four
card-bearing values use the existing I4B per-call overload with a valid mirror
and accepted public projection. I4D does not add an overload.

After each successful acceptance:

    the previous handle is stale
    the previous family cannot resolve against the new family
    the previous domain cannot be reused
    the new handle carries the new family and current complete domain
    the ordinal advances exactly once

The test must also construct a value-owned handle with the current domain but
an intentionally different family, where the internal seam permits it. That
handle must return StalePromptBinding; equal integer response values across
families are not evidence of compatibility.

The final YESNO acceptance must prove that a repeated family/key later in the
stream is still a new prompt instance. Matching text is not matching binding.

## 5. Failure atomicity across families

The cross-family transaction is:

    accepted family A
        → attempt family B
        → B fails
        → no public context or candidates
        → old A binding becomes unusable
        → ordinal is unchanged
        → next valid prompt receives previous ordinal + 1

I4D must exercise materially different failure paths:

    wire parse failure
        malformed/truncated OPTION after a valid YESNO

    authority/public-reference failure
        unproven EFFECTYN or BATTLE/IDLE card reference after a valid family

    candidate-domain failure
        valid zero-option BATTLE or IDLE after a valid family

    binding validation failure
        existing internal CurrentFlatPromptBindingV1.TryCreate negative seam,
        using wrong family/type/key/response tuples where constructible

Every failure must satisfy:

    IsSuccess=false
    Context=null
    Candidates=null
    current binding invalidated
    old handle resolves as StalePromptBinding
    ordinal unchanged

The following successful acceptance then captures a new handle and asserts:

    new.Ordinal == old.Ordinal + 1

No failed family may consume an ordinal, publish a partial domain, or leave a
private response available through an old handle.

## 6. Complete domains and response isolation

I4D treats the existing family tests as the detailed source-domain evidence
and adds a cross-family table proving that those domains do not collapse into
one generic integer choice space.

The accepted response examples are:

    YESNO       NO=0, YES=1
    OPTION      OPTION:i=i
    POSITION    position bit 1/2/4/8
    EFFECTYN    NO=0, YES=1
    CHAIN       CHAIN_ENTRY:i=i, NO_CHAIN=-1
    BATTLECMD   ACTIVATE:i=(i<<16)|0
                ATTACK:i=(i<<16)|1
                TO_M2=2, TO_EP=3
    IDLECMD     SUMMON:i=(i<<16)|0
                SPECIAL_SUMMON:i=(i<<16)|1
                REPOSITION:i=(i<<16)|2
                MSET:i=(i<<16)|3
                SSET:i=(i<<16)|4
                ACTIVATE:i=(i<<16)|5
                TO_BP=6, TO_EP=7, SHUFFLE_HAND=8

I4D must prove that a key is accepted only in its family/binding context. A
valid NO, OPTION:0, EFFECTYN:NO, and other candidates may share integer
responses, but a handle from one family never resolves against another.

The existing I4A/I4B/I4C groups remain authoritative for:

    complete legal domains
    exact source order
    duplicate preservation
    no sorting or deduplication
    no candidate cap or top-K filter
    no fabricated pass/cancel
    no N=1 auto-answer

I4D adds cross-family duplicate witnesses by invoking the existing accepted
vectors and constructing at least one duplicate-bearing domain from each
family where the contract permits it. Duplicate occurrences remain distinct by
their frozen source section/ordinal or family-local identity.

## 7. Public/private seam and layer barrier

The future I4D test must inspect every public I4 context and candidate type from
I4A, I4B, and I4C. It must reject public properties or methods exposing:

    response_i32 or raw response bytes
    ModernLocInfoV1 or raw protocol offsets
    MirrorSnapshotV1 or MirrorEntityIdV1
    socket, network, room, password, host, or port state
    process/PID, wall time, thread/task identity, or filesystem path
    prompt ordinal or private binding state
    OCGForge public_action_key
    model-input or checkpoint identity

It must additionally assert:

    conditional CardCode fields exist only on approved CardCode variants
    POSITION has no card locator/CardCode member in accepted output
    transition candidates have no card-bearing fields
    public candidate collections are read-only/value-owned
    FlatPromptSessionV1 has no public send/network/model method

Static audit gates inspect the I4 production files for later-layer imports and
vocabulary. The expected result is:

    I4_NETWORK_RESPONSE_SENDING=ABSENT
    I4_MODEL_INPUT_AUTHORITY=ABSENT
    I4_CONTINUATION_AUTHORITY=ABSENT
    I4_PUBLIC_ACTION_MAPPING=ABSENT

This is a layer-barrier proof, not an invitation to add abstractions for I5 or
I6.

## 8. Determinism and ownership

Within the I4D test file, equivalent accepted inputs must produce equal:

    success/failure result
    public context values
    ordered candidate count
    ordered candidate runtime types
    ordered local keys
    every public field
    private key-to-response resolution
    ordinal transition after success/failure

The values must not depend on dictionary or HashSet iteration, enum declaration
order, object identity, allocation order, PID, wall time, scheduling, locale,
absolute paths, or TCP segmentation.

The complete Gameplay harness is the process-level determinism witness. Its two
fresh runs must have identical stdout and exit code. Protocol and Client remain
paired regression witnesses even though I4D changes neither project.

## 9. Exact future I4D test catalog

The accepted current Gameplay harness has 103 top-level registrations. I4D
adds exactly five coherent registrations, for a future total of 108:

    01 I4D seven-family support and unsupported boundary
    02 I4D cross-family binding lifecycle
    03 I4D failure atomicity and ordinal isolation
    04 I4D complete domains and response isolation
    05 I4D public/private authority determinism barrier

The five groups are minimal by invariant: family-set closure, lifecycle
closure, failure/ordinal closure, domain/response closure, and public
authority/determinism closure. Splitting them further would inflate the
harness without adding a new acceptance dimension.

All existing 103 registrations are reused unchanged. In particular, the
following existing groups remain required regression evidence:

    I3D golden, privacy, paired-world, and canonical-byte groups
    all 14 I4A groups
    all 18 I4B groups
    all 18 I4C groups

The exact existing I4 registrations are:

    I4A YESNO exact domain and response values
    I4A YESNO malformed input and ownership
    I4A OPTION source order and public values
    I4A OPTION duplicates and local-key metamorphic identity
    I4A OPTION invalid domains fail closed
    I4A POSITION valid mask order and private responses
    I4A POSITION invalid masks fail closed
    I4A POSITION unbound card code stays absent
    I4A private binding resolves exact response values
    I4A stale same-looking selection is rejected
    I4A invalid key family and domain bindings fail closed
    I4A failed prompts do not publish or advance state
    I4A public values preserve the privacy boundary
    I4A public values own source data

    I4B EFFECTYN exact wire and context
    I4B EFFECTYN domain order and responses
    I4B EFFECTYN malformed wire failures
    I4B EFFECTYN authority validation failures
    I4B EFFECTYN indexed correlation
    I4B EFFECTYN pile and overlay correlation
    I4B EFFECTYN card code safety and ambiguity
    I4B EFFECTYN privacy and staleness
    I4B CHAIN optional wire context and no-chain
    I4B CHAIN forced marker and single entry
    I4B CHAIN optional empty domain
    I4B CHAIN entry order duplicates and values
    I4B CHAIN no-chain authority
    I4B CHAIN malformed wire and enumeration
    I4B CHAIN correlation authority and card code safety
    I4B CHAIN atomicity staleness and ownership
    I4B public private boundary
    I4B I4A and I3 regression boundary

    I4C BATTLE exact wire and context
    I4C BATTLE mixed sections and complete order
    I4C BATTLE response bindings and section ordinals
    I4C BATTLE transition flags and zero-domain
    I4C BATTLE indexed correlation and accepted locator
    I4C BATTLE CardCode safety and ambiguity
    I4C BATTLE malformed wire and enum validation
    I4C BATTLE authority atomicity staleness ownership privacy
    I4C IDLE exact wire and context
    I4C IDLE all sections and canonical order
    I4C IDLE per-section response bindings
    I4C IDLE transition flags and zero-domain
    I4C IDLE indexed and pile correlation
    I4C IDLE CardCode safety and duplicate ambiguity
    I4C IDLE malformed wire and enum validation
    I4C IDLE authority atomicity staleness ownership privacy
    I4C public private boundary
    I4C I3 I4A I4B regression boundary

No existing registration may be renamed, removed, reordered, or merged.

## 10. Future acceptance matrix

The later implementation review must report the following machine-readable
style fields:

    I4_SEVEN_FAMILIES_PRESENT=PASS
    I4_SIMPLE_DISPATCH_NO_EXTRA_FAMILY=PASS
    I4_AUTHORITY_DISPATCH_NO_EXTRA_FAMILY=PASS
    I4_NO_EXTRA_FAMILY=PASS
    I4_CROSS_FAMILY_BINDING_ISOLATION=PASS
    I4_CROSS_FAMILY_STALE_HANDLE_REJECTION=PASS
    I4_FAILURE_ATOMICITY=PASS
    I4_FAILED_PROMPT_NO_ORDINAL_ADVANCE=PASS
    I4_COMPLETE_DOMAIN_PRESERVATION=PASS
    I4_DUPLICATE_PRESERVATION=PASS
    I4_N1_AUTOANSWER=NO
    I4_PUBLIC_PRIVATE_SEAM=PASS
    I4_PUBLICATION_AUTHORITY=PASS
    I4_HIDDEN_CONTINUITY_LEAK=NO
    I4_RESPONSE_BINDING_PRIVATE=PASS
    I4_NETWORK_RESPONSE_SENDING=ABSENT
    I4_LOCAL_KEY_EQUALS_OCGFORGE_PUBLIC_ACTION_KEY=NO
    I4_MODEL_INPUT_AUTHORITY=ABSENT
    I4_CONTINUATION_AUTHORITY=ABSENT
    I4_FRESH_PROCESS_DETERMINISM=PASS
    I3D_REGRESSION=PASS
    I4A_REGRESSION=PASS
    I4B_REGRESSION=PASS
    I4C_REGRESSION=PASS
    GAMEPLAY=108/108
    PROTOCOL=20/20
    CLIENT=17/17
    WARNINGS=0
    ERRORS=0
    DIFF_CHECK=PASS

These fields are future implementation acceptance requirements, not claims
made by this design document.

## 11. Sufficient and insufficient evidence

I4 FINAL requires all of the following after the separately authorized test
implementation:

    five I4D groups pass in the full 108-group Gameplay harness
    all existing 103 I3/I4A/I4B/I4C groups remain green
    Protocol 20/20 and Client 17/17 remain green
    two fresh processes per harness have identical stdout and exit code
    Release builds have zero warnings and zero errors
    exact future scope and clean diff are verified
    independent review accepts the feature head
    PR Hosted CI passes on the exact feature/merge ref
    merge and post-merge main CI are successful

None of these alone establishes I4 FINAL:

    green compilation
    one family passing
    aggregate 103/103 before I4D
    manual inspection only
    fixture existence
    one deterministic run
    PR mergeability
    Hosted CI without I4D-specific acceptance
    no crash
    successful parsing
    N=1 success

I4 FINAL is not declared at the end of this design task. The implementation,
independent review, PR, merge, and post-merge verification remain separate
gates.

## 12. I5 and I6 barriers

I4D does not authorize I5A0. The required sequence remains:

    I4D design final review
        → separate explicit I4D implementation authorization
        → I4D implementation
        → independent review
        → I4D feature final pass
        → PR and Hosted CI
        → merge and post-merge verification
        → I4_FINAL=YES
        → separate I5A0 contract freeze

At every I4D handoff:

    I5_AUTHORIZED=NO
    I5_STARTED=NO
    I6_AUTHORIZED=NO
    I6_STARTED=NO
