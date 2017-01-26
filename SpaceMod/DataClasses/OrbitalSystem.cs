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

    public class OrbitalSystem : Entity
    {
        private readonly Prop _prop;
        private readonly List<Orbital> _orbitals;
        private readonly List<LockedOrbital> _lockedOrbitals;
        private readonly RotationAxis _rotationAxis;

        /// <summary>
        /// This is an orbital system derived from entity, who's handle is a skybox / prop. Check the <see cref="Constants.SpaceDomeModel"/> 
        /// for an example of a dome / skybox.
        /// </summary>
        /// <param name="handle">The handle of the dome / prop that's the skybox.</param>
        /// <param name="orbitals">The orbital entity's</param>
        /// <param name="lockedOrbitals">These entities will be locked to the orbital.</param>
        /// <param name="skyboxRotationSpeed">The rotation speed of the skybox.</param>
        /// <param name="rotationAxis">The axis we wish to rotate the skybox on.</param>
        public OrbitalSystem(int handle, List<Orbital> orbitals, List<LockedOrbital> lockedOrbitals,
            float skyboxRotationSpeed = 0, RotationAxis rotationAxis = RotationAxis.Z) : base(handle)
        {
            _prop = new Prop(handle);
            _orbitals = orbitals;
            this._lockedOrbitals = lockedOrbitals;
            _rotationAxis = rotationAxis;
            SkyboxRotationSpeed = skyboxRotationSpeed;
        }

        public float SkyboxRotationSpeed { get; set; }

        public void Process(Vector3 galaxyCenter)
        {
            if (_prop == null) return;
            _prop.Position = galaxyCenter;
            var rotation = _prop.Rotation;

            switch (_rotationAxis)
            {
                case RotationAxis.Z:
                    rotation.Z += Game.LastFrameTime * SkyboxRotationSpeed;
                    break;
                case RotationAxis.X:
                    rotation.X += Game.LastFrameTime * SkyboxRotationSpeed;
                    break;
                case RotationAxis.Y:
                    rotation.Y += Game.LastFrameTime * SkyboxRotationSpeed;
                    break;
            }
            _prop.Rotation = rotation;
            _orbitals?.ForEach(p => p?.Orbit());
            _lockedOrbitals?.ForEach(UpdateStar);
        }

        private void UpdateStar(LockedOrbital star)
        {
            if (!star.IsAttached()) star.AttachTo(_prop, star.Offset);
            star.Update(_prop.Position);
        }

        public void Abort()
        {
            _prop?.Delete();
            while (_lockedOrbitals.Count > 0)
            {
                _lockedOrbitals[0]?.Delete();
                _lockedOrbitals.RemoveAt(0);
            }
            while (_orbitals.Count > 0)
            {
                _orbitals[0]?.Delete();
                _orbitals.RemoveAt(0);
            }
        }

        public string Log()
        {
            var str = string.Empty;
            var pIndex = 0;
            _orbitals.ForEach(p => str += $"Planet{pIndex}: {p.Position}");
            return str;
        }
    }
}
