namespace DefaultMissions
{
    public interface ICutScene
    {
        bool Complete { get; set; }

        void Start();

        void Update();

        void Stop();
    }

    public interface IEvent : ICutScene
    { }
}
