using GTA.Native;

namespace GTS.Library
{
    public static class FollowCam
    {
        public static FollowCamViewMode ViewMode
        {
            get
            {
                if (IsFollowingVehicle)
                    return (FollowCamViewMode) Function.Call<int>(Hash.GET_FOLLOW_VEHICLE_CAM_VIEW_MODE);
                return (FollowCamViewMode) Function.Call<int>(Hash.GET_FOLLOW_PED_CAM_VIEW_MODE);
            }
            set
            {
                if (IsFollowingVehicle)
                {
                    Function.Call(Hash.SET_FOLLOW_VEHICLE_CAM_VIEW_MODE, (int) value);
                    return;
                }
                Function.Call(Hash.SET_FOLLOW_PED_CAM_VIEW_MODE, (int) value);
            }
        }

        public static bool IsFollowingVehicle => Function.Call<bool>(Hash.IS_FOLLOW_VEHICLE_CAM_ACTIVE);
        public static bool IsFollowingPed => Function.Call<bool>(Hash.IS_FOLLOW_PED_CAM_ACTIVE);

        public static void DisableFirstPerson()
        {
            Function.Call(ViewMode == FollowCamViewMode.FirstPerson
                ? Hash._DISABLE_FIRST_PERSON_CAM_THIS_FRAME
                : Hash._DISABLE_VEHICLE_FIRST_PERSON_CAM_THIS_FRAME);
        }
    }
}