using System;
using System.Collections.Generic;
using NXOpen;
using GirderArrangements.Core;

namespace GirderArrangements.Nx
{
    /// <summary>
    /// Enregistre des arcs en mode « PIÈCE DE TRAVAIL UNIQUEMENT » : pour chaque arc, on l'active puis
    /// on appelle Part.Save avec SaveComponents.False → seul le fichier de l'arc est écrit, PAS ses
    /// sous-produits (poutres, aimants, cav). On ne touche QUE les arcs passés en argument.
    /// </summary>
    public sealed class NxSaveService
    {
        private readonly NxContext _ctx;
        private readonly IBuildLog _log;

        public NxSaveService(NxContext ctx, IBuildLog log)
        {
            _ctx = ctx;
            _log = log ?? NullBuildLog.Instance;
        }

        /// <summary>Enregistre les arcs donnés (pièce seule). Renvoie le nombre d'arcs réellement écrits.</summary>
        public int SaveWorkPartsOnly(IEnumerable<Part> parts)
        {
            int ok = 0;
            var previous = _ctx.Session.Parts.Display;

            foreach (var part in parts)
            {
                if (part == null) continue;
                try
                {
                    Activate(part);
                    var status = part.Save(BasePart.SaveComponents.False, BasePart.CloseAfterSave.False);
                    if (status != null) status.Dispose();
                    ok++;
                    _log.Info("Enregistré (pièce de travail uniquement) : " + (part.Name ?? ""));
                }
                catch (Exception ex)
                {
                    _log.Error("Échec d'enregistrement de « " + (part.Name ?? "") + " » : " + ex.Message);
                }
            }

            // Restaure l'affichage précédent (best effort).
            try { if (previous != null) Activate(previous); } catch { }
            _ctx.RefreshParts();
            return ok;
        }

        private void Activate(Part part)
        {
            PartLoadStatus pls;
            _ctx.Session.Parts.SetActiveDisplay(part, DisplayPartOption.AllowAdditional,
                PartDisplayPartWorkPartOption.UseLast, out pls);
            if (pls != null) pls.Dispose();
        }
    }
}
