using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using GirderArrangements.Core;

namespace GirderArrangements
{
    /// <summary>
    /// Implémentation WinForms d'<see cref="IBuildLog"/> : journalise dans la zone de texte et pilote
    /// la barre de progression. Mémorise toutes les entrées et ne réaffiche que celles dont le niveau
    /// est ≥ <see cref="MinLevel"/>. DoEvents garde l'UI réactive (NX est mono-thread STA → le bouton
    /// Annuler est traité via le pompage d'événements). Patron repris de CheckDistances / 3DBuilder.
    /// </summary>
    public sealed class UiBuildLog : IBuildLog
    {
        public const int LevelInfo = 0;
        public const int LevelWarn = 1;
        public const int LevelError = 2;

        private static readonly string[] Tags = { "INFO", "AVERT.", "ERREUR" };

        private readonly TextBox _log;
        private readonly ProgressBar _bar;
        private readonly List<KeyValuePair<int, string>> _entries = new List<KeyValuePair<int, string>>();
        private int _minLevel = LevelInfo;

        public UiBuildLog(TextBox log, ProgressBar bar)
        {
            _log = log;
            _bar = bar;
        }

        /// <summary>Niveau minimal affiché (0=Info, 1=Avert., 2=Erreur). Réaffiche en cas de changement.</summary>
        public int MinLevel
        {
            get { return _minLevel; }
            set { _minLevel = value; Rerender(); }
        }

        public void Info(string message) => Add(LevelInfo, message);
        public void Warn(string message) => Add(LevelWarn, message);
        public void Error(string message) => Add(LevelError, message);

        public void Progress(int current, int total)
        {
            if (_bar == null) return;
            int v = total > 0 ? (int)(1000L * current / total) : 0;
            _bar.Value = Math.Max(0, Math.Min(_bar.Maximum, v));
            Application.DoEvents();
        }

        /// <summary>Journal complet (toutes entrées, indépendamment du niveau filtré) pour le presse-papiers.</summary>
        public string FullText()
        {
            var sb = new StringBuilder();
            foreach (var e in _entries)
                sb.Append("[" + Tags[e.Key] + "] ").Append(e.Value).Append(Environment.NewLine);
            return sb.ToString();
        }

        public void Clear()
        {
            _entries.Clear();
            if (_log != null) _log.Clear();
        }

        private void Add(int level, string message)
        {
            _entries.Add(new KeyValuePair<int, string>(level, message));
            if (_log != null && level >= _minLevel)
            {
                _log.AppendText("[" + Tags[level] + "] " + message + Environment.NewLine);
                Application.DoEvents();
            }
        }

        private void Rerender()
        {
            if (_log == null) return;
            var sb = new StringBuilder();
            foreach (var e in _entries)
                if (e.Key >= _minLevel)
                    sb.Append("[" + Tags[e.Key] + "] ").Append(e.Value).Append(Environment.NewLine);
            _log.Text = sb.ToString();
            _log.SelectionStart = _log.TextLength;
            _log.ScrollToCaret();
        }
    }
}
