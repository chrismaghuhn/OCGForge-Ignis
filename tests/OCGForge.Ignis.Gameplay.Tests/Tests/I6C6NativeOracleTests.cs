using System.Diagnostics;
using OCGForge.Ignis.Client;
using OCGForge.Ignis.Gameplay;
using OCGForge.Ignis.Gameplay.Tests.Fixtures;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I6C6NativeOracleTests
{
    internal static void TestI6C6_3ClosureHarnessContract()
    {
        I6C6ClosureHarnessConfigurationV1 configuration =
            new(
                RuntimeExecutablePath:
                    @"C:\Users\chris\.config\superpowers\worktrees\edopro\i6c6-edopro-server-only-v1\bin\release\ygoprodll.exe",
                RuntimeExecutableSha256:
                    "d57d9d705fb01ef2ba9d73eb89b29b6c438bf9d9c91e20daa8bca2fbf19babed",
                AssetRoot: @"C:\ProjectIgnis",
                DatabaseSha256:
                    "c49a077285e1d999f32056cb65303b75e311e859b4486c48f41772a193069225",
                CardscriptsCommit:
                    "00a828b79303d047d6905f528857cc287ad3a84e",
                EdoproRuntimeHead:
                    "d72872347c34e7a7f37ba12b7e8fb20cdac78e0d",
                A2PatchCommit:
                    "edfb77d7baf68b986209d327a871021296c2954b",
                A2PatchsetSha256:
                    "fd97edae44cb07a0b43f477f14863c40177eb4ac9a804d7c425450f07d5894d7",
                StartupCompatPatchCommit:
                    "d72872347c34e7a7f37ba12b7e8fb20cdac78e0d",
                StartupCompatPatchsetSha256:
                    "83bf958fd115b6f85e4dee744dfc4685d5612d1c9d795480adc01831e7e33b49",
                LoopbackHostPatchParent:
                    "d72872347c34e7a7f37ba12b7e8fb20cdac78e0d",
                LoopbackHostPatchCommit:
                    "68a660565650aa2988cf4ad55831bd9ef861931d",
                LoopbackHostPatchsetSha256:
                    "aa87d5467290b4ae7a95774e1e8ba2288a5587501991e0a7fcfc20271e102997",
                ServerBootstrapPatchParent:
                    "68a660565650aa2988cf4ad55831bd9ef861931d",
                ServerBootstrapPatchCommit:
                    "690d031c9b882a36ffd1ea167af80d0ef9cf791b",
                ServerBootstrapPatchsetSha256:
                    "7500ad5f75fe31910427343d65672b871e8a7c092f75f121ef57831ce4b0c31f",
                ServerBootstrapRuntimeExecutableSha256:
                    "ebb959d087ed0dd26a2b891f4db42ff5afc79f73e3aeadce06dac10fb49b504e",
                TimerGuardParent:
                    "690d031c9b882a36ffd1ea167af80d0ef9cf791b",
                TimerGuardCommit:
                    "2f728fd80e82eb952baeb7ffe9968c7086dca9d0",
                TimerGuardPatchsetSha256:
                    "35613a11761aa76a770d9400186895c38f1aa9bf041739e4b2109bcd47f2a9a1",
                TimerGuardRuntimeExecutableSha256:
                    "7f0433b62660e9c1dae3054ca07a446c07a7d112d005e988d575deba64930f66",
                RngParent:
                    "2f728fd80e82eb952baeb7ffe9968c7086dca9d0",
                RngImplementationCommit:
                    "c5cd78d282c220b557df5cafe30cd6a6aed80c26",
                RngKatCommit:
                    "4e1f93683d4ccec76558d06a5483f4090e30113c",
                RngCombinedPatchsetSha256:
                    "d5f4fdde30cd1fb427d92d07304806f95974f138a2bc94928f65e6885541d1a8",
                EvidenceRngId:
                    "ocgforge-ignis.i6c6.evidence-rng.v1",
                EvidenceRngRoot: 0x2e43fb46490a681dUL,
                FinalRuntimeExecutableSha256:
                    "d57d9d705fb01ef2ba9d73eb89b29b6c438bf9d9c91e20daa8bca2fbf19babed",
                ServerOnlyBootstrapFixParent:
                    "4e1f93683d4ccec76558d06a5483f4090e30113c",
                ServerOnlyBootstrapFixCommit:
                    "1dcb983b2b7a2ead807eda8aa98d26066ce0baa3",
                ServerOnlyBootstrapFixPatchsetSha256:
                    "5019259b4db733e4a38f285b45d0a4dea8b11a245047edee76fcb6234fa4bf79",
                ServerOnlyBootstrapFixRuntimeExecutableSha256:
                    "d57d9d705fb01ef2ba9d73eb89b29b6c438bf9d9c91e20daa8bca2fbf19babed",
                ExternalRuntimeReadinessTimeout: TimeSpan.FromSeconds(10),
                LinkScenario: new(
                    "projectignis.tactical-try.cyber-dragon.v1",
                    @"C:\ProjectIgnis\deck\[Tactical-Try Deck] Decisive Strike Cyber Dragon.ydk",
                    "5807306a04e08b452938aa06e6692738ffc8c3346cde3045202c6d380ddd4b10",
                    @"C:\ProjectIgnis\WindBot\Decks\AI_Blackwing.ydk",
                    "0051f350303eed589fed1bba0cf58e345644c91cb5825415357a5ac297ee09b2",
                    "EXTRA"),
                CounterScenario: new(
                    "projectignis.windbot.ai-blackwing.v1",
                    @"C:\ProjectIgnis\WindBot\Decks\AI_Blackwing.ydk",
                    "0051f350303eed589fed1bba0cf58e345644c91cb5825415357a5ac297ee09b2",
                    @"C:\ProjectIgnis\WindBot\Decks\AI_CyberDragon.ydk",
                    "ed30c491ad4323ed4729c2de68d7298714e01d71ac5321aaabcb7401a91fbda1",
                    "NONE"));

        I6C6ClosureHarnessValidationResultV1 valid =
            I6C6ClosureHarnessV1.ValidateConfiguration(configuration);
        True(valid.IsSuccess, valid.ErrorCode.ToString());
        Equal(1, valid.ExternalRuntimeProcessOwnerCount);
        Equal(1, valid.TcpCaptureOwnerCount);
        True(valid.ReusesIgnisDecoder);
        True(valid.ReusesIgnisMirror);
        True(valid.ReusesCurrentPath);
        False(valid.AllowsSyntheticEvidenceInRealMode);
        False(valid.AllowsSyntheticLinkEvidenceInRealMode);
        False(valid.AllowsSyntheticCounterEvidenceInRealMode);
        Equal(
            TimeSpan.FromSeconds(10),
            configuration.ExternalRuntimeReadinessTimeout);

        I6C6ClosureHarnessValidationResultV1 zeroReadinessTimeout =
            I6C6ClosureHarnessV1.ValidateConfiguration(
                configuration with
                {
                    ExternalRuntimeReadinessTimeout = TimeSpan.Zero
                });
        Equal(
            I6C6ClosureHarnessErrorCodeV1.ScenarioConfigurationInvalid,
            zeroReadinessTimeout.ErrorCode);

        I6C6ClosureHarnessValidationResultV1 negativeReadinessTimeout =
            I6C6ClosureHarnessV1.ValidateConfiguration(
                configuration with
                {
                    ExternalRuntimeReadinessTimeout =
                        TimeSpan.FromMilliseconds(-1)
                });
        Equal(
            I6C6ClosureHarnessErrorCodeV1.ScenarioConfigurationInvalid,
            negativeReadinessTimeout.ErrorCode);

        I6C6ClosureHarnessValidationResultV1 unboundedReadinessTimeout =
            I6C6ClosureHarnessV1.ValidateConfiguration(
                configuration with
                {
                    ExternalRuntimeReadinessTimeout =
                        TimeSpan.FromMinutes(1).Add(
                            TimeSpan.FromMilliseconds(1))
                });
        Equal(
            I6C6ClosureHarnessErrorCodeV1.ScenarioConfigurationInvalid,
            unboundedReadinessTimeout.ErrorCode);

        if (OperatingSystem.IsWindows())
        {
            I6C6ClosureHarnessValidationResultV1 local =
                I6C6ClosureHarnessV1.ValidateConfiguration(
                    configuration,
                    requireLocalArtifacts: true);
            True(local.IsSuccess, local.ErrorCode.ToString());

            I6C6ClosureBindingResultV1 linkBinding =
                I6C6ClosureHarnessV1.TryBind(
                    configuration,
                    I6C6ClosureScenarioKindV1.Link,
                    requireLocalArtifacts: true);
            True(linkBinding.IsSuccess, linkBinding.ErrorCode.ToString());
            NotNull(linkBinding.Binding);
            PrevalidatedProtocolDeck linkDeck =
                I6C6ClosureHarnessV1.LoadDeck(
                    configuration.LinkScenario.PrimaryDeckPath);
            True(linkDeck.MainAndExtraCards.Count > 0);
        }

        I6C6ClosureHarnessValidationResultV1 wrongRuntime =
            I6C6ClosureHarnessV1.ValidateConfiguration(
                configuration with { EdoproRuntimeHead = "wrong" });
        Equal(I6C6ClosureHarnessErrorCodeV1.RuntimeProvenanceMismatch,
            wrongRuntime.ErrorCode);

        I6C6ClosureHarnessValidationResultV1 wrongLoopbackParent =
            I6C6ClosureHarnessV1.ValidateConfiguration(
                configuration with { LoopbackHostPatchParent = "wrong" });
        Equal(I6C6ClosureHarnessErrorCodeV1.RuntimeProvenanceMismatch,
            wrongLoopbackParent.ErrorCode);

        I6C6ClosureHarnessValidationResultV1 wrongLoopbackCommit =
            I6C6ClosureHarnessV1.ValidateConfiguration(
                configuration with { LoopbackHostPatchCommit = "wrong" });
        Equal(I6C6ClosureHarnessErrorCodeV1.RuntimeProvenanceMismatch,
            wrongLoopbackCommit.ErrorCode);

        I6C6ClosureHarnessValidationResultV1 wrongLoopbackPatchset =
            I6C6ClosureHarnessV1.ValidateConfiguration(
                configuration with { LoopbackHostPatchsetSha256 = "wrong" });
        Equal(I6C6ClosureHarnessErrorCodeV1.RuntimeProvenanceMismatch,
            wrongLoopbackPatchset.ErrorCode);

        I6C6ClosureHarnessValidationResultV1 wrongBootstrapParent =
            I6C6ClosureHarnessV1.ValidateConfiguration(
                configuration with { ServerBootstrapPatchParent = "wrong" });
        Equal(I6C6ClosureHarnessErrorCodeV1.RuntimeProvenanceMismatch,
            wrongBootstrapParent.ErrorCode);

        I6C6ClosureHarnessValidationResultV1 wrongBootstrapCommit =
            I6C6ClosureHarnessV1.ValidateConfiguration(
                configuration with { ServerBootstrapPatchCommit = "wrong" });
        Equal(I6C6ClosureHarnessErrorCodeV1.RuntimeProvenanceMismatch,
            wrongBootstrapCommit.ErrorCode);

        I6C6ClosureHarnessValidationResultV1 wrongBootstrapPatchset =
            I6C6ClosureHarnessV1.ValidateConfiguration(
                configuration with { ServerBootstrapPatchsetSha256 = "wrong" });
        Equal(I6C6ClosureHarnessErrorCodeV1.RuntimeProvenanceMismatch,
            wrongBootstrapPatchset.ErrorCode);

        I6C6ClosureHarnessValidationResultV1 wrongBootstrapRuntime =
            I6C6ClosureHarnessV1.ValidateConfiguration(
                configuration with
                {
                    ServerBootstrapRuntimeExecutableSha256 = "wrong"
                });
        Equal(I6C6ClosureHarnessErrorCodeV1.RuntimeProvenanceMismatch,
            wrongBootstrapRuntime.ErrorCode);

        I6C6ClosureHarnessValidationResultV1 missingCorrectiveCommit =
            I6C6ClosureHarnessV1.ValidateConfiguration(
                configuration with
                {
                    ServerOnlyBootstrapFixCommit = string.Empty
                });
        Equal(
            I6C6ClosureHarnessErrorCodeV1.RuntimeProvenanceMismatch,
            missingCorrectiveCommit.ErrorCode);

        I6C6ClosureHarnessValidationResultV1 wrongCorrectiveCommit =
            I6C6ClosureHarnessV1.ValidateConfiguration(
                configuration with
                {
                    ServerOnlyBootstrapFixCommit = "wrong"
                });
        Equal(
            I6C6ClosureHarnessErrorCodeV1.RuntimeProvenanceMismatch,
            wrongCorrectiveCommit.ErrorCode);

        I6C6ClosureHarnessValidationResultV1 wrongCorrectiveParent =
            I6C6ClosureHarnessV1.ValidateConfiguration(
                configuration with { ServerOnlyBootstrapFixParent = "wrong" });
        Equal(
            I6C6ClosureHarnessErrorCodeV1.RuntimeProvenanceMismatch,
            wrongCorrectiveParent.ErrorCode);

        I6C6ClosureHarnessValidationResultV1 wrongCorrectivePatchset =
            I6C6ClosureHarnessV1.ValidateConfiguration(
                configuration with
                {
                    ServerOnlyBootstrapFixPatchsetSha256 = "wrong"
                });
        Equal(
            I6C6ClosureHarnessErrorCodeV1.RuntimeProvenanceMismatch,
            wrongCorrectivePatchset.ErrorCode);

        I6C6ClosureHarnessValidationResultV1 wrongCorrectiveRuntime =
            I6C6ClosureHarnessV1.ValidateConfiguration(
                configuration with
                {
                    ServerOnlyBootstrapFixRuntimeExecutableSha256 = "wrong"
                });
        Equal(
            I6C6ClosureHarnessErrorCodeV1.RuntimeProvenanceMismatch,
            wrongCorrectiveRuntime.ErrorCode);

        I6C6ClosureHarnessValidationResultV1 wrongTimerParent =
            I6C6ClosureHarnessV1.ValidateConfiguration(
                configuration with { TimerGuardParent = "wrong" });
        Equal(I6C6ClosureHarnessErrorCodeV1.RuntimeProvenanceMismatch,
            wrongTimerParent.ErrorCode);

        I6C6ClosureHarnessValidationResultV1 wrongTimerCommit =
            I6C6ClosureHarnessV1.ValidateConfiguration(
                configuration with { TimerGuardCommit = "wrong" });
        Equal(I6C6ClosureHarnessErrorCodeV1.RuntimeProvenanceMismatch,
            wrongTimerCommit.ErrorCode);

        I6C6ClosureHarnessValidationResultV1 wrongTimerPatchset =
            I6C6ClosureHarnessV1.ValidateConfiguration(
                configuration with { TimerGuardPatchsetSha256 = "wrong" });
        Equal(I6C6ClosureHarnessErrorCodeV1.RuntimeProvenanceMismatch,
            wrongTimerPatchset.ErrorCode);

        I6C6ClosureHarnessValidationResultV1 wrongTimerRuntime =
            I6C6ClosureHarnessV1.ValidateConfiguration(
                configuration with { TimerGuardRuntimeExecutableSha256 = "wrong" });
        Equal(I6C6ClosureHarnessErrorCodeV1.RuntimeProvenanceMismatch,
            wrongTimerRuntime.ErrorCode);

        I6C6ClosureHarnessValidationResultV1 wrongRngParent =
            I6C6ClosureHarnessV1.ValidateConfiguration(
                configuration with { RngParent = "wrong" });
        Equal(I6C6ClosureHarnessErrorCodeV1.RuntimeProvenanceMismatch,
            wrongRngParent.ErrorCode);

        I6C6ClosureHarnessValidationResultV1 wrongRngImplementation =
            I6C6ClosureHarnessV1.ValidateConfiguration(
                configuration with { RngImplementationCommit = "wrong" });
        Equal(I6C6ClosureHarnessErrorCodeV1.RuntimeProvenanceMismatch,
            wrongRngImplementation.ErrorCode);

        I6C6ClosureHarnessValidationResultV1 wrongRngKat =
            I6C6ClosureHarnessV1.ValidateConfiguration(
                configuration with { RngKatCommit = "wrong" });
        Equal(I6C6ClosureHarnessErrorCodeV1.RuntimeProvenanceMismatch,
            wrongRngKat.ErrorCode);

        I6C6ClosureHarnessValidationResultV1 wrongRngPatchset =
            I6C6ClosureHarnessV1.ValidateConfiguration(
                configuration with { RngCombinedPatchsetSha256 = "wrong" });
        Equal(I6C6ClosureHarnessErrorCodeV1.RuntimeProvenanceMismatch,
            wrongRngPatchset.ErrorCode);

        I6C6ClosureHarnessValidationResultV1 wrongRngId =
            I6C6ClosureHarnessV1.ValidateConfiguration(
                configuration with { EvidenceRngId = "wrong" });
        Equal(I6C6ClosureHarnessErrorCodeV1.RuntimeProvenanceMismatch,
            wrongRngId.ErrorCode);

        I6C6ClosureHarnessValidationResultV1 wrongRngRoot =
            I6C6ClosureHarnessV1.ValidateConfiguration(
                configuration with { EvidenceRngRoot = 1 });
        Equal(I6C6ClosureHarnessErrorCodeV1.RuntimeProvenanceMismatch,
            wrongRngRoot.ErrorCode);

        I6C6ClosureHarnessValidationResultV1 wrongFinalRuntime =
            I6C6ClosureHarnessV1.ValidateConfiguration(
                configuration with { FinalRuntimeExecutableSha256 = "wrong" });
        Equal(I6C6ClosureHarnessErrorCodeV1.RuntimeProvenanceMismatch,
            wrongFinalRuntime.ErrorCode);

        I6C6ClosureHarnessValidationResultV1 wrongRuntimeSha =
            I6C6ClosureHarnessV1.ValidateConfiguration(
                configuration with { RuntimeExecutableSha256 = "wrong" });
        Equal(I6C6ClosureHarnessErrorCodeV1.RuntimeProvenanceMismatch,
            wrongRuntimeSha.ErrorCode);

        I6C6ClosureHarnessValidationResultV1 forbiddenExecutable =
            I6C6ClosureHarnessV1.ValidateConfiguration(
                configuration with
                {
                    RuntimeExecutablePath = @"C:\ProjectIgnis\EDOPro.exe"
                });
        Equal(I6C6ClosureHarnessErrorCodeV1.ForbiddenRuntimeExecutable,
            forbiddenExecutable.ErrorCode);

        ConnectionConfigurationV1 connection = new(
            "127.0.0.1",
            7911,
            "Ignis",
            0,
            RoomPasswordV1.Create(string.Empty),
            TimeSpan.FromSeconds(1));
        True(
            configuration.ExternalRuntimeReadinessTimeout >
            connection.ConnectionTimeout);
        ProcessStartInfo startInfo =
            I6C6ExternalRuntimeProcessOwnerV1.CreateStartInfo(
                configuration,
                connection);
        True(startInfo.ArgumentList.SequenceEqual(
            new[]
            {
                "-C",
                @"C:\ProjectIgnis",
                "-r",
                "-m",
                "-i6c6-server-only",
                "-i6c6-server-port",
                "7911"
            },
            StringComparer.Ordinal));

        ConnectionConfigurationV1 alternateConnection = new(
            "127.0.0.1",
            7912,
            "Ignis",
            0,
            RoomPasswordV1.Create(string.Empty),
            TimeSpan.FromSeconds(1));
        ProcessStartInfo alternateStartInfo =
            I6C6ExternalRuntimeProcessOwnerV1.CreateStartInfo(
                configuration,
                alternateConnection);
        True(alternateStartInfo.ArgumentList.SequenceEqual(
            new[]
            {
                "-C",
                @"C:\ProjectIgnis",
                "-r",
                "-m",
                "-i6c6-server-only",
                "-i6c6-server-port",
                "7912"
            },
            StringComparer.Ordinal));

        foreach (int invalidPort in new[] { 0, 65536 })
        {
            try
            {
                _ = new ConnectionConfigurationV1(
                    "127.0.0.1",
                    invalidPort,
                    "Ignis",
                    0,
                    RoomPasswordV1.Create(string.Empty),
                    TimeSpan.FromSeconds(1));
                throw new InvalidOperationException(
                    $"invalid connection port {invalidPort} was accepted");
            }
            catch (ClientConfigurationException exception)
            {
                Equal(I2ErrorCode.InvalidConfiguration, exception.Code);
            }
        }

        I6C6ClosureHarnessExecutionResultV1 blocked =
            I6C6ClosureHarnessV1.TryBeginRealExecution(
                configuration,
                realRunAuthorized: false);
        Equal(I6C6ClosureHarnessErrorCodeV1.RealRunNotAuthorized,
            blocked.ErrorCode);
        False(blocked.ProcessStarted);
        False(blocked.GameplayCaptureSucceeded);

        I6C6ClosureHarnessExecutionResultV1 guarded =
            I6C6ClosureHarnessV1.TryBeginRealExecution(
                configuration,
                realRunAuthorized: true);
        Equal(I6C6ClosureHarnessErrorCodeV1.RealExecutionRequiresInputs,
            guarded.ErrorCode);
        False(guarded.ProcessStarted);
        False(guarded.GameplayCaptureSucceeded);

        I6C6ClosureEvidenceValidationResultV1 incomplete =
            I6C6ClosureHarnessV1.ValidateEvidence(
                null,
                new I6C6ClosureEvidenceRequirementsV1(
                    null,
                    true,
                    false,
                    false));
        Equal(I6C6ClosureHarnessErrorCodeV1.IncompleteLiveEvidence,
            incomplete.ErrorCode);

        TestI6C6_3PropertyEvidenceExtraction();
    }

    internal static void TestI6C6_3PropertyEvidenceExtraction()
    {
        PerspectiveSafeCardPropertiesV1 linkProperties =
            new(
                type: 0x4000000,
                linkRating: 3,
                linkMarkers: new[]
                {
                    PerspectiveSafeLinkMarkerV1.Bottom,
                    PerspectiveSafeLinkMarkerV1.Top
                });
        PerspectiveSafeFrameV1 linkFrame = CreatePropertyFrame(
            new[]
            {
                new PerspectiveSafeEntityV1(
                    "p0:EXTRA_DECK:public:12345678:0",
                    true,
                    12345678,
                    0,
                    0,
                    PerspectiveSafeSemanticZoneV1.ExtraDeck,
                    null,
                    null,
                    PerspectiveSafePositionV1.FaceDownDefense,
                    false,
                    true,
                    linkProperties,
                    linkProperties),
                new PerspectiveSafeEntityV1(
                    "p0:MONSTER_ZONE:0",
                    true,
                    12345678,
                    0,
                    0,
                    PerspectiveSafeSemanticZoneV1.MonsterZone,
                    0,
                    null,
                    PerspectiveSafePositionV1.FaceUpAttack,
                    true,
                    false,
                    linkProperties,
                    linkProperties),
                new PerspectiveSafeEntityV1(
                    "p1:EXTRA_DECK:0",
                    false,
                    null,
                    null,
                    1,
                    PerspectiveSafeSemanticZoneV1.ExtraDeck,
                    0,
                    null,
                    PerspectiveSafePositionV1.FaceDownDefense,
                    false,
                    true)
            });

        I6C6LinkPropertyEvidenceResultV1 linkEvidence =
            I6C6LinkPropertyEvidenceExtractorV1.Extract(
                new[]
                {
                    new I6C6LiveGameplayObservationV1(
                        0,
                        GameplayMessageV1.FromSummoned(
                            8,
                            GameplayMessageKindV1.Summoned),
                        linkFrame)
                },
                new I6C6NativeLinkPropertyReferenceV1(
                    "p0:EXTRA_DECK:public:12345678:0",
                    0,
                    3,
                    new[]
                    {
                        PerspectiveSafeLinkMarkerV1.Bottom,
                        PerspectiveSafeLinkMarkerV1.Top
                    }));
        True(linkEvidence.IsSuccess, linkEvidence.ErrorCode.ToString());
        True(linkEvidence.OwnerPrivatePresent);
        True(linkEvidence.OpponentHiddenAbsent);
        True(linkEvidence.FaceUpPublicPresent);
        True(linkEvidence.LinkRatingExactMatch);
        True(linkEvidence.LinkMarkersExactMatch);

        const string counterLocator = "p0:MONSTER_ZONE:0";
        PerspectiveSafeFrameV1 counterBefore = CreatePropertyFrame(
            new[]
            {
                new PerspectiveSafeEntityV1(
                    counterLocator,
                    true,
                    12345678,
                    0,
                    0,
                    PerspectiveSafeSemanticZoneV1.MonsterZone,
                    0,
                    null,
                    PerspectiveSafePositionV1.FaceUpAttack,
                    true,
                    false,
                    null,
                    new PerspectiveSafeCardPropertiesV1(
                        counters: Array.Empty<PerspectiveSafeCounterV1>()))
            });
        PerspectiveSafeFrameV1 counterAfterAdd = CreatePropertyFrame(
            new[]
            {
                new PerspectiveSafeEntityV1(
                    counterLocator,
                    true,
                    12345678,
                    0,
                    0,
                    PerspectiveSafeSemanticZoneV1.MonsterZone,
                    0,
                    null,
                    PerspectiveSafePositionV1.FaceUpAttack,
                    true,
                    false,
                    null,
                    new PerspectiveSafeCardPropertiesV1(
                        counters: new[] { new PerspectiveSafeCounterV1(7, 3) }))
            });
        PerspectiveSafeFrameV1 counterAfterRemove = CreatePropertyFrame(
            new[]
            {
                new PerspectiveSafeEntityV1(
                    counterLocator,
                    true,
                    12345678,
                    0,
                    0,
                    PerspectiveSafeSemanticZoneV1.MonsterZone,
                    0,
                    null,
                    PerspectiveSafePositionV1.FaceUpAttack,
                    true,
                    false,
                    null,
                    new PerspectiveSafeCardPropertiesV1(
                        counters: new[] { new PerspectiveSafeCounterV1(7, 2) }))
            });
        PerspectiveSafeFrameV1 counterAfterReset = CreatePropertyFrame(
            new[]
            {
                new PerspectiveSafeEntityV1(
                    counterLocator,
                    true,
                    12345678,
                    0,
                    0,
                    PerspectiveSafeSemanticZoneV1.MonsterZone,
                    0,
                    null,
                    PerspectiveSafePositionV1.FaceUpAttack,
                    true,
                    false,
                    null,
                    new PerspectiveSafeCardPropertiesV1(
                        counters: Array.Empty<PerspectiveSafeCounterV1>()))
            });

        I6C6CounterPropertyEvidenceResultV1 counterEvidence =
            I6C6CounterPropertyEvidenceExtractorV1.Extract(
                new[]
                {
                    new I6C6LiveGameplayObservationV1(
                        0,
                        GameplayMessageV1.FromSummoned(
                            8,
                            GameplayMessageKindV1.Summoned),
                        counterBefore),
                    new I6C6LiveGameplayObservationV1(
                        1,
                        GameplayMessageV1.FromCounter(
                            101,
                            GameplayMessageKindV1.AddCounter,
                            new GameplayCounterPayloadV1(7, 0, 0x04, 0, 3)),
                        counterAfterAdd),
                    new I6C6LiveGameplayObservationV1(
                        2,
                        GameplayMessageV1.FromCounter(
                            102,
                            GameplayMessageKindV1.RemoveCounter,
                            new GameplayCounterPayloadV1(7, 0, 0x04, 0, 1)),
                        counterAfterRemove),
                    new I6C6LiveGameplayObservationV1(
                        3,
                        GameplayMessageV1.FromSet(
                            new GameplaySetPayloadV1(
                                12345678,
                                new ModernLocInfoV1(0, 0x04, 0, 0x05))),
                        counterAfterReset)
                });
        True(counterEvidence.IsSuccess, counterEvidence.ErrorCode.ToString());
        True(counterEvidence.AddObserved);
        True(counterEvidence.AddCurrentMatch);
        True(counterEvidence.RemoveObserved);
        True(counterEvidence.RemoveCurrentMatch);
        True(counterEvidence.ResetLifecycleObserved);
        Equal((uint)3, counterEvidence.Transitions[0].ExpectedAfter);
        Equal((uint)3, counterEvidence.Transitions[0].ActualAfter);
        Equal((uint)2, counterEvidence.Transitions[1].ExpectedAfter);
        Equal((uint)2, counterEvidence.Transitions[1].ActualAfter);
    }

    internal static void TestI6C6_1RedContract()
    {
        AssertPlayerToActBoundary();
        AssertOptionalPresenceIsSemantic();
        AssertEnumMappingAndUnknownRejection();
        AssertLocatorOrderingIsDeterministic();
        AssertHiddenIdentityDataIsRejected();
        AssertUnprovenPairingFailsClosed();
        AssertFullFrameAndNativeRowBoundary();
        AssertFullBoundaryDiagnosticsArePublicSafe();
        AssertFullCanonicalizationRules();
    }

    private static void AssertPlayerToActBoundary()
    {
        I6C6ComparisonResultV1 eligible =
            I6C6ComparisonFixtures.Compare(
                I6C6ScenarioFixtures.StateWithoutPlayerToAct(),
                I6C6ScenarioFixtures.StateWithoutPlayerToAct(),
                I6C6ScenarioFixtures.SupportedPairing());
        True(eligible.IsSuccess, eligible.ErrorCode.ToString());

        I6C6ComparisonResultV1 blocked =
            I6C6ComparisonFixtures.Compare(
                I6C6ScenarioFixtures.StateWithKnownPlayerToAct(),
                I6C6ScenarioFixtures.StateWithoutPlayerToAct(),
                I6C6ScenarioFixtures.SupportedPairing());
        Equal(I6C6ComparisonErrorCodeV1.BlockedPendingI6D, blocked.ErrorCode);
    }

    private static void AssertOptionalPresenceIsSemantic()
    {
        I6C6PublicSafeStateV1 absent =
            I6C6ScenarioFixtures.StateWithoutPlayerToAct();
        I6C6PublicSafeStateV1 presentZero =
            I6C6ScenarioFixtures.StateWithPresentZeroTurnPlayer();

        I6C6ComparisonResultV1 result =
            I6C6ComparisonFixtures.Compare(
                absent,
                presentZero,
                I6C6ScenarioFixtures.SupportedPairing());
        Equal(I6C6ComparisonErrorCodeV1.OptionalPresenceMismatch,
            result.ErrorCode);
    }

    private static void AssertEnumMappingAndUnknownRejection()
    {
        I6C6PublicSafeStateV1 expected =
            I6C6ScenarioFixtures.PublicState(I6C6OptionalByteV1.Absent);
        I6C6ComparisonResultV1 known =
            I6C6ComparisonFixtures.Compare(
                expected,
                I6C6ScenarioFixtures.PublicState(I6C6OptionalByteV1.Absent),
                I6C6ScenarioFixtures.SupportedPairing());
        True(known.IsSuccess, known.ErrorCode.ToString());

        I6C6ComparisonResultV1 unknown =
            I6C6ComparisonFixtures.Compare(
                expected,
                I6C6ScenarioFixtures.StateWithUnknownEnum(),
                I6C6ScenarioFixtures.SupportedPairing());
        Equal(I6C6ComparisonErrorCodeV1.UnknownEnum, unknown.ErrorCode);
    }

    private static void AssertLocatorOrderingIsDeterministic()
    {
        I6C6PublicSafeStateV1 firstConstruction =
            I6C6ScenarioFixtures.StateWithEntities(
                I6C6PublicEntityV1.Known(
                    "p0:HAND:public:12345678:1",
                    12345678),
                I6C6PublicEntityV1.Known(
                    "p0:HAND:public:12345678:0",
                    12345678));
        I6C6PublicSafeStateV1 secondConstruction =
            I6C6ScenarioFixtures.StateWithEntities(
                I6C6PublicEntityV1.Known(
                    "p0:HAND:public:12345678:0",
                    12345678),
                I6C6PublicEntityV1.Known(
                    "p0:HAND:public:12345678:1",
                    12345678));

        I6C6ComparisonResultV1 result =
            I6C6ComparisonFixtures.Compare(
                firstConstruction,
                secondConstruction,
                I6C6ScenarioFixtures.SupportedPairing());
        True(result.IsSuccess, result.ErrorCode.ToString());

        I6C6ComparisonResultV1 reverseResult =
            I6C6ComparisonFixtures.Compare(
                secondConstruction,
                firstConstruction,
                I6C6ScenarioFixtures.SupportedPairing());
        True(reverseResult.IsSuccess, reverseResult.ErrorCode.ToString());
    }

    private static void AssertHiddenIdentityDataIsRejected()
    {
        I6C6ComparisonResultV1 redacted =
            I6C6ComparisonFixtures.Compare(
                I6C6ScenarioFixtures.StateWithHiddenUnknownEntity(),
                I6C6ScenarioFixtures.StateWithHiddenUnknownEntity(),
                I6C6ScenarioFixtures.SupportedPairing());
        True(redacted.IsSuccess, redacted.ErrorCode.ToString());

        I6C6ComparisonResultV1 forbidden =
            I6C6ComparisonFixtures.Compare(
                I6C6ScenarioFixtures.StateWithForbiddenHiddenIdentityData(),
                I6C6ScenarioFixtures.StateWithForbiddenHiddenIdentityData(),
                I6C6ScenarioFixtures.SupportedPairing());
        Equal(I6C6ComparisonErrorCodeV1.HiddenIdentityData,
            forbidden.ErrorCode);
    }

    private static void AssertUnprovenPairingFailsClosed()
    {
        I6C6ComparisonResultV1 result =
            I6C6ComparisonFixtures.Compare(
                I6C6ScenarioFixtures.StateWithoutPlayerToAct(),
                I6C6ScenarioFixtures.StateWithoutPlayerToAct(),
                I6C6ScenarioFixtures.MissingSetupDescriptorPairing());
        Equal(I6C6ComparisonErrorCodeV1.UnprovenScenarioPairing,
            result.ErrorCode);
    }

    private static void AssertFullFrameAndNativeRowBoundary()
    {
        PerspectiveSafeFrameV1 frame = CreateFullFrame();
        I6C6NativePublicSafeStateRowV1 native = CreateEquivalentNativeRow();

        I6C6FullComparisonResultV1 result =
            I6C6ComparisonFixtures.CompareFrameToNative(frame, native);
        True(result.IsSuccess, result.ErrorCode.ToString());
    }

    private static void AssertFullBoundaryDiagnosticsArePublicSafe()
    {
        PerspectiveSafeFrameV1 frame = CreateFullFrame();
        I6C6NativePublicSafeStateRowV1 native = CreateEquivalentNativeRow();
        I6C6NativePublicSafeStateRowV1 changed = native with
        {
            Globals = native.Globals with
            {
                LifePoints = new uint[] { 7999, 7000 }
            }
        };

        I6C6FullComparisonResultV1 mismatch =
            I6C6ComparisonFixtures.CompareFrameToNative(frame, changed);
        Equal(I6C6FullComparisonErrorCodeV1.SemanticMismatch,
            mismatch.ErrorCode);
        Equal("globals.life_points[0]", mismatch.PublicSafeFieldPath);
        AssertDoesNotContainForbidden(
            mismatch.PublicSafeFieldPath,
            new[] { "7999", "8000", "12345678" });
    }

    private static void AssertFullCanonicalizationRules()
    {
        PerspectiveSafeFrameV1 frame = CreateFullFrame();
        I6C6NativePublicSafeStateRowV1 native = CreateEquivalentNativeRow();

        I6C6FullComparisonResultV1 reversedEvents =
            I6C6ComparisonFixtures.CompareFrameToNative(
                frame,
                native with { VisibleEvents = native.VisibleEvents.Reverse().ToArray() });
        True(reversedEvents.IsSuccess, "reversed events: " + reversedEvents.ErrorCode);

        I6C6FullComparisonResultV1 duplicateEventIndex =
            I6C6ComparisonFixtures.CompareFrameToNative(
                frame,
                native with
                {
                    VisibleEvents = new[]
                    {
                        native.VisibleEvents[0],
                        native.VisibleEvents[0]
                    }
                });
        Equal(I6C6FullComparisonErrorCodeV1.DuplicateEventIndex,
            duplicateEventIndex.ErrorCode);
        Equal("visible_events.event_index", duplicateEventIndex.PublicSafeFieldPath);

        PerspectiveSafeFrameV1 duplicateRelationshipFrame =
            CreateFullFrame(duplicateRelationship: true);
        I6C6NativePublicSafeStateRowV1 duplicateRelationshipNative =
            CreateEquivalentNativeRow(duplicateRelationship: true);
        I6C6FullComparisonResultV1 duplicateRelationships =
            I6C6ComparisonFixtures.CompareFrameToNative(
                duplicateRelationshipFrame,
                duplicateRelationshipNative);
        True(duplicateRelationships.IsSuccess,
            "duplicate relationships: " + duplicateRelationships.ErrorCode);

        PerspectiveSafeFrameV1 duplicateTargetFrame =
            CreateFullFrame(duplicateTargets: true);
        I6C6NativePublicSafeStateRowV1 duplicateTargetNative =
            CreateEquivalentNativeRow(duplicateTargets: true);
        I6C6FullComparisonResultV1 duplicateTargets =
            I6C6ComparisonFixtures.CompareFrameToNative(
                duplicateTargetFrame,
                duplicateTargetNative);
        True(duplicateTargets.IsSuccess,
            "duplicate targets: " + duplicateTargets.ErrorCode);

        I6C6FullComparisonResultV1 missingTargets =
            I6C6ComparisonFixtures.CompareFrameToNative(
                frame,
                native with
                {
                    VisibleEvents = new[]
                    {
                        native.VisibleEvents[0] with { Targets = null },
                        native.VisibleEvents[1]
                    }
                });
        Equal(I6C6FullComparisonErrorCodeV1.InvalidInput,
            missingTargets.ErrorCode);
        Equal("visible_events[0].targets", missingTargets.PublicSafeFieldPath);

        I6C6FullComparisonResultV1 contradictoryFaceFlags =
            I6C6ComparisonFixtures.CompareFrameToNative(
                frame,
                native with
                {
                    Entities = new[]
                    {
                        native.Entities[0] with { FaceUp = true, FaceDown = true },
                        native.Entities[1]
                    }
                });
        Equal(I6C6FullComparisonErrorCodeV1.InvalidInput,
            contradictoryFaceFlags.ErrorCode);
        Equal("entities[0].face_flags", contradictoryFaceFlags.PublicSafeFieldPath);
    }

    private static PerspectiveSafeFrameV1 CreateFullFrame(
        bool duplicateRelationship = false,
        bool duplicateTargets = false)
    {
        PerspectiveSafeCardPropertiesV1 properties =
            new(
                type: 1,
                attribute: 2,
                race: 4,
                attack: 1000,
                defense: 1200,
                baseAttack: 1000,
                baseDefense: 1200,
                level: 4,
                linkMarkers: new[]
                {
                    PerspectiveSafeLinkMarkerV1.Bottom,
                    PerspectiveSafeLinkMarkerV1.Top
                },
                leftScale: 1,
                rightScale: 2,
                statusFlags: 3,
                counters: new[]
                {
                    new PerspectiveSafeCounterV1(1, 0),
                    new PerspectiveSafeCounterV1(2, 1)
                });
        const string knownLocator = "p0:HAND:public:12345678:0";
        const string hiddenLocator = "p1:SPELL_TRAP_ZONE:0";

        PerspectiveSafeFrameSourceResultV1 result =
            PerspectiveSafePublicFrameSourceV1.TryCreate(
                new PerspectiveSafeFrameSourceInputV1(
                    new PerspectiveSafeGlobalsV1(
                        duelFlags: 0x1234,
                        lifePoints: new uint[] { 8000, 7000 },
                        turnPlayer: 0,
                        turnCount: 3,
                        phase: 4,
                        chainLength: 1),
                    new[]
                    {
                        new PerspectiveSafeZoneV1(
                            0,
                            PerspectiveSafeSemanticZoneV1.MainDeck,
                            2,
                            0,
                            2,
                            false),
                        new PerspectiveSafeZoneV1(
                            0,
                            PerspectiveSafeSemanticZoneV1.Hand,
                            1,
                            1,
                            0,
                            true)
                    },
                    new[]
                    {
                        new PerspectiveSafeEntityV1(
                            knownLocator,
                            identityKnown: true,
                            passcode: 12345678,
                            owner: 0,
                            controller: 0,
                            zone: PerspectiveSafeSemanticZoneV1.Hand,
                            sequence: 0,
                            overlaySequence: null,
                            position: PerspectiveSafePositionV1.Unknown,
                            faceUp: false,
                            faceDown: false,
                            printed: properties,
                            current: properties),
                        new PerspectiveSafeEntityV1(
                            hiddenLocator,
                            identityKnown: false,
                            passcode: null,
                            owner: null,
                            controller: 1,
                            zone: PerspectiveSafeSemanticZoneV1.SpellTrapZone,
                            sequence: 0,
                            overlaySequence: null,
                            position: PerspectiveSafePositionV1.FaceDownDefense,
                            faceUp: false,
                            faceDown: true)
                    },
                    duplicateRelationship
                        ? new[]
                        {
                            new PerspectiveSafeRelationshipV1(
                                PerspectiveSafeRelationshipKindV1.Target,
                                knownLocator,
                                hiddenLocator),
                            new PerspectiveSafeRelationshipV1(
                                PerspectiveSafeRelationshipKindV1.Target,
                                knownLocator,
                                hiddenLocator)
                        }
                        : new[]
                        {
                            new PerspectiveSafeRelationshipV1(
                                PerspectiveSafeRelationshipKindV1.Target,
                                knownLocator,
                                hiddenLocator)
                        },
                    new PerspectiveSafeChainStateV1(
                        1,
                        new[]
                        {
                            new PerspectiveSafeChainLinkV1(
                                index: 0,
                                activatingPlayer: 0,
                                source: knownLocator,
                                activationZone: PerspectiveSafeSemanticZoneV1.Hand,
                                effectDescription: 42,
                                targets: duplicateTargets
                                    ? new[] { knownLocator, knownLocator, hiddenLocator }
                                    : new[] { knownLocator, hiddenLocator })
                        }),
                    new[]
                    {
                        new PerspectiveSafeVisibleEventV1(
                            0,
                            PerspectiveSafeVisibleEventKindV1.TurnStarted,
                            player: 0,
                            phase: 1),
                        new PerspectiveSafeVisibleEventV1(
                            1,
                            PerspectiveSafeVisibleEventKindV1.CardMoved,
                            entityLocator: knownLocator,
                            fromZone: PerspectiveSafeSemanticZoneV1.Hand,
                            toZone: PerspectiveSafeSemanticZoneV1.MonsterZone)
                    },
                    new PerspectiveSafeMatchContextV1(
                        perspectivePlayer: 0,
                        duelFlags: 0x1234,
                        knowledge: new PerspectiveSafeKnowledgeV1(true, false),
                        ownDeck: new PerspectiveSafeDeckV1(
                            known: true,
                            mainDeck: new uint[] { 1, 2 },
                            extraDeck: new uint[] { 3 }),
                        opponentDeck: new PerspectiveSafeDeckV1(known: false))));
        True(
            result.IsSuccess,
            $"frame rejected {result.Error?.ToString() ?? "no error"};" +
                $"duplicateRelationship={duplicateRelationship};" +
                $"duplicateTargets={duplicateTargets}");
        NotNull(result.Frame);
        return result.Frame!;
    }

    private static I6C6NativePublicSafeStateRowV1 CreateEquivalentNativeRow(
        bool duplicateRelationship = false,
        bool duplicateTargets = false) =>
        new(
            new I6C6NativeGlobalsV1(
                DuelFlags: 0x1234,
                LifePoints: new uint[] { 8000, 7000 },
                PlayerToAct: null,
                TurnPlayer: 0,
                TurnCount: 3,
                Phase: 4,
                ChainLength: 1,
                Winner: null,
                WinReason: null,
                Terminal: false),
            new I6C6NativeZoneV1[]
            {
                new(0, 1, 2, 0, 2, false),
                new(0, 2, 1, 1, 0, true)
            },
            new I6C6NativeEntityV1[]
            {
                new(
                    "p0:HAND:public:12345678:0",
                    true,
                    12345678,
                    0,
                    0,
                    2,
                    0,
                    null,
                    0,
                    false,
                    false,
                    CreateEquivalentNativeProperties(),
                    CreateEquivalentNativeProperties()),
                new(
                    "p1:SPELL_TRAP_ZONE:0",
                    false,
                    null,
                    null,
                    1,
                    4,
                    0,
                    null,
                    8,
                    false,
                    true,
                    null,
                    null)
            },
            duplicateRelationship
                ? new[]
                {
                    new I6C6NativeRelationshipV1(
                        2,
                        "p0:HAND:public:12345678:0",
                        "p1:SPELL_TRAP_ZONE:0"),
                    new I6C6NativeRelationshipV1(
                        2,
                        "p0:HAND:public:12345678:0",
                        "p1:SPELL_TRAP_ZONE:0")
                }
                : new[]
                {
                    new I6C6NativeRelationshipV1(
                        2,
                        "p0:HAND:public:12345678:0",
                        "p1:SPELL_TRAP_ZONE:0")
                },
            new I6C6NativeChainV1(
                1,
                new[]
                {
                    new I6C6NativeChainLinkV1(
                        0,
                        0,
                        "p0:HAND:public:12345678:0",
                        2,
                        42,
                        duplicateTargets
                            ? new[]
                            {
                                "p0:HAND:public:12345678:0",
                                "p0:HAND:public:12345678:0",
                                "p1:SPELL_TRAP_ZONE:0"
                            }
                            : new[]
                            {
                                "p0:HAND:public:12345678:0",
                                "p1:SPELL_TRAP_ZONE:0"
                            })
                }),
            new[]
            {
                new I6C6NativeVisibleEventV1(
                    0,
                    1,
                    0,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    1,
                    Targets: Array.Empty<string>()),
                new I6C6NativeVisibleEventV1(
                    1,
                    3,
                    null,
                    "p0:HAND:public:12345678:0",
                    null,
                    2,
                    3,
                    Targets: Array.Empty<string>())
            },
            new I6C6NativeMatchContextV1(
                0,
                0x1234,
                true,
                false,
                new uint[] { 1, 2 },
                new uint[] { 3 },
                Array.Empty<uint>(),
                Array.Empty<uint>()));

    private static I6C6NativeCardPropertiesV1 CreateEquivalentNativeProperties() =>
        new(
            1,
            2,
            4,
            1000,
            1200,
            1000,
            1200,
            4,
            null,
            null,
            new byte[] { 1, 6 },
            1,
            2,
            3,
            new[]
            {
                new I6C6NativeCounterV1(1, 0),
                new I6C6NativeCounterV1(2, 1)
            });

    private static PerspectiveSafeFrameV1 CreatePropertyFrame(
        IReadOnlyList<PerspectiveSafeEntityV1> entities)
    {
        PerspectiveSafeFrameSourceResultV1 result =
            PerspectiveSafePublicFrameSourceV1.TryCreate(
                new PerspectiveSafeFrameSourceInputV1(
                    new PerspectiveSafeGlobalsV1(
                        duelFlags: 0x1234,
                        lifePoints: new uint[] { 8000, 8000 },
                        playerToAct: null,
                        turnPlayer: 0,
                        turnCount: 1,
                        phase: 1,
                        chainLength: 0),
                    new[]
                    {
                        new PerspectiveSafeZoneV1(
                            0,
                            PerspectiveSafeSemanticZoneV1.MainDeck,
                            40,
                            0,
                            40,
                            false),
                        new PerspectiveSafeZoneV1(
                            0,
                            PerspectiveSafeSemanticZoneV1.MonsterZone,
                            1,
                            1,
                            0,
                            true),
                        new PerspectiveSafeZoneV1(
                            0,
                            PerspectiveSafeSemanticZoneV1.ExtraDeck,
                            1,
                            1,
                            0,
                            false),
                        new PerspectiveSafeZoneV1(
                            1,
                            PerspectiveSafeSemanticZoneV1.ExtraDeck,
                            1,
                            0,
                            1,
                            false)
                    },
                    entities,
                    Array.Empty<PerspectiveSafeRelationshipV1>(),
                    new PerspectiveSafeChainStateV1(
                        0,
                        Array.Empty<PerspectiveSafeChainLinkV1>()),
                    new[]
                    {
                        new PerspectiveSafeVisibleEventV1(
                            0,
                            PerspectiveSafeVisibleEventKindV1.TurnStarted,
                            player: 0,
                            targets: Array.Empty<string>())
                    },
                    new PerspectiveSafeMatchContextV1(
                        perspectivePlayer: 0,
                        duelFlags: 0x1234,
                        knowledge: new PerspectiveSafeKnowledgeV1(true, false),
                        ownDeck: new PerspectiveSafeDeckV1(
                            known: true,
                            mainDeck: new uint[] { 1, 2 },
                            extraDeck: new uint[] { 3 }),
                        opponentDeck: new PerspectiveSafeDeckV1(known: false))));
        True(result.IsSuccess, result.Error?.ToString() ?? "frame rejected");
        NotNull(result.Frame);
        return result.Frame!;
    }
}
