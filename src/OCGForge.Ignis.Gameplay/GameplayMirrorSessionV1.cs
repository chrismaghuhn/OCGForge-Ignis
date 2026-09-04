using OCGForge.Ignis.Protocol;

namespace OCGForge.Ignis.Gameplay;

public sealed class GameplayMirrorPumpResult
{
    private GameplayMirrorPumpResult(
        bool isSuccess,
        GameplayErrorCode error,
        GameplayMessageV1? message,
        MirrorSnapshotV1 snapshot)
    {
        IsSuccess = isSuccess;
        Error = error;
        Message = message;
        Snapshot = snapshot;
    }

    public bool IsSuccess { get; }

    public GameplayErrorCode Error { get; }

    public GameplayMessageV1? Message { get; }

    public MirrorSnapshotV1 Snapshot { get; }

    internal static GameplayMirrorPumpResult Success(
        GameplayMessageV1 message,
        MirrorSnapshotV1 snapshot) =>
        new(true, GameplayErrorCode.None, message, snapshot);

    internal static GameplayMirrorPumpResult Failure(
        GameplayErrorCode error,
        MirrorSnapshotV1 snapshot) =>
        new(false, error, null, snapshot);
}

public sealed class GameplayMirrorSessionV1 : IAsyncDisposable
{
    private readonly GameplaySessionV1 transportSession;
    private readonly PerspectiveStateMirrorV1 mirror;
    private readonly GameplayMessageDecoderV1 decoder;
    private readonly SemaphoreSlim operationGate = new(1, 1);
    private readonly byte[] receiveBuffer = new byte[
        ProtocolContractV1.MaxPacketLength +
        ProtocolContractV1.LengthPrefixSize];
    private int receiveCount;
    private int terminal;

    public GameplayMirrorSessionV1(
        GameplaySessionV1 transportSession,
        PerspectiveStateMirrorV1 mirror)
    {
        this.transportSession = transportSession ??
            throw new ArgumentNullException(nameof(transportSession));
        this.mirror = mirror ?? throw new ArgumentNullException(nameof(mirror));
        if (!transportSession.Perspective.Equals(mirror.Snapshot.Perspective))
        {
            throw new ArgumentException(
                "The gameplay transport and mirror perspectives must match.",
                nameof(mirror));
        }

        decoder = new GameplayMessageDecoderV1(transportSession.Perspective);
    }

    public PerspectiveStateMirrorV1 Mirror => mirror;

    public async ValueTask<GameplayMirrorPumpResult> PumpAsync(
        CancellationToken cancellationToken)
    {
        await operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (Volatile.Read(ref terminal) != 0)
            {
                return GameplayMirrorPumpResult.Failure(
                    GameplayErrorCode.InvalidState,
                    mirror.Snapshot);
            }

            while (true)
            {
                if (receiveCount > 0)
                {
                    FrameReadResult<ValidatedStocPacket> parsed =
                        PacketPayloadValidator.TryReadValidatedStoc(
                            receiveBuffer.AsSpan(0, receiveCount));
                    if (parsed.Status == FrameReadStatus.Invalid)
                    {
                        return await FailAsync(MapProtocolError(parsed.Error))
                            .ConfigureAwait(false);
                    }

                    if (parsed.Status == FrameReadStatus.Success)
                    {
                        if (parsed.Frame is null || parsed.ConsumedBytes <= 0)
                        {
                            return await FailAsync(
                                    GameplayErrorCode.MalformedOuterFrame)
                                .ConfigureAwait(false);
                        }

                        ValidatedStocPacket packet = parsed.Frame;
                        Consume(parsed.ConsumedBytes);
                        if (packet.Type != StocPacketType.GameMsg ||
                            packet.Payload is not StocGameMessagePayload gameMessage)
                        {
                            return await FailAsync(
                                    GameplayErrorCode.UnsupportedOuterPacket)
                                .ConfigureAwait(false);
                        }

                        GameplayMessageDecodeResult decoded = decoder.Decode(gameMessage);
                        if (!decoded.IsSuccess || decoded.Message is null)
                        {
                            return await FailAsync(decoded.Error)
                                .ConfigureAwait(false);
                        }

                        MirrorApplyResult applied = mirror.Apply(decoded.Message);
                        if (!applied.IsSuccess)
                        {
                            return await FailAsync(applied.Error)
                                .ConfigureAwait(false);
                        }

                        return GameplayMirrorPumpResult.Success(
                            decoded.Message,
                            applied.Snapshot);
                    }
                }

                int available = receiveBuffer.Length - receiveCount;
                if (available == 0)
                {
                    return await FailAsync(
                            GameplayErrorCode.MalformedOuterFrame)
                        .ConfigureAwait(false);
                }

                int readCount;
                try
                {
                    readCount = await transportSession.ReadAsync(
                            receiveBuffer.AsMemory(receiveCount, available),
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    return await FailAsync(GameplayErrorCode.Cancelled)
                        .ConfigureAwait(false);
                }
                catch
                {
                    return await FailAsync(GameplayErrorCode.TransportReadFailed)
                        .ConfigureAwait(false);
                }

                if (readCount < 0 || readCount > available)
                {
                    return await FailAsync(GameplayErrorCode.MalformedOuterFrame)
                        .ConfigureAwait(false);
                }

                if (readCount == 0)
                {
                    return await FailAsync(
                            receiveCount == 0
                                ? GameplayErrorCode.RemoteClosed
                                : GameplayErrorCode.TruncatedStream)
                        .ConfigureAwait(false);
                }

                receiveCount += readCount;
            }
        }
        finally
        {
            operationGate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await operationGate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            if (Interlocked.Exchange(ref terminal, 2) == 0)
            {
                await transportSession.DisposeAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            operationGate.Release();
        }
    }

    private async ValueTask<GameplayMirrorPumpResult> FailAsync(
        GameplayErrorCode error)
    {
        if (Interlocked.Exchange(ref terminal, 2) == 0)
        {
            await transportSession.DisposeAsync().ConfigureAwait(false);
        }

        return GameplayMirrorPumpResult.Failure(error, mirror.Snapshot);
    }

    private void Consume(int count)
    {
        int remaining = receiveCount - count;
        if (remaining > 0)
        {
            Buffer.BlockCopy(receiveBuffer, count, receiveBuffer, 0, remaining);
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
