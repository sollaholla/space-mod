using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using GTA;
using GTA.Math;
using GTA.Native;
using MapEditor;
using SpaceMod.Static;

namespace SpaceMod.DataClasses
{
    public enum IplType
    {
        GTA,
        MapEditor
    }

    public class Ipl
    {
        public static bool AllowTraversal { get; set; }

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

        public bool IsActive => !string.IsNullOrEmpty(Name) && Function.Call<bool>(Hash.IS_IPL_ACTIVE, Name) || _map != null && _map.Objects.Any();

        public List<Vehicle> Vehicles { get; }

        public List<Prop> Props { get; }

        public List<Ped> Peds { get; }

        public string Name { get; }

        public void Request()
        {
            if (IsActive) return;
            switch (_type)
            {
                case IplType.GTA:
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
                    _map = MyXmlSerializer.Deserialize<Map>(Database.PathToInteriors + "/" + Name + ".xml");
                    if (_map != null)
                    {
                        if (_map.Objects != null)
                        {
                            _map.Objects.ForEach(o =>
                            {
                                var model = new Model(o.Hash);
                                model.Request();
                                var timeout2 = DateTime.UtcNow + new TimeSpan(0, 0, 0, 0, 1500);
                                while (!model.IsLoaded)
                                {
                                    Script.Yield();
                                    if (DateTime.UtcNow > timeout2)
                                        break;
                                }

                                switch (o.Type)
                                {
                                    case ObjectTypes.Ped:
                                        CreatePed(o, model);
                                        break;
                                    case ObjectTypes.Prop:
                                        CreateProp(o, model);
                                        break;
                                    case ObjectTypes.Vehicle:
                                        CreateVehicle(o, model);
                                        break;
                                }
                            });
                            DebugLogger.Log($"{_map.Objects.Count} Objects Tried To Be Created", MessageType.Debug);
                        }

                        DebugLogger.Log($"{Name} Created Successfully", MessageType.Debug);
                    }
                    else
                    {
                        DebugLogger.Log($"Failed To Create {Database.PathToInteriors + "/" + Name + ".xml"}", MessageType.Error);
                    }

                    break;
            }
        }

        private void CreateProp(MapObject mapObject, Model model)
        {
            var prop = Utilities.CreatePropNoOffset(model, mapObject.Position, mapObject.Dynamic && mapObject.Door);
            if (prop == null) return;
            prop.FreezePosition = !mapObject.Dynamic;
            prop.Rotation = mapObject.Rotation;
            prop.Quaternion = mapObject.Quaternion;
            Props.Add(prop);
        }

        private void CreateVehicle(MapObject mapObject, Model model)
        {
            var vehicle = World.CreateVehicle(model, mapObject.Position);
            if (vehicle == null) return;
            vehicle.Rotation = mapObject.Rotation;
            vehicle.Quaternion = mapObject.Quaternion;
            vehicle.PrimaryColor = (VehicleColor) mapObject.PrimaryColor;
            vehicle.SecondaryColor = (VehicleColor) mapObject.SecondaryColor;
            vehicle.FreezePosition = !mapObject.Dynamic;
            vehicle.SirenActive = mapObject.SirensActive;
            Vehicles.Add(vehicle);
        }

        private void CreatePed(MapObject mapObject, Model model)
        {
            var ped = World.CreatePed(model, mapObject.Position - new Vector3(0, 0, 1), mapObject.Rotation.Z);
            if (ped == null) return;
            if (!mapObject.Dynamic)
                ped.FreezePosition = true;
            ped.Quaternion = mapObject.Quaternion;
            if (mapObject.Weapon != null)
                ped.Weapons.Give(mapObject.Weapon.Value, 15, true, true);
            ped.SetDefaultClothes();
            SetScenario(mapObject, ped);
            Relationship relationship;
            if (Enum.TryParse(mapObject.Relationship, out relationship))
            {
                if (relationship == Relationship.Hate)
                    ped.RelationshipGroup = Game.GenerateHash("HATES_PLAYER");
                World.SetRelationshipBetweenGroups(relationship, ped.RelationshipGroup,
                    Game.Player.Character.RelationshipGroup);
                World.SetRelationshipBetweenGroups(relationship, Game.Player.Character.RelationshipGroup,
                    ped.RelationshipGroup);
            }
            ped.BlockPermanentEvents = false;
            model.MarkAsNoLongerNeeded();
            Peds?.Add(ped);
        }

        private void SetScenario(MapObject mapObject, Ped ped)
        {
            switch (mapObject.Action)
            {
                case null:
                case "None":
                    return;
                case "Any":
                case "Any - Walk":
                    ped.TaskUseNearestScenarioToCoord(100f, -1);
                    break;
                case "Any - Warp":
                    ped.TaskUseNearestScenarioToCoordWarp(100f, -1);
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
            while (_map.Objects.Count > 0)
            {
                _map.Objects.RemoveAt(0);
            }

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
    }
}
