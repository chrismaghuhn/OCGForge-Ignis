# I6C6-3A2 Local/Private Link Query Evidence Patch

## 1. Executive result

This record documents one local/private EDOPro source patch and its
perspective-filter probe. The patch changes only the three GenericDuel
default refresh masks required to request the already existing QUERY_LINK
field. It does not change Ignis, ocgcore, the protocol, rules, RNG, CardScripts,
card data, or the counter branch.

The source, mask, privacy-probe, boundary, patch-identity, and build gates
pass. No authorized local 1v1 runtime scenario with provisioned card/script
assets was available in the existing worktrees, so runtime boundary evidence
is explicitly not claimed.

~~~text
PATCH_SOURCE_AND_PRIVACY_EVIDENCE=PASS
RUNTIME_LINK_BOUNDARY_EVIDENCE=UNPROVEN
~~~

## 2. Governance authority

The repository-side authority is the merged ADR-0004 at
ce9274d23f95dd3f0f14eba2ecf9697384c899ce:

~~~text
ADR_0001_LINK_QUERY_REVISION=APPROVED_LOCAL_PRIVATE_ORACLE_ONLY
PUBLIC_EDOPRO_SERVER_MODIFICATION_AUTHORIZED=NO
LINK_QUERY_EXPANSION_IMPLEMENTATION_AUTHORIZED=YES
~~~

The approval is limited to a separately provisioned local/private oracle
runtime. It does not make the patch an Ignis release dependency and does not
authorize a public EDOPro server change.

## 3. Runtime provenance

~~~text
EDOPRO_BASE=30935e847165a9ef0e547fb51a43f36168fab7c7
EDOPRO_OCGCORE_GITLINK=46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57
EDOPRO_LUA_SUBMODULE=6e22fedb74cf0c9b6656e9fce8b7331db847c605
EDOPRO_PATCH_BRANCH=ocgforge/i6c6-link-query-evidence-v1
EDOPRO_PATCH_COMMIT=edfb77d7baf68b986209d327a871021296c2954b
EDOPRO_PATCH_FILE=gframe/generic_duel.h
EDOPRO_PATCH_PUSHED=NO
~~~

The EDOPro worktree was created separately from the existing dirty checkout.
The tracked worktree is clean after the local commit. Both the ocgcore
gitlink and its Lua submodule resolve to the exact pinned commits shown above.

## 4. Exact mask delta

The only tracked EDOPro diff is the following three default-mask value
changes:

~~~text
QUERY_LINK_HEX=0x00800000

REFRESH_HAND_OLD=0x03781fff
REFRESH_HAND_NEW=0x03f81fff

REFRESH_GRAVE_OLD=0x00381fff
REFRESH_GRAVE_NEW=0x00b81fff

REFRESH_EXTRA_OLD=0x00381fff
REFRESH_EXTRA_NEW=0x00b81fff
~~~

The numeric proof was evaluated mechanically for each row:

~~~text
HAND_XOR=0x00800000
GRAVE_XOR=0x00800000
EXTRA_XOR=0x00800000

HAND_QUERY_LINK_ONLY_DELTA=PASS
GRAVE_QUERY_LINK_ONLY_DELTA=PASS
EXTRA_QUERY_LINK_ONLY_DELTA=PASS
~~~

For every mask, NEW AND QUERY_LINK equals QUERY_LINK and NEW WITHOUT
QUERY_LINK equals OLD. No other bit changes.

## 5. Out-of-scope masks

The following values were inspected and are outside the patch:

~~~text
RefreshMzone default = 0x03981fff
RefreshSzone default = 0x03f81fff
RefreshSingle default = 0x03f81fff
PseudoRefreshDeck = 0x01181fff
TAG_RELAY_EXPLICIT_MASKS = unchanged
QUERY_COUNTERS = not added
~~~

The patch changes no explicit Tag/Relay overrides, no replay-only
PseudoRefreshDeck mask, and no MZONE, SZONE, or RefreshSingle default.

~~~text
REFRESH_MZONE_CHANGED=NO
REFRESH_SZONE_CHANGED=NO
REFRESH_SINGLE_CHANGED=NO
PSEUDO_REFRESH_DECK_CHANGED=NO
TAG_RELAY_EXPLICIT_MASKS_CHANGED=NO
QUERY_COUNTERS_CHANGED=NO

CORE_UTILS_CHANGED=NO
NETWORK_PROTOCOL_CHANGED=NO
OCGCORE_CHANGED=NO
CARDSCRIPTS_CHANGED=NO
CARD_DATA_CHANGED=NO
RULES_CHANGED=NO
RNG_CHANGED=NO
MESSAGE_ORDER_CHANGED=NO
~~~

## 6. Privacy-filter proof

The temporary probe was compiled outside both repositories against the real
pinned CoreUtils::QueryStream implementation in gframe/core_utils.cpp. It
constructed only synthetic query records containing position, is-public,
Link rating 3, and Link marker bits 0x81. It contained no passcodes, card
database rows, scripts, paths, or runtime state.

The probe parsed the generated query stream structurally: it read the stream
length, each record length and flag, and the two QUERY_LINK values. It did not
grep raw byte sequences.

~~~text
OWNER_PRIVATE_LINK_PRESENT=PASS
OPPONENT_HIDDEN_LINK_REDACTED=PASS
FACEUP_PUBLIC_LINK_PRESENT=PASS
EXPLICIT_PUBLIC_LINK_PRESENT=PASS
HIDDEN_HAND_PRIVACY=PASS
HIDDEN_EXTRA_PRIVACY=PASS
FACEDOWN_FIELD_PRIVACY=PASS
PUBLIC_ENTITY_LINK_EVIDENCE=PASS
~~~

The result follows the pinned Query::IsPublicQuery rule: QUERY_LINK is
private unless the query is explicitly public or its position is face-up.
The patch does not modify GenerateBuffer, GeneratePublicBuffer, or
IsPublicQuery. Hidden identity is never recovered and no Printed-provider
lookup is involved.

## 7. Temporary synthetic probe

The probe source and executable reside under the temporary directory
ocgforge-i6c6-link-query-probe, outside both repositories. The source is
diagnostic-only and is not part of this commit.

The probe used the pinned CoreUtils path:

~~~text
CoreUtils::QueryStream
CoreUtils::QueryStream::GenerateBuffer
CoreUtils::QueryStream::GeneratePublicBuffer
CoreUtils::Query::IsPublicQuery
~~~

Its two-process comparison produced byte-equivalent classified output:

~~~text
PROBE_RUN=PASS
PROBE_FRESH_PROCESS_DETERMINISM=PASS
~~~

## 8. I6C6 boundary proof

The pinned GenericDuel source calls the default refresh methods at the
existing normal 1v1 boundaries. The startup path calls RefreshExtra for both
players at generic_duel.cpp:722-725. AfterParsing calls RefreshHand,
RefreshExtra, and RefreshGrave for the existing Draw, Shuffle, and
SwapGraveDeck paths at generic_duel.cpp:1143-1257. The helper implementations
at generic_duel.cpp:1354-1395 pass those masks into DuelQueryLocation and then
through the owner/public filtering path.

The patch therefore makes QUERY_LINK available at the existing boundary; it
does not wait for a later RefreshSingle and does not move the comparison
boundary.

~~~text
PATCHED_DEFAULTS_REACH_I6C6_BOUNDARY=PASS
BOUNDARY_DODGE=NO
LATER_REFRESH_DEPENDENCY=NO
~~~

The source-level boundary proof is complete. A real scenario-level value
proof remains separate below.

## 9. Local/private runtime smoke

The patched EDOPro binary was built successfully, but the existing local
worktrees contain no authorized operator-provisioned 1v1 card/script runtime
scenario that can exercise a publicly known Link entity. No real card data,
CDB, CardScripts, or new fixture was downloaded or created to manufacture one.

Accordingly:

~~~text
RUNTIME_LINK_BOUNDARY_EVIDENCE=UNPROVEN
RUNTIME_SMOKE_REASON=
NO_AUTHORIZED_LOCAL_1V1_SCENARIO_WITH_PROVISIONED_ASSETS_AVAILABLE
OWNER_QUERY_LINK_PRESENT=UNPROVEN
LINK_RATING_PRESENT=UNPROVEN
LINK_MARKERS_PRESENT=UNPROVEN
HIDDEN_EXTRA_QUERY_LINK_ABSENT=UNPROVEN
~~~

This is a deliberate fail-closed evidence classification. It does not weaken
the source or privacy result and does not claim I6C6 closure.

## 10. Determinism

The patchset identity is computed from raw binary Git diff bytes, not from
working-copy text conversion:

~~~text
DIFF_COMMAND=
git diff --binary --no-ext-diff --src-prefix=a/ --dst-prefix=b/
30935e847165a9ef0e547fb51a43f36168fab7c7
edfb77d7baf68b986209d327a871021296c2954b
--
gframe/generic_duel.h
~~~

The privacy probe produced identical output in two fresh processes. The
normal EDOPro runtime smoke was not available, so its process-determinism
gate remains explicitly unproven.

~~~text
PRIVACY_PROBE_FRESH_PROCESS_DETERMINISM=PASS
RUNTIME_EVIDENCE_FRESH_PROCESS_DETERMINISM=UNPROVEN
~~~

No semantic evidence includes process IDs, times, paths, socket timing,
machine identity, or pointers.

## 11. Patchset identity

~~~text
PATCHSET_SHA256=fd97edae44cb07a0b43f477f14863c40177eb4ac9a804d7c425450f07d5894d7
PATCHSET_ID=
ocgforge.edopro_i6c6_link_query_evidence.v1.fd97edae44cb07a0b43f477f14863c40177eb4ac9a804d7c425450f07d5894d7
~~~

The committed diff contains 900 raw diff bytes and exactly one changed
tracked EDOPro file. The patch commit is source/build provenance, not a
gameplay semantic hash.

## 12. Third-party boundary

The local build used only externally provisioned build dependencies in
temporary ignored directories. No EDOPro source, ocgcore source, Lua source,
binary, CDB, BabelCDB row, generated card-data artifact, or CardScripts file
was added to OCGForge-Ignis.

This record does not certify legal acquisition or use of any external
runtime. It records only the project-authorized local/private test boundary.

~~~text
BABELCDB_REDISTRIBUTION_AUTHORIZED=NO
DERIVED_ARTIFACT_REDISTRIBUTION_AUTHORIZED=NO
EDOPRO_PATCH_DISTRIBUTION_AUTHORIZED=NO
PUBLIC_EDOPRO_SERVER_MODIFICATION_AUTHORIZED=NO
~~~

## 13. Acceptance gates

~~~text
HAND_QUERY_LINK_ONLY_DELTA=PASS
GRAVE_QUERY_LINK_ONLY_DELTA=PASS
EXTRA_QUERY_LINK_ONLY_DELTA=PASS

OWNER_PRIVATE_LINK_PRESENT=PASS
OPPONENT_HIDDEN_LINK_REDACTED=PASS
FACEUP_PUBLIC_LINK_PRESENT=PASS
EXPLICIT_PUBLIC_LINK_PRESENT=PASS
HIDDEN_HAND_PRIVACY=PASS
HIDDEN_EXTRA_PRIVACY=PASS
FACEDOWN_FIELD_PRIVACY=PASS
PUBLIC_ENTITY_LINK_EVIDENCE=PASS

PATCHED_DEFAULTS_REACH_I6C6_BOUNDARY=PASS
BOUNDARY_DODGE=NO
LATER_REFRESH_DEPENDENCY=NO

EDOPRO_BUILD=PASS
NEW_BUILD_WARNINGS=0
SOURCE_RECONSTRUCTION_FROM_PIN_PLUS_PATCH=PASS
BINARY_BYTE_REPRODUCIBILITY=NOT_CLAIMED

CORE_UTILS_CHANGED=NO
NETWORK_PROTOCOL_CHANGED=NO
OCGCORE_CHANGED=NO
CARDSCRIPTS_CHANGED=NO
CARD_DATA_CHANGED=NO
RULES_CHANGED=NO
RNG_CHANGED=NO
MESSAGE_ORDER_CHANGED=NO

COUNTER_BRANCH_CHANGED=NO
COUNTER_BRANCH_MERGED=NO
COUNTER_CODE_TOUCHED=NO
~~~

The successful EDOPro build used the established Premake/Visual Studio
procedure with the pinned local vcpkg cache. Warnings were emitted only from
unchanged pinned Lua/EDOPro source files; no new warning originated in the
patched header, so NEW_BUILD_WARNINGS is zero. Binary reproducibility is not
claimed.

## 14. Final status

~~~text
IGNIS_FILES_CHANGED=1
IGNIS_PRODUCTION_CODE_CHANGED=NO
IGNIS_TEST_CODE_CHANGED=NO
IGNIS_ADR_CHANGED=NO

EDOPRO_PATCH_FILES_CHANGED=1
EDOPRO_PATCH_WORKTREE_CLEAN=YES
EDOPRO_PATCH_PUSHED=NO

DIFF_CHECK_EDOPRO=PASS
DIFF_CHECK_IGNIS=PASS
PLACEHOLDER_SCAN=PASS
SCOPE_CHECK=PASS

RUNTIME_LINK_BOUNDARY_EVIDENCE=UNPROVEN
I6C6_3A2_READY_FOR_INDEPENDENT_REVIEW=YES

I6C6_3_FINAL=NO
I6C6_4_AUTHORIZED=NO
I6C6_5_AUTHORIZED=NO
I6D_AUTHORIZED=NO
I7_AUTHORIZED=NO
~~~

The missing runtime smoke evidence is the only deliberately unresolved
acceptance item. No fallback, boundary reduction, or data acquisition was
used to obtain a passing result.
