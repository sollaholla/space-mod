using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Math;
using GTA.Native;
using GTS.Scenes.Interiors;
using GTS.Extensions;
using GTS.Library;

namespace GTS
{
    public class ShuttleManager
    {
        private readonly string _mapLocation = @"./scripts/NasaShuttleDemo/Maps/SS.xml";

        private SpaceShuttle _shuttle;
        private Vehicle _shuttleVehicle;

        private readonly Vector3 _shuttlePosition = new Vector3(-3548.056f, 3429.6123f, 43.4789f);
        private readonly float _shuttleHeading = 95;
        private readonly float _shuttleInteractDistance = 75;
        private readonly string _astronautModel = "s_m_m_movspace_01";
        private readonly float _enterOrbitHeight;

        private Interior _map;
        

        public ShuttleManager(float enterOrbitHeight)
        {
            _enterOrbitHeight = enterOrbitHeight;
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
            {
                Function.Call(Hash._0x6C188BE134E074AA, helpText.Substring(i, Math.Min(maxStringLength, helpText.Length - i)));
            }

            Function.Call(Hash._DISPLAY_HELP_TEXT_FROM_STRING_LABEL, 0, 0, IsHelpMessageBeingDisplayed() ? 0 : 1, -1);
        }
        #endregion

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
                DisplayHelpTextThisFrame("Press ~INPUT_ENTER~ to enter the shuttle.");
                if (!Game.IsDisabledControlJustPressed(2, Control.Enter)) return;
                EnterShuttle();
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

            if (_map != null)
            {
                while (_map.Props.Count > 0)
                {
                    Prop prop = _map.Props[0];
                    prop.Delete();
                    _map.Props.RemoveAt(0);
                }

                while (_map.Peds.Count > 0)
                {
                    Ped ped = _map.Peds[0];
                    ped.Delete();
                    _map.Peds.RemoveAt(0);
                }

                while (_map.Vehicles.Count > 0)
                {
                    Vehicle vehicle = _map.Vehicles[0];
                    vehicle.Delete();
                    _map.Vehicles.RemoveAt(0);
                }

                _map.MapBlip?.Remove();
            }
        }

        public void LoadMap()
        {
            _map = new Interior(_mapLocation, InteriorType.MapEditor, false);

            CreateShuttle();

            //_currentMap.Objects?.ForEach(obj =>
            //{
            //    LoadScaleform scaleform = LoadScaleformDrawer.Instance.Create($"Loading {obj.Hash}...");
            //    scaleform.Draw = true;

            //    Model model = new Model(obj.Hash);
            //    model.Request();
            //    DateTime timout = DateTime.UtcNow + new TimeSpan(0, 0, 0, 5);
            //    while (!model.IsLoaded)
            //    {
            //        Yield();
            //        if (DateTime.UtcNow > timout)
            //            break;
            //    }
            //    if (!model.IsLoaded)
            //    {
            //        UI.Notify("An object ~r~failed~s~ to load properly, even after ~y~5~s~ second request.");
            //        return;
            //    }

            //    switch (obj.Type)
            //    {
            //         TODO: Implement pickups and markers??
            //        case ObjectTypes.Marker:
            //            break;
            //        case ObjectTypes.Pickup:
            //            break;
            //        case ObjectTypes.Ped:
            //            _currentMap.Peds.Add(PropStreamer.CreatePed(obj, model));
            //        break;
            //            case ObjectTypes.Prop:
            //                _currentMap.Props.Add(PropStreamer.CreateProp(obj, model));
            //        break;
            //            case ObjectTypes.Vehicle:
            //                _currentMap.Vehicles.Add(PropStreamer.CreateVehicle(obj, model));
            //        break;
            //    }

            //    scaleform.Draw = false;
            //    LoadScaleformDrawer.Instance.RemoveLoadScaleform(scaleform);
            //});

            _map.Request();

            if (_map.Loaded)
            {
                UI.Notify(
                    $"{_map.Peds.Count} out of {_map.GetMapObjects().Count(o => o.Type == ObjectTypes.Ped)} Peds loaded!",
                    true);
                UI.Notify(
                    $"{_map.Vehicles.Count} out of {_map.GetMapObjects().Count(o => o.Type == ObjectTypes.Vehicle)} Vehicles loaded!",
                    true);
                UI.Notify(
                    $"{_map.Props.Count} out of {_map.GetMapObjects().Count(o => o.Type == ObjectTypes.Prop)} Props loaded!",
                    true);

                var positions = _map.GetMapObjects().Select(x => x.Position).ToArray();
                Vector3 accumulator = Vector3.Zero;

                // Add to the accumulator.
                foreach (var position in positions)
                    accumulator += position;

                // This is basically like calculating the average of regular numbers.
                Vector3 average = accumulator / positions.Length;

                _map.MapBlip = World.CreateBlip(average);
                _map.MapBlip.Sprite = BlipSprite.Hangar;
                _map.MapBlip.Color = BlipColor.Blue;
            }
            UI.Notify("Everything loaded ~g~successfully~s~!");
        }

        public void CreateShuttle()
        {
            if (_shuttle != null) return;
            LoadScaleform loadScaleform = LoadScaleformDrawer.Instance.Create("Loading Shuttle Models...");
            loadScaleform.Draw = true;
            Model model = Utils.RequestModel("shuttle");
            Utils.RequestModel("exttank");
            Utils.RequestModel("srbl");
            Utils.RequestModel("srbr");
            loadScaleform.Draw = false;
            LoadScaleformDrawer.Instance.RemoveLoadScaleform(loadScaleform);
            _shuttleVehicle = World.CreateVehicle(model, _shuttlePosition, _shuttleHeading);
            _shuttleVehicle.HasCollision = false;
            var blip = _shuttleVehicle.AddBlip();
            blip.Sprite = BlipSprite.Plane;
            blip.Color = BlipColor.Blue;
            blip.Name = "NASA Shuttle";
            _shuttle = new SpaceShuttle(_shuttleVehicle.Handle, _shuttlePosition);
            _shuttle.Rotation = _shuttle.Rotation + new Vector3(90, 0, 0); // Rotate the shuttle upwards.
            _shuttle.LodDistance = -1;
        }

        public void EnterShuttle()
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
    }
}
