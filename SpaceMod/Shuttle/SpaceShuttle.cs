using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using GTS.Particles;

namespace GTS.Shuttle
{
    public class SpaceShuttle : Entity
    {
        private readonly List<Entity> _attachments = new List<Entity>();
        private readonly PtfxLooped _gantrySmoke = new PtfxLooped("exp_grd_grenade_smoke", "core");
        private readonly PtfxLooped _srblFx = new PtfxLooped("exp_sht_flame", "core");
        private readonly PtfxLooped _srbrFx = new PtfxLooped("exp_sht_flame", "core");
        private readonly PtfxLooped _afterBurner1 = new PtfxLooped("veh_exhaust_afterburner", "core");
        private readonly PtfxLooped _afterBurner2 = new PtfxLooped("veh_exhaust_afterburner", "core");
        private readonly PtfxLooped _afterBurner3 = new PtfxLooped("veh_exhaust_afterburner", "core");
        private bool _finishedLaunch;
        private DateTime _gantrySmokeTime;

        public SpaceShuttle(int handle) : base(handle)
        {
        }

        public void SpawnAttachments()
        {
            var model1 = new Model("srbl");
            var model2 = new Model("srbr");
            var model3 = new Model("exttank");
            model1.Request();
            model2.Request();
            model3.Request();
            while (!model1.IsLoaded || !model2.IsLoaded || !model3.IsLoaded)
                Script.Yield();
            var srbl = World.CreateVehicle(model1, Position);
            var srbr = World.CreateVehicle(model2, Position);
            var extTank = World.CreateVehicle(model3, Position);
            srbl.AttachTo(this, 0);
            srbr.AttachTo(this, 0);
            extTank.AttachTo(this, 0);
            _attachments.AddRange(new[] {srbl, srbr, extTank});
            ((Vehicle) this).LandingGear = VehicleLandingGear.Retracted;
        }

        public void RemoveAttachments()
        {
            foreach (var attachment in _attachments)
                attachment?.Delete();
            _srblFx?.Stop();
            _srbrFx?.Stop();
            _gantrySmoke?.Stop();
            _afterBurner1?.Stop();
            _afterBurner2?.Stop();
            _afterBurner3?.Stop();
            CurrentBlip?.Remove();
        }

        public void Launch()
        {
            if (Game.Player.Character.IsDead)
                return;

            if (IsDead || IsInWater || !Exists(((Vehicle) this).Driver) || _finishedLaunch)
                return;
            _srblFx.Play(_attachments[0], "exhaust", Vector3.Zero, new Vector3(90, 0, 0), 20.0f);
            _srbrFx.Play(_attachments[1], "exhaust", Vector3.Zero, new Vector3(90, 0, 0), 20.0f);
            _gantrySmoke.Play(new Vector3(-6414.427f, -1338.617f, 39.4514f), new Vector3(180, 0, 0), 20);
            _afterBurner1.Play(this, "exhaust", Vector3.Zero, Vector3.Zero, 2.0f);
            _afterBurner1.SetEvolution("LOD", 1f);
            _afterBurner1.SetEvolution("throttle", 0.5f);
            _afterBurner2.Play(this, "exhaust_2", Vector3.Zero, Vector3.Zero, 2.0f);
            _afterBurner2.SetEvolution("LOD", 1f);
            _afterBurner2.SetEvolution("throttle", 0.5f);
            _afterBurner3.Play(this, "exhaust_3", Vector3.Zero, Vector3.Zero, 2.0f);
            _afterBurner3.SetEvolution("LOD", 1f);
            _afterBurner3.SetEvolution("throttle", 0.5f);
            _gantrySmokeTime = DateTime.Now + new TimeSpan(0, 0, 0, 10);
            FreezePosition = false;
            float speed = 0;

            while (true)
            {
                Script.Yield();

                Function.Call(Hash.SET_WIND_SPEED, 0f);

                Game.DisableControlThisFrame(2, Control.VehicleExit);
                Game.DisableControlThisFrame(2, Control.VehicleFlyRollLeftRight);
                Game.DisableControlThisFrame(2, Control.VehicleFlyPitchUpDown);

                if (Game.Player.Character.IsDead)
                {
                    RemoveAttachments();
                    break;
                }

                if (IsDead || IsInWater || !Exists(((Vehicle) this).Driver))
                {
                    ((Vehicle) this).Explode();
                    _attachments[0].Detach();
                    _attachments[1].Detach();
                    _attachments[2].Detach();
                    break;
                }

                if (_gantrySmoke.Exists() && DateTime.Now > _gantrySmokeTime)
                    _gantrySmoke.Stop();

                speed += Game.LastFrameTime * Settings.ShuttleThrustInterpolation;
                speed = Math.Min(speed, Settings.ShuttleNewtonsOfForce);
                ApplyForce(ForwardVector * speed, new Vector3(0, 0, Settings.ShuttleGimbalFront));

                if (HeightAboveGround > Settings.ShutStage1Height &&
                    (_attachments[0].IsAttached() || _attachments[1].IsAttached()))
                {
                    _attachments[0].Detach();
                    _attachments[1].Detach();
                }

                if (HeightAboveGround > Settings.ShutStage2Height && _attachments[2].IsAttached())
                    _attachments[2].Detach();

                if (HeightAboveGround <= Settings.EnterOrbitHeight) continue;
                _finishedLaunch = true;

                break;
            }
        }

        public new void Delete()
        {
            foreach (var attachment in _attachments)
                attachment?.Delete();
            _srblFx?.Stop();
            _srbrFx?.Stop();
            _gantrySmoke?.Stop();
            _afterBurner1?.Stop();
            _afterBurner2?.Stop();
            _afterBurner3?.Stop();
            base.Delete();
        }

        public static explicit operator Vehicle(SpaceShuttle v)
        {
            return new Vehicle(v.Handle);
        }
    }
}