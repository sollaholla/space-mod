/*
 *  Soloman Northrop (c) 2017
 *  https://www.youtube.com/channel/UCd7mQswuowjJCeaChSgnYtA
 */

using System.Collections.Generic;
using System.Xml.Serialization;
using GTA.Math;
using SpaceMod.Scenes.Interiors;
using GTA;

namespace SpaceMod.Scenes
{
    public enum TimeType
    {
        Day,
        Evening,
        Night
    }
    
    [System.Serializable]
    public class CustomXmlScene
    {
        public CustomXmlScene()
        {
            Time = TimeType.Night;
            GravityLevel = 3;
        }

        public string SpaceDomeModel { get; set; }

        public List<OrbitalData> Orbitals { get; set; }

        public List<LockedOrbitalData> LockedOrbitals { get; set; }

        public List<Link> SceneLinks { get; set; }

        public TimeType Time { get; set; }

        public bool UseGravity { get; set; }

        public int GravityLevel { get; set; }

        public bool SurfaceFlag { get; set; }

        public string NextSceneOffSurface { get; set; }

        public Vector3 SurfaceExitRotation { get; set; }

        public List<IplData> Ipls { get; set; }

        public List<ScenarioData> CustomScenarios { get; set; }

        [XmlIgnore] public IplData CurrentIplData { get; set; }

        [XmlIgnore] public string LastSceneFile { get; set; }
    }

    [System.Serializable]
    public class Link
    {
        public Link()
        {
            ExitDistance = 1500;
        }

        public string Name { get; set; }

        public string NextSceneFile { get; set; }

        public Vector3 OriginOffset { get; set; }

        public float ExitDistance { get; set; }

        public Vector3 ExitRotation { get; set; }
    }

    [System.Serializable]
    public partial class LockedOrbitalData
    {
        public LockedOrbitalData()
        {
            Scale = 1.0f;
        }

        public string Model { get; set; }

        public Vector3 OriginOffset { get; set; }

        public float Scale { get; set; }

        public bool EmitLight { get; set; }
    }

    [System.Serializable]
    public partial class OrbitalData : LockedOrbitalData
    {
        public OrbitalData()
        {
            ExitDistance = 1500;
        }

        public string Name { get; set; }

        public float RotationSpeed { get; set; }

        public bool IsWormHole { get; set; }

        public float ExitDistance { get; set; }

        public Vector3 ExitRotation { get; set; }

        public string NextSceneFile { get; set; }
    }

    [System.Serializable]
    public partial class IplData
    {
        public IplData()
        {
            Type = IplType.MapEditor;
            Time = TimeType.Night;
        }

        public string Name { get; set; }

        public IplType Type { get; set; }

        public List<Teleport> Teleports { get; set; }

        public TimeType Time { get; set; }

        [XmlIgnore] public Ipl CurrentIpl { get; set; }
    }

    [System.Serializable]
    public partial class Teleport
    {
        public Vector3 Start { get; set; }

        public Vector3 End { get; set; }

        public IplData EndIpl { get; set; }

        [XmlIgnore] public Blip StartBlip { get; set; }

        [XmlIgnore] public Blip EndBlip { get; set; }
    }

    [System.Serializable]
    public partial class ScenarioData
    {
        public string Name { get; set; }

        public string PathToClass { get; set; }
    }
}
