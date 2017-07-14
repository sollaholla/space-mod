using GTA;
using GTA.Math;
using GTA.Native;
using System;

namespace DefaultMissions
{
    public class OnFootCombatPed : Entity
    {
        private enum TaskState
        {
            Idle,
            Chase,
            Attack
        }

        private static Random random;

        private TaskState state;
        private Ped me;

        static OnFootCombatPed()
        {
            random = new Random();
        }

        public OnFootCombatPed(Ped ped) : base(ped.Handle)
        {
            me = ped;
        }

        /// <summary>
        /// The target ped we want to attack.
        /// </summary>
        public Ped Target { get; set; }

        /// <summary>
        /// The attack range of this ped.
        /// </summary>
        public float AttackRange { get; set; } = random.Next(30 * 30, 50 * 50);

        /// <summary>
        /// A multiplier to the attack range, that tells the AI when he/she can start chasing the Target again.
        /// </summary>
        public float ChaseAfterAttackRangeMultiplier { get; set; } = Function.Call<float>(Hash.GET_RANDOM_FLOAT_IN_RANGE, 1.25f, 2.5f);

        /// <summary>
        /// The <see cref="Ped.Task"/>.
        /// </summary>
        public Tasks Task { get { return me.Task; } }

        /// <summary>
        /// Update this peds combat behaviour.
        /// </summary>
        public void Update()
        {
            if (IsDead)
            {
                if (Blip.Exists(CurrentBlip))
                {
                    CurrentBlip.Remove();
                }
                return;
            }

            switch (state)
            {
                case TaskState.Idle:
                    if (Target != null)
                        state = TaskState.Chase;
                    break;
                case TaskState.Chase:
                    float distance = Vector3.DistanceSquared(Target.Position, Position);
                    if (distance < AttackRange)
                        state = TaskState.Attack;
                    else Task.RunTo(Target.Position, true);
                    break;
                case TaskState.Attack:
                    distance = Vector3.DistanceSquared(Target.Position, Position);
                    if (distance > AttackRange * ChaseAfterAttackRangeMultiplier)
                        state = TaskState.Chase;
                    else Task.FightAgainst(Game.Player.Character);
                    break;
            }
        }
    }
}
