using System.Drawing;
using GTA;
using GTA.Math;
using GTA.Native;

namespace SpaceMod.Particles
{
	public class PtfxLooped : PtfxNonLooped
    {
        private Color _color;

        public PtfxLooped(string fxName, string asset) : base(fxName, asset) { }

        public PtfxLooped(string fxName, string asset, Color color) : this(fxName, asset)
        {
            _color = color;
        }

        public new Color Color
        {
            get { return _color; }
            set
            {
                Function.Call(Hash.SET_PARTICLE_FX_LOOPED_COLOUR, Handle, (float) Color.R / 255, (float) Color.G / 255,
                    (float) Color.B / 255, 0);
                Function.Call(Hash.SET_PARTICLE_FX_LOOPED_ALPHA, Handle, (float)Color.A / 255);
                _color = value;
            }
        }

        public override int Play(Vector3 position, Vector3 rotation, float scale)
        {
            if (Handle != -1) return Handle;
            Function.Call(Hash._SET_PTFX_ASSET_NEXT_CALL, Asset);
            return Handle = Function.Call<int>(Hash.START_PARTICLE_FX_LOOPED_AT_COORD, FxName, position.X, position.Y,
                position.Z, rotation.X, rotation.Y, rotation.Z, scale, false, false, false, false); ;
        }

        public int Play(Entity entity, int boneIndex, Vector3 offset, Vector3 rotation, float scale)
        {
            if (Handle != -1) return Handle;
            Function.Call(Hash._SET_PTFX_ASSET_NEXT_CALL, Asset);
            return Handle = Function.Call<int>(Hash._START_PARTICLE_FX_LOOPED_ON_ENTITY_BONE, FxName, entity, offset.X, offset.Y,
                offset.Z, rotation.X, rotation.Y, rotation.Z, boneIndex, scale, false, false, false, false);
        }

        public bool Exists()
        {
            return Function.Call<bool>(Hash.DOES_PARTICLE_FX_LOOPED_EXIST, Handle);
        }

        public void Stop()
        {
            Function.Call(Hash.STOP_PARTICLE_FX_LOOPED, Handle, 1);
            Function.Call(Hash._REMOVE_NAMED_PTFX_ASSET, Asset);
            Handle = -1;
        }
    }
}
