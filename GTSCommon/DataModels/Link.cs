using System;
using System.ComponentModel;

namespace GTSCommon.DataModels
{
    [Serializable]
    public class Link : NextSceneInfo, ITrigger
    {
        public Link()
        {
            TriggerDistance = 1500;
        }

        [Category("Required")]
        [Description("This is the name that will be displayed on screen by the custom UI.")]
        [RefreshProperties(RefreshProperties.All)]
        public string Name { get; set; }

        [Category("Required")]
        [Description("The position of this trigger offsetted from the center of space.")]
        [RefreshProperties(RefreshProperties.All)]
        public XVector3 Position { get; set; }

        [Category("Next Scene Info")]
        [Description("This is the distance that will trigger the next scene to load.")]
        [RefreshProperties(RefreshProperties.All)]
        public float TriggerDistance { get; set; }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Name) ? base.ToString() : Name;
        }
    }
}