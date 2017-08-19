/*
 *  Soloman Northrop (c) 2017
 *  https://www.youtube.com/channel/UCd7mQswuowjJCeaChSgnYtA
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Xml.Serialization;
using GTA.Math;

public enum InteriorType
{
    Gta,
    MapEditor
}

[TypeConverter(typeof(Vector3Converter))]
public struct XVector3
{
    public XVector3(float x, float y, float z) : this()
    {
        X = x;
        Y = y;
        Z = z;
    }

    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }

    public override string ToString()
    {
        return "(" + X + ", " + Y + ", " + Z + ")";
    }

    public static implicit operator Vector3(XVector3 obj)
    {
        return new Vector3(obj.X, obj.Y, obj.Z);
    }

    public static bool operator ==(Vector3 left, XVector3 right)
    {
        return left == new Vector3(right.X, right.Y, right.Z);
    }

    public static bool operator !=(Vector3 left, XVector3 right)
    {
        return !(left == new Vector3(right.X, right.Y, right.Z));
    }

    public static Vector3 operator +(XVector3 left, Vector3 right)
    {
        return new Vector3(left.X, left.Y, left.Z) + right;
    }

    public static Vector3 operator -(XVector3 left, Vector3 right)
    {
        return new Vector3(left.X, left.Y, left.Z) - right;
    }

    public static XVector3 operator +(XVector3 l, XVector3 r)
    {
        var result = new Vector3(l.X, l.Y, l.Z) + new Vector3(r.X, r.Y, r.Z);
        return new XVector3(result.X, result.Y, result.Z);
    }

    public static implicit operator XVector3(Vector3 v)
    {
        return new XVector3(v.X, v.Y, v.Z);
    }
}

public class Vector3Converter : ExpandableObjectConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        try
        {
            var tokens = ((string) value).Split(';');

            return new XVector3(float.Parse(tokens[0]), float.Parse(tokens[1]), float.Parse(tokens[2]));
        }
        catch
        {
            return context.PropertyDescriptor.GetValue(context.Instance);
        }
    }

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
        Type destinationType)
    {
        var p = (XVector3) value;

        return "(" + p.X + ", " + p.Y + ", " + p.Z + ")";
    }
}

[Serializable]
[XmlRoot("CSceneInfoData")]
public class SceneInfo : NextSceneInfo
{
    public SceneInfo()
    {
        SkyboxModel = "spacedome";
        Time = 23;
        GravityLevel = 3;
        Orbitals = new List<OrbitalInfo>();
        AttachedOrbitals = new List<AttachedOrbitalInfo>();
        Surfaces = new List<SurfaceInfo>();
        SceneLinks = new List<Link>();
        Interiors = new List<InteriorInfo>();
        Scenarios = new List<ScenarioInfo>();
        Teleports = new List<TeleportPoint>();
        Billboards = new List<Billboard>();
        TimeCycleAreas = new List<TimeCycleArea>();
    }

    [Category("Collections")]
    [RefreshProperties(RefreshProperties.All)]
    public List<OrbitalInfo> Orbitals { get; set; }

    [Category("Collections")]
    [RefreshProperties(RefreshProperties.All)]
    public List<AttachedOrbitalInfo> AttachedOrbitals { get; set; }

    [Category("Collections")]
    [RefreshProperties(RefreshProperties.All)]
    public List<SurfaceInfo> Surfaces { get; set; }

    [Category("Collections")]
    [RefreshProperties(RefreshProperties.All)]
    public List<Link> SceneLinks { get; set; }

    [Category("Collections")]
    [RefreshProperties(RefreshProperties.All)]
    public List<InteriorInfo> Interiors { get; set; }

    [Category("Collections")]
    [RefreshProperties(RefreshProperties.All)]
    public List<ScenarioInfo> Scenarios { get; set; }

    [Category("Collections")]
    [RefreshProperties(RefreshProperties.All)]
    public List<TeleportPoint> Teleports { get; set; }

    [Category("Collections")]
    [RefreshProperties(RefreshProperties.All)]
    public List<Billboard> Billboards { get; set; }

    [Category("Collections")]
    [RefreshProperties(RefreshProperties.All)]
    public List<TimeCycleArea> TimeCycleAreas { get; set; }

    [Description("The model name of the skybox.")]
    [Category("Core Settings")]
    [RefreshProperties(RefreshProperties.All)]
    public string SkyboxModel { get; set; }

    [Description("Time time of day in hours that we will use for the scene.")]
    [Category("Core Settings")]
    [RefreshProperties(RefreshProperties.All)]
    public int Time { get; set; }

    [Description("Time time of day in minutes that we will use for the scene.")]
    [Category("Core Settings")]
    [RefreshProperties(RefreshProperties.All)]
    public int TimeMinutes { get; set; }

    [Description("True if you want to use gravity for this scene.")]
    [Category("Core Settings")]
    [RefreshProperties(RefreshProperties.All)]
    public bool UseGravity { get; set; }

    [Description("Gravity in m/s^2")]
    [Category("Core Settings")]
    [RefreshProperties(RefreshProperties.All)]
    public float GravityLevel { get; set; }

    [Description(
        "The force (newt) that will be applied to the player when jumping. Remember that gravity will affect the force.")]
    [Category("Core Settings")]
    public float JumpForceOverride { get; set; } = 10.0f;

    [Description("The position to spawn the vehicle on the surface. NOTE: May be removed in a later version.")]
    [Category("Core Settings")]
    public XVector3 VehicleSurfaceSpawn { get; set; } = new XVector3(-10029.57f, -10016.75f, 10001f);

    [Description("The name of the timecycle modifier to use.")]
    [Category("Core Settings")]
    public string TimecycleModifier { get; set; }

    [Description("The strength of the timecycle modifier.")]
    [Category("Core Settings")]
    public float TimecycleModifierStrength { get; set; } = 1.0f;

    [Description("The weather index to use for this scene.")]
    [Category("Core Settings")]
    public string WeatherName { get; set; } = "EXTRASUNNY";

    [Description("The origin of the skybox, and all props.")]
    [Category("Core Settings")]
    [RefreshProperties(RefreshProperties.All)]
    public XVector3 GalaxyCenter { get; set; } = new XVector3(-10000, -10000, 30000);

    [Category("Surface Settings")]
    [Description("The speed at which the sky rotates based on our movement.")]
    public float HorizonRotationMultiplier { get; set; } = 0.00005f;

    [Category("Surface Settings")]
    [Description("The filename of the next scene that will load once we leave the surface.")]
    [RefreshProperties(RefreshProperties.All)]
    public override string NextScene { get; set; }

    [Category("Surface Settings")]
    [Description("The rotation of our player when the next scene loads.")]
    [RefreshProperties(RefreshProperties.All)]
    public override XVector3 NextSceneRotation { get; set; }

    [Category("Surface Settings")]
    [Description("The position of the player, offsetted from the center of space, when the next scene loads.")]
    [RefreshProperties(RefreshProperties.All)]
    public override XVector3 NextScenePosition { get; set; }

    [Category("Surface Settings")]
    [Description("Set the water wave strength.")]
    [RefreshProperties(RefreshProperties.All)]
    public float WaveStrength { get; set; }

    [Category("Surface Settings")]
    [Description("Set the wind speed.")]
    [RefreshProperties(RefreshProperties.All)]
    public float WindSpeed { get; set; }

    [Category("Surface Settings")]
    [Description("Don't draw 3D clouds.")]
    public bool CloudsEnabled { get; set; }

    [Category("Surface Settings")]
    [Description("The cloud type.")]
    public string CloudType { get; set; }

    [Category("Surface Settings")]
    [Description("The minimum depth (distance from Galaxy Center Z) that we will begin to damage the player.")]
    public float CrushMinDepth { get; set; }

    [Category("Surface Settings")]
    [Description("The maximum crush depth (distance from Galaxy Center Z), if reached will kill the player.")]
    public float CrushMaxDepth { get; set; }

    [Category("Surface Settings")]
    [Description("This is the damage multiplier for Min Max crush depth.")]
    public float CrushDamageMultiplier { get; set; }

    [Category("Surface Settings")]
    [Description("Use leave surface prompt when in a new vehicle.")]
    public bool LeaveSurfacePrompt { get; set; } = false;

    [Category("Surface Settings")]
    [Description("Allow orbit landing if no missions block it.")]
    public bool OrbitAllowLanding { get; set; } = true;

    [Category("Surface Settings")]
    public XVector3 OrbitLandingPosition { get; set; } = new XVector3(0, 0, 500);

    [Category("Surface Settings")]
    public XVector3 OrbitLandingRotation { get; set; } = new XVector3(-25, 0, 0);

    [Category("Surface Settings")]
    public float OrbitLandingSpeed { get; set; } = 150f;

    [Category("Surface Settings")]
    [Description("The height above the galaxy center the player must be to exit the surface.")]
    [RefreshProperties(RefreshProperties.All)]
    public float OrbitLeaveHeight { get; set; } = 750f;

    [Category("Surface Settings")]
    [Description("The position (offset) that non-landable vehicle's will be located after landing.")]
    [RefreshProperties(RefreshProperties.All)]
    public XVector3 OrbitalVehicleOffset { get; set; } = new XVector3(250, 190, 750);

    [Category("Audio")]
    [Description(
        "Use sound. Mostly for scenes (like space) in which you don't want sound to play unless in first person.")]
    public bool UseSound { get; set; }

    [XmlIgnore]
    public bool SurfaceScene => Surfaces.Any();
}

[Serializable]
public class Link : NextSceneInfo, ITrigger
{
    public Link()
    {
        TriggerDistance = 1500;
    }

    [Category("Required")]
    [Description("This is the name that will be displayed on screen by the custom UI.")]
    [RefreshProperties(RefreshProperties.All)]
    public string Name { get; set; }

    [Category("Required")]
    [Description("The position of this trigger offsetted from the center of space.")]
    [RefreshProperties(RefreshProperties.All)]
    public XVector3 Position { get; set; }

    [Category("Next Scene Info")]
    [Description("This is the distance that will trigger the next scene to load.")]
    [RefreshProperties(RefreshProperties.All)]
    public float TriggerDistance { get; set; }
}

[Serializable]
public class AttachedOrbitalInfo : IDrawable
{
    [Category("Other")]
    [Description("The starting rotation of the object.")]
    [RefreshProperties(RefreshProperties.All)]
    public XVector3 Rotation { get; set; }

    [Category("Required")]
    [Description("The name of the ydr/ydd model. Example: 'earth_large'")]
    [RefreshProperties(RefreshProperties.All)]
    public string Model { get; set; }

    [Category("Required")]
    [Description("The position of this object offsetted from the center of space.")]
    [RefreshProperties(RefreshProperties.All)]
    public XVector3 Position { get; set; }

    public int LodDistance { get; set; } = -1;
}

[Serializable]
public class OrbitalInfo : NextSceneInfo, IDrawable, ITrigger
{
    public OrbitalInfo()
    {
        TriggerDistance = 1500;
    }

    [Description("This is the name that will be displayed on screen by the custom UI.")]
    [RefreshProperties(RefreshProperties.All)]
    public string Name { get; set; }

    [Description("The rotation speed (degrees per-second).")]
    [RefreshProperties(RefreshProperties.All)]
    public float RotationSpeed { get; set; }

    [Description("True if you wish for this object to act like a wormhole, and suck the player in.")]
    [RefreshProperties(RefreshProperties.All)]
    public bool WormHole { get; set; }

    [Category("Other")]
    [Description("The starting rotation of the object.")]
    [RefreshProperties(RefreshProperties.All)]
    public XVector3 Rotation { get; set; }

    [Category("Required")]
    [Description("The name of the ydr/ydd model. Example: 'earth_large'")]
    [RefreshProperties(RefreshProperties.All)]
    public string Model { get; set; }

    [Category("Required")]
    [Description("The position of this object offsetted from the center of space.")]
    [RefreshProperties(RefreshProperties.All)]
    public XVector3 Position { get; set; }

    public int LodDistance { get; set; } = -1;

    [Category("Next Scene Info")]
    [Description("This is the distance that will trigger the next scene to load.")]
    [RefreshProperties(RefreshProperties.All)]
    public float TriggerDistance { get; set; }
}

[Serializable]
public class SurfaceInfo : IDrawable
{
    [Category("Settings")]
    [Description(
        "True if you want this surface to tile infinitely. WARNING: Your model should be seamless on each edge.")]
    public bool Tile { get; set; }

    [Category("Settings")]
    [Description(
        "The size of your terrain in meters. 1024 is the default scale. This will affect how far your terrain tiles are generated from the parent terrain tile.")]
    public float TileSize { get; set; } = 1024;

    [Category("Required")]
    [Description("The name of the ydr/ydd model. Example: 'earth_large'")]
    [RefreshProperties(RefreshProperties.All)]
    public string Model { get; set; }

    [Category("Required")]
    [Description("The position of this object offsetted from the center of space.")]
    [RefreshProperties(RefreshProperties.All)]
    public XVector3 Position { get; set; }

    [Category("Settings")]
    public int LodDistance { get; set; } = -1;
}

[Serializable]
public class InteriorInfo
{
    public InteriorInfo()
    {
        Type = InteriorType.MapEditor;
    }

    [Description("The name of this interior. If this is a mapeditor file, it should include the .xml extension.")]
    [RefreshProperties(RefreshProperties.All)]
    public string Name { get; set; }

    [Description("The interior type.")]
    [RefreshProperties(RefreshProperties.All)]
    public InteriorType Type { get; set; }
}

[Serializable]
public class TeleportPoint
{
    [Description("True if you want this point to have a blip.")]
    [RefreshProperties(RefreshProperties.All)]
    public bool CreateBlip { get; set; } = true;

    [Description("The starting point of the teleport. This will recieve have a minimap blip icon in-game.")]
    [RefreshProperties(RefreshProperties.All)]
    public XVector3 Start { get; set; }

    [Description("True if you want the start point to have an in-game marker.")]
    public bool StartMarker { get; set; } = true;

    [Description("Set the heading of the player after going from 'End' to 'Start'.")]
    public float StartHeading { get; set; }

    [Description("The ending point of the teleport.")]
    [RefreshProperties(RefreshProperties.All)]
    public XVector3 End { get; set; }

    [Description("True if you want the end point to have an in-game marker.")]
    public bool EndMarker { get; set; } = true;

    [Description("Set the heading of the player after going from 'Start' to 'End'.")]
    [RefreshProperties(RefreshProperties.All)]
    public float EndHeading { get; set; }
}

[Serializable]
public class ScenarioInfo
{
    [Description("The name of the dll.")]
    [RefreshProperties(RefreshProperties.All)]
    public string Dll { get; set; }

    [Description(
        "The namespace directory to the class. Example 'MyNamespace.MyClassName' NOTE: This class must derive from CustomScenario.")]
    [RefreshProperties(RefreshProperties.All)]
    public string Namespace { get; set; }
}

[Serializable]
public class Billboard : IDrawable
{
    [RefreshProperties(RefreshProperties.All)]
    public float ParallaxScale { get; set; }

    [RefreshProperties(RefreshProperties.All)]
    public string Model { get; set; }

    [RefreshProperties(RefreshProperties.All)]
    public XVector3 Position { get; set; }

    public int LodDistance { get; set; } = -1;
}

[Serializable]
public class NextSceneInfo
{
    [Category("Next Scene Info")]
    [Description("The position of the player, offsetted from the center of space, when the next scene loads.")]
    [RefreshProperties(RefreshProperties.All)]
    public virtual XVector3 NextScenePosition { get; set; }

    [Category("Next Scene Info")]
    [Description("The rotation of the player when the next scene loads.")]
    [RefreshProperties(RefreshProperties.All)]
    public virtual XVector3 NextSceneRotation { get; set; }

    [Category("Next Scene Info")]
    [Description("The filename of the next scene that will load.")]
    [RefreshProperties(RefreshProperties.All)]
    public virtual string NextScene { get; set; }
}

[Serializable]
public class TimeCycleArea : ITrigger
{
    [Category("General")]
    public int Time { get; set; } = 23;

    [Category("General")]
    public int TimeMinutes { get; set; }

    [Category("General")]
    public XVector3 Location { get; set; }

    [Category("Weather")]
    public string TimeCycleModifier { get; set; }

    [Category("Weather")]
    public float TimeCycleModifierStrength { get; set; }

    [Category("Weather")]
    public string WeatherName { get; set; }

    [Category("General")]
    public float TriggerDistance { get; set; }
}

public interface IDrawable
{
    string Model { get; set; }

    XVector3 Position { get; set; }

    int LodDistance { get; set; }
}

public interface ITrigger
{
    float TriggerDistance { get; set; }
}