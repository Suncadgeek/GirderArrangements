using System;
using System.Collections.Generic;
using System.Linq;
using GirderArrangements.Core;
using GirderArrangements.Core.Config;
using GirderArrangements.Core.Geometry;
using GirderArrangements.Core.Partition;
using GirderArrangements.Nx;
using Assemblies = NXOpen.Assemblies;

namespace GirderArrangements
{
    /// <summary>Une poutre détectée, présentée à l'UI.</summary>
    public sealed class BeamInfo
    {
        public int Index { get; set; }
        public string PoutreName { get; set; } = "";
        public string ArrangementName { get; set; } = "";
        public int Number { get; set; }
    }

    public sealed class LoadResult
    {
        public string ArcName { get; set; } = "";
        public List<BeamInfo> Beams { get; } = new List<BeamInfo>();
    }

    public sealed class RunResult
    {
        public int ArcsProcessed { get; set; }
        public int BeamsProcessed { get; set; }
        public int Created { get; set; }
        public int Updated { get; set; }
        public int ItemsAssigned { get; set; }
        public int ItemsUnassigned { get; set; }
        public bool Cancelled { get; set; }
    }

    /// <summary>Un arc dont les arrangements ont été créés/mis à jour (candidat à l'enregistrement).</summary>
    public sealed class ModifiedArc
    {
        public string Name { get; set; } = "";
        public NXOpen.Part Part { get; set; }
        public int Created { get; set; }
        public int Updated { get; set; }
    }

    /// <summary>
    /// Orchestrateur. Deux modes :
    ///  - MONO-ARC : <see cref="Load"/> (réf TC d'arc ou arc courant) puis <see cref="Run"/> (poutres choisies).
    ///  - ANNEAU : <see cref="ListRing"/> (anneau en STRUCTURE SEULE → cellules/arcs, sans charger la
    ///    maquette) puis <see cref="RunRing"/> (ouvre chaque arc des cellules choisies, en le GARDANT
    ///    ouvert pour un enregistrement TC manuel par l'utilisateur).
    /// Relie la couche Nx (NXOpen) et la couche Core (répartition pure).
    /// </summary>
    public sealed class ArrangementGenerator
    {
        private readonly IBuildLog _log;
        private readonly NamingService _naming = new NamingService();
        private readonly NxContext _ctx;
        private readonly NxArcOpener _opener;

        private ArcModel _model;
        private readonly List<ModifiedArc> _modified = new List<ModifiedArc>();
        private readonly Dictionary<NXOpen.Part, ModifiedArc> _modifiedByPart = new Dictionary<NXOpen.Part, ModifiedArc>();

        public ArrangementGenerator(IBuildLog log)
        {
            _log = log ?? NullBuildLog.Instance;
            _ctx = new NxContext();
            _opener = new NxArcOpener(_ctx, _log);
        }

        /// <summary>Arcs modifiés (arrangements créés/MAJ) depuis la création de l'orchestrateur.</summary>
        public IReadOnlyList<ModifiedArc> ModifiedArcs => _modified;

        /// <summary>Enregistre les arcs donnés en « pièce de travail uniquement ». Renvoie le nombre écrit.</summary>
        public int SaveArcs(IEnumerable<NXOpen.Part> parts)
            => new NxSaveService(_ctx, _log).SaveWorkPartsOnly(parts);

        // ===================================================================== MONO-ARC

        /// <summary>Ouvre l'arc (réf TC) ou utilise l'arc courant, puis liste les poutres.</summary>
        public LoadResult Load(ArrangementConfig cfg)
        {
            if (!NxEnvironment.IsManaged(_ctx))
                _log.Warn("Session NON managée détectée — l'ouverture par réf TC peut échouer.");

            var arcRef = (cfg.ArcTcRef ?? "").Trim();
            if (!string.IsNullOrEmpty(arcRef))
            {
                _opener.OpenManaged(arcRef, partial: false);
            }
            else
            {
                _ctx.RefreshParts();
                if (_ctx.WorkPart == null) throw new InvalidOperationException("Aucun arc ouvert et aucune réf TC saisie.");
                _log.Info("Utilisation de l'arc déjà ouvert : " + (_ctx.WorkPart.Name ?? ""));
            }

            _model = Parse();
            return BuildLoadResult(_model);
        }

        /// <summary>Génère/MAJ les arrangements pour les poutres choisies (mono-arc déjà chargé).</summary>
        public RunResult Run(ArrangementConfig cfg, ICollection<int> selectedBeamIndexes, Func<bool> isCancelled)
        {
            if (_model == null) throw new InvalidOperationException("Charge d'abord l'arc (Load).");
            var result = new RunResult();
            ProcessArc(cfg, selectedBeamIndexes, isCancelled, result, reportProgress: true);
            if (!result.Cancelled) result.ArcsProcessed = 1;
            return result;
        }

        // ===================================================================== ANNEAU

        public sealed class RingListResult
        {
            public List<RingCell> Cells { get; } = new List<RingCell>();
        }

        /// <summary>
        /// Ouvre l'anneau en STRUCTURE SEULE, liste les cellules + arcs (réfs TC), PUIS FERME l'anneau.
        /// On ne garde pas l'anneau ouvert : une fois les arcs ouverts pour traitement, l'anneau ouvert
        /// derrière déclencherait des boucles de mise à jour. Les arcs seront rouverts seuls à la génération.
        /// </summary>
        public RingListResult ListRing(ArrangementConfig cfg)
        {
            var ringRef = (cfg.RingTcRef ?? "").Trim();
            if (string.IsNullOrEmpty(ringRef)) throw new InvalidOperationException("Réf TC de l'anneau vide.");
            if (!NxEnvironment.IsManaged(_ctx))
                _log.Warn("Session NON managée — l'ouverture de l'anneau par réf TC peut échouer.");

            _log.Info("Ouverture de l'anneau en structure seule : " + ringRef);
            var ring = _opener.OpenManaged(ringRef, partial: true);
            _ctx.RefreshParts();

            var nav = new NxRingNavigator(_ctx, _naming, _log);
            var res = new RingListResult();
            res.Cells.AddRange(nav.EnumerateCells());

            CloseRing(ring);   // réfs obtenues → on ferme l'anneau (évite les boucles de MAJ)
            return res;
        }

        /// <summary>
        /// Ferme l'anneau (et l'arbre chargé) une fois les références d'arcs récupérées. Fermeture ciblée
        /// d'abord ; repli sur CloseAll si NX refuse de fermer la pièce affichée.
        /// </summary>
        private void CloseRing(NXOpen.Part ring)
        {
            if (ring == null) return;
            try
            {
                ring.Close(NXOpen.BasePart.CloseWholeTree.True, NXOpen.BasePart.CloseModified.CloseModified, null);
                _ctx.RefreshParts();
                _log.Info("Anneau fermé (références d'arcs conservées).");
            }
            catch (Exception ex)
            {
                _log.Warn("Fermeture ciblée de l'anneau impossible (" + ex.Message + ") — repli CloseAll.");
                try
                {
                    _ctx.Session.Parts.CloseAll(NXOpen.BasePart.CloseModified.CloseModified, null);
                    _ctx.RefreshParts();
                    _log.Info("Anneau fermé (CloseAll).");
                }
                catch (Exception ex2) { _log.Warn("Fermeture de l'anneau : " + ex2.Message); }
            }
        }

        /// <summary>
        /// Pour chaque arc demandé : ouvre l'arc seul (géométrie pleine), génère ses arrangements, et le
        /// LAISSE OUVERT (l'enregistrement Teamcenter est manuel). Ne ferme jamais un arc.
        /// </summary>
        public RunResult RunRing(ArrangementConfig cfg, IList<RingArc> arcs, Func<bool> isCancelled)
        {
            if (arcs == null || arcs.Count == 0) throw new InvalidOperationException("Aucun arc sélectionné.");
            var result = new RunResult();

            int idx = 0;
            foreach (var arcRef in arcs)
            {
                if (isCancelled != null && isCancelled()) { result.Cancelled = true; break; }
                idx++;
                _log.Info($"[{idx}/{arcs.Count}] Ouverture de l'arc « {arcRef.Name} » …");
                try
                {
                    _opener.OpenManaged(arcRef.TcRef, partial: false);   // gardé ouvert (pas de Close)
                    _ctx.RefreshParts();
                    _model = Parse();
                    ProcessArc(cfg, null, isCancelled, result, reportProgress: false);
                    result.ArcsProcessed++;
                }
                catch (Exception ex)
                {
                    _log.Error("Arc « " + arcRef.Name + " » : " + ex.Message);
                }
                _log.Progress(idx, arcs.Count);
            }

            _log.Info("Arcs laissés OUVERTS — ENREGISTRE-les dans Teamcenter pour conserver les arrangements.");
            return result;
        }

        // ===================================================================== commun

        private ArcModel Parse()
        {
            var parser = new NxArcParser(_ctx, _naming, _log);
            var model = parser.Parse();
            if (model == null) throw new InvalidOperationException("Impossible d'analyser l'arc.");
            return model;
        }

        private LoadResult BuildLoadResult(ArcModel model)
        {
            var res = new LoadResult { ArcName = model.ArcName };
            for (int i = 0; i < model.Beams.Count; i++)
            {
                var b = model.Beams[i];
                res.Beams.Add(new BeamInfo
                {
                    Index = i,
                    PoutreName = b.Name,
                    Number = b.Number,
                    ArrangementName = _naming.ArrangementName(b.Number)
                });
            }
            return res;
        }

        /// <summary>Traite l'arc courant (_model) : boîtes locales, répartition, arrangements. Accumule dans result.</summary>
        private void ProcessArc(ArrangementConfig cfg, ICollection<int> selectedBeamIndexes,
            Func<bool> isCancelled, RunResult result, bool reportProgress)
        {
            var reader = new NxGeometryReader(_ctx, _log);

            // --- Boîte locale par poutre ---
            var slots = new List<BeamSlot>();
            for (int i = 0; i < _model.Beams.Count; i++)
            {
                var beam = _model.Beams[i];
                if (!reader.TryBoundingBox(beam.Component, out var aabb))
                {
                    _log.Warn("Poutre sans géométrie (ignorée) : " + beam.Name);
                    continue;
                }
                var frame = reader.LocalFrame(beam.Component);
                var box = SelectionBox.FromWorldExtent(frame, Aabb.Corners(aabb), cfg.MarginXYmm, cfg.MarginZmm);
                // Option A : centre = origine de la CSYS de montage de la poutre (GetPosition), stable et
                // sur l'axe de la poutre — au lieu du milieu de l'AABB absolue (décalé en Z si la poutre
                // est inclinée / si des supports débordent). Sert au départage des recouvrements.
                slots.Add(new BeamSlot { Id = i, Name = beam.Name, Center = frame.Origin, Box = box });
            }

            // --- Items (aimants + cav) ---
            var items = _model.AllItems.ToList();
            var leaves = new List<LeafItem>();
            for (int k = 0; k < items.Count; k++)
            {
                if (!reader.TryCenter(items[k], out var c))
                {
                    _log.Warn("Item sans géométrie (ignoré) : " + NxItemName(items[k]));
                    continue;
                }
                leaves.Add(new LeafItem { Id = k, Center = c });
            }

            var partition = BeamPartitioner.Assign(slots, leaves);
            result.ItemsAssigned += partition.LeafToBeam.Count;
            result.ItemsUnassigned += partition.Unassigned.Count;
            _log.Info($"Répartition : {partition.LeafToBeam.Count} item(s) rattaché(s), {partition.Unassigned.Count} hors boîte.");

            // --- Arrangements ---
            var arrSvc = new NxArrangementService(_model.ArcPart, _log);
            var master = arrSvc.ActiveArrangement;

            var selected = (selectedBeamIndexes != null && selectedBeamIndexes.Count > 0)
                ? new HashSet<int>(selectedBeamIndexes)
                : new HashSet<int>(Enumerable.Range(0, _model.Beams.Count));

            int done = 0, localCreated = 0, localUpdated = 0;
            foreach (var slot in slots)
            {
                if (isCancelled != null && isCancelled()) { result.Cancelled = true; break; }
                if (!selected.Contains(slot.Id)) continue;

                var beam = _model.Beams[slot.Id];
                var arrName = _naming.ArrangementName(beam.Number);

                var memberItems = partition.BeamToLeaves.TryGetValue(slot.Id, out var ids) ? ids : new List<int>();
                var memberSet = new HashSet<int>(memberItems);

                var members = new List<Assemblies.Component> { beam.Component };
                foreach (var k in memberItems) members.Add(items[k]);

                var nonMembers = new List<Assemblies.Component>();
                for (int j = 0; j < _model.Beams.Count; j++)
                    if (j != slot.Id) nonMembers.Add(_model.Beams[j].Component);
                for (int k = 0; k < items.Count; k++)
                    if (!memberSet.Contains(k)) nonMembers.Add(items[k]);
                if (!cfg.IncludeSkeleton)
                    nonMembers.AddRange(_model.SkeletonComponents);

                arrSvc.ActiveArrangement = master; // template propre pour la création
                var existing = arrSvc.Find(arrName);
                if (existing != null && !cfg.OverwriteExisting)
                {
                    _log.Info("Arrangement « " + arrName + " » déjà présent — conservé (écrasement désactivé).");
                    continue;
                }

                var arr = arrSvc.FindOrCreate(arrName, out bool created);
                if (created) { result.Created++; localCreated++; } else { result.Updated++; localUpdated++; }
                arrSvc.ApplyMembership(arr, members, nonMembers);

                result.BeamsProcessed++;
                done++;
                if (reportProgress) _log.Progress(done, selected.Count);
            }

            arrSvc.ActiveArrangement = master; // restaure l'arrangement maître
            if (localCreated + localUpdated > 0) RegisterModified(_model, localCreated, localUpdated);
        }

        private void RegisterModified(ArcModel model, int created, int updated)
        {
            ModifiedArc entry;
            if (_modifiedByPart.TryGetValue(model.ArcPart, out entry))
            {
                entry.Created += created;
                entry.Updated += updated;
                return;
            }
            entry = new ModifiedArc { Name = model.ArcName, Part = model.ArcPart, Created = created, Updated = updated };
            _modifiedByPart[model.ArcPart] = entry;
            _modified.Add(entry);
        }

        private static string NxItemName(Assemblies.Component c)
        {
            try { return c.DisplayName ?? c.Name ?? "?"; } catch { return "?"; }
        }
    }
}
