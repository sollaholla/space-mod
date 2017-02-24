using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using System.Drawing;
using GTA.Native;

namespace SpaceMod.DataClasses.SceneTypes
{
    public class MarsOrbitScene : Scene
    {
        public static Vector3[] Positions => new[]
        {
            new Vector3(-6870.744f, -12107.31f, 8620.764f), /* Mars */
            new Vector3(-15370.74f, -12107.31f, 8620.764f)  /* Earth */,
            new Vector3(-9870.744f, -9107.31f, 8831.764f)   /* Worm Hole */
        };

        private OrbitalSystem _planetSystem;
        private Prop _mars;
        private Prop _wormHole;
        private Camera _camera;

        private readonly UIText _leaveMarsNameText = new UIText(string.Empty, new Point(), 0.5f) {
            Centered = true,
            Font = GTA.Font.Monospace,
            Shadow = true
        };

        private readonly UIText _leaveMarsDistanceText = new UIText(string.Empty, new Point(), 0.5f) {
            Centered = true,
            Font = GTA.Font.Monospace,
            Shadow = true
        };

        public override void Init()
        {
            _camera = World.CreateCamera(GameplayCamera.Position, GameplayCamera.Rotation, GameplayCamera.FieldOfView);
            World.RenderingCamera = _camera;

            _mars = World.CreateProp(Database.MarsLargeModel, Vector3.Zero, false, false);
            _wormHole = World.CreateProp(Database.WormHoleSmallModel, Vector3.Zero, false, false);
            var galaxy = World.CreateProp(Database.SpaceDomeModel, Vector3.Zero, false, false);
            var sun = World.CreateProp(Database.SunSmallModel, Vector3.Zero, false, false);

            var orbitals = new List<Orbital> {
                new Orbital(_mars.Handle, "Mars", galaxy, Vector3.Zero, -3.5f),
                new Orbital(_wormHole.Handle, "Keplar 983b", galaxy, Vector3.Zero, -5)
            };
            var lockedOrbitals = new List<LockedOrbital> {
                new LockedOrbital(sun.Handle, Database.SunOffsetNearEarth)
            };

            MovePlayerToGalaxy();

            _mars.Position = Positions[0];
            _wormHole.Position = Positions[2];

            // Reset the suns position.
            sun.Position = Database.GalaxyCenter;

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
                Utilities.ShowUIPosition(null, 10, Positions[1], Database.PathToSprites, "Earth", _leaveMarsNameText,
                    _leaveMarsDistanceText);
        }

        public override void Update()
        {
            _camera.Position = GameplayCamera.Position;
            _camera.Rotation = GameplayCamera.Rotation;
            _camera.Direction = GameplayCamera.Direction;
            _camera.IsActive = FollowCam.ViewMode != FollowCamViewMode.FirstPerson;
            World.RenderingCamera = _camera.IsActive ? _camera : null;

            _planetSystem.Process(Database.GetValidGalaxyDomePosition(PlayerPed));
            GoToMars();
            EnterWormHole(new KaroonOrbitScene(), _wormHole, _camera);
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
            GameplayCamera.StopShaking();
            _camera.Destroy();
            World.RenderingCamera = null;
            _planetSystem?.Abort();
            Game.TimeScale = 1;
        }
    }
}
