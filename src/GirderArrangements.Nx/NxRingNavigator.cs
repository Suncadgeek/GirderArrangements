using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NXOpen;
using GirderArrangements.Core;
using Assemblies = NXOpen.Assemblies;

namespace GirderArrangements.Nx
{
    /// <summary>
    /// Énumère les cellules (R2) et leurs arcs (R3) d'un anneau de stockage OUVERT EN STRUCTURE SEULE
    /// (coquille, aucun composant chargé). On lit la structure sur 2 niveaux via OpenComponents
    /// (ComponentOnly) — qui peuple Component.Name SANS charger la géométrie (la maquette anneau est
    /// énorme : on ne descend jamais dans les aimants/poutres). Porté de CheckDistances.ReportService.
    /// </summary>
    public sealed class NxRingNavigator
    {
        private readonly NxContext _ctx;
        private readonly NamingService _naming;
        private readonly IBuildLog _log;

        public NxRingNavigator(NxContext ctx, NamingService naming, IBuildLog log)
        {
            _ctx = ctx;
            _naming = naming ?? new NamingService();
            _log = log ?? NullBuildLog.Instance;
        }

        /// <summary>
        /// Cellules + arcs de l'anneau présent dans la pièce de travail (ouvert en structure seule).
        /// Ne charge ni ne ferme aucune géométrie.
        /// </summary>
        public List<RingCell> EnumerateCells()
        {
            var cells = new List<RingCell>();
            var root = _ctx.WorkPart != null ? _ctx.WorkPart.ComponentAssembly.RootComponent : null;
            if (root == null)
            {
                _log.Error("Aucun assemblage d'anneau dans la pièce de travail.");
                return cells;
            }

            var cellComps = root.GetChildren();                 // R2 : cellules (non chargées)
            _log.Info(cellComps.Length + " cellule(s) — lecture de la structure (2 niveaux, sans géométrie)…");
            LoadStructure(cellComps, "cellules");               // peuple le nom + expose les enfants

            // Tous les enfants des cellules (R3 : arcs / SD), structure chargée pour lire leur nom.
            var childByCell = new Dictionary<Assemblies.Component, Assemblies.Component[]>();
            var allArcsLevel = new List<Assemblies.Component>();
            foreach (var cell in cellComps)
            {
                var ch = cell.GetChildren();
                childByCell[cell] = ch;
                allArcsLevel.AddRange(ch);
            }
            LoadStructure(allArcsLevel.ToArray(), "arcs/SD");

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in cellComps)
            {
                var cellName = (cell.Name ?? "").Trim();
                string cellId, cellRev;
                DecodeChild(ComponentPartName(cell.Tag), out cellId, out cellRev);

                var rc = new RingCell { Name = cellName.Length > 0 ? cellName : cellId, TcRef = cellId };
                foreach (var node in childByCell[cell])
                {
                    var nom = (node.Name ?? "").Trim();
                    if (!_naming.IsArc(nom)) continue;          // on ne garde que les arcs (pas les SD)
                    string id, rev;
                    if (!DecodeChild(ComponentPartName(node.Tag), out id, out rev)) continue;
                    if (!seen.Add(id)) continue;                // dédup d'occurrence
                    rc.Arcs.Add(new RingArc { Name = nom, TcRef = id });
                }
                if (rc.Arcs.Count > 0) cells.Add(rc);
            }

            _log.Info($"{cells.Count} cellule(s) avec arcs, {cells.Sum(c => c.Arcs.Count)} arc(s) au total.");
            return cells;
        }

        /// <summary>
        /// Charge la STRUCTURE des composants (ComponentOnly) — la pièce elle-même, sans descendre dans
        /// ses enfants. Suffit à peupler Component.Name ; ne charge donc PAS la géométrie.
        /// </summary>
        private void LoadStructure(Assemblies.Component[] comps, string label)
        {
            if (comps == null || comps.Length == 0) return;
            try
            {
                Assemblies.ComponentAssembly.OpenComponentStatus[] st;
                _ctx.WorkPart.ComponentAssembly.OpenComponents(
                    Assemblies.ComponentAssembly.OpenOption.ComponentOnly, comps, out st);
                int ok = 0;
                if (st != null) foreach (var s in st) if (s == Assemblies.ComponentAssembly.OpenComponentStatus.SuccessfullyOpened) ok++;
                _log.Info("Structure " + label + " : " + ok + "/" + comps.Length + " chargée(s).");
            }
            catch (Exception ex) { _log.Warn("Chargement structure " + label + " : " + ex.Message); }
        }

        /// <summary>Nom de pièce complet d'un composant SANS charger sa pièce (UF_ASSEM_ask_component_data).</summary>
        private string ComponentPartName(Tag compTag)
        {
            try
            {
                string partName, refset, instance;
                var origin = new double[3];
                var csys = new double[9];
                var transform = new double[4, 4];
                _ctx.Uf.Assem.AskComponentData(compTag, out partName, out refset, out instance, origin, csys, transform);
                return partName ?? "";
            }
            catch { return ""; }
        }

        private static readonly Regex PnRx = new Regex(@"\bPN=(\S+)", RegexOptions.Compiled);
        private static readonly Regex PrnRx = new Regex(@"\bPRN=(\S+)", RegexOptions.Compiled);

        /// <summary>Extrait l'item-id (PN=) + révision (PRN=) de la chaîne managée d'un enfant.</summary>
        private static bool DecodeChild(string encoded, out string id, out string rev)
        {
            id = ""; rev = "";
            if (string.IsNullOrEmpty(encoded)) return false;
            var mid = PnRx.Match(encoded); if (!mid.Success) return false;
            id = mid.Groups[1].Value.Trim();
            var mrev = PrnRx.Match(encoded); rev = mrev.Success ? mrev.Groups[1].Value.Trim() : "";
            return true;
        }
    }
}
