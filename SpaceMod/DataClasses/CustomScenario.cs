﻿using System;
using System.Threading;
using GTA;

namespace SpaceMod.DataClasses
{
    public abstract class CustomScenario
    {
        internal delegate void OnCompletedEvent(CustomScenario scenario, bool success);

        internal event OnCompletedEvent Completed;

        private readonly object _updateLock = new object();

        public CustomScene CurrentScene = ModController.GetCurrentScene();

        internal bool IsScenarioComplete()
        {
            ScriptSettings settings = ScriptSettings.Load(Database.PathToScenarios + "/" + this + ".ini");

            if (settings.GetValue("scenario_config", "complete", false))
                return true;

            settings.SetValue("scenario_config", "complete", false);
            settings.Save();

            return false;
        }

        internal void SetScenarioComplete()
        {
            ScriptSettings settings = ScriptSettings.Load(Database.PathToScenarios + "/" + this + ".ini");
            settings.SetValue("scenario_config", "complete", false);
            settings.Save();
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
        public abstract void OnEnded();

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
                Completed?.Invoke(this, success);
                OnEnded();

                if (success) SetScenarioComplete();
            }
        }
    }
}
