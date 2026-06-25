using System;
using System.IO;
using System.Text.Json;

namespace GirderArrangements.Core.Config
{
    /// <summary>
    /// Persistance de <see cref="ArrangementConfig"/> en JSON. Emplacement par défaut :
    /// Documents\GirderArrangements_config.json. Repris du patron CheckDistances / 3DBuilder.
    /// </summary>
    public sealed class ConfigStore
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        public string Path { get; }

        public ConfigStore(string path = null)
        {
            Path = string.IsNullOrWhiteSpace(path) ? DefaultPath() : path;
        }

        public static string DefaultPath()
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return System.IO.Path.Combine(docs, "GirderArrangements_config.json");
        }

        /// <summary>Charge la config ; renvoie une config par défaut si le fichier est absent ou illisible.</summary>
        public ArrangementConfig Load()
        {
            try
            {
                if (!File.Exists(Path)) return new ArrangementConfig();
                var json = File.ReadAllText(Path);
                return JsonSerializer.Deserialize<ArrangementConfig>(json, Options) ?? new ArrangementConfig();
            }
            catch
            {
                return new ArrangementConfig();
            }
        }

        public void Save(ArrangementConfig config)
        {
            var json = JsonSerializer.Serialize(config ?? new ArrangementConfig(), Options);
            var dir = System.IO.Path.GetDirectoryName(Path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(Path, json);
        }
    }
}
