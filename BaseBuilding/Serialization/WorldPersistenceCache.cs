using System.Collections.Generic;
using GTA.Math;

namespace BaseBuilding.Serialization
{
    /// <summary>
    /// Holds runtime information about spawns.
    /// </summary>
    public class WorldPersistenceCache
    {
        public WorldPersistenceCache()
        {
            RockPersistence = new List<RockPersistenceInfo>();
            RockSpawnAreas = new List<Vector3>();
        }

        /// <summary>
        /// Persistence info for the rocks that have spawned
        /// at runtime.
        /// </summary>
        public List<RockPersistenceInfo> RockPersistence { get; set; }

        /// <summary>
        /// A set of areas in which rocks have spawned. If
        /// this is detected by the base building spawner then
        /// it won't spawn rocks in the area.
        /// </summary>
        public List<Vector3> RockSpawnAreas { get; set; }
    }

    public class RockPersistenceInfo
    {
        public string Scene { get; set; }

        public int ResourceId { get; set; }

        public int RockModelId { get; set; }

        public Vector3 Position { get; set; }

        public Vector3 Rotation { get; set; }

        public int PersistenceId { get; set; }
    }
}