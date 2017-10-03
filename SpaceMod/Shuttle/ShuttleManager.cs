using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using GTS.Library;

namespace GTS.Shuttle
{
    public class ShuttleManager
    {
        private const string AstronautModel = "s_m_m_movspace_01";
        private const float ShuttleHeading = 25;
        private const float ShuttleInteractDistance = 60;

        private readonly Vector3 _shuttlePosition = new Vector3(-6412.1f, -1345.5f, 58f);

        private static Ped PlayerPed => Core.PlayerPed;
        public SpaceShuttle Shuttle { get; private set; }

        public void Update()
        {
            if (!Entity.Exists(Shuttle)) return;
            if (PlayerPed.IsInVehicle(new Vehicle(Shuttle.Handle)))
            {
                if (Game.IsControlJustPressed(2, Control.Jump))
                    Shuttle.Launch();
            }
            else
            {
                EnterShuttle();
            }
            if (Shuttle.HeightAboveGround <= Settings.EnterOrbitHeight) return;
            Shuttle.RemoveAttachments();
            Shuttle.HasCollision = true;
            Shuttle = null;
        }

        private void EnterShuttle()
        {
            var dist = PlayerPed.Position.DistanceTo(_shuttlePosition);
            if (dist > ShuttleInteractDistance) return;
            Game.DisableControlThisFrame(2, Control.Enter);
            GtsLibNet.DisplayHelpTextWithGxt("SHUT_ENTER");
            if (!Game.IsDisabledControlJustPressed(2, Control.Enter)) return;
            PlacePlayerInShuttle();
        }

        public void Abort()
        {
            Shuttle?.Delete();
        }

        public void CreateShuttle()
        {
            Shuttle?.RemoveAttachments();
            var m = new Model("shuttle");
            m.Request();
            while (!m.IsLoaded)
                Script.Yield();
            var v = World.CreateVehicle(m, _shuttlePosition, ShuttleHeading);
            v.Rotation = v.Rotation + new Vector3(90, 0, 0); // Rotate the shuttle upwards.
            v.Position = _shuttlePosition;
            v.HasCollision = false;
            v.FreezePosition = true;
            Shuttle = new SpaceShuttle(v.Handle);
            Shuttle.SpawnAttachments();
            var b = Shuttle.AddBlip();
            b.Sprite = BlipSprite.Rockets;
            b.Name = "NASA Shuttle";
        }

        public void PlacePlayerInShuttle()
        {
            var playerPed = PlayerPed;
            var modelName = AstronautModel;
            switch ((PedHash) playerPed.Model.Hash)
            {
                case PedHash.Michael:
                    modelName = "player_zero(spacesuit)";
                    break;
                case PedHash.Franklin:
                    modelName = "player_one(spacesuit)";
                    break;
                case PedHash.Trevor:
                    modelName = "player_two(spacesuit)";
                    break;
                case PedHash.FreemodeMale01:
                    modelName = "mp_m_freemode_01(spacesuit)";
                    break;
                case PedHash.FreemodeFemale01:
                    modelName = "mp_f_freemode_01(spacesuit)";
                    break;
            }
            var m = new Model(modelName);
            m.Request();
            while (!m.IsLoaded)
                Script.Yield();
            var newPlayer = World.CreatePed(m, playerPed.Position);
            var weapons = GetWeapons(playerPed);
            TransferWeapons(weapons, newPlayer);
            Function.Call(Hash.CHANGE_PLAYER_PED, Game.Player, newPlayer, true, true);
            playerPed.Delete();
            playerPed = Game.Player.Character;
            var v = new Vehicle(Shuttle.Handle) {LockStatus = VehicleLockStatus.None};
            playerPed.SetIntoVehicle(v, VehicleSeat.Driver);
            v.LockStatus = VehicleLockStatus.Locked;
        }

        private static void TransferWeapons(IEnumerable<Weapon> weapons, Ped ped)
        {
            foreach (var weapon in weapons)
            {
                var newWeapon = ped.Weapons.Give(weapon.Hash, weapon.Ammo, false, false);
                for (var i = 0; i < weapon.MaxComponents; i++)
                {
                    var component = weapon.GetComponent(i);
                    if (weapon.IsComponentActive(component))
                        newWeapon.SetComponent(component, true);
                }
            }
        }

        private static IEnumerable<Weapon> GetWeapons(Ped ped)
        {
            var weaponHashes = (WeaponHash[]) Enum.GetValues(typeof(WeaponHash));
            return (from weaponHash in weaponHashes
                where ped.Weapons.HasWeapon(weaponHash)
                select ped.Weapons[weaponHash]).ToArray();
        }
    }
}