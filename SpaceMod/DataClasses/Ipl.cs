using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using GTA;
using GTA.Math;
using GTA.Native;
using MapEditor;

namespace SpaceMod.DataClasses
{
    public enum IplType
    {
        GTA,
        MapEditor
    }

    public class Ipl
    {
        internal Dictionary<string, string> ScrenarioDatabase = new Dictionary<string, string> {
            {"Drink Coffee",  "WORLD_HUMAN_AA_COFFEE"},
            {"Smoke", "WORLD_HUMAN_AA_SMOKE" },
            {"Smoke 2", "WORLD_HUMAN_SMOKING" },
            {"Binoculars",  "WORLD_HUMAN_BINOCULARS"},
            {"Bum", "WORLD_HUMAN_BUM_FREEWAY" },
            {"Cheering", "WORLD_HUMAN_CHEERING" },
            {"Clipboard", "WORLD_HUMAN_CLIPBOARD" },
            {"Drilling",  "WORLD_HUMAN_CONST_DRILL"},
            {"Drinking", "WORLD_HUMAN_DRINKING" },
            {"Drug Dealer", "WORLD_HUMAN_DRUG_DEALER"},
            {"Drug Dealer Hard", "WORLD_HUMAN_DRUG_DEALER_HARD" },
            {"Traffic Signaling",  "WORLD_HUMAN_CAR_PARK_ATTENDANT"},
            {"Filming", "WORLD_HUMAN_MOBILE_FILM_SHOCKING" },
            {"Leaf Blower", "WORLD_HUMAN_GARDENER_LEAF_BLOWER" },
            {"Golf Player", "WORLD_HUMAN_GOLF_PLAYER" },
            {"Guard Patrol", "WORLD_HUMAN_GUARD_PATROL" },
            {"Hammering", "WORLD_HUMAN_HAMMERING" },
            {"Janitor", "WORLD_HUMAN_JANITOR" },
            {"Musician", "WORLD_HUMAN_MUSICIAN" },
            {"Paparazzi", "WORLD_HUMAN_PAPARAZZI" },
            {"Party", "WORLD_HUMAN_PARTYING" },
            {"Picnic", "WORLD_HUMAN_PICNIC" },
            {"Push Ups", "WORLD_HUMAN_PUSH_UPS"},
            {"Shine Torch", "WORLD_HUMAN_SECURITY_SHINE_TORCH" },
            {"Sunbathe", "WORLD_HUMAN_SUNBATHE" },
            {"Sunbathe Back", "WORLD_HUMAN_SUNBATHE_BACK"},
            {"Tourist", "WORLD_HUMAN_TOURIST_MAP" },
            {"Mechanic", "WORLD_HUMAN_VEHICLE_MECHANIC" },
            {"Welding", "WORLD_HUMAN_WELDING" },
            {"Yoga", "WORLD_HUMAN_YOGA" },
        };

        private const string Path = "./scripts/SpaceMod/IPL/";

        private readonly IplType _type;
        private Map _map;

        public Ipl(string name, IplType type = IplType.GTA)
        {
            Name = name;
            _type = type;

            Vehicles = new List<Vehicle>();
            Props = new List<Prop>();
            Peds = new List<Ped>();
        }

        public bool IsActive => !string.IsNullOrEmpty(Name) && Function.Call<bool>(Hash.IS_IPL_ACTIVE, Name);
        public List<Vehicle> Vehicles { get; }
        public List<Prop> Props { get; }
        public List<Ped> Peds { get; }
        public string Name { get; }

        public void Request()
        {
            switch (_type)
            {
                case IplType.GTA:
                    if (IsActive) return;
                    Function.Call(Hash.REQUEST_IPL, Name);
                    const int timeout = 5;
                    var time = DateTime.UtcNow + new TimeSpan(0, 0, 0, timeout);
                    while (!IsActive)
                    {
                        Script.Yield();
                        if (DateTime.UtcNow > time)
                            break;
                    }
                    break;
                case IplType.MapEditor:
                    // TODO: Spawn map-objects from map editor file.
                    _map = DeserializeMap();
                    _map?.Objects?.ForEach(o =>
                    {
                        switch (o.Type)
                        {
                            case ObjectTypes.Ped:
                                CreatePed(o);
                                break;
                            case ObjectTypes.Prop:
                                CreateProp(o);
                                break;
                            case ObjectTypes.Vehicle:
                                CreateVehicle(o);
                                break;
                        }
                    });
                    break;
            }
        }

        private void CreateProp(MapObject mapObject)
        {
            var prop = Utilities.CreatePropNoOffset(mapObject.Hash, mapObject.Position, mapObject.Dynamic && mapObject.Door);
            if (prop == null) return;
            prop.FreezePosition = !mapObject.Dynamic;
            prop.Rotation = mapObject.Rotation;
            prop.Quaternion = mapObject.Quaternion;
            Props.Add(prop);
        }

        private void CreateVehicle(MapObject mapObject)
        {
            var vehicle = World.CreateVehicle(mapObject.Hash, mapObject.Position);
            if (vehicle == null) return;
            vehicle.Rotation = mapObject.Rotation;
            vehicle.Quaternion = mapObject.Quaternion;
            vehicle.PrimaryColor = (VehicleColor) mapObject.PrimaryColor;
            vehicle.SecondaryColor = (VehicleColor) mapObject.SecondaryColor;
            vehicle.FreezePosition = !mapObject.Dynamic;
            vehicle.SirenActive = mapObject.SirensActive;
            Vehicles.Add(vehicle);
        }

        private void CreatePed(MapObject mapObject)
        {
            var ped = World.CreatePed(mapObject.Hash, mapObject.Position - new Vector3(0, 0, 1));
            if (ped == null) return;
            ped.Rotation = mapObject.Rotation;
            ped.Quaternion = mapObject.Quaternion;
            SetScenario(mapObject, ped);
            if (mapObject.Weapon != null)
                ped.Weapons.Give(mapObject.Weapon.Value, 15, true, true);

            Relationship relationship;
            if (Enum.TryParse(mapObject.Relationship, out relationship))
                World.SetRelationshipBetweenGroups(relationship, ped.RelationshipGroup,
                    Game.Player.Character.RelationshipGroup);

            ped.FreezePosition = !mapObject.Dynamic;
            Peds?.Add(ped);
        }

        private void SetScenario(MapObject mapObject, Ped ped)
        {
            if (mapObject.Action == null || mapObject.Action == "None") return;

            switch (mapObject.Action)
            {
                case "Any - Warp":
                    ped.TaskUseNearestScenarioToCoordWarp(100f, -1);
                    break;
                case "Any - Walk":
                case "Any":
                    ped.TaskUseNearestScenarioToCoord(100f, -1);
                    break;
                case "Wander":
                    ped.Task.WanderAround();
                    break;
                default:
                    ped.Task.StartScenario(ScrenarioDatabase[mapObject.Action], ped.Position);
                    break;
            }
        }

        public void Remove()
        {
            if (!IsActive) return;

            switch (_type)
            {
                case IplType.GTA:
                    Function.Call(Hash.REMOVE_IPL, Name);
                    break;
                case IplType.MapEditor:
                    RemoveMap_INTERNAL();
                    break;
            }
        }

        private void RemoveMap_INTERNAL()
        {
            while (Props.Count > 0)
            {
                var prop = Props[0];
                prop?.Delete();
                Props.RemoveAt(0);
            }

            while (Vehicles.Count > 0)
            {
                var vehicle = Vehicles[0];
                vehicle?.Delete();
                Vehicles.RemoveAt(0);
            }

            while (Peds.Count > 0)
            {
                var ped = Peds[0];
                ped?.Delete();
                Peds.RemoveAt(0);
            }
        }

        private Map DeserializeMap()
        {
            try
            {
                var reader = new XmlSerializer(typeof(Map));
                var file = new StreamReader(Path + Name + ".xml");
                var map = (Map)reader.Deserialize(file);
                file.Close();
                return map;
            }
            catch (Exception e)
            {
                const string path = "./scripts/SpaceMod.log";
                var text = File.Exists(path) ? File.ReadAllText(path) : string.Empty;
                File.WriteAllText(path, $"{text}\n[{DateTime.Now}] {e.Message}\n{e.StackTrace}");
                throw;
            }
        }
    }
}
