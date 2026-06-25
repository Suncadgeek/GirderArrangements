using System.Collections.Generic;
using NXOpen;
using Assemblies = NXOpen.Assemblies;

namespace GirderArrangements.Nx
{
    /// <summary>Une poutre de l'arc : son composant, son nom et son numéro (suffixe « _NN »).</summary>
    public sealed class ArcBeam
    {
        public Assemblies.Component Component { get; set; }
        public string Name { get; set; } = "";
        public int Number { get; set; }
    }

    /// <summary>
    /// Décomposition topologique d'un arc : poutres, ensembles aimants/cav, squelette, et les
    /// composants « items » (enfants directs des ensembles, hors squelette) à répartir sur les poutres.
    /// </summary>
    public sealed class ArcModel
    {
        /// <summary>Pièce de l'arc (work part) — porte les arrangements.</summary>
        public Part ArcPart { get; set; }

        /// <summary>Composant racine de l'assemblage de l'arc.</summary>
        public Assemblies.Component Root { get; set; }

        public string ArcName { get; set; } = "";

        public List<ArcBeam> Beams { get; } = new List<ArcBeam>();

        public Assemblies.Component MagnetEnsemble { get; set; }
        public Assemblies.Component CavEnsemble { get; set; }

        /// <summary>Items aimants = enfants directs de l'ensemble aimants (hors squelette).</summary>
        public List<Assemblies.Component> MagnetItems { get; } = new List<Assemblies.Component>();

        /// <summary>Items cav = enfants directs de l'ensemble chambre à vide (hors squelette).</summary>
        public List<Assemblies.Component> CavItems { get; } = new List<Assemblies.Component>();

        /// <summary>Composants squelette à masquer quand on n'inclut pas le squelette.</summary>
        public List<Assemblies.Component> SkeletonComponents { get; } = new List<Assemblies.Component>();

        /// <summary>Tous les items à répartir (aimants + cav).</summary>
        public IEnumerable<Assemblies.Component> AllItems
        {
            get
            {
                foreach (var c in MagnetItems) yield return c;
                foreach (var c in CavItems) yield return c;
            }
        }
    }
}
