using Microsoft.Xna.Framework;

namespace RogueCastle
{
    // Local co-op tuning (see docs/COOP_PLAN.md). Everything gameplay-related lives here.
    static class CoopEV
    {
        public const bool COOP_ENABLED = true;

        // Classes Player 2 can cycle through with SELECT in the starting camp, and the spell each one starts with.
        public static readonly byte[] P2_CLASSES = { ClassType.Knight, ClassType.Wizard, ClassType.Barbarian };
        public static readonly byte[] P2_START_SPELLS = { SpellType.Pistol, SpellType.SMG, SpellType.Axe };

        public const float SCREEN_MARGIN = 60f;              // min distance (px) between a player's centre and the camera edge
        public const float VERTICAL_SPLIT_LIMIT = 0.8f;      // fraction of the screen height before the camera stops following P2
        public const float OFFSCREEN_TELEPORT_DELAY = 1f;    // seconds P2 may stay off-camera before being teleported to P1
        public const float PINNED_TELEPORT_DELAY = 2f;       // seconds both players may push opposite screen edges before P2 is pulled to P1
        public const float REVIVE_DELAY = 8f;                // seconds P2 stays knocked out
        public const float REVIVE_HP = 0.5f;                 // fraction of max HP when P2 comes back
        public const float GHOST_OPACITY = 0.35f;            // P2 opacity while knocked out
        public const float CLASS_SWITCH_MAX_PRESS = 0.5f;    // a SELECT press shorter than this cycles the class
        public const float LEAVE_HOLD = 2f;                  // hold SELECT this long to leave
        public const float RETARGET_INTERVAL = 0.25f;        // seconds between enemy re-targeting passes

        public static readonly Color P2_OUTLINE = Color.Cyan;
        public static readonly Vector2 P2_HUD_POSITION = new Vector2(1030, 170); // under the minimap
    }
}
