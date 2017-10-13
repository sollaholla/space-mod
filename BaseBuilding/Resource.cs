using System.Collections.Generic;
using System.Linq;

namespace BaseBuilding
{
    public class Resource
    {
        public int Id { get; set; }

        public virtual int Amount { get; set; }

        /// <summary>
        /// Returns the name of the given resource from it's definition.
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="definitions"></param>
        /// <returns></returns>
        public static string GetName(Resource resource, List<ResourceDefinition> definitions)
        {
            return definitions?.Find(x => x.Id == resource.Id)?.Name ?? "No name found...";
        }

        /// <summary>
        /// Returns true if the player resources contains the resource specified, including the amount.
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="playersResources"></param>
        /// <param name="amountCheck">If true, then we will check if the player has the same amount of this
        /// resource as the resource itself has.</param>
        /// <returns></returns>
        public static bool DoesHaveResource(Resource resource, List<PlayerResource> playersResources, bool amountCheck = true)
        {
            return playersResources.Any(x => x.Id == resource.Id && (x.Amount >= resource.Amount || !amountCheck));
        }

        /// <summary>
        /// Get's a reference to the remaining resources required to build this object.
        /// </summary>
        /// <param name="buildableObjectInfo">The objects information.</param>
        /// <param name="playersResources">The player resources.</param>
        /// <returns></returns>
        public static List<Resource> GetRemainingResourcesRequired(ObjectInfo buildableObjectInfo, List<PlayerResource> playersResources)
        {
            // If the player already has these resources we return null; otherwise, we 
            // return a list of resources the player needs.

            // If the player has no resources, we return all the resources required.
            if (playersResources.Count == 0) return buildableObjectInfo.ResourcesRequired;

            return buildableObjectInfo.ResourcesRequired.TrueForAll(x => DoesHaveResource(x, playersResources))
                ? null
                : (from pR in playersResources
                    from r in buildableObjectInfo.ResourcesRequired
                    where pR.Id == r.Id && r.Amount > pR.Amount
                    select new Resource {Id = r.Id, Amount = r.Amount - pR.Amount}).ToList();
        }
    }
}