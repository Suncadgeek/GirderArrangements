using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using GirderArrangements.Nx;

namespace GirderArrangements
{
    /// <summary>
    /// Cache disque de la structure d'anneau (cellules + arcs) par réf TC d'anneau. Le listing à froid
    /// est lent (ouverture structure + lecture du nom .prt de chaque cellule/arc) ; comme la liste est
    /// stable on la mémorise en JSON dans Documents, à côté de la config.
    ///
    /// « Lister » lit le cache s'il existe ; « Rafraîchir » force la relecture Teamcenter et réécrit.
    ///
    /// RÉUTILISE LE CACHE DE CheckDistances : si notre cache est absent mais que CheckDistances a déjà
    /// listé le même anneau (fichier « CheckDistances_arcs_&lt;ref&gt;.json », liste plate d'arcs), on le
    /// relit et on présente les arcs sous une cellule unique (même clé de fichier → même réf saisie).
    /// </summary>
    internal static class RingListCache
    {
        private static readonly JsonSerializerOptions Opt = new JsonSerializerOptions { WriteIndented = true };

        private static string Docs => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        private static string PathFor(string ringTcRef)
            => Path.Combine(Docs, "GirderArrangements_ring_" + Sanitize(ringTcRef) + ".json");

        /// <summary>Fichier de cache d'arcs de CheckDistances pour le même anneau (même convention de nom).</summary>
        private static string CheckDistancesPathFor(string ringTcRef)
            => Path.Combine(Docs, "CheckDistances_arcs_" + Sanitize(ringTcRef) + ".json");

        /// <summary>
        /// Cellules en cache pour cette réf (notre cache d'abord, puis repli sur celui de CheckDistances),
        /// ou null si rien d'exploitable. <paramref name="source"/> décrit l'origine pour le journal.
        /// </summary>
        public static List<RingCell> Load(string ringTcRef, out string source)
        {
            source = "";

            // 1) Notre cache : structure complète cellules → arcs.
            try
            {
                var p = PathFor(ringTcRef);
                if (File.Exists(p))
                {
                    var cells = JsonSerializer.Deserialize<List<RingCell>>(File.ReadAllText(p), Opt);
                    if (cells != null && cells.Any(c => c.Arcs.Count > 0))
                    {
                        source = "cache GirderArrangements";
                        return cells;
                    }
                }
            }
            catch { /* cache illisible → on tente CheckDistances */ }

            // 2) Repli : cache d'arcs plat de CheckDistances (mêmes objets {Name, TcRef}).
            try
            {
                var p = CheckDistancesPathFor(ringTcRef);
                if (File.Exists(p))
                {
                    var arcs = JsonSerializer.Deserialize<List<RingArc>>(File.ReadAllText(p), Opt);
                    if (arcs != null && arcs.Count > 0)
                    {
                        var cell = new RingCell { Name = "Arcs de l'anneau", TcRef = (ringTcRef ?? "").Trim() };
                        foreach (var a in arcs)
                            if (a != null && !string.IsNullOrWhiteSpace(a.TcRef)) cell.Arcs.Add(a);
                        if (cell.Arcs.Count > 0)
                        {
                            source = "cache CheckDistances";
                            return new List<RingCell> { cell };
                        }
                    }
                }
            }
            catch { /* repli indisponible */ }

            return null;
        }

        public static void Save(string ringTcRef, IEnumerable<RingCell> cells)
        {
            try { File.WriteAllText(PathFor(ringTcRef), JsonSerializer.Serialize(cells, Opt)); }
            catch { /* best effort */ }
        }

        private static string Sanitize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "x";
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s.Trim();
        }
    }
}
