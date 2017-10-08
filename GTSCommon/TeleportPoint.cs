using System;
using System.ComponentModel;

namespace GTSCommon
{
    [Serializable]
    public class TeleportPoint
    {
        [Description("True if you want this point to have a blip.")]
        [RefreshProperties(RefreshProperties.All)]
        public bool CreateBlip { get; set; } = true;

        [Description("The starting point of the teleport. This will recieve have a minimap blip icon in-game.")]
        [RefreshProperties(RefreshProperties.All)]
        public XVector3 Start { get; set; }

        [Description("True if you want the start point to have an in-game marker.")]
        public bool StartMarker { get; set; } = true;

        [Description("Set the heading of the player after going from 'End' to 'Start'.")]
        public float StartHeading { get; set; }

        [Description("The ending point of the teleport.")]
        [RefreshProperties(RefreshProperties.All)]
        public XVector3 End { get; set; }

        [Description("True if you want the end point to have an in-game marker.")]
        public bool EndMarker { get; set; } = true;

        [Description("Set the heading of the player after going from 'Start' to 'End'.")]
        [RefreshProperties(RefreshProperties.All)]
        public float EndHeading { get; set; }

        public override string ToString()
        {
            return "Start=" + Start + " End=" + End;
        }
    }
}