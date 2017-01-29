using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Native;
using GTA.Math;

namespace SpaceMod.DataClasses.SceneTypes
{
    public class MarsOrbitScene : Scene
    {
        public static Vector3[] Positions => new[]
        {
            new Vector3(-6870.744f, -12107.31f, 8620.764f), /*Mars*/
        };

        private OrbitalSystem _planetSystem;
        private Prop _mars;

        public override void Init()
        {
            _mars = World.CreateProp(Constants.MarsLargeModel, Vector3.Zero, false, false);
            var galaxy = World.CreateProp(Constants.SpaceDomeModel, Vector3.Zero, false, false);
            var sun = World.CreateProp(Constants.SunSmallModel, Vector3.Zero, false, false);

            var orbitals = new List<Orbital>()
            {
                new Orbital(_mars.Handle, "Mars", galaxy, Vector3.Zero, -3.5f)
            };
            var lockedOrbitals = new List<LockedOrbital>()
            {
                new LockedOrbital(sun.Handle, Constants.SunOffsetNearEarth)
            };

            MovePlayerToGalaxy();

            _mars.Position = Positions[0];

            // Reset the suns position.
            sun.Position = Constants.GalaxyCenter;

            _planetSystem = new OrbitalSystem(galaxy.Handle, orbitals, lockedOrbitals, -1.5f);
            SetStartDirection(_mars.Position, PlayerPed.IsInVehicle() ? PlayerPed.CurrentVehicle as Entity : PlayerPed,
                StartDirection);
        }

        private void GoToMars()
        {
            var dist = PlayerPosition.DistanceTo(_mars.Position);

            if (dist > 2000) return;
            End(new MarsSurfaceScene());
        }

        public override void Update()
        {
            _planetSystem.Process(Constants.GetValidGalaxyDomePosition(PlayerPed));
            GoToMars();
        }

        public override void Abort()
        {
            Done();
        }

        public override void CleanUp()
        {
            Done();
        }

        private void Done()
        {
            _planetSystem.Abort();
            Game.TimeScale = 1;
        }
    }
}
