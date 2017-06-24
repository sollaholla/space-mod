using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Native;
using GTA.Math;
using System.Drawing;
using SpaceMod;
using SpaceMod.Lib;

namespace SpaceMod
{
    public class IntroMission : SpaceMod.Scenario.CustomScenario
    {
        private Ped PlayerPed => Game.Player.Character;

        private int MissionStep = 0;

        private Ped Colonel;

        private bool blipsCreated = false;

        private List<SatelliteDish> dishes = new List<SatelliteDish>()
        {
            new SatelliteDish(new Vector3(1965.244f, 2917.519f, 56.16845f), new Vector3(1964.9550f, 2916.5950f, 56.3010f), new Vector3(0, 0, 164.8802f), 155.5288f),
            new SatelliteDish(new Vector3(2001.014f, 2930.332f, 56.97068f), new Vector3(2000.7971f, 2929.3704f, 57.0959f), new Vector3(0, 0, 164.9973f), 161.4334f),
            new SatelliteDish(new Vector3(2049.844f, 2946.026f, 57.51732f), new Vector3(2050.7747f, 2945.7986f, 57.673f), new Vector3(0, 0, -110.9965f), 252.0444f),
            new SatelliteDish(new Vector3(2078.602f, 2945.41f, 56.41674f), new Vector3(2078.6648f, 2944.4685f, 56.5484f), new Vector3(0, 0, 179.05f), 178.4034f),
            new SatelliteDish(new Vector3(2106.831f, 2923.428f, 57.42712f), new Vector3(2106.5571f, 2922.5295f, 57.5847f), new Vector3(0, 0, 155.5398f), 159.3203f),
            new SatelliteDish(new Vector3(2136.944f, 2900.711f, 57.26347f), new Vector3(2136.4319f, 2899.8953f, 57.4265f), new Vector3(0, 0, 145.059f), 140.3007f)
        };

        public IntroMission(Ped colonel)
        {
            Colonel = colonel;
        }

        public override void OnEnterScene() { }

        public override void Start()
        {
            //UI.ShowSubtitle("Go to the ~y~satellite dishes~w~ marked on your map, and get their data.");
            Core.Instance.SetMissionInProgress(true);
        }

        bool isSatelliteMessageShown = false;
        bool isHumaneLabsMessageShown = false;
        public override void OnUpdate()
        {
            switch(MissionStep)
            {
                case 0:

                    float distToColonel = Colonel.Position.DistanceTo(PlayerPed.Position);
                    if (distToColonel > 1.75)
                        return;

                    World.DrawMarker(MarkerType.UpsideDownCone, Colonel.Position + Vector3.WorldUp * 1.5f, Vector3.RelativeRight, Vector3.Zero, new Vector3(0.35f, 0.35f, 0.35f), Color.Red);
                    SpaceMod.Lib.SpaceModLib.DisplayHelpTextWithGXT("END_LABEL_1");

                    if (Game.IsControlJustPressed(2, GTA.Control.Context))
                    {
                        Function.Call(Hash._PLAY_AMBIENT_SPEECH1, Colonel.Handle, "Generic_Hi", "Speech_Params_Force");
                        PlayerPed.FreezePosition = true;
                        PlayerPed.Heading = (Colonel.Position - PlayerPed.Position).ToHeading();
                        PlayerPed.Task.StandStill(-1);

                        while (Entity.Exists(Colonel))
                        {
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
                            Colonel.Delete();
                            Colonel.CurrentBlip?.Remove();
                            Script.Wait(1000);
                            Game.FadeScreenIn(1000);
                            Script.Wait(1000);

                            MissionStep++;

                            Script.Yield();
                        }
                    }
                    break;

                case 1:

                    if(!isSatelliteMessageShown)
                    {
                        SpaceModLib.ShowSubtitleWithGXT("INTRO_LABEL_5", 5000);
                        isSatelliteMessageShown = true;
                    }

                    if (!blipsCreated)
                    {
                        dishes.ForEach(x => x.CreateBlip());
                        blipsCreated = true;
                    }

                    dishes.ForEach(x => x.Update());

                    foreach(SatelliteDish dish in dishes)
                    {
                        if (!dish.CheckedForData)
                        {
                            float dist = dish.Position.DistanceTo(PlayerPed.Position);
                            if (dist > 1.75f)
                                return;

                            SpaceModLib.DisplayHelpTextWithGXT("INTRO_LABEL_6");

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

                    MissionStep += (dishes.All(x => x.CheckedForData)) ? 1 : 0;
                    break;

                case 2:

                    if(!isHumaneLabsMessageShown)
                    {
                        SpaceModLib.ShowSubtitleWithGXT("INTRO_LABEL_7");
                        isHumaneLabsMessageShown = true;
                    }


                    break;
            }

            bool allDishesChecked = dishes.All(x => x.CheckedForData);
        }

        public override void OnAborted()
        {
            dishes.ForEach(x => x.Aborted());
        }

        public override void OnEnded(bool success)
        {
            
        }
    }
}
