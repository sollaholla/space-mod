using System.Collections.Generic;
using System.Drawing;
using GTA;
using GTA.Math;
using NativeUI;
using SpaceMod.DataClasses.MissionTypes;

namespace SpaceMod.DataClasses.SceneTypes
{
    public class IsslScene : Scene
    {
        private OrbitalSystem _planetSystem;
        private Camera _camera;
        private Prop _issl;
        private Prop _earth;

        private readonly UIMenu _missionMenu = new UIMenu(string.Empty, "SELECT A MISSION", new Point(0, -105));

        public IsslScene()
        {
            var mission1 = new UIMenuItem("Take Back What's Ours",
                "The ISSL has reported that alien life-forms have been " +
                "spotted on the surface of the moon, planning their next " +
                "attack our home planet. Find and elimite them before they can reach earth.");
            _missionMenu.AddItem(mission1);
            mission1.Activated += (sender, item) =>
            {
                ModController.Instance.SetCurrentMission(new TakeBackWhatsOurs());
                End(new EarthOrbitScene());
            };

            _missionMenu.OnMenuClose += sender => _missionMenu.Visible = true;
        }

        public override void Init()
        {
            var galaxy = World.CreateProp(Constants.SpaceDomeModel, Vector3.Zero, false, false);
            _issl = World.CreateProp(Constants.IsslModel, Vector3.Zero, false, false);
            _earth = World.CreateProp(Constants.EarthLargeModel, Vector3.Zero, false, false);
            
            var orbitals = new List<Orbital>
            {
                new Orbital(_earth.Handle, galaxy, Vector3.Zero, -0.5f),
                new Orbital(_issl.Handle, galaxy, Vector3.Zero, 0)
            };

            MovePlayerToGalaxy();
            
            // Move the earth to the galaxy origin.
            _earth.Position = galaxy.Position;

            // Place the issl.
            _issl.Position = new Vector3(-1200, 0, 8400);
            _issl.Rotation = new Vector3(0, -30f, -70f);

            _planetSystem = new OrbitalSystem(galaxy.Handle, orbitals, new List<LockedOrbital>(), -1.5f);

            // Create cinematic camera.
            _camera = World.CreateCamera(Vector3.Zero, Vector3.Zero, GameplayCamera.FieldOfView);
            _camera.Shake(CameraShake.SkyDiving, 0.05f);
            World.RenderingCamera = _camera;

            // open menu.
            _missionMenu.Visible = true;
        }

        public override void Update()
        {
            if (_missionMenu.Visible)
            {
                _missionMenu.ProcessControl();
                _missionMenu.ProcessMouse();
                _missionMenu.Draw();
            }

            _planetSystem.Process(_camera.Position);

            var dirOut = _issl.Position - _earth.Position;
            dirOut = dirOut.Normalized * 200;
            var pos = _issl.Position + dirOut + _issl.UpVector * 30;

            _camera.Position = pos;
            _camera.PointAt(_issl);
        }

        public override void Abort()
        {
            _planetSystem?.Abort();
            _camera.Destroy();
            World.RenderingCamera = null;
        }

        public override void CleanUp()
        {
            _planetSystem?.Abort();
            _camera.Destroy();
            World.RenderingCamera = null;
        }
    }
}
