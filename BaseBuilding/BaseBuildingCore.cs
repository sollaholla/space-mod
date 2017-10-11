using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using GTA;
using GTS.Scenes;
using GTS.Utility;
using NativeUI;

namespace BaseBuilding
{
    public class BaseBuildingCore : Scenario
    {
        private readonly List<BuildableObject> _buildables = new List<BuildableObject>();
        private readonly List<PlayerResource> _playersResources = new List<PlayerResource>();
        private readonly List<ResourceDefinition> _resourceDefinitions = new List<ResourceDefinition>();
        private readonly UIMenu _inventoryMenu = new UIMenu("Inventory", "Select an Option");
        private readonly MenuPool _menuPool = new MenuPool();

        public static readonly TimerBarPool TimerPool = new TimerBarPool();

        public BaseBuildingCore()
        {
            _menuPool.Add(_inventoryMenu);
        }

        public override bool TargetAllScenes => true;

        public void Start()
        {
            PopulateResourceDefinitions();
            PopulateResourceBars();
            CreateObjectsMenu();
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
                var playerResource = PlayerResource.GetPlayerResource(_resourceDefinitions, r);
                _playersResources.Add(playerResource);
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
            UpdateMenu();
            UpdateTimerBars();
            SpawnRocks();
        }

        private void UpdateMenu()
        {
            _menuPool.ProcessMenus();

            if (!_menuPool.IsAnyMenuOpen() && Game.IsControlJustPressed(2, Control.VehicleHorn))
            {
                _inventoryMenu.Visible = !_inventoryMenu.Visible;
            }
        }

        private static void SpawnRocks()
        {
        }

        private static void UpdateTimerBars()
        {
            TimerPool.Draw();
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
        }
    }
}
