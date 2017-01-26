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
        }

        public override void Init()
        {
            _missionMenu.Visible = true;

            _issl = World.CreateProp(Constants.IsslModel, Vector3.Zero, false, false);
            _earth = World.CreateProp(Constants.EarthLargeModel, Vector3.Zero, false, false);
            var galaxy = World.CreateProp(Constants.SpaceDomeModel, Vector3.Zero, false, false);

            ResetPlayerOrigin();

            var planets = new List<Orbital>
            {
                new Orbital(_earth.Handle, galaxy, Vector3.Zero, -0.5f),
                new Orbital(_issl.Handle, galaxy, Vector3.Zero, 0)
            };

            TeleportPlayerToGalaxy();

            _earth.Position = galaxy.Position;
            _issl.Position = _earth.Position - _earth.RightVector * 1200 + _earth.UpVector * 150;
            var rotation = _issl.Rotation;
            rotation.Y = -30f;
            rotation.Z = -70f;
            _issl.Rotation = rotation;

            _planetSystem = new OrbitalSystem(galaxy.Handle, planets, new List<LockedOrbital>(), -1.5f);

            _camera = World.CreateCamera(Vector3.Zero, Vector3.Zero, GameplayCamera.FieldOfView);
            _camera.Shake(CameraShake.SkyDiving, 0.05f);
            World.RenderingCamera = _camera;
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
