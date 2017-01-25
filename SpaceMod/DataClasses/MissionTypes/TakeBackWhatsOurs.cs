using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;
using SpaceMod.DataClasses.SceneTypes;

namespace SpaceMod.DataClasses.MissionTypes
{
    public class TakeBackWhatsOurs : Mission
    {
        private readonly Random _random = new Random();

        private bool _spawned;
        private bool _showedHelpText;
        private readonly List<Ped> _aliens = new List<Ped>();
        private readonly List<Prop> _spaceShips = new List<Prop>();
        private readonly int _alienRelationship;
        private readonly int _originalMaxHealth;


        public TakeBackWhatsOurs()
        {
            _alienRelationship = World.AddRelationshipGroup("Aliens");
            World.SetRelationshipBetweenGroups(Relationship.Hate, _alienRelationship, Game.GenerateHash("PLAYER"));

            // TODO: Move the game.player.character stuff to a static class
            var character = Game.Player.Character;
            _originalMaxHealth = character.MaxHealth;

            character.Health = character.MaxHealth = 10000;
            character.CanRagdoll = false;
        }

        public override void Tick(Ped playerPed, Scene currentScene)
        {
            if (currentScene == null) return;

            // We're not on the surface of the moon.
            if (currentScene.GetType() != typeof(MoonSurfaceScene)) return;

            if (!_spawned)
            {
                Spawn(playerPed);
                _spawned = true;
            }

            if (!Utilities.IsHelpMessageBeingDisplayed() && !_showedHelpText)
            {
                Utilities.DisplayHelpTextThisFrame(
                    "Your space suit has been heavily equipped, to handle the alien weapon damage.");
                _showedHelpText = true;
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
                if (!ped.IsDead) return;
                ped.CurrentBlip.Remove();
                _aliens.Remove(ped);
                ped.MarkAsNoLongerNeeded();
            });

            if (_aliens.Count <= 0 && _spaceShips.Count <= 0)
            {
                BigMessageThread.MessageInstance.ShowMissionPassedMessage("mission complete");
                End(false);
            }
        }

        private void Spawn(Ped playerPed)
        {
            // spawn 20 enemies.
            for (var i = 0; i < 20; i++)
            {
                var position = playerPed.Position.Around(_random.Next(50, 75));
                position = position.MoveToGroundArtificial();
                var ped = World.CreatePed(PedHash.MovAlien01, position);
                ped.Weapons.Give(WeaponHash.Railgun, 15, true, true);
                ped.IsPersistent = true;
                ped.RelationshipGroup = _alienRelationship;
                ped.Task.FightAgainst(playerPed);
                ped.AlwaysKeepTask = true;
                ped.Voice = "ALIENS";
                ped.Accuracy = 20;
                var blip = ped.AddBlip();
                blip.Name = "Alien Hostile";
                blip.Scale = 0.7f;
                ped.SetDefaultClothes();
                _aliens.Add(ped);
            }

            for (var i = 0; i < 5; i++)
            {
                var position = playerPed.Position.Around(_random.Next(75, 80));
                position = position.MoveToGroundArtificial();

                position = position + new Vector3(0, 0, 15);

                var spaceCraft = World.CreateProp("ufo_zancudo", Vector3.Zero, false, false);
                spaceCraft.IsPersistent = true;
                spaceCraft.FreezePosition = true;
                spaceCraft.Position = position + new Vector3(0, 0, 15);
                spaceCraft.Health = spaceCraft.MaxHealth = 10000;
                var blip = spaceCraft.AddBlip();
                blip.Sprite = BlipSprite.SonicWave;
                blip.Name = "Alien Aircraft";
                _spaceShips.Add(spaceCraft);
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

            while (_spaceShips.Count > 0)
            {
                var ship = _spaceShips[0];
                ship?.Delete();
                _spaceShips.RemoveAt(0);
            }
        }

        public override void CleanUp()
        {
            var character = Game.Player.Character;
            character.Health = character.MaxHealth = _originalMaxHealth;
            character.CanRagdoll = true;
        }
    }
}
