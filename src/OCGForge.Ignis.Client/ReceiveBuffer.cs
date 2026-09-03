namespace OCGForge.Ignis.Client;

public sealed class ReceiveBuffer
{
    private readonly byte[] storage;
    private int count;

    public ReceiveBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(
            capacity,
            1,
            nameof(capacity));

        storage = new byte[capacity];
    }

    public int Capacity => storage.Length;

    public int Count => count;

    public ReadOnlyMemory<byte> Unread => storage.AsMemory(0, count);

    public I2Result Append(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length > storage.Length - count)
        {
            return I2Result.Failure(I2ErrorCode.ReceiveBufferOverflow);
        }

        bytes.CopyTo(storage.AsSpan(count));
        count += bytes.Length;
        return I2Result.Success();
    }

    public void Consume(int consumedBytes)
    {
        if (consumedBytes < 0 || consumedBytes > count)
        {
            throw new ArgumentOutOfRangeException(nameof(consumedBytes));
        }

        if (consumedBytes == 0)
        {
            return;
        }

        int remaining = count - consumedBytes;
        storage.AsSpan(consumedBytes, remaining).CopyTo(storage.AsSpan());
        storage.AsSpan(remaining, consumedBytes).Clear();
        count = remaining;
    }

    public byte[] CopyUnread() => storage.AsSpan(0, count).ToArray();
}
