using GTA;
using GTA.Math;
using System.Drawing;
using Font = GTA.Font;

namespace SpaceMod.DataClasses
{
    public class Orbital : Entity
    {
        private readonly Vector3 _orbitalVelocity;

        private readonly UIText _nameText = new UIText(string.Empty, new Point(), 0.5f)
        {
            Centered = true,
            Font = Font.Monospace,
            Shadow = true
        };

        private readonly UIText _distanceText = new UIText(string.Empty, new Point(), 0.5f)
        {
            Centered = true,
            Font = Font.Monospace,
            Shadow = true
        };

        public Orbital(int handle, string name, Entity orbitalEntity, Vector3 orbitalVelocity, float rotationSpeed,
            bool showUIByDefault = true) : base(handle)
        {
            _orbitalVelocity = orbitalVelocity;

            Name = name;
            OrbitalEntity = orbitalEntity;
            RotationSpeed = rotationSpeed;
            ShowUIByDefault = showUIByDefault;
        }

        public string Name { get; set; }
        public Entity OrbitalEntity { get; set; }
        public float RotationSpeed { get; set; }
        public bool ShowUIByDefault { get; set; }

        public void Orbit()
        {
            if (OrbitalEntity == null) return;
            Position = Utilities.RotatePointAroundPivot(Position, OrbitalEntity.Position, _orbitalVelocity);
            var rotation = Rotation;
            rotation.Z += Game.LastFrameTime * RotationSpeed;
            Rotation = rotation;
        }

        public void ShowUIPosition(int index)
        {
            if (!ShowUIByDefault) return;
            Utilities.ShowUIPosition(this, index, Position, Constants.PathToSprites, Name, _nameText, _distanceText);
        }
    }
}
