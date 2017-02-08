using System.Collections.Generic;
using GTA;
using GTA.Math;
using System.Drawing;

namespace SpaceMod.DataClasses.SceneTypes
{
    public class MarsOrbitScene : Scene
    {
        public static Vector3[] Positions => new[]
        {
            new Vector3(-6870.744f, -12107.31f, 8620.764f), /*Mars*/
            new Vector3(-15370.74f, -12107.31f, 8620.764f) /*The thing we need to get to*/
        };

        private OrbitalSystem _planetSystem;
        private Prop _mars;

        private readonly UIText _leaveMarsNameText = new UIText(string.Empty, new Point(), 0.5f)
        {
            Centered = true,
            Font = GTA.Font.Monospace,
            Shadow = true
        };

        private readonly UIText _leaveMarsDistanceText = new UIText(string.Empty, new Point(), 0.5f)
        {
            Centered = true,
            Font = GTA.Font.Monospace,
            Shadow = true
        };

        public override void Init()
        {
            _mars = World.CreateProp(Constants.MarsLargeModel, Vector3.Zero, false, false);
            var galaxy = World.CreateProp(Constants.SpaceDomeModel, Vector3.Zero, false, false);
            var sun = World.CreateProp(Constants.SunSmallModel, Vector3.Zero, false, false);

            var orbitals = new List<Orbital>
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

        private void LeaveOrbit()
        {
            // Try to leave the moons orbit.
            var dist = PlayerPosition.DistanceTo(Positions[1]);
            if (dist > 2500) return;
            End(new EarthOrbitScene(), SceneStartDirection.ToTarget);
        }

        private void DrawMarker()
        {
            //World.DrawMarker(MarkerType.UpsideDownCone, Positions[1], Vector3.WorldDown, Vector3.Zero, new Vector3(1, 1, 1), System.Drawing.Color.Yellow);

            if (Positions[1].IsOnScreen() && OrbitalSystem.ShowUIPositions)
                Utilities.ShowUIPosition(null, 10, Positions[1], Constants.PathToDatabase, "Earth", _leaveMarsNameText,
                    _leaveMarsDistanceText);
        }

        public override void Update()
        {
            _planetSystem.Process(Constants.GetValidGalaxyDomePosition(PlayerPed));
            GoToMars();
            DrawMarker();
            LeaveOrbit();
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
            _planetSystem?.Abort();
            Game.TimeScale = 1;
        }
    }
}
