using System.Collections.Generic;
using System.Xml.Serialization;

namespace BaseBuilding.Serialization
{
    public class RockInfo
    {
        public RockInfo()
        {
            RockModels = new List<RockModelInfo>();
            TargetScenes = new List<string>();
        }

        [XmlArrayItem("Item")]
        public List<RockModelInfo> RockModels { get; set; }

        [XmlArrayItem("Item")]
        public List<string> TargetScenes { get; set; }
    }
}