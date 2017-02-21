using GTA;
using GTA.Math;

namespace SpaceMod.DataClasses
{
    public enum SceneStartDirection
    {
        ToTarget,
        FromTarget,
        None
    }

    public abstract class Scene
    {
        public delegate void OnSceneEndedEvent(Scene sender, Scene newScene);

        public event OnSceneEndedEvent SceneEnded;

        public SceneStartDirection StartDirection { get; set; }

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

        protected void End(Scene newScene, SceneStartDirection startDirection = SceneStartDirection.None)
        {
            if (newScene != null) newScene.StartDirection = startDirection;
            SceneEnded?.Invoke(this, newScene);
        }

        protected void ResetPlayerOrigin()
        {
            if (!PlayerPed.IsInVehicle()) PlayerPed.Position = Vector3.Zero;
            else PlayerPed.CurrentVehicle.Position = Vector3.Zero;
        }

        protected void MovePlayerToGalaxy(bool surface = false)
        {
            var position = surface ? Database.PlanetSurfaceGalaxyCenter : Database.GalaxyCenter;
            if (!PlayerPed.IsInVehicle()) PlayerPosition = position;
            else PlayerPed.CurrentVehicle.Position = position;
        }

        protected void RotatePlayer(Vector3 rotation)
        {
            if (!PlayerPed.IsInVehicle()) PlayerPed.Rotation = rotation;
            else PlayerPed.CurrentVehicle.Rotation = rotation;
        }

        /// <summary>
        /// Set's the start direction based on <see cref="SceneStartDirection"/>.
        /// </summary>
        /// <param name="target">The target planet or object's position we are going to use to determine our direction.</param>
        /// <param name="ourSpatial">Our spatial whos rotation will be set. (i.e. the player, the ship, etc.)</param>
        /// <param name="direction">The direction in which we want to face.</param>
        public void SetStartDirection(Vector3 target, ISpatial ourSpatial, SceneStartDirection direction)
        {
            var directionToTarget = target - ourSpatial.Position;

            switch (direction)
            {
                    case SceneStartDirection.FromTarget:
                    ourSpatial.Rotation = new Vector3(0, 0, -directionToTarget.ToHeading());
                    break;
                    case SceneStartDirection.ToTarget:
                    ourSpatial.Rotation = new Vector3(0, 0, directionToTarget.ToHeading());
                    break;
            }
        }
    }
}
