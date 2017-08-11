using System;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
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

        private SpaceShuttle _shuttle;
        private Vehicle _shuttleVehicle;


        public ShuttleManager(float enterOrbitHeight)
        {
            _enterOrbitHeight = enterOrbitHeight;
        }

        public void Update()
        {
            if (_shuttleVehicle == null) return;
            if (_shuttle == null) return;

            if (Game.Player.Character.IsInVehicle(_shuttleVehicle))
            {
                _shuttle.Control();
            }
            else
            {
                var dist = _shuttle.Position.DistanceTo(Game.Player.Character.Position);
                if (dist > _shuttleInteractDistance) return;
                Game.DisableControlThisFrame(2, Control.Enter);
                DisplayHelpTextThisFrame(
                    "Press ~INPUT_ENTER~ to enter the shuttle."); // TODO: Replace this with GXT label.
                if (!Game.IsDisabledControlJustPressed(2, Control.Enter)) return;
                PlacePlayerInShuttle();
            }

            if (_shuttle.HeightAboveGround <= _enterOrbitHeight) return;
            _shuttle.CleanUp();
            _shuttle = null;
            _shuttleVehicle.HasCollision = true;
        }

        public void Abort()
        {
            foreach (var shuttlePassenger in _shuttleVehicle.Passengers)
            {
                if (shuttlePassenger.IsPlayer) continue;
                shuttlePassenger.Delete();
            }

            _shuttle?.CleanUp();
            _shuttle?.Delete();

            if (_map == null) return;
            _map.Remove();
            _map.MapBlip?.Remove();
        }

        public void LoadMap()
        {
            _map = new Interior(_mapLocation, InteriorType.MapEditor, false);
            var loadScaleform = LoadScaleformDrawer.Instance.Create("Loading GTS...");
            loadScaleform.Draw = true;
            _map.Request();
            LoadScaleformDrawer.Instance.RemoveLoadScaleform(loadScaleform);

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
            if (_shuttle != null) return;
            var m = new Model("shuttle");
            m.Request(5000);
            _shuttleVehicle = World.CreateVehicle(m, _shuttlePosition, _shuttleHeading);
            _shuttleVehicle.HasCollision = false;
            _shuttle = new SpaceShuttle(_shuttleVehicle.Handle, _shuttlePosition);
            _shuttle.Rotation = _shuttle.Rotation + new Vector3(90, 0, 0); // Rotate the shuttle upwards.
            _shuttle.LodDistance = -1;
        }

        public void PlacePlayerInShuttle()
        {
            var newPlayer = World.CreatePed(_astronautModel, Game.Player.Character.Position);
            var player = Game.Player.Character;
            Function.Call(Hash.CHANGE_PLAYER_PED, Game.Player.Handle, newPlayer.Handle, 1, 1);
            player.Delete();
            Game.Player.Character.Task.WarpIntoVehicle(_shuttleVehicle, VehicleSeat.Driver);
            for (var i = 0; i < _shuttleVehicle.PassengerSeats; i++)
            {
                var ped = World.CreatePed(_astronautModel, Vector3.Zero);
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 3, false);
                ped.SetIntoVehicle(_shuttleVehicle, VehicleSeat.Any);
                ped.RelationshipGroup = newPlayer.RelationshipGroup;
                newPlayer.CurrentPedGroup.Add(ped, false);
                ped.Task.StandStill(-1);
                ped.AlwaysKeepTask = true;
            }
            _shuttleVehicle.LockStatus = VehicleLockStatus.Locked;
            _shuttleVehicle.CurrentBlip?.Remove();
        }

        #region TEMPORARY!

        public static bool IsHelpMessageBeingDisplayed()
        {
            return Function.Call<bool>(Hash.IS_HELP_MESSAGE_BEING_DISPLAYED);
        }

        public static void DisplayHelpTextThisFrame(string helpText)
        {
            Function.Call(Hash._SET_TEXT_COMPONENT_FORMAT, "CELL_EMAIL_BCON");

            const int maxStringLength = 99;

            for (var i = 0; i < helpText.Length; i += maxStringLength)
                Function.Call(Hash._0x6C188BE134E074AA,
                    helpText.Substring(i, Math.Min(maxStringLength, helpText.Length - i)));

            Function.Call(Hash._DISPLAY_HELP_TEXT_FROM_STRING_LABEL, 0, 0, IsHelpMessageBeingDisplayed() ? 0 : 1, -1);
        }

        #endregion
    }
}