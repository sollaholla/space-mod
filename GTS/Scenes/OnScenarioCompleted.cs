namespace GTS.Scenes
{
    /// <summary>
    ///     Called when the <paramref name="scenario" /> is completed.
    /// </summary>
    /// <param name="scenario"></param>
    /// <param name="success"></param>
    internal delegate void OnScenarioCompleted(Scenario scenario, bool success);
}