using System.Collections.Generic;
using System.Linq;

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

        public static bool DoesHaveEnoughResources(Resource r, List<PlayerResource> playersResources, bool amountCheck = true)
        {
            return playersResources.Any(x => x.Id == r.Id && (x.Amount >= r.Amount || !amountCheck));
        }

        public static List<Resource> GetResourcesRequired(ObjectInfo o, List<PlayerResource> playersResources)
        {
            if (o.ResourcesRequired.TrueForAll(x => DoesHaveEnoughResources(x, playersResources)))
                return null;

            var resoursesRequired = new List<Resource>();

            foreach (var pR in playersResources)
            {
                foreach (Resource r in o.ResourcesRequired)
                {
                    if (pR.Id == r.Id && r.Amount > pR.Amount)
                    {
                        resoursesRequired.Add(new Resource() { Id = r.Id, Amount = r.Amount - pR.Amount });
                    }
                }
            }

            return resoursesRequired;
        }
    }
}