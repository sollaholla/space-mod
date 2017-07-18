using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using GTS.Extensions;
using GTS.Scenarios;
// mp_sleep => sleep_loop

namespace DefaultMissions
{
    public class Europa : Scenario
    {
        public Europa()
        {
            _entities = new List<Entity>();
        }

        #region Fields

        #region Readonly

        private readonly Model[] _models = { };
        private readonly List<Entity> _entities;

        #endregion

        #region Scenario

        private int _missionStep;
        private Fire _fire;

        #endregion

        #region Settings

        private Vector3 _explosionCoord = new Vector3(-9878.711f, -10012.36f, 9998.317f);
        private Vector3 _fireCoord = new Vector3(-9879.087f, -10014.16f, 10001.34f);

        #endregion

        #endregion

        #region Properties



        #endregion

        #region Functions

        #region Implemented

        public override void OnAwake()
        {
            ReadSettings();
            SaveSettings();
        }

        public override void OnStart()
        {
            RequestModels();
            SpawnAliens();
        }

        public override void OnUpdate()
        {
            // NOTE: Make sure we didn't already go to mars, 
            // so we don't mess up the story sequence.
            if (!HelperFunctions.DidGoToMars())
            {
                EndScenario(false);
                return;
            }

            switch (_missionStep)
            {
                case 0:
                    Script.Wait(750);
                    World.AddExplosion(_explosionCoord, ExplosionType.Tanker, 150, 5.0f);
                    Function.Call(Hash._PLAY_AMBIENT_SPEECH1, Game.Player.Character, "Generic_Frightened_Med", "Speech_Params_Force_Shouted_Critical");
                    _fire = new Fire(_fireCoord - Vector3.WorldUp, false);
                    _fire.Start();
                    _missionStep++;
                    break;
                case 1:
                    break;
            }
        }

        public override void OnEnded(bool success)
        {
            CleanUp(!success);
        }

        public override void OnAborted()
        {
            CleanUp(true);
        }

        #endregion

        #region Scenario

        private void ReadSettings()
        {
            _missionStep = Settings.GetValue("general", "mission_step", _missionStep);
            _explosionCoord = ParseVector3.Read(Settings.GetValue("general", "explosion_coord"), _explosionCoord);
            _fireCoord = ParseVector3.Read(Settings.GetValue("general", "fire_coord"), _fireCoord);
        }

        private void SaveSettings()
        {
            Settings.SetValue("general", "mission_step", _missionStep);
            Settings.SetValue("general", "explosion_coord", _explosionCoord);
            Settings.SetValue("general", "fire_coord", _fireCoord);
            Settings.Save();
        }

        #endregion

        #region Helpers

        private void SpawnAliens()
        {

        }

        private void RequestModels()
        {
            foreach (var model in _models)
            {
                model.Request();
                while (!model.IsLoaded)
                    Script.Yield();
            }
        }

        private void RemoveModels()
        {
            foreach (var model in _models)
            {
                if (!model.IsLoaded)
                    continue;

                model.MarkAsNoLongerNeeded();
            }
        }

        private void CleanUp(bool delete)
        {
            RemoveModels();
            CleanUpEntities(delete);
        }

        private void CleanUpEntities(bool delete)
        {
            if (_fire != null)
            {
                if (delete)
                {
                    _fire.Remove();
                }
            }
        }

        #endregion

        #endregion
    }
}
