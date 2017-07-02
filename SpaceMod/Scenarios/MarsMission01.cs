using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DefaultMissions.Scenes.SceneTypes;
using GTA;
using GTA.Native;
using GTA.Math;
using SpaceMod;
using SpaceMod.Lib;
using SpaceMod.Scenario;
using SpaceMod.Scenes.Interiors;
using SpaceMod.Scenes;

namespace DefaultMissions
{
    public class MarsMission01 : CustomScenario
    {
        private readonly string _ufoModelName = "zanufo";

        private Model _ufoModel;
        private bool _didNotify;
        private bool _didSetTimeCycle;
        private bool _isCheckingSats;
        private Prop _alienEggProp;

        private readonly MarsMissionSatelliteScene _satelliteScene;
        private readonly Vector3 _computerPos = new Vector3(-2013.115f, 3198.238f, 32.81007f);
        private readonly Vector3 _spawnAlienEgg = new Vector3(-10018.03f, -9976.996f, 10042.64f) + Vector3.WorldDown;

        public MarsMission01()
        {
            Aliens = new List<AlienData>();
            Ufos = new List<Vehicle>();
            _satelliteScene = new MarsMissionSatelliteScene();

            _ufoModelName = Settings.GetValue("settings", "ufo_model", _ufoModelName);
            CurrentMissionStep = Settings.GetValue("mission", "current_mission_step", 0);
            Settings.SetValue("settings", "ufo_model", _ufoModelName);
            Settings.SetValue("mission", "current_mission_step", CurrentMissionStep);
            Settings.Save();

            SetupPlayer();
        }

        public int CurrentMissionStep { get; protected set; }
        public List<AlienData> Aliens { get; }
        public List<Vehicle> Ufos { get; }
        public Ped PlayerPed => Game.Player.Character;
        public bool OriginalCanPlayerRagdoll { get; set; }
        public int OriginalMaxHealth { get; set; }
        public Prop EnterenceBlocker { get; private set; }

        public Vector3 PlayerPosition {
            get { return PlayerPed.Position; }
            set { PlayerPed.Position = value; }
        }

        public override void OnEnterScene() { }

        public override void Start()
        {
            PlayerPed.CanRagdoll = false;
            PlayerPed.IsExplosionProof = true;
            if (CurrentMissionStep == 0 && CurrentScene.SceneFile == "MarsSurface.space")
                SpawnEntities();
        }

        private void SetupPlayer()
        {
            OriginalCanPlayerRagdoll = PlayerPed.CanRagdoll;

            if (CurrentScene.SceneFile.Equals("MarsSurface.space") && CurrentMissionStep < 6)
            {
                OriginalMaxHealth = PlayerPed.MaxHealth;
                PlayerPed.MaxHealth = 1500;
                PlayerPed.Health = PlayerPed.MaxHealth;
            }
            else if (CurrentScene.SceneFile.Equals("EuropaSurface.space"))
            {
                OriginalMaxHealth = PlayerPed.MaxHealth;
                PlayerPed.MaxHealth = 500;
                PlayerPed.Health = PlayerPed.MaxHealth;
            }
            else OriginalMaxHealth = -1;
        }

        public void SpawnEntities()
        {
            var origin = PlayerPosition.Around(100f);

            for (var i = 0; i < 15; i++)
            {
                Vector3 position = origin.Around(new Random().Next(50, 75));
                Vector3 artificial = TryToGetGroundHeight(position);
                if (artificial != Vector3.Zero) position = artificial;

                Ped ped = SpaceModLib.CreateAlien(position, WeaponHash.Railgun);
                ped.Health = 3750;
                ped.AlwaysDiesOnLowHealth = false;
                ped.CanRagdoll = false;
                ped.IsOnlyDamagedByPlayer = true;

                Blip blip = ped.AddBlip();
                blip.Name = "Alien";
                blip.Scale = 0.7f;

                Aliens.Add(new AlienData { Ped = ped, StoppingDistance = new Random().Next(22, 28) });
            }

            _ufoModel = new Model(_ufoModelName);
            _ufoModel.Request();
            DateTime timout = DateTime.UtcNow + new TimeSpan(0, 0, 0, 10);
            while (!_ufoModel.IsLoaded)
            {
                Script.Yield();
                if (DateTime.UtcNow > timout)
                    break;
            }

            if (!_ufoModel.IsLoaded)
            {
                UI.Notify($"{_ufoModelName} model failed to load! Make sure you have a valid model in the .ini file.");
                EndScenario(false);
                return;
            }

            for (var i = 0; i < 1; i++)
            {
                Vector3 position = origin.Around(75);
                position.Z = SpaceModDatabase.PlanetSurfaceGalaxyCenter.Z + 45;

                Vehicle spaceCraft = World.CreateVehicle(_ufoModel, position, 0);
                spaceCraft.IsOnlyDamagedByPlayer = true;

                Ped ped = spaceCraft.CreatePedOnSeat(VehicleSeat.Driver, SpaceModLib.GetAlienModel());
                ped.IsInvincible = true;
                ped.RelationshipGroup = SpaceModDatabase.AlienRelationship;
                ped.IsOnlyDamagedByPlayer = true;
                ped.SetDefaultClothes();

                spaceCraft.Heading = (PlayerPed.Position - spaceCraft.Position).ToHeading();

                ped.Task.FightAgainst(PlayerPed);
                ped.SetCombatAttributes(CombatAttributes.AlwaysFight, true);

                spaceCraft.MaxHealth = 1000;
                spaceCraft.Health = spaceCraft.MaxHealth;

                Blip blip = spaceCraft.AddBlip();
                blip.Name = "UFO";
                blip.Color = BlipColor.Green;

                Ufos.Add(spaceCraft);
            }
        }

        private Vector3 TryToGetGroundHeight(Vector3 position)
        {
            var artificial = position.MoveToGroundArtificial();
            var timeout = DateTime.UtcNow + new TimeSpan(0, 0, 0, 0, 1500);

            while (artificial == Vector3.Zero)
            {
                artificial = position.MoveToGroundArtificial();

                Script.Yield();

                if (DateTime.UtcNow > timeout)
                    break;
            }

            return artificial;
        }

        public override void OnUpdate()
        {
            // Check to make sure we're in the right scene for the right sequence.
            if (CurrentMissionStep < 6 && CurrentScene.SceneFile != "MarsSurface.space")
                return;
            if (CurrentMissionStep >= 6 && CurrentScene.SceneFile != "EuropaSurface.space")
                return;

            CreateEnteranceBlockIfNeeded();
            switch (CurrentMissionStep)
            {
                case 0:
                    UpdateBlockEnterance();
                    CheckAliensAndUfo();
                    Aliens.ForEach(UpdateAlien);
                    Ufos.ForEach(UpdateUfo);
                    break;
                case 1:
                    DeleteEnteranceBlocker();
                    CurrentMissionStep++;
                    break;
                case 2:
                    SpawnInteriorAliens();
                    CurrentMissionStep++;
                    break;
                case 3:
                    Aliens.ForEach(UpdateInteriorAlien);
                    CheckInteriorAliens();
                    break;
                case 4:
                    if (!_isCheckingSats) StartSatteliteScene();
                    else EndSatteliteScene();
                    break;
                case 5:
                    Function.Call(Hash.PLAY_MISSION_COMPLETE_AUDIO, "FRANKLIN_BIG_01");
                    ScaleFormMessages.Message.SHOW_MISSION_PASSED_MESSAGE(Game.GetGXTEntry("BM_LABEL_0"));
                    Script.Wait(1000);
                    SpaceModLib.ShowSubtitleWithGXT("GTS_LABEL_18");
                    CurrentMissionStep++;
                    break;
                case 6:
                    if (CurrentScene.SceneFile == "EuropaSurface.space")
                    {
                        SpaceModLib.ShowSubtitleWithGXT("GTS_LABEL_19");
                        CurrentMissionStep++;
                    }
                    break;
                case 7:
                    if (CurrentScene.SceneFile == "EuropaSurface.space" && CurrentScene.SceneData.CurrentIplData?.Name == "Europa/ufo_interior")
                    {
                        Vector3[] spawnPoints =
                        {
                            new Vector3(-10015.6f, -9970.742f, 10041.6f),
                            new Vector3(-10021.1f, -9975.481f, 10041.6f),
                            new Vector3(-10024.99f, -9963.521f, 10041.6f),
                            new Vector3(-10015.08f, -9968.357f, 10041.6f),
                        };
                        foreach (var spawn in spawnPoints)
                        {
                            var ped = SpaceModLib.CreateAlien(spawn, WeaponHash.MicroSMG);
                            ped.Heading = (PlayerPed.Position - ped.Position).ToHeading();
                            ped.Task.FightAgainst(PlayerPed, -1);
                            Aliens.Add(new AlienData { Ped = ped });
                        }
                        _alienEggProp = World.CreateProp("sm_alien_egg_w_container", _spawnAlienEgg, false, false);
                        _alienEggProp.FreezePosition = true;
                        _alienEggProp.Heading = (PlayerPosition - _spawnAlienEgg).ToHeading();
                        CurrentMissionStep++;
                    }
                    break;
                case 8:
                    if (_alienEggProp == null)
                    {
                        CurrentMissionStep = 7;
                        return;
                    }
                    if (CurrentScene.SceneFile == "EuropaSurface.space")
                    {
                        Aliens.ForEach(UpdateUfoInteriorAliens);
                        if (Aliens.All(a => a.Ped.IsDead))
                        {
                            Aliens.Clear();
                            SpaceModLib.ShowSubtitleWithGXT("GTS_LABEL_20");

                            CurrentMissionStep++;
                        }
                    }
                    break;
                case 9:
                    if (_alienEggProp == null)
                    {
                        CurrentMissionStep = 7;
                        return;
                    }
                    if (CurrentScene.SceneData.CurrentIplData?.Name == "Europa/ufo_interior")
                    {
                        float distance = PlayerPosition.DistanceTo(_alienEggProp.Position);
                        if (distance < 2)
                        {
                            SpaceModLib.DisplayHelpTextWithGXT("GTS_LABEL_7");
                            Game.DisableControlThisFrame(2, Control.Context);

                            if (Game.IsDisabledControlJustPressed(2, Control.Context))
                            {
                                SpaceModLib.ShowSubtitleWithGXT("GTS_LABEL_21");
                                ScaleFormMessages.Message.SHOW_MISSION_PASSED_MESSAGE(Game.GetGXTEntry("BM_LABEL_1"));
                                Function.Call(Hash.PLAY_MISSION_COMPLETE_AUDIO, "FRANKLIN_BIG_01");
                                _alienEggProp.Delete();
                                EndScenario(true);
                            }
                        }
                    }
                    break;
            }
        }

        private void UpdateUfoInteriorAliens(AlienData alien)
        {
            var alienPed = alien.Ped;

            var freeze = CurrentScene.SceneData.CurrentIplData?.Name != "Europa/ufo_interior";
            alienPed.FreezePosition = freeze;
            alienPed.IsVisible = !freeze;
            alienPed.Task.FightAgainst(PlayerPed);
        }

        private void UpdateAlien(AlienData alien)
        {
            var alienPed = alien.Ped;

            if (!string.IsNullOrEmpty(CurrentScene.SceneData.CurrentIplData?.Name))
                alienPed.FreezePosition = PlayerPosition.DistanceTo(alienPed.Position) > 1000;

            if (alienPed.IsDead)
            {
                if (alienPed.IsPersistent)
                    alienPed.MarkAsNoLongerNeeded();

                if (Blip.Exists(alienPed.CurrentBlip))
                    alienPed.CurrentBlip.Remove();

                if (!alienPed.CanRagdoll)
                    alienPed.CanRagdoll = true;

                return;
            }

            SpaceModLib.ArtificialDamage(alienPed, PlayerPed, 1.5f, 75);
            float distance = Vector3.Distance(PlayerPosition, alienPed.Position);

            if (distance > alien.StoppingDistance)
                alienPed.Task.RunTo(PlayerPed.Position, true);
        }

        private void UpdateInteriorAlien(AlienData alien)
        {
            var alienPed = alien.Ped;

            var isMovable = PlayerPosition.DistanceTo(alienPed.Position) < 1000;
            alienPed.FreezePosition = !isMovable;
            alienPed.IsVisible = isMovable;

            if (!alienPed.IsDead || !alienPed.IsPersistent)
                return;

            alienPed.MarkAsNoLongerNeeded();
        }

        private void StartSatteliteScene()
        {
            const float markerSize = 0.4f;
            World.DrawMarker(MarkerType.VerticalCylinder, _computerPos + Vector3.WorldDown, Vector3.Zero, Vector3.Zero, new Vector3(markerSize, markerSize, markerSize), Color.Gold);
            float distance = Vector3.DistanceSquared(PlayerPosition, _computerPos);
            const float interactDist = 1.5f * 2;

            if (distance > interactDist)
                return;

            SpaceModLib.DisplayHelpTextWithGXT("GTS_LABEL_5");
            Game.DisableControlThisFrame(2, Control.Talk);
            Game.DisableControlThisFrame(2, Control.Context);

            if (!Game.IsDisabledControlJustPressed(2, Control.Context))
                return;
            _isCheckingSats = true;
            Game.FadeScreenOut(0);
            _satelliteScene.Spawn();
            Game.FadeScreenIn(100);
        }

        private void EndSatteliteScene()
        {
            if (_satelliteScene.Failed)
            {
                _satelliteScene.Remove();
                CurrentScene.SetTimeCycle();
                CurrentMissionStep++;
                return;
            }
            _satelliteScene.Update();

            SpaceModLib.DisplayHelpTextWithGXT("GTS_LABEL_6");
            Game.DisableControlThisFrame(2, Control.Talk);
            Game.DisableControlThisFrame(2, Control.Context);
            if (Game.IsDisabledControlJustPressed(2, Control.Context))
            {
                _satelliteScene.Remove();
                CurrentScene.SetTimeCycle();
                TimeCycleModifier.Clear();
                UI.ShowSubtitle(string.Empty); // just to clear the subtitle.
                CurrentMissionStep++;
            }

            if (_didSetTimeCycle)
                return;
            SpaceModLib.ShowSubtitleWithGXT("GTS_LABEL_17");
            TimeCycleModifier.Set("CAMERA_secuirity_FUZZ", 1f);
            _didSetTimeCycle = true;
        }

        private void CheckInteriorAliens()
        {
            if (Aliens.All(a => a.Ped.IsDead))
            {
                SpaceModLib.ShowSubtitleWithGXT("GTS_LABEL_16");
                Aliens.Clear();

                IplData iplData = CurrentScene.SceneData.Ipls?.Find(x => x.Name == "Mars/mars_base_int_01");
                if (iplData != null)
                {
                    Ipl ipl = iplData.CurrentIpl;
                    if (ipl != null) ipl.Peds?.ForEach(ped => ped?.Task.ClearAll());
                }

                CurrentMissionStep++;
            }
        }

        private void SpawnInteriorAliens()
        {
            Vector3[] spawnPoints = {
                new Vector3(-2014.449f, 3216.207f, 32.81112f),
                new Vector3(-1989.808f, 3212.001f, 32.81171f),
                new Vector3(-1991.477f, 3205.936f, 32.81038f),
                new Vector3(-1997.719f, 3211.335f, 32.83896f)
            };
            foreach (var spawn in spawnPoints)
            {
                var alien = SpaceModLib.CreateAlien(spawn, WeaponHash.MicroSMG);
                alien.Heading = (PlayerPed.Position - alien.Position).ToHeading();
                alien.Task.FightAgainst(PlayerPed, -1);
                Aliens.Add(new AlienData { Ped = alien });
            }
        }

        private void DeleteEnteranceBlocker()
        {
            SpaceModLib.ShowSubtitleWithGXT("GTS_LABEL_15");
            if (Entity.Exists(EnterenceBlocker)) EnterenceBlocker.Delete();
        }

        private void CheckAliensAndUfo()
        {
            if (!(Aliens.All(a => a.Ped.IsDead) && Ufos.All(u => Entity.Exists(u.Driver) && u.Driver.IsDead)))
                return;

            CurrentMissionStep++;
        }

        private void UpdateBlockEnterance()
        {
            if (Entity.Exists(EnterenceBlocker) && PlayerPed.IsTouching(EnterenceBlocker))
            {
                if (!_didNotify)
                {
                    SpaceModLib.DisplayHelpTextWithGXT("GTS_LABEL_1");
                    _didNotify = true;
                }
            }
            else
            {
                _didNotify = false;
            }
        }

        private void CreateEnteranceBlockIfNeeded()
        {
            if (CurrentMissionStep < 1)
            {
                if (!Entity.Exists(EnterenceBlocker) && CurrentScene.SceneData.Ipls.Any() && CurrentScene.SceneData.Ipls.Any())
                {
                    Vector3 position = CurrentScene.SceneData.Ipls[0]?.Teleports[0]?.Start ?? Vector3.Zero;
                    EnterenceBlocker = new Prop(World.CreateProp("lts_prop_lts_elecbox_24b", position, Vector3.Zero, false, false).Handle)
                    {
                        IsVisible = false
                    };
                }
            }
        }

        private void UpdateUfo(Vehicle ufo)
        {
            if (!ufo.IsPersistent)
                return;

            if (!string.IsNullOrEmpty(CurrentScene.SceneData.CurrentIplData?.Name))
                ufo.FreezePosition = PlayerPosition.DistanceTo(SpaceModDatabase.PlanetSurfaceGalaxyCenter) > 1000;

            if (Entity.Exists(ufo.Driver))
                SpaceModLib.ArtificialDamage(ufo.Driver, PlayerPed, 150, 150);

            if (ufo.IsDead || (Entity.Exists(ufo.Driver) && ufo.Driver.IsDead))
            {
                if (Entity.Exists(ufo.Driver) && !ufo.Driver.IsDead)
                    ufo.Driver.Kill();

                ufo.CurrentBlip.Remove();
                ufo.Health = 0;
                ufo.EngineHealth = 0;
                ufo.Explode();
                ufo.MarkAsNoLongerNeeded();
            }

            ufo.Rotation = new Vector3(0, 0, ufo.Rotation.Z);

            if (ufo.Position.DistanceToSquared2D(PlayerPosition) > 70000)
            {
                const float rotationSpeed = 2.5f;
                ufo.Heading = Mathf.Lerp(ufo.Heading, (PlayerPosition - ufo.Position).ToHeading(), Game.LastFrameTime * rotationSpeed);

                var dir = PlayerPosition - ufo.Position;
                dir.Z = 0;
                bool inAngle = Vector3.Angle(ufo.ForwardVector, dir) < 5;

                if (!inAngle)
                    ufo.Velocity = Vector3.Zero;

                ufo.Speed = inAngle ? 5 : 0;
                return;
            }

            ufo.Speed = 30;

        }

        public override void OnAborted()
        {
            CleanUp();
            Settings.SetValue("mission", "current_mission_step", CurrentMissionStep);
            Settings.Save();

            TimeCycleModifier.Clear();
            _satelliteScene?.Remove();
        }

        public override void OnEnded(bool success)
        {
            if (success)
                MarkEntitesAsNotNeeded();
            else CleanUp();

            if (OriginalMaxHealth != -1)
            {
                PlayerPed.MaxHealth = OriginalMaxHealth;
                PlayerPed.Health = PlayerPed.MaxHealth;
            }
            PlayerPed.CanRagdoll = OriginalCanPlayerRagdoll;
            PlayerPed.IsExplosionProof = false;
            Settings.SetValue("mission", "current_mission_step", CurrentMissionStep);
            Settings.Save();

            TimeCycleModifier.Clear();
            _satelliteScene?.Remove();
        }

        private void MarkEntitesAsNotNeeded()
        {
            while (Aliens.Count > 0)
            {
                var alien = Aliens[0];
                alien.Ped.MarkAsNoLongerNeeded();
                Aliens.RemoveAt(0);
            }

            while (Ufos.Count > 0)
            {
                Vehicle craft = Ufos[0];
                if (Entity.Exists(craft.Driver))
                    craft.Driver.Delete();
                craft.MarkAsNoLongerNeeded();
                Ufos.RemoveAt(0);
            }
            if (Entity.Exists(EnterenceBlocker))
            {
                EnterenceBlocker.Delete();
            }
            if (Entity.Exists(_alienEggProp))
            {
                _alienEggProp.Delete();
            }
        }

        private void CleanUp()
        {
            while (Aliens.Count > 0)
            {
                var alien = Aliens[0];
                alien.Ped.Delete();
                Aliens.RemoveAt(0);
            }

            while (Ufos.Count > 0)
            {
                Vehicle craft = Ufos[0];
                if (Entity.Exists(craft.Driver))
                    craft.Driver.Delete();
                craft.Delete();
                Ufos.RemoveAt(0);
            }

            if (Entity.Exists(EnterenceBlocker))
                EnterenceBlocker.Delete();
            if (Entity.Exists(_alienEggProp))
                _alienEggProp.Delete();
        }
    }
}
