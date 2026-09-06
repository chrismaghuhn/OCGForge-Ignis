using System.Collections;
using System.Reflection;
using OCGForge.Ignis.Gameplay;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I6CPublicFrameSourceTests
{
    internal static void TestI6C1SourceContainer()
    {
        Run("complete structural value", AssertCompleteStructuralValue);
        Run("missing sections fail closed", AssertMissingSectionsFailClosed);
        Run("invalid players and enums fail closed", AssertInvalidValuesFailClosed);
        Run("duplicate and ordered values fail closed", AssertDuplicateAndOrderFailures);
        Run("optional presence is semantic", AssertOptionalPresenceIsSemantic);
        Run("deep value ownership", AssertDeepValueOwnership);
        Run("read-only public collections", AssertReadOnlyCollections);
        Run("equivalent values are deterministic", AssertEquivalentValuesAreDeterministic);
        Run("structured failures contain no sensitive data", AssertStructuredFailureSurface);
        Run("locator bytes are printable", AssertPrintableLocatorBoundaries);
        Run("cross-section invariants are enforced", AssertCrossSectionInvariants);
        Run("first invalid invariant is diagnosed", AssertFirstInvalidInvariant);
        Run("public surface has no private escape hatch", AssertPublicSurface);
    }

    private static void Run(string name, Action test)
    {
        try
        {
            test();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                name + ": " + exception.Message,
                exception);
        }
    }

    private static void AssertCompleteStructuralValue()
    {
        PerspectiveSafeFrameSourceResultV1 result =
            PerspectiveSafePublicFrameSourceV1.TryCreate(CreateValidInput());

        True(result.IsSuccess, result.Error?.ToString() ?? "source value rejected");
        NotNull(result.Frame);
        Null(result.Error);
        Equal(2, result.Frame!.Globals.LifePoints.Count);
        Equal(2, result.Frame.Zones.Count);
        Equal(2, result.Frame.Entities.Count);
        Equal(1, result.Frame.Relationships.Count);
        Equal((uint)1, result.Frame.Chain.Length);
        Equal(2, result.Frame.VisibleEvents.Count);
        Equal((byte)0, result.Frame.MatchContext.PerspectivePlayer);
    }

    private static void AssertMissingSectionsFailClosed()
    {
        PerspectiveSafeGlobalsV1 globals = CreateValidGlobals();
        IReadOnlyList<PerspectiveSafeZoneV1> zones = CreateValidZones();
        IReadOnlyList<PerspectiveSafeEntityV1> entities = CreateValidEntities();
        IReadOnlyList<PerspectiveSafeRelationshipV1> relationships =
            CreateValidRelationships();
        PerspectiveSafeChainStateV1 chain = CreateValidChain();
        IReadOnlyList<PerspectiveSafeVisibleEventV1> events =
            CreateValidEvents();
        PerspectiveSafeMatchContextV1 context = CreateValidMatchContext();

        AssertFailure(
            new PerspectiveSafeFrameSourceInputV1(
                null,
                zones,
                entities,
                relationships,
                chain,
                events,
                context),
            PerspectiveSafeFrameSourceErrorCodeV1.MissingGlobals);
        AssertFailure(
            new PerspectiveSafeFrameSourceInputV1(
                globals,
                null,
                entities,
                relationships,
                chain,
                events,
                context),
            PerspectiveSafeFrameSourceErrorCodeV1.MissingZones);
        AssertFailure(
            new PerspectiveSafeFrameSourceInputV1(
                globals,
                zones,
                null,
                relationships,
                chain,
                events,
                context),
            PerspectiveSafeFrameSourceErrorCodeV1.MissingEntities);
        AssertFailure(
            new PerspectiveSafeFrameSourceInputV1(
                globals,
                zones,
                entities,
                null,
                chain,
                events,
                context),
            PerspectiveSafeFrameSourceErrorCodeV1.MissingRelationships);
        AssertFailure(
            new PerspectiveSafeFrameSourceInputV1(
                globals,
                zones,
                entities,
                relationships,
                null,
                events,
                context),
            PerspectiveSafeFrameSourceErrorCodeV1.MissingChain);
        AssertFailure(
            new PerspectiveSafeFrameSourceInputV1(
                globals,
                zones,
                entities,
                relationships,
                chain,
                null,
                context),
            PerspectiveSafeFrameSourceErrorCodeV1.MissingVisibleEvents);
        AssertFailure(
            new PerspectiveSafeFrameSourceInputV1(
                globals,
                zones,
                entities,
                relationships,
                chain,
                events,
                null),
            PerspectiveSafeFrameSourceErrorCodeV1.MissingMatchContext);
    }

    private static void AssertInvalidValuesFailClosed()
    {
        PerspectiveSafeFrameSourceInputV1 invalidPlayer = CreateInput(
            new PerspectiveSafeGlobalsV1(
                duelFlags: 0,
                lifePoints: new uint[] { 8000, 7000 },
                playerToAct: 2,
                turnPlayer: 0),
            CreateValidZones(),
            CreateValidEntities(),
            CreateValidRelationships(),
            CreateValidChain(),
            CreateValidEvents(),
            CreateValidMatchContext());
        AssertFailure(
            invalidPlayer,
            PerspectiveSafeFrameSourceErrorCodeV1.InvalidPlayer);

        PerspectiveSafeEntityV1 unknownZone = new(
            "entity-a",
            identityKnown: true,
            passcode: 7,
            owner: 0,
            controller: 0,
            zone: (PerspectiveSafeSemanticZoneV1)255,
            sequence: null,
            overlaySequence: null,
            position: PerspectiveSafePositionV1.Unknown,
            faceUp: false,
            faceDown: false);
        AssertFailure(
            CreateInput(
                CreateValidGlobals(),
                CreateValidZones(),
                new[] { unknownZone },
                CreateValidRelationships(),
                CreateValidChain(),
                CreateValidEvents(),
                CreateValidMatchContext()),
            PerspectiveSafeFrameSourceErrorCodeV1.UnknownEnum);

        PerspectiveSafeEntityV1 contradictory = new(
            "entity-a",
            identityKnown: false,
            passcode: null,
            owner: 0,
            controller: 0,
            zone: PerspectiveSafeSemanticZoneV1.Hand,
            sequence: null,
            overlaySequence: null,
            position: PerspectiveSafePositionV1.FaceUpAttack,
            faceUp: true,
            faceDown: true);
        AssertFailure(
            CreateInput(
                CreateValidGlobals(),
                CreateValidZones(),
                new[] { contradictory },
                CreateValidRelationships(),
                CreateValidChain(),
                CreateValidEvents(),
                CreateValidMatchContext()),
            PerspectiveSafeFrameSourceErrorCodeV1.ContradictoryEntityState);
    }

    private static void AssertDuplicateAndOrderFailures()
    {
        PerspectiveSafeEntityV1 first = CreateValidEntities()[0];
        PerspectiveSafeEntityV1 duplicate = CreateValidEntities()[0];
        AssertFailure(
            CreateInput(
                CreateValidGlobals(),
                CreateValidZones(),
                new[] { first, duplicate },
                CreateValidRelationships(),
                CreateValidChain(),
                CreateValidEvents(),
                CreateValidMatchContext()),
            PerspectiveSafeFrameSourceErrorCodeV1.DuplicateLocator);

        PerspectiveSafeVisibleEventV1 duplicateEvent =
            new(0, PerspectiveSafeVisibleEventKindV1.PhaseChanged);
        AssertFailure(
            CreateInput(
                CreateValidGlobals(),
                CreateValidZones(),
                CreateValidEntities(),
                CreateValidRelationships(),
                CreateValidChain(),
                new[] { duplicateEvent, duplicateEvent },
                CreateValidMatchContext()),
            PerspectiveSafeFrameSourceErrorCodeV1.DuplicateEventIndex);

        PerspectiveSafeVisibleEventV1 later =
            new(2, PerspectiveSafeVisibleEventKindV1.PhaseChanged);
        PerspectiveSafeVisibleEventV1 earlier =
            new(1, PerspectiveSafeVisibleEventKindV1.TurnStarted);
        AssertFailure(
            CreateInput(
                CreateValidGlobals(),
                CreateValidZones(),
                CreateValidEntities(),
                CreateValidRelationships(),
                CreateValidChain(),
                new[] { later, earlier },
                CreateValidMatchContext()),
            PerspectiveSafeFrameSourceErrorCodeV1.EventIndexNotIncreasing);

        PerspectiveSafeChainStateV1 mismatchedChain =
            new(2, new[] { CreateValidChain().Links[0] });
        AssertFailure(
            CreateInput(
                CreateValidGlobals(),
                CreateValidZones(),
                CreateValidEntities(),
                CreateValidRelationships(),
                mismatchedChain,
                CreateValidEvents(),
                CreateValidMatchContext()),
            PerspectiveSafeFrameSourceErrorCodeV1.ChainLengthMismatch);
    }

    private static void AssertOptionalPresenceIsSemantic()
    {
        PerspectiveSafeFrameSourceInputV1 absent = CreateInput(
            new PerspectiveSafeGlobalsV1(
                duelFlags: 0x1234,
                lifePoints: new uint[] { 8000, 7000 },
                turnPlayer: 0,
                chainLength: 1),
            CreateValidZones(),
            CreateValidEntities(),
            CreateValidRelationships(),
            CreateValidChain(),
            CreateValidEvents(),
            CreateValidMatchContext());
        PerspectiveSafeFrameSourceInputV1 presentZero = CreateInput(
            new PerspectiveSafeGlobalsV1(
                duelFlags: 0x1234,
                lifePoints: new uint[] { 8000, 7000 },
                playerToAct: 0,
                turnPlayer: 0,
                chainLength: 1),
            CreateValidZones(),
            CreateValidEntities(),
            CreateValidRelationships(),
            CreateValidChain(),
            CreateValidEvents(),
            CreateValidMatchContext());

        PerspectiveSafeFrameV1 absentFrame = Accept(absent);
        PerspectiveSafeFrameV1 presentFrame = Accept(presentZero);
        True(absentFrame.Globals.PlayerToAct is null);
        Equal((byte)0, presentFrame.Globals.PlayerToAct!.Value);
        NotEqual(
            FrameSignature(absentFrame),
            FrameSignature(presentFrame));
    }

    private static void AssertDeepValueOwnership()
    {
        List<uint> lifePoints = new() { 8000, 7000 };
        List<PerspectiveSafeZoneV1> zones = new(CreateValidZones());
        List<PerspectiveSafeLinkMarkerV1> markers = new()
        {
            PerspectiveSafeLinkMarkerV1.Bottom,
            PerspectiveSafeLinkMarkerV1.Top
        };
        List<PerspectiveSafeCounterV1> counters = new()
        {
            new(1, 0),
            new(2, 1)
        };
        PerspectiveSafeCardPropertiesV1 properties =
            new(linkMarkers: markers, counters: counters);
        List<PerspectiveSafeEntityV1> entities = new()
        {
            new(
                "entity-a",
                identityKnown: true,
                passcode: 7,
                owner: 0,
                controller: 0,
                zone: PerspectiveSafeSemanticZoneV1.Hand,
                sequence: 0,
                overlaySequence: null,
                position: PerspectiveSafePositionV1.Unknown,
                faceUp: false,
                faceDown: false,
                printed: properties,
                current: properties)
        };
        List<PerspectiveSafeRelationshipV1> relationships = new()
        {
            new(
                PerspectiveSafeRelationshipKindV1.Target,
                "entity-a",
                "entity-a")
        };
        List<string> chainTargets = new() { "entity-a" };
        PerspectiveSafeChainLinkV1 chainLink = new(
            index: 0,
            activatingPlayer: 0,
            source: "entity-a",
            activationZone: PerspectiveSafeSemanticZoneV1.Hand,
            targets: chainTargets);
        List<PerspectiveSafeVisibleEventV1> events = new()
        {
            new(
                0,
                PerspectiveSafeVisibleEventKindV1.CardRevealed,
                entityLocator: "entity-a",
                publicPasscode: 7,
                targets: chainTargets)
        };
        List<uint> ownMain = new() { 1, 2 };
        List<uint> ownExtra = new() { 3 };
        PerspectiveSafeFrameSourceInputV1 input = CreateInput(
            new PerspectiveSafeGlobalsV1(
                duelFlags: 0,
                lifePoints: lifePoints,
                turnPlayer: 0,
                chainLength: 1),
            zones,
            entities,
            relationships,
            new PerspectiveSafeChainStateV1(1, new[] { chainLink }),
            events,
            new PerspectiveSafeMatchContextV1(
                perspectivePlayer: 0,
                duelFlags: 0,
                knowledge: new(true, false),
                ownDeck: new(true, ownMain, ownExtra),
                opponentDeck: new(false)));

        PerspectiveSafeFrameV1 frame = Accept(input);
        string before = FrameSignature(frame);

        lifePoints[0] = 1;
        zones[0] = new(
            1,
            PerspectiveSafeSemanticZoneV1.Banished,
            99,
            99,
            0,
            false);
        markers[0] = PerspectiveSafeLinkMarkerV1.TopRight;
        counters[0] = new(99, 99);
        entities.Clear();
        relationships.Clear();
        chainTargets[0] = "mutated";
        events.Clear();
        ownMain[0] = 99;
        ownExtra.Clear();

        Equal(before, FrameSignature(frame));
        Equal((uint)8000, frame.Globals.LifePoints[0]);
        Equal("entity-a", frame.Entities[0].Locator);
        Equal("entity-a", frame.Chain.Links[0].Targets[0]);
        Equal((uint)1, frame.MatchContext.OwnDeck.MainDeck[0]);
    }

    private static void AssertReadOnlyCollections()
    {
        PerspectiveSafeFrameV1 frame = Accept(CreateValidInput());
        AssertReadOnly(frame.Zones);
        AssertReadOnly(frame.Entities);
        AssertReadOnly(frame.Relationships);
        AssertReadOnly(frame.VisibleEvents);
        AssertReadOnly(frame.Entities[0].Current!.LinkMarkers);
        AssertReadOnly(frame.Chain.Links[0].Targets);
        AssertReadOnly(frame.MatchContext.OwnDeck.MainDeck);
    }

    private static void AssertEquivalentValuesAreDeterministic()
    {
        PerspectiveSafeFrameV1 first = Accept(CreateValidInput());
        PerspectiveSafeFrameV1 second = Accept(CreateValidInput());
        Equal(FrameSignature(first), FrameSignature(second));
        Equal(first.Entities[0].Locator, second.Entities[0].Locator);
        Equal(first.VisibleEvents[1].EventIndex, second.VisibleEvents[1].EventIndex);
    }

    private static void AssertPublicSurface()
    {
        Type[] publicTypes =
        {
            typeof(PerspectiveSafeSourceSectionV1),
            typeof(PerspectiveSafeFrameSourceErrorCodeV1),
            typeof(PerspectiveSafeFrameSourceErrorV1),
            typeof(PerspectiveSafeSemanticZoneV1),
            typeof(PerspectiveSafePositionV1),
            typeof(PerspectiveSafeLinkMarkerV1),
            typeof(PerspectiveSafeRelationshipKindV1),
            typeof(PerspectiveSafeVisibleEventKindV1),
            typeof(PerspectiveSafeCounterV1),
            typeof(PerspectiveSafeZoneV1),
            typeof(PerspectiveSafeKnowledgeV1),
            typeof(PerspectiveSafeFrameSourceInputV1),
            typeof(PerspectiveSafeFrameV1),
            typeof(PerspectiveSafeFrameSourceResultV1),
            typeof(PerspectiveSafePublicFrameSourceV1),
            typeof(PerspectiveSafeGlobalsV1),
            typeof(PerspectiveSafeCardPropertiesV1),
            typeof(PerspectiveSafeEntityV1),
            typeof(PerspectiveSafeRelationshipV1),
            typeof(PerspectiveSafeChainLinkV1),
            typeof(PerspectiveSafeChainStateV1),
            typeof(PerspectiveSafeVisibleEventV1),
            typeof(PerspectiveSafeDeckV1),
            typeof(PerspectiveSafeMatchContextV1)
        };
        string[] forbidden =
        {
            "OCGForge.Ignis.Protocol",
            "OCGForge.Ignis.Client",
            "FlatPrompt",
            "MirrorEntityIdV1",
            "PrivateResponse",
            "Socket",
            "Stream",
            "DateTime",
            "Guid",
            "Random",
            "SHA256"
        };

        foreach (Type type in publicTypes)
        {
            AssertNoForbiddenType(type, forbidden);
            foreach (PropertyInfo property in type.GetProperties(
                         BindingFlags.Public |
                         BindingFlags.Instance |
                         BindingFlags.Static |
                         BindingFlags.DeclaredOnly))
            {
                AssertNoForbiddenType(property.PropertyType, forbidden);
                False(
                    property.Name is "CanonicalBytes" or "SourceHash" or
                        "PublicObservationDigest" or "ContractId" or "FrameId",
                    property.Name);
            }

            foreach (MethodInfo method in type.GetMethods(
                         BindingFlags.Public |
                         BindingFlags.Instance |
                         BindingFlags.Static |
                         BindingFlags.DeclaredOnly))
            {
                AssertNoForbiddenType(method.ReturnType, forbidden);
                foreach (ParameterInfo parameter in method.GetParameters())
                {
                    AssertNoForbiddenType(parameter.ParameterType, forbidden);
                }
            }
        }
    }

    private static void AssertStructuredFailureSurface()
    {
        PerspectiveSafeFrameSourceResultV1 result =
            PerspectiveSafePublicFrameSourceV1.TryCreate(null);
        Null(result.Frame);
        NotNull(result.Error);
        string rendered = result.Error!.Value.ToString();
        AssertDoesNotContainForbidden(
            rendered,
            new[]
            {
                "\\",
                "/",
                "payload",
                "MirrorEntity",
                "Socket",
                "127.0.0.1"
            });

        PropertyInfo[] properties =
            typeof(PerspectiveSafeFrameSourceErrorV1).GetProperties(
                BindingFlags.Public | BindingFlags.Instance);
        Equal(2, properties.Length);
        True(properties.All(property => property.PropertyType.IsEnum));
    }

    private static void AssertPrintableLocatorBoundaries()
    {
        PerspectiveSafeEntityV1 controlEntity = new(
            "entity-\n",
            identityKnown: false,
            passcode: null,
            owner: null,
            controller: 0,
            zone: PerspectiveSafeSemanticZoneV1.Hand,
            sequence: null,
            overlaySequence: null,
            position: PerspectiveSafePositionV1.Unknown,
            faceUp: false,
            faceDown: false);
        AssertFailure(
            CreateInput(
                CreateValidGlobals(),
                CreateValidZones(),
                new[] { controlEntity },
                CreateValidRelationships(),
                CreateValidChain(),
                CreateValidEvents(),
                CreateValidMatchContext()),
            PerspectiveSafeFrameSourceErrorCodeV1.InvalidLocator);

        PerspectiveSafeRelationshipV1 controlRelationship = new(
            PerspectiveSafeRelationshipKindV1.Target,
            "source\r",
            "target");
        AssertFailure(
            CreateInput(
                CreateValidGlobals(),
                CreateValidZones(),
                CreateValidEntities(),
                new[] { controlRelationship },
                CreateValidChain(),
                CreateValidEvents(),
                CreateValidMatchContext()),
            PerspectiveSafeFrameSourceErrorCodeV1.InvalidLocator);

        PerspectiveSafeChainLinkV1 controlChainLink = new(
            index: 0,
            source: "source\0",
            targets: new[] { "target" });
        AssertFailure(
            CreateInput(
                CreateValidGlobals(),
                CreateValidZones(),
                CreateValidEntities(),
                CreateValidRelationships(),
                new PerspectiveSafeChainStateV1(1, new[] { controlChainLink }),
                CreateValidEvents(),
                CreateValidMatchContext()),
            PerspectiveSafeFrameSourceErrorCodeV1.InvalidLocator);

        PerspectiveSafeVisibleEventV1 controlEvent = new(
            0,
            PerspectiveSafeVisibleEventKindV1.CardRevealed,
            entityLocator: "entity\u007f");
        AssertFailure(
            CreateInput(
                CreateValidGlobals(),
                CreateValidZones(),
                CreateValidEntities(),
                CreateValidRelationships(),
                CreateValidChain(),
                new[] { controlEvent },
                CreateValidMatchContext()),
            PerspectiveSafeFrameSourceErrorCodeV1.InvalidLocator);
    }

    private static void AssertCrossSectionInvariants()
    {
        PerspectiveSafeFrameSourceInputV1 wrongLifePointCardinality = CreateInput(
            new PerspectiveSafeGlobalsV1(
                duelFlags: 0x1234,
                lifePoints: new uint[] { 8000 },
                turnPlayer: 0,
                chainLength: 1),
            CreateValidZones(),
            CreateValidEntities(),
            CreateValidRelationships(),
            CreateValidChain(),
            CreateValidEvents(),
            CreateValidMatchContext());
        AssertFailure(
            wrongLifePointCardinality,
            PerspectiveSafeFrameSourceErrorCodeV1.InvalidLifePointCardinality);

        PerspectiveSafeFrameSourceInputV1 wrongChainLength = CreateInput(
            new PerspectiveSafeGlobalsV1(
                duelFlags: 0x1234,
                lifePoints: new uint[] { 8000, 7000 },
                turnPlayer: 0,
                chainLength: 2),
            CreateValidZones(),
            CreateValidEntities(),
            CreateValidRelationships(),
            CreateValidChain(),
            CreateValidEvents(),
            CreateValidMatchContext());
        AssertFailure(
            wrongChainLength,
            PerspectiveSafeFrameSourceErrorCodeV1.CrossSectionMismatch);

        PerspectiveSafeFrameSourceInputV1 wrongDuelFlags = CreateInput(
            CreateValidGlobals(),
            CreateValidZones(),
            CreateValidEntities(),
            CreateValidRelationships(),
            CreateValidChain(),
            CreateValidEvents(),
            new PerspectiveSafeMatchContextV1(
                perspectivePlayer: 0,
                duelFlags: 0x4321,
                knowledge: new(true, false),
                ownDeck: new(true, new uint[] { 1, 2 }, new uint[] { 3 }),
                opponentDeck: new(false)));
        AssertFailure(
            wrongDuelFlags,
            PerspectiveSafeFrameSourceErrorCodeV1.CrossSectionMismatch);
    }

    private static void AssertFirstInvalidInvariant()
    {
        PerspectiveSafeVisibleEventV1 invalidLocatorWithZone = new(
            0,
            PerspectiveSafeVisibleEventKindV1.CardMoved,
            entityLocator: "entity\n",
            fromZone: PerspectiveSafeSemanticZoneV1.Hand);
        AssertFailure(
            CreateInput(
                CreateValidGlobals(),
                CreateValidZones(),
                CreateValidEntities(),
                CreateValidRelationships(),
                CreateValidChain(),
                new[] { invalidLocatorWithZone },
                CreateValidMatchContext()),
            PerspectiveSafeFrameSourceErrorCodeV1.InvalidLocator);

        PerspectiveSafeChainLinkV1 invalidSourceWithZone = new(
            index: 0,
            source: "source\r",
            activationZone: PerspectiveSafeSemanticZoneV1.Hand);
        AssertFailure(
            CreateInput(
                CreateValidGlobals(),
                CreateValidZones(),
                CreateValidEntities(),
                CreateValidRelationships(),
                new PerspectiveSafeChainStateV1(1, new[] { invalidSourceWithZone }),
                CreateValidEvents(),
                CreateValidMatchContext()),
            PerspectiveSafeFrameSourceErrorCodeV1.InvalidLocator);
    }

    private static PerspectiveSafeFrameV1 Accept(
        PerspectiveSafeFrameSourceInputV1 input)
    {
        PerspectiveSafeFrameSourceResultV1 result =
            PerspectiveSafePublicFrameSourceV1.TryCreate(input);
        True(result.IsSuccess, result.Error?.ToString() ?? "source value rejected");
        NotNull(result.Frame);
        Null(result.Error);
        return result.Frame!;
    }

    private static void AssertFailure(
        PerspectiveSafeFrameSourceInputV1 input,
        PerspectiveSafeFrameSourceErrorCodeV1 expectedCode)
    {
        PerspectiveSafeFrameSourceResultV1 result =
            PerspectiveSafePublicFrameSourceV1.TryCreate(input);
        False(result.IsSuccess);
        Null(result.Frame);
        NotNull(result.Error);
        Equal(expectedCode, result.Error!.Value.Code);
    }

    private static void AssertReadOnly<T>(IReadOnlyList<T> values)
    {
        True(values is IList<T>);
        IList<T> list = (IList<T>)values;
        bool rejected = false;
        try
        {
            list.Add(default!);
        }
        catch (NotSupportedException)
        {
            rejected = true;
        }

        True(rejected, "collection accepted mutation");
    }

    private static void AssertNoForbiddenType(Type type, IEnumerable<string> forbidden)
    {
        string name = type.FullName ?? type.Name;
        AssertDoesNotContainForbidden(name, forbidden);
        if (type.IsGenericType)
        {
            foreach (Type argument in type.GetGenericArguments())
            {
                AssertNoForbiddenType(argument, forbidden);
            }
        }
    }

    private static PerspectiveSafeFrameSourceInputV1 CreateValidInput() =>
        CreateInput(
            CreateValidGlobals(),
            CreateValidZones(),
            CreateValidEntities(),
            CreateValidRelationships(),
            CreateValidChain(),
            CreateValidEvents(),
            CreateValidMatchContext());

    private static PerspectiveSafeFrameSourceInputV1 CreateInput(
        PerspectiveSafeGlobalsV1? globals,
        IEnumerable<PerspectiveSafeZoneV1>? zones,
        IEnumerable<PerspectiveSafeEntityV1>? entities,
        IEnumerable<PerspectiveSafeRelationshipV1>? relationships,
        PerspectiveSafeChainStateV1? chain,
        IEnumerable<PerspectiveSafeVisibleEventV1>? events,
        PerspectiveSafeMatchContextV1? matchContext) =>
        new(
            globals,
            zones,
            entities,
            relationships,
            chain,
            events,
            matchContext);

    private static PerspectiveSafeGlobalsV1 CreateValidGlobals() =>
        new(
            duelFlags: 0x1234,
            lifePoints: new uint[] { 8000, 7000 },
            turnPlayer: 0,
            turnCount: 3,
            phase: 4,
            chainLength: 1,
            terminal: false);

    private static IReadOnlyList<PerspectiveSafeZoneV1> CreateValidZones() =>
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
        };

    private static IReadOnlyList<PerspectiveSafeEntityV1> CreateValidEntities()
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
                counters: new[]
                {
                    new PerspectiveSafeCounterV1(1, 0),
                    new PerspectiveSafeCounterV1(2, 1)
                });
        return new[]
        {
            new PerspectiveSafeEntityV1(
                "entity-a",
                identityKnown: true,
                passcode: 7,
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
                "entity-b",
                identityKnown: false,
                passcode: null,
                owner: null,
                controller: 0,
                zone: PerspectiveSafeSemanticZoneV1.MonsterZone,
                sequence: 1,
                overlaySequence: null,
                position: PerspectiveSafePositionV1.FaceDownDefense,
                faceUp: false,
                faceDown: true)
        };
    }

    private static IReadOnlyList<PerspectiveSafeRelationshipV1>
        CreateValidRelationships() =>
        new[]
        {
            new PerspectiveSafeRelationshipV1(
                PerspectiveSafeRelationshipKindV1.Target,
                "entity-a",
                "entity-b")
        };

    private static PerspectiveSafeChainStateV1 CreateValidChain() =>
        new(
            1,
            new[]
            {
                new PerspectiveSafeChainLinkV1(
                    index: 0,
                    activatingPlayer: 0,
                    source: "entity-a",
                    activationZone: PerspectiveSafeSemanticZoneV1.Hand,
                    effectDescription: 42,
                    targets: new[] { "entity-a", "entity-b" })
            });

    private static IReadOnlyList<PerspectiveSafeVisibleEventV1> CreateValidEvents() =>
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
                entityLocator: "entity-a",
                fromZone: PerspectiveSafeSemanticZoneV1.Hand,
                toZone: PerspectiveSafeSemanticZoneV1.MonsterZone)
        };

    private static PerspectiveSafeMatchContextV1 CreateValidMatchContext() =>
        new(
            perspectivePlayer: 0,
            duelFlags: 0x1234,
            knowledge: new(true, false),
            ownDeck: new(
                known: true,
                mainDeck: new uint[] { 1, 2 },
                extraDeck: new uint[] { 3 }),
            opponentDeck: new(known: false));

    private static string FrameSignature(PerspectiveSafeFrameV1 frame)
    {
        List<string> parts = new();
        parts.Add(frame.Globals.DuelFlags.ToString());
        parts.Add(string.Join(",", frame.Globals.LifePoints));
        parts.Add(frame.Globals.PlayerToAct?.ToString() ?? "absent");
        parts.Add(frame.Globals.TurnPlayer?.ToString() ?? "absent");
        parts.Add(frame.Globals.TurnCount?.ToString() ?? "absent");
        parts.Add(frame.Globals.Phase?.ToString() ?? "absent");
        parts.Add(frame.Globals.ChainLength.ToString());
        parts.Add(frame.Globals.Winner?.ToString() ?? "absent");
        parts.Add(frame.Globals.WinReason?.ToString() ?? "absent");
        parts.Add(frame.Globals.Terminal.ToString());
        foreach (PerspectiveSafeZoneV1 zone in frame.Zones)
        {
            parts.Add(
                string.Join(
                    ":",
                    zone.Player,
                    (byte)zone.Kind,
                    zone.TotalCount,
                    zone.PublicIdentityCount,
                    zone.HiddenCount,
                    zone.PlayerObservableOrder));
        }

        foreach (PerspectiveSafeEntityV1 entity in frame.Entities)
        {
            parts.Add(
                string.Join(
                    ":",
                    entity.Locator,
                    entity.IdentityKnown,
                    entity.Passcode?.ToString() ?? "absent",
                    entity.Owner?.ToString() ?? "absent",
                    entity.Controller?.ToString() ?? "absent",
                    (byte)entity.Zone,
                    entity.Sequence?.ToString() ?? "absent",
                    entity.OverlaySequence?.ToString() ?? "absent",
                    (byte)entity.Position,
                    entity.FaceUp,
                    entity.FaceDown,
                    DescribeProperties(entity.Printed),
                    DescribeProperties(entity.Current)));
        }

        foreach (PerspectiveSafeRelationshipV1 relationship in frame.Relationships)
        {
            parts.Add(
                string.Join(
                    ":",
                    (byte)relationship.Kind,
                    relationship.Source,
                    relationship.Target));
        }

        parts.Add(frame.Chain.Length.ToString());
        foreach (PerspectiveSafeChainLinkV1 link in frame.Chain.Links)
        {
            parts.Add(
                string.Join(
                    ":",
                    link.Index,
                    link.ActivatingPlayer?.ToString() ?? "absent",
                    link.Source ?? "absent",
                    link.ActivationZone?.ToString() ?? "absent",
                    link.EffectDescription?.ToString() ?? "absent",
                    string.Join(",", link.Targets)));
        }

        foreach (PerspectiveSafeVisibleEventV1 visibleEvent in frame.VisibleEvents)
        {
            parts.Add(
                string.Join(
                    ":",
                    visibleEvent.EventIndex,
                    (byte)visibleEvent.Kind,
                    visibleEvent.Player?.ToString() ?? "absent",
                    visibleEvent.EntityLocator ?? "absent",
                    visibleEvent.PublicPasscode?.ToString() ?? "absent",
                    visibleEvent.FromZone?.ToString() ?? "absent",
                    visibleEvent.ToZone?.ToString() ?? "absent",
                    visibleEvent.Count?.ToString() ?? "absent",
                    visibleEvent.Amount?.ToString() ?? "absent",
                    visibleEvent.CounterType?.ToString() ?? "absent",
                    visibleEvent.Phase?.ToString() ?? "absent",
                    visibleEvent.Winner?.ToString() ?? "absent",
                    visibleEvent.WinReason?.ToString() ?? "absent",
                    visibleEvent.EffectDescription?.ToString() ?? "absent",
                    string.Join(",", visibleEvent.Targets)));
        }

        parts.Add(frame.MatchContext.PerspectivePlayer.ToString());
        parts.Add(frame.MatchContext.DuelFlags.ToString());
        parts.Add(frame.MatchContext.Knowledge.ToString());
        parts.Add(frame.MatchContext.OwnDeck.Known.ToString());
        parts.Add(string.Join(",", frame.MatchContext.OwnDeck.MainDeck));
        parts.Add(string.Join(",", frame.MatchContext.OwnDeck.ExtraDeck));
        parts.Add(frame.MatchContext.OpponentDeck.Known.ToString());
        parts.Add(string.Join(",", frame.MatchContext.OpponentDeck.MainDeck));
        parts.Add(string.Join(",", frame.MatchContext.OpponentDeck.ExtraDeck));
        return string.Join("|", parts);
    }

    private static string DescribeProperties(
        PerspectiveSafeCardPropertiesV1? properties)
    {
        if (properties is null)
        {
            return "absent";
        }

        return string.Join(
            ":",
            properties.Type?.ToString() ?? "absent",
            properties.Attribute?.ToString() ?? "absent",
            properties.Race?.ToString() ?? "absent",
            properties.Attack?.ToString() ?? "absent",
            properties.Defense?.ToString() ?? "absent",
            properties.BaseAttack?.ToString() ?? "absent",
            properties.BaseDefense?.ToString() ?? "absent",
            properties.Level?.ToString() ?? "absent",
            properties.Rank?.ToString() ?? "absent",
            properties.LinkRating?.ToString() ?? "absent",
            string.Join(",", properties.LinkMarkers),
            properties.LeftScale?.ToString() ?? "absent",
            properties.RightScale?.ToString() ?? "absent",
            properties.StatusFlags?.ToString() ?? "absent",
            string.Join(",", properties.Counters));
    }
}
