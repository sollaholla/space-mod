using GTA.Math;
using GTA.Native;

namespace DefaultMissions
{
    public class Fire
    {
        public Fire(Vector3 position, bool gasFire)
        {
            Position = position;
            GasFire = gasFire;
        }

        public Vector3 Position { get; }

        public bool GasFire { get; }

        public int Handle { get; private set; } = -1;

        public void Start()
        {
            if (Handle != -1)
                return;
            Handle = Function.Call<int>(Hash.START_SCRIPT_FIRE, Position.X, Position.Y, Position.Z);
        }

        public void Remove()
        {
            Function.Call(Hash.REMOVE_SCRIPT_FIRE, Handle);
            Handle = -1;
        }

        public bool IsFireNear()
        {
            return Function.Call<int>(Hash.GET_NUMBER_OF_FIRES_IN_RANGE, Position.X, Position.Y, Position.Z) > 0;
        }
    }
}