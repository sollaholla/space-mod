using System;
using System.Collections.Generic;
using System.Linq;

namespace SpaceMod.DataClasses
{
    public enum VehicleUpgrade
    {
        Engine1,
        Engine2,
        Engine3,
        WarpDrive,
        Lazers
    }

    public enum Resource
    {
        Helium3,
        Uranium,
        Diamond,
        Co2Deposits,
        NitrogenDeposits,
        DarkMatter,
        QuantumParticles,
        Titanium,
        WeaponParts
    }

    [Serializable]
    public class VehicleUpgradeData
    {
        public VehicleUpgradeData(VehicleUpgrade upgrade, bool on)
        {
            Upgrade = upgrade;
            On = on;
        }

        public VehicleUpgrade Upgrade { get; set; }
        public bool On { get; set; }
    }

    [Serializable]
    public class ResourceData
    {
        public ResourceData(Resource resource, int amount)
        {
            Resource = resource;
            Amount = amount;
        }

        public Resource Resource { get; set; }
        public int Amount { get; set; }
    }

    [Serializable]
    public class PlayerPrefs
    {
        public PlayerPrefs()
        {
            CreateVehicleUpgrades();
            CreateResources();
        }

        public List<VehicleUpgradeData> VehicleUpgrades { get; set; }
        public List<ResourceData> Resources { get; set; }

        private void CreateResources()
        {
            Resources = new List<ResourceData>();
            var values = (Resource[])Enum.GetValues(typeof(Resource));
            var toList = values.ToList();
            toList.ForEach(resource => Resources.Add(new ResourceData(resource, 0)));
        }

        private void CreateVehicleUpgrades()
        {
            VehicleUpgrades = new List<VehicleUpgradeData>();
            var values = (VehicleUpgrade[])Enum.GetValues(typeof(VehicleUpgrade));
            var toList = values.ToList();
            toList.ForEach(upgrade => VehicleUpgrades.Add(new VehicleUpgradeData(upgrade, false)));
        }

        public bool IsVehicleUpgradeActive(VehicleUpgrade upgrade)
        {
            var item = VehicleUpgrades.Find(u => u.Upgrade == upgrade);
            return item != null && item.On;
        }

        public void SetUpgradeOn(VehicleUpgrade upgrade)
        {
            var item = VehicleUpgrades.Find(u => u.Upgrade == upgrade);
            if (item == null) return;
            item.On = true;
        }
    }
}
