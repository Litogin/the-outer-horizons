using System.Linq;
using Content.Server.Worldgen.Components.Debris;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.Worldgen.Systems.Debris;

/// <summary>
///     This handles building the floor plans for smooth asteroid debris with adjustable crust layers.
/// </summary>
public sealed class BlobSmoothFloorPlanBuilderSystem : BaseWorldSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinition = default!;
    [Dependency] private readonly TileSystem _tiles = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BlobSmoothFloorPlanBuilderComponent, ComponentStartup>(OnBlobFloorPlanBuilderStartup);
    }

    private void OnBlobFloorPlanBuilderStartup(EntityUid uid, BlobSmoothFloorPlanBuilderComponent component,
        ComponentStartup args)
    {
        PlaceFloorplanTiles(uid, component, Comp<MapGridComponent>(uid));
    }

    private void PlaceFloorplanTiles(EntityUid gridUid, BlobSmoothFloorPlanBuilderComponent comp, MapGridComponent grid)
    {
        var spawnPoints = new List<Vector2i>(comp.FloorPlacements * 2);
        var activeTiles = new HashSet<Vector2i>(comp.FloorPlacements * 2);

        double radsq = Math.Pow(comp.Radius, 2);
        double stretchX = _random.NextFloat(0.65f, 1.0f);
        double stretchY = _random.NextFloat(0.65f, 1.0f);

        void AddSpawnNeighbors(Vector2i point)
        {
            var neighbors = new[]
            {
                point.Offset(Direction.North),
                point.Offset(Direction.South),
                point.Offset(Direction.East),
                point.Offset(Direction.West)
            };

            foreach (var n in neighbors)
            {
                double evalX = n.X * stretchX;
                double evalY = n.Y * stretchY;

                if (!activeTiles.Contains(n) && (evalX * evalX + evalY * evalY) <= radsq && !spawnPoints.Contains(n))
                {
                    spawnPoints.Add(n);
                }
            }
        }

        // Органическое расширение основной массы
        activeTiles.Add(Vector2i.Zero);
        AddSpawnNeighbors(Vector2i.Zero);

        for (var i = 1; i < comp.FloorPlacements; i++)
        {
            if (spawnPoints.Count == 0) break;

            Vector2i bestPoint = Vector2i.Zero;
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
                }
            }

            spawnPoints.Remove(bestPoint);
            activeTiles.Add(bestPoint);
            AddSpawnNeighbors(bestPoint);
        }

        // Клеточный автомат (сглаживание форм)
        for (int step = 0; step < 2; step++)
        {
            var nextStepTiles = new HashSet<Vector2i>(activeTiles);
            var customBoundaries = new HashSet<Vector2i>();

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

        // Разделение тайлов на внутренние и внешние
        var crustTiles = new HashSet<Vector2i>();
        // Копируем оставшееся ядро, которое будем уменьшать с каждым слоем коры
        var coreTilesRemaining = new HashSet<Vector2i>(activeTiles);

        // Количество слоёв корочки (настройте под ваши нужды, например, вынесите в компонент)
        var crustLayersCount = comp.CrustLayers;

        for (int layer = 0; layer < crustLayersCount; layer++)
        {
            var currentLayerCrust = new HashSet<Vector2i>();

            foreach (var tile in coreTilesRemaining)
            {
                // Проверяем 4 ортогональных соседа
                var n = tile.Offset(Direction.North);
                var s = tile.Offset(Direction.South);
                var e = tile.Offset(Direction.East);
                var w = tile.Offset(Direction.West);

                // Если тайл граничит с пустотой ИЛИ с внешним пространством, не занятым ядром
                if (!coreTilesRemaining.Contains(n) ||
                    !coreTilesRemaining.Contains(s) ||
                    !coreTilesRemaining.Contains(e) ||
                    !coreTilesRemaining.Contains(w))
                {
                    currentLayerCrust.Add(tile);
                }
            }

            // Защита: не даем корочке поглотить абсолютно весь астероид, если он слишком мал
            if (coreTilesRemaining.Count - currentLayerCrust.Count < 3)
                break;

            // Убираем найденную кору из ядра и переносим в общий пул корочки
            foreach (var crustTile in currentLayerCrust)
            {
                coreTilesRemaining.Remove(crustTile);
                crustTiles.Add(crustTile);
            }
        }

        // Финальная отрисовка
        var taken = new Dictionary<Vector2i, Tile>(activeTiles.Count);

        // Переменные для хранения определений тайлов (замените CrustTileset на ваш реальный прототип, если нужно)
        var mainTileset = comp.FloorTileset;
        // Если в компоненте еще нет поля CrustTileset, временно берем тот же или другой доступный ID:
        var crustTileset = comp.CrustTileset;

        foreach (var point in activeTiles)
        {
            // Определяем, какой тип тайла ставить: корочку или внутреннее ядро
            var chosenTileset = crustTiles.Contains(point) ? crustTileset : mainTileset;

            var tileDef = _tileDefinition[_random.Pick(chosenTileset)];
            taken.Add(point, new Tile(tileDef.TileId, 0, _tiles.PickVariant((ContentTileDefinition)tileDef)));
        }

        _map.SetTiles(gridUid, grid, taken.Select(x => (x.Key, x.Value)).ToList());
    }
}
