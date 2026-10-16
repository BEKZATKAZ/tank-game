using System;
using System.Collections.Generic;
using Tanks.Shared.Generation;
using Tanks.Shared.Items.Data;
using Tanks.Utils;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace Tanks.Server.Generation.Crates
{
    public sealed class CrateGenerator : ICrateGenerator
    {
        private const int CrateSize = CrateOccupancy.CrateSize;
        private const int SingleCrateAttempts = 64;

        // The spawner already used the first ItemTier value for regular crates
        private const ItemTier RegularTier = default;

        private static readonly int2[] SideDirections =
        {
            new int2(1, 0), new int2(-1, 0), new int2(0, 1), new int2(0, -1)
        };

        private readonly List<GeneratedCrate> _crates = new List<GeneratedCrate>();
        private readonly List<int2> _centers = new List<int2>();
        private readonly List<int2> _singles = new List<int2>();
        private readonly List<int2> _members = new List<int2>();
        private readonly List<int2> _candidates = new List<int2>();
        private readonly List<float> _weights = new List<float>();

        public TemporaryArrayResult<GeneratedCrate> Generate(
            ReadOnlySpan<GeneratedTile> tiles,
            uint seed,
            CrateGenerationSettings settings)
        {
            _crates.Clear();
            _centers.Clear();
            _singles.Clear();

            if (tiles.IsEmpty) return new TemporaryArrayResult<GeneratedCrate>(0);

            using var occupancy = new CrateOccupancy(tiles);

            // Different stream than the maze generator, otherwise the crate rolls
            // would mirror the rolls that carved the maze
            Random random = Random.CreateFromIndex(seed ^ 0xC4A7E5u);

            BuildClusters(occupancy, settings, ref random);

            // Singles come after the clusters because only then every cluster crate is
            // known and a single can make sure it does not touch one. Epic goes before
            // rare so the rare ones can't take the spots near the center first
            PlaceEpicCrates(occupancy, settings, ref random);
            PlaceRareCrates(occupancy, settings, ref random);

            return CopyResult();
        }

        // Rounds down to an even number so crates stay on the 2x2 grid
        private static int Snap(int value)
        {
            return value & ~1;
        }

        private static int Squared(int value)
        {
            return value * value;
        }

        private static int2 SnapToGrid(int2 tile)
        {
            return new int2(Snap(tile.x), Snap(tile.y));
        }

        private static int GetCountForDensity(CrateOccupancy occupancy, float density)
        {
            if (density <= 0f || occupancy.OpenArea == 0) return 0;

            int count = (int)math.round(occupancy.OpenArea / 1000f * density);

            // Small mazes would otherwise round down to nothing
            // even though crates were requested
            return math.max(1, count);
        }

        private static bool IsFarFrom(List<int2> points, int2 tile, int minDistanceSq)
        {
            for (int i = 0; i < points.Count; i++)
            {
                int2 offset = points[i] - tile;
                if (math.dot(offset, offset) < minDistanceSq) return false;
            }

            return true;
        }

        private void BuildClusters(
            CrateOccupancy occupancy,
            CrateGenerationSettings settings,
            ref Random random)
        {
            int clusterCount = GetCountForDensity(occupancy, settings.ClusterDensity);

            for (int i = 0; i < clusterCount; i++)
            {
                if (TryPickCenter(occupancy, settings, ref random, out int2 center) == false)
                {
                    continue;
                }

                _centers.Add(center);
                BuildCluster(occupancy, settings, center, ref random);
            }
        }

        private bool TryPickCenter(
            CrateOccupancy occupancy,
            CrateGenerationSettings settings,
            ref Random random,
            out int2 center)
        {
            int minDistanceSq = Squared(settings.MinClusterDistance);

            for (int attempt = 0; attempt < 32; attempt++)
            {
                if (occupancy.TryGetRandomOpenTile(ref random, out center) == false)
                {
                    continue;
                }

                if (IsFarFrom(_centers, center, minDistanceSq)) return true;
            }

            center = default;
            return false;
        }

        private void BuildCluster(
            CrateOccupancy occupancy,
            CrateGenerationSettings settings,
            int2 center,
            ref Random random)
        {
            int2 range = settings.MinMaxCratesPerCluster;
            int minCrates = math.max(2, math.min(range.x, range.y));
            int maxCrates = math.max(minCrates, math.max(range.x, range.y));
            int target = random.NextInt(minCrates, maxCrates + 1);

            int radius = math.max(CrateSize, settings.ClusterRadius);
            int2 gridCenter = SnapToGrid(center);

            _members.Clear();
            occupancy.Checkpoint();
            int cratesBefore = _crates.Count;

            while (_members.Count < target)
            {
                CollectCandidates(occupancy, settings, gridCenter, radius);

                int pick = PickWeighted(ref random);
                if (pick < 0) break;

                int2 spot = _candidates[pick];
                occupancy.PlaceCrate(spot);
                _members.Add(spot);
                _crates.Add(new GeneratedCrate(spot, RegularTier));
            }

            // A single crate is not a cluster. Rolling back the grid as well keeps
            // later crates from avoiding crates that are not in the result
            if (_members.Count < 2)
            {
                occupancy.Rollback();
                _crates.RemoveRange(cratesBefore, _crates.Count - cratesBefore);
            }
        }

        private void CollectCandidates(
            CrateOccupancy occupancy,
            CrateGenerationSettings settings,
            int2 gridCenter,
            int radius)
        {
            _candidates.Clear();
            _weights.Clear();

            if (_members.Count == 0)
            {
                CollectFirstCrateCandidates(occupancy, settings, gridCenter, radius);
                return;
            }

            // Only the 4 sides of existing members are used, a gap of even one tile
            // would break the "every crate touches another crate" rule
            for (int m = 0; m < _members.Count; m++)
            {
                for (int d = 0; d < SideDirections.Length; d++)
                {
                    int2 spot = _members[m] + SideDirections[d] * CrateSize;

                    TryAddCandidate(
                        occupancy, settings, spot, gridCenter, radius, requireWall: false);
                }
            }
        }

        private void CollectFirstCrateCandidates(
            CrateOccupancy occupancy,
            CrateGenerationSettings settings,
            int2 gridCenter,
            int radius)
        {
            int gridRadius = radius / CrateSize;

            for (int y = -gridRadius; y <= gridRadius; y++)
            {
                for (int x = -gridRadius; x <= gridRadius; x++)
                {
                    int2 spot = gridCenter + new int2(x, y) * CrateSize;

                    // A cluster has to start at a wall, otherwise it floats
                    // in the middle of nowhere
                    TryAddCandidate(
                        occupancy, settings, spot, gridCenter, radius, requireWall: true);
                }
            }
        }

        private void TryAddCandidate(
            CrateOccupancy occupancy,
            CrateGenerationSettings settings,
            int2 spot,
            int2 gridCenter,
            int radius,
            bool requireWall)
        {
            int2 offset = spot - gridCenter;
            if (math.dot(offset, offset) > radius * radius) return;

            // Two members can share the same free neighbor spot
            if (_candidates.Contains(spot)) return;

            if (occupancy.CanPlace(spot, out int wallSides) == false) return;
            if (requireWall && wallSides == 0) return;

            float bias = math.max(0f, settings.CornerBias);
            float cornerBonus = math.max(0, wallSides - 1) * bias;

            _candidates.Add(spot);
            _weights.Add(1f + cornerBonus);
        }

        // Weighted instead of "best spot wins", so clusters in similar places still differ
        private int PickWeighted(ref Random random)
        {
            if (_candidates.Count == 0) return -1;

            float total = 0f;
            for (int i = 0; i < _weights.Count; i++)
            {
                total += _weights[i];
            }

            float roll = random.NextFloat() * total;

            for (int i = 0; i < _weights.Count; i++)
            {
                roll -= _weights[i];
                if (roll <= 0f) return i;
            }

            // Float rounding can leave a tiny remainder after the last weight
            return _weights.Count - 1;
        }

        private void PlaceEpicCrates(
            CrateOccupancy occupancy,
            CrateGenerationSettings settings,
            ref Random random)
        {
            int radius = math.max(CrateSize, settings.EpicRadius);
            int minDistanceSq = Squared(settings.MinSingleCrateDistance);

            for (int i = 0; i < settings.EpicCrateCount; i++)
            {
                for (int attempt = 0; attempt < SingleCrateAttempts; attempt++)
                {
                    int2 offset = random.NextInt2(new int2(-radius), new int2(radius + 1));

                    // Sampling a square and dropping the corners keeps the spread round
                    if (math.dot(offset, offset) > radius * radius) continue;

                    int2 spot = SnapToGrid(occupancy.Center + offset);
                    if (TryPlaceSingle(occupancy, spot, ItemTier.Epic, minDistanceSq))
                    {
                        break;
                    }
                }
            }
        }

        private void PlaceRareCrates(
            CrateOccupancy occupancy,
            CrateGenerationSettings settings,
            ref Random random)
        {
            int count = GetCountForDensity(occupancy, settings.RareCrateDensity);
            int minDistanceSq = Squared(settings.MinSingleCrateDistance);

            for (int i = 0; i < count; i++)
            {
                for (int attempt = 0; attempt < SingleCrateAttempts; attempt++)
                {
                    if (occupancy.TryGetRandomOpenTile(ref random, out int2 tile) == false)
                    {
                        continue;
                    }

                    int2 spot = SnapToGrid(tile);

                    if (TryPlaceSingle(occupancy, spot, ItemTier.Rare, minDistanceSq))
                    {
                        break;
                    }
                }
            }
        }

        private bool TryPlaceSingle(
            CrateOccupancy occupancy,
            int2 spot,
            ItemTier tier,
            int minDistanceSq)
        {
            if (IsFarFrom(_singles, spot, minDistanceSq) == false) return false;
            if (occupancy.CanPlaceAlone(spot) == false) return false;

            occupancy.PlaceCrate(spot);
            _singles.Add(spot);
            _crates.Add(new GeneratedCrate(spot, tier));
            return true;
        }

        private TemporaryArrayResult<GeneratedCrate> CopyResult()
        {
            var result = new TemporaryArrayResult<GeneratedCrate>(_crates.Count);

            try
            {
                for (int i = 0; i < _crates.Count; i++)
                {
                    result[i] = _crates[i];
                }

                return result;
            }
            catch
            {
                result.Dispose();
                throw;
            }
        }
    }
}
