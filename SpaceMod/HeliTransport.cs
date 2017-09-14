using System.Collections.Generic;
using GTA;
using GTA.Math;

namespace GTS
{
    public class HeliTransport
    {
        private readonly List<Vehicle> _helis;

        public HeliTransport()
        {
            _helis = new List<Vehicle>();
        }

        public void Load()
        {
            var positions = new[]
            {
                new Vector3(-6546.155f, -1332.292f, 30.23943f),
                new Vector3(-6573.748f,-1342.227f,30.23943f)
            };

            var model = new Model("buzzard");
            model.Request();
            while (!model.IsLoaded)
                Script.Yield();

            foreach (var position in positions)
            {
                var veh = World.CreateVehicle(model, position);
                veh.PlaceOnGround();
                _helis.Add(veh);
            }
        }

        public void Delete()
        {
            foreach (var vehicle in _helis)
                vehicle.Delete();
        }
    }
}
