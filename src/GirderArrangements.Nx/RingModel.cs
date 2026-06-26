using System.Collections.Generic;

namespace GirderArrangements.Nx
{
    /// <summary>Réf d'un arc dans l'anneau : nom lisible + item-id TC (pour rouvrir l'arc seul).</summary>
    public sealed class RingArc
    {
        public string Name { get; set; } = "";
        public string TcRef { get; set; } = "";
    }

    /// <summary>Une cellule de l'anneau (enfant direct R2) et les arcs qu'elle contient (R3).</summary>
    public sealed class RingCell
    {
        public string Name { get; set; } = "";
        public string TcRef { get; set; } = "";
        // set; requis : System.Text.Json ne PEUPLE PAS une collection en lecture seule à la
        // désérialisation (le cache anneau relirait des cellules sans arcs).
        public List<RingArc> Arcs { get; set; } = new List<RingArc>();
    }
}
