using NXOpen;

namespace GirderArrangements.Nx
{
    /// <summary>
    /// Détection du mode d'exécution. GirderArrangements cible le mode MANAGÉ (Teamcenter). La détection
    /// s'appuie sur la session PDM ; en cas de doute on n'impose pas (avertissement seulement).
    /// </summary>
    public static class NxEnvironment
    {
        /// <summary>Vrai si la session paraît managée (Teamcenter).</summary>
        public static bool IsManaged(NxContext ctx)
        {
            try
            {
                var pdm = ctx.Session.PdmSession;
                return pdm != null && pdm.Tag != Tag.Null;
            }
            catch
            {
                return false;
            }
        }
    }
}
