using System.Buffers.Binary;
using System.Collections.ObjectModel;
using OCGForge.Ignis.Gameplay;
using OCGForge.Ignis.Model;

namespace OCGForge.Ignis.Gameplay.Tests.Fixtures;

internal enum I6GDeterministicScenarioDriverErrorCodeV1 : byte
{
    None = 0,
    InvalidInput = 1,
    InvalidScript = 2,
    ScenarioMismatch = 3,
    UnexpectedContinuation = 4,
    PromptProjectionRejected = 5,
    DecisionBoundaryRejected = 6,
    UnplannedPromptOrDomain = 7,
    ExpectedCandidateNotUnique = 8,
    SelectionCaptureFailed = 9,
    SelectionApplyFailed = 10,
    MissingContinuationStep = 11,
    DriverAborted = 12
}

internal sealed record I6GDeterministicScenarioScriptStepV1(
    ulong PromptOrdinal,
    FlatPromptFamilyV1 PromptFamily,
    string PublicCandidateDomainDigest,
    string ExpectedI4LocalCandidateKey,
    bool IsContinuation);

internal sealed class I6GDeterministicScenarioScriptV1
{
    internal const string LinkScenarioId =
        "projectignis.tactical-try.cyber-dragon.v1";
    internal const string CounterScenarioId =
        "projectignis.windbot.ai-blackwing.v1";

    private readonly I6GDeterministicScenarioScriptStepV1[] steps;
    private readonly IReadOnlyList<I6GDeterministicScenarioScriptStepV1>
        stepsView;

    private I6GDeterministicScenarioScriptV1(
        string scenarioId,
        I6GDeterministicScenarioScriptStepV1[] steps)
    {
        ScenarioId = scenarioId;
        this.steps = steps.ToArray();
        stepsView = Array.AsReadOnly(this.steps);
    }

    internal string ScenarioId { get; }

    internal IReadOnlyList<I6GDeterministicScenarioScriptStepV1> Steps =>
        stepsView;

    internal static bool TryCreate(
        string? scenarioId,
        IReadOnlyList<I6GDeterministicScenarioScriptStepV1>? steps,
        out I6GDeterministicScenarioScriptV1? script,
        out I6GDeterministicScenarioDriverErrorCodeV1 error)
    {
        script = null;
        error = I6GDeterministicScenarioDriverErrorCodeV1.None;
        if (scenarioId is not (LinkScenarioId or CounterScenarioId) ||
            steps is null ||
            steps.Count == 0)
        {
            error = I6GDeterministicScenarioDriverErrorCodeV1.InvalidScript;
            return false;
        }

        I6GDeterministicScenarioScriptStepV1[] copy = steps.ToArray();
        for (int index = 0; index < copy.Length; index++)
        {
            I6GDeterministicScenarioScriptStepV1? step = copy[index];
            if (step is null ||
                step.PromptOrdinal != (ulong)index ||
                index == 0 && step.IsContinuation ||
                !IsDigest(step.PublicCandidateDomainDigest) ||
                string.IsNullOrWhiteSpace(step.ExpectedI4LocalCandidateKey))
            {
                error = I6GDeterministicScenarioDriverErrorCodeV1.InvalidScript;
                return false;
            }
        }

        script = new I6GDeterministicScenarioScriptV1(
            scenarioId,
            copy);
        return true;
    }

    private static bool IsDigest(string? value) =>
        value is not null &&
        value.Length == 64 &&
        value.All(character => character is
            >= '0' and <= '9' or
            >= 'a' and <= 'f');
}

internal sealed class I6GDeterministicPromptDescriptionResultV1
{
    private I6GDeterministicPromptDescriptionResultV1(
        bool isSuccess,
        I6GDeterministicScenarioDriverErrorCodeV1 errorCode,
        FlatPromptFamilyV1? promptFamily,
        byte? actingPlayer,
        int candidateCount,
        string? publicCandidateDomainDigest,
        IReadOnlyList<FlatPromptChoiceKindV1>? choiceKinds,
        bool completeDomain)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        PromptFamily = promptFamily;
        ActingPlayer = actingPlayer;
        CandidateCount = candidateCount;
        PublicCandidateDomainDigest = publicCandidateDomainDigest;
        ChoiceKinds = choiceKinds;
        CompleteDomain = completeDomain;
    }

    internal bool IsSuccess { get; }

    internal I6GDeterministicScenarioDriverErrorCodeV1 ErrorCode { get; }

    internal FlatPromptFamilyV1? PromptFamily { get; }

    internal byte? ActingPlayer { get; }

    internal int CandidateCount { get; }

    internal string? PublicCandidateDomainDigest { get; }

    internal IReadOnlyList<FlatPromptChoiceKindV1>? ChoiceKinds { get; }

    internal bool CompleteDomain { get; }

    internal static I6GDeterministicPromptDescriptionResultV1 Failure(
        I6GDeterministicScenarioDriverErrorCodeV1 errorCode) =>
        new(false, errorCode, null, null, 0, null, null, false);

    internal static I6GDeterministicPromptDescriptionResultV1 Success(
        FlatPromptFamilyV1 promptFamily,
        byte actingPlayer,
        int candidateCount,
        string publicCandidateDomainDigest,
        IReadOnlyList<FlatPromptChoiceKindV1> choiceKinds) =>
        new(
            true,
            I6GDeterministicScenarioDriverErrorCodeV1.None,
            promptFamily,
            actingPlayer,
            candidateCount,
            publicCandidateDomainDigest,
            Array.AsReadOnly(choiceKinds.ToArray()),
            true);
}

internal sealed class I6GDeterministicScenarioDriverResultV1
{
    private I6GDeterministicScenarioDriverResultV1(
        bool isSuccess,
        I6GDeterministicScenarioDriverErrorCodeV1 errorCode,
        ulong? promptOrdinal,
        FlatPromptFamilyV1? promptFamily,
        byte? actingPlayer,
        int candidateCount,
        string? publicCandidateDomainDigest,
        IReadOnlyList<FlatPromptChoiceKindV1>? choiceKinds,
        bool completeDomain,
        bool domainDigestMatched,
        bool allCandidatesSelectionCaptured,
        bool selectionApplied,
        bool responseReady,
        bool continuationRequired,
        IReadOnlyList<byte>? responseBody,
        FlatPromptProjectionResultV1? nextProjection)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        PromptOrdinal = promptOrdinal;
        PromptFamily = promptFamily;
        ActingPlayer = actingPlayer;
        CandidateCount = candidateCount;
        PublicCandidateDomainDigest = publicCandidateDomainDigest;
        ChoiceKinds = choiceKinds;
        CompleteDomain = completeDomain;
        DomainDigestMatched = domainDigestMatched;
        AllCandidatesSelectionCaptured = allCandidatesSelectionCaptured;
        SelectionApplied = selectionApplied;
        ResponseReady = responseReady;
        ContinuationRequired = continuationRequired;
        ResponseBody = responseBody;
        NextProjection = nextProjection;
    }

    internal bool IsSuccess { get; }

    internal I6GDeterministicScenarioDriverErrorCodeV1 ErrorCode { get; }

    internal ulong? PromptOrdinal { get; }

    internal FlatPromptFamilyV1? PromptFamily { get; }

    internal byte? ActingPlayer { get; }

    internal int CandidateCount { get; }

    internal string? PublicCandidateDomainDigest { get; }

    internal IReadOnlyList<FlatPromptChoiceKindV1>? ChoiceKinds { get; }

    internal bool CompleteDomain { get; }

    internal bool DomainDigestMatched { get; }

    internal bool AllCandidatesSelectionCaptured { get; }

    internal bool SelectionApplied { get; }

    internal bool ResponseReady { get; }

    internal bool ContinuationRequired { get; }

    internal IReadOnlyList<byte>? ResponseBody { get; }

    internal FlatPromptProjectionResultV1? NextProjection { get; }

    internal static I6GDeterministicScenarioDriverResultV1 Failure(
        I6GDeterministicScenarioDriverErrorCodeV1 errorCode,
        I6GDeterministicPromptDescriptionResultV1? description = null,
        bool domainDigestMatched = false,
        bool allCandidatesSelectionCaptured = false,
        bool selectionApplied = false) =>
        new(
            false,
            errorCode,
            null,
            description?.PromptFamily,
            description?.ActingPlayer,
            description?.CandidateCount ?? 0,
            description?.PublicCandidateDomainDigest,
            description?.ChoiceKinds,
            description?.CompleteDomain ?? false,
            domainDigestMatched,
            allCandidatesSelectionCaptured,
            selectionApplied,
            false,
            false,
            null,
            null);

    internal static I6GDeterministicScenarioDriverResultV1 Success(
        I6GDeterministicPromptDescriptionResultV1 description,
        ulong promptOrdinal,
        bool domainDigestMatched,
        bool allCandidatesSelectionCaptured,
        FlatPromptContinuationStepResultV1 applied) =>
        new(
            true,
            I6GDeterministicScenarioDriverErrorCodeV1.None,
            promptOrdinal,
            description.PromptFamily,
            description.ActingPlayer,
            description.CandidateCount,
            description.PublicCandidateDomainDigest,
            description.ChoiceKinds,
            description.CompleteDomain,
            domainDigestMatched,
            allCandidatesSelectionCaptured,
            true,
            applied.IsTerminal,
            !applied.IsTerminal,
            applied.IsTerminal
                ? Array.AsReadOnly(applied.TerminalResponseBody.ToArray())
                : null,
            applied.Projection);

    internal static I6GDeterministicScenarioDriverResultV1 SuccessTerminal(
        I6GDeterministicPromptDescriptionResultV1 description,
        ulong promptOrdinal,
        bool domainDigestMatched,
        bool allCandidatesSelectionCaptured,
        IReadOnlyList<byte> responseBody) =>
        new(
            true,
            I6GDeterministicScenarioDriverErrorCodeV1.None,
            promptOrdinal,
            description.PromptFamily,
            description.ActingPlayer,
            description.CandidateCount,
            description.PublicCandidateDomainDigest,
            description.ChoiceKinds,
            description.CompleteDomain,
            domainDigestMatched,
            allCandidatesSelectionCaptured,
            true,
            true,
            false,
            Array.AsReadOnly(responseBody.ToArray()),
            null);
}

internal sealed class I6GDeterministicEvidenceScenarioDriverV1
{
    private readonly I6GDeterministicScenarioScriptV1 script;
    private readonly FlatPromptSessionV1 session;
    private readonly OcgForgeAcceptedDecisionBoundaryProducerV1 decisionProducer =
        new();
    private int nextStepIndex;
    private bool aborted;
    private ulong? activePromptInstanceOrdinal;
    private int expectedContinuationStep;

    internal I6GDeterministicEvidenceScenarioDriverV1(
        I6GDeterministicScenarioScriptV1 script,
        FlatPromptSessionV1 session)
    {
        this.script = script ?? throw new ArgumentNullException(nameof(script));
        this.session = session ?? throw new ArgumentNullException(nameof(session));
    }

    internal static I6GDeterministicPromptDescriptionResultV1
        TryDescribeAcceptedProjection(
            PerspectiveSafeFrameV1? frame,
            FlatPromptProjectionResultV1? projection)
    {
        OcgForgeAcceptedDecisionBoundaryProducerV1 producer = new();
        return TryDescribeWithProducer(frame, projection, producer);
    }

    internal I6GDeterministicScenarioDriverResultV1 TrySelectPrompt(
        string? scenarioId,
        PerspectiveSafeFrameV1? frame,
        FlatPromptProjectionResultV1? projection) =>
        TryApplyExpectedStep(
            scenarioId,
            frame,
            projection,
            isContinuationCall: false);

    internal I6GDeterministicScenarioDriverResultV1 TrySelectContinuation(
        string? scenarioId,
        PerspectiveSafeFrameV1? frame,
        FlatPromptProjectionResultV1? projection) =>
        TryApplyExpectedStep(
            scenarioId,
            frame,
            projection,
            isContinuationCall: true);

    private I6GDeterministicScenarioDriverResultV1 TryApplyExpectedStep(
        string? scenarioId,
        PerspectiveSafeFrameV1? frame,
        FlatPromptProjectionResultV1? projection,
        bool isContinuationCall)
    {
        if (aborted)
        {
            return I6GDeterministicScenarioDriverResultV1.Failure(
                I6GDeterministicScenarioDriverErrorCodeV1.DriverAborted);
        }

        if (!string.Equals(
                scenarioId,
                script.ScenarioId,
                StringComparison.Ordinal))
        {
            return Abort(
                I6GDeterministicScenarioDriverErrorCodeV1.ScenarioMismatch);
        }

        if (frame is null || projection is null)
        {
            return Abort(
                I6GDeterministicScenarioDriverErrorCodeV1.InvalidInput);
        }

        if (nextStepIndex >= script.Steps.Count)
        {
            return Abort(
                I6GDeterministicScenarioDriverErrorCodeV1.InvalidScript);
        }

        I6GDeterministicScenarioScriptStepV1 step =
            script.Steps[nextStepIndex];
        if (step.IsContinuation != isContinuationCall)
        {
            return Abort(
                I6GDeterministicScenarioDriverErrorCodeV1.UnexpectedContinuation);
        }

        I6GDeterministicPromptDescriptionResultV1 description =
            TryDescribeWithProducer(frame, projection, decisionProducer);
        if (!description.IsSuccess)
        {
            return Abort(description.ErrorCode);
        }

        if (description.PromptFamily != step.PromptFamily ||
            !string.Equals(
                description.PublicCandidateDomainDigest,
                step.PublicCandidateDomainDigest,
                StringComparison.Ordinal))
        {
            return Abort(
                I6GDeterministicScenarioDriverErrorCodeV1.UnplannedPromptOrDomain,
                description);
        }

        if (projection.Candidates is null ||
            projection.Candidates.Count != description.CandidateCount)
        {
            return Abort(
                I6GDeterministicScenarioDriverErrorCodeV1.UnplannedPromptOrDomain,
                description,
                domainDigestMatched: true);
        }

        FlatPromptSelectionHandleV1? domainHandle = null;
        foreach (FlatPublicCandidateDescriptorV1? candidate in projection.Candidates)
        {
            if (candidate is null ||
                !session.TryCaptureSelection(
                    candidate.I4LocalCandidateKey,
                    out FlatPromptSelectionHandleV1? candidateHandle,
                    out _))
            {
                return Abort(
                    I6GDeterministicScenarioDriverErrorCodeV1.SelectionCaptureFailed,
                    description,
                    domainDigestMatched: true);
            }

            domainHandle ??= candidateHandle;
        }

        if (domainHandle is null ||
            !DomainsEqual(domainHandle.OrderedDomain, projection.Candidates))
        {
            return Abort(
                I6GDeterministicScenarioDriverErrorCodeV1.UnplannedPromptOrDomain,
                description,
                domainDigestMatched: true);
        }

        if (isContinuationCall)
        {
            if (!activePromptInstanceOrdinal.HasValue ||
                domainHandle.PromptInstanceOrdinal !=
                    activePromptInstanceOrdinal.Value ||
                domainHandle.ContinuationStep != expectedContinuationStep)
            {
                return Abort(
                    I6GDeterministicScenarioDriverErrorCodeV1.UnplannedPromptOrDomain,
                    description,
                    domainDigestMatched: true,
                    allCandidatesSelectionCaptured: true);
            }
        }
        else if (domainHandle.ContinuationStep != 0 ||
                 activePromptInstanceOrdinal.HasValue &&
                 domainHandle.PromptInstanceOrdinal <=
                     activePromptInstanceOrdinal.Value)
        {
            return Abort(
                I6GDeterministicScenarioDriverErrorCodeV1.UnplannedPromptOrDomain,
                description,
                domainDigestMatched: true,
                allCandidatesSelectionCaptured: true);
        }

        int expectedCandidateCount = projection.Candidates.Count(candidate =>
            candidate is not null &&
            string.Equals(
                candidate.I4LocalCandidateKey,
                step.ExpectedI4LocalCandidateKey,
                StringComparison.Ordinal));
        if (expectedCandidateCount != 1)
        {
            return Abort(
                I6GDeterministicScenarioDriverErrorCodeV1.ExpectedCandidateNotUnique,
                description,
                domainDigestMatched: true,
                allCandidatesSelectionCaptured: true);
        }

        if (!session.TryCaptureSelection(
                step.ExpectedI4LocalCandidateKey,
                out FlatPromptSelectionHandleV1? handle,
                out _)
            || handle is null)
        {
            return Abort(
                I6GDeterministicScenarioDriverErrorCodeV1.SelectionCaptureFailed,
                description,
                domainDigestMatched: true,
                allCandidatesSelectionCaptured: true);
        }

        if (session.TryResolveSelection(
                handle,
                out FlatPromptResponseResolutionV1 atomicResponse,
                out FlatPromptErrorCodeV1 atomicResolutionError))
        {
            activePromptInstanceOrdinal = domainHandle.PromptInstanceOrdinal;
            nextStepIndex = checked(nextStepIndex + 1);
            if (nextStepIndex < script.Steps.Count &&
                script.Steps[nextStepIndex].IsContinuation)
            {
                return Abort(
                    I6GDeterministicScenarioDriverErrorCodeV1.UnexpectedContinuation,
                    description,
                    domainDigestMatched: true,
                    allCandidatesSelectionCaptured: true,
                    selectionApplied: true);
            }

            byte[] responseBody = new byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(
                responseBody,
                atomicResponse.ResponseI32);
            return I6GDeterministicScenarioDriverResultV1.SuccessTerminal(
                description,
                step.PromptOrdinal,
                domainDigestMatched: true,
                allCandidatesSelectionCaptured: true,
                responseBody);
        }

        if (atomicResolutionError !=
            FlatPromptErrorCodeV1.InvalidContinuationAction)
        {
            return Abort(
                I6GDeterministicScenarioDriverErrorCodeV1.SelectionApplyFailed,
                description,
                domainDigestMatched: true,
                allCandidatesSelectionCaptured: true);
        }

        FlatPromptContinuationStepResultV1 applied =
            session.TryApplySelection(handle);
        if (!applied.IsSuccess)
        {
            return Abort(
                I6GDeterministicScenarioDriverErrorCodeV1.SelectionApplyFailed,
                description,
                domainDigestMatched: true,
                allCandidatesSelectionCaptured: true);
        }

        activePromptInstanceOrdinal = domainHandle.PromptInstanceOrdinal;
        expectedContinuationStep = checked(domainHandle.ContinuationStep + 1);
        nextStepIndex = checked(nextStepIndex + 1);
        if (applied.IsTerminal)
        {
            if (applied.TerminalResponseBody.Count == 0 ||
                nextStepIndex < script.Steps.Count &&
                script.Steps[nextStepIndex].IsContinuation)
            {
                return Abort(
                    I6GDeterministicScenarioDriverErrorCodeV1.UnexpectedContinuation,
                    description,
                    domainDigestMatched: true,
                    allCandidatesSelectionCaptured: true,
                    selectionApplied: true);
            }

            return I6GDeterministicScenarioDriverResultV1.Success(
                description,
                step.PromptOrdinal,
                domainDigestMatched: true,
                allCandidatesSelectionCaptured: true,
                applied);
        }

        if (applied.Projection is null ||
            nextStepIndex >= script.Steps.Count ||
            !script.Steps[nextStepIndex].IsContinuation)
        {
            return Abort(
                I6GDeterministicScenarioDriverErrorCodeV1.MissingContinuationStep,
                description,
                domainDigestMatched: true,
                allCandidatesSelectionCaptured: true,
                selectionApplied: true);
        }

        return I6GDeterministicScenarioDriverResultV1.Success(
            description,
            step.PromptOrdinal,
            domainDigestMatched: true,
            allCandidatesSelectionCaptured: true,
            applied);
    }

    private static bool DomainsEqual(
        IReadOnlyList<FlatPublicCandidateDescriptorV1> first,
        IReadOnlyList<FlatPublicCandidateDescriptorV1> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        for (int index = 0; index < first.Count; index++)
        {
            if (first[index] is null ||
                second[index] is null ||
                !first[index].Equals(second[index]))
            {
                return false;
            }
        }

        return true;
    }

    private I6GDeterministicScenarioDriverResultV1 Abort(
        I6GDeterministicScenarioDriverErrorCodeV1 errorCode,
        I6GDeterministicPromptDescriptionResultV1? description = null,
        bool domainDigestMatched = false,
        bool allCandidatesSelectionCaptured = false,
        bool selectionApplied = false)
    {
        aborted = true;
        return I6GDeterministicScenarioDriverResultV1.Failure(
            errorCode,
            description,
            domainDigestMatched,
            allCandidatesSelectionCaptured,
            selectionApplied);
    }

    private static I6GDeterministicPromptDescriptionResultV1
        TryDescribeWithProducer(
            PerspectiveSafeFrameV1? frame,
            FlatPromptProjectionResultV1? projection,
            OcgForgeAcceptedDecisionBoundaryProducerV1 producer)
    {
        if (frame is null || projection is null || producer is null)
        {
            return I6GDeterministicPromptDescriptionResultV1.Failure(
                I6GDeterministicScenarioDriverErrorCodeV1.InvalidInput);
        }

        if (!projection.IsSuccess ||
            projection.Context is null ||
            projection.Candidates is null ||
            projection.Candidates.Count == 0 ||
            projection.Candidates.Any(candidate => candidate is null))
        {
            return I6GDeterministicPromptDescriptionResultV1.Failure(
                I6GDeterministicScenarioDriverErrorCodeV1.PromptProjectionRejected);
        }

        if (!producer.TryAccept(
                frame,
                projection,
                out OcgForgeAcceptedDecisionBoundaryV1? acceptedDecision,
                out _) ||
            acceptedDecision is null)
        {
            return I6GDeterministicPromptDescriptionResultV1.Failure(
                I6GDeterministicScenarioDriverErrorCodeV1.DecisionBoundaryRejected);
        }

        OcgForgePublicDecisionContextResultV1 mapped =
            OcgForgePublicCandidateBridgeV1.TryCreate(acceptedDecision);
        if (!mapped.IsSuccess || mapped.Context is null ||
            mapped.Context.Candidates.Count != projection.Candidates.Count)
        {
            return I6GDeterministicPromptDescriptionResultV1.Failure(
                I6GDeterministicScenarioDriverErrorCodeV1.DecisionBoundaryRejected);
        }

        FlatPromptFamilyV1 family = projection.Context.PromptFamily;
        byte actingPlayer = projection.Context.ActingPlayer;
        FlatPromptChoiceKindV1[] choiceKinds = projection.Candidates
            .Select(candidate => candidate.ChoiceKind)
            .ToArray();
        return I6GDeterministicPromptDescriptionResultV1.Success(
            family,
            actingPlayer,
            projection.Candidates.Count,
            mapped.Context.PublicCandidateDomainDigest,
            choiceKinds);
    }
}
