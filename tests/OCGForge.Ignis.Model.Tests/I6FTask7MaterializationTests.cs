using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using OCGForge.Ignis.Gameplay;
using OCGForge.Ignis.Model;

namespace OCGForge.Ignis.Model.Tests;

internal static class I6FTask7MaterializationTests
{
    private const string ExpectedConfigIdentity =
        "phase6_task7_input_materialization_config.v1.20f394c888e959446fa263c3520f3dd3b1f48b3a23e58373da7153a691ab1e7a";

    private const string ExpectedConfigSha256 =
        "20f394c888e959446fa263c3520f3dd3b1f48b3a23e58373da7153a691ab1e7a";

    private const int ExpectedConfigLength = 8133;

    // Emitted by the pinned native OCGForge Task7 implementation at
    // f929de0b, using ygo::phase6::materialize_task7_input_v1 from
    // src/phase6/task7_input_materialization.cpp. The independent native
    // target tests/model_batch_layout_test.cpp and
    // tests/phase6/phase6_task7_input_materialization_test.cpp were built and
    // executed from that exact source pin. The vector values were emitted by
    // an out-of-tree driver linked to those same pinned native libraries; no
    // C# materializer was used to generate them.
    private const int NativeMinimalMaterializedLength = 3961;
    private const string NativeMinimalMaterializedSha256 =
        "c8aa8ef7e56f49d1620d5e25d04e630a1166b60c83ad6f57b216a3da026890d9";
    private const int NativePickMaterializedLength = 3957;
    private const string NativePickMaterializedSha256 =
        "34c543ef6bd271e865253f5c092a0979aa7a18bbbfd8671c1f81f8dc64656486";
    private const int NativeFinishMaterializedLength = 3953;
    private const string NativeFinishMaterializedSha256 =
        "2ba57d02a3970cb82391e650dea91086931463212749b03744bf23a8585d10b0";
    private const int NativeRichMaterializedLength = 4824;
    private const string NativeRichMaterializedSha256 =
        "7f10a33a01558694f79fd155a600e79c6cf9d39b54de369a96670796b2fbf273";

    public static void TestTask7MaterializationBridge()
    {
        TestConfigurationAndLimbs();
        TestAcceptedSourceAssociation();
        TestRaggedBatchAndReconstruction();
        TestMaterializationAndBatchComposition();
        TestFailClosedBoundaries();
    }

    private static void TestConfigurationAndLimbs()
    {
        byte[] configBytes = OcgForgeTask7InputMaterializationV1
            .CanonicalConfigurationBytes;
        string configSha256 = Convert.ToHexString(
                SHA256.HashData(configBytes))
            .ToLowerInvariant();
        Require(configBytes.Length == ExpectedConfigLength &&
                configSha256 == ExpectedConfigSha256 &&
                OcgForgeTask7InputMaterializationV1.ConfigurationIdentity ==
                    ExpectedConfigIdentity,
            "Task7 configuration differs from the frozen native identity");

        Require(U8(0).SequenceEqual(new ushort[] { 0 }) &&
                U8(byte.MaxValue).SequenceEqual(new ushort[] { 255 }) &&
                U16(0).SequenceEqual(new ushort[] { 0 }) &&
                U16(ushort.MaxValue).SequenceEqual(new ushort[] { 65535 }),
            "U8/U16 limbs are not exact");
        Require(U32(0).SequenceEqual(new ushort[] { 0, 0 }) &&
                U32(1).SequenceEqual(new ushort[] { 0, 1 }) &&
                U32(0xFFFFFF).SequenceEqual(new ushort[] { 255, 65535 }) &&
                U32(0x1000000).SequenceEqual(new ushort[] { 256, 0 }) &&
                U32(0xFFFFFFFE).SequenceEqual(new ushort[] { 65535, 65534 }) &&
                U32(uint.MaxValue).SequenceEqual(new ushort[] { 65535, 65535 }),
            "U32 limbs are not exact");
        Require(U64(0).SequenceEqual(new ushort[] { 0, 0, 0, 0 }) &&
                U64(1).SequenceEqual(new ushort[] { 0, 0, 0, 1 }) &&
                U64(0x1000000).SequenceEqual(new ushort[] { 0, 0, 256, 0 }) &&
                U64(0x100000000).SequenceEqual(new ushort[] { 0, 1, 0, 0 }) &&
                U64(0x100000001).SequenceEqual(new ushort[] { 0, 1, 0, 1 }) &&
                U64(ulong.MaxValue).SequenceEqual(
                    new ushort[] { 65535, 65535, 65535, 65535 }),
            "U64 limbs are not exact");
        Require(I32(int.MinValue).SequenceEqual(new ushort[] { 32768, 0 }) &&
                I32(-1).SequenceEqual(new ushort[] { 65535, 65535 }) &&
                I32(0).SequenceEqual(new ushort[] { 0, 0 }) &&
                I32(1).SequenceEqual(new ushort[] { 0, 1 }) &&
                I32(int.MaxValue).SequenceEqual(new ushort[] { 32767, 65535 }),
            "I32 limbs are not exact");
    }

    private static void TestAcceptedSourceAssociation()
    {
        OcgForgeModelContractBundleV1 bundle = AcceptedBundle();
        (OcgForgeLogicalModelInputV1 logical,
            OcgForgeEncodedModelInputV1 encoded,
            OcgForgeCardVocabularyV1 vocabulary) = CreateSample(
                bundle,
                rich: true,
                candidateCount: 1);
        OcgForgeTask7MaterializationSourceSampleResultV1 source =
            OcgForgeTask7InputMaterializationV1.TryCreateSourceSample(
                bundle,
                logical,
                encoded,
                vocabulary);
        Require(source.IsSuccess && source.Value is not null &&
                source.Value.ModelInputIdentity ==
                    OcgForgeModelInputIdentityV1.Create(logical, encoded, vocabulary) &&
                source.Value.CardVocabularyIdentity == vocabulary.Identity,
            "accepted I6E source association was not bound atomically");

        OcgForgeTask7MaterializationSourceSampleV1 detached = New<
            OcgForgeTask7MaterializationSourceSampleV1>(
                logical,
                encoded,
                vocabulary,
                "model_input.v1." + new string('a', 64),
                vocabulary.Identity);
        OcgForgeTask7MaterializationResultV1 rejected =
            OcgForgeTask7InputMaterializationV1.TryMaterialize(
                bundle,
                new[] { detached });
        Require(!rejected.IsSuccess && rejected.Error?.Code ==
                    OcgForgeTask7MaterializationErrorCodeV1.ModelInputIdentityMismatch,
            "caller-supplied model identity was trusted");

        (OcgForgeLogicalModelInputV1 otherLogical,
            OcgForgeEncodedModelInputV1 otherEncoded,
            OcgForgeCardVocabularyV1 otherVocabulary) = CreateSample(
                bundle,
                rich: false,
                candidateCount: 1);
        OcgForgeTask7MaterializationSourceSampleResultV1 detachedTriple =
            OcgForgeTask7InputMaterializationV1.TryCreateSourceSample(
                bundle,
                logical,
                otherEncoded,
                otherVocabulary);
        Require(!detachedTriple.IsSuccess && detachedTriple.Error?.Code ==
                    OcgForgeTask7MaterializationErrorCodeV1.ModelInputIdentityMismatch,
            "logical/encoded source triple was detached without rejection");
        _ = otherLogical;
    }

    private static void TestRaggedBatchAndReconstruction()
    {
        OcgForgeModelContractBundleV1 bundle = AcceptedBundle();
        OcgForgeEncodedModelInputV1 first = CreateSample(
                bundle,
                rich: false,
                candidateCount: 1)
            .Encoded;
        OcgForgeEncodedModelInputV1 second = CreateSample(
                bundle,
                rich: true,
                candidateCount: 3)
            .Encoded;
        OcgForgeRaggedModelBatchResultV1 result =
            OcgForgeModelBatchLayoutV1.MakeRagged(new[] { first, second });
        Require(result.IsSuccess && result.Value is not null,
            result.Error?.ToString() ?? "RAGGED batch construction failed");
        OcgForgeRaggedModelBatchV1 batch = result.Value!;
        Require(batch.BatchSize == 2 && batch.Samples.Count == 2 &&
                batch.CandidateOffsets.SequenceEqual(new ulong[] { 0, 1, 4 }) &&
                batch.CandidateRows.Count == 4 &&
                batch.CandidateRoutingKeys.Count == 4 &&
                batch.CandidateOptionalPresenceMasks.Count == 4 &&
                batch.CandidateRoutingKeys.SequenceEqual(
                    first.RoutingKeys.Concat(second.RoutingKeys)),
            "candidate RAGGED offsets/order are not N-to-N");
        Require(batch.ZoneOffsets.Count == 3 &&
                batch.EntityOffsets.Count == 3 &&
                batch.VisibleEventOffsets.Count == 3 &&
                batch.LifePointOffsets.SequenceEqual(new ulong[] { 0, 2, 4 }) &&
                batch.RelationshipOffsets.Count == 3 &&
                batch.ChainLinkOffsets.Count == 3 &&
                batch.DecisionContextReferenceOffsets.Count == 3 &&
                batch.PublicLocatorTokenOffsets.Count == 3 &&
                batch.OwnDeckPasscodeOffsets.Count == 3 &&
                batch.OpponentDeckPasscodeOffsets.Count == 3 &&
                batch.OwnExtraDeckPasscodeOffsets.Count == 3 &&
                batch.OpponentExtraDeckPasscodeOffsets.Count == 3,
            "RAGGED offsets are not B+1 source offsets");
        Require(batch.CandidateOptionalPresenceMasks[0].Choice == 1 &&
                batch.CandidateOptionalPresenceMasks[0].SourceReference == 0 &&
                batch.CandidateOptionalPresenceMasks[1].Choice == 1 &&
                batch.CandidateOptionalPresenceMasks[1].SourceReference == 0,
            "candidate optional presence did not follow encoded source fields");

        OcgForgeRaggedModelSampleResultV1 firstReconstructed =
            OcgForgeModelBatchLayoutV1.TryReconstruct(batch, 0);
        OcgForgeRaggedModelSampleResultV1 secondReconstructed =
            OcgForgeModelBatchLayoutV1.TryReconstruct(batch, 1);
        Require(firstReconstructed.IsSuccess && secondReconstructed.IsSuccess &&
                firstReconstructed.Value!.CanonicalBytes.SequenceEqual(first.CanonicalBytes) &&
                secondReconstructed.Value!.CanonicalBytes.SequenceEqual(second.CanonicalBytes),
            "RAGGED reconstruction changed encoded canonical bytes");

        OcgForgeEncodedModelInputV1 nativeBatchFirst = CreateSample(
                bundle,
                rich: false,
                candidateCount: 1)
            .Encoded;
        OcgForgeEncodedModelInputV1 nativeBatchSecond = CreateSample(
                bundle,
                rich: false,
                candidateCount: 2)
            .Encoded;
        OcgForgeRaggedModelBatchResultV1 nativeBatch =
            OcgForgeModelBatchLayoutV1.MakeRagged(
                new[] { nativeBatchFirst, nativeBatchSecond });
        Require(nativeBatch.IsSuccess && nativeBatch.Value is not null &&
                nativeBatch.Value.CandidateOffsets.SequenceEqual(
                    new ulong[] { 0, 1, 3 }) &&
                nativeBatch.Value.ZoneOffsets.SequenceEqual(
                    new ulong[] { 0, 0, 0 }) &&
                nativeBatch.Value.EntityOffsets.SequenceEqual(
                    new ulong[] { 0, 0, 0 }) &&
                nativeBatch.Value.LifePointOffsets.SequenceEqual(
                    new ulong[] { 0, 2, 4 }),
            "multi-sample RAGGED offsets differ from native layout KAT");
    }

    private static void TestMaterializationAndBatchComposition()
    {
        OcgForgeModelContractBundleV1 bundle = AcceptedBundle();
        OcgForgeTask7MaterializationSourceSampleV1 minimal =
            CreateSource(bundle, rich: false, candidateCount: 1);
        OcgForgeTask7MaterializationSourceSampleV1 rich =
            CreateSource(bundle, rich: true, candidateCount: 1);
        OcgForgeTask7MaterializationSourceSampleV1 multiCandidate =
            CreateSource(bundle, rich: false, candidateCount: 3);
        OcgForgeTask7MaterializationSourceSampleV1 pick =
            CreateContinuationSource(bundle, terminal: false);
        OcgForgeTask7MaterializationSourceSampleV1 finish =
            CreateContinuationSource(bundle, terminal: true);
        OcgForgeTask7MaterializationResultV1 one =
            OcgForgeTask7InputMaterializationV1.TryMaterialize(
                bundle,
                new[] { minimal });
        OcgForgeTask7MaterializationResultV1 two =
            OcgForgeTask7InputMaterializationV1.TryMaterialize(
                bundle,
                new[] { minimal, rich });
        OcgForgeTask7MaterializationResultV1 multi =
            OcgForgeTask7InputMaterializationV1.TryMaterialize(
                bundle,
                new[] { minimal, multiCandidate });
        Require(one.IsSuccess && one.Value is not null &&
                two.IsSuccess && two.Value is not null &&
                multi.IsSuccess && multi.Value is not null &&
                one.Value.SchemaId == OcgForgeTask7InputMaterializationV1.SchemaId &&
                one.Value.ConfigurationIdentity == ExpectedConfigIdentity &&
                one.Value.Samples.Count == 1 && two.Value.Samples.Count == 2 &&
                multi.Value.Samples.Count == 2 &&
                one.Value.Samples[0].CandidateCount == 1 &&
                multi.Value.Samples[1].CandidateCount == 3 &&
                two.Value.Samples[0].CanonicalBytes.SequenceEqual(
                    one.Value.Samples[0].CanonicalBytes) &&
                multi.Value.Samples[0].CanonicalBytes.SequenceEqual(
                    one.Value.Samples[0].CanonicalBytes),
            "Task7 materialization did not preserve source/batch semantics");
        Require(one.Value!.Samples[0].CanonicalBytes.Length > 0 &&
                one.Value.Samples[0].ModelInputIdentity == minimal.ModelInputIdentity &&
                one.Value.Samples[0].CardVocabularyIdentity ==
                    minimal.CardVocabularyIdentity,
            "Task7 materialized sample lost source identities");
        OcgForgeTask7MaterializationResultV1 pickResult =
            OcgForgeTask7InputMaterializationV1.TryMaterialize(bundle, new[] { pick });
        OcgForgeTask7MaterializationResultV1 finishResult =
            OcgForgeTask7InputMaterializationV1.TryMaterialize(bundle, new[] { finish });
        Require(pickResult.IsSuccess && pickResult.Value is not null &&
                finishResult.IsSuccess && finishResult.Value is not null &&
                !pick.Encoded.CandidateFeatures[0].SubmitsEngineResponse &&
                finish.Encoded.CandidateFeatures[0].SubmitsEngineResponse,
            "continuation materialization setup was not preserved");
        Require(NativeMaterializationKatsMatch(
                one.Value.Samples[0],
                NativeMinimalMaterializedLength,
                NativeMinimalMaterializedSha256) &&
                NativeMaterializationKatsMatch(
                    pickResult.Value!.Samples[0],
                    NativePickMaterializedLength,
                    NativePickMaterializedSha256) &&
                NativeMaterializationKatsMatch(
                    finishResult.Value!.Samples[0],
                    NativeFinishMaterializedLength,
                    NativeFinishMaterializedSha256) &&
                NativeMaterializationKatsMatch(
                    two.Value!.Samples[1],
                    NativeRichMaterializedLength,
                    NativeRichMaterializedSha256),
            "Task7 materialization differs from pinned native KAT");
        OcgForgeRaggedModelBatchV1 pickRagged =
            OcgForgeModelBatchLayoutV1.MakeRagged(new[] { pick.Encoded }).Value!;
        OcgForgeRaggedModelBatchV1 finishRagged =
            OcgForgeModelBatchLayoutV1.MakeRagged(new[] { finish.Encoded }).Value!;
        Require(pickRagged.CandidateOptionalPresenceMasks[0].SourceIndex == 1 &&
                pick.Encoded.CandidateFeatures[0].SourceIndex == 0 &&
                finishRagged.CandidateOptionalPresenceMasks[0].SourceIndex == 0 &&
                finish.Encoded.CandidateFeatures[0].SourceIndex is null,
            "ABSENT and PRESENT(0) candidate fields were conflated");
        OcgForgeRaggedModelBatchV1 evidenceRagged =
            OcgForgeModelBatchLayoutV1.MakeRagged(
                    new[] { minimal.Encoded, multiCandidate.Encoded })
                .Value!;
        Console.WriteLine($"I6F_CONFIG_IDENTITY={ExpectedConfigIdentity}");
        Console.WriteLine($"I6F_SAMPLE_COUNT={multi.Value!.Samples.Count}");
        Console.WriteLine($"I6F_SAMPLE_0_SHA256={Digest(multi.Value.Samples[0].CanonicalBytes)}");
        Console.WriteLine($"I6F_RAGGED_CANDIDATE_OFFSETS={string.Join(',', evidenceRagged.CandidateOffsets)}");

        foreach (int candidateCount in new[] { 1, 24, 25, 129 })
        {
            OcgForgeTask7MaterializationSourceSampleV1 source =
                CreateSource(bundle, rich: false, candidateCount);
            OcgForgeTask7MaterializationResultV1 materialized =
                OcgForgeTask7InputMaterializationV1.TryMaterialize(
                    bundle,
                    new[] { source });
            Require(materialized.IsSuccess && materialized.Value is not null &&
                    materialized.Value.Samples[0].CandidateCount == candidateCount,
                $"candidate count {candidateCount} was not preserved");
        }
    }

    private static void TestFailClosedBoundaries()
    {
        OcgForgeModelContractBundleV1 bundle = AcceptedBundle();
        OcgForgeTask7MaterializationSourceSampleV1 source =
            CreateSource(bundle, rich: true, candidateCount: 1);
        OcgForgeRaggedModelBatchResultV1 raggedResult =
            OcgForgeModelBatchLayoutV1.MakeRagged(new[] { source.Encoded });
        Require(raggedResult.IsSuccess && raggedResult.Value is not null,
            "negative RAGGED fixture setup failed");
        OcgForgeRaggedModelBatchV1 valid = raggedResult.Value!;

        OcgForgeRaggedModelBatchV1 invalidOffsets = CopyRagged(
            valid,
            candidateOffsets: new ulong[] { 0, 2 });
        Require(!OcgForgeModelBatchLayoutV1.TryReconstruct(invalidOffsets, 0)
                    .IsSuccess,
            "non-monotonic/final-count offsets were accepted");

        OcgForgeTask7MaterializationSourceBatchResultV1 sourceBatchResult =
            OcgForgeTask7InputMaterializationV1.TryCreateSourceBatch(
                bundle,
                new[] { source });
        Require(sourceBatchResult.IsSuccess && sourceBatchResult.Value is not null,
            "accepted source batch was not created");
        OcgForgeTask7MaterializationSourceBatchV1 detachedRaggedBatch = New<
            OcgForgeTask7MaterializationSourceBatchV1>(
                invalidOffsets,
                sourceBatchResult.Value!.Samples);
        OcgForgeTask7MaterializationResultV1 detachedRagged =
            OcgForgeTask7InputMaterializationV1.TryMaterialize(
                bundle,
                detachedRaggedBatch);
        Require(!detachedRagged.IsSuccess && detachedRagged.Error?.Code ==
                    OcgForgeTask7MaterializationErrorCodeV1.RaggedReconstructionMismatch,
            "externally supplied ragged layout was not checked against its source");

        OcgForgeRaggedModelBatchV1 overflowOffsets = CopyRagged(
            valid,
            candidateOffsets: new ulong[] { 0, ulong.MaxValue });
        Require(!OcgForgeModelBatchLayoutV1.TryReconstruct(overflowOffsets, 0)
                    .IsSuccess,
            "overflow offsets were accepted");

        OcgForgeCandidateOptionalPresenceV1[] malformedPresence =
            valid.CandidateOptionalPresenceMasks.ToArray();
        malformedPresence[0] = malformedPresence[0] with { Choice = 2 };
        OcgForgeRaggedModelBatchV1 presenceMismatch = CopyRagged(
            valid,
            candidateOptionalPresenceMasks: malformedPresence);
        Require(!OcgForgeModelBatchLayoutV1.TryReconstruct(presenceMismatch, 0)
                    .IsSuccess,
            "invalid optional presence mask was accepted");

        string[] detachedRouting = valid.CandidateRoutingKeys.ToArray();
        detachedRouting[0] += "00";
        OcgForgeRaggedModelBatchV1 routingMismatch = CopyRagged(
            valid,
            candidateRoutingKeys: detachedRouting);
        Require(!OcgForgeModelBatchLayoutV1.TryReconstruct(routingMismatch, 0)
                    .IsSuccess,
            "detached routing sidecar was accepted");

        OcgForgeEncodedModelInputV1 twoCandidateEncoded = CreateSample(
                bundle,
                rich: false,
                candidateCount: 2)
            .Encoded;
        OcgForgeRaggedModelBatchV1 duplicateBase =
            OcgForgeModelBatchLayoutV1.MakeRagged(new[] { twoCandidateEncoded }).Value!;
        string[] duplicateRouting = duplicateBase.CandidateRoutingKeys.ToArray();
        duplicateRouting[1] = duplicateRouting[0];
        OcgForgeRaggedModelBatchV1 duplicateRoutingBatch = CopyRagged(
            duplicateBase,
            candidateRoutingKeys: duplicateRouting);
        Require(!OcgForgeModelBatchLayoutV1.TryReconstruct(duplicateRoutingBatch, 0)
                    .IsSuccess,
            "duplicate routing keys were accepted");

        OcgForgeEncodedCandidateV1[] invalidReferenceRows =
            valid.CandidateRows.ToArray();
        invalidReferenceRows[0] = invalidReferenceRows[0] with
        {
            SourceReference = new OcgForgeEncodedCardReferenceV1(
                0,
                new OcgForgeEncodedCurrentReferenceV1(999, null))
        };
        OcgForgeRaggedModelBatchV1 invalidReference = CopyRagged(
            valid,
            candidateRows: invalidReferenceRows);
        Require(!OcgForgeModelBatchLayoutV1.TryReconstruct(invalidReference, 0)
                    .IsSuccess,
            "out-of-range reference ordinal was accepted");

        OcgForgeTask7MaterializationSourceSampleV1 detachedVocabulary = New<
            OcgForgeTask7MaterializationSourceSampleV1>(
                source.Logical,
                source.Encoded,
                source.Vocabulary,
                source.ModelInputIdentity,
                "model_card_vocabulary.v1." + new string('b', 64));
        OcgForgeTask7MaterializationResultV1 detached =
            OcgForgeTask7InputMaterializationV1.TryMaterialize(
                bundle,
                new[] { detachedVocabulary });
        Require(!detached.IsSuccess && detached.Error?.Code ==
                    OcgForgeTask7MaterializationErrorCodeV1.CardVocabularyMismatch,
            "detached vocabulary identity was accepted");

        OcgForgeTask7MaterializationResultV1 wrongBundleResult =
            OcgForgeTask7InputMaterializationV1.TryMaterialize(
                null,
                new[] { source });
        Require(!wrongBundleResult.IsSuccess && wrongBundleResult.Error?.Code ==
                    OcgForgeTask7MaterializationErrorCodeV1.UnknownSchema,
            "wrong Task7 bundle configuration was accepted");
    }

    private static OcgForgeTask7MaterializationSourceSampleV1 CreateSource(
        OcgForgeModelContractBundleV1 bundle,
        bool rich,
        int candidateCount) =>
        CreateSourceResult(bundle, rich, candidateCount).Value!;

    private static OcgForgeTask7MaterializationSourceSampleV1 CreateContinuationSource(
        OcgForgeModelContractBundleV1 bundle,
        bool terminal)
    {
        PerspectiveSafeFrameV1 frame = (PerspectiveSafeFrameV1)Invoke(
            "CreateMinimalFrame");
        FlatPromptCardSelectionPublicContextV1 context = New<
            FlatPromptCardSelectionPublicContextV1>((byte)0, 1U, 2U, false);
        FlatPublicCandidateDescriptorV1 candidate = terminal
            ? New<FlatPromptFinishPublicCandidateV1>("task7.finish")
            : New<FlatPromptCardSelectionAnonymousCandidateV1>("task7.pick", 0);
        OcgForgePublicDecisionContextV1 decision = (OcgForgePublicDecisionContextV1)
            Invoke("CreateAcceptedDecision", frame, context, new[] { candidate });
        OcgForgeLogicalModelInputV1 logical = (OcgForgeLogicalModelInputV1)
            Invoke("RequireLogical", decision);
        OcgForgeCardVocabularyV1 vocabulary =
            OcgForgeCardVocabularyV1.TryCreate(Array.Empty<uint>()).Value!;
        OcgForgeEncodedModelInputV1 encoded =
            OcgForgeEncodedModelInputBridgeV1.TryCreate(logical, vocabulary).Value!;
        OcgForgeTask7MaterializationSourceSampleResultV1 result =
            OcgForgeTask7InputMaterializationV1.TryCreateSourceSample(
                bundle,
                logical,
                encoded,
                vocabulary);
        Require(result.IsSuccess && result.Value is not null,
            result.Error?.ToString() ?? "continuation source was rejected");
        return result.Value!;
    }

    private static OcgForgeTask7MaterializationSourceSampleResultV1 CreateSourceResult(
        OcgForgeModelContractBundleV1 bundle,
        bool rich,
        int candidateCount)
    {
        (OcgForgeLogicalModelInputV1 logical,
            OcgForgeEncodedModelInputV1 encoded,
            OcgForgeCardVocabularyV1 vocabulary) = CreateSample(
                bundle,
                rich,
                candidateCount);
        OcgForgeTask7MaterializationSourceSampleResultV1 result =
            OcgForgeTask7InputMaterializationV1.TryCreateSourceSample(
                bundle,
                logical,
                encoded,
                vocabulary);
        Require(result.IsSuccess && result.Value is not null,
            result.Error?.ToString() ?? "source association was rejected");
        return result;
    }

    private static (OcgForgeLogicalModelInputV1 Logical,
        OcgForgeEncodedModelInputV1 Encoded,
        OcgForgeCardVocabularyV1 Vocabulary) CreateSample(
        OcgForgeModelContractBundleV1 bundle,
        bool rich,
        int candidateCount)
    {
        PerspectiveSafeFrameV1 frame = rich
            ? (PerspectiveSafeFrameV1)Invoke("CreateRichFrame", (byte?)0)
            : (PerspectiveSafeFrameV1)Invoke("CreateMinimalFrame");
        FlatPromptPublicContextV1 context;
        FlatPublicCandidateDescriptorV1[] candidates;
        if (candidateCount == 1 && !rich)
        {
            context = (FlatPromptPublicContextV1)Invoke("CreateYesNoContext");
            candidates = new[]
            {
                New<FlatYesNoPublicCandidateDescriptorV1>(
                    "task7.yes-no",
                    FlatPromptChoiceKindV1.No)
            };
        }
        else if (rich && candidateCount == 1)
        {
            context = (FlatPromptPublicContextV1)Invoke("CreateChainContext");
            candidates = new[]
            {
                New<FlatChainPublicCandidateDescriptorV1>(
                    "task7.chain",
                    0,
                    Locator("p0:MONSTER_ZONE:0"),
                    42UL,
                    (byte)0)
            };
        }
        else
        {
            context = New<FlatPromptOptionPublicContextV1>((byte)0);
            candidates = Enumerable.Range(0, candidateCount)
                .Select(index => (FlatPublicCandidateDescriptorV1)
                    New<FlatOptionPublicCandidateDescriptorV1>(
                        $"task7.option.{index}",
                        index,
                        (ulong)(100 + index)))
                .ToArray();
        }

        OcgForgePublicDecisionContextV1 decision = (OcgForgePublicDecisionContextV1)
            Invoke("CreateAcceptedDecision", frame, context, candidates);
        OcgForgeLogicalModelInputV1 logical = (OcgForgeLogicalModelInputV1)
            Invoke("RequireLogical", decision);
        uint[] passcodes = rich ? new uint[] { 1001, 2002, 3003 } : Array.Empty<uint>();
        OcgForgeCardVocabularyV1 vocabulary =
            OcgForgeCardVocabularyV1.TryCreate(passcodes).Value!;
        OcgForgeEncodedModelInputV1 encoded =
            OcgForgeEncodedModelInputBridgeV1.TryCreate(logical, vocabulary).Value!;
        return (logical, encoded, vocabulary);
    }

    private static OcgForgeRaggedModelBatchV1 CopyRagged(
        OcgForgeRaggedModelBatchV1 source,
        IEnumerable<ulong>? candidateOffsets = null,
        IEnumerable<OcgForgeCandidateOptionalPresenceV1>? candidateOptionalPresenceMasks = null,
        IEnumerable<string>? candidateRoutingKeys = null,
        IEnumerable<OcgForgeEncodedCandidateV1>? candidateRows = null) =>
        new(
            source.SchemaId,
            source.BatchSize,
            source.Samples,
            candidateOffsets ?? source.CandidateOffsets,
            source.ZoneOffsets,
            source.EntityOffsets,
            source.RelationshipOffsets,
            source.ChainLinkOffsets,
            source.VisibleEventOffsets,
            source.DecisionContextReferenceOffsets,
            source.PublicLocatorTokenOffsets,
            source.LifePointOffsets,
            source.OwnDeckPasscodeOffsets,
            source.OpponentDeckPasscodeOffsets,
            source.OwnExtraDeckPasscodeOffsets,
            source.OpponentExtraDeckPasscodeOffsets,
            candidateRows ?? source.CandidateRows,
            candidateOptionalPresenceMasks ?? source.CandidateOptionalPresenceMasks,
            candidateRoutingKeys ?? source.CandidateRoutingKeys,
            source.Zones,
            source.Entities,
            source.Relationships,
            source.ChainLinks,
            source.VisibleEvents,
            source.DecisionContextReferenceOrdinals,
            source.PublicLocatorTokens,
            source.LifePoints,
            source.OwnDeckPasscodeIds,
            source.OpponentDeckPasscodeIds,
            source.OwnExtraDeckPasscodeIds,
            source.OpponentExtraDeckPasscodeIds);

    private static OcgForgeModelContractBundleV1 AcceptedBundle() =>
        (OcgForgeModelContractBundleV1)Invoke("AcceptedBundle");

    private static PublicSemanticLocatorV1 Locator(string text)
    {
        Require(PublicSemanticLocatorV1.TryParse(text, out PublicSemanticLocatorV1? value) &&
                value is not null,
            "invalid test locator");
        return value!;
    }

    private static object Invoke(string methodName, params object?[] arguments)
    {
        MethodInfo[] methods = typeof(I6EModelInputTests)
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Where(method => method.Name == methodName &&
                method.GetParameters().Length == arguments.Length)
            .ToArray();
        Require(methods.Length == 1, $"ambiguous I6E fixture method {methodName}");
        return methods[0].Invoke(null, arguments) ??
            throw new InvalidOperationException($"fixture method {methodName} returned null");
    }

    private static T New<T>(params object?[] arguments)
        where T : class
    {
        object? value = Activator.CreateInstance(
            typeof(T),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: arguments,
            culture: CultureInfo.InvariantCulture);
        return (T)(value ?? throw new InvalidOperationException(
            $"could not construct {typeof(T).Name}"));
    }

    private static ushort[] U8(byte value) =>
        OcgForgeTask7InputMaterializationV1.U8Limbs(value);

    private static ushort[] U16(ushort value) =>
        OcgForgeTask7InputMaterializationV1.U16Limbs(value);

    private static ushort[] U32(uint value) =>
        OcgForgeTask7InputMaterializationV1.U32Limbs(value);

    private static ushort[] U64(ulong value) =>
        OcgForgeTask7InputMaterializationV1.U64Limbs(value);

    private static ushort[] I32(int value) =>
        OcgForgeTask7InputMaterializationV1.I32Limbs(value);

    private static string Digest(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static bool NativeMaterializationKatsMatch(
        OcgForgeTask7MaterializedSampleV1 sample,
        int expectedLength,
        string expectedSha256) =>
        sample.CanonicalBytes.Length == expectedLength &&
        Digest(sample.CanonicalBytes) == expectedSha256;

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
