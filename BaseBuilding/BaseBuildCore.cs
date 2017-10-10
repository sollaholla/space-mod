using GTA;
using GTS.Scenes;

namespace BaseBuilding
{
    public class BaseBuildCore : Scenario
    {
        public override bool TargetAllScenes => true;

        public BaseBuildCore()
        { }

        public void Update()
        {
            if (!CurrentScene.Info.SurfaceScene) return;
        }
    }
}
