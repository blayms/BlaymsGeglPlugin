using System;
using System.Drawing;
using System.Windows.Forms;
using PaintDotNet.Effects;

namespace BlaymsGeglPlugin
{
    internal abstract class GeglPresetConfigFormBase<TToken> : EffectConfigForm2
        where TToken : EffectConfigToken
    {
        private readonly string _effectId;
        private readonly TableLayoutPanel _root;
        private readonly ComboBox _historyCombo;
        private readonly ComboBox _savedCombo;
        private readonly Button _saveButton;
        private readonly Button _renameButton;
        private readonly Button _deleteButton;
        private TToken _token;
        private IEffect _effect;
        private bool _loadingControls;

        public event EventHandler TokenChanged;

        protected GeglPresetConfigFormBase(string title, string effectId, IEffect effect, TToken initialToken, int width = 580, int height = 400)
        {
            _effectId = effectId;
            _effect = effect;
            _token = initialToken != null ? CloneToken(initialToken) : Activator.CreateInstance<TToken>();

            // Force standard DPI scaling
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);

            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Font = SystemFonts.MessageBoxFont;

            ClientSize = new Size(width, height);

            _root = new TableLayoutPanel();
            _root.Dock = DockStyle.Fill;
            _root.Padding = new Padding(12);
            _root.ColumnCount = 1;
            _root.RowCount = 5;
            _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f));
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 0: Settings control
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 1: History
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 2: Saved
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 3: Preset buttons
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 4: OK / Cancel buttons
            Controls.Add(_root);

            // ---------- History Row ----------
            TableLayoutPanel historyPanel = new TableLayoutPanel();
            historyPanel.Dock = DockStyle.Top;
            historyPanel.AutoSize = true;
            historyPanel.ColumnCount = 2;
            historyPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            historyPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            historyPanel.RowCount = 1;
            historyPanel.Margin = new Padding(0, 6, 0, 4);

            _historyCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Margin = new Padding(0) };
            _historyCombo.SelectedIndexChanged += HistoryCombo_SelectedIndexChanged;

            historyPanel.Controls.Add(CreateLabel("History"), 0, 0);
            historyPanel.Controls.Add(_historyCombo, 1, 0);
            _root.Controls.Add(historyPanel, 0, 1);

            // ---------- Saved Row ----------
            TableLayoutPanel savedPanel = new TableLayoutPanel();
            savedPanel.Dock = DockStyle.Top;
            savedPanel.AutoSize = true;
            savedPanel.ColumnCount = 2;
            savedPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            savedPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            savedPanel.RowCount = 1;
            savedPanel.Margin = new Padding(0, 4, 0, 4);

            _savedCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Margin = new Padding(0) };
            _savedCombo.SelectedIndexChanged += SavedCombo_SelectedIndexChanged;

            savedPanel.Controls.Add(CreateLabel("Saved"), 0, 0);
            savedPanel.Controls.Add(_savedCombo, 1, 0);
            _root.Controls.Add(savedPanel, 0, 2);

            // ---------- Preset Action Buttons Row ----------
            TableLayoutPanel buttonsRow = new TableLayoutPanel();
            buttonsRow.Dock = DockStyle.Top;
            buttonsRow.AutoSize = true;
            buttonsRow.ColumnCount = 2;
            buttonsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            buttonsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            buttonsRow.RowCount = 1;
            buttonsRow.Margin = new Padding(0, 0, 0, 12);

            FlowLayoutPanel presetButtonsPanel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false, Margin = new Padding(0) };

            _saveButton = new Button { Text = "Save...", AutoSize = true, MinimumSize = new Size(78, 26), FlatStyle = FlatStyle.System, Margin = new Padding(0, 0, 6, 0) };
            _saveButton.Click += SaveButton_Click;

            _renameButton = new Button { Text = "Rename", AutoSize = true, MinimumSize = new Size(78, 26), FlatStyle = FlatStyle.System, Enabled = false, Margin = new Padding(0, 0, 6, 0) };
            _renameButton.Click += RenameButton_Click;

            _deleteButton = new Button { Text = "Delete", AutoSize = true, MinimumSize = new Size(70, 26), FlatStyle = FlatStyle.System, Enabled = false, Margin = new Padding(0) };
            _deleteButton.Click += DeleteButton_Click;

            presetButtonsPanel.Controls.Add(_saveButton);
            presetButtonsPanel.Controls.Add(_renameButton);
            presetButtonsPanel.Controls.Add(_deleteButton);

            buttonsRow.Controls.Add(new Panel { AutoSize = true }, 0, 0);
            buttonsRow.Controls.Add(presetButtonsPanel, 1, 0);
            _root.Controls.Add(buttonsRow, 0, 3);

            // ---------- OK / Cancel Buttons ----------
            Button okButton = new Button { Text = "OK", AutoSize = true, MinimumSize = new Size(84, 28), FlatStyle = FlatStyle.System, DialogResult = DialogResult.OK, Margin = new Padding(6, 0, 0, 0) };
            Button cancelButton = new Button { Text = "Cancel", AutoSize = true, MinimumSize = new Size(84, 28), FlatStyle = FlatStyle.System, DialogResult = DialogResult.Cancel, Margin = new Padding(0) };

            FlowLayoutPanel dialogButtonsPanel = new FlowLayoutPanel();
            dialogButtonsPanel.AutoSize = true;
            dialogButtonsPanel.Dock = DockStyle.Right;
            dialogButtonsPanel.FlowDirection = FlowDirection.RightToLeft;
            dialogButtonsPanel.WrapContents = false;
            dialogButtonsPanel.Margin = new Padding(0);
            dialogButtonsPanel.Controls.Add(cancelButton);
            dialogButtonsPanel.Controls.Add(okButton);
            _root.Controls.Add(dialogButtonsPanel, 0, 4);

            AcceptButton = okButton;
            CancelButton = cancelButton;
        }

        // ---------- EffectConfigForm2 Hooks ----------

        protected override EffectConfigToken OnCreateInitialToken() => Activator.CreateInstance<TToken>();

        protected override void OnUpdateDialogFromToken(EffectConfigToken token)
        {
            if (token == null) return;
            _token = CastToken(token);
            _loadingControls = true;
            ApplyTokenToControls(_token);
            _loadingControls = false;
        }

        protected override void OnUpdateTokenFromDialog(EffectConfigToken token)
        {
            var typed = token as TToken;
            if (typed != null) CopyToken(CreateTokenFromControls(), typed);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            if (DialogResult == DialogResult.OK)
            {
                NotifySettingsChanged();
                GeglPresetStore.AddHistory(CreatePresetRecord(_token, String.Empty, DateTime.Now));
            }
        }

        // ---------- Abstract Hooks ----------
        protected abstract TToken CloneToken(TToken token);
        protected abstract TToken CastToken(EffectConfigToken token);
        protected abstract void ApplyTokenToControls(TToken token);
        protected abstract TToken CreateTokenFromControls();
        protected abstract GeglPresetRecord CreatePresetRecord(TToken token, string name, DateTime timestamp);
        protected abstract TToken CreateTokenFromPreset(GeglPresetRecord record);
        protected abstract void CopyToken(TToken source, TToken destination);

        // ---------- Protected Helpers ----------
        protected void CompleteInitialization(Control settingsControl)
        {
            settingsControl.Dock = DockStyle.Top;
            settingsControl.Margin = new Padding(0, 0, 0, 12);
            _root.Controls.Add(settingsControl, 0, 0);
            _root.SetRow(settingsControl, 0);

            _loadingControls = true;
            ApplyTokenToControls(_token);
            _loadingControls = false;

            ReloadPresetLists();
        }

        protected void NotifySettingsChanged()
        {
            if (_loadingControls) return;
            _token = CreateTokenFromControls();
            UpdateTokenFromDialog();
            TokenChanged?.Invoke(this, EventArgs.Empty);
        }

        // ---------- Preset UI Handlers ----------
        private void HistoryCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingControls) return;
            var record = _historyCombo.SelectedItem as GeglPresetRecord;
            if (record == null) return;
            LoadRecord(record);
            // Removed _historyCombo.SelectedIndex = -1 so the selected history label stays visible
        }

        private void SavedCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool hasSavedPreset = _savedCombo.SelectedItem is GeglPresetRecord;
            _deleteButton.Enabled = hasSavedPreset;
            _renameButton.Enabled = hasSavedPreset;
            if (_loadingControls) return;
            var record = _savedCombo.SelectedItem as GeglPresetRecord;
            if (record != null) LoadRecord(record);
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            NotifySettingsChanged();
            using (var dialog = new GeglPresetNameDialog())
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                string name = dialog.PresetName.Trim();
                if (name.Length == 0) return;

                GeglPresetStore.SavePreset(CreatePresetRecord(_token, name, DateTime.Now));
                ReloadPresetLists();

                for (int i = 0; i < _savedCombo.Items.Count; i++)
                {
                    if (String.Equals((_savedCombo.Items[i] as GeglPresetRecord)?.Name, name, StringComparison.CurrentCultureIgnoreCase))
                    {
                        _savedCombo.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            var record = _savedCombo.SelectedItem as GeglPresetRecord;
            if (record == null) return;

            if (MessageBox.Show(this, "Delete preset \"" + record.Name + "\"?", "Delete Preset", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                GeglPresetStore.DeletePreset(_effectId, record.Name);
                ReloadPresetLists();
            }
        }

        private void RenameButton_Click(object sender, EventArgs e)
        {
            var record = _savedCombo.SelectedItem as GeglPresetRecord;
            if (record == null) return;

            using (var dialog = new GeglPresetNameDialog(record.Name))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                string newName = dialog.PresetName.Trim();
                if (newName.Length == 0) return;

                GeglPresetStore.RenamePreset(_effectId, record.Name, newName);
                ReloadPresetLists();

                for (int i = 0; i < _savedCombo.Items.Count; i++)
                {
                    if (String.Equals((_savedCombo.Items[i] as GeglPresetRecord)?.Name, newName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        _savedCombo.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void LoadRecord(GeglPresetRecord record)
        {
            _token = CreateTokenFromPreset(record);
            _loadingControls = true;
            ApplyTokenToControls(_token);
            _loadingControls = false;
            UpdateTokenFromDialog();
            TokenChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ReloadPresetLists()
        {
            _loadingControls = true;
            _historyCombo.BeginUpdate();
            _historyCombo.Items.Clear();
            foreach (var h in GeglPresetStore.GetHistory(_effectId)) _historyCombo.Items.Add(h);
            _historyCombo.SelectedIndex = -1;
            _historyCombo.EndUpdate();

            _savedCombo.BeginUpdate();
            _savedCombo.Items.Clear();
            foreach (var s in GeglPresetStore.GetSaved(_effectId)) _savedCombo.Items.Add(s);
            _savedCombo.SelectedIndex = -1;
            _savedCombo.EndUpdate();

            _deleteButton.Enabled = false;
            _renameButton.Enabled = false;
            _loadingControls = false;
        }

        private static Label CreateLabel(string text)
        {
            return new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false, Height = 24, Margin = new Padding(0, 0, 8, 0) };
        }
    }

    internal sealed class GeglPresetNameDialog : Form
    {
        private readonly TextBox _nameTextBox;
        public string PresetName => _nameTextBox.Text;

        public GeglPresetNameDialog() : this(String.Empty) { }

        public GeglPresetNameDialog(string initialName)
        {
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);

            Text = "Save Preset";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(380, 130);
            Font = SystemFonts.MessageBoxFont;

            var label = new Label { Text = "Preset Name:", Location = new Point(14, 14), AutoSize = true };
            _nameTextBox = new TextBox { Location = new Point(14, 36), Width = 352, MaxLength = 120, Text = initialName ?? String.Empty };

            var saveButton = new Button { Text = "Save", AutoSize = true, MinimumSize = new Size(84, 28), FlatStyle = FlatStyle.System, DialogResult = DialogResult.OK, Location = new Point(194, 78) };
            var cancelButton = new Button { Text = "Cancel", AutoSize = true, MinimumSize = new Size(84, 28), FlatStyle = FlatStyle.System, DialogResult = DialogResult.Cancel, Location = new Point(282, 78) };

            Controls.Add(label);
            Controls.Add(_nameTextBox);
            Controls.Add(saveButton);
            Controls.Add(cancelButton);
            AcceptButton = saveButton;
            CancelButton = cancelButton;

            Shown += (sender, e) =>
            {
                _nameTextBox.Focus();
                _nameTextBox.SelectAll();
            };
        }
    }
}