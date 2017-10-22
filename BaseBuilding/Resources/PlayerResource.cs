using System.Collections.Generic;
using BaseBuilding.Serialization;
using NativeUI;

namespace BaseBuilding.Resources
{
    public class PlayerResource : Resource
    {
        public override int Amount
        {
            get => base.Amount;
            set
            {
                base.Amount = value;
                if (TextBar != null)
                    TextBar.Label = value + "x";
            }
        }

        public TextTimerBar TextBar { get; set; }

        public void Init(List<ResourceDefinition> defs, TimerBarPool timerPool)
        {
            TextBar = new TextTimerBar(Amount + "x", GetName(this, defs));
            timerPool.Add(TextBar);
        }

        // TODO: Not sure if we need this yet.
        public static PlayerResource GetPlayerResource(Resource r, List<ResourceDefinition> defs,
            TimerBarPool timerPool)
        {
            var pResource = new PlayerResource
            {
                Id = r.Id,
                Amount = r.Amount
            };
            pResource.Init(defs, timerPool);
            return pResource;
        }

        public void Dispose(TimerBarPool pool)
        {
            pool.Remove(TextBar);

            TextBar = null;
        }
    }
}