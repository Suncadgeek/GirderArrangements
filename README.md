# GirderArrangements

Add-in NX 2406 / Teamcenter (SOLEIL II) qui crée **un arrangement d'assemblage par poutre** au sein
d'un arc, de sorte qu'activer l'arrangement « POUTRE NN » n'affiche **que cette poutre et l'équipement
posé dessus** (aimants + chambre à vide), tout le reste étant *supprimé dans l'arrangement*.

## Le besoin

Sous un arc, la décomposition topologique est :

```
ARCxx (assemblage)
├── *_SQL                squelette
├── *_AIMANTS            ensemble aimants (contient les aimants individuels)
├── *POUTRE*_01          poutre 1   ┐
├── *POUTRE*_02          poutre 2   ├─ 3 ou 4 poutres selon la longueur de l'arc
├── *POUTRE*_03          poutre 3   ┘
└── *_CAV_EQUIPEE        ensemble chambre à vide (contient les éléments cav individuels)
```

Les aimants et les cav se répartissent **physiquement le long de l'arc** sur les différentes poutres,
mais structurellement ils vivent dans les ensembles `_AIMANTS` / `_CAV_EQUIPEE`. Nativement on ne peut
donc pas visualiser « une seule poutre avec son équipement ». L'add-in résout ça **sans toucher à la
structure**, uniquement avec des arrangements + suppression par arrangement.

## Deux modes

- **Mono-arc** : charger un arc par sa réf TC (ou utiliser l'arc déjà ouvert), choisir les poutres.
- **Anneau** : saisir la réf TC de l'anneau → « Lister les cellules ». L'anneau est ouvert en
  **structure seule** (`ComponentsToLoad=None`, lecture sur **2 niveaux** via `OpenComponents`
  `ComponentOnly` + `UF_ASSEM_ask_component_data`) : on récupère les réfs TC des arcs **sans charger
  la maquette** (énorme). On sélectionne les **cellules** voulues, puis chaque arc est ouvert
  individuellement (géométrie pleine) et **laissé ouvert** — l'enregistrement Teamcenter est **manuel**.

## Fonctionnement (par arc)

1. **Charger l'arc** : par réf TC (mode managé) ou en utilisant l'arc déjà ouvert.
2. **Détection** : poutres (`*POUTRE*_NN`), ensembles aimants/cav, squelette.
3. Pour chaque poutre :
   - repère **local** de la poutre (`Component.GetPosition`) ;
   - **boîte de sélection** = emprise de la poutre (AABB projetée sur les axes locaux) **élargie de
     700 mm de chaque côté en X et Y** (réglable), Z borné à l'emprise de la poutre (marge Z réglable,
     0 par défaut) ;
   - les **items** (aimants/cav) dont le centre approx tombe dans la boîte sont rattachés à la poutre
     (en cas de recouvrement, à la poutre dont la **CSYS de montage** est la plus proche).
4. **Arrangement** « POUTRE NN » (dérivé du numéro de la poutre) créé s'il n'existe pas, sinon mis à
   jour ; on y **supprime** les autres poutres, les items non rattachés et le squelette (option).

Le nom d'arrangement est dérivé de la poutre : `..._01` → **POUTRE 01**.

## Enregistrement

Les arcs ne sont **jamais fermés** ni enregistrés automatiquement. Le bouton **« Enregistrer les arcs… »**
ouvre une fenêtre listant les arcs modifiés (cases à cocher). « Enregistrer » demande confirmation puis
écrit **uniquement les arcs cochés**, en **pièce de travail uniquement** (`Part.Save(SaveComponents.False)`)
— ni les autres arcs, ni les sous-produits (poutres / aimants / cav).

## Architecture (patron CheckDistances : 4 couches + launcher hot-reload)

```
src/GirderArrangements.Launcher  → DLL chargée par NX (« GirderArrangements.dll »), hot-reload par octets
src/GirderArrangements.Core      → logique pure testable (xUnit) : NamingService, géométrie
                                  (Vec3/Triad/SelectionBox/Aabb), BeamPartitioner, Config/ConfigStore
src/GirderArrangements.Nx        → adaptateur NXOpen : NxContext, NxEnvironment, NxArcOpener (plein /
                                  structure seule), NxRingNavigator (cellules/arcs sans géométrie),
                                  NxArcParser, NxGeometryReader, NxArrangementService
src/GirderArrangements (App)     → WinForms (MainForm redimensionnable + journal) + ArrangementGenerator
tests/GirderArrangements.Core.Tests → NamingService, géométrie/boîte, répartition
```

API NXOpen clé (arrangements) :
- `ComponentAssembly.Arrangements.Create(template, name)`
- `ComponentAssembly.SuppressComponents(comps, arrangements[])` / `UnsuppressComponents(...)`
- `ComponentAssembly.ActiveArrangement`

## Build & déploiement

```bash
dotnet build GirderArrangementsCS.sln -c Release      # sortie : src/GirderArrangements/bin/Release/net48
dotnet test  GirderArrangementsCS.sln                 # tests Core
```

NXOpen est résolu depuis l'install de l'utilisateur via `$(NxManagedDir)` (défaut
`C:\Apps\Siemens\NX2406\NXBIN\managed`, surchargeable par `NX_MANAGED_DIR`) — cf. `Directory.Build.props`.

**Charger dans NX** : `Fichier ▸ Exécuter ▸ NX Open…` → pointer sur `GirderArrangements.dll`.
**Hot-reload** : éditer App/Core/Nx → `dotnet build` → relancer depuis NX sans le fermer.
**Déploiement** : copier `bin/Release/net48/*` (sauf `.pdb` et DLL NXOpen) vers
`V:\INFORMATIQUE\PLM-Nx\Macros NXOpen\GirderArrangements\`.

Config persistée : `Documents\GirderArrangements_config.json`.

## Risques / à valider en NX réel

1. **Suppression par arrangement de composants imbriqués** (aimants/cav dans `_AIMANTS`/`_CAV`) depuis
   l'arrangement de l'arc. La doc Siemens indique que `SuppressComponents` accepte des composants « de
   différents niveaux et sous-assemblages » ; à confirmer sur un vrai arc. Fallback éventuel :
   arrangements coordonnés homonymes dans les sous-ensembles.
2. **Repère local** : `Component.GetPosition` ; si l'orientation n'est pas alignée au faisceau, prévoir
   un repli sur la CSYS du squelette la plus proche.
3. Marges 700 mm / Z et règle d'appartenance (centre dans la boîte) à raffiner.

## À venir / pistes

- Sélection plus fine (arc par arc dans une cellule), bilan par arc exporté.
- Raffinage du repère local (repli sur CSYS du squelette) et des marges selon retours terrain.
