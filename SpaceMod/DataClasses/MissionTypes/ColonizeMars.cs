using System.Collections.Generic;
using GTA;
using SpaceMod.DataClasses.SceneTypes;

namespace SpaceMod.DataClasses.MissionTypes
{
    public class ColonizeMars : Mission
    {
        private bool _notify1;
        private bool _spawned;

        private MarsSurfaceScene _marsSurfaceScene;

        private List<Ped> _aliens = new List<Ped>();
        private List<Entity> _spaceCrafts = new List<Entity>();

        private bool _addedInteriorPeds;

        public override void Tick(Ped playerPed, Scene currentScene)
        {
            if (currentScene.GetType() != typeof(MarsSurfaceScene))
            {
                if (!_notify1)
                {
                    UI.ShowSubtitle("Travel to ~o~Mars~s~.");
                    _notify1 = true;
                }

                Abort();
                return;
            }

            _marsSurfaceScene = currentScene as MarsSurfaceScene;
            _marsSurfaceScene?.SetIpl("mbi2_hostile");
            if (_marsSurfaceScene != null && _marsSurfaceScene.IsIplLoaded)
            {
                if (!_addedInteriorPeds)
                {
                    var collection = _marsSurfaceScene.GetIpl().Peds.ToArray();
                    UI.Notify($"Added {collection.Length} peds.");
                    _aliens.AddRange(collection);
                    _addedInteriorPeds = true;
                }
            }

            if (!_spawned)
            {
                DefaultEnemySpawn(playerPed, ref _aliens, ref _spaceCrafts);
                UI.ShowSubtitle("Eliminate the remaining ~r~hostiles~s~ outside and inside of the ~p~base~s~.", 10000);
                _spawned = true;
            }
            
            _spaceCrafts?.ForEach(prop =>
            {
                // Handle explosions and death.
                if (!prop.IsDead) return;
                World.AddExplosion(prop.Position, ExplosionType.Barrel, 50, 1.5f, true, true);
                var ptfx = new LoopedPTFX("core", "exp_grd_grenade_lod");
                ptfx.Start(prop.Position, 15);
                prop.Delete();
                ptfx.Unload();
                _spaceCrafts.Remove(prop);
            });

            _aliens?.ForEach(alien =>
            {
                if (!alien.IsDead) return;
                alien.MarkAsNoLongerNeeded();
                alien.CurrentBlip?.Remove();
                _aliens.Remove(alien);
            });

            if (_aliens?.Count > 0 && _spaceCrafts?.Count > 0 && !_addedInteriorPeds)
            {
                Ipl.AllowTraversal = false;
            }
            else if (_aliens?.Count <= 0 && _spaceCrafts?.Count <= 0 && !_addedInteriorPeds)
            {
                Ipl.AllowTraversal = true;
            }
            else if (_addedInteriorPeds && _aliens?.Count <= 0)
            {
                End(false);
            }
            else
            {
                Ipl.AllowTraversal = false;
            }
        }

        public override void Abort()
        {
            while (_aliens.Count > 0)
            {
                var alien = _aliens[0];
                alien?.Delete();
                _aliens.RemoveAt(0);
            }

            while (_spaceCrafts.Count > 0)
            {
                var ship = _spaceCrafts[0];
                ship?.Delete();
                _spaceCrafts.RemoveAt(0);
            }
        }

        public override void CleanUp()
        {
            Ipl.AllowTraversal = true;
            _marsSurfaceScene?.LeaveBase();
            _marsSurfaceScene?.SetIpl("mbi2");
        }
    }
}
