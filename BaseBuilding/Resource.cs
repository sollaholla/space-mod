using System.Collections.Generic;

namespace BaseBuilding
{
    public class Resource
    {
        public int Id { get; set; }

        public virtual int Amount { get; set; }

        public static string GetName(Resource resource, List<ResourceDefinition> defs)
        {
            return defs?.Find(x => x.Id == resource.Id)?.Name ?? "No name found...";
        }
    }
}