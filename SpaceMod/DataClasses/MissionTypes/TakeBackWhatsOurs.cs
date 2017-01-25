using System;
using System.Collections.Generic;
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

        public TakeBackWhatsOurs()
        {
            _alienRelationship = World.AddRelationshipGroup("Aliens");
            World.SetRelationshipBetweenGroups(Relationship.Hate, _alienRelationship, Game.GenerateHash("PLAYER"));
        }

        public override void Tick(Ped playerPed, Scene currentScene)
        {
            if (currentScene == null) return;
            // We're not on the surface of the moon.
            if (currentScene.GetType() != typeof(MoonSurfaceScene)) return;

            if (Game.IsScreenFadedOut) return;
            if (Game.IsScreenFadedIn) return;
            if (Game.IsLoading) return;

            if (!_spawned)
            {
                Spawn(playerPed);
                _spawned = true;
            }
        }

        private void Spawn(ISpatial origin)
        {
            // spawn 20 enemies.
            for (var i = 0; i < 20; i++)
            {
                var position = origin.Position.Around(_random.Next(50, 75));
                position = position.MoveToGroundArtificial();
                var ped = World.CreatePed(PedHash.MovAlien01, position);
                ped.Weapons.Give(WeaponHash.Railgun, 15, true, true);
                ped.IsPersistent = true;
                ped.RelationshipGroup = _alienRelationship;
                var blip = ped.AddBlip();
                blip.Name = "Alien Hostile";
                _aliens.Add(ped);
            }

            for (var i = 0; i < 5; i++)
            {
                var position = origin.Position.Around(_random.Next(75, 80));
                position = position.MoveToGroundArtificial();

                position = position + new Vector3(0, 0, 15);

                var spaceCraft = World.CreateProp("ufo_zancudo", Vector3.Zero, false, false);
                spaceCraft.IsPersistent = true;
                spaceCraft.FreezePosition = true;
                spaceCraft.Position = position;
                spaceCraft.Health = spaceCraft.MaxHealth = 5000;
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

        }
    }
}
