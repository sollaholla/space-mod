using System;
using GTA;

namespace AmbientEnemySpawns
{
    public class Alien : Entity
    {
        private readonly float _combatRange;
        private readonly Ped _parent;
        private DateTime _goToTimeout;

        private bool _initializedAi;
        private bool _isAttackingEnemy;
        private bool _isGoingToEnemy;

        public Alien(int handle, float combatRange) : base(handle)
        {
            _parent = new Ped(handle);
            _combatRange = combatRange * combatRange;
        }

        public Ped Enemy { get; set; }

        public float DistToEnemy { get; private set; }

        public void Update()
        {
            if (!Exists() || !Exists(_parent)) return;
            if (IsDead) return;

            if (!_initializedAi)
            {
                InitializeAi();
                _initializedAi = true;
            }

            if (!Exists(Enemy))
                return;

            DistToEnemy = Position.DistanceToSquared(Enemy.Position);

            if (DistToEnemy > _combatRange)
            {
                _isAttackingEnemy = false;
                if (!_isGoingToEnemy)
                {
                    _parent.Task.RunTo(Enemy.Position, true, -1);
                    _goToTimeout = DateTime.Now + new TimeSpan(0, 0, 0, 0, 250);
                    _isGoingToEnemy = true;
                }
                else if (DateTime.Now > _goToTimeout)
                {
                    _isGoingToEnemy = false;
                }

                if (!_isGoingToEnemy) return;
                var lastImpactCoords = Enemy.GetLastWeaponImpactCoords();
                if (!(Position.DistanceToSquared(lastImpactCoords) < 5 * 5)) return;
                _parent.Task.ShootAt(Enemy);
                _parent.Task.RunTo(Enemy.Position, true, -1);
            }
            else
            {
                _isGoingToEnemy = false;
                if (_isAttackingEnemy) return;
                _parent.Task.ClearAll();
                _parent.Task.ShootAt(Enemy);
                _isAttackingEnemy = true;
            }
        }

        private void InitializeAi()
        {
            _parent.BlockPermanentEvents = true;
            _parent.AlwaysKeepTask = true;
        }

        public static explicit operator Ped(Alien other)
        {
            return new Ped(other.Handle);
        }
    }
}