using GTA;
using GTA.Math;
using GTA.Native;
using SpaceMod;
using SpaceMod.Extensions;
using SpaceMod.Lib;
using SpaceMod.Scenario;
using SpaceMod.Scenes;

namespace DefaultMissions
{
    public class MarsMission02 : CustomScenario
    {
        public int CurrentMissionStep = 0;
        public bool GiveEggToScientist = false;
        public bool IsMars01Complete = false;
        public bool DidGetResources = false;
        public Prop Asteroid = null;

        public MarsMission02()
        {
            CurrentScene.Mined += CurrentSceneOnMined;
        }

        public Ped PlayerPed => Game.Player.Character;
        public Vector3 PlayerPosition => PlayerPed.Position;

        public override void Start()
        {
            ScriptSettings settings = ScriptSettings.Load(SpaceModDatabase.PathToScenarios + "/DefaultMissions.MarsMission01.ini");
            IsMars01Complete = settings.GetValue("SCENARIO_CONFIG", "COMPLETE", false);

            if (!IsMars01Complete)
                return;

            GiveEggToScientist = Settings.GetValue("mission", "give_scientist_egg", GiveEggToScientist);
            DidGetResources = Settings.GetValue("mission", "did_get_resources", DidGetResources);
        }

        public override void OnUpdate()
        {
            if (!IsMars01Complete)
            {
                EndScenario(false);
                return;
            }

            if (!DidGetResources)
            {
                if (!GiveEggToScientist)
                    GiveScientistTheEgg();
                else CreateAsteroid();
            }
            else CompleteMission();
        }

        private void GiveScientistTheEgg()
        {
            IplData currentIplData = CurrentScene.SceneData.CurrentIplData;
            Ped[] peds = CurrentScene.SceneData.Ipls.Find(i => i.Name == "Mars/mars_base_int_01")?.CurrentIpl?.Peds?.ToArray() ?? new Ped[0];
            var closestPed = World.GetClosest(PlayerPosition, peds);

            if (Entity.Exists(closestPed))
            {
                float dist = closestPed.Position.DistanceTo(PlayerPosition);
                if (dist < 2f)
                {
                    SpaceModLib.DisplayHelpTextWithGXT("GTS_LABEL_0");
                    Game.DisableControlThisFrame(2, Control.Context);
                    Game.DisableControlThisFrame(2, Control.Talk);
                    if (Game.IsDisabledControlJustPressed(2, Control.Context))
                    {
                        // Remove the scenario props, if any.
                        Vector3 pos = closestPed.Position;
                        Prop clipboard = new Prop(Function.Call<int>(Hash.GET_CLOSEST_OBJECT_OF_TYPE, pos.X, pos.Y, pos.Z, 5.0f, Game.GenerateHash("p_cs_clipboard"), false, 0, 0, 0));
                        if (Entity.Exists(clipboard) && clipboard.IsAttachedTo(closestPed))
                            clipboard.Delete();
                        Prop coffee = new Prop(Function.Call<int>(Hash.GET_CLOSEST_OBJECT_OF_TYPE, pos.X, pos.Y, pos.Z, 5.0f, Game.GenerateHash("p_amb_coffeecup_01"), false, 0, 0, 0));
                        if (Entity.Exists(coffee) && coffee.IsAttachedTo(closestPed))
                            coffee.Delete();

                        closestPed.Task.PlayAnimation("special_ped@impotent_rage@convo_6@convo_6a", "a_tourist_just_asked_0");
                        closestPed.Heading = (PlayerPosition - closestPed.Position).ToHeading();
                        Function.Call(Hash._PLAY_AMBIENT_SPEECH1, closestPed.Handle, "Generic_Thanks", "Speech_Params_Force_Shouted_Critical");
                        SpaceModLib.ShowSubtitleWithGXT(
                            "GTS_LABEL_14",
                            7000);
                        Settings.SetValue("mission", "give_scientist_egg", true);
                        Settings.Save();
                        GiveEggToScientist = true;
                    }
                }
            }
        }

        private void CreateAsteroid()
        {
            if (CurrentScene.SceneFile == "MarsOrbit.space")
            {
                if (!Entity.Exists(Asteroid))
                {
                    // now lets spawn the asteroid.
                    Vector3 offsetFromGal = new Vector3(0, 125, 60);
                    Asteroid = World.CreateProp("prop_asteroid01", SpaceModDatabase.GalaxyCenter + offsetFromGal, Vector3.Zero, true,
                        false);
                    // need more info on the because i have no clue what the rest of the params actually do.
                    // not even sure what the weight units are, maybe KG? 
                    //Object object, float weight, float p2, float p3, float p4, float p5, float gravity, float p7, float p8, float p9, float p10, float buoyancy
                    Function.Call(Hash.SET_OBJECT_PHYSICS_PARAMS, Asteroid.Handle, 7000f, -1f, 1f, 0f, 0f, 0f, 0f, 1f, 1f, 1f, 0f);
                    CurrentScene.RegisterObjectForMining(Asteroid);
                }
            }
            else
            {
                // delete this asteroid when we are not in mars orbit.
                if (Entity.Exists(Asteroid))
                {
                    CurrentScene.UnregisterObjectForMining(Asteroid);
                    Asteroid.Delete();
                }
            }
        }

        private void CurrentSceneOnMined(CustomScene scene, Prop mineableObject)
        {
            if (mineableObject == Asteroid)
            {
                CurrentScene.UnregisterObjectForMining(Asteroid);
                Settings.SetValue("mission", "did_get_resources", true);
                Settings.Save();
                DidGetResources = true;
            }
        }

        private void CompleteMission()
        {
            IplData currentIplData = CurrentScene.SceneData.CurrentIplData;
            Ped[] peds = CurrentScene.SceneData.Ipls.Find(i => i.Name == "Mars/mars_base_int_01")?.CurrentIpl?.Peds?.ToArray() ?? new Ped[0];
            var closestPed = World.GetClosest(PlayerPosition, peds);

            if (Entity.Exists(closestPed))
            {
                float dist = closestPed.Position.DistanceTo(PlayerPosition);
                if (dist < 2f)
                {
                    SpaceModLib.DisplayHelpTextWithGXT("GTS_LABEL_28");
                    Game.DisableControlThisFrame(2, Control.Context);
                    Game.DisableControlThisFrame(2, Control.Talk);
                    if (Game.IsDisabledControlJustPressed(2, Control.Context))
                    {
                        closestPed.Task.PlayAnimation("amb@world_human_cheering@male_b", "base");
                        closestPed.Heading = (PlayerPosition - closestPed.Position).ToHeading();
                        Function.Call(Hash._PLAY_AMBIENT_SPEECH1, closestPed.Handle, "Generic_Thanks", "Speech_Params_Force_Shouted_Critical");
                        SpaceModLib.ShowSubtitleWithGXT("GTS_LABEL_24", 10000);
                        Script.Wait(2500);
                        ScaleFormMessages.Message.SHOW_MISSION_PASSED_MESSAGE(Game.GetGXTEntry("GTS_LABEL_25"));
                        Function.Call(Hash.PLAY_MISSION_COMPLETE_AUDIO, "FRANKLIN_BIG_01");
                        Function.Call(Hash._PLAY_AMBIENT_SPEECH1, PlayerPed.Handle, "Generic_Shocked_Med", "Speech_Params_Force_Shouted_Critical");
                        EndScenario(true);
                    }
                }
            }
        }

        public override void OnEnded(bool success) => DeleteStuff();
        public override void OnAborted() => DeleteStuff();

        private void DeleteStuff()
        {
            if (!Entity.Exists(Asteroid))
                return;
            Asteroid.Delete();
        }
    }
}
