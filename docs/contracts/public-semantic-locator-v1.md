# Public Semantic Locator V1

Status: I3C0 foundation contract (pending independent review)
Contract ID: ocgforge-ignis.public-semantic-locator.v1
Scope: deterministic public-safe locator vocabulary and strict codec foundation

## Purpose

I3C0 defines a canonical public semantic locator value for later State /
Privacy work. It is separate from every protocol-local address and from the
internal MirrorEntityIdV1 identity:

~~~text
protocol-local address != MirrorEntityIdV1 != PublicSemanticLocatorV1
~~~

The locator describes only an admitted semantic reference. It does not decide
whether the referenced entity may be exposed.

**Syntactic validity != authorization to expose an entity.**

Visibility, card knowledge, public-state projection, relationship projection,
chain projection, event projection, candidate construction, and response
binding remain outside I3C0 and belong to future I3C/I3D work.

**I3C0 DOES NOT CLAIM I6 CROSS-ORACLE ACCEPTANCE.**

## Absolute participant identity

All locator values use absolute duel-player tokens p0 and p1. They never use
Self or Opponent.

The deterministic mapping from GameplayPerspectiveV1 and
MirrorParticipantRoleV1 is:

| Perspective | Role | Absolute player |
| --- | --- | --- |
| SelfIsPlayer0 | Self | 0 |
| SelfIsPlayer0 | Opponent | 1 |
| SelfIsPlayer1 | Self | 1 |
| SelfIsPlayer1 | Opponent | 0 |

This mapping uses only the established gameplay perspective and participant
role. It does not consume transport metadata.

## Semantic zone vocabulary

The exact admitted tokens are:

~~~text
HAND
MONSTER_ZONE
SPELL_TRAP_ZONE
FIELD_ZONE
PENDULUM_RELEVANT_STATE
GRAVEYARD
BANISHED
EXTRA_DECK
OVERLAY
~~~

Tokens are case-sensitive. MAIN_DECK and UNKNOWN are not entity-locator zones
in this contract. The codec does not define aliases or lowercase variants.

I3C0 deliberately does not convert a MirrorZoneV1.SpellTrapZone entity into a
public semantic zone. Rules context and sequence can distinguish
SPELL_TRAP_ZONE, FIELD_ZONE, and PENDULUM_RELEVANT_STATE; choosing among them
is not part of this codec.

## Canonical locator forms

### Indexed semantic location

~~~text
p<player>:<zone>:<sequence>
~~~

Indexed locations admit only:

~~~text
HAND
MONSTER_ZONE
SPELL_TRAP_ZONE
FIELD_ZONE
PENDULUM_RELEVANT_STATE
GRAVEYARD
BANISHED
~~~

EXTRA_DECK and OVERLAY are not valid indexed locations.

### Public-identity ordinal

~~~text
p<player>:<zone>:public:<card_code>:<ordinal>
~~~

The admitted zones are HAND and EXTRA_DECK. card_code is greater than zero
and ordinal is an ordinal within a future public-semantic projection rule. It
is not a mirror ordinal, protocol sequence, or physical-card identity. I3C0
does not decide when this family is eligible to be emitted.

### Overlay semantic location

~~~text
p<player>:OVERLAY:<parent_sequence>:<overlay_sequence>
~~~

Both components are semantic positional components, not internal entity
identities.

## Canonical encoding and strict parsing

Numeric components are ASCII decimal with no sign, whitespace, or leading
zeroes except the number zero itself. Parsing is checked and rejects overflow.
No current-culture formatting or parsing is used.

PublicSemanticLocatorV1.TryParse fails closed for null, empty, embedded NUL,
CR/LF, whitespace, malformed component counts, wrong-case tokens, unknown
zones, MAIN_DECK, UNKNOWN, signed or non-ASCII numeric values, leading
zeroes, overflow, invalid zone/family combinations, zero card_code, and
unsupported unknown locator forms. It never rewrites malformed input into a
canonical value.

PublicSemanticLocatorV1.Value is the canonical textual value. Equality is
ordinal equality of that value. Ordering is ordinal, ASCII-compatible lexical
ordering of that value. The value type is immutable and exposes no mirror
identity, mirror address, modern wire location, raw protocol bits, or
allocation ordinal.

GetHashCode uses a deterministic value-only hash for ordinary .NET collection
use. It is not a gameplay hash, trace hash, build hash, provenance hash, or
cross-oracle identity contract.

## Explicit non-goals

I3C0 does not implement:

- card-knowledge reduction or visibility eligibility;
- public state or relationship projection;
- chain or event projection;
- candidate or response construction;
- model input, IPC, replay extraction, or cross-oracle comparison;
- new gameplay messages, query flags, mirror semantics, or deck logic;
- a MirrorEntityIdV1 / MirrorAddress to public-locator conversion;
- pX:HAND:unknown or pX:EXTRA_DECK:unknown.

No production behavior outside this new locator contract is changed.
