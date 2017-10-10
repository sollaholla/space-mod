using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BaseBuilder;
using GTA;
using GTS.Scenes;
using GTS.Utility;
using NativeUI;

namespace BaseBuilding
{
    public class BaseBuildCore : Scenario
    {
        private readonly List<BuildableObject> _buildables = new List<BuildableObject>();
        private readonly UIMenu _inventoryMenu = new UIMenu("Inventory", "Select an Option");
        private readonly MenuPool _menuPool = new MenuPool();

        public BaseBuildCore()
        {
            _menuPool.Add(_inventoryMenu);
        }

        public override bool TargetAllScenes => true;

        public void Start()
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

        private static BuildableObjectsList ReadObjectList()
        {
            var localPath = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            if (string.IsNullOrEmpty(localPath))
                return null;

            var path = localPath + "\\BaseBuilding\\" + "ObjectList.xml";
            var obj = XmlSerializer.Deserialize<BuildableObjectsList>(path);
            return obj;
        }

        public void Update()
        {
            _menuPool.ProcessMenus();

            if (!_menuPool.IsAnyMenuOpen() && Game.IsControlJustPressed(2, Control.SpecialAbilitySecondary))
            {
                _inventoryMenu.Visible = !_inventoryMenu.Visible;
            }
        }
    }
}
