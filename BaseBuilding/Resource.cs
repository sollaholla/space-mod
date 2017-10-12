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

        public static bool DoesHaveEnoughResources(Resource r, bool amountCheck = true)
        {
            return BaseBuildingCore.PlayerResources.Any(x => x.Id == r.Id && (x.Amount >= r.Amount || !amountCheck));
        }

        public static List<Resource> GetResourcesRequired(ObjectInfo o)
        {
            if (o.ResourcesRequired.TrueForAll(x => DoesHaveEnoughResources(x)))
                return null;

            var resoursesRequired = new List<Resource>();

            foreach (var pR in BaseBuildingCore.PlayerResources)
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