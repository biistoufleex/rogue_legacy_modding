using Microsoft.Xna.Framework;

namespace RogueCastle
{
    // Local co-op tuning (see docs/COOP_PLAN.md). Everything gameplay-related lives here.
    static class CoopEV
    {
#if MOD_COOP
        public const bool COOP_ENABLED = true;
#else
        public const bool COOP_ENABLED = false; // Weapons-only build: no Player 2
#endif

        // Classes Player 2 can cycle through with SELECT in the starting camp, and the spell each one starts with.
        public static readonly byte[] P2_CLASSES = { ClassType.Knight, ClassType.Wizard, ClassType.Barbarian };
        // Vanilla spells only: the mod's guns (Pistol/Shotgun/SMG) are not available to P2.
        public static readonly byte[] P2_START_SPELLS = { SpellType.Dagger, SpellType.Boomerang, SpellType.Axe };

        // Class powers (Y / block button) only exist on upgraded classes: Paladin (DOWN + Y: statue),
        // Archmage (Y: cycle 3 spells), Barbarian King (Y: shout). false = P2 gets the upgraded class only if
        // that upgrade is unlocked in the manor (like P1's heirs); true = always.
        public const bool P2_ALWAYS_CLASS_POWERS = false;
        // Archmage's 3 swappable spells (Y), starting with the Mage's start spell. Vanilla spells only.
        public static readonly Vector3 P2_ARCHMAGE_SPELLS = new Vector3(SpellType.Boomerang, SpellType.Dagger, SpellType.Axe);

        // Invisible screen walls (left, right, top). Distances from a player's centre to the camera edge.
        public const float SCREEN_MARGIN = 60f;
        public const float SCREEN_MARGIN_TOP = 100f;         // larger: keeps the head on screen
        // No bottom wall: a player who falls off-screen (only way to leave it) comes back next to P1 after this delay.
        public const float OFFSCREEN_TELEPORT_DELAY = 2f;
        public const float REVIVE_DELAY = 8f;                // seconds P2 stays knocked out
        public const float REVIVE_HP = 0.5f;                 // fraction of max HP when P2 comes back
        public const float GHOST_OPACITY = 0.35f;            // P2 opacity while knocked out
        public const float CLASS_SWITCH_MAX_PRESS = 0.5f;    // a SELECT press shorter than this cycles the class
        public const float LEAVE_HOLD = 3f;                  // hold SELECT this long (real seconds) to leave
        public const float RETARGET_INTERVAL = 0.25f;        // seconds between enemy re-targeting passes
        public const float RETARGET_SWITCH_RATIO = 0.8f;     // an enemy switches player only if the other one is < 80% of the distance

        public static readonly Color P2_OUTLINE = Color.Cyan;
        public static readonly Vector2 P2_HUD_POSITION = new Vector2(1030, 170); // under the minimap
    }
}
