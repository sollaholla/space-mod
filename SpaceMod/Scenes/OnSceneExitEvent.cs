using System;

namespace GTS.Scenes
{
    /// <summary>
    ///     Called when a <see cref="Scene" /> is exited.
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="newSceneFile"></param>
    public delegate void OnSceneExitEvent(object sender, SceneExitEventArgs e);

    public class SceneExitEventArgs : EventArgs
    {
        public SceneExitEventArgs(Scene scene, string newSceneFile)
        {
            Scene = scene;
            NewSceneFile = newSceneFile;
        }

        public Scene Scene { get; }

        public string NewSceneFile { get; }
    }
}