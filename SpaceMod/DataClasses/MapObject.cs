#pragma warning disable 1587
//////////////////////////////////////////////////////////
/// 
/// Credits: All credits to Guad for this script.
/// 
/// Edited by Soloman Northrop as of 2/14/2017
/// 
/// Licensed under MIT
/// 
/// ///////////////////////////////////////////////////////
#pragma warning restore 1587

using System.Xml.Serialization;
using GTA.Math;
using GTA.Native;

namespace MapEditor
{
    public class MapObject
    {
        public ObjectTypes Type;

        // Entity
        public Vector3 Position;
        public Vector3 Rotation;
        public int Hash;
        public bool Dynamic;
        public Quaternion Quaternion;

        // Prop
        public bool Door;

        // Ped
        public string Action;
        public string Relationship;
        public WeaponHash? Weapon;

        // Vehicle
        public bool SirensActive;
        public int PrimaryColor;
        public int SecondaryColor;

        [XmlAttribute("Id")]
        public string Id;
    }
    
    public enum ObjectTypes
    {
        Prop,
        Vehicle,
        Ped
    }
}
