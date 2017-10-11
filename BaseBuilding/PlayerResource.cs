using System.Collections.Generic;
using NativeUI;

namespace BaseBuilding
{
    public class PlayerResource : Resource
    {
        public override int Amount
        {
            get => base.Amount;
            set
            {
                base.Amount = value;
                TextBar.Label = value + "x";
            }
        }

        public TextTimerBar TextBar { get; set; }

        public void Init(List<ResourceDefinition> defs)
        {
            TextBar = new TextTimerBar(Amount + "x", GetName(this, defs));
            BaseBuildingCore.TimerPool.Add(TextBar);
        }

        // TODO: Not sure if we need this yet.
        public static PlayerResource GetPlayerResource(List<ResourceDefinition> defs, Resource r)
        {
            var pResource = new PlayerResource
            {
                Id = r.Id,
                Amount = r.Amount
            };
            pResource.Init(defs);
            return pResource;
        }
    }
}