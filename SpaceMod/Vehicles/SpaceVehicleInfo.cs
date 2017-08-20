using System.Collections.Generic;

namespace GTS.Vehicles
{
    public class SpaceVehicleInfo
    {
        public List<SpaceVehicle> VehicleData { get; } = new List<SpaceVehicle>();
    }

    public class SpaceVehicle
    {
        public string Model { get; set; }
        public float Speed { get; set; }
        public bool RemainInOrbit { get; set; }
        public float RotationMultiplier { get; set; }
    }
}