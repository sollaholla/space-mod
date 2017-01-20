using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Math;
using GTA.Native;

namespace SpaceMod.DataClasses
{
    public class Planet : Entity
    {
        private readonly Prop _prop;
        private readonly Vector3 _orbitalVelocity;

        public Planet(int handle, Entity orbitalEntity, Vector3 orbitalVelocity, bool rotate, float rotationSpeed) : base(handle)
        {
            _prop = new Prop(handle);
            OrbitalEntity = orbitalEntity;
            _orbitalVelocity = orbitalVelocity;
            RotationSpeed = rotationSpeed;
        }

        public Entity OrbitalEntity { get; set; }
        public float RotationSpeed { get; set; }

        public void Orbit()
        {
            if (_prop == null) return;
            if (OrbitalEntity == null) return;
            _prop.Position = Utilities.RotatePointAroundPivot(_prop.Position, OrbitalEntity.Position, _orbitalVelocity);
            var rotation = _prop.Rotation;
            rotation.Z += Game.LastFrameTime * RotationSpeed;
            _prop.Rotation = rotation;
        }
    }
}
