# Plan — Coop local 2 joueurs (drop-in façon arcade)

> Branche cible : `coop` (partant de `guns-prototype`). Tout en local, rien poussé.
> Toutes les décisions de gameplay ci-dessous ont été prises pour toi ; elles sont regroupées
> au §2 et chiffrées dans une classe `CoopEV` pour pouvoir les changer sans toucher au code.

---

## 1. Ce qu'on veut obtenir

- En jeu, une **2ᵉ manette appuie sur START** → le Joueur 2 apparaît à côté du Joueur 1.
- **Seulement dans le camp de départ** (salle `"Start"` : forgeron, enchanteresse, architecte,
  Charon, mannequin). Ailleurs, START sur la manette 2 affiche « Rejoindre au camp de départ ».
- Dans le camp, P2 **choisit sa classe parmi 3** avec un bouton.
- Logique **Metal Slug** : un seul écran, caméra partagée, les deux joueurs ne peuvent pas
  sortir du cadre ; on traverse les salles ensemble.

---

## 2. Décisions de gameplay (prises, modifiables dans `CoopEV`)

| Sujet | Décision | Pourquoi |
|---|---|---|
| Rejoindre | START sur manette 2, uniquement en salle `"Start"`, contrôles débloqués | Demande explicite ; le camp est une zone sûre sans ennemis |
| Cas « P1 au clavier + 1 seule manette » | P2 rejoint avec **START + SELECT (Back) ensemble** sur la manette 1 ; la manette 1 est alors retirée à P1 | Sinon impossible de jouer à 2 avec une seule manette |
| Classes P2 | **Chevalier, Mage, Barbare** (les 3 classes de base, toujours débloquées). Bouton **SELECT** = classe suivante, uniquement dans le camp | Pas de dépendance aux déblocages du manoir ; SELECT est libre en jeu pour P2 (la carte reste à P1) |
| Sort de départ P2 | Chevalier → Pistol, Mage → SMG, Barbare → Axe | Met en valeur les armes du mod ; le Barbare n'a pas d'arme à feu |
| Stats P2 | Classe choisie + **upgrades du manoir partagés** (SkillSystem est global) + **copie de l'équipement et des runes de P1** au moment où P2 apparaît. **Aucun trait génétique** | P2 doit être viable sans sauvegarde propre ; les traits « écran » (vertige, daltonisme, clonus…) sont déjà globaux via P1 |
| Apparence P2 | Tête/torse/épaules aléatoires + **liseré de couleur (cyan)** + étiquette « P2 » au-dessus de la tête | Distinguer les 2 joueurs au premier coup d'œil |
| Or, objets, coffres, salles bonus | **Pot commun = l'or de P1**. P2 ramasse or/vie/mana pour lui-même (vie/mana) et pour le pot commun (or). Coffres, téléporteurs, PNJ, salles bonus : **interaction réservée à P1** | Toute la progression est sauvegardée sur P1 ; évite de dupliquer la logique de sauvegarde |
| Mort de P2 | P2 tombe « K.O. » 8 s (fantôme translucide qui suit P1), puis **réapparaît à côté de P1 avec 50 % PV**. Infini, comme les crédits arcade | Pas de game over pour P2, c'est P1 qui porte la lignée |
| Mort de P1 | Game over normal → écran de lignée. **P2 reste inscrit** et réapparaît automatiquement dans le camp de la vie suivante | Pas besoin de re-rejoindre à chaque mort |
| Quitter | Maintenir **SELECT 2 s** n'importe où (ou débrancher la manette 2) | Simple, pas de menu |
| Pause | START de P2 (une fois inscrit) ouvre la même pause que P1 | Comportement arcade classique |
| Caméra | Centre = **milieu des deux joueurs**, bornée à la salle (code actuel). Pas de zoom | Metal Slug, et le moteur n'a pas de zoom fiable |
| Murs d'écran | Chaque joueur est **bloqué aux bords de la caméra** (marge 40 px), sauf côté « bord de salle » pour pouvoir passer une porte | Les deux restent dans le cadre |
| Joueur à la traîne | Si P2 est hors cadre > 1 s (chute, tour verticale…), il est **téléporté à côté de P1** avec une bouffée de fumée | Filet de sécurité pour les salles verticales / pièges |
| Changement de salle | Déclenché par **P1 uniquement** (comme aujourd'hui) ; P2 est téléporté au même point d'entrée | Les transitions, la carte, la sauvegarde et le chargement de salle sont tous écrits pour P1 |
| Salles spéciales | Dans les salles **bonus, carnaval, coffres-jeux, défis de boss, Throne, Ending, Tutorial**, P2 est **« garé »** : invisible, invulnérable, ne tire pas, suit P1 | Ces salles sont scriptées pour un seul joueur |
| Boss normaux | P2 participe. Pendant les cinématiques d'intro (contrôles de P1 bloqués), P2 est aussi bloqué | Cohérence |
| Équilibrage ennemis | **Pas de bonus de PV par défaut** (`COOP_ENEMY_HP_MULT = 1.0`) ; réglable | Metal Slug ne compense pas ; on ajustera après test |
| Tir allié | Aucun (déjà le cas : les objets de même type de collision ne se touchent pas) | — |
| Sauvegarde | **P2 n'est jamais sauvegardé** (session uniquement) | Évite de toucher au format `.rcdat` ; les saves du mod sont déjà isolées |

---

## 3. Constat technique (ce que fait le code aujourd'hui)

| Zone | Fichier | Constat | Conséquence |
|---|---|---|---|
| Entrées | `InputSystem/InputMap.cs`, `InputManager.cs` | `InputMap(PlayerIndex, useGamePad)` lit **une** manette (`PlayerIndex`) + le clavier. `Game.GlobalInput` = `PlayerIndex.One`. `InputManager` lit déjà les 4 manettes chaque frame | Un 2ᵉ `InputMap(PlayerIndex.Two)` avec `LockKeyboardInput = true` suffit pour P2. Il faut ajouter un `LockGamepadInput` pour le cas « 1 seule manette » |
| Joueur | `PlayerObj.cs` (4 370 lignes) | **189** accès à `Game.PlayerStats` et **38** à `Game.GlobalInput` | Refactor central : `PlayerObj.Stats` et `PlayerObj.Input` (voir §4) |
| Stats | `PlayerStats.cs` | Mélange état **du personnage** (Class, Spell, Traits, PV, apparence, équipement) et état **de la partie** (Gold, boss battus, journal, lignée, tutoriel…) | Il faut classer chaque accès : perso → `Stats`, partie → `Game.PlayerStats` |
| Écran de niveau | `ProceduralLevelScreen.cs` | Un seul `m_player` : Update (l.1538), HandleInput (l.1664), Draw (l.2166), caméra (l.1621), transition (l.1548 → `CheckForRoomTransition` l.1190), physique (l.541), HUD (l.1542) | Ajouter un `m_player2` optionnel à chacun de ces points |
| Caméra | `UpdateCamera()` l.1621 | Suit P1 puis se borne à la salle | Remplacer la cible par le milieu des joueurs |
| Ennemis | `EnemyObj.cs` | `m_target` unique (`SetPlayerTarget`, l.244) ; la collision teste `otherBoxParent == m_target` pour décider critique / « c'est le joueur » (l.673) | Re-cibler le joueur vivant le plus proche ; remplacer le test par `is PlayerObj` |
| Physique | `DS2DEngine/src/Physics/PhysicsManager.cs` l.136 | Deux objets de même `CollisionTypeTag` ne se testent pas (sauf `SameTypesCollide`) | P1/P2 ne se bousculent pas et ne se blessent pas : rien à faire |
| Mort | `PlayerObj.Kill()` l.2483 | Met `IsDead`, appelle `AttachedLevel.RunGameOver()` | Brancher : P2 → K.O./réapparition, jamais de game over |
| HUD | `PlayerHUDObj.Update(PlayerObj)` | Prend déjà le joueur en paramètre, mais lit `Game.PlayerStats.Spell` pour l'icône (l.285, 336) | Un 2ᵉ HUD à droite, à faire lire `player.Stats` |
| Salle de départ | `StartingRoomObj.cs` | `CurrentRoom.Name == "Start"` ; PNJ = forgeron, enchanteresse, architecte, Charon | Point de spawn P2 + zone de choix de classe |

Autres usages directs de `Player` : **199 références dans 54 fichiers** (surtout IA des ennemis via
`m_target`, salles bonus, écrans). Ils restent « P1 » par défaut — c'est voulu (voir §2, salles spéciales).

---

## 4. Architecture cible

```
RogueCastle/src/Coop/
  CoopEV.cs            constantes (touches, marges, timers, classes, couleurs, multiplicateurs)
  CoopManager.cs       état de session : P2 inscrit ?, PlayerIndex, classe, Stats P2, join/leave
  CoopCamera.cs        milieu des joueurs, bornes d'écran, téléport du joueur à la traîne
  CoopTargeting.cs     ré-assignation de m_target des ennemis (joueur vivant le plus proche)
```

**`PlayerObj` devient paramétrable** (le cœur du chantier) :

```csharp
public PlayerStats Stats { get; }     // P1 : Game.PlayerStats ; P2 : CoopManager.P2Stats
public InputMap    Input { get; }     // P1 : Game.GlobalInput  ; P2 : CoopManager.P2Input
public int  PlayerNumber { get; }     // 1 ou 2
public bool IsCoopPartner => PlayerNumber == 2;
```

- Les 38 `Game.GlobalInput` de `PlayerObj` → `Input` (remplacement mécanique sûr).
- Les 189 `Game.PlayerStats` de `PlayerObj` → **classés un par un** :
  - **Perso → `Stats`** : `Class`, `Spell`, `Traits`, `CurrentHealth/Mana`, `IsFemale`, `HeadPiece/ChestPiece/ShoulderPiece`,
    `Bonus*`, `LichHealth/LichMana/LichHealthMod`, `WizardSpellList`, `GetEquippedArray/GetEquippedRuneArray`, `GodMode`.
  - **Partie → `Game.PlayerStats`** (inchangé) : `Gold`, `SpecialItem`, `TutorialComplete`, boss battus, `IsDead`,
    `NumEnemiesBeaten`, `EnemiesKilledInRun`, `LockCastle`, `HardcoreMode`, journal, lignée.
- Les helpers statiques appelés depuis `PlayerObj` qui lisent `Game.PlayerStats` (ex. `SpellType.GetNext3Spells()`,
  `ProceduralLevelScreen.UpdatePlayerSpellIcon()`, `PlayerHUDObj`) reçoivent le joueur/les stats en paramètre.
- Tout ce que P2 **ne doit pas** déclencher est gardé par `if (IsCoopPartner == false)` :
  `RunGameOver`, `UpdatePlayerSpellIcon` de P1, cartes/boussole, `DeathDefy`, dialogues PNJ, achats, objets spéciaux, F9.

Avantage : P1 garde **exactement** le comportement actuel (`Stats == Game.PlayerStats`, `Input == Game.GlobalInput`),
et on peut valider ce refactor seul avant d'ajouter P2.

---

## 5. Phases d'implémentation

Chaque phase se termine par un build sans nouveau warning, un test manuel et **un commit**.

### Phase 0 — Préparation (≈ 0,5 j)
- Branche `coop` depuis `guns-prototype`.
- `CoopEV.cs` avec toutes les constantes du §2 + `COOP_ENABLED = true` (interrupteur global).
- Overlay de debug derrière `CoopEV.COOP_DEBUG` : manettes connectées (index 1–4), boutons pressés.
- **Test** : brancher 2 manettes, vérifier qu'on voit bien la manette 2 en `PlayerIndex.Two` (FNA/SDL3 numérote dans l'ordre de connexion).

### Phase 1 — Entrées P2 + inscription (≈ 1 j)
- `InputMap` : ajouter `LockGamepadInput` (symétrique de `LockKeyboardInput`).
- `CoopManager.CreateP2Input(PlayerIndex)` : même mapping manette que P1 par défaut (A saut, X attaque, Y bloc/capacité,
  B sort, LT/RT dash, croix + stick gauche), clavier verrouillé.
- `ProceduralLevelScreen.HandleInput()` : détecter START sur manette 2 (ou START+SELECT sur manette 1 si une seule manette).
  - Salle `"Start"` → `CoopManager.Join(...)`.
  - Ailleurs → texte « Rejoindre au camp de départ » (nouvelle clé de localisation EN/FR).
- **Fin de phase** : un texte « P2 a rejoint » s'affiche ; rien n'apparaît encore.

### Phase 2 — Refactor `PlayerObj` (≈ 2–3 j, le plus risqué)
- Ajouter `Stats`, `Input`, `PlayerNumber` (constructeur : P1 par défaut → aucun appel existant ne change).
- `Game.GlobalInput` → `Input` dans `PlayerObj.cs`.
- Classer et remplacer les 189 `Game.PlayerStats` (tableau du §4). Garder une trace du classement dans le commit.
- Adapter `PlayerHUDObj` pour lire `player.Stats`.
- **Fin de phase** : **aucune différence visible en solo**. Re-jouer la checklist des armes + un run complet (mort, lignée, sauvegarde).

### Phase 3 — Apparition de P2 dans le camp (≈ 1,5 j)
- `CoopManager.BuildP2Stats(classe)` : nouveau `PlayerStats`, classe/sort de départ, copie équipement + runes de P1,
  apparence aléatoire, pas de traits, PV/mana au max.
- `ProceduralLevelScreen` : `m_player2` créé, `Initialize()`, ajouté au `PhysicsManager`, mis à jour (`Update`, `HandleInput`),
  dessiné juste après P1 (l.2166), liseré cyan + étiquette « P2 ».
- **Choix de classe** : SELECT dans le camp → classe suivante (Chevalier → Mage → Barbare), recalcul des stats, petit effet + nom de la classe.
- 2ᵉ `PlayerHUDObj` en haut à droite (PV, mana, icône de sort, classe).
- P2 réapparaît automatiquement dans le camp à chaque nouvelle vie de P1 (`CoopManager` survit au rechargement du niveau).
- **Fin de phase** : P2 apparaît, bouge, saute, frappe, lance son sort dans le camp ; change de classe.

### Phase 4 — Caméra Metal Slug + traversée des salles (≈ 1,5 j)
- `UpdateCamera()` : cible = milieu de P1/P2 (P1 seul si P2 absent, K.O. ou garé), bornage de salle inchangé.
- Après `m_physicsManager.Update()` : bloquer chaque joueur aux bords de la caméra (marge `COOP_SCREEN_MARGIN`)
  **sauf** du côté où la caméra touche le bord de la salle (pour pouvoir sortir par une porte).
- Téléport du joueur à la traîne (`COOP_OFFSCREEN_TELEPORT_DELAY = 1 s`).
- `CheckForRoomTransition()` : après `m_player.RoomTransitionReset()`, placer P2 au même point d'entrée que P1 et le réinitialiser.
- Téléporteurs / entrée du château / retour au camp : même traitement.
- **Fin de phase** : on traverse 10 salles à deux (horizontales, verticales, tour, donjon) sans perdre P2.

### Phase 5 — Combat, ennemis, mort et réapparition (≈ 1,5 j)
- `CoopTargeting` : toutes les 0,25 s, `enemy.SetPlayerTarget(joueur vivant le plus proche)`.
- `EnemyObj.CollisionResponse` : `otherBoxParent == m_target` → `otherBoxParent is PlayerObj` (critique + crédit du coup corrects pour P2).
  Vérifier les sous-classes qui surchargent ce test (`EnemyObj_*`).
- `PlayerObj.Kill()` : si `IsCoopPartner` → état K.O. (fantôme qui suit P1, `COOP_REVIVE_DELAY = 8 s`), puis réapparition à 50 % PV.
- Objets au sol (`ItemDropObj`, l.2143 de `PlayerObj`) : or → `Game.PlayerStats.Gold` ; vie/mana → le joueur qui ramasse.
- Vibrations : `GamePad.SetVibration(Input.PlayerIndex, …)` au lieu de `PlayerIndex.One`.
- **Fin de phase** : les ennemis attaquent le plus proche ; P2 tue, meurt et revient ; P1 qui meurt → lignée, P2 revient au camp.

### Phase 6 — Salles spéciales et écrans (≈ 1 j)
- Mode « garé » de P2 (`CoopManager.Park/Unpark`) dans : `BonusRoomObj` et dérivées (carnaval, coffres-jeux, échange de sort…),
  `ChallengeBossRoomObj`, `Throne`, `Ending`, `Tutorial`.
- Cinématiques de boss : quand P1 a `LockControls()`, P2 aussi.
- Écrans par-dessus le niveau (pause, carte, profil, objet obtenu, forgeron/enchanteresse) : P2 ne doit rien piloter
  (ils lisent `Game.GlobalInput` → manette 1 uniquement : à vérifier écran par écran).
- Sortie du jeu (maintenir SELECT 2 s) et manette 2 débranchée → `CoopManager.Leave()`.

### Phase 7 — Finitions (≈ 1 j)
- Lisibilité : liseré + « P2 », couleur des dégâts infligés par P2, petit marqueur au bord de l'écran si P2 est K.O.
- Sons : réutiliser les sons existants (rejoindre = `GetItem`, K.O. = son de mort, retour = `mfqt_teleport_in`).
- Équilibrage : `COOP_ENEMY_HP_MULT`, `COOP_REVIVE_DELAY`, `COOP_REVIVE_HP` après quelques runs.
- Textes EN/FR : « Appuie sur START pour rejoindre », « Rejoindre au camp de départ », noms de classe déjà localisés.

**Total estimé : 9 à 11 jours de dev**, dont la phase 2 est le gros morceau.

---

## 6. Constantes prévues (`CoopEV.cs`)

```csharp
public const bool   COOP_ENABLED = true;
public const bool   COOP_DEBUG = true;
public static readonly byte[] P2_CLASSES = { ClassType.Knight, ClassType.Wizard, ClassType.Barbarian };
public const int    COOP_SCREEN_MARGIN = 40;            // px entre un joueur et le bord de la caméra
public const float  COOP_OFFSCREEN_TELEPORT_DELAY = 1f; // s hors cadre avant téléport vers P1
public const float  COOP_REVIVE_DELAY = 8f;             // s de K.O.
public const float  COOP_REVIVE_HP = 0.5f;              // fraction des PV max au retour
public const float  COOP_LEAVE_HOLD = 2f;               // s à maintenir SELECT pour quitter
public const float  COOP_RETARGET_INTERVAL = 0.25f;     // s entre deux re-ciblages ennemis
public const float  COOP_ENEMY_HP_MULT = 1f;            // 1 = pas de compensation
public static readonly Color P2_OUTLINE = Color.Cyan;
```

---

## 7. Plan de test (à jouer à chaque fin de phase concernée)

1. **Solo intact** : sans manette 2, tout se comporte comme avant (armes, F9, mort, lignée, sauvegarde).
2. **Rejoindre** : START manette 2 dans le camp → P2 apparaît ; dans le château → message, rien n'apparaît.
3. **Une seule manette** : P1 au clavier, START+SELECT sur la manette → P2 rejoint, P1 n'est plus piloté par la manette.
4. **Classes** : SELECT fait tourner Chevalier/Mage/Barbare, les stats et le sort changent ; impossible hors du camp.
5. **Caméra** : les deux restent dans le cadre ; salle plus large que l'écran ; tour verticale ; chute de P2 → téléport.
6. **Portes** : P1 sort → P2 suit ; P2 seul ne peut pas déclencher de transition.
7. **Combat** : les ennemis visent le plus proche ; P2 tue avec épée, sort et armes ; critiques de P2.
8. **Mort P2** : K.O. → retour après 8 s ; **mort P1** : lignée → nouvelle vie, P2 de retour au camp sans rejoindre.
9. **Salles spéciales** : coffre-jeu, carnaval, échange de sort, boss + cinématique, défi de boss : pas de crash, P2 garé.
10. **Quitter** : maintien SELECT, débranchement de la manette 2 → P2 disparaît proprement.
11. **Sauvegarde** : quitter/recharger à deux → rien de P2 dans les `.rcdat`, P1 intact.

---

## 8. Risques et parades

| Risque | Parade |
|---|---|
| Un `Game.PlayerStats` mal classé dans `PlayerObj` (P2 modifie l'état de P1 ou l'inverse) | Classement explicite dans le commit de la phase 2, revue ciblée, test solo complet avant d'ajouter P2 |
| Code hors `PlayerObj` qui suppose `Player` = celui qui agit (IA, salles, objets) | Par défaut tout reste lié à P1 ; P2 garé dans les salles scriptées ; on traite au cas par cas ce qui crashe |
| Ordre des manettes FNA/SDL3 (la « manette 2 » n'est pas forcément l'index 2) | Overlay de debug phase 0 ; P2 peut rejoindre depuis n'importe quel index libre (2–4) |
| Caméra et salles verticales / pièges qui « avalent » P2 | Téléport automatique vers P1 après 1 s hors cadre |
| Performance (2 joueurs = 2× projectiles, 2× effets) | Négligeable à cette échelle ; à surveiller avec le SMG à deux |
| Écrans superposés qui écoutent toutes les manettes | `Game.GlobalInput` est déjà limité à `PlayerIndex.One` ; vérifier les appels `InputManager.JustPressed(…, null)` |

---

## 9. Hors périmètre (pour plus tard)

- 3–4 joueurs, coop en ligne, écran partagé.
- Sauvegarde de P2 / lignée propre à P2.
- P2 qui interagit avec les PNJ, les coffres ou l'architecte.
- Zoom dynamique de la caméra.
