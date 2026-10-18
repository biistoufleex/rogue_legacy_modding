# Publier les mods sur Nexus Mods — tes étapes

Tout le travail technique est fait (archives, installeur, notices, pages, bannières, licence). Il te reste les étapes
ci-dessous, dans l'ordre. Coche au fur et à mesure.

Fichiers prêts dans `RogueLegacyMod\release\` :

| Fichier | Usage |
|---|---|
| `RogueLegacy_Weapons_v1.0.0.zip` | Fichier principal du mod Weapons |
| `RogueLegacy_Coop_v1.0.0.zip` | Fichier principal du mod Co-op |
| `banners\banner_weapons.png` / `banners\banner_coop.png` | Image principale de chaque page |

Textes à copier-coller : [NEXUS_PAGE_WEAPONS.md](NEXUS_PAGE_WEAPONS.md) et [NEXUS_PAGE_COOP.md](NEXUS_PAGE_COOP.md).

---

## 1. Test final des archives (≈ 1 h)

Teste les **archives**, pas ton dossier de développement. Idéalement sur un autre PC, sinon dans un dossier neuf
(ex. `Bureau\test-mod\`).

### Weapons
- [ ] Extraire `RogueLegacy_Weapons_v1.0.0.zip`, double-cliquer `install.bat`, lancer `RogueLegacy.exe`.
- [ ] Charger une partie, F9 : Pistol → Shotgun → SMG, chaque arme tire et tue un ennemi.
- [ ] SMG : s'arrête quand on relâche ; la mana diminue.
- [ ] Changer de salle en tirant, mourir, écran de lignée, quitter / relancer : pas de crash, l'arme est conservée.

### Co-op (2 manettes)
- [ ] Extraire `RogueLegacy_Coop_v1.0.0.zip` dans un **autre** dossier, `install.bat`, lancer.
- [ ] START sur la manette 2 : P2 apparaît sur P1 (au camp **et** en chargeant une partie déjà dans le château).
- [ ] Au camp : appui court sur SELECT → Chevalier / Mage / Barbare ; vie et mana restent pleines.
- [ ] Vérifier qu'il n'y a **aucune arme à feu** dans cette version (F9 ne fait rien).
- [ ] Traverser une dizaine de salles, une tour verticale : murs d'écran OK, pas de téléportation intempestive.
- [ ] Un boss à deux. P2 K.O. → revient après 8 s. P1 meurt → lignée → P2 revient au camp.
- [ ] Une salle bonus / coffre-jeu : P2 disparaît puis revient à la sortie.
- [ ] Maintenir SELECT 3 s : P2 quitte. Débrancher la manette 2 : P2 quitte.
- [ ] Optionnel : une partie via **Parsec** avec un ami.

En cas de crash : la fenêtre « SAVE THIS MESSAGE! » + `%APPDATA%\Rogue Legacy\CrashLog_*.log` → envoie-moi le log,
je corrige et je régénère les archives.

## 2. Captures d'écran (≈ 15 min)

3 à 5 images par mod, en jeu, en plein écran. Raccourci : **Win + Shift + S** (capture d'une zone) ou
**Win + Alt + Impr. écran** (Xbox Game Bar, enregistre dans `Vidéos\Captures`).

- **Co-op** : P1 et P2 côte à côte dans le château (les deux HUD visibles) ; un combat à deux ; P2 en fantôme K.O. ;
  le choix de classe au camp.
- **Weapons** : le Shotgun en éventail ; le SMG en rafale ; l'icône d'arme dans le HUD.
- Optionnel mais très efficace : un GIF ou une vidéo courte (Game Bar : **Win + Alt + R**).

## 3. Compte Nexus Mods (≈ 5 min)

- [ ] Créer un compte gratuit sur <https://www.nexusmods.com/> (pas besoin de Premium).
- [ ] Vérifier ton adresse e-mail.

## 4. Créer la page du mod Weapons (≈ 20 min)

Sur la page du jeu Rogue Legacy (<https://www.nexusmods.com/roguelegacy>) → **Upload a mod** (« Mods » > « Add mod »).

1. **Mod details**
   - Nom, version (`1.0.0`), catégorie (Gameplay), langue (English), résumé et description : copier depuis
     [NEXUS_PAGE_WEAPONS.md](NEXUS_PAGE_WEAPONS.md) (la description est déjà au format BBCode de Nexus).
   - Cocher **« Contains adult content » : non**.
2. **Media** : `banner_weapons.png` comme image principale, puis tes captures.
3. **Files** : téléverser `RogueLegacy_Weapons_v1.0.0.zip`, catégorie **Main files**, version `1.0.0`,
   description : « Weapons mod, Windows 64-bit. Requires Rogue Legacy (Steam). »
4. **Requirements** : indiquer « Rogue Legacy (base game, Steam) ». Pas de mod Nexus requis.
5. **Permissions** — ⚠️ indispensable (licence non commerciale de Rogue Legacy) :
   - **Donation Points : NE PAS activer** pour ce mod.
   - **Accepter les dons / lien PayPal : non.**
   - Utilisation commerciale : **interdite**.
   - Mention des crédits : Cellar Door Games, SnoopethDuckDuck, Master484 (déjà dans la description).
6. **Publier**. Nexus analyse le fichier (antivirus) : de quelques minutes à quelques heures avant qu'il soit
   téléchargeable. C'est normal.

## 5. Créer la page du mod Co-op (≈ 15 min)

Mêmes étapes avec [NEXUS_PAGE_COOP.md](NEXUS_PAGE_COOP.md), `banner_coop.png` et `RogueLegacy_Coop_v1.0.0.zip`.
Mêmes réglages de permissions (**pas de Donation Points, pas de dons**).

## 6. Après publication

- [ ] Dans chaque description, remplacer « See the companion mod … » par un lien vers l'autre page Nexus.
- [ ] Ajouter les liens Nexus dans le README GitHub (ou me les donner, je m'en occupe).
- [ ] Surveiller les onglets **Bugs** et **Posts** les premiers jours.
- [ ] Mettre à jour une version (pour plus tard) :
  1. Me décrire les changements → je corrige et j'ajoute une entrée dans `CHANGELOG.md`.
  2. Régénérer :
     `powershell -ExecutionPolicy Bypass -File tools\make-release.ps1 -Variant Coop -Version 1.0.1`
     (idem `-Variant Weapons`).
  3. Sur Nexus : **Files** → uploader la nouvelle archive, changer la version du mod, archiver l'ancien fichier.

## 7. Optionnel : prévenir Cellar Door Games

Pas obligatoire (la licence autorise les mods non commerciaux), mais c'est une marque de respect, et c'est le seul
moyen d'obtenir un accord pour les bannières ou une éventuelle intégration officielle comme « Lament of Zors ».
Formulaire : <https://cellardoorgames.com/contact/>

Modèle de message (anglais) :

> Hi Cellar Door team,
>
> Thank you for releasing the Rogue Legacy 1 source code! I used it to build two free, non-commercial mods:
> a drop-in local 2-player co-op mode and a small weapons mod. Both follow the source code license: no game assets
> are distributed (players copy the Content folder from their own copy), the builds are clearly marked as
> unofficial, and there are no donations or ads.
>
> Source: https://github.com/biistoufleex/rogue_legacy_modding
> I plan to publish them on Nexus Mods. My mod pages use your Steam capsule art with a small "COOP" / "WEAPONS"
> label — please let me know if you'd rather I didn't, and I'll replace it right away.
>
> If you'd ever be interested in shipping the co-op mode as an optional launcher on Steam (like Lament of Zors),
> I'd be happy to help.
>
> Best regards,
> Kevin

## Si Cellar Door Games demande un retrait

Dis-le-moi : je retire les bannières (dépôt + historique Git si besoin) et je te dis quoi modifier sur Nexus.
Sur Nexus, tu peux masquer une page à tout moment (« Hide mod »).
