using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using GTS.Library;
using GTS.Scenes;
using GTS.Scenes.Interiors;

namespace GTS.Shuttle
{
    public class ShuttleManager
    {
        // TODO: Convert some of these to settings.
        private readonly string _astronautModel = "s_m_m_movspace_01";

        private readonly float _enterOrbitHeight;
        private readonly string _mapLocation = Database.PathToInteriors + "\\LaunchStation.xml";
        private readonly float _shuttleHeading = 95;
        private readonly float _shuttleInteractDistance = 75;

        private readonly Vector3 _shuttlePosition = new Vector3(-3548.056f, 3429.6123f, 43.4789f);

        private Interior _map;
        private Vehicle _shuttleVehicle;

        public ShuttleManager(float enterOrbitHeight)
        {
            _enterOrbitHeight = enterOrbitHeight;
        }

        public SpaceShuttle Shuttle { get; private set; }

        public void Update()
        {
            if (_shuttleVehicle == null) return;
            if (Shuttle == null) return;

            if (Game.Player.Character.IsInVehicle(_shuttleVehicle))
            {
                Shuttle.Control();
            }
            else
            {
                var dist = Game.Player.Character.Position.DistanceTo(_shuttlePosition);
                if (dist > _shuttleInteractDistance) return;
                Game.DisableControlThisFrame(2, Control.Enter);
                GtsLibNet.DisplayHelpTextWithGxt("SHUT_ENTER");
                if (!Game.IsDisabledControlJustPressed(2, Control.Enter)) return;
                PlacePlayerInShuttle();
            }

            if (Shuttle.HeightAboveGround <= _enterOrbitHeight) return;
            Shuttle.CleanUp();
            Shuttle = null;
            _shuttleVehicle.HasCollision = true;
        }

        public void Abort()
        {
            Shuttle?.CleanUp();
            Shuttle?.Delete();
            _map?.Remove();
            _map?.MapBlip?.Remove();
        }

        public void LoadMap()
        {
            if (!File.Exists(_mapLocation)) return;
            _map = new Interior(_mapLocation, InteriorType.MapEditor, false);
            _map.Request();
            if (!_map.Loaded) return;
            var positions = _map.GetMapObjects().Select(x => x.Position).ToArray();
            var accumulator = positions.Aggregate(Vector3.Zero, (current, position) => current + position);

            // This is basically like calculating the average of regular numbers.
            var average = accumulator / positions.Length;
            _map.MapBlip = World.CreateBlip(average);
            _map.MapBlip.Sprite = BlipSprite.Hangar;
            _map.MapBlip.Color = Scene.MarkerBlipColor;
            _map.MapBlip.Name = "Shuttle Launch Site";
        }

        public void CreateShuttle()
        {
            if (Shuttle != null) return;
            var m = new Model("shuttle");
            m.Request();
            while (!m.IsLoaded)
                Script.Yield();
            _shuttleVehicle = World.CreateVehicle(m, _shuttlePosition, _shuttleHeading);
            _shuttleVehicle.Rotation = _shuttleVehicle.Rotation + new Vector3(90, 0, 0); // Rotate the shuttle upwards.
            _shuttleVehicle.HasCollision = false;
            _shuttleVehicle.FreezePosition = true;
            Shuttle = new SpaceShuttle(_shuttleVehicle.Handle, _shuttlePosition);
        }

        public void PlacePlayerInShuttle()
        {
            var playerPed = Game.Player.Character;

            // Get the model for the player.
            var modelName = _astronautModel;
            if (playerPed.Model == PedHash.Michael)
                modelName = "player_zero(spacesuit)";
            else if (playerPed.Model == PedHash.Franklin)
                modelName = "player_one(spacesuit)";
            else if (playerPed.Model == PedHash.Trevor)
                modelName = "player_two(spacesuit)";
            else if (playerPed.Model == PedHash.FreemodeMale01)
                modelName = "mp_m_freemode_01(spacesuit)";
            else if (playerPed.Model == PedHash.FreemodeFemale01)
                modelName = "mp_f_freemode_01(spacesuit)";

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
            _shuttleVehicle.LockStatus = VehicleLockStatus.None;
            playerPed.SetIntoVehicle(_shuttleVehicle, VehicleSeat.Driver);
            _shuttleVehicle.LockStatus = VehicleLockStatus.Locked;
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