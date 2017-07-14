/*
 *  Soloman Northrop (c) 2017
 *  https://www.youtube.com/channel/UCd7mQswuowjJCeaChSgnYtA
 */

using GTA.Math;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace GTS.Scenes
{
    public enum InteriorType
    {
        GTA,
        MapEditor
    }

    [System.Serializable]
    [XmlRoot("CSceneInfoData")]
    public sealed class SceneInfo : NextSceneInfo
    {
        public SceneInfo()
        {
            SkyboxModel = "spacedome";
            Time = 23;
            GravityLevel = 3;
            Orbitals = new List<OrbitalInfo>();
            AttachedOrbitals = new List<AttachedOrbitalInfo>();
            SceneLinks = new List<Link>();
            Interiors = new List<InteriorInfo>();
            Scenarios = new List<ScenarioInfo>();
            Teleports = new List<TeleportPoint>();
        }

        [Category("Collections"), RefreshProperties(RefreshProperties.All)]
        public List<OrbitalInfo> Orbitals { get; set; }

        [Category("Collections"), RefreshProperties(RefreshProperties.All)]
        public List<AttachedOrbitalInfo> AttachedOrbitals { get; set; }

        [Category("Collections"), RefreshProperties(RefreshProperties.All)]
        public List<Link> SceneLinks { get; set; }

        [Category("Collections"), RefreshProperties(RefreshProperties.All)]
        public List<InteriorInfo> Interiors { get; set; }

        [Category("Collections"), RefreshProperties(RefreshProperties.All)]
        public List<ScenarioInfo> Scenarios { get; set; }

        [Category("Collections"), RefreshProperties(RefreshProperties.All)]
        public List<TeleportPoint> Teleports { get; set; }

        [Description("The model name of the skybox."), Category("Core Settings"), RefreshProperties(RefreshProperties.All)]
        public string SkyboxModel { get; set; }

        [Description("Time time of day in hours that we will use for the scene."), Category("Core Settings"), RefreshProperties(RefreshProperties.All)]
        public int Time { get; set; }

        [Description("True if you want to use gravity for this scene."), Category("Core Settings"), RefreshProperties(RefreshProperties.All)]
        public bool UseGravity { get; set; }

        [Description("Gravity in m/s^2"), Category("Core Settings"), RefreshProperties(RefreshProperties.All)]
        public float GravityLevel { get; set; }

        [Description("The origin of the skybox, and all props."), Category("Core Settings"), RefreshProperties(RefreshProperties.All)]
        public Vector3 GalaxyCenter { get; set; } = new Vector3(-10000, -10000, 10000);

        [Category("Surface Settings"), Description("This should be true if this scene is meant to be a planet surface scene."), RefreshProperties(RefreshProperties.All)]
        public bool SurfaceScene { get; set; }

        [Category("Surface Settings"), Description("The angular speed multiplier for the skybox. Rotation is based your angle to any celestial body. (E.g. Travelling far enough north of a celestail body will hide it behind the southern horizon line)")]
        public float HorizonRotationMultiplier { get; set; } = 0.00005f;

        [Category("Surface Settings"), Description("The filename of the next scene that will load once we leave the surface."), RefreshProperties(RefreshProperties.All)]
        public override string NextScene { get; set; }

        [Category("Surface Settings"), Description("The rotation of our player when the next scene loads."), RefreshProperties(RefreshProperties.All)]
        public override Vector3 NextSceneRotation { get; set; }

        [Category("Surface Settings"), Description("The position of the player, offsetted from the center of space, when the next scene loads."), RefreshProperties(RefreshProperties.All)]
        public override Vector3 NextScenePosition { get; set; }
    }

    [System.Serializable]
    public class Link : NextSceneInfo, ITrigger
    {
        public Link()
        {
            TriggerDistance = 1500;
        }

        [Category("Required"), Description("This is the name that will be displayed on screen by the custom UI."), RefreshProperties(RefreshProperties.All)]
        public string Name { get; set; }

        [Category("Required"), Description("The position of this trigger offsetted from the center of space."), RefreshProperties(RefreshProperties.All)]
        public Vector3 Position { get; set; }

        [Category("Next Scene Info"), Description("This is the distance that will trigger the next scene to load."), RefreshProperties(RefreshProperties.All)]
        public float TriggerDistance { get; set; }
    }

    [System.Serializable]
    public partial class AttachedOrbitalInfo : IDrawable
    {
        [Category("Required"), Description("The name of the ydr/ydd model. Example: 'earth_large'"), RefreshProperties(RefreshProperties.All)]
        public string Model { get; set; }

        [Category("Required"), Description("The position of this object offsetted from the center of space."), RefreshProperties(RefreshProperties.All)]
        public Vector3 Position { get; set; }
    }

    [System.Serializable]
    public partial class OrbitalInfo : NextSceneInfo, IDrawable, ITrigger
    {
        public OrbitalInfo()
        {
            TriggerDistance = 1500;
        }

        [Description("This is the name that will be displayed on screen by the custom UI."), RefreshProperties(RefreshProperties.All)]
        public string Name { get; set; }

        [Category("Required"), Description("The name of the ydr/ydd model. Example: 'earth_large'"), RefreshProperties(RefreshProperties.All)]
        public string Model { get; set; }

        [Category("Required"), Description("The position of this object offsetted from the center of space."), RefreshProperties(RefreshProperties.All)]
        public Vector3 Position { get; set; }

        [Description("The rotation speed (degrees per-second)."), RefreshProperties(RefreshProperties.All)]
        public float RotationSpeed { get; set; }

        [Description("True if you wish for this object to act like a wormhole, and suck the player in."), RefreshProperties(RefreshProperties.All)]
        public bool WormHole { get; set; }

        [Description("If true this will tile infinitely as the player moves around. Very performance friendly."), RefreshProperties(RefreshProperties.All)]
        public bool Tile { get; set; }

        [Category("Next Scene Info"), Description("This is the distance that will trigger the next scene to load."), RefreshProperties(RefreshProperties.All)]
        public float TriggerDistance { get; set; }
    }

    [System.Serializable]
    public partial class InteriorInfo
    {
        public InteriorInfo()
        {
            Type = InteriorType.MapEditor;
        }

        [Description("The name of this interior. If this is a mapeditor file, it should include the .xml extension."), RefreshProperties(RefreshProperties.All)]
        public string Name { get; set; }

        [Description("The interior type."), RefreshProperties(RefreshProperties.All)]
        public InteriorType Type { get; set; }
    }

    [System.Serializable]
    public partial class TeleportPoint
    {
        [Description("The starting point of the teleport. This will recieve have a minimap blip icon in-game."), RefreshProperties(RefreshProperties.All)]
        public Vector3 Start { get; set; }

        [Description("The ending point of the teleport."), RefreshProperties(RefreshProperties.All)]
        public Vector3 End { get; set; }
    }

    [System.Serializable]
    public partial class ScenarioInfo
    {
        [Description("The name of the dll."), RefreshProperties(RefreshProperties.All)]
        public string Dll { get; set; }

        [Description("The namespace directory to the class. Example 'MyNamespace.MyClassName' NOTE: This class must derive from CustomScenario."), RefreshProperties(RefreshProperties.All)]
        public string Namespace { get; set; }
    }

    public partial class NextSceneInfo
    {
        [Category("Next Scene Info"), Description("The position of the player, offsetted from the center of space, when the next scene loads."), RefreshProperties(RefreshProperties.All)]
        public virtual Vector3 NextScenePosition { get; set; }

        [Category("Next Scene Info"), Description("The rotation of the player when the next scene loads."), RefreshProperties(RefreshProperties.All)]
        public virtual Vector3 NextSceneRotation { get; set; }

        [Category("Next Scene Info"), Description("The filename of the next scene that will load."), RefreshProperties(RefreshProperties.All)]
        public virtual string NextScene { get; set; }
    }

    public interface IDrawable
    {
        string Model { get; set; }

        Vector3 Position { get; set; }
    }

    public interface ITrigger
    {
        float TriggerDistance { get; set; }
    }
}
