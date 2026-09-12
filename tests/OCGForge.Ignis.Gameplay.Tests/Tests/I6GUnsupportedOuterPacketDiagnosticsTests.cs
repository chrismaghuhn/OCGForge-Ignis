using System.Reflection;
using OCGForge.Ignis.Gameplay;
using OCGForge.Ignis.Protocol;
using OCGForge.Ignis.Gameplay.Tests.Fixtures;
using static OCGForge.Ignis.Gameplay.Tests.GameplayMessageFixtures;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;
using static OCGForge.Ignis.Gameplay.Tests.TransportFixtures;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I6GUnsupportedOuterPacketDiagnosticsTests
{
    internal static void TestOuterPacketTracePreservesWireOrdinal()
    {
        Type traceType = typeof(I6C6LiveGameplayCaptureResultV1)
            .Assembly
            .GetType(
                "OCGForge.Ignis.Gameplay.Tests.Fixtures.I6C6CapturedGameplayMessageTraceV1")
            ?? throw new InvalidOperationException("capture trace type missing");
        MethodInfo? method = traceType.GetMethod(
            "TryFindOuterPacketAtOrdinal",
            BindingFlags.Static | BindingFlags.NonPublic);
        NotNull(method);

        byte[] startFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            CreateStartBytes(0));
        byte[] waitingFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            new byte[] { 3 });
        byte[] newTurnFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            new byte[] { 40, 0 });
        byte[] timeLimitFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.TimeLimit,
            PacketPayloadCodec.EncodeStocTimeLimit(
                new StocTimeLimitPayload(0, 120)));
        byte[] outerFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.TypeChange,
            PacketPayloadCodec.EncodeStocTypeChange(
                new StocTypeChangePayload(0)));

        object? diagnostic = method!.Invoke(
            null,
            new object[]
            {
                GameplayPerspectiveV1.SelfIsPlayer0,
                new ReadOnlyMemory<byte>(startFrame),
                new[]
                {
                    Join(
                        waitingFrame,
                        newTurnFrame,
                        timeLimitFrame,
                        outerFrame)
                },
                (ulong)3
            });
        NotNull(diagnostic);

        Type diagnosticType = diagnostic!.GetType();
        Equal(
            (byte)StocPacketType.TypeChange,
            (byte)GetProperty(diagnostic, diagnosticType, "RawType")!);
        Equal(
            StocPacketType.TypeChange,
            GetProperty(diagnostic, diagnosticType, "Type"));
        Equal(
            PacketTypeDisposition.Supported,
            GetProperty(diagnostic, diagnosticType, "Disposition"));
        Equal(
            PayloadContractKind.ExactTypedLayout,
            GetProperty(diagnostic, diagnosticType, "PayloadContract"));
        Equal(
            1,
            GetProperty(diagnostic, diagnosticType, "PayloadLength"));
        False(
            diagnosticType.GetProperties(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic)
                .Any(property => property.Name is "Payload" or "Bytes"));
    }

    internal static void TestCaptureFailureDiagnosticsExposeOuterPacketShape()
    {
        Type diagnosticsType = typeof(I6C6CaptureFailureDiagnosticsV1);
        PropertyInfo? outerProperty = diagnosticsType.GetProperty(
            "OuterPacketDiagnostics",
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic);
        NotNull(outerProperty);
        Type outerType = Nullable.GetUnderlyingType(outerProperty!.PropertyType) ??
            outerProperty.PropertyType;
        foreach (string propertyName in new[]
                 {
                     "RawType",
                     "Type",
                     "Disposition",
                     "PayloadContract",
                     "PayloadLength"
                 })
        {
            NotNull(
                outerType.GetProperty(
                    propertyName,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic));
        }
    }

    private static object? GetProperty(
        object instance,
        Type type,
        string name) =>
        type.GetProperty(
                name,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic)!
            .GetValue(instance);
}
