using System.Numerics;
using Content.Shared.NPC.Components;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._NF.Salvage.Expeditions;

/// <summary>
/// Drives <see cref="SalvageObjectiveNpcSpawnerComponent"/> objective structure spawning.
/// </summary>
public sealed class SalvageObjectiveNpcSpawnerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly AnchorableSystem _anchorable = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SalvageObjectiveNpcSpawnerComponent, MapInitEvent>(OnMapInit);
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<SalvageObjectiveNpcSpawnerComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (Paused(uid) || comp.SpawnPrototypes.Count == 0 || comp.NextSpawn > now)
                continue;

            comp.NextSpawn += TimeSpan.FromSeconds(comp.SpawnIntervalSeconds);

            if (CountNearbyFactionMobs(uid, comp) >= comp.MaxNearby)
                continue;

            var spawn = _random.Pick(comp.SpawnPrototypes);

            if (TryGetNearbySpawnCoordinates(uid, comp, out var coords))
                SpawnAtPosition(spawn, coords);
            else
                SpawnAtPosition(spawn, Transform(uid).Coordinates);
        }
    }

    private void OnMapInit(Entity<SalvageObjectiveNpcSpawnerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextSpawn = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.SpawnIntervalSeconds);
    }

    private int CountNearbyFactionMobs(EntityUid uid, SalvageObjectiveNpcSpawnerComponent comp)
    {
        var xform = Transform(uid);
        var mapCoords = _xforms.GetMapCoordinates((uid, xform));
        var count = 0;

        foreach (var nearby in _lookup.GetEntitiesInRange<NpcFactionMemberComponent>(mapCoords, comp.NearbyRange))
        {
            if (nearby.Owner == uid)
                continue;

            if (!comp.NearbyFactions.Overlaps(nearby.Comp.Factions))
                continue;

            count++;
        }

        return count;
    }

    private bool TryGetNearbySpawnCoordinates(EntityUid uid, SalvageObjectiveNpcSpawnerComponent comp, out EntityCoordinates coords)
    {
        var xform = Transform(uid);
        if (xform.GridUid is not { Valid: true } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
        {
            coords = default;
            return false;
        }

        var centerTile = _map.CoordinatesToTile(gridUid, grid, _xforms.GetMapCoordinates((uid, xform)));
        var tileRange = Math.Max(1, (int)MathF.Ceiling(comp.NearbyRange));
        var candidates = new List<Vector2i>();

        for (var x = -tileRange; x <= tileRange; x++)
        {
            for (var y = -tileRange; y <= tileRange; y++)
            {
                if (x == 0 && y == 0)
                    continue;

                var offset = new Vector2(x, y);
                if (offset.Length() > comp.NearbyRange)
                    continue;

                candidates.Add(centerTile + new Vector2i(x, y));
            }
        }

        while (candidates.Count > 0)
        {
            var index = _random.Next(candidates.Count);
            var tile = candidates[index];
            candidates.RemoveAt(index);

            if (!_anchorable.TileFree((gridUid, grid), tile, (int)CollisionGroup.MachineLayer, (int)CollisionGroup.MachineLayer))
                continue;

            coords = _map.GridTileToLocal(gridUid, grid, tile);
            return true;
        }

        coords = default;
        return false;
    }
}
