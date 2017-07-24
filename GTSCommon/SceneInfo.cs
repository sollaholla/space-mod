﻿/*
 *  Soloman Northrop (c) 2017
 *  https://www.youtube.com/channel/UCd7mQswuowjJCeaChSgnYtA
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
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
        return new GTA.Math.Vector3(obj.X, obj.Y, obj.Z);
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
        Vector3 result = new Vector3(l.X, l.Y, l.Z) + new Vector3(r.X, r.Y, r.Z);
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

    public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
    {
        try
        {
            string[] tokens = ((string)value).Split(';');

            return new XVector3(float.Parse(tokens[0]), float.Parse(tokens[1]), float.Parse(tokens[2]));
        }
        catch
        {
            return context.PropertyDescriptor.GetValue(context.Instance);
        }
    }

    public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
    {
        XVector3 p = (XVector3)value;

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

    [Description("The model name of the skybox.")]
    [Category("Core Settings")]
    [RefreshProperties(RefreshProperties.All)]
    public string SkyboxModel { get; set; }

    [Description("Time time of day in hours that we will use for the scene.")]
    [Category("Core Settings")]
    [RefreshProperties(RefreshProperties.All)]
    public int Time { get; set; }

    [Description("True if you want to use gravity for this scene.")]
    [Category("Core Settings")]
    [RefreshProperties(RefreshProperties.All)]
    public bool UseGravity { get; set; }

    [Description("Gravity in m/s^2")]
    [Category("Core Settings")]
    [RefreshProperties(RefreshProperties.All)]
    public float GravityLevel { get; set; }

    [Description("The origin of the skybox, and all props.")]
    [Category("Core Settings")]
    [RefreshProperties(RefreshProperties.All)]
    public XVector3 GalaxyCenter { get; set; } = new XVector3(-10000, -10000, 10000);

    [Category("Surface Settings")]
    [Description(
        "The angular speed multiplier for the skybox. Rotation is based your angle to any celestial body. (E.g. Travelling far enough north of a celestail body will hide it behind the southern horizon line)")]
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
    [Category("Required")]
    [Description("The name of the ydr/ydd model. Example: 'earth_large'")]
    [RefreshProperties(RefreshProperties.All)]
    public string Model { get; set; }

    [Category("Required")]
    [Description("The position of this object offsetted from the center of space.")]
    [RefreshProperties(RefreshProperties.All)]
    public XVector3 Position { get; set; }

    [Category("Other")]
    [Description("The starting rotation of the object.")]
    [RefreshProperties(RefreshProperties.All)]
    public XVector3 Rotation { get; set; }
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
    public string Model { get; set; }

    [RefreshProperties(RefreshProperties.All)]
    public XVector3 Position { get; set; }

    [RefreshProperties(RefreshProperties.All)]
    public float ParallaxScale { get; set; }
}

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

public interface IDrawable
{
    string Model { get; set; }

    XVector3 Position { get; set; }
}

public interface ITrigger
{
    float TriggerDistance { get; set; }
}