namespace GTS.Scenes
{
    /// <summary>
    ///     Called when a <see cref="Scene" /> is exited.
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="newSceneFile"></param>
    public delegate void OnSceneExitEvent(Scene scene, string newSceneFile);
}