using System.Threading;
using GTA;
using SpaceMod.Scenes;
using System.IO;

namespace SpaceMod.Scenarios
{
    internal delegate void OnScenarioCompleted(Scenario scenario, bool success);

    public abstract class Scenario
    {
        internal event OnScenarioCompleted completed;
        private ScriptSettings settings;
        private readonly object updateLock = new object();

        public ScriptSettings Settings => settings ?? (settings = ScriptSettings.Load(Path.ChangeExtension(Path.Combine(Database.PathToScenarios, GetType().Name), "ini")));
        public Scene CurrentScene => Core.Instance.GetCurrentScene();

        internal bool IsScenarioComplete()
        {
            if (Settings.GetValue("scenario_config", "complete", false))
                return true;

            Settings.SetValue("scenario_config", "complete", false);
            Settings.Save();

            return false;
        }

        internal void SetScenarioComplete()
        {
            Settings.SetValue("scenario_config", "complete", true);
            Settings.Save();
        }

        internal void Update()
        {
            if (!Monitor.TryEnter(updateLock)) return;

            try
            {
                OnUpdate();
            }
            finally
            {
                Monitor.Exit(updateLock);
            }
        }

        /// <summary>
        /// This is where you spawn entities or setup variables.
        /// </summary>
        public abstract void OnStart();

        /// <summary>
        /// This is where your code will be updated.
        /// </summary>
        public abstract void OnUpdate();

        /// <summary>
        /// This is where you can clean up some excess entities, and / or objects.
        /// </summary>
        public abstract void OnEnded(bool success);

        /// <summary>
        /// This is executed when the space mod scripts are aborted or reloaded.
        /// This function can be used to like the OnEnded function to clean up 
        /// any remaining objects / entities.
        /// </summary>
        public abstract void OnAborted();

        /// <summary>
        /// Called whenever you enter the specified scene. It is called even if you have 
        /// completed the mission. 
        /// </summary>
        public abstract void OnAwake();

        /// <summary>
        /// End's this scenario and discontinues running it.
        /// </summary>
        /// <param name="success">True if we completed this scenario.</param>
        public void EndScenario(bool success)
        {
            lock (updateLock)
            {
                if (success)
                {
                    SetScenarioComplete();
                }
                completed?.Invoke(this, success);
                OnEnded(success);
            }
        }
    }
}
