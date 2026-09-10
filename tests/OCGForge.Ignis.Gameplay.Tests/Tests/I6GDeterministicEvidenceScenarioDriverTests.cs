using System.Buffers.Binary;
using System.Reflection;
using OCGForge.Ignis.Gameplay;
using OCGForge.Ignis.Gameplay.Tests.Fixtures;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I6GDeterministicEvidenceScenarioDriverTests
{
    internal static void TestScenarioScriptsBindOnlyAcceptedScenarios()
    {
        I6GDeterministicScenarioScriptStepV1 step = new(
            0,
            FlatPromptFamilyV1.MsgSelectYesNo,
            new string('a', 64),
            "MSG_SELECT_YESNO:YES",
            false);

        True(
            I6GDeterministicScenarioScriptV1.TryCreate(
                "projectignis.tactical-try.cyber-dragon.v1",
                new[] { step },
                out I6GDeterministicScenarioScriptV1? link,
                out I6GDeterministicScenarioDriverErrorCodeV1 linkError),
            linkError.ToString());
        NotNull(link);

        True(
            I6GDeterministicScenarioScriptV1.TryCreate(
                "projectignis.windbot.ai-blackwing.v1",
                new[] { step },
                out I6GDeterministicScenarioScriptV1? counter,
                out I6GDeterministicScenarioDriverErrorCodeV1 counterError),
            counterError.ToString());
        NotNull(counter);

        False(
            I6GDeterministicScenarioScriptV1.TryCreate(
                "projectignis.unbound.v1",
                new[] { step },
                out _,
                out I6GDeterministicScenarioDriverErrorCodeV1 wrongScenarioError));
        Equal(
            I6GDeterministicScenarioDriverErrorCodeV1.InvalidScript,
            wrongScenarioError);

        I6GDeterministicScenarioScriptStepV1 outOfOrder = step with
        {
            PromptOrdinal = 1
        };
        False(
            I6GDeterministicScenarioScriptV1.TryCreate(
                "projectignis.windbot.ai-blackwing.v1",
                new[] { outOfOrder },
                out _,
                out I6GDeterministicScenarioDriverErrorCodeV1 orderError));
        Equal(
            I6GDeterministicScenarioDriverErrorCodeV1.InvalidScript,
            orderError);
    }

    internal static void TestCompleteDomainBindsExactI4Selection()
    {
        (PerspectiveSafeFrameV1 frame,
            FlatPromptSessionV1 session,
            FlatPromptProjectionResultV1 projection) = CreateYesNo();
        I6GDeterministicPromptDescriptionResultV1 description =
            I6GDeterministicEvidenceScenarioDriverV1.TryDescribeAcceptedProjection(
                frame,
                projection);
        True(description.IsSuccess, description.ErrorCode.ToString());
        Equal(FlatPromptFamilyV1.MsgSelectYesNo, description.PromptFamily);
        Equal((byte)0, description.ActingPlayer);
        Equal(2, description.CandidateCount);
        True(description.CompleteDomain);
        NotNull(description.PublicCandidateDomainDigest);
        True(description.ChoiceKinds!.SequenceEqual(
            new[]
            {
                FlatPromptChoiceKindV1.No,
                FlatPromptChoiceKindV1.Yes
            }));

        I6GDeterministicScenarioScriptV1 script = CreateScript(
            "projectignis.windbot.ai-blackwing.v1",
            new I6GDeterministicScenarioScriptStepV1(
                0,
                description.PromptFamily!.Value,
                description.PublicCandidateDomainDigest!,
                "MSG_SELECT_YESNO:YES",
                false));
        I6GDeterministicEvidenceScenarioDriverV1 driver =
            new(script, session);

        I6GDeterministicScenarioDriverResultV1 result =
            driver.TrySelectPrompt(
                script.ScenarioId,
                frame,
                projection);

        True(result.IsSuccess, result.ErrorCode.ToString());
        Equal((ulong)0, result.PromptOrdinal);
        Equal(2, result.CandidateCount);
        True(result.CompleteDomain);
        True(result.DomainDigestMatched);
        True(result.AllCandidatesSelectionCaptured);
        True(result.SelectionApplied);
        True(result.ResponseReady);
        False(result.ContinuationRequired);
        NotNull(result.ResponseBody);
        BytesEqual(
            new byte[] { 1, 0, 0, 0 },
            result.ResponseBody!.ToArray());
    }

    internal static void TestUnplannedPromptOrDomainFailsBeforeSelection()
    {
        (PerspectiveSafeFrameV1 frame,
            FlatPromptSessionV1 session,
            FlatPromptProjectionResultV1 projection) = CreateYesNo();
        I6GDeterministicPromptDescriptionResultV1 description =
            I6GDeterministicEvidenceScenarioDriverV1.TryDescribeAcceptedProjection(
                frame,
                projection);
        True(description.IsSuccess, description.ErrorCode.ToString());
        string wrongDigest = description.PublicCandidateDomainDigest ==
            new string('0', 64)
            ? new string('1', 64)
            : new string('0', 64);
        I6GDeterministicScenarioScriptV1 script = CreateScript(
            "projectignis.windbot.ai-blackwing.v1",
            new I6GDeterministicScenarioScriptStepV1(
                0,
                description.PromptFamily!.Value,
                wrongDigest,
                "MSG_SELECT_YESNO:YES",
                false));
        I6GDeterministicEvidenceScenarioDriverV1 driver =
            new(script, session);

        I6GDeterministicScenarioDriverResultV1 result =
            driver.TrySelectPrompt(
                script.ScenarioId,
                frame,
                projection);

        False(result.IsSuccess);
        Equal(
            I6GDeterministicScenarioDriverErrorCodeV1.UnplannedPromptOrDomain,
            result.ErrorCode);
        True(result.CompleteDomain);
        False(result.DomainDigestMatched);
        False(result.SelectionApplied);
        False(result.ResponseReady);

        I6GDeterministicScenarioDriverResultV1 afterAbort =
            driver.TrySelectPrompt(
                script.ScenarioId,
                frame,
                projection);
        False(afterAbort.IsSuccess);
        Equal(
            I6GDeterministicScenarioDriverErrorCodeV1.DriverAborted,
            afterAbort.ErrorCode);
    }

    internal static void TestExpectedLocalKeyMustBeOneCompleteDomainMember()
    {
        (PerspectiveSafeFrameV1 frame,
            FlatPromptSessionV1 session,
            FlatPromptProjectionResultV1 projection) = CreateYesNo();
        I6GDeterministicPromptDescriptionResultV1 description =
            I6GDeterministicEvidenceScenarioDriverV1.TryDescribeAcceptedProjection(
                frame,
                projection);
        True(description.IsSuccess, description.ErrorCode.ToString());
        I6GDeterministicScenarioScriptV1 script = CreateScript(
            "projectignis.tactical-try.cyber-dragon.v1",
            new I6GDeterministicScenarioScriptStepV1(
                0,
                description.PromptFamily!.Value,
                description.PublicCandidateDomainDigest!,
                "MSG_SELECT_YESNO:UNPLANNED",
                false));
        I6GDeterministicEvidenceScenarioDriverV1 driver =
            new(script, session);

        I6GDeterministicScenarioDriverResultV1 result =
            driver.TrySelectPrompt(
                script.ScenarioId,
                frame,
                projection);

        False(result.IsSuccess);
        Equal(
            I6GDeterministicScenarioDriverErrorCodeV1.ExpectedCandidateNotUnique,
            result.ErrorCode);
        False(result.SelectionApplied);
        False(result.ResponseReady);
    }

    internal static void TestTerminalSelectionCannotBeReusedAsNextPrompt()
    {
        (PerspectiveSafeFrameV1 frame,
            FlatPromptSessionV1 session,
            FlatPromptProjectionResultV1 projection) = CreateYesNo();
        I6GDeterministicPromptDescriptionResultV1 description =
            I6GDeterministicEvidenceScenarioDriverV1.TryDescribeAcceptedProjection(
                frame,
                projection);
        True(description.IsSuccess, description.ErrorCode.ToString());
        I6GDeterministicScenarioScriptV1 script = CreateScript(
            "projectignis.windbot.ai-blackwing.v1",
            new I6GDeterministicScenarioScriptStepV1(
                0,
                description.PromptFamily!.Value,
                description.PublicCandidateDomainDigest!,
                "MSG_SELECT_YESNO:YES",
                false),
            new I6GDeterministicScenarioScriptStepV1(
                1,
                description.PromptFamily.Value,
                description.PublicCandidateDomainDigest!,
                "MSG_SELECT_YESNO:NO",
                false));
        I6GDeterministicEvidenceScenarioDriverV1 driver =
            new(script, session);

        I6GDeterministicScenarioDriverResultV1 first =
            driver.TrySelectPrompt(script.ScenarioId, frame, projection);
        True(first.IsSuccess, first.ErrorCode.ToString());

        I6GDeterministicScenarioDriverResultV1 reused =
            driver.TrySelectPrompt(script.ScenarioId, frame, projection);
        False(reused.IsSuccess);
        Equal(
            I6GDeterministicScenarioDriverErrorCodeV1.UnplannedPromptOrDomain,
            reused.ErrorCode);
        False(reused.ResponseReady);
    }

    internal static void TestContinuationStepsRemainScenarioAndDomainBound()
    {
        byte[] promptBytes =
        {
            0x12, 0x00, 0x02, 0xFE, 0xFF, 0xFF, 0xFB
        };
        PerspectiveSafeFrameV1 frame = CreateFullFrame();

        FlatPromptSessionV1 probeSession = new();
        FlatPromptProjectionResultV1 firstProjection =
            probeSession.TryAcceptI5Prompt(promptBytes);
        True(firstProjection.IsSuccess, firstProjection.Error.ToString());
        I6GDeterministicPromptDescriptionResultV1 firstDescription =
            I6GDeterministicEvidenceScenarioDriverV1.TryDescribeAcceptedProjection(
                frame,
                firstProjection);
        True(firstDescription.IsSuccess, firstDescription.ErrorCode.ToString());

        True(
            probeSession.TryCaptureSelection(
                firstProjection.Candidates![0].I4LocalCandidateKey,
                out FlatPromptSelectionHandleV1? firstHandle,
                out FlatPromptErrorCodeV1 firstCaptureError),
            firstCaptureError.ToString());
        FlatPromptContinuationStepResultV1 afterFirst =
            probeSession.TryApplySelection(firstHandle);
        True(afterFirst.IsSuccess, afterFirst.Error.ToString());
        True(afterFirst.Projection is not null);
        I6GDeterministicPromptDescriptionResultV1 secondDescription =
            I6GDeterministicEvidenceScenarioDriverV1.TryDescribeAcceptedProjection(
                frame,
                afterFirst.Projection!);
        True(secondDescription.IsSuccess, secondDescription.ErrorCode.ToString());

        I6GDeterministicScenarioScriptV1 script = CreateScript(
            "projectignis.windbot.ai-blackwing.v1",
            new I6GDeterministicScenarioScriptStepV1(
                0,
                firstDescription.PromptFamily!.Value,
                firstDescription.PublicCandidateDomainDigest!,
                firstProjection.Candidates[0].I4LocalCandidateKey,
                false),
            new I6GDeterministicScenarioScriptStepV1(
                1,
                secondDescription.PromptFamily!.Value,
                secondDescription.PublicCandidateDomainDigest!,
                afterFirst.Projection!.Candidates![0].I4LocalCandidateKey,
                true));

        FlatPromptSessionV1 session = new();
        FlatPromptProjectionResultV1 projection =
            session.TryAcceptI5Prompt(promptBytes);
        I6GDeterministicEvidenceScenarioDriverV1 driver =
            new(script, session);
        I6GDeterministicScenarioDriverResultV1 first =
            driver.TrySelectPrompt(script.ScenarioId, frame, projection);
        True(first.IsSuccess, first.ErrorCode.ToString());
        True(first.ContinuationRequired);
        False(first.ResponseReady);
        NotNull(first.NextProjection);

        I6GDeterministicScenarioDriverResultV1 second =
            driver.TrySelectContinuation(
                script.ScenarioId,
                frame,
                first.NextProjection!);
        True(second.IsSuccess, second.ErrorCode.ToString());
        True(second.ResponseReady);
        False(second.ContinuationRequired);
        BytesEqual(
            new byte[] { 0x00, 0x04, 0x00, 0x01, 0x08, 0x02 },
            second.ResponseBody!.ToArray());
    }

    internal static void TestLocalRoutingKeyDoesNotChangePublicDomainDigest()
    {
        PerspectiveSafeFrameV1 frame = CreateFullFrame();
        FlatPromptYesNoPublicContextV1 context =
            new(0, 42);
        FlatPromptProjectionResultV1 first =
            FlatPromptProjectionResultV1.Success(
                context,
                new FlatPublicCandidateDescriptorV1[]
                {
                    new FlatYesNoPublicCandidateDescriptorV1(
                        "local.first",
                        FlatPromptChoiceKindV1.Yes)
                });
        FlatPromptProjectionResultV1 second =
            FlatPromptProjectionResultV1.Success(
                context,
                new FlatPublicCandidateDescriptorV1[]
                {
                    new FlatYesNoPublicCandidateDescriptorV1(
                        "local.second",
                        FlatPromptChoiceKindV1.Yes)
                });

        I6GDeterministicPromptDescriptionResultV1 firstDescription =
            I6GDeterministicEvidenceScenarioDriverV1.TryDescribeAcceptedProjection(
                frame,
                first);
        I6GDeterministicPromptDescriptionResultV1 secondDescription =
            I6GDeterministicEvidenceScenarioDriverV1.TryDescribeAcceptedProjection(
                frame,
                second);

        True(firstDescription.IsSuccess, firstDescription.ErrorCode.ToString());
        True(secondDescription.IsSuccess, secondDescription.ErrorCode.ToString());
        Equal(
            firstDescription.PublicCandidateDomainDigest,
            secondDescription.PublicCandidateDomainDigest);
    }

    private static I6GDeterministicScenarioScriptV1 CreateScript(
        string scenarioId,
        params I6GDeterministicScenarioScriptStepV1[] steps)
    {
        True(
            I6GDeterministicScenarioScriptV1.TryCreate(
                scenarioId,
                steps,
                out I6GDeterministicScenarioScriptV1? script,
                out I6GDeterministicScenarioDriverErrorCodeV1 error),
            error.ToString());
        return script!;
    }

    private static (PerspectiveSafeFrameV1 Frame,
        FlatPromptSessionV1 Session,
        FlatPromptProjectionResultV1 Projection) CreateYesNo()
    {
        FlatPromptSessionV1 session = new();
        FlatPromptProjectionResultV1 projection =
            session.TryAcceptPrompt(YesNoMessage());
        True(projection.IsSuccess, projection.Error.ToString());
        return (CreateFullFrame(), session, projection);
    }

    private static byte[] YesNoMessage()
    {
        byte[] bytes = new byte[10];
        bytes[0] = (byte)FlatPromptFamilyV1.MsgSelectYesNo;
        bytes[1] = 0;
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(2), 42);
        return bytes;
    }

    private static PerspectiveSafeFrameV1 CreateFullFrame()
    {
        MethodInfo method = typeof(I6C6NativeOracleTests).GetMethod(
                "CreateFullFrame",
                BindingFlags.NonPublic | BindingFlags.Static) ??
            throw new InvalidOperationException("full frame factory missing");
        return (PerspectiveSafeFrameV1)method.Invoke(
            null,
            new object[] { false, false })!;
    }
}
