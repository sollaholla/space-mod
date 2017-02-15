using System.Collections.Generic;
using GTA;
using GTA.Native;
using SpaceMod.DataClasses.SceneTypes;

namespace SpaceMod.DataClasses.MissionTypes
{
    public class TakeBackWhatsOurs : Mission
    {
        private bool _spawned;
        private List<Ped> _aliens = new List<Ped>();
        private List<Entity> _spaceShips = new List<Entity>();
        private readonly int _originalMaxHealth;

        // Mission flags.
        private bool _mFlag1;   // Show subtitle to go to the moon.
        private bool _mFlag2;   // Show help text when on moon.
 
        public TakeBackWhatsOurs()
        {
            // TODO: Move the game.player.character stuff to a static class
            var character = Game.Player.Character;
            _originalMaxHealth = character.MaxHealth;

            character.Health = character.MaxHealth = 3000;
            character.CanRagdoll = false;
            character.IsExplosionProof = true;
        }
        
        public override void Tick(Ped playerPed, Scene currentScene)
        {
            if (currentScene == null) return;
            if (playerPed.IsDead) return;       // Death is handled from ModController.

            // We're not on the surface of the moon.
            if (currentScene.GetType() != typeof(MoonSurfaceScene))
            {
                Abort();

                if (!_mFlag1) return;
                UI.ShowSubtitle("Go to the ~g~moon~s~!");
                _mFlag1 = true;
                return;
            }

            if (!_spawned)
            {
                DefaultEnemySpawn(playerPed, ref _aliens, ref _spaceShips);
                _spawned = true;
            }

            if (!Utilities.IsHelpMessageBeingDisplayed() && !_mFlag2)
            {
                Utilities.DisplayHelpTextThisFrame(
                    "Your space suit has been heavily equipped, to handle the alien weapon damage.");
                _mFlag2 = true;
            }

            _spaceShips.ForEach(prop =>
            {
                // Handle explosions and death.
                if (!prop.IsDead) return;
                World.AddExplosion(prop.Position, ExplosionType.Barrel, 50, 1.5f, true, true);
                var ptfx = new LoopedPTFX("core", "exp_grd_grenade_lod");
                ptfx.Start(prop.Position, 15);
                prop.Delete();
                ptfx.Unload();
                _spaceShips.Remove(prop);
            });

            _aliens.ForEach(ped =>
            {
                // Handle alien AI.
                var dist = Function.Call<float>(Hash.VDIST, ped.Position.X, ped.Position.Y, ped.Position.Z,
                    playerPed.Position.X, playerPed.Position.Y, playerPed.Position.Z);
                ped.AlwaysKeepTask = false;
                if (!ped.IsInCombatAgainst(playerPed) && dist > 35)
                    ped.Task.RunTo(playerPed.Position, true);

                // Do artificial damage.
                Utilities.ArtificalDamage(ped, playerPed, 2.5f, 50);

                // Handle death.
                if (!ped.IsDead) return;
                ped.CurrentBlip.Remove();
                _aliens.Remove(ped);
                ped.MarkAsNoLongerNeeded();
            });

            // End the mission.
            if (_aliens.Count > 0 || _spaceShips.Count > 0) return;
            End(false);
        }

        public override void Abort()
        {
            while (_aliens.Count > 0)
            {
                var alien = _aliens[0];
                alien?.Delete();
                _aliens.RemoveAt(0);
            }

            while (_spaceShips.Count > 0)
            {
                var ship = _spaceShips[0];
                ship?.Delete();
                _spaceShips.RemoveAt(0);
            }

            ResetPlayer();
        }

        public override void CleanUp()
        {
            ResetPlayer();
        }

        private void ResetPlayer()
        {
            var character = Game.Player.Character;
            character.Health = character.MaxHealth = _originalMaxHealth;
            character.CanRagdoll = true;
            character.IsExplosionProof = false;
        }
    }
}
