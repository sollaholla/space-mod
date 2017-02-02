using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using SpaceMod.DataClasses.SceneTypes;

namespace SpaceMod.DataClasses.MissionTypes
{
    public class TakeBackWhatsOurs : Mission
    {
        private readonly Random _random = new Random();

        private bool _spawned;
        private readonly List<Ped> _aliens = new List<Ped>();
        private readonly List<Prop> _spaceShips = new List<Prop>();
        private readonly int _alienRelationship;
        private readonly int _originalMaxHealth;

        // Mission flags.
        private bool _mFlag1;   // Show subtitle to go to the moon.
        private bool _mFlag2;   // Show help text when on moon.
 
        public TakeBackWhatsOurs()
        {
            _alienRelationship = World.AddRelationshipGroup("Aliens");
            World.SetRelationshipBetweenGroups(Relationship.Hate, _alienRelationship, Game.GenerateHash("PLAYER"));
            World.SetRelationshipBetweenGroups(Relationship.Companion, _alienRelationship, _alienRelationship);

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
                Spawn(playerPed);
                playerPed.Position = playerPed.Position.MoveToGroundArtificial() - playerPed.UpVector;
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
                _spaceShips.Remove(prop);
                World.AddExplosion(prop.Position, ExplosionType.Barrel, 50, 1.5f, true, true);
                Function.Call(Hash.REQUEST_NAMED_PTFX_ASSET, "core");
                Function.Call(Hash._SET_PTFX_ASSET_NEXT_CALL, "core");
                var pos = prop.Position;
                Function.Call(Hash.START_PARTICLE_FX_NON_LOOPED_AT_COORD, "exp_grd_grenade_lod", pos.X, pos.Y, pos.Z, 0, 0, 0, 15.0f, 0, 0, 0);
                Function.Call(Hash.START_PARTICLE_FX_NON_LOOPED_AT_COORD, "exp_air_blimp2", pos.X, pos.Y, pos.Z, 0, 0, 0, 20.0f, 0, 0, 0);
                prop.Delete();
            });

            _aliens.ForEach(ped =>
            {
                // Handle alien AI.
                var dist = Function.Call<float>(Hash.VDIST, ped.Position.X, ped.Position.Y, ped.Position.Z,
                    playerPed.Position.X, playerPed.Position.Y, playerPed.Position.Z);
                ped.AlwaysKeepTask = false;
                if (!ped.IsInCombatAgainst(playerPed) && dist > 35)
                    ped.Task.RunTo(playerPed.Position, true);

                ArtificalDamage(ped, playerPed, 2.5f, 50);

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

        // TODO: Move to static class.
        private static void ArtificalDamage(Ped ped, Ped target, float damageDistance, float damageMultiplier)
        {
            var impCoords = ped.GetLastWeaponImpactCoords();
            if (impCoords == Vector3.Zero) return;
            var distanceTo = impCoords.DistanceTo(target.Position);
            if (distanceTo < damageDistance)
                target.ApplyDamage((int)(1 / distanceTo * damageMultiplier));
        }

        private void Spawn(ISpatial spatial)
        {
            var origin = spatial.Position.Around(100);

            // spawn 20 enemies.
            for (var i = 0; i < 20; i++)
            {
                // Get position.
                var position = origin.Around(_random.Next(50, 75));
                position = position.MoveToGroundArtificial();

                // Create ped.
                var ped = World.CreatePed(PedHash.MovAlien01, position);
                ped.Accuracy = 50;
                ped.Weapons.Give(WeaponHash.Railgun, 15, true, true);
                ped.IsPersistent = true;
                ped.RelationshipGroup = _alienRelationship;
                ped.Voice = "ALIENS";
                ped.Accuracy = 15;
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 46, true);
                Function.Call(Hash.SET_PED_COMBAT_RANGE, ped.Handle, 2);
                ped.IsFireProof = true;

                // Create blip.
                var blip = ped.AddBlip();
                blip.Name = "Alien Hostile";
                blip.Scale = 0.7f;
                ped.SetDefaultClothes();
                _aliens.Add(ped);
            }

            // Spawn spaceships.
            for (var i = 0; i < 5; i++)
            {
                // Move the spaceship to a spawn position.
                var position = origin.Around(_random.Next(50, 80));
                position = position.MoveToGroundArtificial() + new Vector3(0, 0, 15);

                // Skip this loop if the position is too close to another one.
                if (IsCloseToAnyEntity(position, _spaceShips, 25))
                continue;

                // Create he spacecraft.
                var spaceCraft = World.CreateProp("ufo_zancudo", Vector3.Zero, false, false);
                spaceCraft.IsPersistent = true;
                spaceCraft.FreezePosition = true;
                spaceCraft.Position = position;
                spaceCraft.Health = spaceCraft.MaxHealth = 4500;
                spaceCraft.IsFireProof = true;      // NOTE: There's no fire in a space vaccum.

                // Configure the blip.
                var blip = spaceCraft.AddBlip();
                blip.Sprite = BlipSprite.SonicWave;
                blip.Color = BlipColor.Green;
                blip.Name = "Alien Aircraft";
                _spaceShips.Add(spaceCraft);
            }
        }

        // TODO: Move to utils. This could be useful.
        private static bool IsCloseToAnyEntity(Vector3 position, IReadOnlyCollection<Entity> collection, float distance)
        {
            if (collection == null) return false;
            if (collection.Count <= 0) return false;

            return
                collection.Where(entity1 => entity1 != null)
                    .Any(entity1 => entity1.Position.DistanceTo(position) < distance);
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
