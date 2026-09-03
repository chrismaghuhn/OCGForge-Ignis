# Third-Party Policy and Upstream License Risk

Status: researched for I0; no third-party source or asset is included
Date: 2026-09-03

This file records technical provenance and license risk for upstream
references. It is not legal advice.

## Policy

The repositories below may be consulted as protocol, behavior, or runtime
references. They are not dependencies of the I0 repository. No EDOPro,
WindBot, ygopro-core, CardScripts, BabelCDB, or Distribution source, binary,
database, deck, or other asset is vendored or copied.

Future source reuse requires a separate explicit decision, a source-level
license review, and an update to the repository provenance and release
artifacts. Clean-room protocol facts must be independently implemented.

## Upstream records

| Upstream | Role | Frozen pin/reference | HEAD observed 2026-09-03 | License signal at pinned source | Intended relationship |
| --- | --- | --- | --- | --- | --- |
| [EDOPro](https://github.com/edo9300/edopro) | client and local/private server reference | 30935e847165a9ef0e547fb51a43f36168fab7c7 | 30935e847165a9ef0e547fb51a43f36168fab7c7 | AGPL-family: root LICENSE states independently written client modules and script engine are AGPLv3; some modified client modules are GPLv3 | protocol reference and independent oracle only |
| [WindBot](https://github.com/ProjectIgnis/windbot) | external bot-client reference | bffe6b62679c8b2fafea8f59740e03a132517da4 | bffe6b62679c8b2fafea8f59740e03a132517da4 | AGPLv3-or-later terms in root LICENSE | protocol reference and independent oracle only |
| [ygopro-core](https://github.com/edo9300/ygopro-core) | ocgcore research reference | e747e1771fcf91dd7c53a5950f030012229e66e4 | e747e1771fcf91dd7c53a5950f030012229e66e4 | AGPLv3-or-later terms in root LICENSE | research reference only; not the EDOPro V1 runtime core |
| [CardScripts](https://github.com/ProjectIgnis/CardScripts) | script-content provenance | 00a828b79303d047d6905f528857cc287ad3a84e | e6fdc75f131d00904db1f96bd79ff0f9ffe66c84 | AGPLv3 license text in root COPYING | pinned provenance only; no scripts or assets included |
| [BabelCDB](https://github.com/ProjectIgnis/BabelCDB) | card-database provenance | 2142b4b45e7963fd944940f144951177e87eb15c | f37312b2bff3c8ba575f19a1f0a1da3c2d44ccf4 | no standalone license declaration was located in the pinned root; no redistribution right is inferred | pinned provenance only; no CDB files included |
| [Distribution](https://github.com/ProjectIgnis/Distribution) | distribution-resource provenance | 54a6e2395c532648ff762540e9615319fac4f51b | 54a6e2395c532648ff762540e9615319fac4f51b | AGPLv3 license text in root LICENSE | pinned provenance only; no assets included |

The current HEAD column is informational provenance. A different current HEAD
does not replace the frozen pin. CardScripts and BabelCDB are the two pins
whose current default-branch HEADs differed at bootstrap; both frozen commits
were independently resolved in their intended repositories.

For V1, the runtime core is the EDOPro tree's ocgcore gitlink
46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57. The ygopro-core pin
e747e1771fcf91dd7c53a5950f030012229e66e4 remains a separate research
reference and must not be silently mixed into the EDOPro runtime identity.
An intentional override requires a separate explicit identity and acceptance
decision.

## Release implications

No release may imply that Ignis contains or links to an upstream implementation
unless a later, explicit decision authorizes that relationship. A future
distribution must carry the applicable notices and source-offer obligations
after a real source and binary inventory. I0 makes no such distribution claim.
