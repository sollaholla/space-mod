/*
 *  Soloman Northrop (c) 2017
 *  https://www.youtube.com/channel/UCd7mQswuowjJCeaChSgnYtA
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;

namespace GTSCommon
{
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

        [Description("A value indicating whether or not you wish to use atmosphere.asi (if applicable) in this scene.")]
        [Category("VisualV.Integrations")]
        [RefreshProperties(RefreshProperties.All)]
        public bool AtmosphereEnabled { get; set; } = true;

        [Description("The file index of the atmosphere.ini's (1 / 2 / etc).")]
        [Category("VisualV.Integrations")]
        [RefreshProperties(RefreshProperties.All)]
        public int AtmosphereIndex { get; set; } = 1;

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
        public float JumpForceOverride { get; set; } = 15.0f;

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
}