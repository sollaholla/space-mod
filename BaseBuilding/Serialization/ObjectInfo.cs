using System.Collections.Generic;
using System.Xml.Serialization;
using BaseBuilding.Resources;

namespace BaseBuilding.Serialization
{
    public class ObjectInfo
    {
        public ObjectInfo()
        {
            ValidAttachmentsList = new List<string>();
            ResourcesRequired = new List<Resource>();
        }

        public string ModelName { get; set; }

        public string FriendlyName { get; set; }

        [XmlArrayItem("Item")]
        public List<string> ValidAttachmentsList { get; set; }

        [XmlArrayItem("Item")]
        public List<Resource> ResourcesRequired { get; set; }
    }
}