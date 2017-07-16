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

using System;
using System.Xml.Serialization;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GTS.Scenes.Interiors
{
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

    public class DynamicPickup
    {
        private bool _dynamic = true;

        public DynamicPickup(int handle)
        {
            PickupHandle = handle;
            IsInRange = handle != -1;
        }

        public bool IsInRange { get; set; }

        public bool Dynamic
        {
            get => _dynamic;
            set
            {
                _dynamic = value;
                new Prop(ObjectHandle).FreezePosition = !value;
            }
        }

        public string PickupName => "";

        public int Uid { get; set; }
        public int PickupHash { get; set; }
        public int PickupHandle { get; set; }
        public int Amount { get; set; }
        public int Flag { get; set; }

        public bool PickedUp { get; set; }
        public DateTime LastPickup { get; set; }

        public int ObjectHandle => Function.Call<int>((Hash) 0x5099BC55630B25AE, PickupHandle);

        public Vector3 RealPosition { get; set; }

        public Vector3 Position
        {
            get => new Prop(ObjectHandle).Position;
            set
            {
                new Prop(ObjectHandle).Position = value;
                RealPosition = value;
            }
        }

        public int Timeout { get; set; } = -1;

        public bool PickupObjectExists => Function.Call<bool>(Hash.DOES_PICKUP_OBJECT_EXIST, PickupHandle);

        public bool PickupExists => Function.Call<bool>(Hash.DOES_PICKUP_EXIST, PickupHandle);

        public void UpdatePos()
        {
            RealPosition = Position;
        }

        public void SetPickupHash(int newHash)
        {
            PickupHash = newHash;
            ReloadPickup();
        }

        public void SetPickupFlag(int newFlag)
        {
            Flag = newFlag;
            ReloadPickup();
        }

        public void SetAmount(int newAmount)
        {
            Amount = newAmount;
            ReloadPickup();
        }

        private void ReloadPickup()
        {
            var newPickup = Function.Call<int>(Hash.CREATE_PICKUP_ROTATE, PickupHash, RealPosition.X, RealPosition.Y,
                RealPosition.Z, 0, 0, 0, Flag, Amount, 0, false, 0);

            var tmpDyn = new DynamicPickup(newPickup);

            var start = 0;
            while (tmpDyn.ObjectHandle == -1 && start < 20)
            {
                start++;
                Script.Yield();
                tmpDyn.Position = RealPosition;
            }

            tmpDyn.Dynamic = Dynamic;
            tmpDyn.Timeout = Timeout;
            new Prop(tmpDyn.ObjectHandle).IsPersistent = true;

            if (PickupHandle != -1)
                Remove();


            PickupHandle = newPickup;
        }

        public void Remove()
        {
            Function.Call(Hash.REMOVE_PICKUP, PickupHandle);
        }

        public void Update()
        {
            var inRange = Game.Player.Character.IsInRangeOf(RealPosition, 20f);

            if (inRange && PickupHandle == -1)
                ReloadPickup();

            if (PickupHandle == -1) return;
            Position = RealPosition;

            if (!inRange) return;

            if (!PickupObjectExists && !PickedUp)
            {
                PickedUp = true;
                LastPickup = DateTime.Now;
                Remove();
            }

            if (!PickedUp || !(DateTime.Now.Subtract(LastPickup).TotalSeconds > Timeout) || Timeout == 0) return;
            PickedUp = false;
            ReloadPickup();
        }
    }

    public enum ObjectTypes
    {
        Prop,
        Vehicle,
        Ped,
        Marker,
        Pickup
    }
}