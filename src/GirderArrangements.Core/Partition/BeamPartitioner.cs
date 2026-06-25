using System.Collections.Generic;
using GirderArrangements.Core.Geometry;

namespace GirderArrangements.Core.Partition
{
    /// <summary>Une poutre candidate : son centre approximatif et sa boîte de sélection locale.</summary>
    public sealed class BeamSlot
    {
        /// <summary>Identifiant stable (index) faisant le lien avec le composant NX côté adapter.</summary>
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public Vec3 Center { get; set; }
        public SelectionBox Box { get; set; }
    }

    /// <summary>Un composant feuille (aimant / cav individuel) à rattacher à une poutre.</summary>
    public sealed class LeafItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public Vec3 Center { get; set; }
    }

    /// <summary>Résultat de la répartition : pour chaque feuille, la poutre retenue (ou aucune).</summary>
    public sealed class PartitionResult
    {
        /// <summary>feuille.Id → poutre.Id (absent si la feuille n'est dans aucune boîte).</summary>
        public Dictionary<int, int> LeafToBeam { get; } = new Dictionary<int, int>();

        /// <summary>poutre.Id → feuilles assignées.</summary>
        public Dictionary<int, List<int>> BeamToLeaves { get; } = new Dictionary<int, List<int>>();

        /// <summary>Feuilles tombées dans aucune boîte (utile pour le bilan).</summary>
        public List<int> Unassigned { get; } = new List<int>();
    }

    /// <summary>
    /// Répartit chaque feuille sur AU PLUS une poutre. Une feuille est candidate aux poutres dont la
    /// boîte la contient ; en cas de recouvrement (marges ±700), elle est rattachée à la poutre dont
    /// le centre est le plus proche (départage : plus petit Id). Pur, testable (sans NXOpen).
    /// </summary>
    public static class BeamPartitioner
    {
        public static PartitionResult Assign(IReadOnlyList<BeamSlot> beams, IReadOnlyList<LeafItem> leaves)
        {
            var res = new PartitionResult();
            foreach (var b in beams) res.BeamToLeaves[b.Id] = new List<int>();

            foreach (var leaf in leaves)
            {
                int bestId = -1;
                double bestDist = double.MaxValue;
                foreach (var b in beams)
                {
                    if (b.Box == null || !b.Box.Contains(leaf.Center)) continue;
                    double d = (leaf.Center - b.Center).Length;
                    if (d < bestDist || (d == bestDist && b.Id < bestId))
                    {
                        bestDist = d;
                        bestId = b.Id;
                    }
                }

                if (bestId < 0) { res.Unassigned.Add(leaf.Id); continue; }
                res.LeafToBeam[leaf.Id] = bestId;
                res.BeamToLeaves[bestId].Add(leaf.Id);
            }

            return res;
        }
    }
}
