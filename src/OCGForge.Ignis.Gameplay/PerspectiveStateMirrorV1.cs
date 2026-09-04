using System.Text;

namespace OCGForge.Ignis.Gameplay;

public sealed class PerspectiveStateMirrorV1
{
    private const uint MaximumAuthoritativeLifePoints = int.MaxValue;
    private const byte LocationDeck = 0x01;
    private const byte LocationHand = 0x02;
    private const byte LocationMonster = 0x04;
    private const byte LocationSpellTrap = 0x08;
    private const byte LocationGraveyard = 0x10;
    private const byte LocationBanished = 0x20;
    private const byte LocationExtra = 0x40;
    private const byte LocationOverlay = 0x80;
    private const byte PositionFaceUp = 0x05;
    private const byte PositionFaceDown = 0x0a;
    private const uint MaximumMonsterSequence = 6;
    private const uint MaximumSpellTrapSequence = 7;

    private MirrorState state;

    private PerspectiveStateMirrorV1(MirrorState state)
    {
        this.state = state;
    }

    public MirrorSnapshotV1 Snapshot => CreateSnapshot(state);

    public static MirrorCreateResult TryCreate(
        GameplayMessageV1 start,
        GameplayPerspectiveV1 perspective)
    {
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(perspective);
        if (start.Kind != GameplayMessageKindV1.Start ||
            start.Start.PlayerType != perspective.PlayerType)
        {
            return MirrorCreateResult.Failure(
                GameplayErrorCode.InvalidStateTransition);
        }

        if (start.Start.LifePoints0 > MaximumAuthoritativeLifePoints ||
            start.Start.LifePoints1 > MaximumAuthoritativeLifePoints)
        {
            return MirrorCreateResult.Failure(
                GameplayErrorCode.ArithmeticFailure);
        }

        MirrorState initial = new(perspective);
        initial.LifePoints[0] = start.Start.LifePoints0;
        initial.LifePoints[1] = start.Start.LifePoints1;
        initial.ZoneCounts[0, (int)MirrorZoneV1.MainDeck] = start.Start.DeckCount0;
        initial.ZoneCounts[0, (int)MirrorZoneV1.ExtraDeck] = start.Start.ExtraCount0;
        initial.ZoneCounts[1, (int)MirrorZoneV1.MainDeck] = start.Start.DeckCount1;
        initial.ZoneCounts[1, (int)MirrorZoneV1.ExtraDeck] = start.Start.ExtraCount1;
        for (int player = 0; player < 2; player++)
        {
            for (int zone = 2; zone < initial.ZoneCounts.GetLength(1); zone++)
            {
                initial.ZoneCounts[player, zone] = 0;
            }
        }

        return MirrorCreateResult.Success(new PerspectiveStateMirrorV1(initial));
    }

    public MirrorApplyResult Apply(GameplayMessageV1 message)
    {
        ArgumentNullException.ThrowIfNull(message);
        MirrorSnapshotV1 before = CreateSnapshot(state);
        if (state.Terminal.IsTerminal)
        {
            return MirrorApplyResult.Failure(
                GameplayErrorCode.TerminalStateMutation,
                before);
        }

        MirrorState candidate = state.Clone();
        GameplayErrorCode error = ApplyTo(candidate, message);
        if (error != GameplayErrorCode.None)
        {
            return MirrorApplyResult.Failure(error, before);
        }

        error = ValidateState(candidate);
        if (error != GameplayErrorCode.None)
        {
            return MirrorApplyResult.Failure(error, before);
        }

        state = candidate;
        return MirrorApplyResult.Success(CreateSnapshot(state));
    }

    private static GameplayErrorCode ApplyTo(MirrorState candidate, GameplayMessageV1 message) =>
        message.Kind switch
        {
            GameplayMessageKindV1.Start => GameplayErrorCode.DuplicatePerspective,
            GameplayMessageKindV1.Win => ApplyWin(candidate, message.Win),
            GameplayMessageKindV1.UpdateData => ApplyUpdateData(candidate, message.UpdateData!),
            GameplayMessageKindV1.UpdateCard => ApplyUpdateCard(candidate, message.UpdateCard),
            GameplayMessageKindV1.NewTurn => ApplyNewTurn(candidate, message.NewTurn),
            GameplayMessageKindV1.NewPhase => ApplyNewPhase(candidate, message.NewPhase),
            GameplayMessageKindV1.Move => ApplyMove(candidate, message.Move),
            GameplayMessageKindV1.PosChange => ApplyPositionChange(candidate, message.PositionChange),
            GameplayMessageKindV1.Set => ApplySet(candidate, message.Set),
            GameplayMessageKindV1.Swap => ApplySwap(candidate, message.Swap),
            GameplayMessageKindV1.Chaining => ApplyChaining(candidate, message.Chaining),
            GameplayMessageKindV1.Chained => ApplyChainSize(candidate, message.ChainSize, message.Kind),
            GameplayMessageKindV1.ChainSolving => ApplyChainSize(candidate, message.ChainSize, message.Kind),
            GameplayMessageKindV1.ChainSolved => ApplyChainSize(candidate, message.ChainSize, message.Kind),
            GameplayMessageKindV1.ChainEnd => ApplyChainEnd(candidate),
            GameplayMessageKindV1.ChainNegated => ApplyChainSize(candidate, message.ChainSize, message.Kind),
            GameplayMessageKindV1.ChainDisabled => ApplyChainSize(candidate, message.ChainSize, message.Kind),
            GameplayMessageKindV1.BecomeTarget => ApplyBecomeTarget(candidate, message.BecomeTarget!),
            GameplayMessageKindV1.Draw => ApplyDraw(candidate, message.Draw!),
            GameplayMessageKindV1.Damage => ApplyLifePoints(candidate, message.LifePoints, message.Kind),
            GameplayMessageKindV1.Recover => ApplyLifePoints(candidate, message.LifePoints, message.Kind),
            GameplayMessageKindV1.Equip => ApplyEquip(candidate, message.Equip),
            GameplayMessageKindV1.LpUpdate => ApplyLifePoints(candidate, message.LifePoints, message.Kind),
            GameplayMessageKindV1.Unequip => ApplyUnequip(candidate, message.Unequip),
            GameplayMessageKindV1.CardTarget => ApplyCardTarget(candidate, message.CardTarget, cancel: false),
            GameplayMessageKindV1.CancelTarget => ApplyCardTarget(candidate, message.CardTarget, cancel: true),
            GameplayMessageKindV1.PayLpCost => ApplyLifePoints(candidate, message.LifePoints, message.Kind),
            _ => GameplayErrorCode.UnsupportedMessage
        };

    private static GameplayErrorCode ApplyWin(
        MirrorState candidate,
        GameplayWinPayloadV1 payload)
    {
        if (payload.Player > 2)
        {
            return GameplayErrorCode.InvalidParticipant;
        }

        MirrorParticipantRoleV1? winner = payload.Player <= 1
            ? MapPlayer(candidate.Perspective, payload.Player)
            : null;
        candidate.Terminal = new MirrorTerminalSnapshotV1(
            true,
            winner,
            payload.WinType);
        return GameplayErrorCode.None;
    }

    private static GameplayErrorCode ApplyNewTurn(
        MirrorState candidate,
        GameplayNewTurnPayloadV1 payload)
    {
        if (payload.Player > 1)
        {
            return GameplayErrorCode.InvalidParticipant;
        }

        try
        {
            candidate.TurnCount = checked(candidate.TurnCount + 1);
        }
        catch (OverflowException)
        {
            return GameplayErrorCode.ArithmeticFailure;
        }

        candidate.TurnPlayer = MirrorValueV1.Known(
            MapPlayer(candidate.Perspective, payload.Player));
        return GameplayErrorCode.None;
    }

    private static GameplayErrorCode ApplyNewPhase(
        MirrorState candidate,
        GameplayNewPhasePayloadV1 payload)
    {
        candidate.Phase = MirrorValueV1.Known(payload.Phase);
        return GameplayErrorCode.None;
    }

    private static GameplayErrorCode ApplySet(
        MirrorState candidate,
        GameplaySetPayloadV1 payload)
    {
        return TryNormalizeAddress(payload.Location, out _, out GameplayErrorCode error)
            ? GameplayErrorCode.None
            : error;
    }

    private static GameplayErrorCode ApplyPositionChange(
        MirrorState candidate,
        GameplayPositionChangePayloadV1 payload)
    {
        if (payload.Controller > 1)
        {
            return GameplayErrorCode.InvalidParticipant;
        }

        ModernLocInfoV1 location = new(
            payload.Controller,
            payload.Location,
            payload.Sequence,
            0);
        if (!TryNormalizeAddress(location, out MirrorAddress address, out GameplayErrorCode error) ||
            address.IsOverlay)
        {
            return address.IsOverlay
                ? GameplayErrorCode.InvalidLocation
                : error;
        }

        if (!IsSequenceWithinPileCount(candidate, address))
        {
            return GameplayErrorCode.StateCapacityExceeded;
        }

        if (!candidate.Entities.TryGetValue(address, out EntityState? entity))
        {
            return GameplayErrorCode.UnknownMirrorReference;
        }

        if (entity.Position.IsKnown &&
            entity.Position.Value != payload.PreviousPosition)
        {
            return GameplayErrorCode.ConflictingSlotOccupancy;
        }

        entity.Position = MirrorValueV1.Known((uint)payload.CurrentPosition);
        if ((payload.PreviousPosition & PositionFaceUp) != 0 &&
            (payload.CurrentPosition & PositionFaceDown) != 0)
        {
            RemoveEntityRelations(candidate, entity.Id);
            entity.CardCode = MirrorValueV1.Unknown<uint>();
            entity.QueryFields.Clear();
        }

        if (payload.CardCode != 0)
        {
            ApplyCardCodeObservation(
                candidate,
                entity,
                address,
                payload.CardCode,
                payload.CurrentPosition);
        }

        return GameplayErrorCode.None;
    }

    private static GameplayErrorCode ApplyMove(
        MirrorState candidate,
        GameplayMovePayloadV1 payload)
    {
        bool previousEmpty = payload.Previous.Location == 0;
        bool currentEmpty = payload.Current.Location == 0;
        if (previousEmpty && currentEmpty)
        {
            return GameplayErrorCode.InvalidLocation;
        }

        MirrorAddress current = default;
        if (!currentEmpty &&
            !TryNormalizeAddress(payload.Current, out current, out GameplayErrorCode currentError))
        {
            return currentError;
        }

        if (previousEmpty)
        {
            if (candidate.Entities.ContainsKey(current))
            {
                return GameplayErrorCode.ConflictingSlotOccupancy;
            }

            if (!ValidateOverlayParent(candidate, current))
            {
                return GameplayErrorCode.UnknownMirrorReference;
            }

            if (!AdjustZoneCounts(candidate, null, current, out GameplayErrorCode countError))
            {
                return countError;
            }

            if (!IsSequenceWithinPileCount(candidate, current))
            {
                return GameplayErrorCode.StateCapacityExceeded;
            }

            if (!TryCreateEntity(
                    candidate,
                    current,
                    payload.CardCode,
                    payload.Current.Position,
                    payload.CardCode != 0,
                    out EntityState? created,
                    out GameplayErrorCode createError))
            {
                return createError;
            }

            candidate.Entities.Add(current, created!);
            return UpdateOverlayRelation(candidate, created!.Id, current);
        }

        if (!TryNormalizeAddress(
                payload.Previous,
                out MirrorAddress previous,
                out GameplayErrorCode previousError))
        {
            return previousError;
        }

        if (!candidate.Entities.TryGetValue(previous, out EntityState? entity))
        {
            return GameplayErrorCode.UnknownMirrorReference;
        }

        if (HasOverlayChildren(candidate, entity.Id))
        {
            return GameplayErrorCode.InvalidStateTransition;
        }

        if (currentEmpty)
        {
            if (!CanRemoveEntity(candidate, entity))
            {
                return GameplayErrorCode.ConflictingSlotOccupancy;
            }

            if (!AdjustZoneCounts(candidate, previous, null, out GameplayErrorCode countError))
            {
                return countError;
            }

            if (!RemoveEntityAtAddress(
                    candidate,
                    previous,
                    out GameplayErrorCode removeError))
            {
                return removeError;
            }

            RemoveEntityRelations(candidate, entity.Id);
            return GameplayErrorCode.None;
        }

        if (previous.Equals(current))
        {
            return GameplayErrorCode.InvalidStateTransition;
        }

        bool samePile = !current.IsOverlay &&
                        !previous.IsOverlay &&
                        previous.Controller == current.Controller &&
                        previous.Zone == current.Zone &&
                        IsPileZone(current.Zone);
        if (!samePile && !current.IsOverlay && candidate.Entities.ContainsKey(current))
        {
            return GameplayErrorCode.ConflictingSlotOccupancy;
        }

        if (!ValidateOverlayParent(candidate, current))
        {
            return GameplayErrorCode.UnknownMirrorReference;
        }

        if (!AdjustZoneCounts(candidate, previous, current, out GameplayErrorCode movementCountError))
        {
            return movementCountError;
        }

        if (!IsSequenceWithinPileCount(candidate, current))
        {
            return GameplayErrorCode.StateCapacityExceeded;
        }

        if (!RemoveEntityAtAddress(
                candidate,
                previous,
                out GameplayErrorCode sourceRemoveError))
        {
            return sourceRemoveError;
        }

        if (samePile)
        {
            if (!ReindexPileForInsertion(
                    candidate,
                    current,
                    out GameplayErrorCode insertionError))
            {
                return insertionError;
            }
        }

        if (candidate.Entities.ContainsKey(current))
        {
            return GameplayErrorCode.ConflictingSlotOccupancy;
        }

        entity.Address = current;
        entity.Position = current.IsOverlay
            ? MirrorValueV1.Unknown<uint>()
            : MirrorValueV1.Known(payload.Current.Position);
        bool currentFaceDown = !current.IsOverlay &&
                               (payload.Current.Position & PositionFaceDown) != 0;
        if (currentFaceDown && !samePile)
        {
            RemoveEntityRelations(candidate, entity.Id);
            entity.CardCode = MirrorValueV1.Unknown<uint>();
            entity.QueryFields.Clear();
        }

        if (payload.CardCode != 0)
        {
            ApplyCardCodeObservation(
                candidate,
                entity,
                current,
                payload.CardCode,
                payload.Current.Position);
        }

        candidate.Entities.Add(current, entity);

        if (previous.Zone != current.Zone ||
            previous.IsOverlay != current.IsOverlay)
        {
            RemoveEntityRelations(candidate, entity.Id);
        }

        return UpdateOverlayRelation(candidate, entity.Id, current);
    }

    private static GameplayErrorCode ApplySwap(
        MirrorState candidate,
        GameplaySwapPayloadV1 payload)
    {
        GameplayErrorCode firstError;
        if (!TryNormalizeAddress(payload.Location0, out MirrorAddress first, out firstError))
        {
            return firstError;
        }

        if (!TryNormalizeAddress(
                payload.Location1,
                out MirrorAddress second,
                out GameplayErrorCode secondError))
        {
            return secondError;
        }

        if (first.IsOverlay || second.IsOverlay || first.Equals(second))
        {
            return GameplayErrorCode.InvalidStateTransition;
        }

        if (IsPileZone(first.Zone) || IsPileZone(second.Zone))
        {
            return GameplayErrorCode.InvalidStateTransition;
        }

        if (!candidate.Entities.TryGetValue(first, out EntityState? firstEntity) ||
            !candidate.Entities.TryGetValue(second, out EntityState? secondEntity))
        {
            return GameplayErrorCode.UnknownMirrorReference;
        }

        candidate.Entities.Remove(first);
        candidate.Entities.Remove(second);
        firstEntity.Address = second;
        secondEntity.Address = first;
        candidate.Entities.Add(second, firstEntity);
        candidate.Entities.Add(first, secondEntity);
        return GameplayErrorCode.None;
    }

    private static bool RemoveEntityAtAddress(
        MirrorState candidate,
        MirrorAddress address,
        out GameplayErrorCode error)
    {
        error = GameplayErrorCode.None;
        if (!candidate.Entities.Remove(address))
        {
            error = GameplayErrorCode.UnknownMirrorReference;
            return false;
        }

        if (address.IsOverlay)
        {
            ReindexOverlaySiblings(candidate, address);
        }
        else if (IsPileZone(address.Zone))
        {
            ReindexPileAfterRemoval(candidate, address);
        }

        return true;
    }

    private static void ReindexPileAfterRemoval(
        MirrorState candidate,
        MirrorAddress removed)
    {
        EntityState[] shifted = candidate.Entities.Values
            .Where(entity => !entity.Address.IsOverlay &&
                             entity.Address.Controller == removed.Controller &&
                             entity.Address.Zone == removed.Zone &&
                             entity.Address.Sequence > removed.Sequence)
            .OrderBy(entity => entity.Address.Sequence)
            .ToArray();

        foreach (EntityState entity in shifted)
        {
            candidate.Entities.Remove(entity.Address);
        }

        foreach (EntityState entity in shifted)
        {
            entity.Address = new MirrorAddress(
                entity.Address.Controller,
                entity.Address.Zone,
                entity.Address.Sequence - 1,
                false,
                0);
            candidate.Entities.Add(entity.Address, entity);
        }
    }

    private static bool ReindexPileForInsertion(
        MirrorState candidate,
        MirrorAddress inserted,
        out GameplayErrorCode error)
    {
        error = GameplayErrorCode.None;
        EntityState[] shifted = candidate.Entities.Values
            .Where(entity => !entity.Address.IsOverlay &&
                             entity.Address.Controller == inserted.Controller &&
                             entity.Address.Zone == inserted.Zone &&
                             entity.Address.Sequence >= inserted.Sequence)
            .OrderByDescending(entity => entity.Address.Sequence)
            .ToArray();

        if (shifted.Any(entity => entity.Address.Sequence == uint.MaxValue))
        {
            error = GameplayErrorCode.ArithmeticFailure;
            return false;
        }

        foreach (EntityState entity in shifted)
        {
            candidate.Entities.Remove(entity.Address);
        }

        foreach (EntityState entity in shifted)
        {
            entity.Address = new MirrorAddress(
                entity.Address.Controller,
                entity.Address.Zone,
                entity.Address.Sequence + 1,
                false,
                0);
            candidate.Entities.Add(entity.Address, entity);
        }

        return true;
    }

    private static void ReindexOverlaySiblings(
        MirrorState candidate,
        MirrorAddress removed)
    {
        EntityState[] shifted = candidate.Entities.Values
            .Where(entity => entity.Address.IsOverlay &&
                             entity.Address.Controller == removed.Controller &&
                             entity.Address.Zone == removed.Zone &&
                             entity.Address.Sequence == removed.Sequence &&
                             entity.Address.OverlayIndex > removed.OverlayIndex)
            .OrderBy(entity => entity.Address.OverlayIndex)
            .ToArray();

        foreach (EntityState entity in shifted)
        {
            candidate.Entities.Remove(entity.Address);
        }

        foreach (EntityState entity in shifted)
        {
            entity.Address = new MirrorAddress(
                entity.Address.Controller,
                entity.Address.Zone,
                entity.Address.Sequence,
                true,
                entity.Address.OverlayIndex - 1);
            candidate.Entities.Add(entity.Address, entity);
        }
    }

    private static bool HasOverlayChildren(
        MirrorState candidate,
        MirrorEntityIdV1 entityId) =>
        candidate.OverlayRelations.Any(relation => relation.Source == entityId);

    private static bool IsPileZone(MirrorZoneV1 zone) =>
        zone is MirrorZoneV1.MainDeck or
            MirrorZoneV1.ExtraDeck or
            MirrorZoneV1.Hand or
            MirrorZoneV1.Graveyard or
            MirrorZoneV1.Banished;

    private static bool IsSequenceWithinPileCount(
        MirrorState candidate,
        MirrorAddress address) =>
        !IsPileZone(address.Zone) ||
        address.Sequence < candidate.ZoneCounts[address.Controller, (int)address.Zone];

    private static GameplayErrorCode ValidateState(MirrorState candidate)
    {
        HashSet<MirrorEntityIdV1> entityIds = new();
        uint[,] represented = new uint[2, 7];
        foreach ((MirrorAddress address, EntityState entity) in candidate.Entities)
        {
            if (!address.Equals(entity.Address) ||
                !entityIds.Add(entity.Id) ||
                entity.Id.Ordinal == 0)
            {
                return GameplayErrorCode.InvalidState;
            }

            if (address.IsOverlay)
            {
                MirrorAddress parent = new(
                    address.Controller,
                    address.Zone,
                    address.Sequence,
                    false,
                    0);
                if (!candidate.Entities.TryGetValue(parent, out EntityState? parentEntity))
                {
                    return GameplayErrorCode.UnknownMirrorReference;
                }

                RelationState? overlayRelation = candidate.OverlayRelations.Find(
                    relation => relation.Target == entity.Id);
                if (overlayRelation is null ||
                    overlayRelation.Source != parentEntity.Id)
                {
                    return GameplayErrorCode.InvalidRelation;
                }
            }
            else
            {
                represented[address.Controller, (int)address.Zone]++;
            }
        }

        for (int player = 0; player < 2; player++)
        {
            for (int zone = 0; zone < candidate.ZoneCounts.GetLength(1); zone++)
            {
                if (represented[player, zone] > candidate.ZoneCounts[player, zone])
                {
                    return GameplayErrorCode.StateCapacityExceeded;
                }
            }
        }

        if (!ValidateRelations(candidate.TargetRelations, entityIds) ||
            !ValidateRelations(candidate.ChainTargetRelations, entityIds) ||
            !ValidateRelations(candidate.EquipmentRelations, entityIds) ||
            !ValidateRelations(candidate.OverlayRelations, entityIds))
        {
            return GameplayErrorCode.UnknownMirrorReference;
        }

        foreach (ChainState chain in candidate.Chains)
        {
            if (chain.ChainSize == 0 ||
                !entityIds.Contains(chain.Card) ||
                chain.Targets.Any(target => !entityIds.Contains(target)))
            {
                return GameplayErrorCode.InvalidChainState;
            }
        }

        if (candidate.PendingChain is not null &&
            !entityIds.Contains(candidate.PendingChain.Card))
        {
            return GameplayErrorCode.UnknownMirrorReference;
        }

        return GameplayErrorCode.None;
    }

    private static bool ValidateRelations(
        IEnumerable<RelationState> relations,
        HashSet<MirrorEntityIdV1> entityIds)
    {
        HashSet<ulong> ordinals = new();
        foreach (RelationState relation in relations)
        {
            if (relation.Ordinal == 0 ||
                !ordinals.Add(relation.Ordinal) ||
                !entityIds.Contains(relation.Source) ||
                !entityIds.Contains(relation.Target) ||
                relation.Source == relation.Target)
            {
                return false;
            }
        }

        return true;
    }

    private static GameplayErrorCode ApplyUpdateCard(
        MirrorState candidate,
        GameplayUpdateCardPayloadV1 payload)
    {
        if (payload.Player > 1)
        {
            return GameplayErrorCode.InvalidParticipant;
        }

        ModernLocInfoV1 location = new(
            payload.Player,
            payload.Location,
            payload.Sequence,
            0);
        if (!TryNormalizeAddress(
                location,
                out MirrorAddress address,
                out GameplayErrorCode error) || address.IsOverlay)
        {
            return address.IsOverlay
                ? GameplayErrorCode.InvalidLocation
                : error;
        }

        if (!IsSequenceWithinPileCount(candidate, address))
        {
            return GameplayErrorCode.StateCapacityExceeded;
        }

        if (!candidate.Entities.TryGetValue(address, out EntityState? entity))
        {
            return GameplayErrorCode.UnknownMirrorReference;
        }

        return ApplyQuery(candidate, entity, payload.Query);
    }

    private static GameplayErrorCode ApplyUpdateData(
        MirrorState candidate,
        GameplayUpdateDataPayloadV1 payload)
    {
        if (payload.Player > 1)
        {
            return GameplayErrorCode.InvalidParticipant;
        }

        MirrorZoneV1 zone = ToZone(payload.Location, out bool valid);
        if (!valid || (payload.Location & LocationOverlay) != 0)
        {
            return GameplayErrorCode.InvalidLocation;
        }

        for (int index = 0; index < payload.Queries.Count; index++)
        {
            if (!IsValidFieldSequence(zone, (uint)index))
            {
                return GameplayErrorCode.StateCapacityExceeded;
            }

            MirrorAddress address = new(
                payload.Player,
                zone,
                (uint)index,
                false,
                0);
            ModernQueryV1 query = payload.Queries[index];
            if (query.IsOnFieldSkipped)
            {
                if (candidate.Entities.ContainsKey(address))
                {
                    return GameplayErrorCode.ConflictingSlotOccupancy;
                }

                continue;
            }

            if (!candidate.Entities.TryGetValue(address, out EntityState? entity))
            {
                return GameplayErrorCode.UnknownMirrorReference;
            }

            GameplayErrorCode error = ApplyQuery(candidate, entity, query);
            if (error != GameplayErrorCode.None)
            {
                return error;
            }
        }

        return GameplayErrorCode.None;
    }

    private static GameplayErrorCode ApplyQuery(
        MirrorState candidate,
        EntityState entity,
        ModernQueryV1 query)
    {
        if (query.IsOnFieldSkipped)
        {
            return GameplayErrorCode.None;
        }

        if (!TryBuildQueryContext(
                entity,
                query,
                out QueryContext context,
                out GameplayErrorCode contextError))
        {
            return contextError;
        }

        List<MirrorQueryFieldSnapshotV1> semanticFields = new(
            query.Fields.Count);
        foreach (ModernQueryFieldV1 field in query.Fields)
        {
            if (!TryCreateSemanticQueryField(
                    candidate,
                    context,
                    field,
                    out MirrorQueryFieldSnapshotV1? semanticField,
                    out GameplayErrorCode fieldError))
            {
                return fieldError;
            }

            semanticFields.Add(semanticField!);
        }

        if (context.IdentityProvenance == MirrorProvenanceV1.UnknownRedacted)
        {
            entity.CardCode = MirrorValueV1.Unknown<uint>();
            entity.QueryFields.RemoveAll(
                field => !IsAlwaysPublicQueryField(field.Flag));
        }

        foreach (MirrorQueryFieldSnapshotV1 field in semanticFields)
        {
            switch (field.Flag)
            {
                case QueryFlagV1.Code:
                    entity.CardCode = field.Value.IsKnown
                        ? MirrorValueV1.Known(
                            field.Value.UInt32Value,
                            field.Value.Provenance)
                        : MirrorValueV1.Unknown<uint>();
                    break;
                case QueryFlagV1.Position:
                    if (!field.Value.IsKnown ||
                        field.Value.Kind != MirrorQueryValueKindV1.UInt32)
                    {
                        return GameplayErrorCode.MalformedQuery;
                    }

                    entity.Position = MirrorValueV1.Known(
                        field.Value.UInt32Value,
                        field.Value.Provenance);
                    break;
                case QueryFlagV1.Owner:
                    if (!field.Value.IsKnown ||
                        field.Value.Kind != MirrorQueryValueKindV1.UInt8 ||
                        field.Value.UInt8Value > 1)
                    {
                        return field.Value.Kind == MirrorQueryValueKindV1.UInt8
                            ? GameplayErrorCode.InvalidParticipant
                            : GameplayErrorCode.MalformedQuery;
                    }

                    entity.Owner = MirrorValueV1.Known(
                        MapPlayer(entity.Perspective, field.Value.UInt8Value),
                        field.Value.Provenance);
                    break;
            }

            int existing = entity.QueryFields.FindIndex(
                item => item.Flag == field.Flag);
            if (existing >= 0)
            {
                entity.QueryFields[existing] = field;
            }
            else
            {
                entity.QueryFields.Add(field);
            }
        }

        return GameplayErrorCode.None;
    }

    private static bool TryBuildQueryContext(
        EntityState entity,
        ModernQueryV1 query,
        out QueryContext context,
        out GameplayErrorCode error)
    {
        bool hasPosition = entity.Position.IsKnown;
        uint position = entity.Position.Value;
        bool isPublic = false;
        bool isHidden = false;
        foreach (ModernQueryFieldV1 field in query.Fields)
        {
            switch (field.Flag)
            {
                case QueryFlagV1.Position when
                    field.Payload is ModernQueryUInt32PayloadV1 positionPayload:
                    hasPosition = true;
                    position = positionPayload.Value;
                    break;
                case QueryFlagV1.Position:
                    context = default;
                    error = GameplayErrorCode.MalformedQuery;
                    return false;
                case QueryFlagV1.IsPublic when
                    field.Payload is ModernQueryUInt8PayloadV1 publicPayload:
                    isPublic = publicPayload.Value != 0;
                    break;
                case QueryFlagV1.IsPublic:
                    context = default;
                    error = GameplayErrorCode.MalformedQuery;
                    return false;
                case QueryFlagV1.IsHidden when
                    field.Payload is ModernQueryUInt8PayloadV1 hiddenPayload:
                    isHidden = hiddenPayload.Value != 0;
                    break;
                case QueryFlagV1.IsHidden:
                    context = default;
                    error = GameplayErrorCode.MalformedQuery;
                    return false;
            }
        }

        bool faceUp = hasPosition && (position & PositionFaceUp) != 0;
        bool identityPublic = isPublic || faceUp;
        bool self = MapPlayer(entity.Perspective, entity.Address.Controller) ==
                    MirrorParticipantRoleV1.Self;
        MirrorProvenanceV1 identityProvenance = identityPublic
            ? MirrorProvenanceV1.PublicProtocolFact
            : self
                ? MirrorProvenanceV1.PerspectivePrivateFact
                : MirrorProvenanceV1.UnknownRedacted;
        context = new QueryContext(
            identityProvenance,
            faceUp,
            isPublic,
            isHidden);
        error = GameplayErrorCode.None;
        return true;
    }

    private static bool TryCreateSemanticQueryField(
        MirrorState candidate,
        QueryContext context,
        ModernQueryFieldV1 field,
        out MirrorQueryFieldSnapshotV1? semanticField,
        out GameplayErrorCode error)
    {
        semanticField = null;
        error = GameplayErrorCode.None;
        MirrorProvenanceV1 provenance = QueryProvenance(field.Flag, context);
        switch (field.Payload)
        {
            case ModernQueryUInt8PayloadV1 value:
                if (field.Flag == QueryFlagV1.Owner && value.Value > 1)
                {
                    error = GameplayErrorCode.InvalidParticipant;
                    return false;
                }

                semanticField = new MirrorQueryFieldSnapshotV1(
                    field.Flag,
                    MirrorQueryValueV1.UInt8(value.Value, provenance));
                return true;
            case ModernQueryUInt32PayloadV1 value:
                semanticField = new MirrorQueryFieldSnapshotV1(
                    field.Flag,
                    provenance == MirrorProvenanceV1.UnknownRedacted
                        ? MirrorQueryValueV1.Unknown()
                        : MirrorQueryValueV1.UInt32(value.Value, provenance));
                return true;
            case ModernQueryInt32PayloadV1 value:
                semanticField = new MirrorQueryFieldSnapshotV1(
                    field.Flag,
                    provenance == MirrorProvenanceV1.UnknownRedacted
                        ? MirrorQueryValueV1.Unknown()
                        : MirrorQueryValueV1.Int32(value.Value, provenance));
                return true;
            case ModernQueryUInt64PayloadV1 value:
                semanticField = new MirrorQueryFieldSnapshotV1(
                    field.Flag,
                    provenance == MirrorProvenanceV1.UnknownRedacted
                        ? MirrorQueryValueV1.Unknown()
                        : MirrorQueryValueV1.UInt64(value.Value, provenance));
                return true;
            case ModernQueryLinkPayloadV1 value:
                semanticField = new MirrorQueryFieldSnapshotV1(
                    field.Flag,
                    provenance == MirrorProvenanceV1.UnknownRedacted
                        ? MirrorQueryValueV1.Unknown()
                        : MirrorQueryValueV1.UInt32Pair(
                            value.Link,
                            value.LinkMarker,
                            provenance));
                return true;
            case ModernQueryUInt32VectorPayloadV1 value:
                semanticField = new MirrorQueryFieldSnapshotV1(
                    field.Flag,
                    provenance == MirrorProvenanceV1.UnknownRedacted
                        ? MirrorQueryValueV1.Unknown()
                        : MirrorQueryValueV1.UInt32Vector(
                            value.Values,
                            provenance,
                            packed: false));
                return true;
            case ModernQueryPackedUInt32VectorPayloadV1 value:
                semanticField = new MirrorQueryFieldSnapshotV1(
                    field.Flag,
                    provenance == MirrorProvenanceV1.UnknownRedacted
                        ? MirrorQueryValueV1.Unknown()
                        : MirrorQueryValueV1.UInt32Vector(
                            value.Values,
                            provenance,
                            packed: true));
                return true;
            case ModernQueryLocInfoPayloadV1 value:
                if (!TryResolveEntity(
                        candidate,
                        value.Value,
                        out MirrorEntityIdV1 entityId,
                        out error))
                {
                    return false;
                }

                semanticField = new MirrorQueryFieldSnapshotV1(
                    field.Flag,
                    MirrorQueryValueV1.EntityReference(entityId));
                return true;
            case ModernQueryLocInfoVectorPayloadV1 value:
                List<MirrorEntityIdV1> references = new(value.Values.Count);
                foreach (ModernLocInfoV1 locInfo in value.Values)
                {
                    if (!TryResolveEntity(
                            candidate,
                            locInfo,
                            out MirrorEntityIdV1 resolvedEntityId,
                            out error))
                    {
                        return false;
                    }

                    references.Add(resolvedEntityId);
                }

                semanticField = new MirrorQueryFieldSnapshotV1(
                    field.Flag,
                    MirrorQueryValueV1.EntityReferenceVector(references));
                return true;
            default:
                error = GameplayErrorCode.MalformedQuery;
                return false;
        }
    }

    private static bool TryResolveEntity(
        MirrorState candidate,
        ModernLocInfoV1 locInfo,
        out MirrorEntityIdV1 entityId,
        out GameplayErrorCode error)
    {
        entityId = default;
        if (!TryNormalizeAddress(locInfo, out MirrorAddress address, out error))
        {
            return false;
        }

        if (!candidate.Entities.TryGetValue(address, out EntityState? entity))
        {
            error = GameplayErrorCode.UnknownMirrorReference;
            return false;
        }

        entityId = entity.Id;
        error = GameplayErrorCode.None;
        return true;
    }

    private static MirrorProvenanceV1 QueryProvenance(
        QueryFlagV1 flag,
        QueryContext context) =>
        flag is QueryFlagV1.Position or
            QueryFlagV1.Owner or
            QueryFlagV1.IsPublic or
            QueryFlagV1.IsHidden
            ? MirrorProvenanceV1.PublicProtocolFact
            : context.IdentityProvenance;

    private static bool IsAlwaysPublicQueryField(QueryFlagV1 flag) =>
        flag is QueryFlagV1.Position or
            QueryFlagV1.Owner or
            QueryFlagV1.IsPublic or
            QueryFlagV1.IsHidden;

    private readonly record struct QueryContext(
        MirrorProvenanceV1 IdentityProvenance,
        bool FaceUp,
        bool IsPublic,
        bool IsHidden);

    private static GameplayErrorCode ApplyDraw(
        MirrorState candidate,
        GameplayDrawPayloadV1 payload)
    {
        if (payload.Player > 1 || payload.Cards.Count == 0)
        {
            return payload.Player > 1
                ? GameplayErrorCode.InvalidParticipant
                : GameplayErrorCode.InvalidDrawCount;
        }

        uint count = (uint)payload.Cards.Count;
        uint mainCount = candidate.ZoneCounts[payload.Player, (int)MirrorZoneV1.MainDeck];
        if (mainCount < count)
        {
            return GameplayErrorCode.StateCapacityExceeded;
        }

        if (candidate.Entities.Values.Any(
                entity => !entity.Address.IsOverlay &&
                          entity.Address.Controller == payload.Player &&
                          entity.Address.Zone == MirrorZoneV1.MainDeck))
        {
            // MSG_DRAW identifies the revealed records but not which
            // previously represented hidden deck entities were removed. Do
            // not guess that continuity; the later knowledge slice owns it.
            return GameplayErrorCode.UnknownMirrorReference;
        }

        uint handCount = candidate.ZoneCounts[payload.Player, (int)MirrorZoneV1.Hand];
        try
        {
            candidate.ZoneCounts[payload.Player, (int)MirrorZoneV1.MainDeck] =
                checked(mainCount - count);
            candidate.ZoneCounts[payload.Player, (int)MirrorZoneV1.Hand] =
                checked(handCount + count);
        }
        catch (OverflowException)
        {
            return GameplayErrorCode.ArithmeticFailure;
        }

        for (int index = 0; index < payload.Cards.Count; index++)
        {
            uint sequence = checked(handCount + (uint)index);
            MirrorAddress address = new(
                payload.Player,
                MirrorZoneV1.Hand,
                sequence,
                false,
                0);
            if (candidate.Entities.ContainsKey(address))
            {
                return GameplayErrorCode.ConflictingSlotOccupancy;
            }

            GameplayDrawCardRecordV1 card = payload.Cards[index];
            if (!TryCreateEntity(
                    candidate,
                    address,
                    card.CardCode,
                    card.Position,
                    card.CardCode != 0,
                    out EntityState? entity,
                    out GameplayErrorCode createError))
            {
                return createError;
            }

            candidate.Entities.Add(address, entity!);
        }

        return GameplayErrorCode.None;
    }

    private static GameplayErrorCode ApplyLifePoints(
        MirrorState candidate,
        GameplayLifePointPayloadV1 payload,
        GameplayMessageKindV1 kind)
    {
        if (payload.Player > 1)
        {
            return GameplayErrorCode.InvalidParticipant;
        }

        if (payload.Amount > MaximumAuthoritativeLifePoints)
        {
            return GameplayErrorCode.ArithmeticFailure;
        }

        uint current = candidate.LifePoints[payload.Player];
        try
        {
            candidate.LifePoints[payload.Player] = kind switch
            {
                GameplayMessageKindV1.Damage or GameplayMessageKindV1.PayLpCost =>
                    payload.Amount > current ? 0 : current - payload.Amount,
                GameplayMessageKindV1.Recover => checked(current + payload.Amount),
                GameplayMessageKindV1.LpUpdate => payload.Amount,
                _ => current
            };
        }
        catch (OverflowException)
        {
            return GameplayErrorCode.ArithmeticFailure;
        }

        return GameplayErrorCode.None;
    }

    private static GameplayErrorCode ApplyChaining(
        MirrorState candidate,
        GameplayChainingPayloadV1 payload)
    {
        if (payload.ChainSize == 0 || candidate.PendingChain is not null)
        {
            return GameplayErrorCode.InvalidChainState;
        }

        try
        {
            if (payload.ChainSize != checked((uint)candidate.Chains.Count + 1))
            {
                return GameplayErrorCode.InvalidChainState;
            }
        }
        catch (OverflowException)
        {
            return GameplayErrorCode.ArithmeticFailure;
        }

        if (!TryNormalizeAddress(payload.Location, out MirrorAddress address, out GameplayErrorCode error) ||
            !candidate.Entities.TryGetValue(address, out EntityState? entity))
        {
            return error == GameplayErrorCode.None
                ? GameplayErrorCode.UnknownMirrorReference
                : error;
        }

        if (payload.CardCode != 0)
        {
            entity.CardCode = MirrorValueV1.Known(
                payload.CardCode,
                MirrorProvenanceV1.PublicProtocolFact);
        }

        candidate.PendingChain = new ChainState(
            payload.ChainSize,
            entity.Id,
            payload.CardCode == 0
                ? MirrorValueV1.Unknown<uint>()
                : MirrorValueV1.Known(
                    payload.CardCode,
                    MirrorProvenanceV1.PublicProtocolFact),
            payload.Description);
        return GameplayErrorCode.None;
    }

    private static GameplayErrorCode ApplyChainSize(
        MirrorState candidate,
        GameplayChainSizePayloadV1 payload,
        GameplayMessageKindV1 kind)
    {
        if (payload.ChainSize == 0)
        {
            return GameplayErrorCode.InvalidChainState;
        }

        if (kind == GameplayMessageKindV1.Chained)
        {
            if (candidate.PendingChain is null ||
                candidate.PendingChain.ChainSize != payload.ChainSize)
            {
                return GameplayErrorCode.InvalidChainState;
            }

            candidate.PendingChain.Status = MirrorChainStatusV1.Chained;
            candidate.Chains.Add(candidate.PendingChain);
            candidate.PendingChain = null;
            return GameplayErrorCode.None;
        }

        ChainState? chain = candidate.Chains.Find(
            item => item.ChainSize == payload.ChainSize);
        if (chain is null)
        {
            return GameplayErrorCode.InvalidChainState;
        }

        switch (kind)
        {
            case GameplayMessageKindV1.ChainSolving when
                chain.Status == MirrorChainStatusV1.Chained:
                chain.Status = MirrorChainStatusV1.Solving;
                return GameplayErrorCode.None;
            case GameplayMessageKindV1.ChainSolved when
                chain.Status is MirrorChainStatusV1.Solving or
                    MirrorChainStatusV1.Disabled:
                chain.Status = MirrorChainStatusV1.Solved;
                return GameplayErrorCode.None;
            case GameplayMessageKindV1.ChainNegated when
                chain.Status is MirrorChainStatusV1.Chained or
                    MirrorChainStatusV1.Solving:
                chain.Status = MirrorChainStatusV1.Negated;
                return GameplayErrorCode.None;
            case GameplayMessageKindV1.ChainDisabled when
                chain.Status is MirrorChainStatusV1.Chained or
                    MirrorChainStatusV1.Solving:
                chain.Status = MirrorChainStatusV1.Disabled;
                return GameplayErrorCode.None;
            default:
                return GameplayErrorCode.InvalidChainState;
        }
    }

    private static GameplayErrorCode ApplyChainEnd(MirrorState candidate)
    {
        if (candidate.PendingChain is null && candidate.Chains.Count == 0)
        {
            return GameplayErrorCode.InvalidChainState;
        }

        candidate.PendingChain = null;
        candidate.Chains.Clear();
        candidate.ChainTargetRelations.Clear();
        return GameplayErrorCode.None;
    }

    private static GameplayErrorCode ApplyBecomeTarget(
        MirrorState candidate,
        GameplayBecomeTargetPayloadV1 payload)
    {
        ChainState? chain = candidate.PendingChain ?? candidate.Chains.LastOrDefault();
        if (chain is null)
        {
            return GameplayErrorCode.InvalidChainState;
        }

        HashSet<MirrorEntityIdV1> seen = new();
        foreach (ModernLocInfoV1 target in payload.Targets)
        {
            if (!TryNormalizeAddress(target, out MirrorAddress address, out GameplayErrorCode error) ||
                !candidate.Entities.TryGetValue(address, out EntityState? entity))
            {
                return error == GameplayErrorCode.None
                    ? GameplayErrorCode.UnknownMirrorReference
                    : error;
            }

            if (!seen.Add(entity.Id) || chain.Targets.Contains(entity.Id))
            {
                return GameplayErrorCode.InvalidRelation;
            }

            if (entity.Id == chain.Card)
            {
                return GameplayErrorCode.InvalidRelation;
            }

            chain.Targets.Add(entity.Id);
            if (!TryCreateRelation(
                    candidate,
                    chain.Card,
                    entity.Id,
                    out RelationState? relation,
                    out GameplayErrorCode relationError))
            {
                return relationError;
            }

            candidate.ChainTargetRelations.Add(relation!);
        }

        return GameplayErrorCode.None;
    }

    private static GameplayErrorCode ApplyCardTarget(
        MirrorState candidate,
        GameplayCardTargetPayloadV1 payload,
        bool cancel)
    {
        if (!TryNormalizeAddress(
                payload.Source,
                out MirrorAddress source,
                out GameplayErrorCode sourceError))
        {
            return sourceError;
        }

        if (!TryNormalizeAddress(
                payload.Target,
                out MirrorAddress target,
                out GameplayErrorCode targetError))
        {
            return targetError;
        }

        if (!candidate.Entities.TryGetValue(source, out EntityState? sourceEntity) ||
            !candidate.Entities.TryGetValue(target, out EntityState? targetEntity))
        {
            return GameplayErrorCode.UnknownMirrorReference;
        }

        RelationState? relation = candidate.TargetRelations.Find(
            item => item.Source == sourceEntity.Id && item.Target == targetEntity.Id);
        if (cancel)
        {
            if (relation is null)
            {
                return GameplayErrorCode.InvalidRelation;
            }

            candidate.TargetRelations.Remove(relation);
            return GameplayErrorCode.None;
        }

        if (relation is not null || sourceEntity.Id == targetEntity.Id)
        {
            return GameplayErrorCode.InvalidRelation;
        }

        if (!TryCreateRelation(
                candidate,
                sourceEntity.Id,
                targetEntity.Id,
                out RelationState? newRelation,
                out GameplayErrorCode relationError))
        {
            return relationError;
        }

        candidate.TargetRelations.Add(newRelation!);
        return GameplayErrorCode.None;
    }

    private static GameplayErrorCode ApplyEquip(
        MirrorState candidate,
        GameplayEquipPayloadV1 payload)
    {
        if (!TryNormalizeAddress(
                payload.Card,
                out MirrorAddress card,
                out GameplayErrorCode cardError))
        {
            return cardError;
        }

        if (!TryNormalizeAddress(
                payload.Target,
                out MirrorAddress target,
                out GameplayErrorCode targetError))
        {
            return targetError;
        }

        if (!candidate.Entities.TryGetValue(card, out EntityState? cardEntity) ||
            !candidate.Entities.TryGetValue(target, out EntityState? targetEntity))
        {
            return GameplayErrorCode.UnknownMirrorReference;
        }

        if (cardEntity.Id == targetEntity.Id)
        {
            return GameplayErrorCode.InvalidRelation;
        }

        RelationState? existing = candidate.EquipmentRelations.Find(
            relation => relation.Source == cardEntity.Id);
        if (existing is not null)
        {
            if (existing.Target == targetEntity.Id)
            {
                return GameplayErrorCode.InvalidRelation;
            }

            // The pinned client removes an existing equip relation before
            // applying a retargeting MSG_EQUIP.
            candidate.EquipmentRelations.Remove(existing);
        }

        if (!TryCreateRelation(
                candidate,
                cardEntity.Id,
                targetEntity.Id,
                out RelationState? relation,
                out GameplayErrorCode relationError))
        {
            return relationError;
        }

        candidate.EquipmentRelations.Add(relation!);
        return GameplayErrorCode.None;
    }

    private static GameplayErrorCode ApplyUnequip(
        MirrorState candidate,
        GameplayUnequipPayloadV1 payload)
    {
        if (!TryNormalizeAddress(payload.Card, out MirrorAddress card, out GameplayErrorCode error))
        {
            return error;
        }

        if (!candidate.Entities.TryGetValue(card, out EntityState? entity))
        {
            return GameplayErrorCode.UnknownMirrorReference;
        }

        RelationState? relation = candidate.EquipmentRelations.Find(
            item => item.Source == entity.Id);
        if (relation is null)
        {
            return GameplayErrorCode.InvalidRelation;
        }

        candidate.EquipmentRelations.Remove(relation);
        return GameplayErrorCode.None;
    }

    private static bool TryCreateEntity(
        MirrorState candidate,
        MirrorAddress address,
        uint cardCode,
        uint position,
        bool hasCardCode,
        out EntityState? entity,
        out GameplayErrorCode error)
    {
        entity = null;
        error = GameplayErrorCode.None;
        ulong ordinal = candidate.NextEntityOrdinal;
        if (ordinal == 0 || ordinal == ulong.MaxValue)
        {
            error = GameplayErrorCode.ArithmeticFailure;
            return false;
        }

        candidate.NextEntityOrdinal = ordinal + 1;
        entity = new EntityState(
            new MirrorEntityIdV1(ordinal),
            address,
            hasCardCode
                ? CreateCardCodeValue(candidate, address, cardCode, position)
                : MirrorValueV1.Unknown<uint>(),
            address.IsOverlay
                ? MirrorValueV1.Unknown<uint>()
                : MirrorValueV1.Known(position),
            MirrorValueV1.Unknown<MirrorParticipantRoleV1>(),
            candidate.Perspective);
        return true;
    }

    private static void ApplyCardCodeObservation(
        MirrorState candidate,
        EntityState entity,
        MirrorAddress address,
        uint cardCode,
        uint position)
    {
        MirrorValueV1<uint> value = CreateCardCodeValue(
            candidate,
            address,
            cardCode,
            position);
        entity.CardCode = value;
        if (!value.IsKnown)
        {
            entity.QueryFields.RemoveAll(
                field => !IsAlwaysPublicQueryField(field.Flag));
        }
    }

    private static MirrorValueV1<uint> CreateCardCodeValue(
        MirrorState candidate,
        MirrorAddress address,
        uint cardCode,
        uint position)
    {
        if (cardCode == 0)
        {
            return MirrorValueV1.Unknown<uint>();
        }

        if (!address.IsOverlay && (position & PositionFaceUp) != 0)
        {
            return MirrorValueV1.Known(
                cardCode,
                MirrorProvenanceV1.PublicProtocolFact);
        }

        return MapPlayer(candidate.Perspective, address.Controller) ==
               MirrorParticipantRoleV1.Self
            ? MirrorValueV1.Known(
                cardCode,
                MirrorProvenanceV1.PerspectivePrivateFact)
            : MirrorValueV1.Unknown<uint>();
    }

    private static bool CanRemoveEntity(MirrorState candidate, EntityState entity)
    {
        if (!entity.Address.IsOverlay && candidate.Entities.Values.Any(
                value => value.Address.IsOverlay &&
                         value.Address.Controller == entity.Address.Controller &&
                         value.Address.Zone == entity.Address.Zone &&
                         value.Address.Sequence == entity.Address.Sequence))
        {
            return false;
        }

        return true;
    }

    private static void RemoveEntityRelations(
        MirrorState candidate,
        MirrorEntityIdV1 id)
    {
        candidate.TargetRelations.RemoveAll(
            relation => relation.Source == id || relation.Target == id);
        candidate.EquipmentRelations.RemoveAll(
            relation => relation.Source == id || relation.Target == id);
        candidate.OverlayRelations.RemoveAll(
            relation => relation.Source == id || relation.Target == id);
        candidate.ChainTargetRelations.RemoveAll(
            relation => relation.Source == id || relation.Target == id);
        foreach (ChainState chain in candidate.Chains)
        {
            chain.Targets.Remove(id);
        }

        candidate.PendingChain?.Targets.Remove(id);
    }

    private static bool AdjustZoneCounts(
        MirrorState candidate,
        MirrorAddress? previous,
        MirrorAddress? current,
        out GameplayErrorCode error)
    {
        error = GameplayErrorCode.None;
        try
        {
            if (previous is MirrorAddress oldAddress && !oldAddress.IsOverlay)
            {
                int player = oldAddress.Controller;
                int zone = (int)oldAddress.Zone;
                if (candidate.ZoneCounts[player, zone] == 0)
                {
                    error = GameplayErrorCode.StateCapacityExceeded;
                    return false;
                }

                candidate.ZoneCounts[player, zone]--;
            }

            if (current is MirrorAddress newAddress && !newAddress.IsOverlay)
            {
                candidate.ZoneCounts[newAddress.Controller, (int)newAddress.Zone] =
                    checked(candidate.ZoneCounts[newAddress.Controller, (int)newAddress.Zone] + 1);
            }
        }
        catch (OverflowException)
        {
            error = GameplayErrorCode.ArithmeticFailure;
            return false;
        }

        return true;
    }

    private static GameplayErrorCode UpdateOverlayRelation(
        MirrorState candidate,
        MirrorEntityIdV1 entityId,
        MirrorAddress address)
    {
        candidate.OverlayRelations.RemoveAll(
            relation => relation.Target == entityId);
        if (!address.IsOverlay)
        {
            return GameplayErrorCode.None;
        }

        MirrorAddress parent = new(
            address.Controller,
            address.Zone,
            address.Sequence,
            false,
            0);
        if (!candidate.Entities.TryGetValue(parent, out EntityState? parentEntity))
        {
            return GameplayErrorCode.UnknownMirrorReference;
        }

        if (!TryCreateRelation(
                candidate,
                parentEntity.Id,
                entityId,
                out RelationState? relation,
                out GameplayErrorCode error))
        {
            return error;
        }

        candidate.OverlayRelations.Add(relation!);
        return GameplayErrorCode.None;
    }

    private static bool TryCreateRelation(
        MirrorState candidate,
        MirrorEntityIdV1 source,
        MirrorEntityIdV1 target,
        out RelationState? relation,
        out GameplayErrorCode error)
    {
        relation = null;
        error = GameplayErrorCode.None;
        ulong ordinal = candidate.NextRelationOrdinal;
        if (ordinal == 0 || ordinal == ulong.MaxValue)
        {
            error = GameplayErrorCode.ArithmeticFailure;
            return false;
        }

        candidate.NextRelationOrdinal = ordinal + 1;
        relation = new RelationState(ordinal, source, target);
        return true;
    }

    private static bool ValidateOverlayParent(
        MirrorState candidate,
        MirrorAddress address)
    {
        if (!address.IsOverlay)
        {
            return true;
        }

        MirrorAddress parent = new(
            address.Controller,
            address.Zone,
            address.Sequence,
            false,
            0);
        if (!candidate.Entities.ContainsKey(parent))
        {
            return false;
        }

        uint siblingCount = 0;
        foreach (EntityState entity in candidate.Entities.Values)
        {
            if (entity.Address.IsOverlay &&
                entity.Address.Controller == address.Controller &&
                entity.Address.Zone == address.Zone &&
                entity.Address.Sequence == address.Sequence)
            {
                siblingCount++;
            }
        }

        return address.OverlayIndex <= siblingCount &&
               !candidate.Entities.Keys.Any(key => key.Equals(address));
    }

    private static bool TryNormalizeAddress(
        ModernLocInfoV1 value,
        out MirrorAddress address,
        out GameplayErrorCode error)
    {
        address = default;
        error = GameplayErrorCode.None;
        if (value.Controller > 1)
        {
            error = GameplayErrorCode.InvalidParticipant;
            return false;
        }

        MirrorZoneV1 zone = ToZone(value.Location, out bool valid);
        if (!valid)
        {
            error = GameplayErrorCode.InvalidLocation;
            return false;
        }

        bool isOverlay = (value.Location & LocationOverlay) != 0;
        if (isOverlay && zone != MirrorZoneV1.MonsterZone)
        {
            error = GameplayErrorCode.InvalidLocation;
            return false;
        }

        if (!isOverlay &&
            !IsValidFieldSequence(zone, value.Sequence))
        {
            error = GameplayErrorCode.StateCapacityExceeded;
            return false;
        }

        address = new(
            value.Controller,
            zone,
            value.Sequence,
            isOverlay,
            isOverlay ? value.Position : 0);
        return true;
    }

    private static bool IsValidFieldSequence(
        MirrorZoneV1 zone,
        uint sequence) =>
        zone switch
        {
            MirrorZoneV1.MonsterZone => sequence <= MaximumMonsterSequence,
            MirrorZoneV1.SpellTrapZone => sequence <= MaximumSpellTrapSequence,
            _ => true
        };

    private static MirrorZoneV1 ToZone(byte location, out bool valid)
    {
        byte baseLocation = (byte)(location & 0x7f);
        (MirrorZoneV1 zone, bool isValid) = baseLocation switch
        {
            LocationDeck => (MirrorZoneV1.MainDeck, true),
            LocationExtra => (MirrorZoneV1.ExtraDeck, true),
            LocationHand => (MirrorZoneV1.Hand, true),
            LocationMonster => (MirrorZoneV1.MonsterZone, true),
            LocationSpellTrap => (MirrorZoneV1.SpellTrapZone, true),
            LocationGraveyard => (MirrorZoneV1.Graveyard, true),
            LocationBanished => (MirrorZoneV1.Banished, true),
            _ => (default, false)
        };
        valid = isValid;
        return zone;
    }

    private static MirrorParticipantRoleV1 MapPlayer(
        GameplayPerspectiveV1 perspective,
        byte canonicalPlayer) =>
        perspective.PlayerType == canonicalPlayer
            ? MirrorParticipantRoleV1.Self
            : MirrorParticipantRoleV1.Opponent;

    private MirrorSnapshotV1 CreateSnapshot(MirrorState current)
    {
        MirrorParticipantSnapshotV1[] participants = new MirrorParticipantSnapshotV1[2];
        MirrorParticipantRoleV1[] roles =
        {
            MirrorParticipantRoleV1.Self,
            MirrorParticipantRoleV1.Opponent
        };
        for (int index = 0; index < roles.Length; index++)
        {
            MirrorParticipantRoleV1 role = roles[index];
            byte canonicalPlayer = CanonicalPlayer(current.Perspective, role);
            List<MirrorZoneSnapshotV1> zones = new();
            foreach (MirrorZoneV1 zone in Enum.GetValues<MirrorZoneV1>())
            {
                MirrorCardSnapshotV1[] cards = current.Entities.Values
                    .Where(entity => entity.Address.Controller == canonicalPlayer &&
                                     entity.Address.Zone == zone)
                    .OrderBy(entity => entity.Address.Sequence)
                    .ThenBy(entity => entity.Address.IsOverlay ? 1 : 0)
                    .ThenBy(entity => entity.Address.OverlayIndex)
                    .ThenBy(entity => entity.Id.Ordinal)
                    .Select(ToCardSnapshot)
                    .ToArray();
                zones.Add(new MirrorZoneSnapshotV1(
                    zone,
                    MirrorValueV1.Known(
                        current.ZoneCounts[canonicalPlayer, (int)zone]),
                    cards));
            }

            participants[index] = new MirrorParticipantSnapshotV1(
                role,
                MirrorValueV1.Known(current.LifePoints[canonicalPlayer]),
                zones);
        }

        MirrorCardSnapshotV1[] allCards = current.Entities.Values
            .OrderBy(entity => entity.Id.Ordinal)
            .Select(ToCardSnapshot)
            .ToArray();
        MirrorChainSnapshotV1[] chains = current.Chains
            .Select(chain => new MirrorChainSnapshotV1(
                chain.ChainSize,
                chain.Card,
                chain.CardCode,
                chain.Description,
                chain.Status,
                chain.Targets))
            .ToArray();
        MirrorValueV1<MirrorEntityIdV1> pending = current.PendingChain is null
            ? MirrorValueV1.Unknown<MirrorEntityIdV1>()
            : MirrorValueV1.Known(
                current.PendingChain.Card,
                MirrorProvenanceV1.DerivedFromProvenPublicFacts);

        return new MirrorSnapshotV1(
            current.Perspective,
            participants,
            allCards,
            current.TurnCount,
            current.TurnPlayer,
            current.Phase,
            current.Terminal,
            pending,
            chains,
            current.TargetRelations.Select(ToRelationSnapshot),
            current.ChainTargetRelations.Select(ToRelationSnapshot),
            current.EquipmentRelations.Select(ToRelationSnapshot),
            current.OverlayRelations.Select(ToRelationSnapshot));

        MirrorCardSnapshotV1 ToCardSnapshot(EntityState entity) =>
            new(
                entity.Id,
                MapPlayer(current.Perspective, entity.Address.Controller),
                entity.Owner,
                entity.Address.Zone,
                entity.Address.Sequence,
                entity.Address.IsOverlay,
                entity.Address.OverlayIndex,
                entity.Position,
                entity.CardCode,
                entity.QueryFields);

        static MirrorRelationSnapshotV1 ToRelationSnapshot(RelationState relation) =>
            new(relation.Source, relation.Target, relation.Ordinal);
    }

    private static byte CanonicalPlayer(
        GameplayPerspectiveV1 perspective,
        MirrorParticipantRoleV1 role) =>
        role == MirrorParticipantRoleV1.Self
            ? perspective.PlayerType
            : (byte)(1 - perspective.PlayerType);

    private readonly record struct MirrorAddress(
        byte Controller,
        MirrorZoneV1 Zone,
        uint Sequence,
        bool IsOverlay,
        uint OverlayIndex);

    private sealed class EntityState
    {
        internal EntityState(
            MirrorEntityIdV1 id,
            MirrorAddress address,
            MirrorValueV1<uint> cardCode,
            MirrorValueV1<uint> position,
            MirrorValueV1<MirrorParticipantRoleV1> owner,
            GameplayPerspectiveV1 perspective)
        {
            Id = id;
            Address = address;
            CardCode = cardCode;
            Position = position;
            Owner = owner;
            Perspective = perspective;
        }

        internal MirrorEntityIdV1 Id { get; }

        internal MirrorAddress Address { get; set; }

        internal MirrorValueV1<uint> CardCode { get; set; }

        internal MirrorValueV1<uint> Position { get; set; }

        internal MirrorValueV1<MirrorParticipantRoleV1> Owner { get; set; }

        internal GameplayPerspectiveV1 Perspective { get; }

        internal List<MirrorQueryFieldSnapshotV1> QueryFields { get; } = new();

        internal EntityState Clone()
        {
            EntityState clone = new(Id, Address, CardCode, Position, Owner, Perspective);
            clone.QueryFields.AddRange(QueryFields);
            return clone;
        }
    }

    private sealed class ChainState
    {
        internal ChainState(
            uint chainSize,
            MirrorEntityIdV1 card,
            MirrorValueV1<uint> cardCode,
            ulong description)
        {
            ChainSize = chainSize;
            Card = card;
            CardCode = cardCode;
            Description = description;
        }

        internal uint ChainSize { get; }

        internal MirrorEntityIdV1 Card { get; }

        internal MirrorValueV1<uint> CardCode { get; }

        internal ulong Description { get; }

        internal MirrorChainStatusV1 Status { get; set; }

        internal List<MirrorEntityIdV1> Targets { get; } = new();

        internal ChainState Clone()
        {
            ChainState clone = new(ChainSize, Card, CardCode, Description)
            {
                Status = Status
            };
            clone.Targets.AddRange(Targets);
            return clone;
        }
    }

    private sealed class RelationState
    {
        internal RelationState(
            ulong ordinal,
            MirrorEntityIdV1 source,
            MirrorEntityIdV1 target)
        {
            Ordinal = ordinal;
            Source = source;
            Target = target;
        }

        internal ulong Ordinal { get; }

        internal MirrorEntityIdV1 Source { get; }

        internal MirrorEntityIdV1 Target { get; }

        internal RelationState Clone() => new(Ordinal, Source, Target);
    }

    private sealed class MirrorState
    {
        internal MirrorState(GameplayPerspectiveV1 perspective)
        {
            Perspective = perspective;
        }

        internal GameplayPerspectiveV1 Perspective { get; }

        internal uint[] LifePoints { get; } = new uint[2];

        internal uint[,] ZoneCounts { get; } = new uint[2, 7];

        internal ulong TurnCount { get; set; }

        internal MirrorValueV1<MirrorParticipantRoleV1> TurnPlayer { get; set; } =
            MirrorValueV1.Unknown<MirrorParticipantRoleV1>();

        internal MirrorValueV1<ushort> Phase { get; set; } =
            MirrorValueV1.Unknown<ushort>();

        internal MirrorTerminalSnapshotV1 Terminal { get; set; }

        internal Dictionary<MirrorAddress, EntityState> Entities { get; } = new();

        internal List<ChainState> Chains { get; } = new();

        internal ChainState? PendingChain { get; set; }

        internal List<RelationState> TargetRelations { get; } = new();

        internal List<RelationState> ChainTargetRelations { get; } = new();

        internal List<RelationState> EquipmentRelations { get; } = new();

        internal List<RelationState> OverlayRelations { get; } = new();

        internal ulong NextEntityOrdinal { get; set; } = 1;

        internal ulong NextRelationOrdinal { get; set; } = 1;

        internal MirrorState Clone()
        {
            MirrorState clone = new(Perspective)
            {
                TurnCount = TurnCount,
                TurnPlayer = TurnPlayer,
                Phase = Phase,
                Terminal = Terminal,
                PendingChain = PendingChain?.Clone(),
                NextEntityOrdinal = NextEntityOrdinal,
                NextRelationOrdinal = NextRelationOrdinal
            };
            LifePoints.CopyTo(clone.LifePoints, 0);
            for (int player = 0; player < ZoneCounts.GetLength(0); player++)
            {
                for (int zone = 0; zone < ZoneCounts.GetLength(1); zone++)
                {
                    clone.ZoneCounts[player, zone] = ZoneCounts[player, zone];
                }
            }

            foreach ((MirrorAddress address, EntityState entity) in Entities)
            {
                clone.Entities.Add(address, entity.Clone());
            }

            clone.Chains.AddRange(Chains.Select(chain => chain.Clone()));
            clone.TargetRelations.AddRange(TargetRelations.Select(relation => relation.Clone()));
            clone.ChainTargetRelations.AddRange(ChainTargetRelations.Select(relation => relation.Clone()));
            clone.EquipmentRelations.AddRange(EquipmentRelations.Select(relation => relation.Clone()));
            clone.OverlayRelations.AddRange(OverlayRelations.Select(relation => relation.Clone()));
            return clone;
        }
    }
}
