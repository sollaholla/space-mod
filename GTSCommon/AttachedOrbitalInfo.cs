using System;
using System.ComponentModel;

namespace GTSCommon
{
    [Serializable]
    public class AttachedOrbitalInfo : IDrawable
    {
        [Category("Other")]
        [Description("The starting rotation of the object.")]
        [RefreshProperties(RefreshProperties.All)]
        public XVector3 Rotation { get; set; }

        [Category("Other")]
        [Description(
            "Will cause the attached orbital to slightly shift it's position the further from the 'GalaxyCenter' the player goes.")]
        [RefreshProperties(RefreshProperties.All)]
        public float ShiftAmount { get; set; }

        [Category("Other.Shift")]
        [RefreshProperties(RefreshProperties.All)]
        public bool ShiftX { get; set; }

        [Category("Other.Shift")]
        [RefreshProperties(RefreshProperties.All)]
        public bool ShiftY { get; set; }

        [Category("Other.Shift")]
        [RefreshProperties(RefreshProperties.All)]
        public bool ShiftZ { get; set; }

        [Category("Optional")]
        [Description("Stop this object from moving on the X axis.")]
        public bool FreezeXCoord { get; set; }

        [Category("Optional")]
        [Description("Stop this object from moving on the Y axis.")]
        public bool FreezeYCoord { get; set; }

        [Category("Optional")]
        [Description("Stop this object from moving on the Z axis.")]
        public bool FreezeZCoord { get; set; }

        [Category("Required")]
        [Description("The name of the ydr/ydd model. Example: 'earth_large'")]
        [RefreshProperties(RefreshProperties.All)]
        public string Model { get; set; }

        [Category("Required")]
        [Description("The position of this object offsetted from the center of space.")]
        [RefreshProperties(RefreshProperties.All)]
        public XVector3 Position { get; set; }

        public int LodDistance { get; set; } = -1;

        public override string ToString()
        {
            return string.IsNullOrEmpty(Model) ? base.ToString() : Model;
        }
    }
}