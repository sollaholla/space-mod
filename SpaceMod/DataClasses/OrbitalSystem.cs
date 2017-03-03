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
        public static bool ShowUIPositions = true;

        private readonly RotationAxis _rotationAxis;

        /// <summary>
        /// This is an orbital system derived from entity, who's handle is a skybox / prop. Check the <see cref="Database.SpaceDomeModel"/> 
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
            Orbitals = orbitals;
            LockedOrbitals = lockedOrbitals;
            _rotationAxis = rotationAxis;
            SkyboxRotationSpeed = skyboxRotationSpeed;
        }

        public float SkyboxRotationSpeed { get; set; }

        public List<Orbital> Orbitals { get; }

        public List<LockedOrbital> LockedOrbitals { get; }

        public void Process(Vector3 galaxyCenter)
        {
            // Set our rotation.
            SetRotation();

            // Stay at the galaxy center. Cause' you ain't leavin boi.
            Position = galaxyCenter;

            // Update locked orbitals.
            LockedOrbitals?.ForEach(UpdateLockedOrbital);

            // Update orbitals.
            Orbitals?.ForEach(orbital => orbital?.Orbit());
            if (ShowUIPositions)
                Orbitals?.ForEach(orbital => orbital.ShowUIPosition(Orbitals.IndexOf(orbital)));
        }

        private void SetRotation()
        {
            var rotation = Rotation;
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
            Rotation = rotation;
        }

        private void UpdateLockedOrbital(LockedOrbital lockedOrbital)
        {
            if (!lockedOrbital.IsAttached()) lockedOrbital.AttachTo(this, lockedOrbital.Offset);
            lockedOrbital.Update(Position);
        }

        public void Abort()
        {
            Delete();
            while (LockedOrbitals.Count > 0)
            {
                LockedOrbitals[0]?.Delete();
                LockedOrbitals.RemoveAt(0);
            }
            while (Orbitals.Count > 0)
            {
                Orbitals[0]?.Delete();
                Orbitals.RemoveAt(0);
            }
        }

        /// <summary>
        /// Returns all planets positions and rotations in the array order 0 to length.
        /// </summary>
        /// <returns></returns>
        public string GetInfo()
        {
            var str = string.Empty;
            Orbitals.ForEach(p => str += $"{p.Name}: position = {p.Position} | rotation = {p.Rotation}\n");
            return str;
        }
    }
}
