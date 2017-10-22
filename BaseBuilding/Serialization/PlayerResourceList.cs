using System.Collections.Generic;
using System.Xml.Serialization;
using BaseBuilding.Resources;

namespace BaseBuilding.Serialization
{
    public class PlayerResourceList
    {
        public PlayerResourceList()
        {
            Resources = new List<Resource>();
        }

        [XmlArrayItem("Item")]
        public List<Resource> Resources { get; set; }
    }
}