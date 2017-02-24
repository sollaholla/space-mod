using System;
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

        private bool _setTimer;
        private DateTime _timer;

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

        public void EnterWormHole(Scene scene, ISpatial wormHole, Camera camera, SceneStartDirection dir = SceneStartDirection.ToTarget)
        {
            var distanceToWormHole = PlayerPosition.DistanceTo(wormHole.Position);

            if (distanceToWormHole < 50)
            {
                End(scene, dir);
                World.RenderingCamera = null;
                camera?.Destroy();
                _setTimer = false;
                return;
            }

            if (distanceToWormHole > 1500 && camera != null)
            {
                camera.FieldOfView = Mathf.Lerp(camera.FieldOfView, GameplayCamera.FieldOfView,
                    Game.LastFrameTime * 15);
                return;
            }

            var distanceScale = 1000 / distanceToWormHole;
            if (camera != null && !camera.IsShaking)
                camera.Shake(CameraShake.SkyDiving, distanceScale);

            if (camera != null)
            {
                camera.ShakeAmplitude = distanceScale;
                camera.FieldOfView = distanceToWormHole / 180 + 60;
            }

            if (PlayerPed.IsInVehicle() && !PlayerPed.CurrentVehicle.AlarmActive)
                PlayerPed.CurrentVehicle.StartAlarm();

            if (distanceToWormHole > 950) return;
            if (distanceToWormHole <= 260)
            {
                if (!_setTimer)
                {
                    _timer = DateTime.UtcNow + new TimeSpan(0, 0, 0, 0, 7500);
                    _setTimer = true;

                    if (PlayerPed.IsInVehicle())
                        PlayerPed.CurrentVehicle.Velocity = Vector3.Zero;
                    PlayerPed.Velocity = Vector3.Zero;
                    return;
                }

                if (DateTime.UtcNow < _timer)
                {
                    var direction = PlayerPosition - wormHole.Position;
                    direction.Normalize();
                    var pos = Utilities.RotatePointAroundPivot(PlayerPosition, wormHole.Position, new Vector3(0, 0, 2000 * Game.LastFrameTime));
                    var velocity = pos - (PlayerPed.IsInVehicle() ? PlayerPed.CurrentVehicle.Position : PlayerPosition);
                    if (PlayerPed.IsInVehicle())
                    {
                        var currentVehicle = PlayerPed.CurrentVehicle;
                        currentVehicle.Velocity = velocity;
                        //currentVehicle.Quaternion =
                        //    Quaternion.FromToRotation(currentVehicle.ForwardVector,
                        //        _wormHole.Position - currentVehicle.Position) * currentVehicle.Quaternion;
                        return;
                    }
                    PlayerPed.Velocity = velocity;

                    return;
                }
            }

            var playerPedVelocity = (wormHole.Position - PlayerPed.Position) * Game.LastFrameTime * 150;
            if (PlayerPed.IsInVehicle())
            {
                PlayerPed.CurrentVehicle.Velocity = Vector3.Lerp(PlayerPed.CurrentVehicle.Velocity, playerPedVelocity, Game.LastFrameTime * 5);
                return;
            }
            PlayerPed.Velocity = Vector3.Lerp(PlayerPed.Velocity, playerPedVelocity, Game.LastFrameTime * 5);
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
