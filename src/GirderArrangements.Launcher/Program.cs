using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NXOpen;

namespace GirderArrangements.Launcher
{
    /// <summary>
    /// Point d'entrée chargé par NX (« GirderArrangements.dll »). Charge le code applicatif PAR OCTETS
    /// pour permettre le HOT-RELOAD : les DLL GirderArrangements.App/Core/Nx ne sont jamais verrouillées
    /// → on les rebuild sans fermer NX, et chaque lancement reprend le nouveau code. Patron CheckDistances.
    ///
    /// Un cache PAR EXÉCUTION garantit qu'une exécution n'utilise QUE son propre jeu d'assemblies
    /// GirderArrangements.* fraîchement chargés (pas de conflit de types entre exécutions).
    /// </summary>
    public static class Program
    {
        private static string _dir;
        private static Dictionary<string, Assembly> _runCache;

        public static void Main(string[] args)
        {
            _dir = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            _runCache = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

            Environment.SetEnvironmentVariable("GIRDERARR_ADDIN_DIR", _dir);
            AppDomain.CurrentDomain.AssemblyResolve += Resolve;

            var app = LoadFresh("GirderArrangements.App");
            var entry = app.GetType("GirderArrangements.AppEntry", throwOnError: true);
            entry.GetMethod("Run", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
        }

        /// <summary>Décharge l'image dès la fin de l'exécution.</summary>
        public static int GetUnloadOption(string dummy)
        {
            return (int)Session.LibraryUnloadOption.Immediately;
        }

        private static Assembly Resolve(object sender, ResolveEventArgs e)
        {
            var name = new AssemblyName(e.Name).Name;

            // GirderArrangements.* : toujours le jeu frais de l'exécution courante (hot-reload, non verrouillé).
            if (name.StartsWith("GirderArrangements", StringComparison.OrdinalIgnoreCase))
                return LoadFresh(name);

            // Dépendances stables (System.Text.Json…) : réutilise si déjà chargé, sinon LoadFrom.
            var already = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == name);
            if (already != null) return already;

            var dep = Path.Combine(_dir, name + ".dll");
            return File.Exists(dep) ? Assembly.LoadFrom(dep) : null;
        }

        private static Assembly LoadFresh(string name)
        {
            Assembly cached;
            if (_runCache.TryGetValue(name, out cached)) return cached;

            var path = Path.Combine(_dir, name + ".dll");
            if (!File.Exists(path))
                throw new FileNotFoundException("DLL de l'add-in introuvable : " + path);

            var asm = Assembly.Load(File.ReadAllBytes(path)); // ReadAllBytes => fichier non verrouillé
            _runCache[name] = asm;
            return asm;
        }
    }
}
