namespace GirderArrangements.Core.Config
{
    /// <summary>Granularité du balayage (le multi-arcs depuis la réf anneau est à venir).</summary>
    public enum ScopeMode
    {
        SingleArc,   // V1 : un seul arc, saisi par sa réf TC (ou l'arc déjà ouvert)
        WholeRing,   // à venir
        Cells,       // à venir
        Arcs         // à venir
    }

    /// <summary>
    /// Préférences utilisateur de GirderArrangements, persistées en JSON par <see cref="ConfigStore"/>.
    /// </summary>
    public sealed class ArrangementConfig
    {
        // --- Périmètre ---

        /// <summary>Mode de périmètre. V1 = SingleArc.</summary>
        public ScopeMode Scope { get; set; } = ScopeMode.SingleArc;

        /// <summary>Réf TC de l'arc à traiter (mode mono-arc). Vide = utiliser l'arc déjà ouvert.</summary>
        public string ArcTcRef { get; set; } = "";

        /// <summary>Réf TC de l'anneau de stockage (mode multi-arcs, à venir).</summary>
        public string RingTcRef { get; set; } = "";

        // --- Boîte de sélection ---

        /// <summary>Débordement de la boîte en X et Y (mm), de chaque côté de l'emprise de la poutre.</summary>
        public double MarginXYmm { get; set; } = 700.0;

        /// <summary>Débordement de la boîte le long du faisceau (Z local, mm), de chaque côté.</summary>
        public double MarginZmm { get; set; } = 0.0;

        // --- Contenu / comportement ---

        /// <summary>Inclure le squelette (« *_SQL ») dans les arrangements (sinon il est masqué).</summary>
        public bool IncludeSkeleton { get; set; } = false;

        /// <summary>Écraser (recalculer entièrement) un arrangement existant de même nom.</summary>
        public bool OverwriteExisting { get; set; } = true;

        /// <summary>Niveau de journal affiché (0=Info, 1=Avert., 2=Erreur).</summary>
        public int LogLevel { get; set; } = 0;
    }
}
