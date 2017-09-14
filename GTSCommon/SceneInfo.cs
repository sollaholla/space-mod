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

namespace GTSCommon
{
    public enum InteriorType
    {
        Gta,
        MapEditor
    }

    [TypeConverter(typeof(Vector3Converter))]
    public struct XVector3
    {
        public bool Equals(XVector3 other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is XVector3 && Equals((XVector3) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                // ReSharper disable NonReadonlyMemberInGetHashCode
                var hashCode = X.GetHashCode();
                hashCode = (hashCode * 397) ^ Y.GetHashCode();
                hashCode = (hashCode * 397) ^ Z.GetHashCode();
                return hashCode;
            }
        }

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
                var s = (string) value;
                if (s == null) return null;
                var tokens = s.Split(';');
                return new XVector3(float.Parse(tokens[0]), float.Parse(tokens[1]), float.Parse(tokens[2]));
            }
            catch
            {
                if (context.PropertyDescriptor != null) return context.PropertyDescriptor.GetValue(context.Instance);
            }
            return null;
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
            TimecycleAreas = new List<TimecycleArea>();
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
        public List<TimecycleArea> TimecycleAreas { get; set; }

        [Description("The model name of the skybox.")]
        [Category("Core.Models")]
        [RefreshProperties(RefreshProperties.All)]
        public string SkyboxModel { get; set; }

        [Description("Time time of day in hours that we will use for the scene.")]
        [Category("Core.Weather")]
        [RefreshProperties(RefreshProperties.All)]
        public int Time { get; set; }

        [Description("Time time of day in minutes that we will use for the scene.")]
        [Category("Core.Weather")]
        [RefreshProperties(RefreshProperties.All)]
        public int TimeMinutes { get; set; }

        [Description("True if you want to use gravity for this scene.")]
        [Category("Core.Physics")]
        [RefreshProperties(RefreshProperties.All)]
        public bool UseGravity { get; set; }

        [Description("Gravity in m/s^2")]
        [Category("Core.Physics")]
        [RefreshProperties(RefreshProperties.All)]
        public float GravityLevel { get; set; }

        [Description(
            "The force (newt) that will be applied to the player when jumping. Remember that gravity will affect the force.")]
        [Category("Core.Physics")]
        public float JumpForceOverride { get; set; } = 10.0f;

        [Description("The position to spawn the vehicle on the surface. NOTE: May be removed in a later version.")]
        [Category("Core.Positions")]
        public XVector3 VehicleSurfaceSpawn { get; set; } = new XVector3(-10025, -10025, 1000f);

        [Description("The name of the timecycle modifier to use.")]
        [Category("Core.Rendering")]
        public string TimecycleModifier { get; set; }

        [Description("The strength of the timecycle modifier.")]
        [Category("Core.Rendering")]
        public float TimecycleModifierStrength { get; set; } = 1.0f;

        [Description("The weather index to use for this scene.")]
        [Category("Core.Weather")]
        public string WeatherName { get; set; } = "EXTRASUNNY";

        [Description("The origin of the skybox, and all props.")]
        [Category("Core.Positioning")]
        [RefreshProperties(RefreshProperties.All)]
        public XVector3 GalaxyCenter { get; set; } = new XVector3(-10000, -10000, 30000);

        [Category("Surface.Scenes")]
        [Description("The filename of the next scene that will load once we leave the surface.")]
        [RefreshProperties(RefreshProperties.All)]
        public override string NextScene { get; set; }

        [Category("Surface.Positioning")]
        [Description("The rotation of our player when the next scene loads.")]
        [RefreshProperties(RefreshProperties.All)]
        public override XVector3 NextSceneRotation { get; set; }

        [Category("Surface.Positioning")]
        [Description("The position of the player, offsetted from the center of space, when the next scene loads.")]
        [RefreshProperties(RefreshProperties.All)]
        public override XVector3 NextScenePosition { get; set; }

        [Category("Surface.Weather")]
        [Description("Set the puddle intensity on this planet. Only works for puddles that 'don't' already exist.")]
        [RefreshProperties(RefreshProperties.All)]
        public float PuddleIntensity { get; set; } = 0.0f;

        [Category("Surface.Weather")]
        [Description("Set the water wave strength.")]
        [RefreshProperties(RefreshProperties.All)]
        public float WaveStrength { get; set; }

        [Category("Surface.Weather")]
        [Description("Set the wind speed.")]
        [RefreshProperties(RefreshProperties.All)]
        public float WindSpeed { get; set; }

        [Category("Surface.Weather")]
        [Description("Don't draw 3D clouds.")]
        public bool CloudsEnabled { get; set; }

        [Category("Surface.Weather")]
        [Description("The cloud type.")]
        public string CloudType { get; set; }

        [Category("Surface.Player")]
        [Description("Use leave surface prompt when in a new vehicle.")]
        public bool LeaveSurfacePrompt { get; set; } = false;

        [Category("Surface.Player")]
        [Description("Allow orbit landing if no missions block it.")]
        public bool OrbitAllowLanding { get; set; } = true;

        [Category("Surface.Landing")]
        public XVector3 OrbitLandingPosition { get; set; } = new XVector3(0, 0, 500);

        [Category("Surface.Landing")]
        public XVector3 OrbitLandingRotation { get; set; } = new XVector3(-25, 0, 0);

        [Category("Surface.Landing")]
        public float OrbitLandingSpeed { get; set; } = 150;

        [Category("Surface.Landing")]
        [Description("The height above the galaxy center the player must be to exit the surface.")]
        [RefreshProperties(RefreshProperties.All)]
        public float OrbitLeaveHeight { get; set; } = 750;

        [Category("Surface.Positioning")]
        [Description("The position (offset) that non-landable vehicle's will be located after landing.")]
        [RefreshProperties(RefreshProperties.All)]
        public XVector3 OrbitalVehicleOffset { get; set; } = new XVector3(500, 500, 750);

        [Category("Audio")]
        [Description(
            "Use sound. Mostly for scenes (like space) in which you don't want sound to play unless in first person.")]
        public bool UseSound { get; set; }

        [XmlIgnore]
        [Category("Core.Internal")]
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

        [Category("Optional")]
        [Description("Stop this object from moving on the X axis.")]
        public bool FreezeXCoord { get; set; }

        [Category("Optional")]
        [Description("Stop this object from moving on the Y axis.")]
        public bool FreezeYCoord { get; set; }

        [Category("Optional")]
        [Description("Stop this object from moving on the Z axis.")]
        public bool FreezeZCoord { get; set; }

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

        [Category("Settings")]
        [Description("The dimensions of the terrain e.g. 1x1 makes 7 tiles ([0,0][1,0][0,1][1,1][-1,0][0,-1][-1,-1]) etc.")]
        public int Dimensions { get; set; } = 4;

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
        public float ParallaxAmount { get; set; } = 0.125f;

        [RefreshProperties(RefreshProperties.All)]
        public float ParallaxStartDistance { get; set; } = 5000f;

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
    public class TimecycleArea : ITrigger
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
}