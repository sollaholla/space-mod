using System.Xml.Serialization;
using GTA.Math;
using GTA.Native;

namespace GTS.Scenes.Interiors
{
    /// <summary>
    ///     All credits to Guad for this script.
    /// </summary>
    public class MapObject
    {
        // Ped stuff
        public string Action;

        // Pickup stuff
        public int Amount;

        // Prop stuff
        public bool Door;

        public bool Dynamic;
        public int Flag;
        public int Hash;

        [XmlAttribute("Id")] public string Id;

        public Vector3 Position;
        public int PrimaryColor;
        public Quaternion Quaternion;
        public string Relationship;
        public int RespawnTimer;
        public Vector3 Rotation;
        public int SecondaryColor;

        // Vehicle stuff
        public bool SirensActive;

        public ObjectTypes Type;
        public WeaponHash? Weapon;

        // XML Stuff
        public bool ShouldSerializeDoor()
        {
            return Type == ObjectTypes.Prop;
        }

        public bool ShouldSerializeAction()
        {
            return Type == ObjectTypes.Ped;
        }

        public bool ShouldSerializeRelationship()
        {
            return Type == ObjectTypes.Ped;
        }

        public bool ShouldSerializeWeapon()
        {
            return Type == ObjectTypes.Ped;
        }

        public bool ShouldSerializeSirensActive()
        {
            return Type == ObjectTypes.Vehicle;
        }

        public bool ShouldSerializePrimaryColor()
        {
            return Type == ObjectTypes.Vehicle;
        }

        public bool ShouldSerializeSecondaryColor()
        {
            return Type == ObjectTypes.Vehicle;
        }

        public bool ShouldSerializeAmount()
        {
            return Type == ObjectTypes.Pickup;
        }

        public bool ShouldSerializeRespawnTimer()
        {
            return Type == ObjectTypes.Pickup;
        }

        public bool ShouldSerializeFlag()
        {
            return Type == ObjectTypes.Pickup;
        }
    }
}