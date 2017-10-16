using System;

namespace BaseBuilding
{
    public class PickupEventArgs : EventArgs
    {
        public PickupEventArgs(int amount, ResourceDefinition resourceDefinition)
        {
            Amount = amount;
            ResourceDefinition = resourceDefinition;
        }

        public int Amount { get; set; }
        public ResourceDefinition ResourceDefinition { get; set; }
    }
}