using System.Buffers.Binary;
using System.Reflection;
using OCGForge.Ignis.Client;
using OCGForge.Ignis.Gameplay;
using OCGForge.Ignis.Protocol;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;
using static OCGForge.Ignis.Gameplay.Tests.GameplayMessageFixtures;
using static OCGForge.Ignis.Gameplay.Tests.ModernQueryFixtures;
using static OCGForge.Ignis.Gameplay.Tests.MirrorFixtures;
using static OCGForge.Ignis.Gameplay.Tests.TransportFixtures;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class TransportFixtures
{
    internal static string RunChunking(byte[][] chunks, out TestTransport transport)
    {
        transport = new TestTransport(chunks);
        GameplayHandoffAcquireResult acquired = GameplayHandoffConsumerV1.TryCreate(
            CreateHandoff(transport, Array.Empty<byte>()));
        True(acquired.IsSuccess);
        GameplayPumpResult result = acquired.Consumer!.PumpAsync(
            CancellationToken.None).GetAwaiter().GetResult();
        True(result.IsSuccess);
        string semantic = $"{result.Perspective!.Kind}|{result.Message!.Start.PlayerType}|" +
            $"{result.Message.Start.LifePoints0}|{result.Message.Start.LifePoints1}|" +
            $"{result.Session!.PendingBytes.Length}";
        acquired.Consumer.DisposeAsync().GetAwaiter().GetResult();
        result.Session.CloseOwnedTransportAsync().GetAwaiter().GetResult();
        return semantic;
    }

    internal static GameplayHandoffOfferV1 CreateHandoff(
        IGameplayTransportV1 transport,
        ReadOnlySpan<byte> pendingBytes)
    {
        PreDuelSessionV1 publicSession = new(
            default,
            0,
            false,
            PreDuelOutcome.RpsLoss,
            Array.Empty<I2Event>());
        return new GameplayHandoffOfferV1(transport, publicSession, pendingBytes);
    }

    internal static byte[][] Split(byte[] bytes, int[] sizes)
    {
        List<byte[]> chunks = new();
        int offset = 0;
        int sizeIndex = 0;
        while (offset < bytes.Length)
        {
            int count = Math.Min(sizes[sizeIndex % sizes.Length], bytes.Length - offset);
            chunks.Add(bytes.AsSpan(offset, count).ToArray());
            offset += count;
            sizeIndex++;
        }

        return chunks.ToArray();
    }
}

internal sealed class TestTransport : IByteTransport, IGameplayTransportV1
{
    private readonly Queue<byte[]> chunks;
    private byte[]? current;
    private int currentOffset;
    private bool closed;

    internal TestTransport(IEnumerable<byte[]> chunks)
    {
        this.chunks = new Queue<byte[]>(chunks.Select(chunk => chunk.ToArray()));
    }

    internal int ReadCallCount { get; private set; }

    internal int CloseCallCount { get; private set; }

    internal void Enqueue(params byte[][] additionalChunks)
    {
        foreach (byte[] chunk in additionalChunks)
        {
            chunks.Enqueue(chunk.ToArray());
        }
    }

    public ValueTask ConnectAsync(
        string host,
        int port,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    public ValueTask<int> ReadAsync(
        Memory<byte> destination,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReadCallCount++;
        while (current is null || currentOffset == current.Length)
        {
            if (chunks.Count == 0)
            {
                return ValueTask.FromResult(0);
            }

            current = chunks.Dequeue();
            currentOffset = 0;
        }

        int count = Math.Min(destination.Length, current.Length - currentOffset);
        current.AsMemory(currentOffset, count).CopyTo(destination);
        currentOffset += count;
        return ValueTask.FromResult(count);
    }

    public ValueTask WriteAsync(
        ReadOnlyMemory<byte> source,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new InvalidOperationException("I3A never writes transport bytes");
    }

    public ValueTask CloseAsync()
    {
        if (!closed)
        {
            closed = true;
            CloseCallCount++;
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => CloseAsync();
}

internal sealed class LifecycleRaceTransport : IGameplayTransportV1
{
    private readonly byte[] remainder;
    private readonly TaskCompletionSource<bool> readStarted =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> release =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int closeCallCount;
    private bool closed;

    internal LifecycleRaceTransport(byte[] remainder)
    {
        this.remainder = remainder.ToArray();
    }

    internal Task ReadStarted => readStarted.Task;

    internal int CloseCallCount => closeCallCount;

    internal void Release() => release.TrySetResult(true);

    public async ValueTask<int> ReadAsync(
        Memory<byte> destination,
        CancellationToken cancellationToken)
    {
        readStarted.TrySetResult(true);
        await release.Task.WaitAsync(cancellationToken);
        remainder.CopyTo(destination);
        return remainder.Length;
    }

    public ValueTask CloseAsync()
    {
        if (!closed)
        {
            closed = true;
            Interlocked.Increment(ref closeCallCount);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => CloseAsync();
}
