# ADR-0004: I6C6 Local/Private EDOPro Query Evidence

Status: ACCEPTED ARCHITECTURE DECISION; implementation separately unauthorized

Date: 2026-09-08

Decision owners: OCGForge-Ignis governance

## Context

The I6C6 Current-property audit proves that the pinned standalone TCP client
cannot obtain complete Current.link_markers evidence. The pinned EDOPro
GenericDuel server includes QUERY_LINK in MZONE, SZONE, and RefreshSingle
queries, but not in the normal Extra, Grave, and Hand refresh masks. The CTOS
inventory has no query-mask request that a normal client can send.

The accepted I6C6-3A architecture therefore needs a separately controlled
evidence runtime for the Link-marker field. This is an oracle/evidence
capability decision, not a change to the Ignis production integration mode.

## Decision

Authorize a narrowly scoped, separately pinned local/private EDOPro server
patch for I6C6 oracle and model-state evidence. The patch may add QUERY_LINK
to the relevant GenericDuel refresh masks so that the existing
perspective-filtered public query path supplies Link rating and Link markers
at the selected comparison boundary.

The external runtime identity remains:

~~~text
EDOPRO_COMMIT=30935e847165a9ef0e547fb51a43f36168fab7c7
EDOPRO_OCGCORE_GITLINK=46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57
~~~

The patch must receive its own source-controlled patchset identifier and
digest. Those provenance values are separate from gameplay semantic identity.

This decision is an exception recorded under ADR-0001's revisit rule. It
does not change the production statement that OCGForge-Ignis is an
independent external TCP client, and it does not require EDOPro to be forked,
embedded, injected into, or loaded by the Ignis process.

## Strict boundary

The future patch may change only the server-owned query-mask selection needed
for complete public Link-marker evidence. It must not change:

* game rules, CardScripts, legality, decisions, or response handling;
* RNG, seed derivation, deck state, card data, or message ordering;
* CTOS/STOC wire formats or client behavior;
* the Ignis mirror, public-frame, Printed-provider, or model contracts;
* counter handling, which is owned by the separate event-reconstruction track.

The patch is local/private test and oracle infrastructure. It is not a
public-server modification and is not an Ignis release dependency unless a
later release decision explicitly says so.

## Required implementation gates

The patch implementation remains separately unauthorized. Before it can be
used for I6C6 evidence, a later task must prove:

1. the exact patch diff changes only the permitted GenericDuel masks;
2. the patched EDOPro and ocgcore build reproducibly from the two pins above;
3. owner and opponent query outputs remain recipient-specific;
4. hidden hand, deck, face-down field, and face-down Extra identities remain
   redacted;
5. Link markers are available only where the native public visibility
   contract permits them;
6. the patched refresh occurs before the selected I6C6 boundary, with no
   boundary reduction or timing assumption;
7. the public query bytes and normalized Current values are deterministic
   across fresh processes;
8. scenario, runtime, CardScripts, and Printed-source provenance remain
   separately bound;
9. absence, malformed evidence, privacy failure, and patch mismatch fail
   closed;
10. no EDOPro source, binary, CDB, BabelCDB row, generated artifact, or
    acquisition path is added to OCGForge-Ignis.

The future implementation must use the existing recipient-filtered query
path. Adding a bit to a mask is not itself privacy evidence.

## Third-party and release boundary

This ADR does not make a legal determination about EDOPro, ocgcore,
CardScripts, BabelCDB, card data, or any operator's right to acquire or use
an external runtime. It authorizes only a repository architecture for
operator- or test-owner-provisioned local/private evidence infrastructure.

OCGForge-Ignis may not distribute or vendor the patched EDOPro runtime,
ocgcore, BabelCDB, cards.cdb, generated card-data artifacts, or real card-data
fixtures under this decision. It may not add download, clone, generation,
automatic discovery, or update tooling. Existing third-party restrictions
remain in force.

## Consequences

The Link-marker capability gap has an explicit future owner without weakening
the standalone Ignis process boundary. Counter reconstruction remains an
Ignis event-state slice and does not depend on this ADR.

The future Link evidence task must be run only against the separately pinned
local/private server and must record the patch provenance without mixing it
into gameplay semantic identity. If the patch, privacy behavior, or boundary
binding cannot be proven, I6C6 remains UNPROVEN for scenarios requiring Link
markers.

~~~text
ADR_0001_LINK_QUERY_REVISION=APPROVED_LOCAL_PRIVATE_ORACLE_ONLY
LINK_QUERY_EXPANSION_IMPLEMENTATION_AUTHORIZED=NO
PUBLIC_EDOPRO_SERVER_MODIFICATION_AUTHORIZED=NO
I6C6_3A_IMPLEMENTATION_AUTHORIZED=NO
~~~
