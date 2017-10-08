using System.Drawing;
using GTA.Math;
using GTA.Native;

namespace GTS.Library
{
    public class PtfxNonLooped
    {
        public PtfxNonLooped(string fxName, string asset)
        {
            FxName = fxName;
            Asset = asset;
        }

        public PtfxNonLooped(string fxName, string asset, Color color) : this(fxName, asset)
        {
            Color = color;
        }

        public string FxName { get; }

        public string Asset { get; }

        public Color Color { get; set; }

        public int Handle { get; protected set; } = -1;

        public bool IsLoaded => Function.Call<bool>(Hash.HAS_NAMED_PTFX_ASSET_LOADED, Asset);

        public void Request()
        {
            if (IsLoaded) return;
            Function.Call(Hash.REQUEST_NAMED_PTFX_ASSET, Asset);
        }

        public virtual int Play(Vector3 position, Vector3 rotation, float scale)
        {
            if (Handle != -1)
                Remove();

            Function.Call(Hash._SET_PTFX_ASSET_NEXT_CALL, Asset);
            var handle = Function.Call<int>(Hash.START_PARTICLE_FX_NON_LOOPED_AT_COORD, FxName, position.X, position.Y,
                position.Z,
                rotation.X, rotation.Y, rotation.Z, scale, false, false, false);
            Function.Call(Hash.SET_PARTICLE_FX_NON_LOOPED_COLOUR, Color.R, Color.G, Color.B);
            Function.Call(Hash.SET_PARTICLE_FX_NON_LOOPED_ALPHA, Color.A);
            Handle = handle;
            return handle;
        }

        public virtual void Remove()
        {
            Function.Call(Hash.REMOVE_PARTICLE_FX, Handle, 1);
            Function.Call(Hash._REMOVE_NAMED_PTFX_ASSET, Asset);
            Handle = -1;
        }
    }
}