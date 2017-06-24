using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Math;
using GTA;

namespace SpaceMod
{
    public class SatelliteDish
    {
        public Vector3 Position { get; set; }
        public bool CheckedForData { get; set; }
        public Blip blip = null;
        public Prop Laptop = null;
        public float Heading;

        public SatelliteDish(Vector3 position)
        {
            Position = position;
            CheckedForData = false;
        }

        public SatelliteDish(Vector3 position, Vector3 laptopPosition, Vector3 laptopRotation, float heading)
        {
            Position = position;
            CheckedForData = false;
            Laptop = World.CreateProp(new Model("p_cs_laptop_02"), laptopPosition, laptopRotation, false, false);
            Heading = heading;
        }

        public void CreateBlip()
        {
            if (blip == null)
            {
                blip = new Blip(World.CreateBlip(Position).Handle)
                {
                    Color = BlipColor.Yellow,
                    Name = "Satellite Dish",
                };
            }
        }

        public void Update()
        {
            if (CheckedForData)
            {
                RemoveBlip();
                return;
            }
            if (Game.IsLoading) return;

            World.DrawMarker(MarkerType.UpsideDownCone, Position, Vector3.Zero, Vector3.Zero, new Vector3(1, 1, 1), System.Drawing.Color.Yellow);
        }

        public void RemoveBlip()
        {
            if(blip != null)
            {
                blip.Remove();
                blip = null;
            }
        }

        public void RemoveLaptop()
        {
            if(Laptop != null)
            {
                Laptop.Delete();
                Laptop = null;
            }
        }

        public void Aborted()
        {
            RemoveBlip();
            RemoveLaptop();
        }
    }
}
