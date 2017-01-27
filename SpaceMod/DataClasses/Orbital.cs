using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Math;
using GTA.Native;
using System.Drawing;

namespace SpaceMod.DataClasses
{
    public class Orbital : Entity
    {
        private readonly Prop _prop;
        private readonly string _name;
        private readonly Vector3 _orbitalVelocity;
        UIText recticle;

        public Orbital(int handle, string name, Entity orbitalEntity, Vector3 orbitalVelocity, float rotationSpeed) : base(handle)
        {
            _prop = new Prop(handle);
            _name = name;
            OrbitalEntity = orbitalEntity;
            _orbitalVelocity = orbitalVelocity;
            RotationSpeed = rotationSpeed;

            recticle = new UIText(name, Point.Empty, 1f);
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

            if(Function.Call<bool>(Hash.IS_ENTITY_ON_SCREEN, _prop))
            {
                Point posToDraw = UI.WorldToScreen(_prop.Position);
                recticle.Caption = _name;
                recticle.Color = Color.White;
                recticle.Position = posToDraw;
                recticle.Draw();
            }
        }
    }
}
