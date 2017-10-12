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

        public static bool DoesHaveEnoughResources(Resource r, int amount, List<PlayerResource> playersResources)
        {
            foreach (PlayerResource pR in playersResources)
            {
                if (pR.Id == r.Id && pR.Amount >= amount)
                    return true;
            }

            return false;
        }

        public static List<Resource> GetResourcesRequired(ObjectInfo o, List<PlayerResource> playersResources)
        {
            if (o.ResourcesRequired.TrueForAll(x => DoesHaveEnoughResources(x, x.Amount)))
                return null;

            var resourcesRequired = new List<Resource>();

            foreach (var pR in playersResources)
            {
                foreach (Resource r in o.ResourcesRequired)
                {
                    if (pR.Id == r.Id && r.Amount > pR.Amount)
                    {
                        resourcesRequired.Add(new Resource() { Id = r.Id, Amount = r.Amount - pR.Amount });
                    }
                }
            }

            return resourcesRequired;
        }
    }
}