using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GTA;
using GTA.Math;
using GTA.Native;
using GTS.Library;
using GTS.Scenes;
using GTS.Utility;
using NativeUI;

namespace BaseBuilding
{
    public class BaseBuildingCore : Scenario
    {
        private readonly List<BuildableObject> _buildables = new List<BuildableObject>();
        private readonly List<PlayerResource> _playerResources = new List<PlayerResource>();
        private readonly List<ResourceDefinition> _resourceDefinitions = new List<ResourceDefinition>();
        private readonly List<MinableRock> _rocks = new List<MinableRock>();
        private readonly TimerBarPool _timerPool = new TimerBarPool();
        private readonly UIMenu _inventoryMenu = new UIMenu("Inventory", "Select an Option");
        private readonly MenuPool _menuPool = new MenuPool();
        private readonly Random _rand = new Random();

        private bool _spawnedRocks;

        public BaseBuildingCore()
        {
            _menuPool.Add(_inventoryMenu);
        }

        public override bool TargetAllScenes => true;

        public void Start()
        {
            try
            {
                PopulateResourceDefinitions();
                PopulateResourceBars();
                CreateObjectsMenu();
            }
            catch (Exception e)
            {
                Debug.Log(e.Message + "\n" + e.StackTrace, DebugMessageType.Error);
            }
        }

        private void PopulateResourceDefinitions()
        {
            var resourceDefinitions = ReadResourceDefinitions();
            if (resourceDefinitions == null) return;

            foreach (var r in resourceDefinitions.Definitions)
            {
                _resourceDefinitions.Add(r);
            }
        }

        private void PopulateResourceBars()
        {
            var playerResourceList = ReadPlayersResources();
            if (playerResourceList == null) return;

            foreach (var r in playerResourceList.Resources)
            {
                var playerResource = PlayerResource.GetPlayerResource(r, _resourceDefinitions, _timerPool);
                _playerResources.Add(playerResource);
            }
        }

        private void CreateObjectsMenu()
        {
            var l = ReadObjectList();
            var subMenu = _menuPool.AddSubMenu(_inventoryMenu, "Base Building");
            if (l == null) return;
            foreach (var o in l.ObjectDefs)
            {
                var menuItem = new UIMenuItem(o.FriendlyName, $"Click to place '{o.FriendlyName}'");

                menuItem.Activated += (sender, item) =>
                {
                    if (!(o.ResourcesRequired.TrueForAll(x => Resource.DoesHaveResource(x, _playerResources))))
                    {
                        var resourcesRequired = Resource.GetRemainingResourcesRequired(o, _playerResources);
                        if (resourcesRequired == null) return;

                        var message = "You need: " + Environment.NewLine;
                        resourcesRequired.ForEach(x =>
                        {
                            message += "~r~" + x.Amount + " ~w~" + Resource.GetName(x, _resourceDefinitions) + Environment.NewLine;
                        });

                        UI.Notify(message);
                        return;
                    } 

                    Game.DisableControlThisFrame(2, Control.Attack);
                    var b = BuildableObject.PlaceBuildable(o.ModelName, _buildables);
                    if (b == null) return;
                    _buildables.Add(b);
                };
                subMenu.AddItem(menuItem);
            }
            _menuPool.RefreshIndex();
        }

        private static ResourceDefinitionList ReadResourceDefinitions()
        {
            var localPath = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            if (string.IsNullOrEmpty(localPath))
                return null;

            var path = localPath + "\\BaseBuilding\\" + "Resource.xml";
            var obj = XmlSerializer.Deserialize<ResourceDefinitionList>(path);
            return obj;
        }

        private static BuildableObjectsList ReadObjectList()
        {
            var localPath = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            if (string.IsNullOrEmpty(localPath))
                return null;

            var path = localPath + "\\BaseBuilding\\" + "ObjectList.xml";
            var obj = XmlSerializer.Deserialize<BuildableObjectsList>(path);
            return obj;
        }

        private static PlayerResourceList ReadPlayersResources()
        {
            var localPath = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            if (string.IsNullOrEmpty(localPath))
                return null;

            var path = localPath + "\\BaseBuilding\\" + "PlayerResources.xml";
            var obj = XmlSerializer.Deserialize<PlayerResourceList>(path);
            return obj;
        }

        public void Update()
        {
            try
            {
                UpdateMenu();
                UpdateTimerBars();
                SpawnRocks();
                UpdateRocks();
            }
            catch (Exception e)
            {
                Debug.Log(e.Message + "\n" + e.StackTrace, DebugMessageType.Error);
            }
        }

        private void UpdateMenu()
        {
            _menuPool.ProcessMenus();

            if (!_menuPool.IsAnyMenuOpen() && Game.IsControlJustPressed(2, Control.VehicleHorn))
            {
                _inventoryMenu.Visible = !_inventoryMenu.Visible;
            }
        }

        private void SpawnRocks()
        {
            if (_spawnedRocks)
                return;
            
            foreach (var res in _resourceDefinitions)
            {
                if (res.RockInfo.TargetScenes.All(x => x != CurrentScene.FileName))
                    continue;

                foreach (var rockInfoRockModel in res.RockInfo.RockModels)
                {
                    for (var j = 0; j < rockInfoRockModel.MaxPatches; j++)
                    {
                        const float minDist = 50f;
                        const float dist = 5f;
                        var patchArea = PlayerPed.Position.Around((Perlin.GetNoise() + minDist) * dist);

                        for (var i = 0; i < rockInfoRockModel.MaxRocksPerPatch; i++)
                        {
                            var chance = Perlin.GetNoise() * 100f;
                            if (chance > rockInfoRockModel.SpawnChance)
                                continue;

                            const float minPatchDist = 10f;
                            var patchSpawn = patchArea.Around((Perlin.GetNoise() + minPatchDist) * 1.5f);
                            var ground = GtsLibNet.GetGroundHeightRay(patchSpawn);
                            if (ground == Vector3.Zero) continue;

                            var model = new Model(rockInfoRockModel.RockModel);
                            model.Request();
                            while (!model.IsLoaded)
                                Script.Yield();
                            var prop = World.CreateProp(model, ground - Vector3.WorldUp * rockInfoRockModel.ZOffset, false, false);
                            prop.Heading = Perlin.GetNoise() * 360f;
                            prop.FreezePosition = true;
                            prop.MaxHealth = rockInfoRockModel.MaxHealth;
                            prop.Health = prop.MaxHealth;
                            model.MarkAsNoLongerNeeded();
                            var rock = new MinableRock(prop.Handle, res, rockInfoRockModel);
                            rock.AddBlip();
                            rock.PickedUpResource += MinableRockOnPickedUpResource;
                            _rocks.Add(rock);
                            Script.Yield();
                        }
                    }
                }
            }

            _spawnedRocks = true;
        }

        private void UpdateRocks()
        {
            var impCoords = PlayerPed.GetLastWeaponImpactCoords();
            foreach (var minableRock in _rocks)
            {
                minableRock?.Update(impCoords);
                minableRock?.UpdatePickups();
            }
        }

        private void MinableRockOnPickedUpResource(object sender, PickupEventArgs pickupEventArgs)
        {
            if (pickupEventArgs == null) return;
            var def = pickupEventArgs.ResourceDefinition;
            var am = pickupEventArgs.Amount;
            GivePlayerResource(def, am);
        }

        private void GivePlayerResource(ResourceDefinition def, int am)
        {
            var res = new Resource { Amount = am, Id = def.Id };
            var find = _playerResources.Find(x => x.Id == res.Id);
            if (find != null) find.Amount += res.Amount;
            else _playerResources.Add(PlayerResource.GetPlayerResource(res, _resourceDefinitions, _timerPool));
        }

        private void UpdateTimerBars()
        {
            _timerPool.Draw();
        }

        public void OnAborted()
        {
            CleanUp();
        }

        public void OnDisable(bool success)
        {
            CleanUp();
        }

        private void CleanUp()
        {
            foreach (var buildableObject in _buildables)
            {
                buildableObject?.Delete();
            }

            foreach (var minableRock in _rocks)
            {
                minableRock?.Delete();
            }
        }
    }
}
