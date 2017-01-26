using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;

namespace SpaceMod.DataClasses.SceneTypes
{
    public class MoonOrbitScene : Scene
    {
        private PlanetSystem _planetSystem;
        private Prop _earth;
        private Prop _moon;

        public static Vector3[] Positions => new[]
        {
            new Vector3(-15370.74f, -12107.31f, 8620.764f), /*Earth*/
            new Vector3(-6870.744f, -12107.31f, 8620.764f)  /*Moon*/
        };

        public override void Init()
        {
            // Create props.
            _earth = World.CreateProp(Constants.EarthMedModel, Vector3.Zero, false, false);
            _moon = World.CreateProp(Constants.MoonLargeModel, Vector3.Zero, false, false);
            var sun = World.CreateProp(Constants.SunSmallModel, Vector3.Zero, false, false);
            var galaxy = World.CreateProp(Constants.SpaceDomeModel, Vector3.Zero, false, false);

            // Move the player to v3.zero so that we can spawn these props.
            ResetPlayerOrigin();

            // Create planets and stars.
            var planets = new List<Planet>
            {
                new Planet(_earth.Handle, galaxy, Vector3.Zero, -3.0f) /*Earth*/,
                new Planet(_moon.Handle, galaxy, Vector3.Zero, 3.5f) /*Moon*/
            };
            var stars = new List<Star>
            {
                new Star(sun.Handle, Constants.SunOffsetNearEarth) /*Sun*/
            };

            // Move player back to the galaxy center.
            TeleportPlayerToGalaxy();

            // Set the position of the earth and moon.
            _earth.Position = Positions[0];
            _moon.Position = Positions[1];

            // Reset the suns position.
            sun.Position = Constants.GalaxyCenter;

            // Create the planet system.
            _planetSystem = new PlanetSystem(galaxy.Handle, planets, stars, -1.5f);

            SetStartDirection(_moon.Position, PlayerPed.IsInVehicle() ? PlayerPed.CurrentVehicle as Entity : PlayerPed, StartDirection);
        }

        private void GoToMoon()
        {
            // Travel to the moon.
            var dist = PlayerPosition.DistanceTo(_moon.Position);
            if (dist > 2000) return;
            End(new MoonSurfaceScene());
        }

        public override void Update()
        {
            // Process the planets and stars.
            _planetSystem.Process(Constants.GetValidGalaxyDomePosition(PlayerPed));

            // Try to go to the moon.
            GoToMoon();

            // Try to leave orbit.
            LeaveOrbit();
        }

        private void LeaveOrbit()
        {
            // Try to leave the moons orbit.
            var dist = PlayerPosition.DistanceTo(_earth.Position);
            if (dist > 2500) return;
            End(new EarthOrbitScene(), SceneStartDirection.ToTarget);
        }

        public override void Abort()
        {
            _planetSystem?.Abort();
        }

        public override void CleanUp()
        {
            _planetSystem?.Abort();
        }
    }
}
