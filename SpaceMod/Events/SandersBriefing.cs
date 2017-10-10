using GTA;
using GTA.Math;
using GTA.Native;
using GTS.Library;
using GTS.Scenes;
using GTS.Utility;

namespace GTS.Events
{
    internal class SandersBriefing : Scenario
    {
        private Ped _sanders;
        private readonly Vector3 _sandersSpawn = new Vector3(-6545.557f, -1396.539f, 97.03667f);
        private const float SandersHeading = 205.49f;
        private bool _didDialogue;
        private readonly Vector3 _camera1Spawn = new Vector3(-6547.721f, -1397.484f, 97.99258f);
        private readonly Vector3 _camera1PointAt = new Vector3(-6545.693f, -1395.639f, 97.04079f);
        private readonly Vector3 _slidePosition = new Vector3(-6545.506f, -1395.039f, 97.04079f);
        private const float SlideHeading = 163.1979f;

        public SandersBriefing() { }
        
        public void Start()
        {
            var model = new Model(PedHash.Marine03SMY);
            model.Request();
            while (!model.IsLoaded)
                Script.Yield();
            _sanders = World.CreatePed(model, _sandersSpawn, SandersHeading);
            _sanders.PositionNoOffset = _sandersSpawn;
            _sanders.TaskStartScenarioInPlace("world_human_binoculars");
            _sanders.RelationshipGroup = PlayerPed.RelationshipGroup;
            _sanders.SetDefaultClothes();
            _sanders.FreezePosition = true;
            var b = _sanders.AddBlip();
            b.Sprite = BlipSprite.GTAOMission;
            b.Color = Scene.MarkerBlipColor;
            _didDialogue = Settings.GetValue("progress", "did_dialogue", _didDialogue);
        }

        public void Update()
        {
            DoDialogue();
        }

        private void DoDialogue()
        {
            if (_didDialogue) return;

            var distance = Vector3.DistanceSquared(PlayerPed.Position, _sanders.Position);
            const float maxDist = 2 * 2;
            if (distance > maxDist) return;

            GtsLibNet.DisplayHelpTextWithGxt(GtsLabels.INPUT_CONTEXT_GENERIC);
            if (!Game.IsControlJustPressed(2, Control.Context)) return;

            _sanders.Task.TurnTo(PlayerPed);
            _sanders.Task.LookAt(PlayerPed);
            PlayerPed.Task.ClearAll();
            PlayerPed.Task.TurnTo(_sanders, -1);
            PlayerPed.Task.LookAt(_sanders);
            Function.Call(Hash._PLAY_AMBIENT_SPEECH1, PlayerPed, "GENERIC_HI", "SPEECH_PARAMS_FORCE_NORMAL");
            _sanders.FreezePosition = false;
            _sanders.CanRagdoll = false;

            var camera = World.CreateCamera(_camera1Spawn, Vector3.Zero, 60f);
            camera.PointAt(_camera1PointAt);
            camera.DepthOfFieldStrength = 5.0f;
            camera.Shake(CameraShake.Hand, 1.0f);
            Function.Call(Hash.RENDER_SCRIPT_CAMS, true, true, 500, 0, 0);

            Function.Call(Hash.TASK_PED_SLIDE_TO_COORD, PlayerPed, _slidePosition.X, _slidePosition.Y, _slidePosition.Z, SlideHeading, 0.1f);

            _didDialogue = true;
        }

        public void OnAborted()
        {
            World.RenderingCamera = null;
            _sanders.Delete();
        }

        public void OnDisable()
        {
            World.RenderingCamera = null;
            _sanders.Delete();
        }
    }
}