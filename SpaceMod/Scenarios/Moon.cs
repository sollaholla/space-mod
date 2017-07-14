using System;
using GTA;
using GTA.Math;
using GTA.Native;
using SpaceMod.Lib;
using SpaceMod.Scenarios;
using System.Collections.Generic;
using SpaceMod.Extensions;
using System.Linq;
using System.IO;
using SpaceMod.Particles;
using System.Reflection;

namespace DefaultMissions
{
    public class Moon : Scenario
    {
        #region Fields
        private List<OnFootCombatPed> aliens = new List<OnFootCombatPed>();
        private List<Ped> pilots = new List<Ped>();
        private List<Vehicle> vehicles = new List<Vehicle>();
        private Vehicle carrierShip;
        private Vector3 carrierSpawn;
        private Prop laptop;
        private ICutScene cutscene;
        private Random random = new Random();
        private Vector3 laptopSpawnPosition = new Vector3(-10002.10f, -10004.52f, 10001.463f);
        private Vector3 laptopSpawnRotation = new Vector3(0, 0, 0.2404f);

        #region Settings
        private int missionStep = 0;
        private int enemyCount = 15;
        private int pilotCount = 3;
        private float aiWeaponDamage = 0.01f;
        private string flagModel = "ind_prop_dlc_flag_02";
        private string cutscenePlanetModel = "mars_large";
        private Vector3 lastFlagPos = Vector3.Zero;
        #endregion
        #endregion

        #region Functions
        #region Implemented
        public override void OnAwake()
        {
            ReadSettings();

            ///////////////////////////////////////////////
            // NOTE: Doing this to create the ini if it doesn't exist.
            ///////////////////////////////////////////////
            SaveSettings();

            SpawnFlag();
        }

        public override void OnStart()
        {
            if (missionStep == 0)
            {
                SpawnAliens();

                Function.Call(Hash.SET_AI_WEAPON_DAMAGE_MODIFIER, aiWeaponDamage);
            }

            SpawnLaptop();

            Game.Player.Character.CanRagdoll = false;
        }

        public override void OnUpdate()
        {
            switch (missionStep)
            {
                case 0:
                    ProcessAliens();
                    UpdateCarierShip();
                    if (!AreAllAliensDead()) return;
                    missionStep++;
                    break;
                case 1:
                    if (Entity.Exists(carrierShip))
                        carrierShip.Delete();
                    Function.Call(Hash._PLAY_AMBIENT_SPEECH1, Game.Player.Character, "Generic_Insult_Med", "Speech_Params_Force");
                    Function.Call(Hash.PLAY_MISSION_COMPLETE_AUDIO, "FRANKLIN_BIG_01");
                    missionStep++;
                    break;
                case 2:
                    if (!Function.Call<bool>(Hash.IS_MISSION_COMPLETE_PLAYING))
                        return;
                    Effects.Start(ScreenEffect.SuccessNeutral, 5000);
                    ScaleFormMessages.Message.SHOW_MISSION_PASSED_MESSAGE(Game.GetGXTEntry("ENEMIES_ELIM"));
                    Script.Wait(4000);
                    missionStep++;
                    break;
                case 3:
                    SpaceModLib.DisplayHelpTextWithGXT("PLANT_FLAG");
                    if (!Game.IsControlJustPressed(2, Control.Context))
                        return;
                    Vector3 spawn = Game.Player.Character.Position + Game.Player.Character.ForwardVector;
                    Vector3 ground = spawn.MoveToGroundArtificial(Game.Player.Character);
                    if (ground != Vector3.Zero) spawn = ground;
                    Game.Player.Character.Task.PlayAnimation("pickup_object", "pickup_low");
                    lastFlagPos = spawn;
                    SpawnFlag();
                    CheckMarsMission();
                    break;
                case 4:
                    Script.Wait(1000);
                    if (!Entity.Exists(laptop))
                    {
                        SpawnLaptop();
                        return;
                    }
                    UI.ShowSubtitle(Game.GetGXTEntry("SAT_CHECK"), 7500);
                    laptop.AddBlip().Scale = 0.6f;
                    laptop.CurrentBlip.Color = BlipColor.Yellow;
                    missionStep++;
                    break;
                case 5:
                    float distance = Vector3.DistanceSquared2D(Game.Player.Character.Position, laptop.Position);
                    if (distance > 3)
                        return;
                    SpaceModLib.DisplayHelpTextWithGXT("PRESS_E");
                    if (!Game.IsControlJustPressed(2, Control.Context))
                        return;
                    cutscene = new MoonSatelliteCutscene(cutscenePlanetModel);
                    cutscene.Start();
                    laptop.CurrentBlip.Remove();
                    missionStep++;
                    break;
                case 6:
                    if (!cutscene.Complete)
                    {
                        cutscene.Update();
                        return;
                    }
                    cutscene.Stop();
                    Effects.Start(ScreenEffect.FocusOut);
                    missionStep++;
                    break;
                case 7:
                    Script.Wait(500);
                    Game.Player.Character.Task.PlayAnimation("gestures@f@standing@casual", "gesture_no_way");
                    Function.Call(Hash._PLAY_AMBIENT_SPEECH1, Game.Player.Character, "Generic_Shocked_High", "Speech_Params_Force");
                    Script.Wait(1000);
                    UI.ShowSubtitle(Game.GetGXTEntry("GO_TO") + " ~p~Mars~s~.", 7500);
                    EndScenario(true);
                    break;
            }
        }

        public override void OnEnded(bool success)
        {
            Game.Player.Character.CanRagdoll = true;

            SaveSettings();

            if (!success)
            {
                DeleteAliens(true);

                return;
            }

            DeleteAliens(false);
        }

        public override void OnAborted()
        {
            DeleteAliens(true);
        }
        #endregion

        private void ReadSettings()
        {
            missionStep = Settings.GetValue("general", "mission_step", missionStep);
            enemyCount = Settings.GetValue("general", "enemy_count", enemyCount);
            pilotCount = Settings.GetValue("general", "pilot_count", pilotCount);
            flagModel = Settings.GetValue("general", "flag_model", flagModel);
            aiWeaponDamage = Settings.GetValue("general", "ai_weapon_damage", aiWeaponDamage);
            lastFlagPos = ParseVector3.Read(Settings.GetValue("general", "last_flag_pos"), Vector3.Zero);
            cutscenePlanetModel = Settings.GetValue("cutscene", "cutscene_planet_model", cutscenePlanetModel);
            laptopSpawnPosition = ParseVector3.Read(Settings.GetValue("cutscene", "laptop_spawn_position"), laptopSpawnPosition);
            laptopSpawnRotation = ParseVector3.Read(Settings.GetValue("cutscene", "laptop_spawn_rotation"), laptopSpawnRotation);
        }

        private void SaveSettings()
        {
            Settings.SetValue("general", "mission_step", missionStep);
            Settings.SetValue("general", "enemy_count", enemyCount);
            Settings.SetValue("general", "pilot_count", pilotCount);
            Settings.SetValue("general", "ai_weapon_damage", aiWeaponDamage);
            Settings.SetValue("general", "last_flag_pos", lastFlagPos);
            Settings.SetValue("general", "flag_model", flagModel);
            Settings.SetValue("cutscene", "cutscene_planet_model", cutscenePlanetModel);
            Settings.SetValue("cutscene", "laptop_spawn_position", laptopSpawnPosition);
            Settings.SetValue("cutscene", "laptop_spawn_rotation", laptopSpawnRotation);
            Settings.Save();
        }

        private void DeleteAliens(bool delete)
        {
            cutscene?.Stop();

            Function.Call(Hash.RESET_AI_WEAPON_DAMAGE_MODIFIER);

            foreach (OnFootCombatPed alien in aliens)
            {
                if (delete)
                {
                    alien.Delete();
                    continue;
                }

                alien.MarkAsNoLongerNeeded();
            }

            foreach (Ped pilot in pilots)
            {
                if (delete)
                {
                    pilot.Delete();
                    continue;
                }

                pilot.MarkAsNoLongerNeeded();
            }

            foreach (Vehicle vehicle in vehicles)
            {
                if (delete)
                {
                    vehicle.Delete();
                    continue;
                }

                vehicle.MarkAsNoLongerNeeded();
            }

            if (Entity.Exists(laptop))
            {
                if (delete)
                    laptop.Delete();
                else
                {
                    laptop.MarkAsNoLongerNeeded();
                    laptop.CurrentBlip.Remove();
                }
            }

            if (Entity.Exists(carrierShip))
            {
                carrierShip.Delete();
            }
        }

        private void SpawnAliens()
        {
            Vector3 pedSpawn = CurrentScene.Info.GalaxyCenter + (Vector3.RelativeLeft * 150);

            if (!DidGoToMars())
            {
                carrierShip = World.CreateVehicle("zanufo", pedSpawn + Vector3.WorldUp * 15);

                if (Entity.Exists(carrierShip))
                {
                    carrierSpawn = carrierShip.Position;
                    carrierShip.FreezePosition = true;
                    carrierShip.IsInvincible = true;
                    carrierShip.HasCollision = false;
                }
            }

            for (int i = 0; i < enemyCount; i++)
            {
                Ped alien = HelperFunctions.SpawnAlien(pedSpawn.Around(random.Next(5, 40)));

                if (Entity.Exists(alien))
                {
                    alien.AddBlip().Scale = 0.5f;

                    aliens.Add(new OnFootCombatPed(alien) { Target = Game.Player.Character });
                }
            }

            Vector3 vehicleSpawnArea = CurrentScene.Info.GalaxyCenter + Vector3.RelativeLeft * 250f;
            float maxZ = CurrentScene.Info.GalaxyCenter.Z;

            for (int i = 0; i < pilotCount; i++)
            {
                float randomDistance = Function.Call<float>(Hash.GET_RANDOM_FLOAT_IN_RANGE, 25, 50);

                Vector3 spawnPoint = vehicleSpawnArea.Around(randomDistance);

                Vehicle vehicle = HelperFunctions.SpawnUfo(spawnPoint);

                if (Entity.Exists(vehicle))
                {
                    Ped pilot = vehicle.CreatePedOnSeat(VehicleSeat.Driver, PedHash.MovAlien01);

                    if (Entity.Exists(pilot))
                    {
                        pilot.SetDefaultClothes();
                        pilot.RelationshipGroup = SpaceMod.Database.AlienRelationshipGroup;

                        Function.Call(Hash.TASK_PLANE_MISSION, pilot, vehicle, 0, Game.Player.Character, 0, 0, 0, 6, 0f, 0f, 0f, 0f, maxZ + 150f);
                        Function.Call(Hash._SET_PLANE_MIN_HEIGHT_ABOVE_TERRAIN, vehicle, maxZ + 150f);

                        pilot.AlwaysKeepTask = true;
                        pilot.SetCombatAttributes(CombatAttributes.AlwaysFight, true);
                        pilots.Add(pilot);

                        vehicle.AddBlip().Scale = 0.8f;
                        vehicle.MarkAsNoLongerNeeded();
                        vehicles.Add(vehicle);

                        continue;
                    }

                    vehicle.Delete();
                }
            }
        }

        private void SpawnLaptop()
        {
            Prop prop = World.CreateProp("bkr_prop_clubhouse_laptop_01a", laptopSpawnPosition, laptopSpawnRotation, false, false);

            if (!Entity.Exists(prop))
                return;

            prop.FreezePosition = true;
            prop.IsInvincible = true;
            laptop = prop;
        }

        private void SpawnFlag()
        {
            if (string.IsNullOrEmpty(flagModel) && lastFlagPos != Vector3.Zero)
                return;

            Prop flag = World.CreateProp(flagModel, lastFlagPos, false, false);

            if (!Entity.Exists(flag))
                return;

            flag.FreezePosition = true;

            ///////////////////////////
            // NOTE: Just in case the 
            // flag moved.
            ///////////////////////////
            flag.Position = lastFlagPos;
        }

        private void ProcessAliens()
        {
            Vector3 playerPosition = Game.Player.Character.Position;

            foreach (OnFootCombatPed alien in aliens)
            {
                alien.Update();
            }

            foreach (Vehicle vehicle in vehicles)
            {
                if ((vehicle.IsDead || (Entity.Exists(vehicle.Driver) && vehicle.Driver.IsDead)))
                {
                    if (Blip.Exists(vehicle.CurrentBlip))
                    {
                        vehicle.CurrentBlip.Remove();
                    }

                    continue;
                }

                if (vehicle.IsPersistent)
                {
                    float dist = Vector3.DistanceSquared2D(playerPosition, vehicle.Position);

                    const float maxDist = 1024 * 1024;

                    if (dist > maxDist)
                    {
                        vehicle.MarkAsNoLongerNeeded();
                    }
                }
            }
        }

        private void UpdateCarierShip()
        {
            if (Entity.Exists(carrierShip))
            {
                if (carrierSpawn.Z > CurrentScene.Info.GalaxyCenter.Z + 75)
                    carrierSpawn += Vector3.RelativeRight * Game.LastFrameTime * 500;
                else carrierSpawn += Vector3.WorldUp * Game.LastFrameTime * 10;

                carrierShip.Position = carrierSpawn;
                carrierShip.EngineRunning = true;
                carrierShip.LightsOn = true;
                carrierShip.InteriorLightOn = true;
                carrierShip.BrakeLightsOn = true;

                const int maxDist = 100000;
                float d = Vector3.DistanceSquared(Game.Player.Character.Position, carrierSpawn);

                if (d > maxDist)
                {
                    carrierShip.Delete();
                }
            }
        }

        private bool DidGoToMars()
        {
            string currentDirectory = Directory.GetParent(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath).FullName;
            string path = Path.Combine(currentDirectory, Path.ChangeExtension(typeof(Mars).Name, "ini"));
            ScriptSettings settings = ScriptSettings.Load(path);
            int currentStep = settings.GetValue(Mars.settings_GeneralSectionString, Mars.settings_MissionStepString, 0);
            return currentStep > 0;
        }

        private void CheckMarsMission()
        {
            if (DidGoToMars())
            {
                EndScenario(true);
                return;
            }
            missionStep++;
        }

        private bool AreAllAliensDead()
        {
            return aliens.TrueForAll(alien => alien.IsDead) && vehicles.TrueForAll(vehicle => vehicle.IsDead);
        }
        #endregion

        private class MoonSatelliteCutscene : ICutScene
        {
            private const string TextureDict = "securitycam";
            private const string TextureName = "securitycam_box";
            private Camera camera;
            private Vehicle ufo;
            private Prop planetProp;
            private int step = 0;
            private Vector3 pos;
            private string planetModel;
            private float seconds;
            private Random random = new Random();

            public MoonSatelliteCutscene(string planetModel)
            {
                this.planetModel = planetModel;
            }

            public bool Complete { get; set; }

            public void Start()
            {
                Vector3 spawn = new Vector3(10000, 10000, 10000);
                camera = World.CreateCamera(spawn, new Vector3(270, 0, 0), 60);
                World.RenderingCamera = camera;
                Game.FadeScreenOut(0);
                Function.Call(Hash.REQUEST_STREAMED_TEXTURE_DICT, TextureDict, 0);
                while (!Function.Call<bool>(Hash.HAS_STREAMED_TEXTURE_DICT_LOADED, TextureDict))
                    Script.Yield();
                Game.FadeScreenIn(1000);

                Prop planet = World.CreateProp(planetModel, spawn + (Vector3.RelativeFront * 5000) + (Vector3.WorldDown * 100), false, false);

                if (Entity.Exists(planet))
                {
                    planet.FreezePosition = true;
                    planetProp = planet;
                }

                Vehicle v = World.CreateVehicle("zanufo", spawn + Vector3.WorldDown * 100, 0);
                if (Entity.Exists(v))
                {
                    v.FreezePosition = true;
                    ufo = v;
                    pos = v.Position;
                }
            }

            public void Stop()
            {
                if (World.RenderingCamera == camera)
                    World.RenderingCamera = null;

                camera.Destroy();

                if (Entity.Exists(ufo))
                {
                    ufo.Delete();
                }

                if (Entity.Exists(planetProp))
                {
                    planetProp.Delete();
                }

                Function.Call(Hash.SET_STREAMED_TEXTURE_DICT_AS_NO_LONGER_NEEDED, TextureDict);
            }

            public void Update()
            {
                Function.Call(Hash.HIDE_HUD_AND_RADAR_THIS_FRAME);

                if (ufo == null)
                {
                    Complete = true;
                    return;
                }

                switch (step)
                {
                    case 0:
                        TimeCycleModifier.Set("CAMERA_secuirity", 1.0f);
                        camera.PointAt(ufo);
                        step++;
                        break;
                    case 1:
                        pos += Vector3.RelativeFront * 100 * Game.LastFrameTime;
                        ufo.PositionNoOffset = pos;
                        seconds += Game.LastFrameTime;
                        DrawUI();
                        if (seconds < 7f) return;
                        Complete = true;
                        break;
                }
            }

            private void DrawUI()
            {
                const float ImgWidth = 1536f;
                const float ImgHeight = 1024f;
                const float Width = (1f / 1920) / (1f / ImgWidth);
                const float Height = (1f / 1080) / (1f / ImgHeight);

                Function.Call(Hash.DRAW_SPRITE, TextureDict, TextureName, 0.5f, 0.5f, Width, Height, 0f, 255, 255, 255, 255);

                /////////////////////////////////////////////////////////////
                Function.Call(Hash.SET_TEXT_FONT, (int)Font.Monospace);
                Function.Call(Hash.SET_TEXT_SCALE, 0.3f, 0.3f);
                Function.Call(Hash.SET_TEXT_COLOUR, 255, 255, 255, 255);
                Function.Call(Hash.SET_TEXT_DROPSHADOW, 1, 1, 1, 1, 1);
                Function.Call(Hash.SET_TEXT_EDGE, 1, 1, 1, 1, 205);
                Function.Call(Hash.SET_TEXT_JUSTIFICATION, 1);
                Function.Call(Hash.SET_TEXT_WRAP, 0, Width);
                Function.Call(Hash._SET_TEXT_ENTRY, "CELL_EMAIL_BCON");
                Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, "UAC~n~VEHICLE IDENTIFICATION " + RandomString(5));
                Function.Call(Hash._DRAW_TEXT, 0.5f, 0.5f);
                /////////////////////////////////////////////////////////////

                /////////////////////////////////////////////////////////////
                Function.Call(Hash.SET_TEXT_FONT, (int)Font.ChaletComprimeCologne);
                Function.Call(Hash.SET_TEXT_SCALE, 0.3f, 0.3f);
                Function.Call(Hash.SET_TEXT_COLOUR, 255, 255, 255, 255);
                Function.Call(Hash.SET_TEXT_DROPSHADOW, 1, 1, 1, 1, 1);
                Function.Call(Hash.SET_TEXT_EDGE, 1, 1, 1, 1, 205);
                Function.Call(Hash.SET_TEXT_JUSTIFICATION, 1);
                Function.Call(Hash.SET_TEXT_WRAP, 0, Width);
                Function.Call(Hash._SET_TEXT_ENTRY, "CELL_EMAIL_BCON");
                Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, "MARS SAT CAM 11~n~" + World.CurrentDate.ToShortDateString());
                Function.Call(Hash._DRAW_TEXT, 0.15f, 0.075f);
                /////////////////////////////////////////////////////////////

                if (Entity.Exists(planetProp))
                {
                    Function.Call(Hash.SET_DRAW_ORIGIN, planetProp.Position.X, planetProp.Position.Y, planetProp.Position.Z, 0);

                    /////////////////////////////////////////////////////////////
                    Function.Call(Hash.SET_TEXT_FONT, (int)Font.Pricedown);
                    Function.Call(Hash.SET_TEXT_SCALE, 0.3f, 0.3f);
                    Function.Call(Hash.SET_TEXT_COLOUR, 255, 255, 255, 255);
                    Function.Call(Hash.SET_TEXT_DROPSHADOW, 1, 1, 1, 1, 1);
                    Function.Call(Hash.SET_TEXT_EDGE, 1, 1, 1, 1, 205);
                    Function.Call(Hash.SET_TEXT_JUSTIFICATION, 1);
                    Function.Call(Hash.SET_TEXT_WRAP, 0, Width);
                    Function.Call(Hash._SET_TEXT_ENTRY, "CELL_EMAIL_BCON");
                    Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, $"ORBITAL ID: MARS~n~AVG TEMP: 125c~n~CUR ORBITAL VEL: 24.{random.Next(0, 10)} km/s");
                    Function.Call(Hash._DRAW_TEXT, 0f, 0f);
                    /////////////////////////////////////////////////////////////

                    Function.Call(Hash.CLEAR_DRAW_ORIGIN);
                }
            }

            private string RandomString(int length)
            {
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                return new string(Enumerable.Repeat(chars, length)
                  .Select(s => s[random.Next(s.Length)]).ToArray());
            }
        }
    }
}
