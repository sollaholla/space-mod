using System.Collections.Generic;
using System.Xml.Serialization;

namespace BaseBuilding
{
    public class ResourceDefinitionList
    {
        public ResourceDefinitionList()
        {
            Definitions = new List<ResourceDefinition>();
        }

        [XmlArrayItem("Item")]
        public List<ResourceDefinition> Definitions;
    }
}