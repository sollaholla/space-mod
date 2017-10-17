using GTA;
using GTA.Math;
using GTA.Native;

namespace GTS.Library
{
    public class GtsCameraRig
    {
        private int _handle;
        private float _xAxis;
        private float _yAxis;

        public GtsCameraRig(Vector3 offset, float sensitivity)
        {
            Offset = offset;
            Sensitivity = sensitivity;
        }

        public Vector3 Offset { get; set; }

        public float Sensitivity { get; set; }

        public bool IsRendering => _handle != 0 && Function.Call<int>(Hash.GET_RENDERING_CAM) == _handle;

        /// <summary>
        /// The camera handle.
        /// </summary>
        public int Handle
        {
            get => _handle;
            private set => _handle = value;
        }

        /// <summary>
        /// Get and set the camera field of view.
        /// </summary>
        public float Fov
        {
            get => Function.Call<float>(Hash.GET_CAM_FOV, _handle);
            set => Function.Call(Hash.SET_CAM_FOV, _handle, value);
        }

        public bool Exists()
        {
            return Function.Call<bool>(Hash.DOES_CAM_EXIST, _handle);
        }

        public void StartRendering(bool render, bool ease, int easeTime)
        {
            if (_handle == 0)
                _handle = Function.Call<int>(Hash.CREATE_CAM, "DEFAULT_SCRIPTED_CAMERA", 1);
            Function.Call(Hash.RENDER_SCRIPT_CAMS, render, ease, easeTime, 0, 0);
        }

        public void StopRendering(bool ease, int easeTime)
        {
            Function.Call(Hash.RENDER_SCRIPT_CAMS, false, ease, easeTime, 0, 0);
            Function.Call(Hash.DESTROY_CAM, _handle);
            _handle = 0;
        }

        public static void DestroyAllCameras()
        {
            Function.Call(Hash.DESTROY_ALL_CAMS, true);
        }

        public void Update(Ped ped, float x, float y)
        {
            if (!Exists()) return;

            y = 1f;

            _xAxis += x * Sensitivity * Game.LastFrameTime;
            _yAxis += y * Sensitivity * Game.LastFrameTime;

            var rotation = Quaternion.Euler(_xAxis, 0, _yAxis);
            var position = ped.Position + rotation * Offset;
            Function.Call(Hash.SET_CAM_COORD, _handle, position.X, position.Y, position.Z);
        }
    }
}
