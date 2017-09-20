using System.IO;
using System.Reflection;
using System.Threading;
using GTA;
using GTS.Scenes;

namespace GTS.Scenarios
{
    /// <summary>
    ///     Called when the <paramref name="scenario" /> is completed.
    /// </summary>
    /// <param name="scenario"></param>
    /// <param name="success"></param>
    internal delegate void OnScenarioCompleted(Scenario scenario, bool success);

    /// <summary>
    ///     A class that represents an event, or series of events.
    /// </summary>
    public abstract class Scenario
    {
        private readonly object _updateLock = new object();
        private ScriptSettings _settings;

        /// <summary>
        /// </summary>
        public ScriptSettings Settings => 
            _settings ?? (_settings =
            ScriptSettings.Load(Path.ChangeExtension(
            Path.Combine(Database.PathToScenarios, GetType().Name), "ini")));

        /// <summary>
        /// </summary>
        public Scene CurrentScene => Core.Instance.GetCurrentScene();

        public virtual bool BlockOrbitLanding { get; set; } = true;

        /// <summary>
        /// </summary>
        internal event OnScenarioCompleted Completed;

        /// <summary>
        /// </summary>
        /// <returns>
        /// </returns>
        internal bool IsScenarioComplete()
        {
            return Settings.GetValue("scenario_config", "complete", false);
        }

        /// <summary>
        /// </summary>
        internal void SetScenarioComplete()
        {
            Settings.SetValue("scenario_config", "complete", true);
            Settings.Save();
        }

        /// <summary>
        /// </summary>
        internal void Tick()
        {
            if (!Monitor.TryEnter(_updateLock)) return;

            try
            {
                SendMessage("Update");
            }
            finally
            {
                Monitor.Exit(_updateLock);
            }
        }

        /// <summary>
        ///     End's this scenario.
        /// </summary>
        /// <param name="success">True if we completed this scenario.</param>
        public void EndScenario(bool success)
        {
            lock (_updateLock)
            {
                if (success) SetScenarioComplete();
                Completed?.Invoke(this, success);
                SendMessage("OnDisable", success);
            }
        }

        public void SendMessage(string name, params object[] args)
        {
            var m = GetType().GetMethod(name);
            m?.Invoke(m.IsStatic ? null : this, args);
        }
    }
}