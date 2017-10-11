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

    public class ResourceDefinitionList
    {
        public ResourceDefinitionList()
        {
            Definitions = new List<ResourceDefinition>();
        }

        [XmlArrayItem("Item")]
        public List<ResourceDefinition> Definitions;
    }

    public class ResourceDefinition
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public class Resource
    {
        public int Id { get; set; }

        public virtual int Amount { get; set; }

        public static string GetName()
        {
            
        }
    }

    public class PlayerResource : Resource
    {
        public override int Amount
        {
            get { return base.Amount; }
            set
            {
                base.Amount = value;
                TextBar.Label = value + "x";
            }
        }

        public TextTimerBar TextBar { get; set; }

        private int _amount;

        public void Init()
        {
            TextBar = new TextTimerBar(AmountOfResources + "x", Name);
            BaseBuildingCore.TimerPool.Add(TextBar);
        }

        public static PlayerResource GetPlayerResource(Resource r)
        {
            var pResource = new PlayerResource();
            pResource.Id = r.Id;
            pResource.Amount = r.Amount;
            pResource.Init();
            return pResource;
        }
    }
}
