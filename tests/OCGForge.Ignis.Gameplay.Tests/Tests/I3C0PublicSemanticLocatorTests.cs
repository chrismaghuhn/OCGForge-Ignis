using System.Globalization;
using System.Reflection;
using OCGForge.Ignis.Gameplay;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I3C0PublicSemanticLocatorTests
{
    internal static void TestCanonicalFormsRoundTripExactly()
    {
        string[] canonical =
        {
            "p0:HAND:3",
            "p1:MONSTER_ZONE:2",
            "p0:SPELL_TRAP_ZONE:4",
            "p1:FIELD_ZONE:5",
            "p0:PENDULUM_RELEVANT_STATE:6",
            "p0:GRAVEYARD:0",
            "p1:BANISHED:12",
            "p1:HAND:public:12345678:0",
            "p0:EXTRA_DECK:public:87654321:2",
            "p0:OVERLAY:2:1"
        };

        foreach (string value in canonical)
        {
            True(PublicSemanticLocatorV1.TryParse(value, out PublicSemanticLocatorV1 locator));
            Equal(value, locator.Value);
            Equal(value, locator.ToString());
        }

        True(PublicSemanticLocatorV1.TryCreateIndexed(
            0,
            PublicSemanticZoneV1.Hand,
            3,
            out PublicSemanticLocatorV1 indexed));
        Equal("p0:HAND:3", indexed.Value);

        True(PublicSemanticLocatorV1.TryCreatePublicOrdinal(
            1,
            PublicSemanticZoneV1.Hand,
            12345678,
            0,
            out PublicSemanticLocatorV1 publicOrdinal));
        Equal("p1:HAND:public:12345678:0", publicOrdinal.Value);

        True(PublicSemanticLocatorV1.TryCreateOverlay(
            0,
            2,
            1,
            out PublicSemanticLocatorV1 overlay));
        Equal("p0:OVERLAY:2:1", overlay.Value);
    }

    internal static void TestMalformedFormsFailClosed()
    {
        string?[] invalid =
        {
            null,
            string.Empty,
            "p2:HAND:0",
            "p00:HAND:0",
            "P0:HAND:0",
            "p0:HAND",
            "p0:HAND:0:extra",
            "p0:monster_zone:2",
            "p0:UNKNOWN:2",
            "p0:MAIN_DECK:2",
            "p0:HAND:00",
            "p0:HAND:+1",
            "p0:HAND:-1",
            "p0:HAND: 1",
            "p0:HAND:1 ",
            "p0:HAND:4294967296",
            "p0:EXTRA_DECK:0",
            "p0:OVERLAY:0",
            "p0:OVERLAY:0:0:0",
            "p0:OVERLAY:00:0",
            "p0:OVERLAY:+1:0",
            "p0:HAND:public:1:00",
            "p0:HAND:public:1:+0",
            "p0:HAND:public:1:-1",
            "p0:HAND:public:1:4294967296",
            "p0:MONSTER_ZONE:public:123:0",
            "p0:GRAVEYARD:public:123:0",
            "p0:HAND:public:0:0",
            "p0:HAND:PUBLIC:1:0",
            "p0:HAND:public:1",
            "p0:HAND:public:1:0:extra",
            "p0:HAND:\t1",
            "p0:HAND:\r\n1",
            "p0:HAND:\0"
        };

        foreach (string? value in invalid)
        {
            False(
                PublicSemanticLocatorV1.TryParse(value, out _),
                value ?? "<null>");
        }

        False(PublicSemanticLocatorV1.TryCreateIndexed(
            0,
            PublicSemanticZoneV1.ExtraDeck,
            0,
            out _));
        False(PublicSemanticLocatorV1.TryCreateIndexed(
            0,
            PublicSemanticZoneV1.Overlay,
            0,
            out _));
        False(PublicSemanticLocatorV1.TryCreatePublicOrdinal(
            0,
            PublicSemanticZoneV1.MonsterZone,
            1,
            0,
            out _));
        False(PublicSemanticLocatorV1.TryCreatePublicOrdinal(
            0,
            PublicSemanticZoneV1.Graveyard,
            1,
            0,
            out _));
        False(PublicSemanticLocatorV1.TryCreatePublicOrdinal(
            0,
            PublicSemanticZoneV1.Hand,
            0,
            0,
            out _));
        False(PublicSemanticLocatorV1.TryCreateIndexed(
            2,
            PublicSemanticZoneV1.Hand,
            0,
            out _));
    }

    internal static void TestAbsolutePlayerMapping()
    {
        (GameplayPerspectiveV1 Perspective, MirrorParticipantRoleV1 Role, byte Expected)[] cases =
        {
            (GameplayPerspectiveV1.SelfIsPlayer0, MirrorParticipantRoleV1.Self, 0),
            (GameplayPerspectiveV1.SelfIsPlayer0, MirrorParticipantRoleV1.Opponent, 1),
            (GameplayPerspectiveV1.SelfIsPlayer1, MirrorParticipantRoleV1.Self, 1),
            (GameplayPerspectiveV1.SelfIsPlayer1, MirrorParticipantRoleV1.Opponent, 0)
        };

        foreach ((GameplayPerspectiveV1 perspective, MirrorParticipantRoleV1 role, byte expected)
            in cases)
        {
            True(PublicSemanticLocatorV1.TryGetAbsolutePlayer(
                perspective,
                role,
                out byte actual));
            Equal(expected, actual);
        }

        False(PublicSemanticLocatorV1.TryGetAbsolutePlayer(
            null,
            MirrorParticipantRoleV1.Self,
            out _));
        False(PublicSemanticLocatorV1.TryGetAbsolutePlayer(
            GameplayPerspectiveV1.SelfIsPlayer0,
            (MirrorParticipantRoleV1)2,
            out _));
    }

    internal static void TestDeterministicCultureIndependentValue()
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");

            True(PublicSemanticLocatorV1.TryCreatePublicOrdinal(
                1,
                PublicSemanticZoneV1.Hand,
                12345678,
                0,
                out PublicSemanticLocatorV1 created));
            Equal("p1:HAND:public:12345678:0", created.Value);

            True(PublicSemanticLocatorV1.TryParse(
                "p0:HAND:3",
                out PublicSemanticLocatorV1 first));
            True(PublicSemanticLocatorV1.TryParse(
                "p0:HAND:3",
                out PublicSemanticLocatorV1 same));
            True(PublicSemanticLocatorV1.TryParse(
                "p0:HAND:10",
                out PublicSemanticLocatorV1 ten));
            True(PublicSemanticLocatorV1.TryParse(
                "p0:HAND:2",
                out PublicSemanticLocatorV1 two));

            Equal(first, same);
            Equal(first.GetHashCode(), same.GetHashCode());
            Equal(2069333687, first.GetHashCode());
            True(ten.CompareTo(two) < 0);
            True(ten < two);
            True(ten <= two);
            True(two > ten);
            True(two >= ten);
            NotEqual(first, two);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    internal static void TestPublicApiBoundary()
    {
        Type locatorType = typeof(PublicSemanticLocatorV1);
        PropertyInfo[] publicProperties = locatorType.GetProperties(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
        Equal(1, publicProperties.Length);
        Equal("Value", publicProperties[0].Name);
        Equal(typeof(string), publicProperties[0].PropertyType);

        Equal(
            0,
            locatorType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Length);
        Equal(
            0,
            locatorType.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length);

        string[] forbiddenTypes =
        {
            "MirrorEntityIdV1",
            "MirrorAddress",
            "ModernLocInfoV1",
            "NextEntityOrdinal"
        };
        foreach (MemberInfo member in locatorType.GetMembers(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
        {
            AssertDoesNotContainForbidden(member.ToString(), forbiddenTypes);
        }

        string[] forbiddenProjectionNames =
        {
            "FromMirrorCard",
            "FromMirrorAddress",
            "ProjectMirrorSnapshot",
            "MirrorToPublic"
        };
        foreach (MethodInfo method in locatorType.GetMethods(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
        {
            False(
                forbiddenProjectionNames.Contains(method.Name, StringComparer.Ordinal),
                method.Name);
        }
    }
}
