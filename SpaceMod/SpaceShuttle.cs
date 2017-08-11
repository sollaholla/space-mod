using System;
using GTA;
using GTA.Math;
using GTA.Native;
using GTS.Particles;
using GTS.Audio;

namespace GTS
{
    public enum DetachSequence
    {
        Attached,
        Default
    }

    public class SpaceShuttle : Entity
    {
        private readonly Entity _srbL;
        private readonly Entity _srbR;
        private readonly Entity _extTank;

        private PtfxLooped _srbREffect;
        private PtfxLooped _srbLEffect;
        private PtfxLooped _mainThrusters;

        private Vector3 _currentForce;
        private Vector3 _startRotation;
        private Vector3 _flipRotation;
        private float _forceMult;

        private bool _launching;

        private DetachSequence _currentSequence = GTS.DetachSequence.Attached;

        private readonly Vehicle _parent;

        #region TEMPORARY!
        public static bool IsHelpMessageBeingDisplayed()
        {
            return Function.Call<bool>(Hash.IS_HELP_MESSAGE_BEING_DISPLAYED);
        }

        public static void DisplayHelpTextThisFrame(string helpText)
        {
            Function.Call(Hash._SET_TEXT_COMPONENT_FORMAT, "CELL_EMAIL_BCON");

            const int maxStringLength = 99;

            for (var i = 0; i < helpText.Length; i += maxStringLength)
            {
                Function.Call(Hash._0x6C188BE134E074AA, helpText.Substring(i, Math.Min(maxStringLength, helpText.Length - i)));
            }

            Function.Call(Hash._DISPLAY_HELP_TEXT_FROM_STRING_LABEL, 0, 0, IsHelpMessageBeingDisplayed() ? 0 : 1, -1);
        }
        #endregion

        public SpaceShuttle(int handle, Vector3 spawn) : base(handle)
        {
            _parent = new Vehicle(handle);
            spawn = spawn + new Vector3(0, 0, 5);
            Function.Call((Hash)0xCFC8BE9A5E1FE575, handle, 3);
            _extTank = World.CreateProp("exttank", spawn, false, false);
            _srbL = World.CreateProp("srbl", Vector3.Zero, false, false);
            _srbR = World.CreateProp("srbr", Vector3.Zero, false, false);
            _extTank.LodDistance = -1;
            _srbL.LodDistance = -1;
            _srbR.LodDistance = -1;
            _extTank.AttachTo(this, 0, new Vector3(0, -1, 0), new Vector3());
            _srbL.AttachTo(_extTank, 0, new Vector3(), new Vector3());
            _srbR.AttachTo(_extTank, 0, new Vector3(), new Vector3());
            IsFireProof = true;
            FreezePosition = true;
        }

        public void Control()
        {
            //         Game.DisableAllControlsThisFrame(2);
            //         Game.EnableControlThisFrame(2, GTA.Control.LookUpDown);
            //         Game.EnableControlThisFrame(2, GTA.Control.LookLeftRight);
            //         Game.EnableControlThisFrame(2, GTA.Control.ScaledLookLeftRight);
            //         Game.EnableControlThisFrame(2, GTA.Control.ScaledLookUpDown);
            //         Game.EnableControlThisFrame(2, GTA.Control.NextCamera);
            //Game.EnableControlThisFrame(2, GTA.Control.ReplayRecord);
            //Game.EnableControlThisFrame(2, GTA.Control.ReplayStartStopRecording);
            //Game.EnableControlThisFrame(2, GTA.Control.ReplayStartStopRecordingSecondary);
            //Game.EnableControlThisFrame(2, GTA.Control.FrontendPause);
            //Game.EnableControlThisFrame(2, GTA.Control.FrontendPauseAlternate);
            //Game.EnableControlThisFrame(2, GTA.Control.ReplayPause);
            //Game.EnableControlThisFrame(2, GTA.Control.ReplaySave);
            //Game.EnableControlThisFrame(2, GTA.Control.SaveReplayClip);
            //Game.EnableControlThisFrame(2, GTA.Control.CharacterWheel);
            Game.DisableControlThisFrame(2, GTA.Control.Jump);
            var values = (Control[])Enum.GetValues(typeof(Control));
            foreach (var control in values)
            {
                if (control.ToString().Contains("Vehicle"))
                    Game.DisableControlThisFrame(2, control);
            }

            if (_launching)
            {
                Thrust();
            }
            else
            {
                DisplayHelpTextThisFrame("Press ~INPUT_JUMP~ to launch.");
                if (!Game.IsDisabledControlJustPressed(2, GTA.Control.Jump)) return;
                _launching = true;
                var character = Game.Player.Character;
                if (character.IsInVehicle())
                    character.Task.PlayAnimation("veh@plane@lazer@front@base", "start_engine");
                FreezePosition = false;
                AudioController.PlayAudio(AudioType.Launch01, 0.35f);
                _startRotation = Rotation;
            }
        }

        private void Thrust()
        {
            PlayEffects();
            DetachSequence();
            ApplyForce();
        }

        private void ApplyForce()
        {
            if (_forceMult < 10)
            {
                _forceMult += Game.LastFrameTime * 5;
            }
            else
            {
                _forceMult = 10;
            }

            _currentForce = Vector3.Lerp(_currentForce, ForwardVector * _forceMult, Game.LastFrameTime * 1.5f);

            if (HeightAboveGround < 1000 && HeightAboveGround > 250)
            {
                Vector3 nextRotation = _startRotation + new Vector3(0, 0, 75);
                var rotation = Rotation;
                rotation.Z = nextRotation.Z % 360;
                Rotation = Vector3.Lerp(Rotation, rotation, Game.LastFrameTime * 0.3f);
                _flipRotation = Rotation;
            }
            else if (HeightAboveGround > 250)
            {
                Vector3 nextRotation = _flipRotation + new Vector3(50, 0, 0);
                var rotation = Rotation;
                rotation.X = nextRotation.X % 360;
                Rotation = Vector3.Lerp(Rotation, rotation, Game.LastFrameTime * 0.075f);
            }
            else
            {
                Rotation = _startRotation;
            }

            ApplyForce(_currentForce, Vector3.Zero);
        }

        private void PlayEffects()
        {
            PlayLoopedOnEnt(ref _srbLEffect, _srbL, "core", "exp_sht_flame", new Vector3(-6.5f, -17, -7), new Vector3(85, 0, 0), 20.0f);
            PlayLoopedOnEnt(ref _srbREffect, _srbR, "core", "exp_sht_flame", new Vector3(6.5f, -17, -7), new Vector3(85, 0, 0), 20.0f);

            if (_mainThrusters != null) return;
            _mainThrusters = new PtfxLooped("veh_exhaust_afterburner", "core");
            var thrust1 = _mainThrusters.Play(this, "exhaust", new Vector3(0, -1, 0), Vector3.Zero, 2.0f); 
            var thrust2 = _mainThrusters.Play(this, "exhaust_2", new Vector3(0, -1, 0), Vector3.Zero, 2.0f);
            var thrust3 = _mainThrusters.Play(this, "exhaust_3", new Vector3(0, -1, 0), Vector3.Zero, 2.0f);
            Function.Call(Hash.SET_PARTICLE_FX_LOOPED_EVOLUTION, thrust1, "throttle", 100, 0);
            Function.Call(Hash.SET_PARTICLE_FX_LOOPED_EVOLUTION, thrust2, "throttle", 100, 0);
            Function.Call(Hash.SET_PARTICLE_FX_LOOPED_EVOLUTION, thrust3, "throttle", 100, 0);
        }

        private static void PlayLoopedOnEnt(ref PtfxLooped effect, Entity ent, string asset, string name, Vector3 offset,
            Vector3 rotation, float scale)
        {
            if (effect != null) return;
            effect = new PtfxLooped(name, asset);
            effect.Play(ent, 0, offset, rotation, scale);
            //effect.PlayLoopedOnEntity(ent, offset, rotation, scale);
        }

        private void DetachSequence()
        {
            switch (_currentSequence)
            {
                case GTS.DetachSequence.Attached:
                    if (HeightAboveGround > 2000 && _srbL.IsAttached() && _srbR.IsAttached())
                    {
                        _srbL.HasCollision = false;
                        _srbR.HasCollision = false;

                        _srbL.Detach();
                        _srbL.ApplyForce(-RightVector * 15);

                        _srbR.Detach();
                        _srbR.ApplyForce(RightVector * 15);

                        Function.Call(Hash.REMOVE_PARTICLE_FX_FROM_ENTITY, _srbL.Handle);
                        Function.Call(Hash.REMOVE_PARTICLE_FX_FROM_ENTITY, _srbR.Handle);
                        AudioController.PlayAudio(AudioType.Detach01, 0.35f);
                    }
                    if (HeightAboveGround > 4500 && _extTank.IsAttached())
                    {
                        _extTank.HasCollision = false;

                        Detach();
                        Function.Call(Hash.REMOVE_PARTICLE_FX_FROM_ENTITY, _extTank.Handle);

                        _currentSequence = GTS.DetachSequence.Default;
                    }
                    break;
            }
        }

        public void CleanUp()
        {
            Function.Call(Hash.REMOVE_PARTICLE_FX_FROM_ENTITY, Handle);
            _srbL?.Delete();
            _srbR?.Delete();
            _extTank?.Delete();
        }
    }
}
