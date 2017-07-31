using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTS.Extensions;
using GTS.Scenarios;

namespace DefaultMissions
{
    public class Karoon : Scenario
    {
        #region Fields

        #region Settings

        private readonly List<Vector3> _humanTubePositions = new List<Vector3>
        {
            new Vector3(-10137.02f, -10048.25f, 46.91925f),
            new Vector3(-10134.73f, -10063.99f, 46.43756f),
            new Vector3(-10118.92f, -10061.62f, 46.18332f),
            new Vector3(-10121.32f, -10045.66f, 46.26348f)
        };

        #endregion

        #region Misc

        private readonly List<Ped> _tubePeds = new List<Ped>();

        #endregion

        #endregion

        #region Functions

        #region Implemented

        public override void OnAwake()
        {
            ReadSettings();
            CreateTubePeds();
        }

        public override void OnStart()
        {
        }

        public override void OnUpdate()
        {
        }

        public override void OnEnded(bool success)
        {
            RemoveTubePeds();
        }

        public override void OnAborted()
        {
            RemoveTubePeds();
        }

        private void RemoveTubePeds()
        {
            foreach (var ped in _tubePeds)
                ped.Delete();
        }

        #endregion

        #region Scenario

        private void ReadSettings()
        {
            for (var i = 0; i < 50; i++)
            {
                var parse = ParseVector3.Read(Settings.GetValue("map", "test_tube_human" + i), Vector3.Zero);
                if (i < _humanTubePositions.Count)
                {
                    _humanTubePositions[i] = parse == Vector3.Zero ? _humanTubePositions[i] : parse;
                    continue;
                }
                if (parse == Vector3.Zero) continue;
                _humanTubePositions.Add(parse);
            }
        }

        private void CreateTubePeds()
        {
            foreach (var position in _humanTubePositions)
            {
                var ped = World.CreateRandomPed(position);
                if (ped == null) continue;
                ped.HasCollision = false;
                ped.FreezePosition = true;
                ped.BlockPermanentEvents = true;
                ped.IsInvincible = true;
                ped.IsExplosionProof = true;
                ped.IsFireProof = true;
                ped.Task.PlayAnimation("skydive@parachute@", "chute_idle", 8.0f, -8.0f, -1, AnimationFlags.Loop, 0.0f);
                ped.Task.PlayAnimation("mp_sleep", "sleep_loop", 8.0f, -8.0f, -1, (AnimationFlags) 49, 0.0f);
                _tubePeds.Add(ped);
            }
        }

        #endregion

        #endregion
    }
}