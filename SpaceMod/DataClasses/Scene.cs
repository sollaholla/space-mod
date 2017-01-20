using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Math;

namespace SpaceMod.DataClasses
{
    public abstract class Scene
    {
        public delegate void OnSceneEndedEvent(Scene sender, Scene newScene);

        public event OnSceneEndedEvent SceneEnded;

        public Ped PlayerPed => Game.Player.Character;

        public Vector3 PlayerPosition
        {
            get { return PlayerPed.Position; }
            set { PlayerPed.Position = value; }
        }
        
        public abstract void Init();
        public abstract void Update();
        public abstract void Abort();
        public abstract void CleanUp();

        protected void End(Scene newScene)
        {
            SceneEnded?.Invoke(this, newScene);
        }

        protected void ResetPlayerOrigin()
        {
            if (!PlayerPed.IsInVehicle()) PlayerPed.Position = Vector3.Zero;
            else PlayerPed.CurrentVehicle.Position = Vector3.Zero;
        }

        protected void TeleportPlayerToGalaxy()
        {
            if (!PlayerPed.IsInVehicle()) PlayerPosition = Constants.GalaxyCenter;
            else PlayerPed.CurrentVehicle.Position = Constants.GalaxyCenter;
        }

        protected void RotatePlayer(Vector3 rotation)
        {
            if (!PlayerPed.IsInVehicle()) PlayerPed.Rotation = rotation;
            else PlayerPed.CurrentVehicle.Rotation = rotation;
        }
    }
}
