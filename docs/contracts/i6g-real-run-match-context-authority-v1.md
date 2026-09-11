# I6G Real-Run Match Context Authority Contract V1

Contract ID: `ocgforge-ignis.i6g-real-run-match-context-authority.v1`
Status: FROZEN DOCUMENTATION-ONLY AUTHORITY CONTRACT
Date: 2026-09-12

```text
REAL_RUN_MATCH_CONTEXT_AUTHORITY_FROZEN=YES
MATCH_CONTEXT_IMPLEMENTATION_AUTHORIZED=NO
RUNTIME_BINDING_AUTHORIZED=NO
```

This contract freezes where the five fields of
`PerspectiveSafeMatchContextV1` come from when an I6G real run is eventually
constructed. It does not implement that binding, create a context, authorize
a runtime attempt, or change the public observation schema.

## 1. Scope and ownership

The context is configuration and knowledge policy. It is not inferred from
the current mirror, revealed cards, participant process state, or the files
that the harness happens to know for operational purposes.

The ownership chain is:

```text
explicit real-run match-context configuration
    → frozen PerspectiveSafeMatchContextV1
    → I6C6 request transports and validates it
    → I6C5 public-frame source consumes it
    → I6D binds it to the same accepted frame snapshot
```

The explicit real-run configuration is the future authority owner for the
five values. `I6C6ClosureScenarioConfigurationV1` remains an operational
scenario/provenance record; it is not itself a match-context authority.
`I6C6OpponentRuntimeParticipantLeaseV1` proves participant provenance only.
Neither object may silently construct or upgrade player knowledge.

`PerspectiveSafeMatchContextV1` is caller-supplied to the current I6C6
request surface. Until an explicit configuration owner supplies and freezes
all required values, the real run is not constructible.

## 2. Field authority

The five context components have these authorities and limits:

| Context component | Authoritative source | Required rule | Forbidden substitution |
| --- | --- | --- | --- |
| `PerspectivePlayer` | The bound I1 `GameplayPerspectiveV1.PlayerType` for the real-run perspective | Must be an absolute `p0` or `p1` value and agree with the accepted gameplay perspective | Process role, executable path, PID, deck path, or participant order |
| `DuelFlags` | An explicit real-run/scenario configuration value used by the I2 public projection context | Must be supplied before projection and agree with the accepted frame globals | Default value, EDOPro GUI state, card code, zone shape, or inferred layout |
| `OwnDeck` | A complete canonical list explicitly bound by the real-run context owner to the primary deck input | A known list must be nonzero and canonically sorted; a path or hash alone is not the list | Deck counts, runtime reveals, process arguments, or an unverified `.ydk` path |
| `OpponentDeck` | A complete canonical list only when the explicit context policy declares opponent full knowledge | Without that declaration it is `Known=false` with empty main and extra lists | The opponent deck path/hash, participant lease, WindBot arguments, or revealed cards |
| `Knowledge` | Explicit context policy | `OwnDecklistKnown` must equal `OwnDeck.Known`; `OpponentDecklistKnown` must equal `OpponentDeck.Known` | Inference from harness access, runtime visibility, or partial reveals |

The context owner MUST bind the values to the selected real-run scenario, but
scenario identity and forensic inputs remain separate provenance. They are not
additional public-context fields and must not be copied into public state,
model input, replay identity, or semantic hashes merely because they were used
to validate the run.

## 3. Deck knowledge policy

Operational harness knowledge and perspective knowledge are different
authorities:

```text
harness knows an exact deck path/hash
    !=
perspective player knows the corresponding decklist
```

For a `Known=false` deck, both `MainDeck` and `ExtraDeck` MUST be empty. For a
`Known=true` deck, the context owner MUST supply the complete canonical lists;
each passcode MUST be nonzero and each list MUST use the established canonical
ordering. A partial, unsorted, inferred, or reveal-derived list is invalid.

`OwnDecklistKnown` MAY be true only when the explicit context policy declares
the primary decklist known and binds its complete canonical list. The primary
deck path used by I6C6 to load the duel is not, by itself, a knowledge-policy
declaration.

`OpponentDecklistKnown` MAY be true only under an explicit full-knowledge or
open-deck match configuration that supplies the complete opponent list. The
current Counter scenario has an opponent deck path for participant launch, but
the current I6C6 scenario configuration contains no such open-deck
declaration. A future binding MUST therefore keep the opponent deck unknown
and its lists empty unless that explicit policy is added and independently
accepted.

No runtime reveal, public card event, participant process, or printed-provider
lookup may upgrade either knowledge bit. Knowledge-destroying transitions
also cannot be used to reconstruct a decklist.

## 4. Required construction and validation order

A future real-run implementation MUST use this order:

```text
select the exact scenario
    → obtain explicit perspective and duel-layout configuration
    → obtain explicit deck/knowledge policy
    → bind complete known deck lists or empty unknown lists
    → validate Knowledge against deck Known values
    → validate nonzero canonical ordering for known lists
    → construct immutable PerspectiveSafeMatchContextV1
    → bind it to the run request before I6C5/I6D composition
```

Missing configuration, contradictory knowledge, a known deck without a
complete list, an unknown deck with list entries, invalid passcodes, or
unsorted lists MUST fail closed before a real run is constructed. There is no
fallback to the scenario file, EDOPro `DataManager`, ambient filesystem state,
runtime card reveals, or a default duel-flag value.

## 5. Layer boundaries

```text
I1 GameplayPerspectiveV1
    supplies the established perspective authority

I6G real-run context owner
    supplies and freezes explicit DuelFlags, deck policy, and complete lists

I6C6RealRunRequestV1
    carries the already-frozen context and rejects its absence

I6C5 public-frame source
    validates/consumes context and does not infer it from the mirror

I6D same-snapshot composition
    binds the context to the accepted frame but does not create or mutate it

Model/replay/evidence layers
    consume the accepted context semantics but do not become its authority
```

Process ownership, executable/deck/port/version provenance, runtime hashes,
PIDs, and artifact forensic fields remain outside this context. They may be
retained in a separate acceptance/provenance envelope only.

## 6. Current baseline and authorization state

The current repository has no explicit I6G real-run match-context
configuration owner or implementation. The existing synthetic test helpers
construct contexts for contract tests only; they are not Counter runtime
inputs.

```text
CURRENT_I6G_MATCH_CONTEXT_CONFIGURATION=ABSENT
CURRENT_COUNTER_MATCH_CONTEXT_AUTHORITY=UNRESOLVED
CURRENT_COUNTER_RUN_CONSTRUCTIBLE=NO
SYNTHETIC_MATCH_CONTEXT_FOR_REAL_RUN=FORBIDDEN
```

This contract authorizes no implementation, provider provisioning, runtime
attempt, Counter capture, Link capture, fresh-process comparison, or I7 work.
Those require separate authorization after the missing authority is supplied
and independently reviewed.

## 7. Acceptance invariants

```text
HARNESS_KNOWS_DECK_PATH_IS_PLAYER_KNOWLEDGE=NO
OPPONENT_DECK_PATH_IMPLIES_OPPONENT_KNOWLEDGE=NO
DUEL_FLAGS_EXPLICIT=YES
KNOWLEDGE_EQUALS_DECK_KNOWN=YES
UNKNOWN_DECK_LISTS_EMPTY=YES
KNOWN_DECK_LISTS_COMPLETE_NONZERO_SORTED=YES
REVEAL_DERIVED_DECK_KNOWLEDGE=NO
CONTEXT_MISSING_OR_CONTRADICTORY=FAIL_CLOSED
```

No value in this contract changes public-state bytes, public action identity,
replay identity, or model schema. It only prevents an unproven real-run
configuration from being presented as an accepted perspective-safe context.
