using System.Collections.Generic;
using GTA;
using GTA.Math;

namespace SpaceMod.DataClasses.SceneTypes
{
    public class EarthOrbitScene : Scene
    {
        public static Vector3[] Positions => new[]
        {
            new Vector3(-6870.744f, -12107.31f, 8620.764f), /*Earth*/
            new Vector3(-15370.74f, -12107.31f, 8620.764f) /*Moon*/
        };
        
        private PlanetSystem _planetSystem;
        private Prop _earth;
        private Prop _moon;

        public EarthOrbitScene()
        {
        }

        public override void Init()
        {
            _earth = World.CreateProp(Constants.EarthLargeModel, Vector3.Zero, false, false);
            _moon = World.CreateProp(Constants.MoonMedModel, Vector3.Zero, false, false);
            var sun = World.CreateProp(Constants.SunSmallModel, Vector3.Zero, false, false);
            var galaxy = World.CreateProp(Constants.SpaceDomeModel, Vector3.Zero, false, false);

            ResetPlayerOrigin();
            
            var planets = new List<Planet>
            {
                new Planet(_earth.Handle, PlayerPed, Vector3.Zero, false, -3.5f) /*Earth*/,
                new Planet(_moon.Handle, galaxy, Vector3.Zero, true, 3.0f) /*Moon*/
            };
            var stars = new List<Star>
            {
                new Star(sun.Handle, Constants.SunOffsetNearEarth) /*Sun*/
            };

            TeleportPlayerToGalaxy();

            _earth.Position = Positions[0];
            _moon.Position = Positions[1];

            sun.Position = Constants.GalaxyCenter;
            _planetSystem = new PlanetSystem(galaxy.Handle, planets, stars, -1.5f);
        }

        public override void Update()
        {
            _planetSystem.Process(Constants.GetValidGalaxyDomePosition(PlayerPed));
            GoToMoon();
            GoToEarth();
        }

        private void GoToMoon()
        {
            var dist = PlayerPosition.DistanceTo(_moon.Position);
            if (dist > 2500) return;

            // Check the moon orbit scene for reasons why i'm rotating towards the earth here.
            End(new MoonOrbitScene());
        }

        private void GoToEarth()
        {
            var dist = PlayerPosition.DistanceTo(_earth.Position);
            if (dist > 1500) return;
            End(null);
        }

        public override void Abort()
        {
            _planetSystem.Abort();
        }

        public override void CleanUp()
        {
            _planetSystem.Abort();
        }
    }
}
