﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace BaseBuilding
{
    public class PlayerResourceList
    {
        public PlayerResourceList()
        {
            Resources = new List<Resource>();
        }

        [XmlArrayItem("Item")]
        public List<Resource> Resources { get; set; }
    }
}