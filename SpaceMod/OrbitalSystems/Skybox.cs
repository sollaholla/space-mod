using GTA;

namespace GTS.OrbitalSystems
{
    //public enum SkyboxRotationAxis
    //{
    //    Z,
    //    X,
    //    Y
    //}

    public class Skybox : Entity
    {
        //private readonly SkyboxRotationAxis _rotationAxis;

        public Skybox(
            IHandleable prop /*, float rotationSpeed = 0*/ /*, SkyboxRotationAxis rotationAxis = SkyboxRotationAxis.Z*/)
            : base(prop.Handle)
        {
            //SkyboxRotationSpeed = rotationSpeed;

            //_rotationAxis = rotationAxis;
        }

        //public float SkyboxRotationSpeed { get; }

        //public List<Orbital> Orbitals { get; }

        public void Update()
        {
            // Set our rotation.
            //Rotate();

            // Stay with the camera.
            Position = Database.ViewFinderPosition();
        }

        //private void Rotate()
        //{
        //    var rotation = Rotation;

        //    switch (_rotationAxis)
        //    {
        //        case SkyboxRotationAxis.Z:
        //            rotation.Z += Game.LastFrameTime * SkyboxRotationSpeed;
        //            break;
        //        case SkyboxRotationAxis.X:
        //            rotation.X += Game.LastFrameTime * SkyboxRotationSpeed;
        //            break;
        //        case SkyboxRotationAxis.Y:
        //            rotation.Y += Game.LastFrameTime * SkyboxRotationSpeed;
        //            break;
        //    }

        //    Rotation = rotation;
        //}

        public new void Delete()
        {
            base.Delete();
        }
        /////     length.
        /////     Returns all planets positions and rotations in the array order 0 to

        ///// <summary>
        ///// </summary>
        ///// <returns>
        ///// </returns>
        //public override string ToString()
        //{
        //    var ret = string.Empty;
        //    Orbitals.ForEach(o => ret += $"{o.Name}: position = {o.Position} | rotation = {o.Rotation}\n");
        //    return ret;
        //}
    }
}