using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace GirderArrangements
{
    /// <summary>
    /// Fenêtre de sélection des arcs modifiés à enregistrer. Cases à cochées (tout coché par défaut),
    /// boutons Enregistrer / Annuler. « Enregistrer » demande confirmation puis n'écrit QUE les arcs
    /// cochés (pièce de travail uniquement, via <see cref="ArrangementGenerator.SaveArcs"/>).
    /// </summary>
    public sealed class SaveArcsDialog : Form
    {
        private static readonly Color Navy = Color.FromArgb(33, 43, 64);
        private static readonly Color Accent = Color.FromArgb(0, 114, 198);
        private static readonly Color FormBg = Color.FromArgb(249, 250, 252);

        private readonly IReadOnlyList<ModifiedArc> _arcs;
        private readonly ArrangementGenerator _gen;
        private CheckedListBox _list;
        private Label _status;
        private Button _btnSave, _btnCancel;

        public SaveArcsDialog(IReadOnlyList<ModifiedArc> arcs, ArrangementGenerator gen)
        {
            _arcs = arcs ?? new List<ModifiedArc>();
            _gen = gen;
            Build();
        }

        private void Build()
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(12), BackColor = FormBg };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // titre
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // liste
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // statut
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));  // boutons

            root.Controls.Add(new Label
            {
                Text = "Arcs modifiés — coche ceux à enregistrer (pièce de travail uniquement) :",
                AutoSize = true, ForeColor = Navy, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 8)
            }, 0, 0);

            _list = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, IntegralHeight = false, Margin = new Padding(0, 0, 0, 8) };
            foreach (var a in _arcs)
            {
                var suffix = a.Created + a.Updated > 0 ? $"   ({a.Created} créé(s), {a.Updated} MAJ)" : "";
                _list.Items.Add(a.Name + suffix, true);
            }
            root.Controls.Add(_list, 0, 1);

            _status = new Label { Text = _arcs.Count + " arc(s) modifié(s).", AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 6), ForeColor = Color.FromArgb(40, 90, 40), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            root.Controls.Add(_status, 0, 2);

            var buttons = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Margin = new Padding(0) };
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            _btnSave = new Button { Text = "Enregistrer", Dock = DockStyle.Fill, Height = 36, FlatStyle = FlatStyle.Flat, BackColor = Accent, ForeColor = Color.White, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand, Margin = new Padding(3, 0, 3, 0), UseVisualStyleBackColor = false };
            _btnSave.FlatAppearance.BorderSize = 0;
            _btnSave.Click += OnSave;
            _btnCancel = new Button { Text = "Annuler", Dock = DockStyle.Fill, Height = 36, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand, Margin = new Padding(3, 0, 3, 0) };
            _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            buttons.Controls.Add(_btnSave, 0, 0);
            buttons.Controls.Add(_btnCancel, 1, 0);
            root.Controls.Add(buttons, 0, 3);

            Controls.Add(root);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 9F);
            BackColor = FormBg;
            ClientSize = new Size(460, 380);
            MinimumSize = new Size(380, 280);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Enregistrer les arcs modifiés";
        }

        private List<ModifiedArc> Checked()
        {
            var sel = new List<ModifiedArc>();
            foreach (int i in _list.CheckedIndices)
                if (i >= 0 && i < _arcs.Count) sel.Add(_arcs[i]);
            return sel;
        }

        private void OnSave(object sender, EventArgs e)
        {
            var sel = Checked();
            if (sel.Count == 0) { _status.Text = "Aucun arc coché."; _status.ForeColor = Color.FromArgb(170, 30, 30); return; }

            var names = string.Join("\n  • ", sel.Select(a => a.Name));
            var confirm = MessageBox.Show(this,
                $"Enregistrer UNIQUEMENT ces {sel.Count} arc(s) (pièce de travail uniquement, sans les sous-produits) ?\n\n  • {names}",
                "Confirmer l'enregistrement", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            if (confirm != DialogResult.OK) return;

            try
            {
                Cursor = Cursors.WaitCursor;
                _btnSave.Enabled = false; _btnCancel.Enabled = false;
                int ok = _gen.SaveArcs(sel.Select(a => a.Part));
                _status.Text = ok + "/" + sel.Count + " arc(s) enregistré(s).";
                _status.ForeColor = Color.FromArgb(40, 90, 40);
                MessageBox.Show(this, ok + " arc(s) enregistré(s) dans Teamcenter.", "Enregistrement",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                _status.Text = "Échec : " + ex.Message;
                _status.ForeColor = Color.FromArgb(170, 30, 30);
                _btnSave.Enabled = true; _btnCancel.Enabled = true;
            }
            finally { Cursor = Cursors.Default; }
        }
    }
}
