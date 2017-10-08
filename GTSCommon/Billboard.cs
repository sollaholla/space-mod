using System;
using System.ComponentModel;

namespace GTSCommon
{
    [Serializable]
    public class Billboard : IDrawable
    {
        [RefreshProperties(RefreshProperties.All)]
        public float ParallaxAmount { get; set; } = 0.125f;

        [RefreshProperties(RefreshProperties.All)]
        public float ParallaxStartDistance { get; set; } = 5000f;

        [RefreshProperties(RefreshProperties.All)]
        public string Model { get; set; }

        [RefreshProperties(RefreshProperties.All)]
        public XVector3 Position { get; set; }

        public int LodDistance { get; set; } = -1;

        public override string ToString()
        {
            return string.IsNullOrEmpty(Model) ? base.ToString() : Model;
        }
    }
}