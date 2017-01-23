using System.Collections.Generic;
using GTA;
using GTA.Math;

namespace SpaceMod.DataClasses
{
    public enum RotationAxis
    {
        Z,
        X,
        Y
    }

    public class PlanetSystem : Entity
    {
        private readonly Prop _prop;
        private readonly List<Planet> _planets;
        private readonly List<Star> _stars;
        private readonly RotationAxis _axis;

        public PlanetSystem(int handle, List<Planet> planets, List<Star> stars, float rotationSpeed = 0, RotationAxis axis = RotationAxis.Z) : base(handle)
        {
            _prop = new Prop(handle);
            _planets = planets;
            _stars = stars;
            _axis = axis;
            RotationSpeed = rotationSpeed;
        }

        public float RotationSpeed { get; set; }

        public void Process(Vector3 galaxyCenter)
        {
            if (_prop == null) return;
            _prop.Position = galaxyCenter;
            var rotation = _prop.Rotation;

            switch (_axis)
            {
                case RotationAxis.Z:
                    rotation.Z += Game.LastFrameTime * RotationSpeed;
                    break;
                case RotationAxis.X:
                    rotation.X += Game.LastFrameTime * RotationSpeed;
                    break;
                case RotationAxis.Y:
                    rotation.Y += Game.LastFrameTime * RotationSpeed;
                    break;
            }
            _prop.Rotation = rotation;
            _planets?.ForEach(p => p?.Orbit());
            _stars?.ForEach(UpdateStar);
        }

        private void UpdateStar(Star star)
        {
            if (!star.IsAttached()) star.AttachTo(_prop, star.Offset);
            star.Update(_prop.Position);
        }

        public void Abort()
        {
            _prop?.Delete();
            while (_stars.Count > 0)
            {
                _stars[0]?.Delete();
                _stars.RemoveAt(0);
            }
            while (_planets.Count > 0)
            {
                _planets[0]?.Delete();
                _planets.RemoveAt(0);
            }
        }

        public string Log()
        {
            var str = string.Empty;
            var pIndex = 0;
            _planets.ForEach(p => str += $"Planet{pIndex}: {p.Position}");
            return str;
        }
    }
}
