namespace GTSCommon
{
    public interface IDrawable
    {
        string Model { get; set; }

        XVector3 Position { get; set; }

        int LodDistance { get; set; }
    }
}