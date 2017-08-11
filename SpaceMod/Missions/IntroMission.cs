using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using GTS.Library;
using GTS.Missions.Types;
using GTS.Scenarios;
using GTS.Scenes.Interiors;

namespace GTS.Missions
{
    internal class IntroMission : Scenario
    {
        private readonly float _colonelHeading = 313.5386f;

        private readonly Vector3 _colonelSpawn = new Vector3(-2356.895f, 3248.412f, 101.4508f);

        private readonly List<SatelliteDish> _dishes = new List<SatelliteDish>
        {
            new SatelliteDish(new Vector3(1965.244f, 2917.519f, 56.16845f),
                new Vector3(1964.9550f, 2916.5950f, 56.3010f), new Vector3(0, 0, 164.8802f), 155.5288f),
            new SatelliteDish(new Vector3(2001.014f, 2930.332f, 56.97068f),
                new Vector3(2000.7971f, 2929.3704f, 57.0959f), new Vector3(0, 0, 164.9973f), 161.4334f),
            new SatelliteDish(new Vector3(2049.844f, 2946.026f, 57.51732f),
                new Vector3(2050.7747f, 2945.7986f, 57.673f), new Vector3(0, 0, -110.9965f), 252.0444f),
            new SatelliteDish(new Vector3(2078.602f, 2945.41f, 56.41674f),
                new Vector3(2078.6648f, 2944.4685f, 56.5484f), new Vector3(0, 0, 179.05f), 178.4034f),
            new SatelliteDish(new Vector3(2106.831f, 2923.428f, 57.42712f),
                new Vector3(2106.5571f, 2922.5295f, 57.5847f), new Vector3(0, 0, 155.5398f), 159.3203f),
            new SatelliteDish(new Vector3(2136.944f, 2900.711f, 57.26347f),
                new Vector3(2136.4319f, 2899.8953f, 57.4265f), new Vector3(0, 0, 145.059f), 140.3007f)
        };

        private readonly Vector3 _humaneLabsEnterance = new Vector3(3574.148f, 3736.34f, 36.64266f);

        private readonly Vector3 _humaneLabsExit = new Vector3(3540.65f, 3675.77f, 28.12f);
        private Ped _colonel;

        private bool _dishesInitialized;

        private Blip _humaneLabsBlip;

        private Interior _humaneLabsIpl;

        private bool _isHumaneLabsMessageShown;

        private bool _isSatelliteMessageShown;
        private int _missionStep;

        public IntroMission()
        {
            Peds = new List<Ped>();
            Vehicles = new List<Vehicle>();
        }

        private Ped PlayerPed => Game.Player.Character;

        private List<Ped> Peds { get; }

        private List<Vehicle> Vehicles { get; }

        public override void OnAwake()
        {
        }

        public override void OnStart()
        {
            while (Game.IsLoading)
                Script.Yield();

            //_missionStep = 10;

            CreateColonel();

            _humaneLabsIpl = new Interior("v_lab");
        }

        private void CreateColonel()
        {
            _colonel?.Delete(); // in case the colonel already exists somehow.

            var m = new Model(PedHash.Marine01SMM);
            m.Request(5000);
            _colonel = World.CreatePed(m, _colonelSpawn - Vector3.WorldUp, _colonelHeading);
            if (_colonel == null)
                // Let's break here just in case.
                throw new NullReferenceException("Colonel returned null for IntroMission.");

            var b = new Blip(_colonel.AddBlip().Handle)
            {
                Sprite = BlipSprite.GTAOMission,
                Color = BlipColor.Blue,
                ShowRoute = true,
                Name = "Colonel Larson"
            };
            _colonel.SetDefaultClothes();
            _colonel.RelationshipGroup = PlayerPed.RelationshipGroup;
            _colonel.CanRagdoll = false;
        }

        public override void OnUpdate()
        {
            switch (_missionStep)
            {
                case 0:

                    var distToColonel = _colonel.Position.DistanceTo(PlayerPed.Position);
                    if (distToColonel > 1.75)
                        return;

                    World.DrawMarker(MarkerType.UpsideDownCone, _colonel.Position + Vector3.WorldUp * 1.5f,
                        Vector3.RelativeRight, Vector3.Zero, new Vector3(0.35f, 0.35f, 0.35f), Color.Red);
                    Utils.DisplayHelpTextWithGxt("END_LABEL_1");

                    if (Game.IsControlJustPressed(2, Control.Context))
                    {
                        Function.Call(Hash._PLAY_AMBIENT_SPEECH1, _colonel.Handle, "Generic_Hi", "Speech_Params_Force");

                        PlayerPed.FreezePosition = true;
                        PlayerPed.Heading = (_colonel.Position - PlayerPed.Position).ToHeading();
                        PlayerPed.Task.StandStill(-1);

                        Utils.ShowSubtitleWithGxt("INTRO_LABEL_1", 5000);
                        Script.Wait(5000);

                        Utils.ShowSubtitleWithGxt("INTRO_LABEL_2", 5000);
                        Script.Wait(5000);

                        Utils.ShowSubtitleWithGxt("INTRO_LABEL_3", 6000);
                        Script.Wait(6000);

                        Utils.ShowSubtitleWithGxt("INTRO_LABEL_4", 6000);
                        Script.Wait(6000);

                        Game.FadeScreenOut(1000);
                        Script.Wait(1000);
                        PlayerPed.Task.ClearAllImmediately();
                        PlayerPed.FreezePosition = false;
                        _colonel.Delete();
                        _colonel.CurrentBlip?.Remove();
                        Script.Wait(1000);
                        Game.FadeScreenIn(1000);
                        Script.Wait(1000);

                        _missionStep++;
                    }
                    break;

                case 1:

                    if (!_isSatelliteMessageShown)
                    {
                        Utils.ShowSubtitleWithGxt("INTRO_LABEL_5", 5000);
                        _isSatelliteMessageShown = true;
                    }

                    if (!_dishesInitialized)
                    {
                        _dishes.ForEach(dish =>
                        {
                            dish.CreateLaptop();
                            dish.CreateBlip();
                        });
                        _dishesInitialized = true;
                    }

                    _dishes.ForEach(x => x.Update());

                    foreach (var dish in _dishes)
                        if (!dish.CheckedForData)
                        {
                            var dist = dish.Position.DistanceTo(PlayerPed.Position);
                            if (dist > 1.75f)
                                continue;

                            Utils.DisplayHelpTextWithGxt("INTRO_LABEL_6");

                            // Ayyee that's pretty good. -Solla
                            if (Game.IsControlJustPressed(2, Control.Context))
                            {
                                PlayerPed.FreezePosition = true;
                                PlayerPed.Task.StandStill(-1);
                                PlayerPed.Position = dish.Position;
                                PlayerPed.Heading = dish.Heading;

                                var groundZ = new OutputArgument();
                                Function.Call(Hash.GET_GROUND_Z_FOR_3D_COORD, PlayerPed.Position.X,
                                    PlayerPed.Position.Y, PlayerPed.Position.Z, groundZ, false);
                                PlayerPed.Position = new Vector3(PlayerPed.Position.X, PlayerPed.Position.Y,
                                    groundZ.GetResult<float>());

                                PlayerPed.Task.PlayAnimation("missbigscore2aswitch", "switch_mic_car_fra_laptop_hacker",
                                    8f, -1, AnimationFlags.None);

                                Script.Wait(3000);

                                PlayerPed.FreezePosition = false;
                                PlayerPed.Task.ClearAll();
                                dish.CheckedForData = true;
                                dish.RemoveBlip();
                            }
                        }

                    _missionStep += _dishes.All(x => x.CheckedForData) ? 1 : 0;
                    break;

                case 2:

                    if (!_isHumaneLabsMessageShown)
                    {
                        ScaleFormMessages.Message.SHOW_MISSION_PASSED_MESSAGE(Game.GetGXTEntry("INTRO_LABEL_8"));
                        Script.Wait(2000);
                        Utils.ShowSubtitleWithGxt("INTRO_LABEL_7");
                        _isHumaneLabsMessageShown = true;
                    }

                    if (_humaneLabsBlip == null)
                        _humaneLabsBlip = new Blip(World.CreateBlip(_humaneLabsEnterance).Handle)
                        {
                            Color = BlipColor.Green,
                            Name = "Humane Labs",
                            ShowRoute = true
                        };

                    _missionStep++;

                    break;
                case 3:
                    if (PlayerPed.IsInVehicle())
                        return;
                    World.DrawMarker(MarkerType.VerticalCylinder, _humaneLabsEnterance - Vector3.WorldUp,
                        Vector3.RelativeRight, Vector3.Zero, new Vector3(0.5f, 0.5f, 0.5f), Color.Gold);
                    var distance = Vector3.Distance(PlayerPed.Position, _humaneLabsEnterance);
                    if (distance <= 1.5f)
                    {
                        Utils.DisplayHelpTextWithGxt(
                            "INTRO_LABEL_9"); // "Press ~INPUT_CONTEXT~ to enter/exit humane labs."

                        if (Game.IsControlJustPressed(2, Control.Context))
                        {
                            Game.FadeScreenOut(1);
                            _humaneLabsIpl.Request();
                            var timeout = DateTime.UtcNow + new TimeSpan(0, 0, 5);
                            while (!_humaneLabsIpl.IsActive)
                            {
                                Script.Yield();
                                if (DateTime.UtcNow > timeout)
                                    break;
                            }
                            PlayerPed.Position = _humaneLabsExit - Vector3.WorldUp;
                            PlayerPed.Heading = 173.5802f;
                            Game.FadeScreenIn(750);
                            _missionStep++;
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

                    _humaneLabsBlip?.Remove();

                    Peds.Add(World.CreatePed(PedHash.Marine02SMM, new Vector3(3534.057f, 3671.142f, 27.12115f),
                        331.006f));
                    Peds.Add(World.CreatePed(PedHash.Scientist01SMM, new Vector3(3539.069f, 3663.527f, 27.12188f),
                        172.762f));
                    Peds.Add(World.CreatePed(PedHash.Scientist01SMM, new Vector3(3534.83f, 3660.603f, 27.12189f),
                        316.3855f));
                    Peds.Add(World.CreatePed(PedHash.Scientist01SMM, new Vector3(3537.047f, 3664.484f, 27.12189f),
                        172.7052f));

                    StartScenarioChecked(Peds[0], "WORLD_HUMAN_GUARD_STAND"); // guard
                    StartScenarioChecked(Peds[1], "WORLD_HUMAN_CLIPBOARD");
                    StartScenarioChecked(Peds[2], "WORLD_HUMAN_CLIPBOARD");
                    StartScenarioChecked(Peds[3], "WORLD_HUMAN_AA_COFFEE");
                    var b = Peds[3]?.AddBlip();
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

                    _missionStep++;
                    break;
                case 5:

                    if (Peds[3] == null)
                    {
                        DeletePeds();
                        _missionStep--; // go back and request again.
                        return;
                    }

                    var mainScientist = Peds[3];

                    distance = Vector3.Distance(mainScientist.Position, PlayerPed.Position);
                    if (distance > 1.3f)
                        return;

                    Utils.DisplayHelpTextWithGxt("INTRO_LABEL_10"); // "Press INPUT_TALK to talk to the scientist".

                    if (Game.IsControlJustPressed(2, Control.Talk))
                    {
                        Function.Call(Hash._PLAY_AMBIENT_SPEECH1, mainScientist.Handle, "Generic_Thanks",
                            "Speech_Params_Force_Shouted_Critical");
                        mainScientist.Task.AchieveHeading((PlayerPed.Position - mainScientist.Position).ToHeading());
                        Script.Wait(1000);
                        Function.Call(Hash.PLAY_MISSION_COMPLETE_AUDIO, "FRANKLIN_BIG_01");
                        Script.Wait(250);
                        ScaleFormMessages.Message.SHOW_MISSION_PASSED_MESSAGE(Game.GetGXTEntry("INTRO_LABEL_11"));
                        Script.Wait(750);
                        Utils.ShowSubtitleWithGxt("INTRO_LABEL_12");
                        mainScientist.CurrentBlip?.Remove();
                        _missionStep++;
                    }

                    break;
                case 6:

                    CreateColonel();
                    _humaneLabsBlip?.Remove();
                    _humaneLabsBlip = new Blip(World.CreateBlip(_humaneLabsExit).Handle)
                    {
                        Color = BlipColor.Green,
                        Name = "Outside"
                    };

                    _missionStep++;

                    break;
                case 7:

                    World.DrawMarker(MarkerType.VerticalCylinder, _humaneLabsExit - Vector3.WorldUp,
                        Vector3.RelativeRight, Vector3.Zero, new Vector3(0.5f, 0.5f, 0.5f), Color.Gold);

                    distance = Vector3.Distance(PlayerPed.Position, _humaneLabsExit);
                    if (distance > 1.3f)
                        return;

                    Utils.DisplayHelpTextWithGxt("INTRO_LABEL_9"); // "Press ~INPUT_CONTEXT~ to enter/exit humane labs."

                    if (Game.IsControlJustPressed(2, Control.Context))
                    {
                        _humaneLabsIpl?.Remove();
                        Game.FadeScreenOut(1);
                        PlayerPed.Position = _humaneLabsEnterance - Vector3.WorldUp;
                        PlayerPed.Heading = -173.5802f;
                        _humaneLabsBlip?.Remove();
                        Peds?.ForEach(p => p?.Delete());
                        Script.Wait(750);
                        Game.FadeScreenIn(1000);
                        Utils.ShowSubtitleWithGxt("INTRO_LABEL_13");
                        _missionStep++;
                    }

                    break;
                case 8:

                    var pos = PlayerPed.CurrentVehicle?.Position ?? Vector3.Zero;
                    if (PlayerPed.IsInVehicle() && PlayerPed.Position.DistanceTo(_humaneLabsEnterance) > 200 &&
                        Function.Call<bool>(Hash.IS_POINT_ON_ROAD, pos.X, pos.Y, pos.Z, PlayerPed.CurrentVehicle))
                    {
                        Peds.Clear();
                        for (var i = 0; i < 4; i++)
                        {
                            var spawn = World.GetNextPositionOnStreet(PlayerPed.Position.Around(100), true);
                            if (spawn == Vector3.Zero || spawn.IsOnScreen())
                                continue;
                            var v = World.CreateVehicle(VehicleHash.Paradise, spawn);
                            Function.Call(Hash.SET_VEHICLE_ON_GROUND_PROPERLY, v);
                            if (v == null)
                                continue;
                            for (var j = 0; j < 2; j++)
                            {
                                var p = v.CreatePedOnSeat(j == 0 ? VehicleSeat.Driver : VehicleSeat.Passenger,
                                    PedHash.Hippy01AMY);
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

                        if (_colonel?.CurrentBlip != null)
                        {
                            _colonel.CurrentBlip.Alpha = 0;
                            _colonel.CurrentBlip.ShowRoute = false;
                        }

                        _missionStep++;
                    }

                    break;
                case 9:

                    Vehicles.ForEach(v =>
                    {
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

                    Peds.ForEach(p =>
                    {
                        if (!Entity.Exists(p))
                            return;

                        if (p.IsDead)
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
                        if (_colonel?.CurrentBlip != null)
                        {
                            _colonel.CurrentBlip.Alpha = 255;
                            _colonel.CurrentBlip.ShowRoute = true;
                        }
                        Peds.ForEach(p => p?.CurrentBlip?.Remove());
                        Vehicles.ForEach(v => v?.CurrentBlip?.Remove());

                        _missionStep++;
                    }

                    break;
                case 10:

                    distance = Vector3.Distance(PlayerPed.Position, _colonel.Position);
                    if (distance > 1.75f)
                        return;

                    World.DrawMarker(MarkerType.UpsideDownCone, _colonel.Position + Vector3.WorldUp * 1.5f,
                        Vector3.RelativeRight, Vector3.Zero, new Vector3(0.35f, 0.35f, 0.35f), Color.Red);
                    Utils.DisplayHelpTextWithGxt("END_LABEL_1");

                    if (Game.IsControlJustPressed(2, Control.Context))
                    {
                        PlayerPed.Heading = (_colonel.Position - PlayerPed.Position).ToHeading();
                        PlayerPed.Task.ChatTo(_colonel);
                        PlayerPed.Task.StandStill(-1);
                        _colonel.Task.ChatTo(PlayerPed);

                        Utils.ShowSubtitleWithGxt("INTRO_LABEL_14");
                        Script.Wait(7000);
                        Utils.ShowSubtitleWithGxt("INTRO_LABEL_15");
                        Script.Wait(7000);
                        Utils.ShowSubtitleWithGxt("INTRO_LABEL_16");
                        Script.Wait(7000);
                        Utils.ShowSubtitleWithGxt("INTRO_LABEL_17");
                        Script.Wait(7000);
                        Utils.ShowSubtitleWithGxt("INTRO_LABEL_18");
                        Script.Wait(2000);
                        Utils.ShowSubtitleWithGxt("INTRO_LABEL_19");
                        Script.Wait(4000);

                        Game.FadeScreenOut(1500);
                        Script.Wait(1500);
                        PlayerPed.Task.ClearAll();
                        EndScenario(true);
                        Game.FadeScreenIn(1500);

                        ScaleFormMessages.Message.SHOW_MISSION_PASSED_MESSAGE(
                            "~g~" + Game.GetGXTEntry("INTRO_LABEL_20"));
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
            if (_humaneLabsIpl.IsActive)
            {
                PlayerPed.Position = _humaneLabsEnterance;
                _humaneLabsIpl.Remove();
            }

            _dishes?.ForEach(x => x?.Aborted());
            Vehicles?.ForEach(v => v?.Delete());
            _humaneLabsBlip?.Remove();
            _colonel?.Delete();
            DeletePeds();
        }
    }
}