using System.Windows.Forms;

namespace GirderArrangements
{
    /// <summary>
    /// Point d'entrée du code applicatif, invoqué par RÉFLEXION depuis le launcher
    /// (GirderArrangements.Launcher → « GirderArrangements.dll »). Ne pas renommer : le launcher cherche
    /// « GirderArrangements.AppEntry.Run ».
    ///
    /// MODELESS (Show) : la fenêtre REND LA MAIN à la session NX dès l'affichage — on peut tourner la
    /// maquette et activer/contrôler les arrangements créés AVANT de les enregistrer via le bouton de
    /// l'appli. Pour que la fenêtre ne tourne pas sur du code déchargé, le launcher garde l'image chargée
    /// jusqu'à la fermeture de NX (GetUnloadOption = AtTermination), et on conserve une référence statique
    /// vers la fenêtre (sinon le GC la ramasserait après le retour de Run).
    /// </summary>
    public static class AppEntry
    {
        private static MainForm _form;   // racine GC de la fenêtre modeless (survit au retour de Run)

        public static void Run()
        {
            // Relancement / hot-reload : referme une éventuelle fenêtre déjà ouverte (toute version).
            CloseExisting();

            _form = new MainForm();
            _form.FormClosed += (s, e) => { _form = null; };
            _form.Show();        // modeless : NX reste interactif (contrôle en session avant d'enregistrer)
            _form.Activate();
        }

        private static void CloseExisting()
        {
            try
            {
                var open = Application.OpenForms;
                for (int i = open.Count - 1; i >= 0; i--)
                {
                    var f = open[i];
                    if (f != null && f.GetType().FullName == "GirderArrangements.MainForm")
                        try { f.Close(); } catch { /* best effort */ }
                }
            }
            catch { /* best effort */ }
        }
    }
}
