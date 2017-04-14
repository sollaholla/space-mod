using System.Drawing;
using GTA;
using GTA.Math;
using SpaceMod.Extensions;
using Font = GTA.Font;

namespace SpaceMod.OrbitalSystems
{
    public class Orbital : LockedOrbital
    {
        private readonly Vector3 _orbitalVelocity;

        private readonly UIText _nameText = new UIText(string.Empty, new Point(), 0.5f)
        {
            Centered = true,
            Font = Font.Monospace,
            Shadow = true
        };

        public Orbital(int handle, string name, Entity orbitalEntity, Vector3 orbitalVelocity, float rotationSpeed, bool emitLight,
				float scale, bool showUiByDefault = true) : base(handle, Vector3.Zero, emitLight, scale)
        {
            _orbitalVelocity = orbitalVelocity;

            Name = name;
            OrbitalEntity = orbitalEntity;
            RotationSpeed = rotationSpeed;
            ShowUiByDefault = showUiByDefault;
        }

        public string Name { get; set; }

        public Entity OrbitalEntity { get; set; }

        public float RotationSpeed { get; set; }

        public bool ShowUiByDefault { get; set; }

        public bool IsWormHole { get; set; }
        
        public void Orbit()
        {
            if (OrbitalEntity != null)
            {
                Position = SpaceModLib.RotatePointAroundPivot(Position, OrbitalEntity.Position, _orbitalVelocity);
            }

            var rotation = Rotation;
            rotation.Z += Game.LastFrameTime * RotationSpeed;
            Rotation = rotation;
        }

        public void ShowUiPosition(int index)
        {
            if (string.IsNullOrEmpty(Name)) return;
            if (!ShowUiByDefault) return;
            SpaceModLib.ShowUIPosition(this, index, Position, SpaceModDatabase.PathToSprites, Name, _nameText);
        }
    }
}
