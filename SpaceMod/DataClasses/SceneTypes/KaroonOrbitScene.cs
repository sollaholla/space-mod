using System.Collections.Generic;
using GTA;
using GTA.Math;

namespace SpaceMod.DataClasses.SceneTypes
{
    public class KaroonOrbitScene : Scene
    {
        private Camera _camera;
        private Prop _karoon;
        private Prop _wormHole;
        private OrbitalSystem _system;

        public static Vector3[] Positions => new[]
        {
            new Vector3(-6870.744f, -12107.31f, 8620.764f),     /*Karoon*/
            new Vector3(-11870.744f, -12107.31f, 8831.764f),    /*Worm Hole*/ 
        };

        public override void Init()
        {
            _camera = World.CreateCamera(GameplayCamera.Position, GameplayCamera.Rotation, GameplayCamera.FieldOfView);
            World.RenderingCamera = _camera;

            _karoon = World.CreateProp(Database.KaroonLargeModel, Vector3.Zero, false, false);
            _wormHole = World.CreateProp(Database.WormHoleSmallModel, Vector3.Zero, false, false);
            var galaxy = World.CreateProp(Database.SpaceDomeAndromedaMode, Vector3.Zero, false, false);
            var sun = World.CreateProp(Database.AndromedaSunBlueModel, Vector3.Zero, false, false);

            var orbitals = new List<Orbital> {
                new Orbital(_karoon.Handle, "Karoon", galaxy, Vector3.Zero, -3.5f),
                new Orbital(_wormHole.Handle, "Keplar 983b", galaxy, Vector3.Zero, -5)
            };
            var lockedOrbitals = new List<LockedOrbital> {
                new LockedOrbital(sun.Handle, Database.SunOffsetNearEarth)
            };

            MovePlayerToGalaxy();

            _karoon.Position = Positions[0];
            _wormHole.Position = Positions[1];

            _system = new OrbitalSystem(galaxy.Handle, orbitals, lockedOrbitals, -1.5f);
            SetStartDirection(_karoon.Position, PlayerPed.IsInVehicle() ? (ISpatial)PlayerPed.CurrentVehicle : PlayerPed, StartDirection);
        }

        public override void Update()
        {
            _camera.Position = GameplayCamera.Position;
            _camera.Rotation = GameplayCamera.Rotation;
            _camera.Direction = GameplayCamera.Direction;
            _camera.IsActive = FollowCam.ViewMode != FollowCamViewMode.FirstPerson;
            World.RenderingCamera = _camera.IsActive ? _camera : null;

            EnterWormHole(new MarsOrbitScene(), _wormHole, _camera);
            _system?.Process(Database.GetValidGalaxyDomePosition(PlayerPed));
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
            _system?.Abort();
        }
    }
}
