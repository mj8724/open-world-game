using UnityEngine;

namespace OpenWorld
{
    public class OpenWorldJobSystem : MonoBehaviour
    {
        private OpenWorldState _world;
        private SurfaceTerrainSystem _terrain;
        private BuildingSystem _buildings;
        private UnitSystem _units;

        public void Initialize(OpenWorldState world, SurfaceTerrainSystem terrain, BuildingSystem buildings, UnitSystem units)
        {
            _world = world;
            _terrain = terrain;
            _buildings = buildings;
            _units = units;
        }

        private void Update()
        {
            if (_world == null) return;
            AssignJobs();
            TickAssignedJobs();
        }

        public void QueueDigArea(Vector2Int center, int radius)
        {
            for (int z = -radius; z <= radius; z++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    var p = center + new Vector2Int(x, z);
                    if (!_world.InBounds(p)) continue;
                    if (new Vector2(x, z).magnitude > radius + 0.25f) continue;
                    _world.AddJob(UnitTask.Digging, p);
                }
            }
        }

        public void QueueBuild(BuildableKind kind, Vector2Int origin)
        {
            _world.AddJob(UnitTask.Building, origin, kind);
        }

        private void AssignJobs()
        {
            foreach (var job in _world.Jobs)
            {
                if (job.AssignedUnitId != 0) continue;
                var worker = _units.GetIdleWorker();
                if (worker == null) return;
                job.AssignedUnitId = worker.Entity.Id;
                worker.Entity.Task = job.Task;
                worker.MoveTo(job.TargetCell);
            }
        }

        private void TickAssignedJobs()
        {
            for (int i = _world.Jobs.Count - 1; i >= 0; i--)
            {
                var job = _world.Jobs[i];
                var worker = FindWorker(job.AssignedUnitId);
                if (worker == null)
                {
                    job.AssignedUnitId = 0;
                    continue;
                }

                if (!worker.IsAt(job.TargetCell)) continue;

                job.WorkRemaining -= Time.deltaTime;
                worker.Entity.Task = job.Task;
                if (job.WorkRemaining > 0) continue;

                CompleteJob(job);
                worker.Entity.Task = UnitTask.Idle;
                _world.Jobs.RemoveAt(i);
            }
        }

        private void CompleteJob(JobRecord job)
        {
            switch (job.Task)
            {
                case UnitTask.Digging:
                    _terrain.ApplyBrush(TerrainTool.Dig, job.TargetCell, 0, 0.5f);
                    break;
                case UnitTask.Building:
                    _buildings.TryPlace(job.BuildKind, job.TargetCell, 0, 1);
                    break;
            }
        }

        private UnitAgent FindWorker(int id)
        {
            if (id == 0) return null;
            foreach (var agent in _units.AllAgents())
                if (agent.Entity.Id == id) return agent;
            return null;
        }
    }
}
