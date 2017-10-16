using System.Collections.Generic;
using System.Xml.Serialization;

namespace BaseBuilding
{
    public class ResourceDefinitionList
    {
        [XmlArrayItem("Item")] public List<ResourceDefinition> Definitions;

        public ResourceDefinitionList()
        {
            Definitions = new List<ResourceDefinition>();
        }
    }
}