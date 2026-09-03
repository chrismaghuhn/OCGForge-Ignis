using OCGForge.Ignis.Client;
using OCGForge.Ignis.Protocol;

namespace OCGForge.Ignis.Gameplay;

public sealed class GameplayHandoffConsumerV1 : IAsyncDisposable
{
    private readonly GameplayHandoffLeaseV1 lease;
    private readonly GameplayMessageDecoderV1 decoder = new();
    private readonly SemaphoreSlim lifecycleGate = new(1, 1);
    private readonly byte[] receiveBuffer = new byte[
        ProtocolContractV1.MaxPacketLength +
        ProtocolContractV1.LengthPrefixSize];
    private int receiveCount;
    private int terminal;

    private GameplayHandoffConsumerV1(GameplayHandoffLeaseV1 lease)
    {
        this.lease = lease;
    }

    public static GameplayHandoffAcquireResult TryCreate(
        GameplayHandoffOfferV1 offer)
    {
        ArgumentNullException.ThrowIfNull(offer);
        GameplayHandoffClaimResult claim = offer.TryClaim();
        if (!claim.IsSuccess || claim.Lease is null)
        {
            return GameplayHandoffAcquireResult.Failure(
                GameplayErrorCode.HandoffAlreadyClaimed);
        }

        return GameplayHandoffAcquireResult.Success(
            new GameplayHandoffConsumerV1(claim.Lease));
    }

    public async ValueTask<GameplayPumpResult> PumpAsync(
        CancellationToken cancellationToken)
    {
        await lifecycleGate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            if (Volatile.Read(ref terminal) != 0)
            {
                return GameplayPumpResult.Failure(GameplayErrorCode.InvalidState);
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
                        if (packet.Type != StocPacketType.GameMsg)
                        {
                            return await FailAsync(
                                    GameplayErrorCode.UnsupportedOuterPacket)
                                .ConfigureAwait(false);
                        }

                        if (packet.Payload is not StocGameMessagePayload gameMessage)
                        {
                            return await FailAsync(
                                    GameplayErrorCode.MalformedOuterFrame)
                                .ConfigureAwait(false);
                        }

                        GameplayMessageDecodeResult decoded =
                            decoder.Decode(gameMessage);
                        if (!decoded.IsSuccess)
                        {
                            return await FailAsync(decoded.Error)
                                .ConfigureAwait(false);
                        }

                        GameplayPerspectiveV1 establishedPerspective =
                            decoded.Perspective!;
                        GameplaySessionV1 session = new(
                            lease,
                            establishedPerspective,
                            lease.PublicSession,
                            receiveBuffer.AsSpan(0, receiveCount));
                        Volatile.Write(ref terminal, 1);
                        return GameplayPumpResult.Success(
                            decoded.Message!,
                            establishedPerspective,
                            session);
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
                    readCount = await lease.ReadAsync(
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
                    return await FailAsync(
                            GameplayErrorCode.MalformedOuterFrame)
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
            lifecycleGate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await lifecycleGate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            if (Volatile.Read(ref terminal) == 1)
            {
                return;
            }

            Volatile.Write(ref terminal, 2);
            await lease.CloseOwnedTransportAsync().ConfigureAwait(false);
        }
        finally
        {
            lifecycleGate.Release();
        }
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

    private async ValueTask<GameplayPumpResult> FailAsync(
        GameplayErrorCode error)
    {
        if (Interlocked.Exchange(ref terminal, 2) == 0)
        {
            await lease.CloseOwnedTransportAsync().ConfigureAwait(false);
        }

        return GameplayPumpResult.Failure(error);
    }

    private static GameplayErrorCode MapProtocolError(
        ProtocolErrorCode error) =>
        error switch
        {
            ProtocolErrorCode.UnsupportedPacketType =>
                GameplayErrorCode.UnsupportedOuterPacket,
            ProtocolErrorCode.TruncatedFrame =>
                GameplayErrorCode.TruncatedStream,
            _ => GameplayErrorCode.MalformedOuterFrame
        };
}
