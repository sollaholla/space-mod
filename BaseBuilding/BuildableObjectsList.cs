using System.Collections.Generic;
using System.Xml.Serialization;
using GTA.Math;

namespace BaseBuilding
{
    public class BuildableObjectsList
    {
        public BuildableObjectsList()
        {
            ObjectDefs = new List<ObjectInfo>();
        }

        [XmlArrayItem("Item")]
        public List<ObjectInfo> ObjectDefs { get; set; }
    }
}
