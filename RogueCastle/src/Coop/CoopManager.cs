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
        private static float m_selectPressStart = -1; // Game.TotalGameTime when SELECT went down, -1 = released
        private static float m_retargetTimer;

        private static PlayerHUDObj m_hud;
        private static SpriteObj m_hudSpellIcon;
        private static TextObj m_hudLabel;
        private static TextObj m_tag;

        // Called once at startup (Game.LoadContent). Enemy AI is built at init with P1 baked into its logic actions
        // (Move/Chase/Teleport/FireProjectile); this makes those actions use the enemy's *current* target instead,
        // which RetargetEnemies() keeps on the nearest player. In solo the current target is always P1: no change.
        public static void Initialize()
        {
            LogicAction.TargetResolver = ResolveLogicTarget;
        }

        private static GameObj ResolveLogicTarget(GameObj owner, GameObj storedTarget)
        {
            EnemyObj enemy = owner as EnemyObj;
            if (enemy != null && storedTarget is PlayerObj && enemy.PlayerTarget != null)
                return enemy.PlayerTarget;
            return storedTarget;
        }

        // P1 locks its own controls while dashing; that is not a cutscene and must not freeze or drag P2.
        private static bool IsInScriptedSequence(PlayerObj p1)
        {
            return p1.ControlsLocked && p1.State != PlayerObj.STATE_DASHING;
        }

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

            bool consumed = false;

            // SELECT: short press = next class (camp only), long hold = leave.
            // Timed with real game time: the game runs with a variable time step (IsFixedTimeStep = false),
            // so counting frames would make the hold far shorter on high refresh-rate screens.
            if (InputManager.Pressed(Buttons.Back, m_padIndex))
            {
                if (m_selectPressStart < 0)
                    m_selectPressStart = Game.TotalGameTime;
                if (Game.TotalGameTime - m_selectPressStart >= CoopEV.LEAVE_HOLD)
                {
                    m_selectPressStart = -1;
                    Leave(level);
                    return true;
                }
            }
            else if (m_selectPressStart >= 0)
            {
                float heldFor = Game.TotalGameTime - m_selectPressStart;
                m_selectPressStart = -1;
                if (heldFor < CoopEV.CLASS_SWITCH_MAX_PRESS && level.CurrentRoom.Name == "Start" && PartnerActive)
                    CycleClass(level);
            }

            // START on P2's pad opens the shared pause screen (P1's GlobalInput never reads this pad: it is either
            // another index, or pad 1 with LockGamepadInput set).
            if (InputManager.JustPressed(Buttons.Start, m_padIndex) && level.CurrentRoom.Name != "Ending")
            {
                (level.ScreenManager as RCScreenManager).DisplayScreen(ScreenType.Pause, true);
                consumed = true;
            }

            // P2 is frozen whenever P1 is (cutscenes, boss intros, menus).
            if (PartnerActive && IsInScriptedSequence(level.Player) == false && level.Player.IsKilled == false)
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
            m_selectPressStart = -1;
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
            byte baseClass = CoopEV.P2_CLASSES[classIndex];
            stats.Class = (CoopEV.P2_ALWAYS_CLASS_POWERS || ClassType.Upgraded(baseClass)) ? UpgradedClass(baseClass) : baseClass;
            stats.Spell = CoopEV.P2_START_SPELLS[classIndex];
            stats.Traits = new Vector2(TraitType.None, TraitType.None);
            if (stats.Class == ClassType.Wizard2)
            {
                stats.WizardSpellList = CoopEV.P2_ARCHMAGE_SPELLS;
                stats.Spell = (byte)CoopEV.P2_ARCHMAGE_SPELLS.X;
            }
        }

        // Base class -> its upgraded version, which carries the class power on the block button.
        private static byte UpgradedClass(byte baseClass)
        {
            switch (baseClass)
            {
                case ClassType.Knight: return ClassType.Knight2;
                case ClassType.Wizard: return ClassType.Wizard2;
                case ClassType.Barbarian: return ClassType.Barbarian2;
                case ClassType.Assassin: return ClassType.Assassin2;
                case ClassType.Ninja: return ClassType.Ninja2;
                case ClassType.Banker: return ClassType.Banker2;
                case ClassType.SpellSword: return ClassType.SpellSword2;
                case ClassType.Lich: return ClassType.Lich2;
            }
            return baseClass;
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
            ApplyClass(m_stats, m_classIndex); // a class upgrade may have been bought in the manor since last life
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

        // ProceduralLevelScreen.CheckForRoomTransition, right after the room's OnEnter(): some rooms reposition P1 there
        // (e.g. the castle entrance moves P1 past the gate), so P2 is placed on P1 again.
        public static void AfterRoomOnEnter(ProceduralLevelScreen level)
        {
            if (IsJoined == false || Partner.AttachedLevel != level || m_state != PartnerState.Active)
                return;
            PlaceNextToP1(level);
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
                    // While P1 is in a scripted sequence (auto-walk through the castle gate, boss intro, teleport...),
                    // P2 sticks to P1 so scripted movement can't leave it behind a closing gate or in another room.
                    if (IsInScriptedSequence(p1))
                        PlaceNextToP1(level);
                    else
                        Partner.Update(gameTime);
                    // Same rule as P1 in StartingRoomObj.Update(): full HP and MP while in the starting camp.
                    if (level.CurrentRoom != null && level.CurrentRoom.Name == "Start")
                    {
                        Partner.CurrentMana = Partner.MaxMana;
                        Partner.CurrentHealth = Partner.MaxHealth;
                    }
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
            if (forceP1 || PartnerActive == false || p1.IsKilled)
                return p1;
            float d1 = Vector2.Distance(p1.Position, enemy.Position);
            float d2 = Vector2.Distance(Partner.Position, enemy.Position);

            // Hysteresis: only switch when the other player is clearly closer, so enemies don't flicker
            // between two players standing at about the same distance.
            PlayerObj current = enemy.PlayerTarget;
            if (current == p1 && d2 < d1 * CoopEV.RETARGET_SWITCH_RATIO)
                return Partner;
            if (current == Partner && d1 < d2 * CoopEV.RETARGET_SWITCH_RATIO)
                return p1;
            if (current != p1 && current != Partner)
                return d2 < d1 ? Partner : p1;
            return current;
        }

        #endregion

        #region Camera (Metal Slug style)

        // Point the camera centres on: always the midpoint of both players (no snapping to P1), only nudged so that P1
        // stays on screen in the rare case nothing could stop them separating (a fall). Room transitions are driven by P1.
        public static Vector2 GetCameraFocus(ProceduralLevelScreen level)
        {
            PlayerObj p1 = level.Player;
            if (PartnerActive == false || Partner.AttachedLevel != level)
                return p1.Position;

            Camera2D camera = level.ScreenManager.Camera;
            Vector2 focus = (p1.Position + Partner.Position) / 2f;
            float halfW = camera.Width / 2f - CoopEV.SCREEN_MARGIN;
            float halfH = camera.Height / 2f - CoopEV.SCREEN_MARGIN_TOP;
            focus.X = MathHelper.Clamp(focus.X, p1.X - halfW - GlobalEV.Camera_XOffset, p1.X + halfW - GlobalEV.Camera_XOffset);
            focus.Y = MathHelper.Clamp(focus.Y, p1.Y - halfH - GlobalEV.Camera_YOffset, p1.Y + halfH - GlobalEV.Camera_YOffset);
            return focus;
        }

        // Called at the end of UpdateCamera(). Arcade-style invisible walls: the left, right and top screen edges block
        // BOTH players, so the one in front simply can't go further while the other lags behind (no teleport).
        // A side where the camera already touches the room edge is left open so doors still work.
        // There is no bottom wall (it would make a player stand in mid-air): a player who falls off-screen is
        // brought back next to P1 after OFFSCREEN_TELEPORT_DELAY.
        public static void ConstrainToCamera(ProceduralLevelScreen level, float elapsed)
        {
            if (PartnerActive == false || Partner.AttachedLevel != level || level.CurrentRoom == null || level.Player.IsKilled)
                return;

            Camera2D camera = level.ScreenManager.Camera;
            Rectangle cam = camera.Bounds;
            Rectangle room = level.CurrentRoom.Bounds;
            float minX = cam.Left > room.Left ? cam.Left + CoopEV.SCREEN_MARGIN : float.MinValue;
            float maxX = cam.Right < room.Right ? cam.Right - CoopEV.SCREEN_MARGIN : float.MaxValue;
            float minY = cam.Top > room.Top ? cam.Top + CoopEV.SCREEN_MARGIN_TOP : float.MinValue;

            ClampToWalls(level.Player, minX, maxX, minY);
            ClampToWalls(Partner, minX, maxX, minY);

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

        // Invisible walls: push the player back inside and cancel the velocity going through the wall.
        private static void ClampToWalls(PlayerObj player, float minX, float maxX, float minY)
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

            if (player.Y < minY) // ceiling: a jump/flight can't take a player above the screen
            {
                player.Y = minY;
                if (player.AccelerationY < 0) player.AccelerationY = 0;
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
            // In the camp, remind P2 how to switch class.
            bool inCamp = Partner.AttachedLevel != null && Partner.AttachedLevel.CurrentRoom != null && Partner.AttachedLevel.CurrentRoom.Name == "Start";
            if (inCamp)
                m_hudLabel.Text += "\n" + LocaleBuilder.getResourceString("LOC_ID_COOP_CLASS_HINT");
            m_hudLabel.Position = CoopEV.P2_HUD_POSITION + new Vector2(0, inCamp ? -40 : -15); // keep the 2-line label above the bars
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
