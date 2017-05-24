using GTA;
using SpaceMod.Lib;
using SpaceMod.Scenario;

namespace DefaultMissions
{
    public class BigMessage : CustomScenario
    {
        private const float NotifyTime = 1.5f;

        private const string MarsSceneName = "MarsOrbit.space";
        private const string EarthSceneName = "EarthOrbit.space";

        private float _notifyTimeTimer;

        public override void Start() { }

        public override void OnUpdate()
        {
            if (Game.IsLoading) return;
            _notifyTimeTimer += Game.LastFrameTime;

            if (_notifyTimeTimer < NotifyTime) return;
            if (string.IsNullOrEmpty(CurrentScene.SceneFile)) return;
            if (string.IsNullOrEmpty(CurrentScene.SceneData.LastSceneFile)) return;
            if (CurrentScene.SceneFile == MarsSceneName && CurrentScene.SceneData.LastSceneFile == EarthSceneName ||
                CurrentScene.SceneFile == EarthSceneName && CurrentScene.SceneData.LastSceneFile == MarsSceneName) 
            {
                BigMessageThread.MessageInstance.ShowMissionPassedMessage(Game.GetGXTEntry("BM_LABEL_5"));
            }

            EndScenario(false);
        }

        public override void OnEnded(bool success) { }

        public override void OnAborted() { }
    }
}
