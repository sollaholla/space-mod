using GTA;
using GTA.Math;
using GTA.Native;
using System.Drawing;
using System.IO;

namespace SpaceMod.DataClasses
{
    public class Orbital : Entity
    {
        private readonly Prop _prop;
        private readonly string _name;
        private readonly Vector3 _orbitalVelocity;
        private const string PATH = @".\scripts\SpaceMod";

        public Orbital(int handle, string name, Entity orbitalEntity, Vector3 orbitalVelocity, float rotationSpeed) : base(handle)
        {
            _prop = new Prop(handle);
            _name = name;
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

            if(!Function.Call<bool>(Hash.IS_ENTITY_OCCLUDED, _prop))
            {
                Point posToDraw = UI.WorldToScreen(_prop.Position);

                string pathFile = Path.Combine(PATH, _name+"Reticle.png");

                if (File.Exists(pathFile))
                    UI.DrawTexture(pathFile, 0, 1, 60, posToDraw, new Size(50, 10));
            }
        }
    }
}
