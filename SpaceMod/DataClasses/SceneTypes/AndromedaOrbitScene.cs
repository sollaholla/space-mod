using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Native;
using GTA.Math;

namespace SpaceMod.DataClasses.SceneTypes
{
    public class AndromedaOrbitScene : Scene
    {
        public static Vector3[] Positions => new[]
        {
            new Vector3(-6870.744f, -12107.31f, 8620.764f), /*Andromeda*/
        };

        private Prop _andromedaPlanet;

        private OrbitalSystem _planetSystem;

        public override void Init()
        {
            // Creating the props.
            var blueSun = World.CreateProp(Database.BlueSunSmallModel, Vector3.Zero, false, false);
            var galaxy = World.CreateProp(Database.SpaceDomeAndromedaMode, Vector3.Zero, false, false);
            _andromedaPlanet = World.CreateProp(Database.AlienPlanet1LargeModel, Vector3.Zero, false, false);

            // Setup our lists.
            var orbitals = new List<Orbital>
            {
                new Orbital(_andromedaPlanet.Handle, "Andromeda", PlayerPed, Vector3.Zero, -3.5f) /*Andromeda*/,
            };
            var lockedOrbitals = new List<LockedOrbital>
            {
                new LockedOrbital(blueSun.Handle, Database.SunOffsetNearEarth) /*BLEWWWWWWWW SUN*/
            };

            // Do I have to explain myself here ;-;
            MovePlayerToGalaxy();

            // Set the position for the planet and sun.
            _andromedaPlanet.Position = Positions[0];
            blueSun.Position = Database.GalaxyCenter;

            // Set up the planetary system.
            _planetSystem = new OrbitalSystem(galaxy, orbitals, lockedOrbitals, -1.5f);
        }

        public override void Update()
        {
            _planetSystem.Process(Database.GetValidGalaxyDomePosition(PlayerPed));

            //You cant get out of here muahahahahaha.
        }

        public override void Abort()
        {
            
        }

        public override void CleanUp()
        {
            
        }

        public void Done()
        {
            _planetSystem?.Abort();
            Game.TimeScale = 1; // Do we have to this here? I mean it will be ok but its not needed
        }                       // As in somehow the user really does find a way to glitch this mod up.
    }                           // Well Rockstar didnt know in GTA SA so I doubt we will :P
}
