using System;

namespace Game.Systems.Charm
{
    [Flags]
    public enum CharmAbility
    {
        None = 0,
        DoubleJump = 1 << 0,
        Dash = 1 << 1,
        GeoMagnet = 1 << 2,
        QuickHeal = 1 << 3,
    }
}
