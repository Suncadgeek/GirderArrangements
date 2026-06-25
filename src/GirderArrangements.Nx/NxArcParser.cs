using NXOpen;
using GirderArrangements.Core;
using Assemblies = NXOpen.Assemblies;

namespace GirderArrangements.Nx
{
    /// <summary>
    /// Parcourt la pièce de travail (= l'arc ouvert) et en extrait la décomposition : poutres
    /// (« *POUTRE*_NN »), ensemble aimants (« *_AIMANTS »), ensemble chambre (« *_CAV_EQUIPEE »),
    /// squelette (« *_SQL »), et les items (enfants directs des ensembles, hors squelette).
    /// </summary>
    public sealed class NxArcParser
    {
        private readonly NxContext _ctx;
        private readonly NamingService _naming;
        private readonly IBuildLog _log;

        public NxArcParser(NxContext ctx, NamingService naming, IBuildLog log)
        {
            _ctx = ctx;
            _naming = naming ?? new NamingService();
            _log = log ?? NullBuildLog.Instance;
        }

        /// <summary>Analyse l'arc présent dans la pièce de travail. Null si aucun assemblage.</summary>
        public ArcModel Parse()
        {
            var part = _ctx.WorkPart;
            var root = part != null ? part.ComponentAssembly.RootComponent : null;
            if (root == null)
            {
                _log.Error("Aucun assemblage dans la pièce de travail.");
                return null;
            }

            var model = new ArcModel
            {
                ArcPart = part,
                Root = root,
                ArcName = NxNaming.PartName(root)
            };

            foreach (var child in root.GetChildren())
            {
                var name = NxNaming.PartName(child);

                if (_naming.IsPoutre(name))
                {
                    int n;
                    if (_naming.TryExtractPoutreNumber(name, out n))
                        model.Beams.Add(new ArcBeam { Component = child, Name = name, Number = n });
                    else
                        _log.Warn("Poutre sans numéro « _NN » ignorée : " + name);
                    continue;
                }

                if (model.MagnetEnsemble == null && _naming.IsMagnetEnsemble(name)) { model.MagnetEnsemble = child; continue; }
                if (model.CavEnsemble == null && _naming.IsVacuumChamber(name)) { model.CavEnsemble = child; continue; }
                if (_naming.IsSkeleton(name)) { model.SkeletonComponents.Add(child); continue; }

                _log.Info("Composant d'arc non classé (laissé visible) : " + name);
            }

            CollectItems(model.MagnetEnsemble, model.MagnetItems, model);
            CollectItems(model.CavEnsemble, model.CavItems, model);

            model.Beams.Sort((a, b) => a.Number.CompareTo(b.Number));

            _log.Info($"Arc « {model.ArcName} » : {model.Beams.Count} poutre(s), "
                + $"{model.MagnetItems.Count} aimant(s), {model.CavItems.Count} cav.");
            if (model.MagnetEnsemble == null) _log.Warn("Ensemble aimants (« *_AIMANTS ») introuvable.");
            if (model.CavEnsemble == null) _log.Warn("Ensemble chambre (« *_CAV_EQUIPEE ») introuvable.");
            return model;
        }

        // Items = enfants directs de l'ensemble, le squelette éventuel étant écarté (et mémorisé pour masquage).
        private void CollectItems(Assemblies.Component ensemble, System.Collections.Generic.List<Assemblies.Component> dest, ArcModel model)
        {
            if (ensemble == null) return;
            foreach (var c in ensemble.GetChildren())
            {
                var name = NxNaming.PartName(c);
                if (_naming.IsSkeleton(name)) { model.SkeletonComponents.Add(c); continue; }
                dest.Add(c);
            }
        }
    }
}
