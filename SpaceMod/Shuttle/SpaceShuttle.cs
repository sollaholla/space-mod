using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTS.Library;
using GTS.Particles;

namespace GTS.Shuttle
{
    public class SpaceShuttle : Entity
    {
        private readonly List<Prop> _attachments = new List<Prop>();
        private readonly PtfxLooped _srblFx = new PtfxLooped("exp_sht_flame", "core");
        private readonly PtfxLooped _srbrFx = new PtfxLooped("exp_sht_flame", "core");
        private readonly PtfxLooped _gantrySmoke = new PtfxLooped("exp_grd_grenade_smoke", "core");
        private DateTime _gantrySmokeTime;
        private bool _detached;

        public SpaceShuttle(int handle) : base(handle) { }

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
            var srbl = World.CreateProp(model1, Position, Rotation, false, false);
            var srbr = World.CreateProp(model2, Position, Rotation, false, false);
            var extTank = World.CreateProp(model3, Position, Rotation, false, false);
            srbl.AttachTo(this, 0);
            srbr.AttachTo(this, 0);
            extTank.AttachTo(this, 0);
            _attachments.AddRange(new[] { srbl, srbr, extTank });
            ((Vehicle)this).LandingGear = VehicleLandingGear.Retracted;
        }

        public void RemoveAttachments()
        {
            foreach (var attachment in _attachments)
                attachment?.Delete();
            _srblFx?.Stop();
            _srbrFx?.Stop();
            _gantrySmoke?.Stop();
            CurrentBlip?.Remove();
        }

        public void Launch()
        {
            if (IsDead || IsInWater || !Exists(((Vehicle)this).Driver) || _detached)
                return;

            _srblFx.Play(_attachments[0], 0, new Vector3(-6.5f, -17, -7), new Vector3(85, 0, 0), 20.0f);
            _srbrFx.Play(_attachments[1], 0, new Vector3(6.5f, -17, -7), new Vector3(85, 0, 0), 20.0f);
            _gantrySmoke.Play(new Vector3(-6414.427f, -1338.617f, 39.4514f), new Vector3(180, 0, 0), 20);
            _gantrySmokeTime = DateTime.Now + new TimeSpan(0, 0, 0, 10);
            FreezePosition = false;
            float speed = 0;

            while (true)
            {
                Script.Yield();

                if (IsDead || IsInWater || !Exists(((Vehicle)this).Driver))
                {
                    ((Vehicle)this).Explode();
                    _attachments[0].Detach();
                    _attachments[1].Detach();
                    _attachments[2].Detach();
                    break;
                }

                if (_gantrySmoke.Exists() && DateTime.Now > _gantrySmokeTime)
                    _gantrySmoke.Stop();

                speed += Game.LastFrameTime * 0.7f;
                speed = Math.Min(speed, 50);
                ApplyForce(ForwardVector * speed,
                    new Vector3(0, 0, _attachments[0].IsAttached() || _attachments[1].IsAttached() ? 0.12f : 0.2f));

                GtsLibNet.DisplayHelpTextWithGxt("SHUT_STAGE");
                if (Game.IsControlJustPressed(2, Control.ParachuteSmoke))
                {
                    if (_attachments[0].IsAttached() || _attachments[1].IsAttached())
                    {
                        _attachments[0].Detach();
                        _attachments[1].Detach();
                        _srblFx?.Stop();
                        _srbrFx?.Stop();
                    }
                    else
                    {
                        _attachments[2].Detach();
                        _detached = true;
                        break;
                    }
                }

                if (HeightAboveGround > GTS.Settings.EnterOrbitHeight)
                {
                    _detached = true;
                    break;
                }
            }
        }

        public new void Delete()
        {
            foreach (var attachment in _attachments)
                attachment?.Delete();
            _srblFx?.Stop();
            _srbrFx?.Stop();
            _gantrySmoke?.Stop();
            base.Delete();
        }

        public static explicit operator Vehicle(SpaceShuttle v)
        {
            return new Vehicle(v.Handle);
        }
    }
}