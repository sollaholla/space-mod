using System;
using System.ComponentModel;

namespace GTSCommon
{
    [Serializable]
    public class ScenarioInfo
    {
        [Description("The name of the dll.")]
        [RefreshProperties(RefreshProperties.All)]
        public string Dll { get; set; }

        [Description(
            "The namespace directory to the class. Example 'MyNamespace.MyClassName' NOTE: This class must derive from CustomScenario.")]
        [RefreshProperties(RefreshProperties.All)]
        public string TypeName { get; set; }
    }
}