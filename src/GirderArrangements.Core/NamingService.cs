using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GirderArrangements.Core
{
    /// <summary>
    /// Classement des composants d'un arc par convention de nom et dérivation du nom d'arrangement à
    /// partir d'une poutre. Pur, testable, sans dépendance NXOpen.
    ///
    /// Structure d'un arc (cf. CheckDistances / 3DBuilder) : squelette « *_SQL », ensemble aimants
    /// « *_AIMANTS », chambre à vide « *_CAV_EQUIPEE », et plusieurs poutres « *POUTRE*_NN »
    /// (ex. « ARC12_POUTRE MOYENNE_01 », « ARC12_POUTRE COURTE_02 »).
    /// </summary>
    public sealed class NamingService
    {
        public const string EnsembleToken = "AIMANTS";
        public const string SkeletonToken = "_SQL";
        public const string ChamberToken = "CAV_EQUIPEE";
        public const string PoutreToken = "POUTRE";

        private static bool ContainsToken(string name, string token)
            => !string.IsNullOrEmpty(name)
               && name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;

        /// <summary>Squelette de section (« *_SQL ») — exclu des poutres et masqué par défaut.</summary>
        public bool IsSkeleton(string name) => ContainsToken(name, SkeletonToken);

        /// <summary>Ensemble aimants (« *_AIMANTS ») — conteneur des aimants feuilles.</summary>
        public bool IsMagnetEnsemble(string name) => ContainsToken(name, EnsembleToken);

        /// <summary>Chambre à vide (« *_CAV_EQUIPEE ») — conteneur des éléments cav.</summary>
        public bool IsVacuumChamber(string name) => ContainsToken(name, ChamberToken);

        /// <summary>Poutre (« *POUTRE*_NN ») = enfant direct de l'arc portant un numéro de fin.</summary>
        public bool IsPoutre(string name) => ContainsToken(name, PoutreToken);

        // Arc : nom contenant « ARC », ou suffixe « -AR » / « _AR ». Repris de CheckDistances.
        private static readonly Regex ArcRx = new Regex(@"[-_]AR($|[-_/.])", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>Vrai si le nom désigne un arc (niveau portant poutres / aimants / chambre).</summary>
        public bool IsArc(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return name.IndexOf("ARC", StringComparison.OrdinalIgnoreCase) >= 0 || ArcRx.IsMatch(name);
        }

        // Numéro de poutre = suffixe « _NN » en fin de nom (ex. « ..._01 » → 1, « ..._03 » → 3).
        private static readonly Regex NumRx =
            new Regex(@"_(\d{1,3})\s*$", RegexOptions.Compiled);

        /// <summary>Extrait le numéro de poutre depuis le suffixe « _NN ». False si absent.</summary>
        public bool TryExtractPoutreNumber(string name, out int number)
        {
            number = 0;
            if (string.IsNullOrWhiteSpace(name)) return false;
            var m = NumRx.Match(name.Trim());
            if (!m.Success) return false;
            return int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number);
        }

        /// <summary>
        /// Nom d'arrangement dérivé d'une poutre : « POUTRE 01 » (numéro sur au moins 2 chiffres).
        /// </summary>
        public string ArrangementName(int number)
            => "POUTRE " + number.ToString("00", CultureInfo.InvariantCulture);

        /// <summary>Nom d'arrangement directement depuis le nom de la poutre, ou null si pas de numéro.</summary>
        public string ArrangementNameFor(string poutreName)
            => TryExtractPoutreNumber(poutreName, out var n) ? ArrangementName(n) : null;
    }
}
