using System.Collections.Generic;
using System.Xml.Serialization;
using GTA;

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
        public bool NewtonianPhysics { get; set; } = true;
        public float Drag { get; set; } = 0.001f;

        [XmlArrayItem("Item")]
        public List<VehicleDoor> OpenDoorsSpaceWalk { get; set; } = new List<VehicleDoor>();

        public float RopeLength { get; set; } = 25f;
    }
}