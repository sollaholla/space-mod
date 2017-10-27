﻿using System.Collections.Generic;
using System.Xml.Serialization;
using BaseBuilding.Interfaces;

namespace BaseBuilding.Serialization
{
    public class RockModelInfo : IParticleInfo
    {
        public RockModelInfo()
        {
            ChunkModels = new List<ChunkInfo>();
        }

        public float SpawnChance { get; set; }

        public int Id { get; set; }

        public int MaxRocksPerPatch { get; set; }

        public int MaxPatches { get; set; }

        public int MaxHits { get; set; }

        public string RockModel { get; set; }

        public float ZOffset { get; set; }

        [XmlArrayItem("Item")]
        public List<ChunkInfo> ChunkModels { get; set; }

        public string ParticleDict { get; set; }

        public string ParticleName { get; set; }

        public float ParticleScale { get; set; }
    }
}