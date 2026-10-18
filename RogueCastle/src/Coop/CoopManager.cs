using System;
using DS2DEngine;
using InputSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace RogueCastle
{
    // Drop-in local co-op (Player 2). Session-only: nothing here is saved.
    // ProceduralLevelScreen calls the hooks (HandleInput, Update, camera, physics, draw); see docs/COOP_PLAN.md.
    public static class CoopManager
    {
        private enum PartnerState { Active, Down, Parked }

        public static bool IsJoined { get; private set; }
        public static PlayerObj Partner { get; private set; }

        private static PlayerStats m_stats;
        private static InputMap m_input;
        private static PlayerIndex m_padIndex;
        private static bool m_sharesP1Pad;
        private static int m_classIndex;

        private static PartnerState m_state;
        private static float m_downTimer;
        private static float m_offscreenTimer;
        private static float m_selectHeld;
        private static float m_retargetTimer;

        private static PlayerHUDObj m_hud;
        private static SpriteObj m_hudSpellIcon;
        private static TextObj m_hudLabel;
        private static TextObj m_tag;

        /// <summary>P2 is in the level, alive and controllable.</summary>
        public static bool PartnerActive
        {
            get { return IsJoined && Partner != null && m_state == PartnerState.Active && Partner.IsKilled == false; }
        }

        #region Join / leave

        // Returns true when the input was consumed (so P1's pause doesn't also trigger on a shared pad).
        public static bool HandleInput(ProceduralLevelScreen level)
        {
            if (CoopEV.COOP_ENABLED == false || level.Player == null || level.CurrentRoom == null)
                return false;

            if (IsJoined == false)
                return CheckJoin(level);

            if (Partner.AttachedLevel != level)
                return false;

            if (InputManager.GamePadIsConnected(m_padIndex) == false)
            {
                Leave(level);
                return false;
            }

            float elapsed = 1 / 60f;
            bool consumed = false;

            // SELECT: short press = next class (camp only), long hold = leave.
            if (InputManager.Pressed(Buttons.Back, m_padIndex))
            {
                m_selectHeld += elapsed;
                if (m_selectHeld >= CoopEV.LEAVE_HOLD)
                {
                    Leave(level);
                    return true;
                }
            }
            else if (m_selectHeld > 0)
            {
                if (m_selectHeld < CoopEV.CLASS_SWITCH_MAX_PRESS && level.CurrentRoom.Name == "Start" && PartnerActive)
                    CycleClass(level);
                m_selectHeld = 0;
            }

            // START on P2's pad opens the shared pause screen (on a shared pad P1's GlobalInput already does it).
            if (m_sharesP1Pad == false && InputManager.JustPressed(Buttons.Start, m_padIndex) && level.CurrentRoom.Name != "Ending")
            {
                (level.ScreenManager as RCScreenManager).DisplayScreen(ScreenType.Pause, true);
                consumed = true;
            }

            // P2 is frozen whenever P1 is (cutscenes, boss intros, menus).
            if (PartnerActive && level.Player.ControlsLocked == false && level.Player.IsKilled == false)
                Partner.HandleInput();

            return consumed;
        }

        private static bool CheckJoin(ProceduralLevelScreen level)
        {
            PlayerIndex? pad = null;
            for (PlayerIndex i = PlayerIndex.Two; i <= PlayerIndex.Four; i++)
            {
                if (InputManager.JustPressed(Buttons.Start, i))
                {
                    pad = i;
                    break;
                }
            }

            bool sharedPad = false;
            // Only one pad and P1 on keyboard: SELECT + START on pad 1.
            if (pad == null && InputManager.Pressed(Buttons.Back, PlayerIndex.One) && InputManager.JustPressed(Buttons.Start, PlayerIndex.One))
            {
                pad = PlayerIndex.One;
                sharedPad = true;
            }

            if (pad == null)
                return false;

            PlayerObj p1 = level.Player;
            if (level.CurrentRoom.Name != "Start" || p1.IsKilled == true || p1.ControlsLocked == true || Game.PlayerStats.TutorialComplete == false)
            {
                level.TextManager.DisplayStringText("LOC_ID_COOP_JOIN_AT_START", Color.Cyan, new Vector2(p1.X, p1.Bounds.Top - 60));
                SoundManager.PlaySound("Error_Spell");
                return sharedPad;
            }

            Join(level, pad.Value, sharedPad);
            return true;
        }

        private static void Join(ProceduralLevelScreen level, PlayerIndex padIndex, bool sharesP1Pad)
        {
            m_padIndex = padIndex;
            m_sharesP1Pad = sharesP1Pad;
            m_classIndex = 0;
            m_input = CreateInputMap(padIndex);
            if (sharesP1Pad)
                Game.GlobalInput.LockGamepadInput = true;

            m_stats = new PlayerStats(); // random look, no traits
            ApplyClass(m_stats, m_classIndex);
            CopyGearFromP1(m_stats);

            Game game = level.ScreenManager.Game as Game;
            Partner = new PlayerObj("PlayerIdle_Character", padIndex, game.PhysicsManager, level, game, 2, m_stats, m_input);
            Partner.Initialize();
            Partner.AttachLevel(level);

            IsJoined = true;
            CreateHUD();
            Revive(level, 1f);
            AddToPhysics(level);

            SoundManager.PlaySound("mfqt_teleport_in");
            level.TextManager.DisplayStringText("LOC_ID_COOP_JOINED", Color.Cyan, new Vector2(Partner.X, Partner.Bounds.Top - 60));
        }

        public static void Leave(ProceduralLevelScreen level)
        {
            if (IsJoined == false)
                return;

            RetargetEnemies(level, true);
            if (level != null && level.PhysicsManager != null)
                level.PhysicsManager.RemoveObject(Partner);
            if (level != null && level.TextManager != null && level.Player != null)
                level.TextManager.DisplayStringText("LOC_ID_COOP_LEFT", Color.Cyan, new Vector2(level.Player.X, level.Player.Bounds.Top - 60));

            Game.GlobalInput.LockGamepadInput = false;
            IsJoined = false;
            Partner = null; // not disposed: projectiles/enemies may still reference it this frame
            m_input = null;
            m_stats = null;
            m_hud = null;
        }

        // PhysicsManager.AddObject throws if the object is already in the list.
        private static void AddToPhysics(ProceduralLevelScreen level)
        {
            if (Partner.PhysicsMngr == null)
                level.PhysicsManager.AddObject(Partner);
        }

        private static InputMap CreateInputMap(PlayerIndex padIndex)
        {
            // Same layout as P1's default gamepad bindings (Game.InitializeGlobalInput), keyboard disabled.
            InputMap map = new InputMap(padIndex, true);
            map.LockKeyboardInput = true;
            map.AddInput(InputMapType.MENU_CONFIRM1, Buttons.A);
            map.AddInput(InputMapType.MENU_CANCEL1, Buttons.B);
            map.AddInput(InputMapType.PLAYER_JUMP1, Buttons.A);
            map.AddInput(InputMapType.PLAYER_ATTACK, Buttons.X);
            map.AddInput(InputMapType.PLAYER_BLOCK, Buttons.Y);
            map.AddInput(InputMapType.PLAYER_DASHLEFT, Buttons.LeftTrigger);
            map.AddInput(InputMapType.PLAYER_DASHRIGHT, Buttons.RightTrigger);
            map.AddInput(InputMapType.PLAYER_UP1, Buttons.DPadUp);
            map.AddInput(InputMapType.PLAYER_UP2, ThumbStick.LeftStick, -90, 30);
            map.AddInput(InputMapType.PLAYER_DOWN1, Buttons.DPadDown);
            map.AddInput(InputMapType.PLAYER_DOWN2, ThumbStick.LeftStick, 90, 37);
            map.AddInput(InputMapType.PLAYER_LEFT1, Buttons.DPadLeft);
            map.AddInput(InputMapType.PLAYER_LEFT2, Buttons.LeftThumbstickLeft);
            map.AddInput(InputMapType.PLAYER_RIGHT1, Buttons.DPadRight);
            map.AddInput(InputMapType.PLAYER_RIGHT2, Buttons.LeftThumbstickRight);
            map.AddInput(InputMapType.PLAYER_SPELL1, Buttons.B);
            return map;
        }

        #endregion

        #region Class & stats

        private static void ApplyClass(PlayerStats stats, int classIndex)
        {
            stats.Class = CoopEV.P2_CLASSES[classIndex];
            stats.Spell = CoopEV.P2_START_SPELLS[classIndex];
            stats.Traits = new Vector2(TraitType.None, TraitType.None);
        }

        // P2 has no save of its own: it borrows P1's gear, runes and permanent stat bonuses.
        private static void CopyGearFromP1(PlayerStats stats)
        {
            PlayerStats p1 = Game.PlayerStats;
            for (int i = 0; i < stats.GetEquippedArray.Length; i++)
            {
                stats.GetEquippedArray[i] = p1.GetEquippedArray[i];
                stats.GetEquippedRuneArray[i] = p1.GetEquippedRuneArray[i];
            }
            stats.BonusHealth = p1.BonusHealth;
            stats.BonusMana = p1.BonusMana;
            stats.BonusStrength = p1.BonusStrength;
            stats.BonusMagic = p1.BonusMagic;
            stats.BonusDefense = p1.BonusDefense;
            stats.BonusWeight = p1.BonusWeight;
        }

        private static void CycleClass(ProceduralLevelScreen level)
        {
            m_classIndex = (m_classIndex + 1) % CoopEV.P2_CLASSES.Length;
            ApplyClass(m_stats, m_classIndex);
            Partner.StopAllSpells();
            Partner.ChangeSprite("PlayerIdle_Character"); // refreshes class props (shield, beard, horns)
            Partner.UpdateEquipmentColours();
            Partner.CurrentHealth = Partner.MaxHealth;
            Partner.CurrentMana = Partner.MaxMana;

            SoundManager.PlaySound("Spell_Switch");
            level.ImpactEffectPool.SpellCastEffect(Partner.Position, 0, false);
            level.TextManager.DisplayStringText(ClassType.ToStringID(m_stats.Class, m_stats.IsFemale), Color.Cyan, new Vector2(Partner.X, Partner.Bounds.Top - 60));
        }

        #endregion

        #region Level / room lifecycle

        // ProceduralLevelScreen.OnEnter: a new life (or a fresh level) starts. P2 comes back automatically.
        public static void OnLevelEnter(ProceduralLevelScreen level)
        {
            if (IsJoined == false || Partner.AttachedLevel == level)
                return;

            Partner.AttachLevel(level);
            CopyGearFromP1(m_stats); // the new heir may wear different gear
            Partner.StopAllSpells();
            Revive(level, 1f);
        }

        // ProceduralLevelScreen.LoadPhysicsObjects: called on every room entry, right after P1 is added.
        public static void OnRoomEntered(ProceduralLevelScreen level, RoomObj room)
        {
            if (IsJoined == false || Partner.AttachedLevel != level)
                return;

            AddToPhysics(level);
            Partner.RoomTransitionReset();

            if (IsParkingRoom(room))
            {
                Park(level);
                return;
            }

            if (m_state == PartnerState.Parked)
                m_state = PartnerState.Active;

            if (m_state == PartnerState.Active)
            {
                Partner.Visible = true;
                Partner.IsCollidable = true;
                PlaceNextToP1(level);
            }
        }

        // Rooms scripted for a single player: P2 is hidden and invulnerable there.
        private static bool IsParkingRoom(RoomObj room)
        {
            return room is BonusRoomObj || room is ChallengeBossRoomObj ||
                room.Name == "Throne" || room.Name == "Ending" || room.Name == "Tutorial";
        }

        private static void Park(ProceduralLevelScreen level)
        {
            if (m_state == PartnerState.Down)
                Partner.Reset();
            m_state = PartnerState.Parked;
            Partner.StopAllSpells();
            Partner.Visible = false;
            Partner.IsCollidable = false;
            Partner.Position = level.Player.Position;
            RetargetEnemies(level, true);
        }

        private static void Revive(ProceduralLevelScreen level, float hpFraction)
        {
            Partner.Reset(); // clears IsKilled, restores collisions, HP = max
            Partner.ChangeSprite("PlayerIdle_Character");
            Partner.UpdateEquipmentColours();
            Partner.Scale = new Vector2(2, 2); // P2 has no size traits
            Partner.UpdateInternalScale();
            Partner.CurrentHealth = Math.Max(1, (int)(Partner.MaxHealth * hpFraction));
            Partner.CurrentMana = Partner.MaxMana;
            Partner.Opacity = 1;
            Partner.Visible = true;
            m_state = PartnerState.Active;
            m_offscreenTimer = 0;
            PlaceNextToP1(level);
        }

        private static void PlaceNextToP1(ProceduralLevelScreen level)
        {
            PlayerObj p1 = level.Player;
            Partner.Position = p1.Position;
            Partner.Flip = p1.Flip;
            Partner.AccelerationX = 0;
            Partner.AccelerationY = 0;
            Partner.CurrentSpeed = 0;
        }

        // PlayerObj.Kill() for P2: knocked out, comes back next to P1 after a delay. Never a game over.
        public static void OnPartnerKilled(PlayerObj partner)
        {
            if (partner != Partner)
                return;
            m_state = PartnerState.Down;
            m_downTimer = CoopEV.REVIVE_DELAY;
            Partner.StopAllSpells();
            Partner.Opacity = CoopEV.GHOST_OPACITY;
            if (m_stats.IsFemale)
                SoundManager.PlaySound("Player_Female_Death_01", "Player_Female_Death_02");
            else
                SoundManager.PlaySound("Player_Male_Death_01", "Player_Male_Death_02", "Player_Male_Death_03");
            RetargetEnemies(partner.AttachedLevel, true);
        }

        #endregion

        #region Per-frame update

        // Called from ProceduralLevelScreen.Update (unpaused), right after P1's update.
        public static void Update(ProceduralLevelScreen level, GameTime gameTime)
        {
            if (IsJoined == false || Partner.AttachedLevel != level || level.Player == null)
                return;

            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
            PlayerObj p1 = level.Player;

            if (p1.IsKilled == true) // P1's death sequence / game over: freeze P2.
                return;

            switch (m_state)
            {
                case PartnerState.Parked:
                    Partner.Position = p1.Position;
                    break;

                case PartnerState.Down:
                    // Ghost drifts behind P1.
                    Vector2 target = p1.Position + new Vector2(p1.Flip == Microsoft.Xna.Framework.Graphics.SpriteEffects.None ? -80 : 80, -40);
                    Partner.Position = Vector2.Lerp(Partner.Position, target, Math.Min(1, elapsed * 4));
                    m_downTimer -= elapsed;
                    if (m_downTimer <= 0)
                    {
                        Revive(level, CoopEV.REVIVE_HP);
                        AddToPhysics(level);
                        SoundManager.PlaySound("mfqt_teleport_in");
                        level.ImpactEffectPool.SpellCastEffect(Partner.Position, 0, false);
                    }
                    break;

                case PartnerState.Active:
                    Partner.Update(gameTime);
                    break;
            }

            m_retargetTimer -= elapsed;
            if (m_retargetTimer <= 0)
            {
                m_retargetTimer = CoopEV.RETARGET_INTERVAL;
                RetargetEnemies(level, false);
            }

            UpdateHUD();
        }

        // Points every enemy of the room at the nearest living player (or back at P1 when forceP1).
        private static void RetargetEnemies(ProceduralLevelScreen level, bool forceP1)
        {
            if (level == null || level.CurrentRoom == null || level.Player == null)
                return;

            foreach (EnemyObj enemy in level.CurrentRoom.EnemyList)
                enemy.SetPlayerTarget(PickTarget(level.Player, enemy, forceP1));
            foreach (EnemyObj enemy in level.CurrentRoom.TempEnemyList)
                enemy.SetPlayerTarget(PickTarget(level.Player, enemy, forceP1));
        }

        private static PlayerObj PickTarget(PlayerObj p1, EnemyObj enemy, bool forceP1)
        {
            if (forceP1 || PartnerActive == false)
                return p1;
            float d1 = Vector2.DistanceSquared(p1.Position, enemy.Position);
            float d2 = Vector2.DistanceSquared(Partner.Position, enemy.Position);
            return d2 < d1 ? Partner : p1;
        }

        #endregion

        #region Camera (Metal Slug style)

        // Point the camera should centre on: midpoint of both players, or P1 only when P2 is out of play
        // or too far vertically (P1 must always stay on screen: room transitions are driven by P1).
        public static Vector2 GetCameraFocus(ProceduralLevelScreen level)
        {
            PlayerObj p1 = level.Player;
            if (PartnerActive == false || Partner.AttachedLevel != level)
                return p1.Position;

            Camera2D camera = level.ScreenManager.Camera;
            if (Math.Abs(Partner.Y - p1.Y) > camera.Height * CoopEV.VERTICAL_SPLIT_LIMIT)
                return p1.Position;

            return (p1.Position + Partner.Position) / 2f;
        }

        // Called at the end of UpdateCamera(): keeps both players inside the camera horizontally, except on a side where
        // the camera already touches the room edge (so doors still work). P2 lost off-screen is teleported to P1.
        public static void ConstrainToCamera(ProceduralLevelScreen level, float elapsed)
        {
            if (PartnerActive == false || Partner.AttachedLevel != level || level.CurrentRoom == null || level.Player.IsKilled)
                return;

            Camera2D camera = level.ScreenManager.Camera;
            Rectangle cam = camera.Bounds;
            Rectangle room = level.CurrentRoom.Bounds;
            float minX = cam.Left > room.Left ? cam.Left + CoopEV.SCREEN_MARGIN : float.MinValue;
            float maxX = cam.Right < room.Right ? cam.Right - CoopEV.SCREEN_MARGIN : float.MaxValue;

            ClampX(level.Player, minX, maxX);
            ClampX(Partner, minX, maxX);

            Rectangle visible = cam;
            visible.Inflate(40, 40);
            if (visible.Contains((int)Partner.X, (int)Partner.Y))
                m_offscreenTimer = 0;
            else
            {
                m_offscreenTimer += elapsed;
                if (m_offscreenTimer >= CoopEV.OFFSCREEN_TELEPORT_DELAY)
                {
                    m_offscreenTimer = 0;
                    PlaceNextToP1(level);
                    SoundManager.PlaySound("mfqt_teleport_in");
                    level.ImpactEffectPool.SpellCastEffect(Partner.Position, 0, false);
                }
            }
        }

        private static void ClampX(PlayerObj player, float minX, float maxX)
        {
            if (player.X < minX)
            {
                player.X = minX;
                if (player.AccelerationX < 0) player.AccelerationX = 0;
            }
            else if (player.X > maxX)
            {
                player.X = maxX;
                if (player.AccelerationX > 0) player.AccelerationX = 0;
            }
        }

        #endregion

        #region Drawing

        // World space, right after P1 is drawn.
        public static void Draw(ProceduralLevelScreen level, Camera2D camera)
        {
            if (IsJoined == false || Partner.AttachedLevel != level || m_state == PartnerState.Parked)
                return;

            Partner.OutlineColour = CoopEV.P2_OUTLINE;
            if (m_state == PartnerState.Down)
            {
                // Base Kill() hides the character; draw it as a ghost anyway.
                Partner.Visible = true;
                Partner.Opacity = CoopEV.GHOST_OPACITY;
            }
            Partner.Draw(camera);

            m_tag.Position = new Vector2(Partner.X, Partner.Bounds.Top - 35);
            m_tag.Text = m_state == PartnerState.Down ? "P2  " + (int)Math.Ceiling(m_downTimer) : "P2";
            m_tag.Draw(camera);
        }

        private static void CreateHUD()
        {
            m_hud = new PlayerHUDObj();
            m_hud.ShowBarsOnly = true;
            m_hud.SetPosition(CoopEV.P2_HUD_POSITION);

            m_hudSpellIcon = new SpriteObj("Blank_Sprite");
            m_hudSpellIcon.ForceDraw = true;
            m_hudSpellIcon.OutlineWidth = 1;
            m_hudSpellIcon.Position = CoopEV.P2_HUD_POSITION + new Vector2(-30, 55);

            m_hudLabel = new TextObj(Game.JunicodeFont);
            m_hudLabel.FontSize = 12;
            m_hudLabel.OutlineWidth = 2;
            m_hudLabel.ForceDraw = true;
            m_hudLabel.TextureColor = CoopEV.P2_OUTLINE;
            m_hudLabel.Position = CoopEV.P2_HUD_POSITION + new Vector2(0, -15);

            m_tag = new TextObj(Game.JunicodeFont);
            m_tag.FontSize = 12;
            m_tag.Align = Types.TextAlign.Centre;
            m_tag.OutlineWidth = 2;
            m_tag.TextureColor = CoopEV.P2_OUTLINE;
        }

        private static void UpdateHUD()
        {
            if (m_hud == null)
                return;
            m_hud.Update(Partner);
            m_hudSpellIcon.ChangeSprite(SpellType.Icon(m_stats.Spell));
            m_hudLabel.Text = "P2  " + LocaleBuilder.getResourceStringCustomFemale(ClassType.ToStringID(m_stats.Class, m_stats.IsFemale), m_stats.IsFemale);
        }

        // Screen space, right after P1's HUD.
        public static void DrawHUD(ProceduralLevelScreen level, Camera2D camera)
        {
            if (IsJoined == false || m_hud == null || Partner.AttachedLevel != level)
                return;
            m_hud.Draw(camera);
            m_hudSpellIcon.Draw(camera);
            m_hudLabel.Draw(camera);
        }

        #endregion
    }
}
