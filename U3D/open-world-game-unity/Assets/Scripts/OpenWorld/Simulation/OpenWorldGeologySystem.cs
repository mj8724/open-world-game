using System.Collections.Generic;
using UnityEngine;

namespace OpenWorld
{
    public class OpenWorldGeologySystem : MonoBehaviour
    {
        public string StatusSummary { get; private set; } = "Geology idle";

        private OpenWorldState _world;
        private UnitSystem _units;
        private BuildingSystem _buildings;
        private BlueprintSystem _blueprints;
        private float _tickTimer;

        public void Initialize(OpenWorldState world, UnitSystem units, BuildingSystem buildings, BlueprintSystem blueprints)
        {
            _world = world;
            _units = units;
            _buildings = buildings;
            _blueprints = blueprints;
        }

        private void Update()
        {
            if (_world == null) return;
            _tickTimer += Time.deltaTime;
            if (_tickTimer < 0.25f) return;
            float delta = _tickTimer;
            _tickTimer = 0f;
            AssignAndTickJobs(delta);
            RefreshSummary();
        }

        public JobRecord QueueSurvey(Vector2Int center, int radius = 6, int priority = 4)
        {
            var job = _world.AddJob(UnitTask.Surveying, center);
            job.Radius = Mathf.Max(1, radius);
            job.Priority = Mathf.Clamp(priority, 1, 5);
            job.WorkRemaining = 4f;
            return job;
        }

        public JobRecord QueueCoreDrill(Vector2Int cell, int priority = 4)
        {
            var job = _world.AddJob(UnitTask.Drilling, cell, BuildableKind.DrillRig);
            job.Priority = Mathf.Clamp(priority, 1, 5);
            job.WorkRemaining = 7f;
            var rig = FindNearbyBuilding(BuildableKind.DrillRig, cell, 3);
            if (rig == null)
            {
                _blueprints.QueueBuilding(BuildableKind.DrillRig, cell, OpenWorldConstants.PlayerFactionId, priority);
                job.BlockedReason = "Waiting for drill rig construction";
            }
            else job.RelatedEntityId = rig.Id;
            return job;
        }

        public MiningZoneRecord AssignMiningZone(Vector2Int center, int radius, GroundMaterial material, int priority)
        {
            var mine = FindNearbyBuilding(BuildableKind.MinePost, center, 20);
            var zone = _world.AddMiningZone(center, radius, material, mine?.Id ?? 0, priority);
            zone.Status = mine == null ? "No mine post in range" : $"Bound to mine #{mine.Id}";
            return zone;
        }

        public void ReceiveExcavatedMaterial(Vector2Int cell, GroundMaterial material, int amount)
        {
            if (amount <= 0 || material == GroundMaterial.Oil) return;
            var resource = ResourceInventory.MatToResource(material);
            MiningZoneRecord zone = null;
            foreach (var candidate in _world.MiningZones)
            {
                if (!candidate.Active || candidate.TargetMaterial != material) continue;
                if ((candidate.Center - cell).sqrMagnitude > candidate.Radius * candidate.Radius) continue;
                if (zone == null || candidate.Priority > zone.Priority) zone = candidate;
            }

            if (zone != null && _world.Buildings.TryGetValue(zone.MineBuildingId, out var mine))
            {
                int stored = _world.AddToStorage(mine, resource, amount);
                zone.ExtractedAmount += stored;
                zone.Status = stored == amount ? $"Extracting {material}" : "Mine storage full";
                if (stored < amount) _world.Inventory.Add(resource, amount - stored);
            }
            else
            {
                _world.Inventory.Add(resource, amount);
            }

            var survey = _world.UpsertSurvey(cell);
            if (survey.State == SurveyState.Unknown || survey.State == SurveyState.Suspected)
            {
                survey.State = SurveyState.Surveyed;
                survey.Confidence = Mathf.Max(survey.Confidence, 0.72f);
                survey.EstimatedMaterial = material;
                survey.SurveyedAt = Time.time;
            }
        }

        public void MarkExhausted(Vector2Int cell, GroundMaterial material)
        {
            var survey = _world.UpsertSurvey(cell);
            survey.State = SurveyState.Exhausted;
            survey.Confidence = 1f;
            survey.EstimatedMaterial = material;
        }

        private void AssignAndTickJobs(float delta)
        {
            var jobs = new List<JobRecord>();
            foreach (var job in _world.Jobs)
                if (job.Task is UnitTask.Surveying or UnitTask.Drilling && job.Status == BlueprintStatus.Active)
                    jobs.Add(job);
            jobs.Sort((a, b) => b.Priority != a.Priority ? b.Priority.CompareTo(a.Priority) : a.Id.CompareTo(b.Id));

            foreach (var job in jobs)
            {
                var engineer = _units.GetAgent(job.AssignedUnitId);
                if (engineer == null)
                {
                    engineer = _units.GetIdleEngineer();
                    if (engineer == null)
                    {
                        job.BlockedReason = "No idle engineer";
                        continue;
                    }
                    job.AssignedUnitId = engineer.Entity.Id;
                    engineer.Entity.Task = job.Task;
                    engineer.MoveTo(job.TargetCell);
                }

                if (job.Task == UnitTask.Drilling)
                {
                    var rig = FindNearbyBuilding(BuildableKind.DrillRig, job.TargetCell, 3);
                    if (rig == null)
                    {
                        job.BlockedReason = "Waiting for drill rig";
                        continue;
                    }
                    job.RelatedEntityId = rig.Id;
                }

                if (!engineer.IsAt(job.TargetCell))
                {
                    job.BlockedReason = "Engineer travelling";
                    continue;
                }

                engineer.Entity.Task = job.Task;
                job.BlockedReason = "";
                float efficiency = Mathf.Clamp01(engineer.Entity.Morale / 100f) * Mathf.Clamp(1f - engineer.Entity.Fatigue / 140f, 0.25f, 1f);
                job.WorkRemaining -= delta * Mathf.Max(0.25f, efficiency);
                engineer.Entity.Fatigue = Mathf.Min(100f, engineer.Entity.Fatigue + delta * 0.7f);
                if (job.WorkRemaining > 0f) continue;

                if (job.Task == UnitTask.Surveying) CompleteSurvey(job);
                else CompleteDrill(job);
                engineer.Entity.Task = UnitTask.Idle;
                job.Status = BlueprintStatus.Complete;
                _world.Jobs.Remove(job);
            }
        }

        private void CompleteSurvey(JobRecord job)
        {
            int radius = Mathf.Max(1, job.Radius);
            for (int z = -radius; z <= radius; z++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    var cellPos = job.TargetCell + new Vector2Int(x, z);
                    if (!_world.InBounds(cellPos) || x * x + z * z > radius * radius) continue;
                    var cell = _world.GetCell(cellPos);
                    OpenWorldState.NormalizeLayers(ref cell);
                    var target = FindBestResourceLayer(cell.Layers);
                    var record = _world.UpsertSurvey(cellPos);
                    float signal = Mathf.PerlinNoise((cellPos.x + _world.Seed) * 0.17f, (cellPos.y - _world.Seed) * 0.17f);
                    record.State = target.Material is GroundMaterial.Dirt or GroundMaterial.Stone or GroundMaterial.Clay ? SurveyState.Suspected : SurveyState.Surveyed;
                    record.Confidence = Mathf.Lerp(0.35f, 0.78f, signal);
                    record.EstimatedMaterial = target.Material;
                    int depth = DepthToLayer(cell.Layers, target.Index);
                    int error = Mathf.RoundToInt(Mathf.Lerp(3f, 1f, record.Confidence));
                    record.MinDepth = Mathf.Max(0, depth - error);
                    record.MaxDepth = depth + error;
                    record.MinGrade = Mathf.Max(0.05f, target.Layer.Grade - 0.18f);
                    record.MaxGrade = Mathf.Min(1f, target.Layer.Grade + 0.18f);
                    record.MinReserve = Mathf.Max(0, Mathf.RoundToInt(target.Layer.RemainingAmount * 0.65f));
                    record.MaxReserve = Mathf.RoundToInt(target.Layer.RemainingAmount * 1.35f);
                    record.SurveyedAt = Time.time;
                }
            }
        }

        private void CompleteDrill(JobRecord job)
        {
            var cell = _world.GetCell(job.TargetCell);
            OpenWorldState.NormalizeLayers(ref cell);
            var report = _world.AddDrillReport(job.TargetCell, job.RelatedEntityId);
            report.CompletedAt = Time.time;
            int depth = 0;
            foreach (var layer in cell.Layers)
            {
                report.Layers.Add(new DrillLayerReport
                {
                    Material = layer.Material,
                    StartDepth = depth,
                    EndDepth = depth + layer.Thickness,
                    Grade = layer.Grade,
                    Hardness = layer.Hardness,
                    WaterRisk = layer.WaterRisk,
                    RemainingAmount = layer.RemainingAmount
                });
                depth += layer.Thickness;
            }

            var target = FindBestResourceLayer(cell.Layers);
            var record = _world.UpsertSurvey(job.TargetCell);
            record.State = target.Layer.RemainingAmount > 0 ? SurveyState.Drilled : SurveyState.Exhausted;
            record.Confidence = 1f;
            record.EstimatedMaterial = target.Material;
            record.MinDepth = record.MaxDepth = DepthToLayer(cell.Layers, target.Index);
            record.MinGrade = record.MaxGrade = target.Layer.Grade;
            record.MinReserve = record.MaxReserve = target.Layer.RemainingAmount;
            record.SurveyedAt = Time.time;

            for (int z = -2; z <= 2; z++)
            for (int x = -2; x <= 2; x++)
            {
                var neighbor = job.TargetCell + new Vector2Int(x, z);
                if (!_world.InBounds(neighbor)) continue;
                var nearby = _world.UpsertSurvey(neighbor);
                if (nearby.State == SurveyState.Unknown) nearby.State = SurveyState.Suspected;
                nearby.Confidence = Mathf.Max(nearby.Confidence, 0.55f);
            }
        }

        private void RefreshSummary()
        {
            int active = 0;
            foreach (var job in _world.Jobs)
                if (job.Task is UnitTask.Surveying or UnitTask.Drilling) active++;
            StatusSummary = $"Survey {_world.Surveys.Count} | drill reports {_world.DrillReports.Count} | mines {_world.MiningZones.Count} | jobs {active}";
        }

        private BuildingEntity FindNearbyBuilding(BuildableKind kind, Vector2Int cell, int radius)
        {
            BuildingEntity best = null;
            int bestDistance = radius * radius;
            foreach (var building in _world.Buildings.Values)
            {
                if (building.Kind != kind || building.FactionId != OpenWorldConstants.PlayerFactionId) continue;
                int distance = (building.Origin - cell).sqrMagnitude;
                if (distance > bestDistance) continue;
                best = building;
                bestDistance = distance;
            }
            return best;
        }

        private static (GroundMaterial Material, MaterialLayer Layer, int Index) FindBestResourceLayer(MaterialLayer[] layers)
        {
            for (int i = 0; i < layers.Length; i++)
            {
                var material = layers[i].Material;
                if (material is GroundMaterial.IronOre or GroundMaterial.Coal or GroundMaterial.Sulfur or GroundMaterial.Nitrate or GroundMaterial.Oil)
                    return (material, layers[i], i);
            }
            int fallback = Mathf.Max(0, layers.Length - 1);
            return (layers[fallback].Material, layers[fallback], fallback);
        }

        private static int DepthToLayer(MaterialLayer[] layers, int layerIndex)
        {
            int depth = 0;
            for (int i = 0; i < layerIndex && i < layers.Length; i++) depth += layers[i].Thickness;
            return depth;
        }

        // ToResource now delegates to ResourceInventory.MatToResource
    }
}
