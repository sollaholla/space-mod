using GTA.Math;

namespace GTS.Scenes
{
    public class PreviousSceneInfo
    {
        public PreviousSceneInfo(Vector3 dirToPlayer, string orbitalName, string scene, float modelDimensions)
        {
            DirToPlayer = dirToPlayer;
            OrbitalName = orbitalName;
            Scene = scene;
            ModelDimensions = modelDimensions;
        }

        public Vector3 DirToPlayer { get; set; }
        public string OrbitalName { get; set; }
        public string Scene { get; set; }
        public float ModelDimensions { get; set; }
    }
}