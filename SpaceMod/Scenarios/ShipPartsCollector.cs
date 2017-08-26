using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using GTS.Extensions;
using GTS.Library;
using GTS.Scenarios;

namespace DefaultMissions
{
    public class ShipPartsCollector : Scenario
    {
        private const int MaxParts = 15;

        public override bool BlockOrbitLanding => false;
        private readonly List<SpaceShipPart> _spaceShipParts = new List<SpaceShipPart>();
        private readonly List<Entity> _entites = new List<Entity>();

        public override void OnAwake() { }

        public override void OnStart()
        {
            for (var i = 0; i < MaxParts; i++)
            {
                var key = "part" + (i + 1);
                if (Settings.GetValue("collected", key, false)) continue;
                var scene = Settings.GetValue(key, "scene", string.Empty);
                if (string.IsNullOrEmpty(scene)) continue;
                var pos = ParseVector3.Read(Settings.GetValue(key, "pos"), Vector3.Zero);
                if (pos == Vector3.Zero) continue;
                var spaceShipPart = new SpaceShipPart(scene, pos, i);
                _spaceShipParts.Add(spaceShipPart);
            }
            CreateParts();
        }

        public override void OnUpdate()
        {
            var partsCopy = _spaceShipParts.ToArray();
            foreach (var part in partsCopy)
            {
                if (!_entites.Contains(part.Entity)) continue;
                if (Entity.Exists(part.Entity))
                {
                    var dist = Vector3.Distance(part.Position, Game.Player.Character.Position);
                    if (dist < 1.5f)
                        part.Entity.Delete();
                    continue;
                }
                ShowCollectMessage();
                Settings.SetValue("collected", "part" + (part.Index + 1), true);
                Settings.Save();
                _entites.Remove(part.Entity);
            }
        }

        private void CreateParts()
        {
            foreach (var spaceShipPart in _spaceShipParts)
            {
                if (CurrentScene.FileName != spaceShipPart.TargetScene) continue;
                var model = new Model("prop_power_cell");
                model.Request();
                while (!model.IsLoaded)
                    Script.Yield();
                spaceShipPart.Entity =
                    World.CreateAmbientPickup(PickupType.CustomScript, spaceShipPart.Position, model, 1);
                spaceShipPart.Entity.IsPersistent = true;
                _entites.Add(spaceShipPart.Entity);
            }
        }

        public override void OnEnded(bool success)
        {
            Cleanup();
        }

        public override void OnAborted()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            foreach (var entity in _entites)
                entity?.Delete();

            _entites.Clear();
            _spaceShipParts.Clear();
        }

        private void ShowCollectMessage()
        {
            Function.Call(Hash.PLAY_MISSION_COMPLETE_AUDIO, "FRANKLIN_BIG_01");
            while (!Function.Call<bool>(Hash.IS_MISSION_COMPLETE_PLAYING))
                Script.Yield();
            ScaleFormMessages.Message.SHOW_MISSION_PASSED_MESSAGE(Game.GetGXTEntry("COL_SHIP_PT"));
            Effects.Start(ScreenEffect.SuccessNeutral, 5000);
        }
    }

    public class SpaceShipPart
    {
        public SpaceShipPart(string targetScene, Vector3 position, int index)
        {
            TargetScene = targetScene;
            Position = position;
            Index = index;
        }

        public Entity Entity { get; set; }
        public string TargetScene { get; set; }
        public Vector3 Position { get; set; }
        public int Index { get; set; }
    }
}
