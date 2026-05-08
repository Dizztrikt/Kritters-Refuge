using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Salvage.Expeditions;

public static class SalvageExpeditionReservation
{
    public static Box2 GetLandingZone(Box2 shuttleBox, Vector2 origin, float padding = 16f)
    {
        return shuttleBox.Translated(origin).Enlarged(padding);
    }

    public static bool IntersectsDungeonBounds(SalvageExpeditionComponent expedition, Box2 area, float dungeonPadding = 0f)
    {
        var dungeon = dungeonPadding > 0f
            ? expedition.DungeonBounds.Enlarged(dungeonPadding)
            : expedition.DungeonBounds;

        return dungeon.Intersects(area);
    }

    public static bool IntersectsReservedLandingZone(SalvageExpeditionComponent expedition, Box2 area)
    {
        foreach (var zone in expedition.ReservedLandingZones)
        {
            if (zone.Intersects(area))
                return true;
        }

        return false;
    }

    public static bool IsReservedTile(SalvageExpeditionComponent expedition, MapGridComponent grid, Vector2i tile)
    {
        var min = new Vector2(tile.X, tile.Y);
        var max = min + new Vector2(grid.TileSize, grid.TileSize);
        var tileBox = new Box2(min, max);

        return IntersectsDungeonBounds(expedition, tileBox) ||
               IntersectsReservedLandingZone(expedition, tileBox);
    }
}