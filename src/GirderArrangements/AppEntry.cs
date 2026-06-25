using System.Windows.Forms;

namespace GirderArrangements
{
    /// <summary>
    /// Point d'entrée du code applicatif, invoqué par RÉFLEXION depuis le launcher
    /// (GirderArrangements.Launcher → « GirderArrangements.dll »). Ne pas renommer : le launcher cherche
    /// « GirderArrangements.AppEntry.Run ».
    ///
    /// MODAL (ShowDialog) : le launcher décharge l'image dès le retour de Run (hot-reload). Une fenêtre
    /// modeless planterait (code déchargé sous elle).
    /// </summary>
    public static class AppEntry
    {
        public static void Run()
        {
            using (var form = new MainForm())
            {
                form.ShowDialog();
            }
        }
    }
}
