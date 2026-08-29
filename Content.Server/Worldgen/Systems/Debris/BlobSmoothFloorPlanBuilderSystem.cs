using System;
using System.Collections.Generic;
using Content.Server.Worldgen.Components.Debris;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.Worldgen.Systems.Debris;

public sealed class BlobSmoothFloorPlanBuilderSystem : BaseWorldSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinition = default!;
    [Dependency] private readonly TileSystem _tiles = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    // Кэшируем направления для ортогонального обхода, чтобы избежать аллокаций массивов в циклах
    private static readonly Vector2i[] OrthogonalNeighbors =
    {
        new(0, 1),  // North
        new(0, -1), // South
        new(1, 0),  // East
        new(-1, 0)  // West
    };

    public override void Initialize()
    {
        SubscribeLocalEvent<BlobSmoothFloorPlanBuilderComponent, ComponentStartup>(OnBlobFloorPlanBuilderStartup);
    }

    private void OnBlobFloorPlanBuilderStartup(EntityUid uid, BlobSmoothFloorPlanBuilderComponent component, ComponentStartup args)
    {
        PlaceFloorplanTiles(uid, component, Comp<MapGridComponent>(uid));
    }

    private void PlaceFloorplanTiles(EntityUid gridUid, BlobSmoothFloorPlanBuilderComponent comp, MapGridComponent grid)
    {
        var capacity = comp.FloorPlacements * 2;
        var spawnPoints = new List<Vector2i>(capacity);
        var spawnPointsSet = new HashSet<Vector2i>(capacity); // Быстрая O(1) проверка вместо .Contains() на List
        var activeTiles = new HashSet<Vector2i>(capacity);

        double radsq = Math.Pow(comp.Radius, 2);
        double stretchX = _random.NextFloat(0.65f, 1.0f);
        double stretchY = _random.NextFloat(0.65f, 1.0f);

        void AddSpawnNeighbors(Vector2i point)
        {
            foreach (var offset in OrthogonalNeighbors)
            {
                var n = point + offset;
                double evalX = n.X * stretchX;
                double evalY = n.Y * stretchY;

                if (!activeTiles.Contains(n) && (evalX * evalX + evalY * evalY) <= radsq && !spawnPointsSet.Contains(n))
                {
                    spawnPoints.Add(n);
                    spawnPointsSet.Add(n);
                }
            }
        }

        activeTiles.Add(Vector2i.Zero);
        AddSpawnNeighbors(Vector2i.Zero);

        // 1. Органическое расширение
        for (var i = 1; i < comp.FloorPlacements; i++)
        {
            if (spawnPoints.Count == 0) break;

            Vector2i bestPoint = Vector2i.Zero;
            int bestIndex = -1;
            double minScore = double.MaxValue;

            int sampleCount = Math.Min(5, spawnPoints.Count);
            for (int s = 0; s < sampleCount; s++)
            {
                int randomIndex = _random.Next(0, spawnPoints.Count);
                var candidate = spawnPoints[randomIndex];

                double evalX = candidate.X * stretchX;
                double evalY = candidate.Y * stretchY;
                double distSq = evalX * evalX + evalY * evalY;

                double organicNoise = _random.NextDouble() * (radsq * 0.45);
                double score = distSq + organicNoise;

                if (score < minScore)
                {
                    minScore = score;
                    bestPoint = candidate;
                    bestIndex = randomIndex;
                }
            }

            if (bestIndex != -1)
            {
                // Быстрое удаление из List за O(1) вместо O(N) сдвига элементов
                int lastIndex = spawnPoints.Count - 1;
                spawnPoints[bestIndex] = spawnPoints[lastIndex];
                spawnPoints.RemoveAt(lastIndex);

                spawnPointsSet.Remove(bestPoint);
                activeTiles.Add(bestPoint);
                AddSpawnNeighbors(bestPoint);
            }
        }

        // 2. Клеточный автомат (Сглаживание)
        var customBoundaries = new HashSet<Vector2i>(capacity);
        for (int step = 0; step < 2; step++)
        {
            var nextStepTiles = new HashSet<Vector2i>(activeTiles);
            customBoundaries.Clear();

            foreach (var tile in activeTiles)
            {
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        customBoundaries.Add(new Vector2i(tile.X + x, tile.Y + y));
                    }
                }
            }

            foreach (var cell in customBoundaries)
            {
                int neighborCount = 0;
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        if (x == 0 && y == 0) continue;
                        if (activeTiles.Contains(new Vector2i(cell.X + x, cell.Y + y)))
                            neighborCount++;
                    }
                }

                if (activeTiles.Contains(cell))
                {
                    if (neighborCount < 4)
                        nextStepTiles.Remove(cell);
                }
                else
                {
                    double evalX = cell.X * stretchX;
                    double evalY = cell.Y * stretchY;
                    if (neighborCount >= 5 && (evalX * evalX + evalY * evalY) <= radsq)
                        nextStepTiles.Add(cell);
                }
            }

            activeTiles = nextStepTiles;
        }

        // 3. Выделение слоев коры (Crust)
        var crustTiles = new HashSet<Vector2i>(activeTiles.Count / 2);
        var coreTilesRemaining = new HashSet<Vector2i>(activeTiles);
        var currentLayerCrust = new List<Vector2i>(); // Используем List для итерации слоя

        for (int layer = 0; layer < comp.CrustLayers; layer++)
        {
            currentLayerCrust.Clear();

            foreach (var tile in coreTilesRemaining)
            {
                foreach (var offset in OrthogonalNeighbors)
                {
                    if (!coreTilesRemaining.Contains(tile + offset))
                    {
                        currentLayerCrust.Add(tile);
                        break;
                    }
                }
            }

            // Исправленная защита: прерываемся ДО того, как испортим или "потеряем" слой тайлов
            if (coreTilesRemaining.Count - currentLayerCrust.Count < 3)
                break;

            foreach (var crustTile in currentLayerCrust)
            {
                coreTilesRemaining.Remove(crustTile);
                crustTiles.Add(crustTile);
            }
        }

        // 4. Финальная отрисовка структуры
        var tilesToSet = new List<(Vector2i, Tile)>(activeTiles.Count);
        var mainTileset = comp.FloorTileset;
        var crustTileset = comp.CrustTileset;

        foreach (var point in activeTiles)
        {
            var chosenTileset = crustTiles.Contains(point) ? crustTileset : mainTileset;
            var tileDef = _tileDefinition[_random.Pick(chosenTileset)];
            var tile = new Tile(tileDef.TileId, 0, _tiles.PickVariant((ContentTileDefinition)tileDef));

            tilesToSet.Add((point, tile));
        }

        // Избавились от LINQ Select().ToList(), передаем чистый готовый список
        _map.SetTiles(gridUid, grid, tilesToSet);
    }
}
