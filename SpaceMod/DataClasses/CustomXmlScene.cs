using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using GTA.Math;

namespace SpaceMod.DataClasses
{
    public enum TimeType
    {
        Day,
        Evening,
        Night
    }

    public class CustomXmlScene
    {
        public string SpaceDomeModel { get; set; }

        public List<OrbitalData> Orbitals { get; set; }

        public List<LockedOrbitalData> LockedOrbitals { get; set; }

        public List<Link> SceneLinks { get; set; }

        public TimeType Time { get; set; } = TimeType.Night;

        public bool UseGravity { get; set; }

        public int GravityLevel { get; set; } = 3;

        public bool SurfaceFlag { get; set; }

        public string NextSceneOffSurface { get; set; }

        public Vector3 SurfaceExitRotation { get; set; }

        public List<IplData> Ipls { get; set; }
        
        public List<ScenarioData> CustomScenarios { get; set; }

        [XmlIgnore] public IplData CurrentIplData { get; set; }

        [XmlIgnore] public string LastSceneFile { get; set; }
    }

    public class Link
    {
        public string Name { get; set; }

        public string NextSceneFile { get; set; }

        public Vector3 OriginOffset { get; set; }

        public float ExitDistance { get; set; } = 1500;

        public Vector3 ExitRotation { get; set; }
    }

    public class LockedOrbitalData
    {
        public string Model { get; set; }

        public Vector3 Offset { get; set; }
    }

    public class OrbitalData
    {
        public string Name { get; set; }

        public string Model { get; set; }

        public Vector3 OriginOffset { get; set; }

        public float RotationSpeed { get; set; }

        public bool IsWormHole { get; set; }

        public float ExitDistance { get; set; } = 1500;

        public Vector3 ExitRotation { get; set; }

        public string NextSceneFile { get; set; }
    }

    public class IplData
    {
        public string Name { get; set; }

        public IplType Type { get; set; } = IplType.MapEditor;

        public List<Teleport> Teleports { get; set; }

        public TimeType Time { get; set; } = TimeType.Night;

        [XmlIgnore] public Ipl CurrentIpl { get; set; }
    }

    public class Teleport
    {
        public Vector3 Start { get; set; }

        public Vector3 End { get; set; }

        public IplData EndIpl { get; set; }
    }

    public class ScenarioData
    {
        public string Name { get; set; }

        public string PathToClass { get; set; }
    }
}
