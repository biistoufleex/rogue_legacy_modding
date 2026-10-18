# Rogue Legacy 1 — Mod « Guns & Co-op » (non officiel)

> ⚠️ **Version modifiée, non officielle.** Ce dépôt est un mod amateur de **Rogue Legacy 1**, basé sur le
> [code source officiel publié par Cellar Door Games](https://github.com/flibitijibibo/RogueLegacy1).
> Il n'est ni produit, ni approuvé, ni supporté par Cellar Door Games.
>
> **Rogue Legacy est © Cellar Door Games 2013-2024.** Le code d'origine est distribué sous la
> [Rogue Legacy Source Code License v1.0](LICENSE.md) (usage **non commercial** uniquement — voir aussi
> [EXCEPTIONS.md](EXCEPTIONS.md)). Ce mod respecte cette licence : il est gratuit, sans publicité, et
> **n'inclut aucun fichier du jeu** (graphismes, musiques, sons). Pour y jouer, il faut **posséder le jeu**
> (par exemple sur [Steam](https://store.steampowered.com/app/241600/Rogue_Legacy/)).

Ajouts du mod :

1. **Trois armes à feu** façon *Neon Abyss* (Pistol, Shotgun, SMG), intégrées au système de sorts.
2. **Coop local à 2 joueurs** « drop-in » façon arcade : une 2ᵉ manette appuie sur START pour rejoindre.

---

## Sommaire

- [Fonctionnalités](#fonctionnalités)
  - [Armes à feu](#armes-à-feu)
  - [Coop local 2 joueurs](#coop-local-2-joueurs)
  - [Jouer en ligne (Parsec)](#jouer-en-ligne-parsec)
- [Installation et compilation](#installation-et-compilation)
- [Sauvegardes](#sauvegardes)
- [Équilibrage et réglages](#équilibrage-et-réglages)
- [Branches](#branches)
- [Crédits et sources](#crédits-et-sources)
- [Licence](#licence)

---

## Fonctionnalités

### Armes à feu

Trois nouveaux « sorts » (IDs 16, 17, 18), utilisant la mana et les dégâts magiques comme les sorts d'origine.
Ils sont disponibles pour le **Chevalier** et le **Mage** (salle d'échange de sort, héritiers).

| Arme | Comportement | Mana | Dégâts (× dégâts magiques) |
|---|---|---|---|
| **Pistol** | 1 balle rapide, droite, sans gravité | 5 | 1.0 |
| **Shotgun** | 5 plombs en éventail (−20° à +20°), vitesse aléatoire, courte portée | 15 | 0.5 par plomb |
| **SMG** | tir automatique tant que la touche de sort est maintenue, légère dispersion | 3 par balle | 0.35 |

- **Debug** : touche **F9** (joueur 1) pour faire tourner le sort courant Pistol → Shotgun → SMG
  (désactivable via `GUNS_DEBUG` dans `SpellEV.cs`).
- Noms et descriptions localisés en **anglais** et **français** (les autres langues retombent sur l'anglais).

### Coop local 2 joueurs

| Action (manette du joueur 2) | Bouton |
|---|---|
| **Rejoindre** la partie (n'importe où, P2 apparaît sur P1) | **START** |
| Rejoindre avec **une seule manette** (P1 au clavier) | **SELECT + START** sur la manette 1 |
| **Changer de classe** (camp de départ uniquement) | **appui court sur SELECT** (View / Share / −) |
| **Quitter** la partie | **maintenir SELECT 3 s** (ou débrancher la manette) |
| Pause | START |
| Jouer | même disposition que P1 : A saut, X attaque, Y capacité, B sort, gâchettes = dash |

Règles :

- **Classes de P2** : Chevalier, Mage ou Barbare. Si l'amélioration de la classe est **débloquée dans le manoir**,
  P2 joue la version améliorée avec son pouvoir sur **Y** (Paladin : Bas + Y statue ; Archimage : Y change de sort ;
  Roi barbare : Y cri). Revérifié à chaque nouvelle vie.
- **Sorts de P2** : sorts d'origine uniquement (Dague / Croix / Hache) — les armes à feu du mod sont réservées à P1.
- **Stats de P2** : améliorations du manoir partagées, copie de l'équipement et des runes de P1, aucun trait génétique.
  Vie et mana au maximum dans le camp de départ, comme P1.
- **Caméra façon arcade (Metal Slug)** : centrée entre les deux joueurs ; les bords gauche, droit et haut de l'écran
  sont des **murs invisibles** pour les deux joueurs (ouverts du côté d'une porte). Un joueur qui tombe hors de
  l'écran réapparaît à côté de P1 après 2 s.
- **Salles** : P1 fait passer les portes, P2 suit. Dans les salles scriptées pour un joueur (bonus, carnaval,
  défis de boss, fin…), P2 est mis de côté (invisible, invulnérable).
- **Ennemis** : visent le joueur le plus proche ; coups critiques, mana, vampirisme et bonus de kill crédités au
  joueur qui a frappé.
- **Mort** : P2 est K.O. 8 s puis revient à 50 % de vie. Si P1 meurt : écran de lignée normal, P2 revient
  automatiquement à la vie suivante.
- **Réservé à P1** : coffres, téléporteurs, portes de boss, carte, PNJ, or (pot commun), sauvegarde.
- P2 n'est **jamais sauvegardé** (session uniquement).

Détails techniques et décisions de conception : [docs/COOP_PLAN.md](docs/COOP_PLAN.md).

### Jouer en ligne (Parsec)

Il n'y a pas de netcode intégré. Le plus simple pour jouer à distance : l'hôte lance le jeu et partage son écran
avec [Parsec](https://parsec.app/) ; Parsec émule la manette de l'invité comme une manette Xbox sur le PC de
l'hôte, qui devient la **manette 2** → l'invité appuie sur START et joue P2.

---

## Installation et compilation

Testé sous **Windows 11** avec le **.NET SDK 10**. Le jeu cible .NET Framework 4.0 ; le SDK fournit
automatiquement les assemblies de référence.

1. **Prérequis** : [git](https://git-scm.com/), [.NET SDK](https://dotnet.microsoft.com/download) (8 ou plus récent),
   et une copie de **Rogue Legacy** (Steam).
2. **Cloner FNA et ce dépôt côte à côte** (le projet attend `../FNA`) :
   ```bash
   git clone --recursive https://github.com/FNA-XNA/FNA.git
   git clone https://github.com/biistoufleex/rogue_legacy_modding.git RogueLegacy1
   ```
3. **Bibliothèques natives FNA** : télécharger l'artefact `fnalibs` du dernier build CI de
   [FNA-XNA/fnalibs-dailies](https://github.com/FNA-XNA/fnalibs-dailies/actions) (compte GitHub requis)
   et le décompresser dans `fnalibs/` à côté des deux dépôts :
   ```
   RogueLegacyMod/
   ├── FNA/
   ├── fnalibs/x64/   (SDL3.dll, FNA3D.dll, FAudio.dll, libtheorafile.dll)
   └── RogueLegacy1/  (ce dépôt)
   ```
4. **Compiler, copier et lancer** avec le script fourni (depuis `RogueLegacy1/`) :
   ```bash
   powershell -ExecutionPolicy Bypass -File run.ps1
   ```
   Le script compile `RogueLegacy.sln`, copie le dossier `Content` de l'installation Steam
   (`C:\Program Files (x86)\Steam\steamapps\common\Rogue Legacy\Content`, jamais modifié) et les DLL `fnalibs`
   dans `RogueCastle/bin/x64/Debug/net40/`, puis lance le jeu.
   Options : `-NoBuild`, `-NoLaunch`, `-KillRunning` (ferme une instance en cours), `-SteamDir <chemin>`.

Le build moddé tourne **sans Steam** (`steam_api.dll` n'est pas copiée) : aucun succès Steam ne peut être débloqué.

### Créer une archive distribuable (Nexus Mods…)

```bash
powershell -ExecutionPolicy Bypass -File tools\make-release.ps1 -Version 0.1.0
```

Compile en Release et produit `../release/RogueLegacy_GunsCoop_v<version>.zip` : exe et DLL du mod, FNA et fnalibs,
traductions, sprites CC0, `install.ps1` (copie le `Content` depuis le jeu Steam du joueur), licences et crédits.
Le script **refuse de créer l'archive** si elle contient un fichier du jeu (comparaison par hash avec
l'installation Steam, `steam_api`, formats audio/`.xnb`). Texte et réglages de la page Nexus :
[docs/NEXUS_PAGE.md](docs/NEXUS_PAGE.md) — **Donation Points et dons désactivés** (licence non commerciale).

---

## Sauvegardes

Le mod utilise un **conteneur de sauvegarde séparé** (`RogueLegacyGunsStorageContainer`) dans
`Documents\SavedGames\RogueLegacy\`. Au premier lancement, `run.ps1` y copie les sauvegardes du jeu d'origine.
Les sauvegardes Steam (et Steam Cloud) ne sont **jamais modifiées** — une sauvegarde contenant une arme du mod
ne peut donc pas atteindre le jeu officiel.

---

## Équilibrage et réglages

| Fichier | Contenu |
|---|---|
| [`RogueCastle/src/EVs/SpellEV.cs`](RogueCastle/src/EVs/SpellEV.cs) | Armes : `PISTOL_*`, `SHOTGUN_*`, `SMG_*` (coût, dégâts, portée, vitesse, dispersion, cadence), `GUNS_DEBUG` |
| [`RogueCastle/src/Coop/CoopEV.cs`](RogueCastle/src/Coop/CoopEV.cs) | Coop : classes et sorts de P2, pouvoirs de classe, marges des murs d'écran, délais (K.O., quitter, chute), ciblage des ennemis, couleur et position du HUD de P2 |

Sprites des armes : regénérer la planche avec
[`tools/build-gun-sprites.ps1`](tools/build-gun-sprites.ps1) (packs à décompresser dans `../gun-assets`).

---

## Branches

| Branche | Contenu |
|---|---|
| `guns-prototype` | Les 3 armes à feu (+ sprites) |
| `coop` | Armes + coop local 2 joueurs |

---

## Crédits et sources

- **Rogue Legacy** — © [Cellar Door Games](https://cellardoorgames.com/) 2013-2024.
  Code source : [flibitijibibo/RogueLegacy1](https://github.com/flibitijibibo/RogueLegacy1)
  ([licence](LICENSE.md)). Ce mod est basé sur ce code ; il n'est pas l'œuvre de Cellar Door Games.
- **FNA** — [FNA-XNA/FNA](https://github.com/FNA-XNA/FNA) (Ms-PL) et bibliothèques natives
  [fnalibs-dailies](https://github.com/FNA-XNA/fnalibs-dailies) (SDL3, FNA3D, FAudio, Theorafile).
- **Sprites ajoutés par le mod** (tous en **CC0 / domaine public**, voir
  [`RogueCastle/ModContent/CREDITS.txt`](RogueCastle/ModContent/CREDITS.txt)) :
  - Icônes des armes : **« Guns »** par **SnoopethDuckDuck** — <https://snoopethduckduck.itch.io/guns>
    (Pack 2 #26 Pistol, Pack 1 #51 Shotgun, Pack 2 #9 SMG, variantes contour 2 px).
  - Balles et étincelles d'impact : **« Bullet Collection 1 (M484) »** par **Master484** —
    <https://opengameart.org/content/bullet-collection-1-m484>.
- Toutes les autres images, sons et musiques proviennent de **votre copie du jeu** et ne sont pas inclus ici.

---

## Licence

Le code d'origine reste sous la [Rogue Legacy Source Code License v1.0](LICENSE.md) de Cellar Door Games, dont
cette notice ne doit pas être retirée. Les modifications de ce dépôt suivent les mêmes conditions :
**usage non commercial uniquement**, version modifiée clairement identifiée comme telle, aucune distribution des
fichiers du jeu. Pour toute distribution sortant de ce cadre, contacter Cellar Door Games
(voir [EXCEPTIONS.md](EXCEPTIONS.md)).

L'éditeur de niveaux d'origine est disponible [ici](https://github.com/flibitijibibo/RogueCastleEditor)
(non compatible avec la dernière version du jeu).
