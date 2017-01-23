using System.Collections.Generic;
using GTA;
using GTA.Math;

namespace SpaceMod.DataClasses.SceneTypes
{
    public class IsslScene : Scene
    {
        private PlanetSystem _planetSystem;
        private Camera _camera;
        private Prop _issl;
        private Prop _earth;

        public override void Init()
        {
            _issl = World.CreateProp(Constants.IsslModel, Vector3.Zero, false, false);
            _earth = World.CreateProp(Constants.EarthLargeModel, Vector3.Zero, false, false);
            var galaxy = World.CreateProp(Constants.SpaceDomeModel, Vector3.Zero, false, false);

            ResetPlayerOrigin();

            var planets = new List<Planet>
            {
                new Planet(_earth.Handle, galaxy, Vector3.Zero, -0.5f),
                new Planet(_issl.Handle, galaxy, Vector3.Zero, 0)
            };

            TeleportPlayerToGalaxy();

            _earth.Position = galaxy.Position;
            _issl.Position = _earth.Position - _earth.RightVector * 1200 + _earth.UpVector * 150;
            var rotation = _issl.Rotation;
            rotation.Y = -30f;
            rotation.Z = -70f;
            _issl.Rotation = rotation;

            _planetSystem = new PlanetSystem(galaxy.Handle, planets, new List<Star>(), -1.5f);
            
            _camera = World.CreateCamera(Vector3.Zero, Vector3.Zero, GameplayCamera.FieldOfView);
            _camera.Shake(CameraShake.SkyDiving, 0.05f);
            World.RenderingCamera = _camera;
        }

        public override void Update()
        {
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
