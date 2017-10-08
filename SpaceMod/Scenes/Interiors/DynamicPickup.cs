using System;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GTS.Scenes.Interiors
{
    /// <summary>
    /// All credits to Guad for this script.
    /// </summary>
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
}