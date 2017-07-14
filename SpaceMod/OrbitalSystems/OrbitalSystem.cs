using System.Collections.Generic;
using GTA;
using GTA.Math;
using SpaceMod.Lib;

namespace SpaceMod.OrbitalSystems
{
    public enum SkyboxRotationAxis
    {
        Z,
        X,
        Y
    }

    public class OrbitalSystem : Entity
    {
        private readonly SkyboxRotationAxis rotationAxis;
        
        public OrbitalSystem(Prop prop, List<Orbital> orbitals, List<AttachedOrbital> attachedOrbitals, float rotationSpeed = 0, SkyboxRotationAxis rotationAxis = SkyboxRotationAxis.Z) : base(prop.Handle)
        {
            Orbitals = orbitals;
            AttachedOrbitals = attachedOrbitals;
            SkyboxRotationSpeed = rotationSpeed;

            this.rotationAxis = rotationAxis;
        }

        public float SkyboxRotationSpeed { get; }

        public List<Orbital> Orbitals { get; }

        public List<AttachedOrbital> AttachedOrbitals { get; }

        public void Update()
        {
            // Set our rotation.
            //Rotate();

            // Stay at the galaxy center. Cause' you ain't leavin boi.
            Position = Database.GetGalaxPosition();

            // Update locked orbitals.
            AttachedOrbitals  ?.ForEach(AttachOrbital);

            // Update orbitals.
            Orbitals        ?.ForEach(orbital => orbital?.Rotate());
        }

        private void Rotate()
        {
            Vector3 rotation = Rotation;

            switch (rotationAxis)
            {
                case SkyboxRotationAxis.Z:
                    rotation.Z += Game.LastFrameTime * SkyboxRotationSpeed;
                    break;
                case SkyboxRotationAxis.X:
                    rotation.X += Game.LastFrameTime * SkyboxRotationSpeed;
                    break;
                case SkyboxRotationAxis.Y:
                    rotation.Y += Game.LastFrameTime * SkyboxRotationSpeed;
                    break;
            }

            Rotation = rotation;
        }

        private void AttachOrbital(AttachedOrbital attachedOrbital)
        {
            if (!attachedOrbital.IsAttachedTo(this))
            {
                attachedOrbital.AttachTo(this, attachedOrbital.AttachOffset);
            }
        }

        public new void Delete()
        {
            foreach(AttachedOrbital o in AttachedOrbitals)
            {
                o.Delete();
            }

            foreach (Orbital o in Orbitals)
            {
                o.Delete();
            }

            base.Delete();
        }

        /// <summary>
        /// Returns all planets positions and rotations in the array order 0 to length.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string ret = string.Empty;
            Orbitals.ForEach(o => ret += $"{o.Name}: position = {o.Position} | rotation = {o.Rotation}\n");
            return ret;
        }
    }
}
