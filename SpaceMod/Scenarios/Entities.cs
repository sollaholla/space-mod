using System;
using GTA;
using GTA.Math;
using GTA.Native;

namespace DefaultMissions
{
    public class OnFootCombatPed : Entity
    {
        private static readonly Random Random;
        private readonly Ped _me;

        private TaskState _state;

        static OnFootCombatPed()
        {
            Random = new Random();
        }

        public OnFootCombatPed(Ped ped) : base(ped.Handle)
        {
            _me = ped;
        }

        /// <summary>
        ///     The target ped we want to attack.
        /// </summary>
        public Ped Target { get; set; }

        /// <summary>
        ///     The attack range of <c>this</c> ped.
        /// </summary>
        public float AttackRange { get; set; } = Random.Next(30 * 30, 50 * 50);

        /// <summary>
        ///     A multiplier to the attack range, that tells the AI when he/she can start chasing the <see cref="Target"/> again.
        /// </summary>
        public float ChaseAfterAttackRangeMultiplier { get; set; } =
            Function.Call<float>(Hash.GET_RANDOM_FLOAT_IN_RANGE, 1.25f, 2.5f);

        /// <summary>
        ///     The <see cref="Ped.Task" />.
        /// </summary>
        public Tasks Task => _me.Task;

        /// <summary>
        ///     Update <c>this</c> <see cref="Ped"/>'s combat behavior.
        /// </summary>
        public void Update()
        {
            if (IsDead)
            {
                if (Blip.Exists(CurrentBlip))
                    CurrentBlip.Remove();
                return;
            }

            switch (_state)
            {
                case TaskState.Idle:
                    if (Target != null)
                        _state = TaskState.Chase;
                    break;
                case TaskState.Chase:
                    var distance = Vector3.DistanceSquared(Target.Position, Position);
                    if (distance < AttackRange)
                        _state = TaskState.Attack;
                    else Task.RunTo(Target.Position, true);
                    break;
                case TaskState.Attack:
                    distance = Vector3.DistanceSquared(Target.Position, Position);
                    if (distance > AttackRange * ChaseAfterAttackRangeMultiplier)
                        _state = TaskState.Chase;
                    else Task.FightAgainst(Game.Player.Character);
                    break;
            }
        }

        private enum TaskState
        {
            Idle,
            Chase,
            Attack
        }
    }
}