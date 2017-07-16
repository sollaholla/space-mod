using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using GTS.Extensions;
using GTS.Library;

namespace GTS.Scenes.Interiors
{
    public sealed class Interior
    {
        private bool _hidden;
        private Map _map;

        internal Dictionary<string, string> ScenarioDatabase = new Dictionary<string, string>
        {
            {"Drink Coffee", "WORLD_HUMAN_AA_COFFEE"},
            {"Smoke", "WORLD_HUMAN_AA_SMOKE"},
            {"Smoke 2", "WORLD_HUMAN_SMOKING"},
            {"Binoculars", "WORLD_HUMAN_BINOCULARS"},
            {"Bum", "WORLD_HUMAN_BUM_FREEWAY"},
            {"Cheering", "WORLD_HUMAN_CHEERING"},
            {"Clipboard", "WORLD_HUMAN_CLIPBOARD"},
            {"Drilling", "WORLD_HUMAN_CONST_DRILL"},
            {"Drinking", "WORLD_HUMAN_DRINKING"},
            {"Drug Dealer", "WORLD_HUMAN_DRUG_DEALER"},
            {"Drug Dealer Hard", "WORLD_HUMAN_DRUG_DEALER_HARD"},
            {"Traffic Signaling", "WORLD_HUMAN_CAR_PARK_ATTENDANT"},
            {"Filming", "WORLD_HUMAN_MOBILE_FILM_SHOCKING"},
            {"Leaf Blower", "WORLD_HUMAN_GARDENER_LEAF_BLOWER"},
            {"Golf Player", "WORLD_HUMAN_GOLF_PLAYER"},
            {"Guard Patrol", "WORLD_HUMAN_GUARD_PATROL"},
            {"Hammering", "WORLD_HUMAN_HAMMERING"},
            {"Janitor", "WORLD_HUMAN_JANITOR"},
            {"Musician", "WORLD_HUMAN_MUSICIAN"},
            {"Paparazzi", "WORLD_HUMAN_PAPARAZZI"},
            {"Party", "WORLD_HUMAN_PARTYING"},
            {"Picnic", "WORLD_HUMAN_PICNIC"},
            {"Push Ups", "WORLD_HUMAN_PUSH_UPS"},
            {"Shine Torch", "WORLD_HUMAN_SECURITY_SHINE_TORCH"},
            {"Sunbathe", "WORLD_HUMAN_SUNBATHE"},
            {"Sunbathe Back", "WORLD_HUMAN_SUNBATHE_BACK"},
            {"Tourist", "WORLD_HUMAN_TOURIST_MAP"},
            {"Mechanic", "WORLD_HUMAN_VEHICLE_MECHANIC"},
            {"Welding", "WORLD_HUMAN_WELDING"},
            {"Yoga", "WORLD_HUMAN_YOGA"}
        };

        public Interior(string name, InteriorType type = InteriorType.Gta)
        {
            Name = name;

            Vehicles = new List<Vehicle>();
            Props = new List<Prop>();
            Peds = new List<Ped>();

            Type = type;
        }

        public bool IsActive => !string.IsNullOrEmpty(Name) &&
                                (Function.Call<bool>(Hash.IS_IPL_ACTIVE, Name) || _map != null && _map.Objects.Any());

        public List<Vehicle> Vehicles { get; }

        public List<Prop> Props { get; }

        public List<Ped> Peds { get; }

        public string Name { get; }

        public InteriorType Type { get; }

        public void Request()
        {
            if (IsActive) return;
            Debug.Log("Current IPL Type: " + Type);
            switch (Type)
            {
                case InteriorType.Gta:
                    Function.Call(Hash.REQUEST_IPL, Name);
                    const int timeout = 5;
                    var time = DateTime.UtcNow + new TimeSpan(0, 0, 0, timeout);
                    while (!IsActive)
                    {
                        Script.Yield();
                        if (DateTime.UtcNow > time)
                            break;
                    }
                    Debug.Log("Request GTA IPL: " + Name);
                    break;
                case InteriorType.MapEditor:
                    _map = XmlSerializer.Deserialize<Map>(Database.PathToInteriors + "/" + Name + ".xml");
                    if (_map != null && _map != default(Map))
                    {
                        _map.Objects?.ForEach(InstantiateObject);
                        LogMapObjects();
                    }
                    else
                    {
                        Debug.Log($"Failed To Create {Database.PathToInteriors + "/" + Name + ".xml"}",
                            DebugMessageType.Error);
                    }
                    break;
            }
        }

        private void LogMapObjects()
        {
            if (_map?.Objects != null)
                Debug.Log(
                    $"Created {Name} With " +
                    $"{Peds.Count}/{_map.Objects.Count(i => i.Type == ObjectTypes.Ped)} Peds | " +
                    $"{Vehicles.Count}/{_map.Objects.Count(i => i.Type == ObjectTypes.Vehicle)} Vehicles | " +
                    $"{Props.Count}/{_map.Objects.Count(i => i.Type == ObjectTypes.Prop)} Props"
                );
        }

        private void InstantiateObject(MapObject mapObject)
        {
            var model = GetModel(mapObject);
            switch (mapObject.Type)
            {
                case ObjectTypes.Ped:
                    CreatePed(mapObject, model);
                    break;
                case ObjectTypes.Prop:
                    CreateProp(mapObject, model);
                    break;
                case ObjectTypes.Vehicle:
                    CreateVehicle(mapObject, model);
                    break;
            }
        }

        private static Model GetModel(MapObject o)
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

            return model;
        }

        private void CreateProp(MapObject mapObject, Model model)
        {
            var prop = new Prop(Function.Call<int>(Hash.CREATE_OBJECT_NO_OFFSET, model.Hash, mapObject.Position.X,
                mapObject.Position.Y, mapObject.Position.Z, true, true, mapObject.Dynamic));

            if (!Entity.Exists(prop))
                return;

            prop.Rotation = mapObject.Rotation;

            if (!mapObject.Dynamic && !mapObject.Door)
                prop.FreezePosition = true;

            prop.Quaternion = mapObject.Quaternion;

            prop.Position = mapObject.Position;

            prop.LodDistance = 512;

            model.MarkAsNoLongerNeeded();

            Props.Add(prop);
        }

        private void CreateVehicle(MapObject mapObject, Model model)
        {
            var vehicle = World.CreateVehicle(model, mapObject.Position);

            if (!Entity.Exists(vehicle))
                return;

            vehicle.Rotation = mapObject.Rotation;

            vehicle.Quaternion = mapObject.Quaternion;

            vehicle.PrimaryColor = (VehicleColor) mapObject.PrimaryColor;

            vehicle.SecondaryColor = (VehicleColor) mapObject.SecondaryColor;

            vehicle.FreezePosition = !mapObject.Dynamic;

            vehicle.SirenActive = mapObject.SirensActive;

            vehicle.LodDistance = 512;

            model.MarkAsNoLongerNeeded();

            Vehicles.Add(vehicle);
        }

        private void CreatePed(MapObject mapObject, Model model)
        {
            var ped = new Ped(World.CreatePed(model, mapObject.Position - Vector3.WorldUp, mapObject.Rotation.Z)
                .Handle);

            if (!mapObject.Dynamic)
                ped.FreezePosition = true;

            ped.Quaternion = mapObject.Quaternion;

            if (mapObject.Weapon != null)
                ped.Weapons.Give(mapObject.Weapon.Value, 15, true, true);

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

                if (relationship == Relationship.Companion)
                {
                    ped.CanBeTargetted = false;
                    ped.RelationshipGroup = Game.Player.Character.RelationshipGroup;
                }
            }

            ped.BlockPermanentEvents = false;

            ped.LodDistance = 512;

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
                    ped.TaskStartScenarioInPlace(ScenarioDatabase[mapObject.Action]);
                    break;
            }
        }

        public void Remove()
        {
            if (!IsActive)
                return;

            switch (Type)
            {
                case InteriorType.Gta:
                    Function.Call(Hash.REMOVE_IPL, Name);
                    break;
                case InteriorType.MapEditor:
                    RemoveMap();
                    break;
            }
        }

        private void RemoveMap()
        {
            foreach (var p in Props)
                p.Delete();

            foreach (var v in Vehicles)
                if (Game.Player.Character.CurrentVehicle != v)
                    v.Delete();

            foreach (var p in Peds)
                p.Delete();
        }

        public void Hide()
        {
            if (Type != InteriorType.Gta)
                return;
            if (_hidden)
                return;

            Peds.ForEach(p => p.IsVisible = false);
            Vehicles.ForEach(v => v.IsVisible = false);
            Props.ForEach(p => p.IsVisible = false);
            _hidden = true;
        }

        public void Unhide()
        {
            if (Type != InteriorType.Gta)
                return;
            if (!_hidden)
                return;

            Peds.ForEach(p => p.IsVisible = true);
            Vehicles.ForEach(v => v.IsVisible = true);
            Props.ForEach(p => p.IsVisible = true);
            _hidden = false;
        }
    }
}