using NXOpen;
using NXOpen.UF;

namespace GirderArrangements.Nx
{
    /// <summary>
    /// Contexte de session NX partagé par les services de l'adapter (theSession, workPart, displayPart,
    /// UFSession). Patron repris de CheckDistances / 3DBuilder.
    /// </summary>
    public sealed class NxContext
    {
        public Session Session { get; }
        public UFSession Uf { get; }
        public ListingWindow ListingWindow { get; }

        public Part WorkPart { get; set; }
        public Part DisplayPart { get; set; }

        public NxContext()
        {
            Session = Session.GetSession();
            Uf = UFSession.GetUFSession();
            ListingWindow = Session.ListingWindow;
            WorkPart = Session.Parts.Work;
            DisplayPart = Session.Parts.Display;
        }

        /// <summary>Resynchronise WorkPart/DisplayPart depuis la session.</summary>
        public void RefreshParts()
        {
            WorkPart = Session.Parts.Work;
            DisplayPart = Session.Parts.Display;
        }
    }
}
