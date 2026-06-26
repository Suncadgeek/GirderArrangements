using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using GirderArrangements.Core.Config;
using GirderArrangements.Nx;

namespace GirderArrangements
{
    /// <summary>
    /// Fenêtre unique : réf TC d'arc (ou arc courant) → Charger → cocher les poutres → Générer.
    /// Crée/met à jour un arrangement par poutre (« POUTRE NN ») masquant tout sauf la poutre et ses
    /// aimants/cav. UI redimensionnable, journal riche.
    /// </summary>
    public partial class MainForm : Form
    {
        private enum PickMode { SingleArc, Ring }

        private readonly UiBuildLog _log;
        private readonly ConfigStore _store = new ConfigStore();
        private ArrangementConfig _config;
        private ArrangementGenerator _gen;
        private List<BeamInfo> _beams = new List<BeamInfo>();
        private List<RingCell> _cells = new List<RingCell>();
        private PickMode _mode = PickMode.SingleArc;
        private bool _cancel;
        private bool _busy;

        private static readonly Color Green = Color.FromArgb(40, 90, 40);
        private static readonly Color DarkOrange = Color.FromArgb(180, 95, 0);
        private static readonly Color Red = Color.FromArgb(170, 30, 30);

        public MainForm()
        {
            InitializeComponent();
            LoadIcon();
            _log = new UiBuildLog(txtLog, progressBar);
            _config = _store.Load();
            ApplyConfigToUi();
            UpdateBeamScopeEnabled();
        }

        private void LoadIcon()
        {
            try
            {
                using (var s = typeof(MainForm).Assembly.GetManifestResourceStream("GirderArrangements.Resources.ico.ico"))
                {
                    if (s == null) return;
                    Icon = new System.Drawing.Icon(s);
                    if (picIcon != null) picIcon.Image = Icon.ToBitmap();
                }
            }
            catch { /* icône optionnelle */ }
        }

        // ----------------------------------------------------------------- config <-> UI

        private void ApplyConfigToUi()
        {
            txtArcTc.Text = _config.ArcTcRef;
            txtRingTc.Text = _config.RingTcRef;
            txtMarginXY.Text = _config.MarginXYmm.ToString("0.###", CultureInfo.InvariantCulture);
            txtMarginZ.Text = _config.MarginZmm.ToString("0.###", CultureInfo.InvariantCulture);
            chkIncludeSkeleton.Checked = _config.IncludeSkeleton;
            chkOverwrite.Checked = _config.OverwriteExisting;
            cboLogLevel.SelectedIndex = Math.Max(0, Math.Min(2, _config.LogLevel));
            _log.MinLevel = cboLogLevel.SelectedIndex;
        }

        private ArrangementConfig ReadConfigFromUi()
        {
            _config.ArcTcRef = (txtArcTc.Text ?? "").Trim();
            _config.RingTcRef = (txtRingTc.Text ?? "").Trim();
            _config.MarginXYmm = ParseDouble(txtMarginXY.Text, 700.0);
            _config.MarginZmm = ParseDouble(txtMarginZ.Text, 0.0);
            _config.IncludeSkeleton = chkIncludeSkeleton.Checked;
            _config.OverwriteExisting = chkOverwrite.Checked;
            _config.LogLevel = cboLogLevel.SelectedIndex;
            return _config;
        }

        private static double ParseDouble(string s, double fallback)
        {
            s = (s ?? "").Trim().Replace(',', '.');
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : fallback;
        }

        // ----------------------------------------------------------------- périmètre poutres

        private void OnAllBeamsChanged(object sender, EventArgs e) => UpdateBeamScopeEnabled();

        private void UpdateBeamScopeEnabled()
        {
            bool pick = !chkAllBeams.Checked && lstBeams.Items.Count > 0;
            lstBeams.Enabled = pick;
            btnCheckAll.Enabled = pick;
            btnUncheckAll.Enabled = pick;
        }

        private void OnCheckAll(object sender, EventArgs e)
        {
            for (int i = 0; i < lstBeams.Items.Count; i++) lstBeams.SetItemChecked(i, true);
        }

        private void OnUncheckAll(object sender, EventArgs e)
        {
            for (int i = 0; i < lstBeams.Items.Count; i++) lstBeams.SetItemChecked(i, false);
        }

        // ----------------------------------------------------------------- actions

        private void OnLoadArc(object sender, EventArgs e)
        {
            var cfg = ReadConfigFromUi();
            if (string.IsNullOrWhiteSpace(cfg.ArcTcRef))
            {
                SetStatus("Renseigne une réf TC d'arc, ou clique « Arc courant ».", Red);
                return;
            }
            DoLoad(cfg);
        }

        private void OnUseCurrent(object sender, EventArgs e)
        {
            var cfg = ReadConfigFromUi();
            cfg.ArcTcRef = "";          // forcer l'usage de l'arc ouvert
            txtArcTc.Text = "";
            DoLoad(cfg);
        }

        private void DoLoad(ArrangementConfig cfg)
        {
            if (_busy) return;
            SetBusy(true);
            _log.Clear();
            SetStatus("Chargement de l'arc et détection des poutres…", DarkOrange);
            try
            {
                EnsureGen();
                var res = _gen.Load(cfg);

                _mode = PickMode.SingleArc;
                lblPickHead.Text = "Poutres → arrangement :";
                chkAllBeams.Text = "Toutes les poutres";
                _beams = res.Beams;
                _cells = new List<RingCell>();
                lstBeams.Items.Clear();
                foreach (var b in _beams)
                    lstBeams.Items.Add(b.PoutreName + "   →   " + b.ArrangementName, true);

                UpdateBeamScopeEnabled();
                btnGenerate.Enabled = _beams.Count > 0;
                progressBar.Value = progressBar.Maximum;
                SetStatus($"Arc « {res.ArcName} » : {_beams.Count} poutre(s) détectée(s)."
                    + (_beams.Count > 0 ? " Choisis les poutres puis « Générer les arrangements »." : ""),
                    _beams.Count > 0 ? Green : Red);
            }
            catch (Exception ex)
            {
                _log.Error("Chargement : " + ex.Message);
                SetStatus("Échec du chargement — voir le journal.", Red);
            }
            finally { SetBusy(false); }
        }

        private void OnListRing(object sender, EventArgs e) => ListRing(forceRefresh: false);

        private void OnRefreshRing(object sender, EventArgs e) => ListRing(forceRefresh: true);

        private void ListRing(bool forceRefresh)
        {
            if (_busy) return;
            var cfg = ReadConfigFromUi();
            if (string.IsNullOrWhiteSpace(cfg.RingTcRef)) { SetStatus("Renseigne la réf TC de l'anneau.", Red); return; }

            SetBusy(true);
            _log.Clear();
            SetStatus(forceRefresh
                ? "Rafraîchissement des arcs depuis Teamcenter…"
                : "Lecture de la structure de l'anneau (cellules / arcs)…", DarkOrange);
            try
            {
                EnsureGen();
                var res = _gen.ListRing(cfg, forceRefresh);

                _mode = PickMode.Ring;
                lblPickHead.Text = "Cellules à traiter :";
                chkAllBeams.Text = "Toutes les cellules";
                _cells = res.Cells;
                _beams = new List<BeamInfo>();
                lstBeams.Items.Clear();
                foreach (var c in _cells)
                    lstBeams.Items.Add($"{c.Name}   ({c.Arcs.Count} arc(s))", true);

                UpdateBeamScopeEnabled();
                int arcCount = _cells.Sum(c => c.Arcs.Count);
                btnGenerate.Enabled = arcCount > 0;
                progressBar.Value = progressBar.Maximum;
                SetStatus($"{_cells.Count} cellule(s), {arcCount} arc(s). Choisis les cellules puis « Générer les arrangements ».",
                    arcCount > 0 ? Green : Red);
            }
            catch (Exception ex)
            {
                _log.Error("Anneau : " + ex.Message);
                SetStatus("Échec de la lecture de l'anneau — voir le journal.", Red);
            }
            finally { SetBusy(false); }
        }

        private void OnGenerate(object sender, EventArgs e)
        {
            if (_busy || _gen == null) return;
            var cfg = ReadConfigFromUi();

            SetBusy(true);
            _cancel = false;
            btnCancel.Enabled = true;
            SetStatus("Génération des arrangements…", DarkOrange);
            try
            {
                Func<bool> cancel = () => { Application.DoEvents(); return _cancel; };
                RunResult res;

                if (_mode == PickMode.Ring)
                {
                    var arcs = SelectedRingArcs();
                    if (arcs.Count == 0) { SetStatus("Aucune cellule/arc sélectionné.", Red); return; }
                    res = _gen.RunRing(cfg, arcs, cancel);
                }
                else
                {
                    var selected = SelectedBeamIndexes();
                    if (!chkAllBeams.Checked && selected.Count == 0) { SetStatus("Aucune poutre sélectionnée.", Red); return; }
                    res = _gen.Run(cfg, chkAllBeams.Checked ? null : selected, cancel);
                }

                lblSummary.Text = $"Arcs : {res.ArcsProcessed}    Arrangements : {res.BeamsProcessed}    "
                    + $"Créés : {res.Created}    MAJ : {res.Updated}    Items rattachés : {res.ItemsAssigned}"
                    + (res.ItemsUnassigned > 0 ? $"    Hors boîte : {res.ItemsUnassigned}" : "");
                lblSummary.ForeColor = res.ItemsUnassigned > 0 ? DarkOrange : Green;

                var tail = _mode == PickMode.Ring
                    ? "Arcs laissés ouverts — enregistre-les dans Teamcenter."
                    : "Pense à enregistrer l'arc dans Teamcenter.";
                SetStatus(res.Cancelled
                    ? "Génération ANNULÉE — arrangements partiels."
                    : $"Terminé — {res.ArcsProcessed} arc(s), {res.BeamsProcessed} arrangement(s) "
                      + $"({res.Created} créé(s), {res.Updated} MAJ). " + tail,
                    res.Cancelled ? DarkOrange : Green);
            }
            catch (Exception ex)
            {
                _log.Error("Génération : " + ex.Message);
                SetStatus("Échec de la génération — voir le journal.", Red);
            }
            finally { btnCancel.Enabled = false; SetBusy(false); }
        }

        private List<RingArc> SelectedRingArcs()
        {
            var arcs = new List<RingArc>();
            IEnumerable<int> cellIdx = chkAllBeams.Checked
                ? Enumerable.Range(0, _cells.Count)
                : lstBeams.CheckedIndices.Cast<int>();
            foreach (int i in cellIdx)
                if (i >= 0 && i < _cells.Count) arcs.AddRange(_cells[i].Arcs);
            return arcs;
        }

        private List<int> SelectedBeamIndexes()
        {
            var sel = new List<int>();
            if (chkAllBeams.Checked)
            {
                foreach (var b in _beams) sel.Add(b.Index);
                return sel;
            }
            foreach (int i in lstBeams.CheckedIndices)
                if (i >= 0 && i < _beams.Count) sel.Add(_beams[i].Index);
            return sel;
        }

        private void EnsureGen()
        {
            if (_gen == null) _gen = new ArrangementGenerator(_log);
        }

        private void OnSaveArcs(object sender, EventArgs e)
        {
            if (_busy || _gen == null) return;
            if (_gen.ModifiedArcs.Count == 0) { SetStatus("Aucun arc modifié à enregistrer.", DarkOrange); return; }
            using (var dlg = new SaveArcsDialog(_gen.ModifiedArcs, _gen))
            {
                dlg.ShowDialog(this);
            }
            SetBusy(false); // rafraîchit l'état des boutons
        }

        private void OnCancel(object sender, EventArgs e) => _cancel = true;

        private void OnLogLevelChanged(object sender, EventArgs e) => _log.MinLevel = cboLogLevel.SelectedIndex;

        private void OnCopyLog(object sender, EventArgs e)
        {
            try
            {
                var text = _log.FullText();
                if (string.IsNullOrEmpty(text)) { lblStatus.Text = "Journal vide."; return; }
                Clipboard.SetText(text);
                lblStatus.Text = "Journal copié dans le presse-papiers.";
            }
            catch (Exception ex) { lblStatus.Text = "Copie impossible : " + ex.Message; }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            try { _store.Save(ReadConfigFromUi()); } catch { /* best effort */ }
        }

        // ----------------------------------------------------------------- état UI

        private void SetStatus(string text, Color color)
        {
            lblStatus.Text = text;
            lblStatus.ForeColor = color;
            Application.DoEvents();
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;
            btnLoad.Enabled = !busy;
            btnUseCurrent.Enabled = !busy;
            btnListRing.Enabled = !busy;
            btnRefreshRing.Enabled = !busy;
            bool hasWork = _mode == PickMode.Ring ? (_cells != null && _cells.Count > 0) : (_beams != null && _beams.Count > 0);
            btnGenerate.Enabled = !busy && hasWork;
            btnSave.Enabled = !busy && _gen != null && _gen.ModifiedArcs.Count > 0;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
            Application.DoEvents();
        }
    }
}
