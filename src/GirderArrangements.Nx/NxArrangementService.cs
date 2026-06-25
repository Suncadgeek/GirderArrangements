using System.Collections.Generic;
using NXOpen;
using GirderArrangements.Core;
using Assemblies = NXOpen.Assemblies;

namespace GirderArrangements.Nx
{
    /// <summary>
    /// Crée / met à jour les ARRANGEMENTS d'assemblage de l'arc et y applique la SUPPRESSION par
    /// arrangement (désactivation des composants hors poutre). API NXOpen :
    ///  - ComponentAssembly.Arrangements.Create(template, name)
    ///  - ComponentAssembly.SuppressComponents(comps, arrangements[]) / UnsuppressComponents(...)
    ///
    /// NOTE : la suppression cible des composants pouvant être imbriqués dans les sous-ensembles
    /// (_AIMANTS / _CAV) ; la doc Siemens indique que SuppressComponents accepte des composants « de
    /// différents niveaux et sous-assemblages ». À valider en conditions réelles (cf. README, risque #1).
    /// </summary>
    public sealed class NxArrangementService
    {
        private readonly Assemblies.ComponentAssembly _ca;
        private readonly IBuildLog _log;

        public NxArrangementService(Part arcPart, IBuildLog log)
        {
            _ca = arcPart.ComponentAssembly;
            _log = log ?? NullBuildLog.Instance;
        }

        /// <summary>Arrangement actif courant (à restaurer en fin de traitement).</summary>
        public Assemblies.Arrangement ActiveArrangement
        {
            get { return _ca.ActiveArrangement; }
            set { if (value != null) _ca.ActiveArrangement = value; }
        }

        /// <summary>Trouve un arrangement par nom (insensible à la casse), ou null.</summary>
        public Assemblies.Arrangement Find(string name)
        {
            foreach (Assemblies.Arrangement a in _ca.Arrangements)
                if (string.Equals(a.Name, name, System.StringComparison.OrdinalIgnoreCase))
                    return a;
            return null;
        }

        /// <summary>
        /// Trouve l'arrangement de nom <paramref name="name"/>, ou le crée (cloné de l'arrangement actif).
        /// <paramref name="created"/> indique s'il vient d'être créé.
        /// </summary>
        public Assemblies.Arrangement FindOrCreate(string name, out bool created)
        {
            var existing = Find(name);
            if (existing != null) { created = false; return existing; }

            var template = _ca.ActiveArrangement;
            var arr = _ca.Arrangements.Create(template, name);
            created = true;
            _log.Info("Arrangement créé : « " + name + " ».");
            return arr;
        }

        /// <summary>
        /// Applique l'appartenance dans l'arrangement : on désuspend d'abord tout (remise à plat pour
        /// une MAJ idempotente), puis on supprime les non-membres. Les membres restent actifs.
        /// </summary>
        public void ApplyMembership(Assemblies.Arrangement arr,
            IList<Assemblies.Component> members, IList<Assemblies.Component> nonMembers)
        {
            var arrs = new[] { arr };

            var all = new List<Assemblies.Component>();
            if (members != null) all.AddRange(members);
            if (nonMembers != null) all.AddRange(nonMembers);
            if (all.Count > 0)
                SafeUnsuppress(all.ToArray(), arrs);

            if (nonMembers != null && nonMembers.Count > 0)
                SafeSuppress(AsArray(nonMembers), arrs);

            _log.Info($"Arrangement « {arr.Name} » : {Count(members)} membre(s) actif(s), "
                + $"{Count(nonMembers)} composant(s) supprimé(s).");
        }

        private void SafeSuppress(Assemblies.Component[] comps, Assemblies.Arrangement[] arrs)
        {
            try
            {
                var err = _ca.SuppressComponents(comps, arrs);
                if (err != null) err.Dispose();
            }
            catch (System.Exception ex) { _log.Warn("Suppression partielle : " + ex.Message); }
        }

        private void SafeUnsuppress(Assemblies.Component[] comps, Assemblies.Arrangement[] arrs)
        {
            try
            {
                var err = _ca.UnsuppressComponents(comps, arrs);
                if (err != null) err.Dispose();
            }
            catch (System.Exception ex) { _log.Warn("Désuppression partielle : " + ex.Message); }
        }

        private static Assemblies.Component[] AsArray(IList<Assemblies.Component> list)
        {
            var arr = new Assemblies.Component[list.Count];
            list.CopyTo(arr, 0);
            return arr;
        }

        private static int Count(IList<Assemblies.Component> list) => list != null ? list.Count : 0;
    }
}
