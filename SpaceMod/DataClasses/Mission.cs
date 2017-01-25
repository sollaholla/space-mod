using GTA;

namespace SpaceMod.DataClasses
{
    public abstract class Mission
    {
        public delegate void OnMissionEndedEvent();

        public event OnMissionEndedEvent MissionEnded;

        public abstract void Tick(Ped playerPed, Scene currentScene);

        public void End(bool failed)
        {
            MissionEnded?.Invoke();
        }

        public abstract void Abort();
        public abstract void CleanUp();
    }
}
