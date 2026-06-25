namespace GirderArrangements.Core
{
    /// <summary>
    /// Canal de journalisation / progression, indépendant de l'UI. Implémenté côté application
    /// (zone de texte WinForms + barre de progression). Repris du patron CheckDistances / 3DBuilder.
    /// </summary>
    public interface IBuildLog
    {
        void Info(string message);
        void Warn(string message);
        void Error(string message);

        /// <summary>Avancement courant / total (pour une barre de progression).</summary>
        void Progress(int current, int total);
    }

    /// <summary>Implémentation neutre (ignore tout) utile pour les tests ou l'absence d'UI.</summary>
    public sealed class NullBuildLog : IBuildLog
    {
        public static readonly NullBuildLog Instance = new NullBuildLog();
        public void Info(string message) { }
        public void Warn(string message) { }
        public void Error(string message) { }
        public void Progress(int current, int total) { }
    }
}
