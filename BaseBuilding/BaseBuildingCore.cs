using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
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
        private const string BaseBuildingFolder = "\\BaseBuilding\\";
        private readonly List<BuildableObject> _buildables = new List<BuildableObject>();
        private readonly List<PlayerResource> _playerResources = new List<PlayerResource>();
        private readonly List<ResourceDefinition> _resourceDefinitions = new List<ResourceDefinition>();
        private readonly List<MinableRock> _rocks = new List<MinableRock>();
        private readonly TimerBarPool _timerPool = new TimerBarPool();
        private readonly UIMenu _inventoryMenu = new UIMenu("Inventory", "Select an Option");
        private WorldPersistenceCache _wordPersistenceCache = new WorldPersistenceCache();
        private readonly MenuPool _menuPool = new MenuPool();

        private DateTime _rockSpawnTimer;
        private int _persistenceId;
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
                InstantiatePersistentObjects();
                PopulateResourceDefinitions();
                PopulateResourceBars();
                CreateObjectsMenu();
                _rockSpawnTimer = DateTime.Now;
            }
            catch (Exception e)
            {
                Debug.Log(e.Message + "\n" + e.StackTrace, DebugMessageType.Error);
            }
        }

        private void InstantiatePersistentObjects()
        {
            _wordPersistenceCache = ReadWorldCache() ?? new WorldPersistenceCache();
            CreatePersistentRocks();
        }

        private void CreatePersistentRocks()
        {
            foreach (var rockPersistenceInfo in _wordPersistenceCache.RockPersistence)
            {
                if (CurrentScene.FileName == rockPersistenceInfo.Scene)
                {
                    CreateRock(rockPersistenceInfo.Resource, rockPersistenceInfo.RockModel, rockPersistenceInfo.Position,
                        rockPersistenceInfo.Rotation.Z, false, rockPersistenceInfo.PersistenceId);
                }
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
                        var resourcesRequired = Resource.GetRemainingResourcesRequired(o, _playerResources) ?? o.ResourcesRequired;

                        var message = "You need: " + Environment.NewLine;
                        resourcesRequired.ForEach(x =>
                        {
                            message += "~r~" + x.Amount + " ~w~" + Resource.GetName(x, _resourceDefinitions);
                        });

                        UI.Notify(message);
                        return;
                    }

                    _playerResources.ForEach(pR =>
                    {
                        o.ResourcesRequired.ForEach(r =>
                        {
                            if (pR.Id == r.Id) pR.Amount -= r.Amount;
                            if (pR.Amount <= 0)
                            {
                                _playerResources.Remove(pR);
                                pR.Dispose(_timerPool);
                                pR = null;
                            }
                        });
                    });

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

            var path = localPath + BaseBuildingFolder + "Resource.xml";
            var obj = XmlSerializer.Deserialize<ResourceDefinitionList>(path);
            return obj;
        }

        private static BuildableObjectsList ReadObjectList()
        {
            var localPath = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            if (string.IsNullOrEmpty(localPath))
                return null;

            var path = localPath + BaseBuildingFolder + "ObjectList.xml";
            var obj = XmlSerializer.Deserialize<BuildableObjectsList>(path);
            return obj;
        }

        private static PlayerResourceList ReadPlayersResources()
        {
            var localPath = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            if (string.IsNullOrEmpty(localPath))
                return null;

            var path = localPath + BaseBuildingFolder + "PlayerResources.xml";
            var obj = XmlSerializer.Deserialize<PlayerResourceList>(path);
            return obj;
        }

        private static WorldPersistenceCache ReadWorldCache()
        {
            var localPath = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            if (string.IsNullOrEmpty(localPath))
                return null;

            var path = localPath + BaseBuildingFolder + "Autosave.savedata";
            var obj = XmlSerializer.Deserialize<WorldPersistenceCache>(path);
            return obj;
        }

        private static void SaveWorldCache(WorldPersistenceCache cache)
        {
            if (cache == null)
                return;

            var localPath = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            if (string.IsNullOrEmpty(localPath))
                return;

            var path = localPath + BaseBuildingFolder + "Autosave.savedata";
            XmlSerializer.Serialize(path, cache);
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
            _menuPool.DisableInstructionalButtons = true;
            _menuPool.ProcessMenus();

            if (!_menuPool.IsAnyMenuOpen() && Game.IsControlJustPressed(2, Control.VehicleHorn))
            {
                _inventoryMenu.Visible = !_inventoryMenu.Visible;
            }
        }

        private void SpawnRocks()
        {
            if (_spawnedRocks)
            {
                if (DateTime.Now > _rockSpawnTimer)
                    _spawnedRocks = false;
                return;
            }

            foreach (var res in _resourceDefinitions)
            {
                if (res.RockInfo.TargetScenes.All(x => x != CurrentScene.FileName))
                    continue;

                foreach (var rockInfoRockModel in res.RockInfo.RockModels)
                {
                    for (var j = 0; j < rockInfoRockModel.MaxPatches; j++)
                    {
                        const float minDist = 50f;
                        const float maxDist = 100f;
                        const float maxDistSqr = maxDist * maxDist;

                        var patchArea = PlayerPed.Position.Around(Perlin.GetNoise() * maxDist + minDist);
                        if (_wordPersistenceCache.RockSpawnAreas.Any(x => x.DistanceToSquared(patchArea) < maxDistSqr * 2))
                            continue;

                        for (var i = 0; i < rockInfoRockModel.MaxRocksPerPatch; i++)
                        {
                            var chance = Perlin.GetNoise() * 100f;
                            if (chance > rockInfoRockModel.SpawnChance) continue;

                            const float minPatchDist = 25f;
                            const float maxPatchDist = 75f;

                            var patchSpawn = patchArea.Around(Perlin.GetNoise() * maxPatchDist + minPatchDist);
                            var ground = GtsLibNet.GetGroundHeightRay(patchSpawn);
                            if (ground != Vector3.Zero) {
                                CreateRock(res, rockInfoRockModel, ground, Perlin.GetNoise() * 360f, true, _persistenceId++);
                            }
                        }

                        _wordPersistenceCache.RockSpawnAreas.Add(patchArea);
                    }
                }
            }

            SaveWorldCache(_wordPersistenceCache);

            _rockSpawnTimer = DateTime.Now + new TimeSpan(0, 0, 0, 60);
            _spawnedRocks = true;
        }

        private void CreateRock(ResourceDefinition resourceDef, RockModelInfo rockModel, Vector3 position,
            float heading, bool persistent, int persistenceId)
        {
            var model = new Model(rockModel.RockModel);
            model.Request();
            while (!model.IsLoaded)
                Script.Yield();

            var prop = World.CreateProp(model,
                position - Vector3.WorldUp * rockModel.ZOffset,
                false, false);
            prop.Heading = heading;
            prop.FreezePosition = true;
            prop.MaxHealth = rockModel.MaxHealth;
            prop.Health = prop.MaxHealth;
            model.MarkAsNoLongerNeeded();

            var rock = new MinableRock(prop.Handle, resourceDef, rockModel, persistenceId);
            rock.AddBlip();
            rock.PickedUpResource += MinableRockOnPickedUpResource;
            _rocks.Add(rock);

            if (persistent)
            {
                _wordPersistenceCache.RockPersistence.Add(new RockPersistenceInfo
                {
                    Position = prop.Position,
                    Rotation = prop.Rotation,
                    Resource = resourceDef,
                    RockModel = rockModel,
                    Scene = CurrentScene.FileName,
                    PersistenceId = rock.PersistenceId
                });
            }
        }

        private void UpdateRocks()
        {
            var impCoords = PlayerPed.GetLastWeaponImpactCoords();
            foreach (var minableRock in _rocks)
            {
                minableRock?.Update(impCoords);
                minableRock?.UpdatePickups();

                // Remove the rock if it's destroyed and contained in the persistence cache.
                RockPersistenceInfo f;
                if (Entity.Exists(minableRock) || 
                    (f = _wordPersistenceCache.RockPersistence.Find(
                            x => minableRock != null && x.PersistenceId == minableRock.PersistenceId)) ==
                    null) continue;

                // We found the rock by it's persistence ID so let's remove it.
                _wordPersistenceCache.RockPersistence.Remove(f);

                // Autosave the cache.
                SaveWorldCache(_wordPersistenceCache);
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
            if(_menuPool.IsAnyMenuOpen())
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
