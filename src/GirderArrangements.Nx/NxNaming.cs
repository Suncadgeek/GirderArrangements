using NXOpen;
using Assemblies = NXOpen.Assemblies;

namespace GirderArrangements.Nx
{
    /// <summary>Helpers de nommage des composants NX (réf TC / nom de pièce du prototype managé).</summary>
    internal static class NxNaming
    {
        /// <summary>
        /// Nom de pièce du prototype (réf TC en managé), avec repli sur le nom d'instance.
        /// </summary>
        public static string PartName(Assemblies.Component comp)
        {
            try
            {
                var owning = comp.Prototype != null ? comp.Prototype.OwningPart : null;
                if (owning != null && !string.IsNullOrEmpty(owning.Name)) return owning.Name;
            }
            catch { /* repli ci-dessous */ }
            return comp.DisplayName ?? "";
        }

        /// <summary>Nom d'instance lisible.</summary>
        public static string InstanceName(Assemblies.Component comp)
            => comp.DisplayName ?? comp.Name ?? "";
    }
}
