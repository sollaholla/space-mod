using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTS
{
    public class PlayerInfo
    {
        [XmlArrayItem("Item")]
        public List<PlayerInfoItem> Players { get; set; }
    }

    public class PlayerInfoItem
    {
        public string Model { get; set; }
        public bool BreathWithoutO2 { get; set; }
    }
}