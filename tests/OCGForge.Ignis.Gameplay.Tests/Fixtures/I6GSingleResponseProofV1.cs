using System.Buffers.Binary;
using OCGForge.Ignis.Client;
using OCGForge.Ignis.Protocol;
using OCGForge.Ignis.Gameplay.Tests.Fixtures;

namespace OCGForge.Ignis.Gameplay.Tests.Fixtures;

internal sealed class I6GSingleResponseBindingResultV1
{
    private I6GSingleResponseBindingResultV1(
        bool isSuccess,
        FlatPromptErrorCodeV1 error,
        FlatPromptFamilyV1? promptFamily,
        byte? actingPlayer,
        int candidateCount,
        int toEpMatchCount,
        bool completeDomain,
        bool selectionCapturePassed,
        bool selectionResolutionPassed,
        bool responseResolutionPassed,
        bool isTerminal,
        IReadOnlyList<byte> responseBody)
    {
        IsSuccess = isSuccess;
        Error = error;
        PromptFamily = promptFamily;
        ActingPlayer = actingPlayer;
        CandidateCount = candidateCount;
        ToEpMatchCount = toEpMatchCount;
        CompleteDomain = completeDomain;
        SelectionCapturePassed = selectionCapturePassed;
        SelectionResolutionPassed = selectionResolutionPassed;
        ResponseResolutionPassed = responseResolutionPassed;
        IsTerminal = isTerminal;
        ResponseBody = responseBody.ToArray();
    }

    internal bool IsSuccess { get; }

    internal FlatPromptErrorCodeV1 Error { get; }

    internal FlatPromptFamilyV1? PromptFamily { get; }

    internal byte? ActingPlayer { get; }

    internal int CandidateCount { get; }

    internal int ToEpMatchCount { get; }

    internal bool CompleteDomain { get; }

    internal bool SelectionCapturePassed { get; }

    internal bool SelectionResolutionPassed { get; }

    internal bool ResponseResolutionPassed { get; }

    internal bool IsTerminal { get; }

    internal IReadOnlyList<byte> ResponseBody { get; }

    internal static I6GSingleResponseBindingResultV1 Failure(
        FlatPromptErrorCodeV1 error,
        FlatPromptFamilyV1? promptFamily = null,
        byte? actingPlayer = null,
        int candidateCount = 0,
        int toEpMatchCount = 0,
        bool completeDomain = false,
        bool selectionCapturePassed = false,
        bool selectionResolutionPassed = false,
        bool responseResolutionPassed = false,
        bool isTerminal = false) =>
        new(
            false,
            error,
            promptFamily,
            actingPlayer,
            candidateCount,
            toEpMatchCount,
            completeDomain,
            selectionCapturePassed,
            selectionResolutionPassed,
            responseResolutionPassed,
            isTerminal,
            Array.Empty<byte>());

    internal static I6GSingleResponseBindingResultV1 Success(
        FlatPromptFamilyV1 promptFamily,
        byte actingPlayer,
        int candidateCount,
        int toEpMatchCount,
        IReadOnlyList<byte> responseBody) =>
        new(
            true,
            FlatPromptErrorCodeV1.None,
            promptFamily,
            actingPlayer,
            candidateCount,
            toEpMatchCount,
            true,
            true,
            true,
            true,
            true,
            responseBody);
}

internal enum I6GSingleResponseProofErrorCodeV1 : byte
{
    None = 0,
    HandoffAcquire = 1,
    InitialPump = 2,
    MirrorCreate = 3,
    InitialFrame = 4,
    Read = 5,
    OuterPacket = 6,
    TimeLimit = 7,
    Hint = 8,
    GameplayDecode = 9,
    MirrorApply = 10,
    Frame = 11,
    Prompt = 12,
    ResponseBinding = 13,
    ResponseWrite = 14,
    PostResponseRead = 15,
    PostResponsePacket = 16,
    MessageBudget = 17,
    TimerExpired = 18
}

internal enum I6GSingleResponsePostMessageClassV1 : byte
{
    None = 0,
    Presentation = 1,
    State = 2,
    Prompt = 3,
    Control = 4,
    Unknown = 5
}

internal sealed class I6GSingleResponseProofResultV1
{
    private I6GSingleResponseProofResultV1()
    {
    }

    internal bool IsSuccess { get; private init; }

    internal I6GSingleResponseProofErrorCodeV1 ErrorCode { get; private init; }

    internal GameplayErrorCode? UnderlyingGameplayError { get; private init; }

    internal FlatPromptErrorCodeV1? PromptError { get; private init; }

    internal byte? PromptId { get; private init; }

    internal FlatPromptFamilyV1? PromptFamily { get; private init; }

    internal byte? ActingPlayer { get; private init; }

    internal byte? GameplayPerspectivePlayer { get; private init; }

    internal bool ActingPlayerMatchesPerspective { get; private init; }

    internal bool PublicStateProjectionPassed { get; private init; }

    internal bool PromptProjectionPassed { get; private init; }

    internal int LegalCandidateCount { get; private init; }

    internal int ToEpMatchCount { get; private init; }

    internal bool CompleteDomain { get; private init; }

    internal bool AllCandidatesResponseBound { get; private init; }

    internal bool SelectionCapturePassed { get; private init; }

    internal bool SelectionResolutionPassed { get; private init; }

    internal bool ResponseResolutionPassed { get; private init; }

    internal bool ResponseWritePassed { get; private init; }

    internal int ResponseWriteCount { get; private init; }

    internal bool SecondTransportCreated { get; private init; }

    internal bool SecondTcpOwnerCreated { get; private init; }

    internal bool PostResponseProgress { get; private init; }

    internal StocPacketType? FirstPostResponseOuterType { get; private init; }

    internal byte? FirstPostResponseInnerId { get; private init; }

    internal I6GSingleResponsePostMessageClassV1
        FirstPostResponseMessageClass { get; private init; }

    internal bool TimerExpiredBeforeResponse { get; private init; }

    internal bool TimerGeneratedWin { get; private init; }

    internal int GameplayMessagesProcessed { get; private init; }

    internal int PresentationMessagesConsumed { get; private init; }

    internal static I6GSingleResponseProofResultV1 Failure(
        I6GSingleResponseProofErrorCodeV1 errorCode,
        GameplayErrorCode? underlyingGameplayError = null,
        FlatPromptErrorCodeV1? promptError = null,
        byte? promptId = null,
        FlatPromptFamilyV1? promptFamily = null,
        byte? actingPlayer = null,
        byte? gameplayPerspectivePlayer = null,
        bool actingPlayerMatchesPerspective = false,
        bool publicStateProjectionPassed = false,
        bool promptProjectionPassed = false,
        int legalCandidateCount = 0,
        int toEpMatchCount = 0,
        bool completeDomain = false,
        bool allCandidatesResponseBound = false,
        bool selectionCapturePassed = false,
        bool selectionResolutionPassed = false,
        bool responseResolutionPassed = false,
        bool responseWritePassed = false,
        int responseWriteCount = 0,
        bool postResponseProgress = false,
        StocPacketType? firstPostResponseOuterType = null,
        byte? firstPostResponseInnerId = null,
        I6GSingleResponsePostMessageClassV1 firstPostResponseMessageClass =
            I6GSingleResponsePostMessageClassV1.None,
        bool timerExpiredBeforeResponse = false,
        bool timerGeneratedWin = false,
        int gameplayMessagesProcessed = 0,
        int presentationMessagesConsumed = 0) =>
        new()
        {
            ErrorCode = errorCode,
            UnderlyingGameplayError = underlyingGameplayError,
            PromptError = promptError,
            PromptId = promptId,
            PromptFamily = promptFamily,
            ActingPlayer = actingPlayer,
            GameplayPerspectivePlayer = gameplayPerspectivePlayer,
            ActingPlayerMatchesPerspective = actingPlayerMatchesPerspective,
            PublicStateProjectionPassed = publicStateProjectionPassed,
            PromptProjectionPassed = promptProjectionPassed,
            LegalCandidateCount = legalCandidateCount,
            ToEpMatchCount = toEpMatchCount,
            CompleteDomain = completeDomain,
            AllCandidatesResponseBound = allCandidatesResponseBound,
            SelectionCapturePassed = selectionCapturePassed,
            SelectionResolutionPassed = selectionResolutionPassed,
            ResponseResolutionPassed = responseResolutionPassed,
            ResponseWritePassed = responseWritePassed,
            ResponseWriteCount = responseWriteCount,
            PostResponseProgress = postResponseProgress,
            FirstPostResponseOuterType = firstPostResponseOuterType,
            FirstPostResponseInnerId = firstPostResponseInnerId,
            FirstPostResponseMessageClass = firstPostResponseMessageClass,
            TimerExpiredBeforeResponse = timerExpiredBeforeResponse,
            TimerGeneratedWin = timerGeneratedWin,
            GameplayMessagesProcessed = gameplayMessagesProcessed,
            PresentationMessagesConsumed = presentationMessagesConsumed
        };

    internal static I6GSingleResponseProofResultV1 Success(
        I6GSingleResponseBindingResultV1 binding,
        int responseWriteCount,
        bool postResponseProgress,
        StocPacketType? firstPostResponseOuterType,
        byte? firstPostResponseInnerId,
        I6GSingleResponsePostMessageClassV1 firstPostResponseMessageClass,
        int gameplayMessagesProcessed,
        int presentationMessagesConsumed) =>
        new()
        {
            IsSuccess = true,
            ErrorCode = I6GSingleResponseProofErrorCodeV1.None,
            PromptId = (byte)FlatPromptFamilyV1.MsgSelectIdleCmd,
            PromptFamily = binding.PromptFamily,
            ActingPlayer = binding.ActingPlayer,
            GameplayPerspectivePlayer = binding.ActingPlayer,
            ActingPlayerMatchesPerspective = true,
            PublicStateProjectionPassed = true,
            PromptProjectionPassed = true,
            LegalCandidateCount = binding.CandidateCount,
            ToEpMatchCount = binding.ToEpMatchCount,
            CompleteDomain = binding.CompleteDomain,
            AllCandidatesResponseBound = binding.SelectionCapturePassed &&
                binding.SelectionResolutionPassed,
            SelectionCapturePassed = binding.SelectionCapturePassed,
            SelectionResolutionPassed = binding.SelectionResolutionPassed,
            ResponseResolutionPassed = binding.ResponseResolutionPassed,
            ResponseWritePassed = responseWriteCount == 1,
            ResponseWriteCount = responseWriteCount,
            SecondTransportCreated = false,
            SecondTcpOwnerCreated = false,
            PostResponseProgress = postResponseProgress,
            FirstPostResponseOuterType = firstPostResponseOuterType,
            FirstPostResponseInnerId = firstPostResponseInnerId,
            FirstPostResponseMessageClass = firstPostResponseMessageClass,
            TimerExpiredBeforeResponse = false,
            TimerGeneratedWin = false,
            GameplayMessagesProcessed = gameplayMessagesProcessed,
            PresentationMessagesConsumed = presentationMessagesConsumed
        };
}

internal static class I6GSingleResponseBindingV1
{
    internal static I6GSingleResponseBindingResultV1 TryResolveToEp(
        ReadOnlySpan<byte> completePrompt,
        PerspectiveStateMirrorV1 mirror,
        PublicStateProjectionResultV1 acceptedProjection,
        byte expectedPerspective)
    {
        ArgumentNullException.ThrowIfNull(mirror);
        ArgumentNullException.ThrowIfNull(acceptedProjection);
        if (expectedPerspective > 1)
        {
            return I6GSingleResponseBindingResultV1.Failure(
                FlatPromptErrorCodeV1.InvalidParticipant);
        }

        FlatPromptSessionV1 session = new();
        FlatPromptProjectionResultV1 prompt = session.TryAcceptPrompt(
            completePrompt,
            mirror,
            acceptedProjection);
        if (!prompt.IsSuccess ||
            prompt.Context is null ||
            prompt.Candidates is null)
        {
            return I6GSingleResponseBindingResultV1.Failure(prompt.Error);
        }

        FlatPromptPublicContextV1 context = prompt.Context;
        int candidateCount = prompt.Candidates.Count;
        if (context.PromptFamily != FlatPromptFamilyV1.MsgSelectIdleCmd)
        {
            return I6GSingleResponseBindingResultV1.Failure(
                FlatPromptErrorCodeV1.UnsupportedPromptLayout,
                context.PromptFamily,
                context.ActingPlayer,
                candidateCount,
                completeDomain: true);
        }

        if (context.ActingPlayer != expectedPerspective)
        {
            return I6GSingleResponseBindingResultV1.Failure(
                FlatPromptErrorCodeV1.InvalidParticipant,
                context.PromptFamily,
                context.ActingPlayer,
                candidateCount,
                completeDomain: true);
        }

        FlatPublicCandidateDescriptorV1[] toEpCandidates = prompt.Candidates
            .Where(candidate => candidate.ChoiceKind == FlatPromptChoiceKindV1.ToEp)
            .ToArray();
        if (toEpCandidates.Length != 1)
        {
            return I6GSingleResponseBindingResultV1.Failure(
                FlatPromptErrorCodeV1.UnprovenCandidateDomain,
                context.PromptFamily,
                context.ActingPlayer,
                candidateCount,
                toEpCandidates.Length,
                completeDomain: true);
        }

        foreach (FlatPublicCandidateDescriptorV1 candidate in prompt.Candidates)
        {
            if (!session.TryCaptureSelection(
                    candidate.I4LocalCandidateKey,
                    out FlatPromptSelectionHandleV1? handle,
                    out _) ||
                !session.TryResolveSelection(
                    handle,
                    out _,
                    out _))
            {
                return I6GSingleResponseBindingResultV1.Failure(
                    FlatPromptErrorCodeV1.InvalidResponseBinding,
                    context.PromptFamily,
                    context.ActingPlayer,
                    candidateCount,
                    toEpCandidates.Length,
                    completeDomain: true,
                    selectionCapturePassed: false,
                    selectionResolutionPassed: false);
            }
        }

        FlatPublicCandidateDescriptorV1 toEp = toEpCandidates[0];
        if (!session.TryCaptureSelection(
                toEp.I4LocalCandidateKey,
                out FlatPromptSelectionHandleV1? selection,
                out FlatPromptErrorCodeV1 captureError))
        {
            return I6GSingleResponseBindingResultV1.Failure(
                captureError,
                context.PromptFamily,
                context.ActingPlayer,
                candidateCount,
                toEpCandidates.Length,
                completeDomain: true,
                selectionCapturePassed: false,
                selectionResolutionPassed: true);
        }

        if (!session.TryResolveSelection(
                selection,
                out _,
                out FlatPromptErrorCodeV1 resolutionError))
        {
            return I6GSingleResponseBindingResultV1.Failure(
                resolutionError,
                context.PromptFamily,
                context.ActingPlayer,
                candidateCount,
                toEpCandidates.Length,
                completeDomain: true,
                selectionCapturePassed: true,
                selectionResolutionPassed: false);
        }

        if (!session.TryResolveSelection(
                selection,
                out FlatPromptResponseResolutionV1 resolved,
                out FlatPromptErrorCodeV1 finalResolutionError))
        {
            return I6GSingleResponseBindingResultV1.Failure(
                finalResolutionError,
                context.PromptFamily,
                context.ActingPlayer,
                candidateCount,
                toEpCandidates.Length,
                completeDomain: true,
                selectionCapturePassed: true,
                selectionResolutionPassed: false);
        }

        byte[] responseBody = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(
            responseBody,
            resolved.ResponseI32);

        return I6GSingleResponseBindingResultV1.Success(
            context.PromptFamily,
            context.ActingPlayer,
            candidateCount,
            toEpCandidates.Length,
            responseBody);
    }
}

internal static class I6GSingleResponseProofV1
{
    private const byte MsgHint = 2;
    private const int MsgHintLength = 11;

    internal static async ValueTask<I6GSingleResponseProofResultV1> ExecuteAsync(
        GameplayHandoffOfferV1 handoff,
        I6C6TcpCaptureTransportV1 captureTransport,
        PerspectiveSafeMatchContextV1 matchContext,
        PerspectiveSafePrintedProviderV1 printedProvider,
        int maximumAdditionalPackets,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handoff);
        ArgumentNullException.ThrowIfNull(captureTransport);
        ArgumentNullException.ThrowIfNull(matchContext);
        ArgumentNullException.ThrowIfNull(printedProvider);
        if (maximumAdditionalPackets < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumAdditionalPackets));
        }

        GameplayHandoffAcquireResult acquired =
            GameplayHandoffConsumerV1.TryCreate(handoff);
        if (!acquired.IsSuccess || acquired.Consumer is null)
        {
            return I6GSingleResponseProofResultV1.Failure(
                I6GSingleResponseProofErrorCodeV1.HandoffAcquire,
                GameplayErrorCode.InvalidHandoff);
        }

        await using GameplayHandoffConsumerV1 consumer = acquired.Consumer;
        GameplayPumpResult first = await consumer.PumpAsync(cancellationToken)
            .ConfigureAwait(false);
        if (!first.IsSuccess ||
            first.Message is null ||
            first.Perspective is null ||
            first.Session is null)
        {
            return I6GSingleResponseProofResultV1.Failure(
                I6GSingleResponseProofErrorCodeV1.InitialPump,
                first.Error);
        }

        if (matchContext.PerspectivePlayer != first.Perspective.PlayerType)
        {
            return I6GSingleResponseProofResultV1.Failure(
                I6GSingleResponseProofErrorCodeV1.Prompt,
                GameplayErrorCode.InvalidParticipant,
                gameplayPerspectivePlayer: first.Perspective.PlayerType);
        }

        MirrorCreateResult created = PerspectiveStateMirrorV1.TryCreate(
            first.Message,
            first.Perspective);
        if (!created.IsSuccess || created.Mirror is null)
        {
            return I6GSingleResponseProofResultV1.Failure(
                I6GSingleResponseProofErrorCodeV1.MirrorCreate,
                created.Error);
        }

        await using GameplaySessionV1 gameplaySession = first.Session;
        StocPacketReader reader = new(gameplaySession);
        GameplayMessageDecoderV1 decoder =
            new(first.Perspective);
        PerspectiveSafeFrameSourceResultV1 initialFrame =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C5(
                created.Mirror,
                matchContext,
                printedProvider);
        bool frameReady = initialFrame.IsSuccess &&
            initialFrame.Frame is not null;
        if (!frameReady && !IsProvisionalFrameReadinessFailure(initialFrame))
        {
            return I6GSingleResponseProofResultV1.Failure(
                I6GSingleResponseProofErrorCodeV1.InitialFrame,
                GameplayErrorCode.InvalidState);
        }

        int gameplayMessagesProcessed = 0;
        int presentationMessagesConsumed = 0;
        for (int packetOrdinal = 0;
             packetOrdinal < maximumAdditionalPackets;
             packetOrdinal++)
        {
            I6GStocReadPacketResult packetResult =
                await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            if (!packetResult.IsSuccess || packetResult.Packet is null)
            {
                return I6GSingleResponseProofResultV1.Failure(
                    I6GSingleResponseProofErrorCodeV1.Read,
                    packetResult.Error,
                    gameplayMessagesProcessed: gameplayMessagesProcessed,
                    presentationMessagesConsumed: presentationMessagesConsumed);
            }

            ValidatedStocPacket packet = packetResult.Packet;
            if (packet.Type == StocPacketType.TimeLimit)
            {
                if (packet.Payload is not StocTimeLimitPayload timeLimit)
                {
                    return I6GSingleResponseProofResultV1.Failure(
                        I6GSingleResponseProofErrorCodeV1.TimeLimit,
                        GameplayErrorCode.MalformedOuterFrame,
                        gameplayMessagesProcessed: gameplayMessagesProcessed,
                        presentationMessagesConsumed: presentationMessagesConsumed);
                }

                if (timeLimit.Player > 1)
                {
                    return I6GSingleResponseProofResultV1.Failure(
                        I6GSingleResponseProofErrorCodeV1.TimeLimit,
                        GameplayErrorCode.InvalidParticipant,
                        gameplayMessagesProcessed: gameplayMessagesProcessed,
                        presentationMessagesConsumed: presentationMessagesConsumed);
                }

                presentationMessagesConsumed++;
                continue;
            }

            if (packet.Type != StocPacketType.GameMsg ||
                packet.Payload is not StocGameMessagePayload gameMessage ||
                gameMessage.Bytes.IsEmpty)
            {
                return I6GSingleResponseProofResultV1.Failure(
                    I6GSingleResponseProofErrorCodeV1.OuterPacket,
                    GameplayErrorCode.UnsupportedOuterPacket,
                    gameplayMessagesProcessed: gameplayMessagesProcessed,
                    presentationMessagesConsumed: presentationMessagesConsumed);
            }

            byte[] messageBytes = gameMessage.Bytes.ToArray();
            if (messageBytes[0] == MsgHint)
            {
                if (messageBytes.Length != MsgHintLength ||
                    messageBytes[2] > 1)
                {
                    return I6GSingleResponseProofResultV1.Failure(
                        I6GSingleResponseProofErrorCodeV1.Hint,
                        GameplayErrorCode.MalformedGameMessage,
                        gameplayMessagesProcessed: gameplayMessagesProcessed,
                        presentationMessagesConsumed: presentationMessagesConsumed);
                }

                presentationMessagesConsumed++;
                continue;
            }

            gameplayMessagesProcessed++;
            if (messageBytes[0] == (byte)FlatPromptFamilyV1.MsgSelectIdleCmd)
            {
                if (!frameReady)
                {
                    return I6GSingleResponseProofResultV1.Failure(
                        I6GSingleResponseProofErrorCodeV1.Frame,
                        GameplayErrorCode.InvalidState,
                        promptId: messageBytes[0],
                        gameplayMessagesProcessed: gameplayMessagesProcessed,
                        presentationMessagesConsumed: presentationMessagesConsumed);
                }

                PublicStateProjectionResultV1 projection =
                    PublicStateProjectionV1.TryProject(
                        created.Mirror.Snapshot,
                        new PublicStateProjectionContextV1(
                            matchContext.DuelFlags));
                if (!projection.IsSuccess || projection.Snapshot is null)
                {
                    return I6GSingleResponseProofResultV1.Failure(
                        I6GSingleResponseProofErrorCodeV1.Prompt,
                        GameplayErrorCode.InvalidState,
                        promptId: messageBytes[0],
                        publicStateProjectionPassed: false,
                        gameplayMessagesProcessed: gameplayMessagesProcessed,
                        presentationMessagesConsumed: presentationMessagesConsumed);
                }

                I6GSingleResponseBindingResultV1 binding =
                    I6GSingleResponseBindingV1.TryResolveToEp(
                        messageBytes,
                        created.Mirror,
                        projection,
                        first.Perspective.PlayerType);
                if (!binding.IsSuccess)
                {
                    return I6GSingleResponseProofResultV1.Failure(
                        I6GSingleResponseProofErrorCodeV1.ResponseBinding,
                        promptError: binding.Error,
                        promptId: messageBytes[0],
                        promptFamily: binding.PromptFamily,
                        actingPlayer: binding.ActingPlayer,
                        actingPlayerMatchesPerspective:
                            binding.ActingPlayer == first.Perspective.PlayerType,
                        publicStateProjectionPassed: true,
                        promptProjectionPassed: binding.PromptFamily is not null,
                        legalCandidateCount: binding.CandidateCount,
                        toEpMatchCount: binding.ToEpMatchCount,
                        completeDomain: binding.CompleteDomain,
                        selectionCapturePassed: binding.SelectionCapturePassed,
                        selectionResolutionPassed: binding.SelectionResolutionPassed,
                        responseResolutionPassed: binding.ResponseResolutionPassed,
                        gameplayMessagesProcessed: gameplayMessagesProcessed,
                        presentationMessagesConsumed: presentationMessagesConsumed);
                }

                int responseWritesBefore =
                    captureTransport.CtosResponseWriteCount;
                byte[] responsePayload =
                    PacketPayloadCodec.EncodeCtosResponse(
                        new CtosResponsePayload(binding.ResponseBody.ToArray()));
                byte[] responseFrame = WireFrameCodec.EncodeCtos(
                    CtosPacketType.Response,
                    responsePayload);
                try
                {
                    await captureTransport.WriteAsync(
                            responseFrame,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    return I6GSingleResponseProofResultV1.Failure(
                        I6GSingleResponseProofErrorCodeV1.ResponseWrite,
                        GameplayErrorCode.Cancelled,
                        promptId: messageBytes[0],
                        promptFamily: binding.PromptFamily,
                        actingPlayer: binding.ActingPlayer,
                        actingPlayerMatchesPerspective: true,
                        publicStateProjectionPassed: true,
                        promptProjectionPassed: true,
                        legalCandidateCount: binding.CandidateCount,
                        toEpMatchCount: binding.ToEpMatchCount,
                        completeDomain: true,
                        allCandidatesResponseBound: true,
                        selectionCapturePassed: true,
                        selectionResolutionPassed: true,
                        responseResolutionPassed: true,
                        responseWriteCount:
                            captureTransport.CtosResponseWriteCount -
                            responseWritesBefore,
                        gameplayMessagesProcessed: gameplayMessagesProcessed,
                        presentationMessagesConsumed: presentationMessagesConsumed);
                }
                catch
                {
                    return I6GSingleResponseProofResultV1.Failure(
                        I6GSingleResponseProofErrorCodeV1.ResponseWrite,
                        GameplayErrorCode.TransportReadFailed,
                        promptId: messageBytes[0],
                        promptFamily: binding.PromptFamily,
                        actingPlayer: binding.ActingPlayer,
                        actingPlayerMatchesPerspective: true,
                        publicStateProjectionPassed: true,
                        promptProjectionPassed: true,
                        legalCandidateCount: binding.CandidateCount,
                        toEpMatchCount: binding.ToEpMatchCount,
                        completeDomain: true,
                        allCandidatesResponseBound: true,
                        selectionCapturePassed: true,
                        selectionResolutionPassed: true,
                        responseResolutionPassed: true,
                        responseWriteCount:
                            captureTransport.CtosResponseWriteCount -
                            responseWritesBefore,
                        gameplayMessagesProcessed: gameplayMessagesProcessed,
                        presentationMessagesConsumed: presentationMessagesConsumed);
                }

                int responseWriteCount =
                    captureTransport.CtosResponseWriteCount -
                    responseWritesBefore;
                if (responseWriteCount != 1 ||
                    captureTransport.CtosResponseWriteCount != 1)
                {
                    return I6GSingleResponseProofResultV1.Failure(
                        I6GSingleResponseProofErrorCodeV1.ResponseWrite,
                        promptId: messageBytes[0],
                        promptFamily: binding.PromptFamily,
                        actingPlayer: binding.ActingPlayer,
                        actingPlayerMatchesPerspective: true,
                        publicStateProjectionPassed: true,
                        promptProjectionPassed: true,
                        legalCandidateCount: binding.CandidateCount,
                        toEpMatchCount: binding.ToEpMatchCount,
                        completeDomain: true,
                        allCandidatesResponseBound: true,
                        selectionCapturePassed: true,
                        selectionResolutionPassed: true,
                        responseResolutionPassed: true,
                        responseWritePassed: false,
                        responseWriteCount: captureTransport.CtosResponseWriteCount,
                        gameplayMessagesProcessed: gameplayMessagesProcessed,
                        presentationMessagesConsumed: presentationMessagesConsumed);
                }

                I6GStocReadPacketResult postResponse =
                    await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                if (!postResponse.IsSuccess || postResponse.Packet is null)
                {
                    return I6GSingleResponseProofResultV1.Failure(
                        I6GSingleResponseProofErrorCodeV1.PostResponseRead,
                        postResponse.Error,
                        promptId: messageBytes[0],
                        promptFamily: binding.PromptFamily,
                        actingPlayer: binding.ActingPlayer,
                        actingPlayerMatchesPerspective: true,
                        publicStateProjectionPassed: true,
                        promptProjectionPassed: true,
                        legalCandidateCount: binding.CandidateCount,
                        toEpMatchCount: binding.ToEpMatchCount,
                        completeDomain: true,
                        allCandidatesResponseBound: true,
                        selectionCapturePassed: true,
                        selectionResolutionPassed: true,
                        responseResolutionPassed: true,
                        responseWritePassed: true,
                        responseWriteCount: responseWriteCount,
                        gameplayMessagesProcessed: gameplayMessagesProcessed,
                        presentationMessagesConsumed: presentationMessagesConsumed);
                }

                I6GPostResponsePacketClassificationV1 classification =
                    ClassifyPostResponsePacket(
                        postResponse.Packet,
                        decoder);
                if (!classification.IsValid)
                {
                    return I6GSingleResponseProofResultV1.Failure(
                        I6GSingleResponseProofErrorCodeV1.PostResponsePacket,
                        classification.Error,
                        promptId: messageBytes[0],
                        promptFamily: binding.PromptFamily,
                        actingPlayer: binding.ActingPlayer,
                        actingPlayerMatchesPerspective: true,
                        publicStateProjectionPassed: true,
                        promptProjectionPassed: true,
                        legalCandidateCount: binding.CandidateCount,
                        toEpMatchCount: binding.ToEpMatchCount,
                        completeDomain: true,
                        allCandidatesResponseBound: true,
                        selectionCapturePassed: true,
                        selectionResolutionPassed: true,
                        responseResolutionPassed: true,
                        responseWritePassed: true,
                        responseWriteCount: responseWriteCount,
                        firstPostResponseOuterType:
                            postResponse.Packet.Type,
                        firstPostResponseInnerId:
                            classification.InnerMessageId,
                        firstPostResponseMessageClass:
                            classification.MessageClass,
                        gameplayMessagesProcessed: gameplayMessagesProcessed,
                        presentationMessagesConsumed: presentationMessagesConsumed);
                }

                return I6GSingleResponseProofResultV1.Success(
                    binding,
                    responseWriteCount,
                    classification.Progress,
                    postResponse.Packet.Type,
                    classification.InnerMessageId,
                    classification.MessageClass,
                    gameplayMessagesProcessed,
                    presentationMessagesConsumed);
            }

            GameplayMessageDecodeResult decoded = decoder.Decode(gameMessage);
            if (!decoded.IsSuccess || decoded.Message is null)
            {
                return I6GSingleResponseProofResultV1.Failure(
                    I6GSingleResponseProofErrorCodeV1.GameplayDecode,
                    decoded.Error,
                    gameplayMessagesProcessed: gameplayMessagesProcessed,
                    presentationMessagesConsumed: presentationMessagesConsumed);
            }

            MirrorApplyResult applied = created.Mirror.Apply(decoded.Message);
            if (!applied.IsSuccess)
            {
                return I6GSingleResponseProofResultV1.Failure(
                    I6GSingleResponseProofErrorCodeV1.MirrorApply,
                    applied.Error,
                    gameplayMessagesProcessed: gameplayMessagesProcessed,
                    presentationMessagesConsumed: presentationMessagesConsumed);
            }

            PerspectiveSafeFrameSourceResultV1 frame =
                PerspectiveSafePublicFrameSourceV1.TryCreateI6C5(
                    created.Mirror,
                    matchContext,
                    printedProvider);
            if (!frameReady)
            {
                if (frame.IsSuccess && frame.Frame is not null)
                {
                    frameReady = true;
                    continue;
                }

                if (IsProvisionalFrameReadinessFailure(frame))
                {
                    continue;
                }

                return I6GSingleResponseProofResultV1.Failure(
                    I6GSingleResponseProofErrorCodeV1.Frame,
                    GameplayErrorCode.InvalidState,
                    gameplayMessagesProcessed: gameplayMessagesProcessed,
                    presentationMessagesConsumed: presentationMessagesConsumed);
            }

            if (!frame.IsSuccess || frame.Frame is null)
            {
                return I6GSingleResponseProofResultV1.Failure(
                    I6GSingleResponseProofErrorCodeV1.Frame,
                    GameplayErrorCode.InvalidState,
                    gameplayMessagesProcessed: gameplayMessagesProcessed,
                    presentationMessagesConsumed: presentationMessagesConsumed);
            }
        }

        return I6GSingleResponseProofResultV1.Failure(
            I6GSingleResponseProofErrorCodeV1.MessageBudget,
            gameplayMessagesProcessed: gameplayMessagesProcessed,
            presentationMessagesConsumed: presentationMessagesConsumed);
    }

    private static bool IsProvisionalFrameReadinessFailure(
        PerspectiveSafeFrameSourceResultV1 result) =>
        !result.IsSuccess &&
        result.Error is
        {
            Code: PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
            Section: PerspectiveSafeSourceSectionV1.Entities
        };

    private static I6GPostResponsePacketClassificationV1
        ClassifyPostResponsePacket(
            ValidatedStocPacket packet,
            GameplayMessageDecoderV1 decoder)
    {
        if (packet.Type == StocPacketType.TimeLimit)
        {
            return packet.Payload is StocTimeLimitPayload timeLimit &&
                timeLimit.Player <= 1
                ? new(
                    true,
                    true,
                    null,
                    I6GSingleResponsePostMessageClassV1.Presentation,
                    GameplayErrorCode.None)
                : new(
                    false,
                    false,
                    null,
                    I6GSingleResponsePostMessageClassV1.Unknown,
                    GameplayErrorCode.InvalidParticipant);
        }

        if (packet.Type == StocPacketType.GameMsg &&
            packet.Payload is StocGameMessagePayload gameMessage &&
            !gameMessage.Bytes.IsEmpty)
        {
            ReadOnlySpan<byte> bytes = gameMessage.Bytes.Span;
            if (bytes[0] == MsgHint)
            {
                bool valid = bytes.Length == MsgHintLength && bytes[2] <= 1;
                return new(
                    valid,
                    valid,
                    bytes[0],
                    I6GSingleResponsePostMessageClassV1.Presentation,
                    valid
                        ? GameplayErrorCode.None
                        : GameplayErrorCode.MalformedGameMessage);
            }

            if (FlatPromptProjectionV1.TryParseWireDraft(
                    bytes,
                    out _,
                    out _))
            {
                return new(
                    true,
                    true,
                    bytes[0],
                    I6GSingleResponsePostMessageClassV1.Prompt,
                    GameplayErrorCode.None);
            }

            GameplayMessageDecodeResult decoded = decoder.Decode(gameMessage);
            return decoded.IsSuccess
                ? new(
                    true,
                    true,
                    bytes[0],
                    I6GSingleResponsePostMessageClassV1.State,
                    GameplayErrorCode.None)
                : new(
                    false,
                    false,
                    bytes[0],
                    I6GSingleResponsePostMessageClassV1.Unknown,
                    decoded.Error);
        }

        bool progress = packet.Type is not
            (StocPacketType.ErrorMsg or
             StocPacketType.LeaveGame or
             StocPacketType.DuelEnd);
        return new(
            progress,
            progress,
            null,
            progress
                ? I6GSingleResponsePostMessageClassV1.Control
                : I6GSingleResponsePostMessageClassV1.Unknown,
            progress
                ? GameplayErrorCode.None
                : GameplayErrorCode.InvalidState);
    }

    private readonly record struct I6GPostResponsePacketClassificationV1(
        bool IsValid,
        bool Progress,
        byte? InnerMessageId,
        I6GSingleResponsePostMessageClassV1 MessageClass,
        GameplayErrorCode Error);

    private readonly record struct I6GStocReadPacketResult(
        bool IsSuccess,
        GameplayErrorCode Error,
        ValidatedStocPacket? Packet);

    private sealed class StocPacketReader
    {
        private readonly GameplaySessionV1 session;
        private readonly byte[] receiveBuffer = new byte[
            ProtocolContractV1.MaxPacketLength +
            ProtocolContractV1.LengthPrefixSize];
        private int receiveCount;

        internal StocPacketReader(GameplaySessionV1 session)
        {
            this.session = session ??
                throw new ArgumentNullException(nameof(session));
        }

        internal async ValueTask<I6GStocReadPacketResult> ReadAsync(
            CancellationToken cancellationToken)
        {
            while (true)
            {
                if (receiveCount > 0)
                {
                    FrameReadResult<ValidatedStocPacket> parsed =
                        PacketPayloadValidator.TryReadValidatedStoc(
                            receiveBuffer.AsSpan(0, receiveCount));
                    if (parsed.Status == FrameReadStatus.Invalid)
                    {
                        return new(
                            false,
                            MapProtocolError(parsed.Error),
                            null);
                    }

                    if (parsed.Status == FrameReadStatus.Success)
                    {
                        if (parsed.Frame is null || parsed.ConsumedBytes <= 0)
                        {
                            return new(
                                false,
                                GameplayErrorCode.MalformedOuterFrame,
                                null);
                        }

                        ValidatedStocPacket packet = parsed.Frame;
                        Consume(parsed.ConsumedBytes);
                        return new(
                            true,
                            GameplayErrorCode.None,
                            packet);
                    }
                }

                int available = receiveBuffer.Length - receiveCount;
                if (available == 0)
                {
                    return new(
                        false,
                        GameplayErrorCode.MalformedOuterFrame,
                        null);
                }

                int readCount;
                try
                {
                    readCount = await session.ReadAsync(
                            receiveBuffer.AsMemory(receiveCount, available),
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    return new(false, GameplayErrorCode.Cancelled, null);
                }
                catch
                {
                    return new(false, GameplayErrorCode.TransportReadFailed, null);
                }

                if (readCount < 0 || readCount > available)
                {
                    return new(false, GameplayErrorCode.MalformedOuterFrame, null);
                }

                if (readCount == 0)
                {
                    return new(
                        false,
                        receiveCount == 0
                            ? GameplayErrorCode.RemoteClosed
                            : GameplayErrorCode.TruncatedStream,
                        null);
                }

                receiveCount += readCount;
            }
        }

        private void Consume(int count)
        {
            int remaining = receiveCount - count;
            if (remaining > 0)
            {
                Buffer.BlockCopy(
                    receiveBuffer,
                    count,
                    receiveBuffer,
                    0,
                    remaining);
            }

            receiveCount = remaining;
        }

        private static GameplayErrorCode MapProtocolError(
            ProtocolErrorCode error) =>
            error switch
            {
                ProtocolErrorCode.UnsupportedPacketType or
                ProtocolErrorCode.UnknownPacketType =>
                    GameplayErrorCode.UnsupportedOuterPacket,
                ProtocolErrorCode.TruncatedFrame =>
                    GameplayErrorCode.TruncatedStream,
                _ => GameplayErrorCode.MalformedOuterFrame
            };
    }
}
