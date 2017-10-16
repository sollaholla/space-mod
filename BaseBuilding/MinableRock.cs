using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using GTS.Library;

namespace BaseBuilding
{
    public class MinableRock : Entity
    {
        public event OnPickupResourceEvent PickedUpResource;

        private readonly List<Prop> _chunks = new List<Prop>();
        private static readonly Random Random = new Random();
        private bool _didPlayParticles;
        private float _fadeAmount;

        public MinableRock(int handle, ResourceDefinition res, RockModelInfo modelInfo,
            int persistenceId) : base(handle)
        {
            ResourceData = res;
            ModelInfo = modelInfo;
            _fadeAmount = Alpha;
            PersistenceId = persistenceId;
        }

        public ResourceDefinition ResourceData { get; set; }

        public RockModelInfo ModelInfo { get; set; }

        public int PersistenceId { get; set; }

        public void Update(Vector3 damageCoords)
        {
            if (IsDead && !_didPlayParticles)
            {
                var particles = new PtfxNonLooped(ModelInfo.ParticleName, ModelInfo.ParticleDict);
                particles.Request();
                while (!particles.IsLoaded)
                    Script.Yield();
                particles.Play(Position, Vector3.Zero, ModelInfo.ParticleScale);
                particles.Remove();
                HasCollision = false;
                _didPlayParticles = true;
            }

            if (_didPlayParticles && Alpha > 0)
            {
                _fadeAmount -= Game.LastFrameTime * 250f;
                Alpha = (int) _fadeAmount;
            }
            else if (_didPlayParticles && Alpha <= 0)
            {
                BaseDelete();
            }

            if (!Exists() || damageCoords == Vector3.Zero || (!ModelInfo.ChunkModels?.Any() ?? true) ||
            !HasBeenDamagedBy(Game.Player.Character))
                return;

            Function.Call(Hash.CLEAR_ENTITY_LAST_DAMAGE_ENTITY, this);

            var normal = (damageCoords - Position).Normalized;
            const float speed = 5f;
            var randModel = ModelInfo.ChunkModels[Random.Next(ModelInfo.ChunkModels.Count)];
            var m = new Model(randModel.ChunkModel);
            m.Request();
            while (!m.IsLoaded)
                Script.Yield();

            var p = World.CreateProp(m, damageCoords, Vector3.Zero, true, false);
            p.IsInvincible = true;
            p.IsVisible = true;
            p.ApplyForce(normal * speed);

            var chunkParticles = new PtfxNonLooped(randModel.ParticleName, randModel.ParticleDict);
            chunkParticles.Request();
            while (!chunkParticles.IsLoaded)
                Script.Yield();

            chunkParticles.Play(p.Position, Vector3.Zero, randModel.ParticleScale);
            chunkParticles.Remove();
            m.MarkAsNoLongerNeeded();
            _chunks.Add(p);
        }

        public void UpdatePickups()
        {
            var charPos = Game.Player.Character.Position;
            foreach (var piece in _chunks)
            {
                if (!piece.Exists())
                    continue;

                var dist = Vector3.DistanceSquared(charPos, piece.Position);
                var bounds = piece.Model.GetDimensions().Length() * 2f;
                World.DrawLightWithRange(piece.Position, ColorTranslator.FromHtml(ResourceData.ResourceColor), bounds, 1f);
                if (dist > bounds * bounds) continue;

                piece.Delete();
                OnPickedUpResource(new PickupEventArgs(1, ResourceData));
            }
        }

        public new void Delete()
        {
            RemoveChunks();
            BaseDelete();
        }

        public void BaseDelete()
        {
            base.Delete();
        }

        private void RemoveChunks()
        {
            foreach (var piece in _chunks)
                piece?.Delete();
        }

        protected virtual void OnPickedUpResource(PickupEventArgs e)
        {
            PickedUpResource?.Invoke(this, e);
        }
    }
}
