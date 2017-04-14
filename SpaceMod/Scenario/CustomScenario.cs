using System.Threading;
using GTA;
using SpaceMod.Scenes;

namespace SpaceMod.Scenario
{
    public abstract class CustomScenario
    {
        private ScriptSettings _settings;

        public ScriptSettings Settings
            => _settings ?? (_settings = ScriptSettings.Load(SpaceModDatabase.PathToScenarios + "/" + this + ".ini"));

        internal delegate void OnCompletedEvent(CustomScenario scenario, bool success);

        internal event OnCompletedEvent Completed;

        private readonly object _updateLock = new object();

        public CustomScene CurrentScene => Core.Instance.GetCurrentScene();

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
            if (!Monitor.TryEnter(_updateLock)) return;

            try
            {
                OnUpdate();
            }
            finally
            {
                Monitor.Exit(_updateLock);
            }
        }

        /// <summary>
        /// This is where you spawn entities or setup variables.
        /// </summary>
        public abstract void Start();

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
        /// End's this scenario and discontinues running it.
        /// </summary>
        /// <param name="success">True if we completed this scenario.</param>
        public void EndScenario(bool success)
        {
            lock (_updateLock)
            {
                if (success)
                {
                    SetScenarioComplete();
                    GTA.Native.Function.Call(GTA.Native.Hash.PLAY_MISSION_COMPLETE_AUDIO, "FRANKLIN_BIG_01");
                }                                                          // This can be "TREVOR_SMALL_01" or "DEAD" or "GENERIC_FAILED" too.
                Completed?.Invoke(this, success);
                OnEnded(success);
            }
        }
    }
}
