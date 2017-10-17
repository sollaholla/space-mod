using GTA;
using GTA.Native;

namespace GTS.Library
{
    public class WeaponInfo
    {
        public WeaponInfo(WeaponHash hash, WeaponComponent[] components, WeaponTint tint, int ammo)
        {
            Hash = hash;
            Components = components;
            Tint = tint;
            Ammo = ammo;
        }

        public WeaponHash Hash { get; set; }

        public WeaponComponent[] Components { get; set; }

        public WeaponTint Tint { get; set; }

        public int Ammo { get; set; }
    }
}