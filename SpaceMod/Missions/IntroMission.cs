using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Native;
using GTA.Math;
using System.Drawing;
using SpaceMod.Lib;
using SpaceMod.Missions.Objects;
using SpaceMod.Scenario;
using SpaceMod.Scenes.Interiors;
using System;

namespace SpaceMod.Missions
{
    public class IntroMission : CustomScenario
    {
        private int missionStep = 0;

        private Ped colonel;

        private bool dishesInitialized = false;

        private Vector3 colonelSpawn = new Vector3(-2356.895f, 3248.412f, 101.4508f);

        private float colonelHeading = 313.5386f;

        private Blip humaneLabsBlip;

        private Ipl humaneLabsIpl;

        private Vector3 humaneLabsEnterance = new Vector3(3574.148f, 3736.34f, 36.64266f);

        private Vector3 humaneLabsExit = new Vector3(3540.65f, 3675.77f, 28.12f);

        private bool isSatelliteMessageShown = false;

        private bool isHumaneLabsMessageShown = false;

        private List<SatelliteDish> dishes = new List<SatelliteDish>()
        {
            new SatelliteDish(new Vector3(1965.244f, 2917.519f, 56.16845f), new Vector3(1964.9550f, 2916.5950f, 56.3010f), new Vector3(0, 0, 164.8802f), 155.5288f),
            new SatelliteDish(new Vector3(2001.014f, 2930.332f, 56.97068f), new Vector3(2000.7971f, 2929.3704f, 57.0959f), new Vector3(0, 0, 164.9973f), 161.4334f),
            new SatelliteDish(new Vector3(2049.844f, 2946.026f, 57.51732f), new Vector3(2050.7747f, 2945.7986f, 57.673f), new Vector3(0, 0, -110.9965f), 252.0444f),
            new SatelliteDish(new Vector3(2078.602f, 2945.41f, 56.41674f), new Vector3(2078.6648f, 2944.4685f, 56.5484f), new Vector3(0, 0, 179.05f), 178.4034f),
            new SatelliteDish(new Vector3(2106.831f, 2923.428f, 57.42712f), new Vector3(2106.5571f, 2922.5295f, 57.5847f), new Vector3(0, 0, 155.5398f), 159.3203f),
            new SatelliteDish(new Vector3(2136.944f, 2900.711f, 57.26347f), new Vector3(2136.4319f, 2899.8953f, 57.4265f), new Vector3(0, 0, 145.059f), 140.3007f)
        };

        public IntroMission()
        {
            Peds = new List<Ped>();
            Vehicles = new List<Vehicle>();
        }

        private Ped PlayerPed => Game.Player.Character;

        private List<Ped> Peds { get; }

        private List<Vehicle> Vehicles { get; }

        public override void OnEnterScene() { }

        public override void Start()
        {
            while (Game.IsLoading)
                Script.Yield();

            CreateColonel();

            humaneLabsIpl = new Ipl("v_lab");
        }

        private void CreateColonel()
        {
            colonel?.Delete(); // in case the colonel already exists somehow.

            Model m = new Model(PedHash.Marine01SMM);
            m.Request(5000);
            colonel = World.CreatePed(m, colonelSpawn - Vector3.WorldUp, colonelHeading);
            if (colonel == null)
                // Let's break here just in case.
                throw new System.NullReferenceException("Colonel returned null for IntroMission.");

            Blip b = new Blip(colonel.AddBlip().Handle)
            {
                Sprite = BlipSprite.GTAOMission,
                Color = BlipColor.Blue,
                ShowRoute = true,
                Name = "Colonel Larson"
            };
            colonel.SetDefaultClothes();
            colonel.RelationshipGroup = PlayerPed.RelationshipGroup;
            colonel.CanRagdoll = false;
        }

        public override void OnUpdate()
        {
            switch (missionStep)
            {
                case 0:

                    float distToColonel = colonel.Position.DistanceTo(PlayerPed.Position);
                    if (distToColonel > 1.75)
                        return;

                    World.DrawMarker(MarkerType.UpsideDownCone, colonel.Position + Vector3.WorldUp * 1.5f, Vector3.RelativeRight, Vector3.Zero, new Vector3(0.35f, 0.35f, 0.35f), Color.Red);
                    SpaceModLib.DisplayHelpTextWithGXT("END_LABEL_1");

                    if (Game.IsControlJustPressed(2, Control.Context))
                    {
                        Function.Call(Hash._PLAY_AMBIENT_SPEECH1, colonel.Handle, "Generic_Hi", "Speech_Params_Force");

                        PlayerPed.FreezePosition = true;
                        PlayerPed.Heading = (colonel.Position - PlayerPed.Position).ToHeading();
                        PlayerPed.Task.StandStill(-1);

                        SpaceModLib.ShowSubtitleWithGXT("INTRO_LABEL_1", 5000);
                        Script.Wait(5000);

                        SpaceModLib.ShowSubtitleWithGXT("INTRO_LABEL_2", 5000);
                        Script.Wait(5000);

                        SpaceModLib.ShowSubtitleWithGXT("INTRO_LABEL_3", 6000);
                        Script.Wait(6000);

                        SpaceModLib.ShowSubtitleWithGXT("INTRO_LABEL_4", 6000);
                        Script.Wait(6000);

                        Game.FadeScreenOut(1000);
                        Script.Wait(1000);
                        PlayerPed.Task.ClearAllImmediately();
                        PlayerPed.FreezePosition = false;
                        colonel.Delete();
                        colonel.CurrentBlip?.Remove();
                        Script.Wait(1000);
                        Game.FadeScreenIn(1000);
                        Script.Wait(1000);

                        missionStep++;
                    }
                    break;

                case 1:

                    if (!isSatelliteMessageShown)
                    {
                        SpaceModLib.ShowSubtitleWithGXT("INTRO_LABEL_5", 5000);
                        isSatelliteMessageShown = true;
                    }

                    if (!dishesInitialized)
                    {
                        dishes.ForEach(dish => { dish.CreateLaptop(); dish.CreateBlip(); });
                        dishesInitialized = true;
                    }

                    dishes.ForEach(x => x.Update());

                    foreach (SatelliteDish dish in dishes)
                    {
                        if (!dish.CheckedForData)
                        {
                            float dist = dish.Position.DistanceTo(PlayerPed.Position);
                            if (dist > 1.75f)
                                continue;

                            SpaceModLib.DisplayHelpTextWithGXT("INTRO_LABEL_6");

                            // Ayyee that's pretty good. -Solla
                            if (Game.IsControlJustPressed(2, Control.Context))
                            {
                                PlayerPed.FreezePosition = true;
                                PlayerPed.Task.StandStill(-1);
                                PlayerPed.Position = dish.Position;
                                PlayerPed.Heading = dish.Heading;

                                OutputArgument groundZ = new OutputArgument();
                                Function.Call(Hash.GET_GROUND_Z_FOR_3D_COORD, PlayerPed.Position.X, PlayerPed.Position.Y, PlayerPed.Position.Z, groundZ, false);
                                PlayerPed.Position = new Vector3(PlayerPed.Position.X, PlayerPed.Position.Y, groundZ.GetResult<float>());

                                PlayerPed.Task.PlayAnimation("missbigscore2aswitch", "switch_mic_car_fra_laptop_hacker", 8f, -1, AnimationFlags.None);

                                Script.Wait(3000);

                                PlayerPed.FreezePosition = false;
                                PlayerPed.Task.ClearAll();
                                dish.CheckedForData = true;
                                dish.RemoveBlip();
                            }
                        }
                    }

                    missionStep += (dishes.All(x => x.CheckedForData)) ? 1 : 0;
                    break;

                case 2:

                    if (!isHumaneLabsMessageShown)
                    {
                        ScaleFormMessages.Message.SHOW_MISSION_PASSED_MESSAGE(Game.GetGXTEntry("INTRO_LABEL_8"));
                        Script.Wait(2000);
                        SpaceModLib.ShowSubtitleWithGXT("INTRO_LABEL_7");
                        isHumaneLabsMessageShown = true;
                    }

                    if (humaneLabsBlip == null)
                        humaneLabsBlip = new Blip(World.CreateBlip(humaneLabsEnterance).Handle)
                        {
                            Color = BlipColor.Green,
                            Name = "Humane Labs",
                            ShowRoute = true
                        };

                    missionStep++;

                    break;
                case 3:
                    if (PlayerPed.IsInVehicle())
                        return;
                    World.DrawMarker(MarkerType.VerticalCylinder, humaneLabsEnterance - Vector3.WorldUp, Vector3.RelativeRight, Vector3.Zero, new Vector3(0.5f, 0.5f, 0.5f), Color.Gold);
                    float distance = Vector3.Distance(PlayerPed.Position, humaneLabsEnterance);
                    if (distance <= 1.5f)
                    {
                        SpaceModLib.DisplayHelpTextWithGXT("INTRO_LABEL_9"); // "Press ~INPUT_CONTEXT~ to enter/exit humane labs."

                        if (Game.IsControlJustPressed(2, Control.Context))
                        {
                            Game.FadeScreenOut(1);
                            humaneLabsIpl.Request();
                            DateTime timeout = DateTime.UtcNow + new TimeSpan(0, 0, 5);
                            while (!humaneLabsIpl.IsActive)
                            {
                                Script.Yield();
                                if (DateTime.UtcNow > timeout)
                                    break;
                            }
                            PlayerPed.Position = humaneLabsExit - Vector3.WorldUp;
                            PlayerPed.Heading = 173.5802f;
                            Game.FadeScreenIn(750);
                            missionStep++;
                        }
                    }

                    break;

                case 4:

                    // -- GUARDS --
                    // X:3534.057 Y:3671.142 Z:28.12115

                    // -- SCIENTISTS --
                    // X:3539.069 Y:3663.527 Z:28.12188
                    // X:3534.83 Y:3660.603 Z:28.12189
                    // X:3537.047 Y:3664.484 Z:28.12189 -- Coffee

                    humaneLabsBlip?.Remove();

                    Peds.Add(World.CreatePed(PedHash.Marine02SMM, new Vector3(3534.057f, 3671.142f, 27.12115f), 331.006f));
                    Peds.Add(World.CreatePed(PedHash.Scientist01SMM, new Vector3(3539.069f, 3663.527f, 27.12188f), 172.762f));
                    Peds.Add(World.CreatePed(PedHash.Scientist01SMM, new Vector3(3534.83f, 3660.603f, 27.12189f), 316.3855f));
                    Peds.Add(World.CreatePed(PedHash.Scientist01SMM, new Vector3(3537.047f, 3664.484f, 27.12189f), 172.7052f));

                    StartScenarioChecked(Peds[0], "WORLD_HUMAN_GUARD_STAND"); // guard
                    StartScenarioChecked(Peds[1], "WORLD_HUMAN_CLIPBOARD");
                    StartScenarioChecked(Peds[2], "WORLD_HUMAN_CLIPBOARD");
                    StartScenarioChecked(Peds[3], "WORLD_HUMAN_AA_COFFEE");
                    Blip b = Peds[3]?.AddBlip();
                    if (b != null)
                    {
                        b.Sprite = BlipSprite.Friend;
                        b.Name = "Scientist";
                        b.Color = BlipColor.Blue;
                    }

                    Peds.ForEach(p =>
                    {
                        if (p == null)
                            return;

                        p.CanRagdoll = false;
                        p.RelationshipGroup = PlayerPed.RelationshipGroup;
                    });

                    missionStep++;
                    break;
                case 5:

                    if (Peds[3] == null)
                    {
                        DeletePeds();
                        missionStep--; // go back and request again.
                        return;
                    }

                    Ped mainScientist = Peds[3];

                    distance = Vector3.Distance(mainScientist.Position, PlayerPed.Position);
                    if (distance > 1.3f)
                        return;

                    SpaceModLib.DisplayHelpTextWithGXT("INTRO_LABEL_10"); // "Press INPUT_TALK to talk to the scientist".

                    if (Game.IsControlJustPressed(2, Control.Talk))
                    {
                        Function.Call(Hash._PLAY_AMBIENT_SPEECH1, mainScientist.Handle, "Generic_Thanks", "Speech_Params_Force_Shouted_Critical");
                        mainScientist.Task.AchieveHeading((PlayerPed.Position - mainScientist.Position).ToHeading());
                        Script.Wait(1000);
                        Function.Call(Hash.PLAY_MISSION_COMPLETE_AUDIO, "FRANKLIN_BIG_01");
                        Script.Wait(250);
                        ScaleFormMessages.Message.SHOW_MISSION_PASSED_MESSAGE(Game.GetGXTEntry("INTRO_LABEL_11"));
                        Script.Wait(750);
                        SpaceModLib.ShowSubtitleWithGXT("INTRO_LABEL_12");
                        mainScientist.CurrentBlip?.Remove();
                        missionStep++;
                    }

                    break;
                case 6:

                    CreateColonel();
                    humaneLabsBlip?.Remove();
                    humaneLabsBlip = new Blip(World.CreateBlip(humaneLabsExit).Handle)
                    {
                        Color = BlipColor.Green,
                        Name = "Outside"
                    };

                    missionStep++;

                    break;
                case 7:

                    World.DrawMarker(MarkerType.VerticalCylinder, humaneLabsExit - Vector3.WorldUp, Vector3.RelativeRight, Vector3.Zero, new Vector3(0.5f, 0.5f, 0.5f), Color.Gold);

                    distance = Vector3.Distance(PlayerPed.Position, humaneLabsExit);
                    if (distance > 1.3f)
                        return;

                    SpaceModLib.DisplayHelpTextWithGXT("INTRO_LABEL_9"); // "Press ~INPUT_CONTEXT~ to enter/exit humane labs."

                    if (Game.IsControlJustPressed(2, Control.Context))
                    {
                        humaneLabsIpl?.Remove();
                        Game.FadeScreenOut(1);
                        PlayerPed.Position = humaneLabsEnterance - Vector3.WorldUp;
                        PlayerPed.Heading = -173.5802f;
                        humaneLabsBlip?.Remove();
                        Peds.ForEach(p => p.Delete());
                        Script.Wait(750);
                        Game.FadeScreenIn(1000);
                        SpaceModLib.ShowSubtitleWithGXT("INTRO_LABEL_13");
                        missionStep++;
                    }

                    break;
                case 8:

                    Vector3 pos = PlayerPed.CurrentVehicle?.Position ?? Vector3.Zero;
                    if (PlayerPed.IsInVehicle() && PlayerPed.Position.DistanceTo(humaneLabsEnterance) > 200 && Function.Call<bool>(Hash.IS_POINT_ON_ROAD, pos.X, pos.Y, pos.Z, PlayerPed.CurrentVehicle))
                    {
                        Peds.Clear();
                        for (int i = 0; i < 4; i++)
                        {
                            Vector3 spawn = World.GetNextPositionOnStreet(PlayerPed.Position.Around(100), true);
                            if (spawn == Vector3.Zero || spawn.IsOnScreen())
                                continue;
                            Vehicle v = World.CreateVehicle(VehicleHash.Paradise, spawn);
                            Function.Call(Hash.SET_VEHICLE_ON_GROUND_PROPERLY, v);
                            if (v == null)
                                continue;
                            for (int j = 0; j < 2; j++)
                            {
                                Ped p = v.CreatePedOnSeat(j == 0 ? VehicleSeat.Driver : VehicleSeat.Passenger, PedHash.Hippy01AMY);
                                if (p == null)
                                    continue;
                                p.IsEnemy = true;
                                p.Weapons.Give(WeaponHash.Pistol, 10, true, true);
                                if (v.Driver == p)
                                    p.Task.VehicleChase(PlayerPed);
                                p.RelationshipGroup = Game.GenerateHash("HATES_PLAYER");
                                p.AddBlip().Scale = 0.7f;
                                Peds.Add(p);
                            }
                            v.AddBlip();
                            Vehicles.Add(v);
                        }

                        if (colonel?.CurrentBlip != null)
                        {
                            colonel.CurrentBlip.Alpha = 0;
                            colonel.CurrentBlip.ShowRoute = false;
                        }

                        missionStep++;
                    }

                    break;
                case 9:

                    Vehicles.ForEach(v => {
                        if (v == null || !v.Exists() || v.IsDead)
                            return;

                        v.CurrentBlip.Alpha = v.Passengers.Length > 0 && v.Passengers.All(x => !x.IsDead) ? 255 : 0;

                        if (v.Position.DistanceTo(PlayerPed.Position) > 300)
                        {
                            v.Passengers?.ToList()?.ForEach(p => p?.Delete());
                            v.Driver?.Delete();
                            v.Delete();
                        }
                    });

                    Peds.ForEach(p => {
                        if (p == null || !p.Exists() || p.IsDead)
                        {
                            if (Blip.Exists(p.CurrentBlip))
                                p.CurrentBlip.Remove();
                            return;
                        }

                        if (!p.IsInVehicle() && p.Position.DistanceTo(PlayerPed.Position) > 300)
                        {
                            p.Delete();
                            return;
                        }

                        p.CurrentBlip.Alpha = p.IsInVehicle() || p.IsDead ? 0 : 255;
                    });

                    if (Peds.All(p => p?.IsDead ?? true))
                    {
                        if (colonel?.CurrentBlip != null)
                        {
                            colonel.CurrentBlip.Alpha = 255;
                            colonel.CurrentBlip.ShowRoute = true;
                        }
                        Peds.ForEach(p => p.CurrentBlip?.Remove());
                        Vehicles.ForEach(v => v.CurrentBlip?.Remove());

                        missionStep++;
                    }

                    break;
                case 10:

                    distance = Vector3.Distance(PlayerPed.Position, colonel.Position);
                    if (distance > 1.75f)
                        return;

                    World.DrawMarker(MarkerType.UpsideDownCone, colonel.Position + Vector3.WorldUp * 1.5f, Vector3.RelativeRight, Vector3.Zero, new Vector3(0.35f, 0.35f, 0.35f), Color.Red);
                    SpaceModLib.DisplayHelpTextWithGXT("END_LABEL_1");

                    if (Game.IsControlJustPressed(2, Control.Context))
                    {
                        // TODO: Get briefed.
                    }

                    break;
            }
        }

        private void DeletePeds()
        {
            Peds?.ForEach(p => p?.Delete()); // delete the other peds.
        }

        private void StartScenarioChecked(Ped ped, string name)
        {
            if (ped == null)
                return;

            Function.Call(Hash.TASK_START_SCENARIO_IN_PLACE, ped, name, -1, false);
        }

        public override void OnAborted()
        {
            CleanUp();
        }

        public override void OnEnded(bool success)
        {
            CleanUp();
        }

        private void CleanUp()
        {
            if (humaneLabsIpl.IsActive)
            {
                PlayerPed.Position = humaneLabsEnterance;
                humaneLabsIpl.Remove();
            }

            dishes?.ForEach(x => x?.Aborted());
            Vehicles?.ForEach(v => v?.Delete());
            humaneLabsBlip?.Remove();
            colonel?.Delete();
            DeletePeds();
        }
    }
}
