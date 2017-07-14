using GTA.Math;
using GTA;

namespace SpaceMod.Missions.Types
{
    public class SatelliteDish
    {
        private Blip blip = null;
        private Prop Laptop = null;

        /// <summary>
        /// Our standard constuctor.
        /// </summary>
        /// <param name="position">The position of the player while checking the laptop.</param>
        public SatelliteDish(Vector3 position)
        {
            Position = position;
            CheckedForData = false;
        }

        /// <summary>
        /// Our secondary constuctor.
        /// </summary>
        /// <param name="position">The position of the player while checking the laptop.</param>
        /// <param name="laptopPosition">The position that our laptop will be placed.</param>
        /// <param name="laptopRotation">The rotation of the laptop.</param>
        /// <param name="heading">The heading of the player while checking the laptop.</param>
        public SatelliteDish(Vector3 position, Vector3 laptopPosition, Vector3 laptopRotation, float heading)
        {
            LaptopPosition = laptopPosition;
            LaptopRotation = laptopRotation;
            Position = position;
            CheckedForData = false;
            Heading = heading;
        }

        /// <summary>
        /// The position of the player while checking the laptop.
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// True if we've checked the laptop for data.
        /// </summary>
        public bool CheckedForData { get; set; }

        /// <summary>
        /// The position of the laptop.
        /// </summary>
        public Vector3 LaptopPosition { get; set; }

        /// <summary>
        /// The rotation of the laptop.
        /// </summary>
        public Vector3 LaptopRotation { get; set; }

        /// <summary>
        /// The heading of the player while checking the laptop.
        /// </summary>
        public float Heading { get; set; }

        /// <summary>
        /// Create the laptop prop that we'll use for hacking.
        /// </summary>
        public void CreateLaptop()
        {
            Model m = new Model("p_cs_laptop_02");
            if (!m.IsLoaded)
                m.Request(5000);
            Laptop = World.CreateProp(m, LaptopPosition, LaptopRotation, false, false);
            m.MarkAsNoLongerNeeded();
        }

        /// <summary>
        /// Create a blip for the sat dish.
        /// </summary>
        public void CreateBlip()
        {
            if (blip != null)
                return;

            blip = new Blip(World.CreateBlip(Position).Handle)
            {
                Color = BlipColor.Yellow,
                Name = "Satellite Dish",
            };
        }

        /// <summary>
        /// Draw a marker and remove our blip if we've checked for data.
        /// </summary>
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

        /// <summary>
        /// Remove the blip from the world.
        /// </summary>
        public void RemoveBlip()
        {
            blip?.Remove();
            blip = null;
        }

        /// <summary>
        /// Remove the laptop from the world.
        /// </summary>
        public void RemoveLaptop()
        {
            Laptop?.Delete();
            Laptop = null;
        }

        /// <summary>
        /// Abort this class, and clean up props/blips.
        /// </summary>
        public void Aborted()
        {
            RemoveBlip();
            RemoveLaptop();
        }
    }
}
