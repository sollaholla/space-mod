using System;
using System.ComponentModel;

namespace GTSCommon
{
    [Serializable]
    public class NextSceneInfo
    {
        [Category("Next Scene Info")]
        [Description("The position of the player, offsetted from the center of space, when the next scene loads.")]
        [RefreshProperties(RefreshProperties.All)]
        public virtual XVector3 NextScenePosition { get; set; }

        [Category("Next Scene Info")]
        [Description("The rotation of the player when the next scene loads.")]
        [RefreshProperties(RefreshProperties.All)]
        public virtual XVector3 NextSceneRotation { get; set; }

        [Category("Next Scene Info")]
        [Description("The filename of the next scene that will load.")]
        [RefreshProperties(RefreshProperties.All)]
        public virtual string NextScene { get; set; }
    }
}