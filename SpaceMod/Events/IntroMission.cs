using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using GTS.Events.DataClasses;
using GTS.Library;
using GTS.Scenes;

namespace GTS.Events
{
    /// <summary>
    ///     Credits: AHK1221
    /// </summary>
    internal class IntroMission : Scenario
    {
        private readonly float _colonelHeading = 53.71681f;
        private readonly Vector3 _colonelSpawn = new Vector3(-6543.538f, -1395.759f, 97.04076f);

        private readonly List<SatelliteDish> _dishes = new List<SatelliteDish>
        {
            new SatelliteDish(new Vector3(1965.244f, 2917.519f, 56.16845f),
                new Vector3(1964.9550f, 2916.5950f, 56.3010f), new Vector3(0, 0, 164.8802f), 155.5288f),
            new SatelliteDish(new Vector3(2049.844f, 2946.026f, 57.51732f),
                new Vector3(2050.7747f, 2945.7986f, 57.673f), new Vector3(0, 0, -110.9965f), 252.0444f),
            new SatelliteDish(new Vector3(2106.831f, 2923.428f, 57.42712f),
                new Vector3(2106.5571f, 2922.5295f, 57.5847f), new Vector3(0, 0, 155.5398f), 159.3203f)
        };

        private readonly Vector3 _dishesArea = new Vector3(1965.244f, 2917.519f, 56.16845f);
        private readonly Vector3 _humaneLabsEnterance = new Vector3(3574.148f, 3736.34f, 36.64266f);
        private readonly Vector3 _humaneLabsExit = new Vector3(3540.65f, 3675.77f, 28.12f);

        private Ped _colonel;
        private Blip _dishesAreaBlip;
        private bool _dishesInitialized;
        private Blip _humaneLabsBlip;
        private bool _isHumaneLabsMessageShown;

        private bool _isSatelliteMessageShown;
        private int _missionStep;

        public IntroMission()
        {
            Peds = new List<Ped>();
        }

        private List<Ped> Peds { get; }
        public bool DidStart { get; set; }

        public void Awake()
        {
        }

        public void Start()
        {
            while (Game.IsLoading)
                Script.Yield();

            CreateColonel();
        }

        public void Update()
        {
            if (PlayerPed.IsDead)
                EndScenario(false);

            switch (_missionStep)
            {
                case 0:
                    var distToColonel = _colonel.Position.DistanceTo(PlayerPed.Position);
                    if (distToColonel > 1.75) return;
                    World.DrawMarker(MarkerType.UpsideDownCone, _colonel.Position + Vector3.WorldUp * 1.5f,
                        Vector3.RelativeRight, Vector3.Zero, new Vector3(0.35f, 0.35f, 0.35f), Color.Gold);
                    GtsLibNet.DisplayHelpTextWithGxt("END_LABEL_1");
                    if (Game.IsControlJustPressed(2, Control.Context))
                    {
                        Function.Call(Hash._PLAY_AMBIENT_SPEECH1, _colonel.Handle, "Generic_Hi", "Speech_Params_Force");
                        PlayerPed.Heading = (_colonel.Position - PlayerPed.Position).ToHeading();
                        PlayerPed.Task.ChatTo(_colonel);
                        PlayerPed.Task.StandStill(-1);
                        _colonel.Task.ChatTo(PlayerPed);
                        GtsLibNet.ShowSubtitleWithGxt("INTRO_LABEL_1", 5000);
                        Script.Wait(3000);
                        GtsLibNet.ShowSubtitleWithGxt("INTRO_LABEL_2", 5000);
                        Script.Wait(3000);
                        GtsLibNet.ShowSubtitleWithGxt("INTRO_LABEL_3", 6000);
                        Script.Wait(3000);
                        GtsLibNet.ShowSubtitleWithGxt("INTRO_LABEL_4", 6000);
                        Script.Wait(3000);
                        Game.FadeScreenOut(1000);
                        Script.Wait(1000);
                        PlayerPed.Task.ClearAllImmediately();
                        PlayerPed.FreezePosition = false;
                        _colonel.Delete();
                        _colonel.CurrentBlip?.Remove();
                        Script.Wait(1000);
                        Game.FadeScreenIn(1000);
                        Script.Wait(1000);
                        Core.HeliTransport?.ShowHelp();
                        _missionStep++;
                    }
                    break;
                case 1:
                    DidStart = true;
                    if (!_isSatelliteMessageShown)
                    {
                        GtsLibNet.ShowSubtitleWithGxt("INTRO_LABEL_5", 5000);
                        _isSatelliteMessageShown = true;
                    }
                    if (!_dishesInitialized)
                    {
                        if (!Blip.Exists(_dishesAreaBlip))
                        {
                            _dishesAreaBlip = World.CreateBlip(_dishesArea, 200);
                            _dishesAreaBlip.ShowRoute = true;
                            _dishesAreaBlip.Alpha = 155;
                            _dishesAreaBlip.Color = BlipColor.Yellow;
                        }
                        var dist = PlayerPed.Position.DistanceToSquared(_dishesArea);
                        if (dist > 40000) return;
                        _dishesAreaBlip?.Remove();
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
                            if (dist > 1.75f) continue;
                            GtsLibNet.DisplayHelpTextWithGxt("INTRO_LABEL_6");
                            if (!Game.IsControlJustPressed(2, Control.Context)) continue;
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
                                4f, -1, AnimationFlags.None);

                            Script.Wait(3000);

                            PlayerPed.FreezePosition = false;
                            PlayerPed.Task.ClearAll();
                            dish.CheckedForData = true;
                            dish.RemoveBlip();
                        }
                    _missionStep += _dishes.All(x => x.CheckedForData) ? 1 : 0;
                    break;
                case 2:
                    if (!_isHumaneLabsMessageShown)
                    {
                        Function.Call(Hash.PLAY_MISSION_COMPLETE_AUDIO, "FRANKLIN_BIG_01");
                        while (!Function.Call<bool>(Hash.IS_MISSION_COMPLETE_PLAYING))
                            Script.Yield();
                        ScaleFormMessages.Message.SHOW_MISSION_PASSED_MESSAGE(Game.GetGXTEntry("INTRO_LABEL_8"));
                        Effects.Start(ScreenEffect.SuccessNeutral, 5000);
                        Script.Wait(4500);
                        GtsLibNet.ShowSubtitleWithGxt("INTRO_LABEL_7");
                        _isHumaneLabsMessageShown = true;
                    }
                    if (_humaneLabsBlip == null)
                        _humaneLabsBlip = new Blip(World.CreateBlip(_humaneLabsEnterance).Handle)
                        {
                            Color = BlipColor.Yellow,
                            Name = "Humane Labs",
                            ShowRoute = true
                        };
                    _missionStep++;
                    break;
                case 3:
                    if (PlayerPed.IsInVehicle()) return;
                    World.DrawMarker(MarkerType.VerticalCylinder, _humaneLabsEnterance - Vector3.WorldUp,
                        Vector3.RelativeRight, Vector3.Zero, new Vector3(0.5f, 0.5f, 0.5f), Color.Gold);
                    var distance = Vector3.Distance(PlayerPed.Position, _humaneLabsEnterance);
                    if (distance <= 1.5f)
                    {
                        GtsLibNet.DisplayHelpTextWithGxt(
                            "INTRO_LABEL_9"); // "Press ~INPUT_CONTEXT~ to enter/exit humane labs."

                        if (Game.IsControlJustPressed(2, Control.Context))
                        {
                            Game.FadeScreenOut(1);
                            PlayerPed.Position = _humaneLabsExit - Vector3.WorldUp;
                            PlayerPed.Heading = 173.5802f;
                            Game.FadeScreenIn(750);
                            _missionStep++;
                        }
                    }
                    break;
                case 4:
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
                        b.Name = "Scientist";
                        b.Color = BlipColor.Yellow;
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
                    if (distance > 1.3f) return;
                    GtsLibNet.DisplayHelpTextWithGxt("INTRO_LABEL_10"); // "Press INPUT_TALK to talk to the scientist".
                    if (Game.IsControlJustPressed(2, Control.Talk))
                    {
                        Function.Call(Hash._PLAY_AMBIENT_SPEECH1, mainScientist.Handle, "Generic_Thanks",
                            "Speech_Params_Force_Shouted_Critical");
                        mainScientist.Task.AchieveHeading((PlayerPed.Position - mainScientist.Position).ToHeading());
                        Script.Wait(1000);
                        Function.Call(Hash.PLAY_MISSION_COMPLETE_AUDIO, "FRANKLIN_BIG_01");
                        while (!Function.Call<bool>(Hash.IS_MISSION_COMPLETE_PLAYING))
                            Script.Yield();
                        ScaleFormMessages.Message.SHOW_MISSION_PASSED_MESSAGE(Game.GetGXTEntry("INTRO_LABEL_11"));
                        Effects.Start(ScreenEffect.SuccessNeutral, 5000);
                        Script.Wait(750);
                        GtsLibNet.ShowSubtitleWithGxt("INTRO_LABEL_12");
                        mainScientist.CurrentBlip?.Remove();
                        _missionStep++;
                    }
                    break;
                case 6:
                    CreateColonel();
                    _humaneLabsBlip?.Remove();
                    _humaneLabsBlip = new Blip(World.CreateBlip(_humaneLabsExit).Handle)
                    {
                        Color = BlipColor.Yellow,
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
                    GtsLibNet.DisplayHelpTextWithGxt(
                        "INTRO_LABEL_9"); // "Press ~INPUT_CONTEXT~ to enter/exit humane labs."
                    if (Game.IsControlJustPressed(2, Control.Context))
                    {
                        Game.FadeScreenOut(1);
                        PlayerPed.Position = _humaneLabsEnterance - Vector3.WorldUp;
                        PlayerPed.Heading = -173.5802f;
                        _humaneLabsBlip?.Remove();
                        Peds?.ForEach(p => p?.Delete());
                        Script.Wait(750);
                        Game.FadeScreenIn(1000);
                        GtsLibNet.ShowSubtitleWithGxt("INTRO_LABEL_13");
                        _missionStep++;
                    }
                    break;
                case 8:
                    distance = PlayerPed.Position.DistanceToSquared(_colonel.Position);
                    if (distance > 3f) return;
                    World.DrawMarker(MarkerType.UpsideDownCone, _colonel.Position + Vector3.WorldUp * 1.5f,
                        Vector3.RelativeRight, Vector3.Zero, new Vector3(0.35f, 0.35f, 0.35f), Color.Gold);
                    GtsLibNet.DisplayHelpTextWithGxt("END_LABEL_1");

                    if (Game.IsControlJustPressed(2, Control.Context))
                    {
                        PlayerPed.Heading = (_colonel.Position - PlayerPed.Position).ToHeading();
                        PlayerPed.Task.ChatTo(_colonel);
                        PlayerPed.Task.StandStill(-1);
                        _colonel.Task.ChatTo(PlayerPed);
                        GtsLibNet.ShowSubtitleWithGxt("INTRO_LABEL_14");
                        Script.Wait(5000);
                        GtsLibNet.ShowSubtitleWithGxt("INTRO_LABEL_15");
                        Script.Wait(5000);
                        GtsLibNet.ShowSubtitleWithGxt("INTRO_LABEL_16");
                        Script.Wait(5000);
                        GtsLibNet.ShowSubtitleWithGxt("INTRO_LABEL_17");
                        Script.Wait(5000);
                        GtsLibNet.ShowSubtitleWithGxt("INTRO_LABEL_18");
                        Script.Wait(2000);
                        Function.Call(Hash._PLAY_AMBIENT_SPEECH1, PlayerPed.Handle, "Generic_Thanks",
                            "Speech_Params_Force");
                        GtsLibNet.ShowSubtitleWithGxt("INTRO_LABEL_19");
                        Script.Wait(4000);
                        Game.FadeScreenOut(1500);
                        Script.Wait(1500);
                        PlayerPed.Task.ClearAll();
                        _colonel?.Delete();
                        Game.FadeScreenIn(1500);
                        Function.Call(Hash.PLAY_MISSION_COMPLETE_AUDIO, "FRANKLIN_BIG_01");
                        while (!Function.Call<bool>(Hash.IS_MISSION_COMPLETE_PLAYING))
                            Script.Yield();
                        ScaleFormMessages.Message.SHOW_MISSION_PASSED_MESSAGE(Game.GetGXTEntry("INTRO_LABEL_20"));
                        Effects.Start(ScreenEffect.SuccessNeutral, 5000);
                        Script.Wait(4500);
                        UI.ShowSubtitle(Game.GetGXTEntry("GO_TO") + " ~p~Space~s~.");
                        EndScenario(true);
                    }
                    break;
            }
        }

        private static void StartScenarioChecked(Ped ped, string name)
        {
            if (ped == null) return;
            Function.Call(Hash.TASK_START_SCENARIO_IN_PLACE, ped, name, -1, false);
        }

        public void OnAborted()
        {
            CleanUp();
        }

        public void OnDisable(bool success)
        {
            CleanUp();
        }

        private void CleanUp()
        {
            _dishes?.ForEach(x => x?.Aborted());
            _dishesAreaBlip?.Remove();
            _humaneLabsBlip?.Remove();
            _colonel?.Delete();
            DeletePeds();
        }

        private void CreateColonel()
        {
            _colonel?.Delete(); // in case the colonel already exists somehow.
            var m = new Model(PedHash.Marine01SMM);
            m.Request();
            while (!m.IsLoaded)
                Script.Yield();
            _colonel = World.CreatePed(m, _colonelSpawn - Vector3.WorldUp, _colonelHeading);
            if (_colonel == null)
                // Let's break here just in case.
                throw new NullReferenceException("Colonel returned null for IntroMission.");
            var b = _colonel.AddBlip();
            b.Sprite = BlipSprite.GTAOMission;
            b.Color = Scene.MarkerBlipColor;
            b.Name = "Colonel Larson";
            _colonel.SetDefaultClothes();
            _colonel.RelationshipGroup = PlayerPed.RelationshipGroup;
            _colonel.CanRagdoll = false;
            m.MarkAsNoLongerNeeded();
        }

        private void DeletePeds()
        {
            Peds?.ForEach(p => p?.Delete());
        }
    }
}