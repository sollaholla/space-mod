using System;
using System.ComponentModel;

namespace GTSCommon
{
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
}