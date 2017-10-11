using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using NativeUI;

namespace BaseBuilding
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

    public class Resource
    {
        public string Name { get; set; }

        public float AmountOfResources { get; set; } 
    }

    public class PlayerResource : Resource
    {
        public TextTimerBar TextBar { get; set; }

        public void Init()
        {
            TextBar = new TextTimerBar(Name, AmountOfResources.ToString());
            BaseBuildingCore.TimerPool.Add(TextBar);
        }

        public static PlayerResource GetPlayerResource(Resource r)
        {
            var pResource = new PlayerResource();
            pResource.Name = r.Name;
            pResource.AmountOfResources = r.AmountOfResources;
            pResource.Init();
            return pResource;
        }
    }
}
