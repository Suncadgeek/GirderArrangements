using System;
using System.Collections.Generic;
using NXOpen;
using GirderArrangements.Core;

namespace GirderArrangements.Nx
{
    /// <summary>
    /// Ouvre un arc / un anneau en mode MANAGÉ (Teamcenter) par sa réf TC. Plein (géométrie, pour les
    /// boîtes englobantes) ou STRUCTURE SEULE (coquille de l'anneau, sans charger les composants — la
    /// maquette anneau est énorme). Porté de NxRingOpener (CheckDistances / 3DBuilder). Réutilise la
    /// pièce si déjà ouverte.
    /// </summary>
    public sealed class NxArcOpener
    {
        private readonly NxContext _ctx;
        private readonly IBuildLog _log;

        /// <summary>Révision résolue lors de la dernière ouverture (traçabilité).</summary>
        public string ResolvedRevision { get; private set; } = "";

        public NxArcOpener(NxContext ctx, IBuildLog log)
        {
            _ctx = ctx;
            _log = log ?? NullBuildLog.Instance;
        }

        /// <param name="partial">
        /// true = STRUCTURE SEULE (coquille de l'anneau, aucun composant chargé) pour énumérer les arcs
        /// sans charger la maquette ; false = chargement PLEIN (géométrie requise pour AskBoundingBox).
        /// </param>
        public Part OpenManaged(string tcRef, bool partial = false)
        {
            var theSession = _ctx.Session;
            var token = (tcRef ?? "").Trim();

            var existing = FindLoadedPart(token);
            if (existing != null)
            {
                PartLoadStatus plsReuse;
                theSession.Parts.SetActiveDisplay(existing, DisplayPartOption.AllowAdditional,
                    PartDisplayPartWorkPartOption.UseLast, out plsReuse);
                plsReuse.Dispose();
                _ctx.RefreshParts();
                _log.Info("Pièce déjà ouverte : réutilisée.");
                return existing;
            }

            ApplyLoadOptions(theSession.Parts.LoadOptions, partial);
            var spec = ResolveManagedSpec(token);
            _log.Info((partial ? "Ouverture structure : " : "Ouverture : ") + spec);
            PartLoadStatus pls;
            var part = (Part)theSession.Parts.OpenActiveDisplay(spec, DisplayPartOption.AllowAdditional, out pls);
            pls.Dispose();
            _ctx.RefreshParts();
            return part;
        }

        /// <summary>
        /// Structure seule (partial=true) : AUCUN composant chargé (ComponentsToLoad=None) — on lit
        /// ensuite la structure cellule/arc À LA DEMANDE (OpenComponents ComponentOnly). Plein
        /// (partial=false) : géométrie complète. Options par-ouverture (porté de NxRingOpener).
        /// </summary>
        private void ApplyLoadOptions(LoadOptions lo, bool partial)
        {
            try
            {
                if (partial)
                {
                    lo.ComponentsToLoad = LoadOptions.LoadComponents.None;
                    lo.UsePartialLoading = true;
                    lo.UseLightweightRepresentations = false;
                    lo.PartLoadOption = LoadOptions.LoadOption.PartiallyLoad;
                }
                else
                {
                    lo.ComponentsToLoad = LoadOptions.LoadComponents.All;
                    lo.UsePartialLoading = false;
                    lo.UseLightweightRepresentations = false;
                    lo.PartLoadOption = LoadOptions.LoadOption.FullyLoad;
                }
            }
            catch (Exception ex)
            {
                _log.Warn("Options de chargement : " + ex.Message);
            }
        }

        /// <summary>
        /// TC exige la révision (@DB/&lt;id&gt;/&lt;rev&gt;) — on liste les révisions et on retient la
        /// dernière. Repli sur @DB/&lt;id&gt;.
        /// </summary>
        private string ResolveManagedSpec(string itemId)
        {
            var id = (itemId ?? "").Trim();
            ResolvedRevision = "";
            try
            {
                Tag itemTag;
                _ctx.Uf.Ugmgr.AskPartTag(id, out itemTag);
                if (itemTag == Tag.Null)
                {
                    _log.Warn("Item introuvable dans Teamcenter : " + id);
                    return "@DB/" + id;
                }

                int count;
                Tag[] revTags;
                _ctx.Uf.Ugmgr.ListPartRevisions(itemTag, out count, out revTags);
                if (revTags != null && revTags.Length > 0)
                {
                    var ids = new List<string>();
                    foreach (var rt in revTags)
                    {
                        string rid;
                        _ctx.Uf.Ugmgr.AskPartRevisionId(rt, out rid);
                        ids.Add(rid);
                    }
                    _log.Info("Révisions de " + id + " : " + string.Join(", ", ids));
                    var rev = ids[ids.Count - 1];
                    ResolvedRevision = rev;
                    _log.Info("Révision retenue : " + rev);
                    return "@DB/" + id + "/" + rev;
                }
                _log.Warn("Aucune révision listée pour " + id + ".");
            }
            catch (Exception ex)
            {
                _log.Warn("Résolution de révision impossible pour " + id + " : " + ex.Message);
            }
            return "@DB/" + id;
        }

        private Part FindLoadedPart(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;
            foreach (BasePart bp in _ctx.Session.Parts)
            {
                var p = bp as Part;
                if (p == null) continue;
                if ((p.Name ?? "").IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0
                    || (p.Leaf ?? "").IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    return p;
            }
            return null;
        }
    }
}
