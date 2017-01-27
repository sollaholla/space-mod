using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Math;
using GTA.Native;

namespace SpaceMod.DataClasses
{
    public class LockedOrbital : Entity
    {
        private readonly Prop _prop;

        public LockedOrbital(int handle, Vector3 offset) : base(handle)
        {
            _prop = new Prop(handle);
            Offset = offset;
        }

        public Vector3 Offset { get; set; }

        public void Update(Vector3 galaxyCenter)
        {
            var rotation = Rotation;
            rotation.Z += Game.LastFrameTime * 50;
            Rotation = rotation;
        }

        public new Vector3 Position
        {
            get { return _prop.Position; }
            set { _prop.Position = value; }
        }

        public new Vector3 Rotation
        {
            get { return _prop.Rotation; }
            set { _prop.Rotation = value; }
        }
    }
}
