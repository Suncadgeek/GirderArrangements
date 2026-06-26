using System;
using System.Drawing;
using System.Windows.Forms;

namespace GirderArrangements
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private static readonly Color Navy = Color.FromArgb(33, 43, 64);
        private static readonly Color Accent = Color.FromArgb(0, 114, 198);
        private static readonly Color AccentHover = Color.FromArgb(0, 95, 168);
        private static readonly Color SecondaryBg = Color.FromArgb(238, 240, 244);
        private static readonly Color SecondaryFg = Color.FromArgb(40, 48, 64);
        private static readonly Color BorderCol = Color.FromArgb(210, 215, 222);
        private static readonly Color FormBg = Color.FromArgb(249, 250, 252);

        private PictureBox picIcon;
        private TextBox txtArcTc;
        private Button btnLoad, btnUseCurrent;
        private TextBox txtRingTc;
        private Button btnListRing, btnRefreshRing;
        private Label lblPickHead;
        private CheckBox chkAllBeams;
        private CheckedListBox lstBeams;
        private Button btnCheckAll, btnUncheckAll;
        private TextBox txtMarginXY, txtMarginZ;
        private CheckBox chkIncludeSkeleton, chkOverwrite;
        private Button btnGenerate, btnCancel, btnSave;
        private Label lblStatus, lblSummary;
        private ComboBox cboLogLevel;
        private Button btnCopyLog;
        private ProgressBar progressBar, progressBarArc;
        private TextBox txtLog;

        private static Label Lbl(string text)
            => new Label { Text = text, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 7, 3, 1) };

        private static GroupBox GroupFill(string title)
            => new GroupBox { Text = title, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(10, 6, 10, 10), ForeColor = Navy, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };

        private static GroupBox Group(string title)
            => new GroupBox { Text = title, Dock = DockStyle.Fill, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(10, 6, 10, 10), ForeColor = Navy, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };

        private static TableLayoutPanel Grid(int cols)
            => new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = cols, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular), ForeColor = Color.FromArgb(45, 45, 48) };

        private static FlowLayoutPanel HRow()
            => new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false, Margin = new Padding(0), FlowDirection = FlowDirection.LeftToRight, Dock = DockStyle.Fill };

        private static Button Btn(string text, EventHandler onClick, bool primary = false, bool enabled = true)
        {
            var b = new Button
            {
                Text = text, Dock = DockStyle.Fill, Height = 38, Enabled = enabled,
                Margin = new Padding(3, 0, 3, 0), FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand, UseVisualStyleBackColor = false
            };
            b.FlatAppearance.BorderSize = primary ? 0 : 1;
            b.FlatAppearance.BorderColor = BorderCol;
            if (primary) { b.BackColor = Accent; b.ForeColor = Color.White; b.FlatAppearance.MouseOverBackColor = AccentHover; }
            else { b.BackColor = SecondaryBg; b.ForeColor = SecondaryFg; b.FlatAppearance.MouseOverBackColor = Color.FromArgb(225, 229, 236); }
            b.Click += onClick;
            return b;
        }

        private static Button SmallBtn(string text, EventHandler onClick, bool enabled = false)
        {
            var b = new Button { Text = text, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Enabled = enabled, Margin = new Padding(0, 3, 4, 0), Padding = new Padding(4, 1, 4, 1) };
            b.Click += onClick;
            return b;
        }

        private static Button SmallBtnWide(string text, EventHandler onClick, bool primary = false)
        {
            var b = new Button { Text = text, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(6, 2, 3, 6), Padding = new Padding(10, 3, 10, 3), FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand, UseVisualStyleBackColor = false,
                BackColor = primary ? Accent : SecondaryBg, ForeColor = primary ? Color.White : SecondaryFg };
            b.FlatAppearance.BorderColor = BorderCol;
            if (primary) b.FlatAppearance.MouseOverBackColor = AccentHover;
            b.Click += onClick;
            return b;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(10), BackColor = FormBg };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // ---- Bannière ----
            var banner = new Panel { Dock = DockStyle.Fill, BackColor = Navy, Margin = new Padding(0, 0, 0, 10) };
            var bg = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Navy, Padding = new Padding(14, 6, 12, 6) };
            bg.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bg.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bg.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            // Icône centrée verticalement (Anchor=None) face au bloc de titre, lui aussi centré.
            this.picIcon = new PictureBox { Width = 42, Height = 42, SizeMode = PictureBoxSizeMode.Zoom, Margin = new Padding(0, 0, 12, 0), Anchor = AnchorStyles.None };
            bg.Controls.Add(this.picIcon, 0, 0);
            var titleStack = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Anchor = AnchorStyles.Left, Margin = new Padding(0), BackColor = Navy };
            titleStack.Controls.Add(new Label { Text = "GirderArrangements", AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold), Margin = new Padding(0, 0, 0, 0) });
            titleStack.Controls.Add(new Label { Text = "Un arrangement par poutre dans l'arc (aimants + cav posés dessus)", AutoSize = true, ForeColor = Color.FromArgb(176, 187, 204), Font = new Font("Segoe UI", 8.5F), Margin = new Padding(1, 1, 0, 0) });
            bg.Controls.Add(titleStack, 1, 0);
            banner.Controls.Add(bg);

            // ---- 1. Arc (mono-arc) ----
            var gbArc = Group("1 — Arc (réf TC)");
            var ga = Grid(2);
            ga.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            ga.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            ga.Controls.Add(Lbl("Réf TC de l'arc à traiter (vide = arc déjà ouvert)"), 0, 0);
            ga.SetColumnSpan(ga.GetControlFromPosition(0, 0), 2);
            this.txtArcTc = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(3, 2, 3, 6) };
            ga.Controls.Add(this.txtArcTc, 0, 1);
            var arcBtns = HRow();
            this.btnLoad = SmallBtnWide("Charger l'arc", this.OnLoadArc, primary: true);
            this.btnUseCurrent = SmallBtnWide("Arc courant", this.OnUseCurrent);
            arcBtns.Controls.Add(this.btnLoad);
            arcBtns.Controls.Add(this.btnUseCurrent);
            ga.Controls.Add(arcBtns, 1, 1);
            gbArc.Controls.Add(ga);

            // ---- 1bis. Anneau de stockage (balayage multi-arcs, structure seule) ----
            var gbRing = Group("Anneau de stockage — multi-arcs (sélection par cellules)");
            var gri = Grid(2);
            gri.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            gri.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            gri.Controls.Add(Lbl("Réf TC de l'anneau de stockage (structure seule — ne charge pas la maquette)"), 0, 0);
            gri.SetColumnSpan(gri.GetControlFromPosition(0, 0), 2);
            this.txtRingTc = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(3, 2, 3, 6) };
            gri.Controls.Add(this.txtRingTc, 0, 1);
            var ringBtns = HRow();
            this.btnListRing = SmallBtnWide("Lister les cellules", this.OnListRing);
            this.btnRefreshRing = SmallBtnWide("Rafraîchir", this.OnRefreshRing);
            ringBtns.Controls.Add(this.btnListRing);
            ringBtns.Controls.Add(this.btnRefreshRing);
            gri.Controls.Add(ringBtns, 1, 1);
            gbRing.Controls.Add(gri);

            // ---- 2. Poutres (élastique) ----
            var gbBeams = GroupFill("2 — Poutres détectées");
            var gp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Margin = new Padding(0),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular), ForeColor = Color.FromArgb(45, 45, 48) };
            gp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            gp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            gp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            gp.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            this.chkAllBeams = new CheckBox { Text = "Toutes les poutres", AutoSize = true, Checked = true, Margin = new Padding(3, 4, 3, 2) };
            this.chkAllBeams.CheckedChanged += this.OnAllBeamsChanged;
            var head = HRow();
            this.lblPickHead = new Label { Text = "Poutres → arrangement :", AutoSize = true, Margin = new Padding(3, 8, 12, 0) };
            head.Controls.Add(this.lblPickHead);
            this.btnCheckAll = SmallBtn("Tout cocher", this.OnCheckAll);
            this.btnUncheckAll = SmallBtn("Tout décocher", this.OnUncheckAll);
            head.Controls.Add(this.btnCheckAll);
            head.Controls.Add(this.btnUncheckAll);
            this.lstBeams = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, Enabled = false, IntegralHeight = false, MinimumSize = new Size(0, 130), Margin = new Padding(3, 2, 3, 4) };
            gp.Controls.Add(this.chkAllBeams, 0, 0);
            gp.Controls.Add(head, 0, 1);
            gp.Controls.Add(this.lstBeams, 0, 2);
            gbBeams.Controls.Add(gp);

            // ---- 3. Options ----
            var gbOpt = Group("3 — Boîte de sélection et options");
            var go = Grid(1);
            go.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            var mRow = HRow();
            mRow.Controls.Add(new Label { Text = "Débordement boîte (mm) — X/Y", AutoSize = true, Margin = new Padding(3, 6, 4, 3) });
            this.txtMarginXY = new TextBox { Width = 70, Text = "700", Margin = new Padding(3, 3, 12, 3) };
            mRow.Controls.Add(this.txtMarginXY);
            mRow.Controls.Add(new Label { Text = "Z (le long du faisceau)", AutoSize = true, Margin = new Padding(3, 6, 4, 3) });
            this.txtMarginZ = new TextBox { Width = 70, Text = "0", Margin = new Padding(3, 3, 3, 3) };
            mRow.Controls.Add(this.txtMarginZ);
            go.Controls.Add(mRow, 0, 0);
            var optRow = HRow();
            this.chkIncludeSkeleton = new CheckBox { Text = "Inclure le squelette", AutoSize = true, Margin = new Padding(3, 4, 16, 4) };
            this.chkOverwrite = new CheckBox { Text = "Écraser les arrangements existants", AutoSize = true, Checked = true, Margin = new Padding(3, 4, 3, 4) };
            optRow.Controls.Add(this.chkIncludeSkeleton);
            optRow.Controls.Add(this.chkOverwrite);
            go.Controls.Add(optRow, 0, 1);
            gbOpt.Controls.Add(go);

            // ---- Actions ----
            var actions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Margin = new Padding(0, 0, 0, 6) };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            this.btnGenerate = Btn("Générer les arrangements", this.OnGenerate, primary: true, enabled: false);
            this.btnCancel = Btn("Annuler", this.OnCancel, enabled: false);
            this.btnSave = Btn("Enregistrer les arcs…", this.OnSaveArcs, enabled: false);
            actions.Controls.Add(this.btnGenerate, 0, 0);
            actions.Controls.Add(this.btnCancel, 1, 0);
            actions.Controls.Add(this.btnSave, 2, 0);

            this.lblSummary = new Label { Text = "", AutoSize = true, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Margin = new Padding(3, 2, 3, 4) };
            this.lblStatus = new Label { Text = "Prêt. Saisis la réf TC d'un arc (ou ouvre-le dans NX) puis « Charger l'arc ».", AutoSize = true, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(40, 90, 40), Margin = new Padding(3, 2, 3, 8) };

            // ---- Journal (élastique) ----
            var gbJournal = GroupFill("Journal");
            var gj = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Margin = new Padding(0),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular), ForeColor = Color.FromArgb(45, 45, 48) };
            gj.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            gj.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            gj.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            gj.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var logRow = HRow();
            this.cboLogLevel = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170, Margin = new Padding(8, 3, 3, 3) };
            this.cboLogLevel.Items.AddRange(new object[] { "Info", "Avertissements", "Erreurs" });
            this.cboLogLevel.SelectedIndex = 0;
            this.cboLogLevel.SelectedIndexChanged += this.OnLogLevelChanged;
            logRow.Controls.Add(new Label { Text = "Niveau :", AutoSize = true, Margin = new Padding(3, 7, 3, 3) });
            logRow.Controls.Add(this.cboLogLevel);
            this.btnCopyLog = SmallBtn("Copier le journal", this.OnCopyLog, enabled: true);
            this.btnCopyLog.Margin = new Padding(12, 4, 3, 3);
            logRow.Controls.Add(this.btnCopyLog);
            // Deux barres : « Global » (arcs en mode anneau / l'arc en mono-arc) et « Arc courant »
            // (avancement de la création des arrangements, poutre par poutre, dans l'arc en cours).
            var prog = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0) };
            prog.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            prog.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            prog.Controls.Add(new Label { Text = "Global", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 5, 8, 3) }, 0, 0);
            this.progressBar = new ProgressBar { Dock = DockStyle.Fill, Height = 16, Maximum = 1000, Margin = new Padding(0, 4, 3, 3) };
            prog.Controls.Add(this.progressBar, 1, 0);
            prog.Controls.Add(new Label { Text = "Arc courant", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 5, 8, 3) }, 0, 1);
            this.progressBarArc = new ProgressBar { Dock = DockStyle.Fill, Height = 16, Maximum = 1000, Margin = new Padding(0, 3, 3, 4) };
            prog.Controls.Add(this.progressBarArc, 1, 1);
            this.txtLog = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = Color.FromArgb(250, 250, 252), Margin = new Padding(3, 2, 3, 2) };
            gj.Controls.Add(logRow, 0, 0);
            gj.Controls.Add(prog, 0, 1);
            gj.Controls.Add(this.txtLog, 0, 2);
            gbJournal.Controls.Add(gj);

            // ---- Assemblage racine ----
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));   // bannière
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // arc
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // anneau (à venir)
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 42));     // poutres (grandit)
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // options
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));    // actions
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // résumé
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // état
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 58));     // journal (grandit)
            root.Controls.Add(banner, 0, 0);
            root.Controls.Add(gbArc, 0, 1);
            root.Controls.Add(gbRing, 0, 2);
            root.Controls.Add(gbBeams, 0, 3);
            root.Controls.Add(gbOpt, 0, 4);
            root.Controls.Add(actions, 0, 5);
            root.Controls.Add(this.lblSummary, 0, 6);
            root.Controls.Add(this.lblStatus, 0, 7);
            root.Controls.Add(gbJournal, 0, 8);

            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.Font = new Font("Segoe UI", 9F);
            this.BackColor = FormBg;
            this.ClientSize = new Size(660, 820);
            this.MinimumSize = new Size(560, 720);
            this.Controls.Add(root);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true; this.MinimizeBox = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "GirderArrangements — arrangements par poutre";
            this.FormClosing += this.OnFormClosing;

            this.ResumeLayout(false);
        }
    }
}
