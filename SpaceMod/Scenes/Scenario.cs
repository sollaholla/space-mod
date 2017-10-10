using System;
using System.IO;
using System.Threading;
using GTA;
using GTS.Utility;

namespace GTS.Scenes
{
    /// <summary>
    ///     A class that represents an event, or series of events.
    /// </summary>
    public abstract class Scenario
    {
        private readonly object _updateLock = new object();
        private ScriptSettings _settings;

        public ScriptSettings Settings =>
            _settings ?? (_settings =
                ScriptSettings.Load(Path.ChangeExtension(
                    Path.Combine(GtsSettings.ScenariosFolder, GetType().Name), "ini")));

        public Ped PlayerPed => Core.PlayerPed;
        public Scene CurrentScene { get; internal set; }
        public virtual bool BlockOrbitLanding { get; set; }
        public virtual string[] TargetScenes { get; set; }
        public virtual bool TargetAllScenes { get; set; }
        internal event OnScenarioCompleted Completed;

        internal bool IsScenarioComplete()
        {
            return Settings.GetValue("scenario_config", "complete", false);
        }

        internal void SetScenarioComplete()
        {
            Settings.SetValue("scenario_config", "complete", true);
            Settings.Save();
        }

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
            if (m == null) return;
            if (!m.IsPublic) return;
            m.Invoke(m.IsStatic ? null : this, args);
        }
    }
}