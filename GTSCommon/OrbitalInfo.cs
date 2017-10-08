using System;
using System.ComponentModel;

namespace GTSCommon
{
    [Serializable]
    public class OrbitalInfo : IDrawable
    {
        [Description("This is the name that will be displayed on screen by the custom UI.")]
        [RefreshProperties(RefreshProperties.All)]
        public string Name { get; set; }

        [Description("The rotation speed (degrees per-second).")]
        [RefreshProperties(RefreshProperties.All)]
        public float RotationSpeed { get; set; }

        [Description("True if you wish for this object to act like a wormhole, and suck the player in.")]
        [RefreshProperties(RefreshProperties.All)]
        public bool WormHole { get; set; }

        [Category("Other")]
        [Description("The starting rotation of the object.")]
        [RefreshProperties(RefreshProperties.All)]
        public XVector3 Rotation { get; set; }

        [Category("Other")]
        [Description("The filename of the next scene that will load.")]
        [RefreshProperties(RefreshProperties.All)]
        public string NextScene { get; set; }

        [Category("Next Scene Info")]
        [Description("The trigger to the next scene will default to the size of the model bounds. Use this" +
                     "to manipulate that distance. (Setting this to 2 will make the planet trigger 2 times the size" +
                     "of the planet)")]
        [RefreshProperties(RefreshProperties.All)]
        public float TriggerSizeMultiplier { get; set; } = 1.15f;

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
            return string.IsNullOrEmpty(Name) ? string.IsNullOrEmpty(Model) ? base.ToString() : Model : Name;
        }
    }
}