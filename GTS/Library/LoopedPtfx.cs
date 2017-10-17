using System.Drawing;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GTS.Library
{
    public class LoopedPtfx
    {
        /// <summary>
        ///     Initialize the class
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="fxName"></param>
        public LoopedPtfx(string assetName, string fxName)
        {
            Handle = -1;
            AssetName = assetName;
            FxName = fxName;
        }

        public int Handle { get; private set; }
        public string AssetName { get; }
        public string FxName { get; }

        public Color Color
        {
            set => Function.Call(Hash.SET_PARTICLE_FX_LOOPED_COLOUR, Handle, value.R / 255, value.G / 255,
                value.B / 255, false);
        }

        public int Alpha
        {
            set => Function.Call(Hash.SET_PARTICLE_FX_LOOPED_ALPHA, Handle, value / 255);
        }

        /// <summary>
        ///     If the particle FX is spawned.
        /// </summary>
        public bool Exists => Handle != -1 && Function.Call<bool>(Hash.DOES_PARTICLE_FX_LOOPED_EXIST, Handle);

        /// <summary>
        ///     If the particle FX asset is loaded.
        /// </summary>
        public bool IsLoaded => Function.Call<bool>(Hash.HAS_NAMED_PTFX_ASSET_LOADED, AssetName);

        /// <summary>
        ///     <see cref="Load" /> the particle FX asset
        /// </summary>
        public void Load()
        {
            Function.Call(Hash.REQUEST_NAMED_PTFX_ASSET, AssetName);
            while (!IsLoaded)
                Script.Yield();
        }

        /// <summary>
        ///     Start particle FX on the specified entity.
        /// </summary>
        /// <param name="entity"><see cref="Entity" /> to attach to.</param>
        /// <param name="scale">Scale of the fx.</param>
        /// <param name="offset">Optional offset.</param>
        /// <param name="rotation">Optional rotation.</param>
        /// <param name="bone"><see cref="Entity" /> bone.</param>
        public void Start(Entity entity, float scale, Vector3 offset, Vector3 rotation, Bone? bone)
        {
            if (Handle != -1) return;

            Function.Call(Hash._SET_PTFX_ASSET_NEXT_CALL, AssetName);

            Handle = bone == null
                ? Function.Call<int>(Hash.START_PARTICLE_FX_LOOPED_ON_ENTITY, FxName,
                    entity, offset.X, offset.Y, offset.Z, rotation.X, rotation.Y, rotation.Z, scale, 0, 0, 1)
                : Function.Call<int>(Hash._START_PARTICLE_FX_LOOPED_ON_ENTITY_BONE, FxName,
                    entity, offset.X, offset.Y, offset.Z, rotation.X, rotation.Y, rotation.Z, (int) bone, scale, 0, 0,
                    0);
        }

        /// <summary>
        ///     Start particle FX on the specified entity.
        /// </summary>
        /// <param name="entity"><see cref="Entity" /> to attach to.</param>
        /// <param name="scale">Scale of the fx.</param>
        public void Start(Entity entity, float scale)
        {
            Start(entity, scale, Vector3.Zero, Vector3.Zero, null);
        }

        /// <summary>
        ///     Start particle FX at the specified position.
        /// </summary>
        /// <param name="position">Position in world space.</param>
        /// <param name="scale">Scale of the fx.</param>
        /// <param name="rotation">Optional rotation.</param>
        public void Start(Vector3 position, float scale, Vector3 rotation)
        {
            if (Handle != -1) return;

            Function.Call(Hash._SET_PTFX_ASSET_NEXT_CALL, AssetName);

            Handle = Function.Call<int>(Hash.START_PARTICLE_FX_LOOPED_AT_COORD, FxName,
                position.X, position.Y, position.Z, rotation.X, rotation.Y, rotation.Z, scale, 0, 0, 0, 0);
        }

        /// <summary>
        ///     Start particle FX at the specified position.
        /// </summary>
        /// <param name="position">Position in world space.</param>
        /// <param name="scale">Scale of the fx.</param>
        public void Start(Vector3 position, float scale)
        {
            Start(position, scale, Vector3.Zero);
        }

        /// <summary>
        ///     Remove the particle FX
        /// </summary>
        public void Remove()
        {
            if (Handle == -1) return;

            Function.Call(Hash.REMOVE_PARTICLE_FX, Handle, 0);
            Handle = -1;
        }

        /// <summary>
        ///     Remove the particle FX in range
        /// </summary>
        public void Remove(Vector3 position, float radius)
        {
            if (Handle == -1) return;

            Function.Call(Hash.REMOVE_PARTICLE_FX_IN_RANGE, position.X, position.Y, position.Z, radius);
            Handle = -1;
        }

        /// <summary>
        ///     <see cref="Unload" /> the loaded particle FX asset
        /// </summary>
        public void Unload()
        {
            if (IsLoaded)
                Function.Call((Hash) 0x5F61EBBE1A00F96D, AssetName);
        }
    }
}