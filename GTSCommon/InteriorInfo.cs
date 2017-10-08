using System;
using System.ComponentModel;

namespace GTSCommon
{
    [Serializable]
    public class InteriorInfo
    {
        public InteriorInfo()
        {
            Type = InteriorType.MapEditor;
        }

        [Description("The name of this interior. If this is a mapeditor file, it should include the .xml extension.")]
        [RefreshProperties(RefreshProperties.All)]
        public string Name { get; set; }

        [Description("The interior type.")]
        [RefreshProperties(RefreshProperties.All)]
        public InteriorType Type { get; set; }
    }
}