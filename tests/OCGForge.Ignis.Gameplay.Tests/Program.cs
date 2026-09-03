using System.Buffers.Binary;
using OCGForge.Ignis.Client;
using OCGForge.Ignis.Gameplay;
using OCGForge.Ignis.Protocol;

var tests = new (string Name, Action Body)[]
{
    ("MSG_START establishes SelfIsPlayer0", TestStartSelfIsPlayer0),
    ("MSG_START establishes SelfIsPlayer1", TestStartSelfIsPlayer1),
    ("MSG_START exact length and no inner NeedMoreData", TestStartLength),
    ("observer and invalid roles fail closed", TestRoleRejection),
    ("duplicate and conflicting MSG_START fail closed", TestDuplicateAndConflict),
    ("perspective-dependent and unknown messages fail closed", TestUnsupportedMessages),
    ("modern loc_info is explicit little endian", TestModernLocInfo),
    ("handoff claims exactly once", TestHandoffClaimExactlyOnce),
    ("pending bytes are processed before reads", TestPendingBytesFirst),
    ("partial pending frame continues through I1", TestPartialPendingFrame),
    ("session drains pending before live transport", TestSessionPendingReadFirst),
    ("pump and dispose share lifecycle ownership", TestPumpDisposeLifecycle),
    ("outer chunking has identical semantic output", TestChunkingDeterminism),
    ("pending suffix transfers unchanged", TestPendingSuffixTransfer),
    ("failure closes transport exactly once", TestFailureCloseExactlyOnce),
    ("short inner message fails through complete outer frame", TestShortInnerMessage),
    ("malformed outer frame fails closed", TestMalformedOuterFrame),
    ("privacy values exclude control metadata", TestPrivacyBoundary),
    ("fresh decoder values are immutable by construction", TestValueOwnership)
};

int passed = 0;
int failed = 0;
foreach ((string name, Action body) in tests)
{
    try
    {
        body();
        Console.WriteLine($"PASS {name}");
        passed++;
    }
    catch (Exception exception)
    {
        Console.WriteLine($"FAIL {name}: {exception.GetType().Name}: {exception.Message}");
        failed++;
    }
}

Console.WriteLine($"RESULT passed={passed} failed={failed}");
Environment.ExitCode = failed == 0 ? 0 : 1;

static void TestStartSelfIsPlayer0()
{
    GameplayMessageDecoderV1 decoder = new();
    GameplayMessageDecodeResult result = decoder.Decode(
        new StocGameMessagePayload(CreateStartBytes(0x00)));

    True(result.IsSuccess);
    Equal(GameplayErrorCode.None, result.Error);
    NotNull(result.Message);
    NotNull(result.Perspective);
    Equal(GameplayPerspectiveKind.SelfIsPlayer0, result.Perspective!.Kind);
    Equal((byte)0x00, result.Message!.Start.PlayerType);
    Equal((byte)4, GameplayMessageV1.MessageId);
    Equal(8000u, result.Message.Start.LifePoints0);
    Equal(7000u, result.Message.Start.LifePoints1);
    Equal((ushort)40, result.Message.Start.DeckCount0);
    Equal((ushort)15, result.Message.Start.ExtraCount0);
    Equal((ushort)41, result.Message.Start.DeckCount1);
    Equal((ushort)16, result.Message.Start.ExtraCount1);
}

static void TestStartSelfIsPlayer1()
{
    GameplayMessageDecoderV1 decoder = new();
    GameplayMessageDecodeResult result = decoder.Decode(
        new StocGameMessagePayload(CreateStartBytes(0x01)));

    True(result.IsSuccess);
    Equal(GameplayPerspectiveKind.SelfIsPlayer1, result.Perspective!.Kind);
    Equal(GameplayPerspectiveKind.SelfIsPlayer1, decoder.Perspective!.Kind);
}

static void TestStartLength()
{
    byte[] valid = CreateStartBytes(0x00);
    for (int length = 0; length < valid.Length; length++)
    {
        GameplayMessageDecodeResult result = new GameplayMessageDecoderV1().Decode(
            new StocGameMessagePayload(valid.AsSpan(0, length)));
        False(result.IsSuccess);
        Equal(GameplayErrorCode.MalformedGameMessage, result.Error);
    }

    byte[] trailing = new byte[valid.Length + 1];
    valid.CopyTo(trailing, 0);
    trailing[^1] = 0xaa;
    GameplayMessageDecodeResult withTrailing = new GameplayMessageDecoderV1().Decode(
        new StocGameMessagePayload(trailing));
    False(withTrailing.IsSuccess);
    Equal(GameplayErrorCode.MalformedGameMessage, withTrailing.Error);

    True(!Enum.GetNames<GameplayErrorCode>().Contains("NeedMoreData", StringComparer.Ordinal));
}

static void TestRoleRejection()
{
    foreach (byte observer in new byte[] { 0x10, 0x11 })
    {
        GameplayMessageDecodeResult result = new GameplayMessageDecoderV1().Decode(
            new StocGameMessagePayload(CreateStartBytes(observer)));
        False(result.IsSuccess);
        Equal(GameplayErrorCode.UnsupportedPerspective, result.Error);
    }

    foreach (byte invalid in new byte[] { 0x02, 0xff })
    {
        GameplayMessageDecodeResult result = new GameplayMessageDecoderV1().Decode(
            new StocGameMessagePayload(CreateStartBytes(invalid)));
        False(result.IsSuccess);
        Equal(GameplayErrorCode.InvalidPerspectiveRole, result.Error);
    }
}

static void TestDuplicateAndConflict()
{
    GameplayMessageDecoderV1 decoder = new();
    True(decoder.Decode(new StocGameMessagePayload(CreateStartBytes(0x00))).IsSuccess);

    GameplayMessageDecodeResult duplicate = decoder.Decode(
        new StocGameMessagePayload(CreateStartBytes(0x00)));
    False(duplicate.IsSuccess);
    Equal(GameplayErrorCode.DuplicatePerspective, duplicate.Error);

    GameplayMessageDecodeResult conflict = decoder.Decode(
        new StocGameMessagePayload(CreateStartBytes(0x01)));
    False(conflict.IsSuccess);
    Equal(GameplayErrorCode.ConflictingPerspective, conflict.Error);
    Equal(GameplayPerspectiveKind.SelfIsPlayer0, decoder.Perspective!.Kind);
}

static void TestUnsupportedMessages()
{
    GameplayMessageDecoderV1 decoder = new();
    GameplayMessageDecodeResult dependent = decoder.Decode(
        new StocGameMessagePayload(new byte[] { 6, 0, 0 }));
    False(dependent.IsSuccess);
    Equal(GameplayErrorCode.PerspectiveNotEstablished, dependent.Error);
    Null(decoder.Perspective);

    GameplayMessageDecodeResult tooLate = decoder.Decode(
        new StocGameMessagePayload(CreateStartBytes(0x00)));
    False(tooLate.IsSuccess);
    Equal(GameplayErrorCode.PerspectiveEstablishmentTooLate, tooLate.Error);

    GameplayMessageDecodeResult unknown = new GameplayMessageDecoderV1().Decode(
        new StocGameMessagePayload(new byte[] { 0xff }));
    False(unknown.IsSuccess);
    Equal(GameplayErrorCode.UnknownMessageId, unknown.Error);

    GameplayMessageDecodeResult unsupported = new GameplayMessageDecoderV1().Decode(
        new StocGameMessagePayload(new byte[] { 3 }));
    False(unsupported.IsSuccess);
    Equal(GameplayErrorCode.UnsupportedMessage, unsupported.Error);
}

static void TestModernLocInfo()
{
    byte[] bytes = new byte[10];
    bytes[0] = 1;
    bytes[1] = 0x80;
    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(2, 4), 0x11223344);
    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(6, 4), 0x55667788);

    True(GameplayWirePrimitivesV1.TryDecodeModernLocInfo(
        bytes,
        out ModernLocInfoV1 value,
        out GameplayErrorCode error));
    Equal(GameplayErrorCode.None, error);
    Equal((byte)1, value.Controller);
    Equal((byte)0x80, value.Location);
    Equal(0x11223344u, value.Sequence);
    Equal(0x55667788u, value.Position);

    False(GameplayWirePrimitivesV1.TryDecodeModernLocInfo(
        bytes.AsSpan(0, 9),
        out _,
        out error));
    Equal(GameplayErrorCode.MalformedGameMessage, error);
}

static void TestHandoffClaimExactlyOnce()
{
    TestTransport transport = new(Array.Empty<byte[]>());
    GameplayHandoffOfferV1 handoff = CreateHandoff(transport, Array.Empty<byte>());

    GameplayHandoffAcquireResult first = GameplayHandoffConsumerV1.TryCreate(handoff);
    True(first.IsSuccess);
    NotNull(first.Consumer);

    GameplayHandoffAcquireResult second = GameplayHandoffConsumerV1.TryCreate(handoff);
    False(second.IsSuccess);
    Equal(GameplayErrorCode.HandoffAlreadyClaimed, second.Error);
    Null(second.Consumer);

    first.Consumer!.DisposeAsync().GetAwaiter().GetResult();
    Equal(1, transport.CloseCallCount);
}

static void TestPendingBytesFirst()
{
    byte[] frame = WireFrameCodec.EncodeStoc(
        StocPacketType.GameMsg,
        CreateStartBytes(0x00));
    TestTransport transport = new(new[] { new byte[] { 0xaa } });
    GameplayHandoffAcquireResult acquired = GameplayHandoffConsumerV1.TryCreate(
        CreateHandoff(transport, frame));
    True(acquired.IsSuccess);

    GameplayPumpResult result = acquired.Consumer!.PumpAsync(
        CancellationToken.None).GetAwaiter().GetResult();
    True(result.IsSuccess);
    Equal(GameplayPerspectiveKind.SelfIsPlayer0, result.Perspective!.Kind);
    Equal(0, transport.ReadCallCount);
    Equal(0, transport.CloseCallCount);

    GameplaySessionV1 session = result.Session!;
    acquired.Consumer.DisposeAsync().GetAwaiter().GetResult();
    Equal(0, transport.CloseCallCount);
    session.CloseOwnedTransportAsync().GetAwaiter().GetResult();
    session.CloseOwnedTransportAsync().GetAwaiter().GetResult();
    Equal(1, transport.CloseCallCount);
}

static void TestPartialPendingFrame()
{
    byte[] frame = WireFrameCodec.EncodeStoc(
        StocPacketType.GameMsg,
        CreateStartBytes(0x00));
    const int pendingLength = 3;
    TestTransport transport = new(Split(
        frame.AsSpan(pendingLength).ToArray(),
        new[] { 1, 2, 3, 5 }));
    GameplayHandoffAcquireResult acquired = GameplayHandoffConsumerV1.TryCreate(
        CreateHandoff(transport, frame.AsSpan(0, pendingLength)));

    GameplayPumpResult result = acquired.Consumer!.PumpAsync(
        CancellationToken.None).GetAwaiter().GetResult();
    True(result.IsSuccess);
    Equal(GameplayPerspectiveKind.SelfIsPlayer0, result.Perspective!.Kind);
    True(transport.ReadCallCount > 0);
    acquired.Consumer.DisposeAsync().GetAwaiter().GetResult();
    result.Session!.CloseOwnedTransportAsync().GetAwaiter().GetResult();
}

static void TestSessionPendingReadFirst()
{
    byte[] startFrame = WireFrameCodec.EncodeStoc(
        StocPacketType.GameMsg,
        CreateStartBytes(0x00));
    byte[] futureFrame = WireFrameCodec.EncodeStoc(
        StocPacketType.GameMsg,
        new byte[] { 3 });
    byte[] combined = new byte[startFrame.Length + futureFrame.Length];
    startFrame.CopyTo(combined, 0);
    futureFrame.CopyTo(combined, startFrame.Length);

    TestTransport transport = new(new[] { combined });
    GameplayHandoffAcquireResult acquired = GameplayHandoffConsumerV1.TryCreate(
        CreateHandoff(transport, Array.Empty<byte>()));
    GameplayPumpResult result = acquired.Consumer!.PumpAsync(
        CancellationToken.None).GetAwaiter().GetResult();
    True(result.IsSuccess);

    byte[] destination = new byte[futureFrame.Length];
    int readsBeforeSession = transport.ReadCallCount;
    int pendingCount = result.Session!.ReadAsync(
            destination,
            CancellationToken.None)
        .GetAwaiter()
        .GetResult();
    Equal(futureFrame.Length, pendingCount);
    BytesEqual(futureFrame, destination);
    Equal(readsBeforeSession, transport.ReadCallCount);

    transport.Enqueue(new byte[] { 0xa1 });
    int liveCount = result.Session.ReadAsync(
            destination.AsMemory(0, 1),
            CancellationToken.None)
        .GetAwaiter()
        .GetResult();
    Equal(1, liveCount);
    Equal(readsBeforeSession + 1, transport.ReadCallCount);

    acquired.Consumer.DisposeAsync().GetAwaiter().GetResult();
    result.Session.CloseOwnedTransportAsync().GetAwaiter().GetResult();
}

static void TestPumpDisposeLifecycle()
{
    byte[] frame = WireFrameCodec.EncodeStoc(
        StocPacketType.GameMsg,
        CreateStartBytes(0x00));
    const int pendingLength = 3;
    LifecycleRaceTransport transport = new(frame.AsSpan(pendingLength).ToArray());
    GameplayHandoffAcquireResult acquired = GameplayHandoffConsumerV1.TryCreate(
        CreateHandoff(transport, frame.AsSpan(0, pendingLength)));
    True(acquired.IsSuccess);

    Task<GameplayPumpResult> pumpTask = Task.Run(async () =>
        await acquired.Consumer!.PumpAsync(CancellationToken.None));
    True(transport.ReadStarted.Wait(TimeSpan.FromSeconds(5)));

    Task disposeTask = acquired.Consumer!.DisposeAsync().AsTask();
    transport.Release();

    GameplayPumpResult result = pumpTask.GetAwaiter().GetResult();
    disposeTask.GetAwaiter().GetResult();
    True(result.IsSuccess);
    Equal(0, transport.CloseCallCount);

    result.Session!.CloseOwnedTransportAsync().GetAwaiter().GetResult();
    Equal(1, transport.CloseCallCount);
}

static void TestChunkingDeterminism()
{
    byte[] frame = WireFrameCodec.EncodeStoc(
        StocPacketType.GameMsg,
        CreateStartBytes(0x01));
    string whole = RunChunking(new[] { frame }, out _);
    string oneByte = RunChunking(
        frame.Select(value => new[] { value }).ToArray(),
        out _);
    string irregular = RunChunking(
        Split(frame, new[] { 1, 2, 5, 3, 7 }),
        out _);

    Equal(whole, oneByte);
    Equal(whole, irregular);
}

static void TestPendingSuffixTransfer()
{
    byte[] startFrame = WireFrameCodec.EncodeStoc(
        StocPacketType.GameMsg,
        CreateStartBytes(0x00));
    byte[] futureFrame = WireFrameCodec.EncodeStoc(
        StocPacketType.GameMsg,
        new byte[] { 3 });
    byte[] combined = new byte[startFrame.Length + futureFrame.Length];
    startFrame.CopyTo(combined, 0);
    futureFrame.CopyTo(combined, startFrame.Length);

    TestTransport transport = new(new[] { combined });
    GameplayHandoffAcquireResult acquired = GameplayHandoffConsumerV1.TryCreate(
        CreateHandoff(transport, Array.Empty<byte>()));
    GameplayPumpResult result = acquired.Consumer!.PumpAsync(
        CancellationToken.None).GetAwaiter().GetResult();

    True(result.IsSuccess);
    BytesEqual(futureFrame, result.Session!.PendingBytes.Span);
    acquired.Consumer.DisposeAsync().GetAwaiter().GetResult();
    result.Session.CloseOwnedTransportAsync().GetAwaiter().GetResult();
}

static void TestFailureCloseExactlyOnce()
{
    byte[] observerFrame = WireFrameCodec.EncodeStoc(
        StocPacketType.GameMsg,
        CreateStartBytes(0x10));
    TestTransport transport = new(Array.Empty<byte[]>());
    GameplayHandoffAcquireResult acquired = GameplayHandoffConsumerV1.TryCreate(
        CreateHandoff(transport, observerFrame));

    GameplayPumpResult result = acquired.Consumer!.PumpAsync(
        CancellationToken.None).GetAwaiter().GetResult();
    False(result.IsSuccess);
    Equal(GameplayErrorCode.UnsupportedPerspective, result.Error);
    Equal(1, transport.CloseCallCount);
    int readsAfterFailure = transport.ReadCallCount;

    GameplayPumpResult repeated = acquired.Consumer.PumpAsync(
        CancellationToken.None).GetAwaiter().GetResult();
    False(repeated.IsSuccess);
    Equal(readsAfterFailure, transport.ReadCallCount);
    acquired.Consumer.DisposeAsync().GetAwaiter().GetResult();
    Equal(1, transport.CloseCallCount);
}

static void TestMalformedOuterFrame()
{
    TestTransport transport = new(Array.Empty<byte[]>());
    GameplayHandoffAcquireResult acquired = GameplayHandoffConsumerV1.TryCreate(
        CreateHandoff(transport, new byte[] { 1, 0, 0xff }));

    GameplayPumpResult result = acquired.Consumer!.PumpAsync(
        CancellationToken.None).GetAwaiter().GetResult();
    False(result.IsSuccess);
    Equal(GameplayErrorCode.MalformedOuterFrame, result.Error);
    Equal(1, transport.CloseCallCount);
}

static void TestShortInnerMessage()
{
    byte[] frame = WireFrameCodec.EncodeStoc(
        StocPacketType.GameMsg,
        new byte[] { GameplayMessageV1.MessageId, 0x00 });
    TestTransport transport = new(Array.Empty<byte[]>());
    GameplayHandoffAcquireResult acquired = GameplayHandoffConsumerV1.TryCreate(
        CreateHandoff(transport, frame));

    GameplayPumpResult result = acquired.Consumer!.PumpAsync(
        CancellationToken.None).GetAwaiter().GetResult();
    False(result.IsSuccess);
    Equal(GameplayErrorCode.MalformedGameMessage, result.Error);
    Equal(1, transport.CloseCallCount);
}

static void TestPrivacyBoundary()
{
    GameplayMessageDecodeResult result = new GameplayMessageDecoderV1().Decode(
        new StocGameMessagePayload(CreateStartBytes(0x00)));
    True(result.IsSuccess);

    string[] forbidden =
    {
        "socket", "endpoint", "password", "credential", "pid", "thread",
        "timestamp", "wall", "pointer", "object", "runtime", "engine",
        "locator", "CoreHost", "hidden"
    };
    AssertDoesNotContainForbidden(result.ToString(), forbidden);
    AssertDoesNotContainForbidden(result.Message!.ToString(), forbidden);
    AssertDoesNotContainForbidden(result.Perspective!.ToString(), forbidden);
}

static void TestValueOwnership()
{
    byte[] bytes = CreateStartBytes(0x00);
    StocGameMessagePayload payload = new(bytes);
    bytes[1] = 0x01;
    GameplayMessageDecodeResult result = new GameplayMessageDecoderV1().Decode(payload);
    True(result.IsSuccess);
    Equal((byte)0x00, result.Message!.Start.PlayerType);
}

static string RunChunking(byte[][] chunks, out TestTransport transport)
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

static GameplayHandoffOfferV1 CreateHandoff(
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

static byte[] CreateStartBytes(byte playerType)
{
    byte[] bytes = new byte[18];
    bytes[0] = 4;
    bytes[1] = playerType;
    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(2, 4), 8000);
    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(6, 4), 7000);
    BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(10, 2), 40);
    BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(12, 2), 15);
    BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(14, 2), 41);
    BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(16, 2), 16);
    return bytes;
}

static byte[][] Split(byte[] bytes, int[] sizes)
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

static void AssertDoesNotContainForbidden(string? value, IEnumerable<string> forbidden)
{
    string text = value ?? string.Empty;
    foreach (string term in forbidden)
    {
        False(text.Contains(term, StringComparison.OrdinalIgnoreCase),
            $"forbidden value '{term}' appeared");
    }
}

static void True(bool condition, string message = "assertion was false")
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void False(bool condition, string message = "assertion was true") =>
    True(!condition, message);

static void Null(object? value) => True(value is null, "expected null");

static void NotNull(object? value) => True(value is not null, "expected non-null");

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"expected {expected}; actual {actual}");
    }
}

static void BytesEqual(ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual)
{
    True(expected.SequenceEqual(actual),
        $"expected {Convert.ToHexString(expected)}; actual {Convert.ToHexString(actual)}");
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
