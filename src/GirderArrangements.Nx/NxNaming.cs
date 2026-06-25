using NXOpen;
using Assemblies = NXOpen.Assemblies;

namespace GirderArrangements.Nx
{
    /// <summary>Helpers de nommage des composants NX (réf TC / nom de pièce du prototype managé).</summary>
    internal static class NxNaming
    {
        /// <summary>
        /// Nom métier LISIBLE du composant, fiable une fois la pièce chargée :
        ///   1) attribut « DB_PART_NAME » (« V3631_ARC20 ») ;
        ///   2) ((Part)Prototype).Name (« CAO…/AA-V3631_ARC20 ») nettoyé de son préfixe id/rév ;
        ///   3) repli DisplayName / Name (peu fiable).
        /// ⚠️ Ne PAS se fier à Component.Name ni à Prototype.OwningPart.Name : en chargement partiel ils
        /// renvoient l'id seul (« CAO000166374/AA »), d'où des composants « non classés ». (Recette NX.)
        /// </summary>
        public static string PartName(Assemblies.Component comp)
        {
            var part = comp != null ? comp.Prototype as Part : null;
            if (part != null)
            {
                try
                {
#pragma warning disable CS0618 // GetStringAttribute déprécié mais validé ici ; repli sûr sur Prototype.Name
                    var v = part.GetStringAttribute("DB_PART_NAME");
#pragma warning restore CS0618
                    if (!string.IsNullOrEmpty(v)) return v.Trim();
                }
                catch { /* attribut absent → nom de pièce */ }

                if (!string.IsNullOrEmpty(part.Name)) return StripManagedPrefix(part.Name);
            }
            return comp != null ? (comp.DisplayName ?? comp.Name ?? "") : "";
        }

        /// <summary>« CAO…/AA-V3631_ARC20 » → « V3631_ARC20 » (préfixe id/rév retiré).</summary>
        public static string StripManagedPrefix(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            int slash = s.IndexOf('/');
            int dash = s.IndexOf('-');
            return (slash >= 0 && dash > slash) ? s.Substring(dash + 1).Trim() : s.Trim();
        }

        /// <summary>Nom d'instance lisible.</summary>
        public static string InstanceName(Assemblies.Component comp)
            => comp.DisplayName ?? comp.Name ?? "";
    }
}
