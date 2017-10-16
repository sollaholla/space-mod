using System.Collections.Generic;
using System.Xml.Serialization;

namespace BaseBuilding
{
    public class RockModelInfo : IParticleInfo
    {
        public RockModelInfo()
        {
            ChunkModels = new List<ChunkInfo>();
        }

        public float SpawnChance { get; set; }

        public int MaxRocksPerPatch { get; set; }

        public int MaxPatches { get; set; }

        public int MaxHealth { get; set; }

        public string RockModel { get; set; }

        public float ZOffset { get; set; }

        [XmlArrayItem("Item")]
        public List<ChunkInfo> ChunkModels { get; set; }

        public string ParticleDict { get; set; }

        public string ParticleName { get; set; }

        public float ParticleScale { get; set; }
    }
}