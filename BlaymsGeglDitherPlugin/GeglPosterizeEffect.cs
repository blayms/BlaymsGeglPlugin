using System;
using System.Drawing;
using System.Windows.Forms;
using GeglSharp;
using PaintDotNet;
using PaintDotNet.Effects;

namespace BlaymsGeglPlugin
{
    [PluginSupportInfo(typeof(GeglSharpPluginInfo))]
    internal sealed class GeglPosterizeEffect : GeglBitmapEffectBase<PosterizeConfigToken>
    {
        public GeglPosterizeEffect() : base("Posterize", GeglEffectIcons.Posterize, "GEGL", new PosterizeConfigToken()) { }

        protected override PosterizeConfigToken CloneToken(PosterizeConfigToken token) => new PosterizeConfigToken(token);
        protected override IEffectConfigForm CreateConfigForm(PosterizeConfigToken settings) => new GeglPosterizeConfigForm(this, settings);
        protected override byte[] ProcessPixels(byte[] pixels, int width, int height, PosterizeConfigToken settings) => Gegl.Posterize(pixels, width, height, settings.Levels);
    }

    internal sealed class GeglPosterizeConfigForm : GeglPresetConfigFormBase<PosterizeConfigToken>
    {
        private readonly NumericUpDown _levels;

        public GeglPosterizeConfigForm(GeglPosterizeEffect effect, PosterizeConfigToken settings)
            : base("GEGL Posterize", "Posterize", effect, settings, 580, 340)
        {
            _levels = new NumericUpDown { Minimum = 1, Maximum = 64, Value = 8, Dock = DockStyle.Fill, Margin = new Padding(0, 2, 0, 2) };
            _levels.ValueChanged += SettingChanged;

            TableLayoutPanel group = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 1, RowCount = 2 };
            group.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            group.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            group.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            Label titleLbl = new Label { Text = "Posterize settings", Font = new Font(Font, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 0, 0, 8) };

            TableLayoutPanel table = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 2, RowCount = 1 };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            table.Controls.Add(new Label { Text = "Levels per RGB", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = true, Margin = new Padding(0, 4, 8, 4) }, 0, 0);
            table.Controls.Add(_levels, 1, 0);
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            group.Controls.Add(titleLbl, 0, 0);
            group.Controls.Add(table, 0, 1);

            CompleteInitialization(group);
        }

        protected override PosterizeConfigToken CloneToken(PosterizeConfigToken token) => new PosterizeConfigToken(token);
        protected override PosterizeConfigToken CastToken(EffectConfigToken token) => new PosterizeConfigToken((PosterizeConfigToken)token);
        protected override void CopyToken(PosterizeConfigToken source, PosterizeConfigToken destination) => destination.Levels = source.Levels;

        protected override void ApplyTokenToControls(PosterizeConfigToken token)
        {
            if (token.Levels < _levels.Minimum) _levels.Value = _levels.Minimum;
            else if (token.Levels > _levels.Maximum) _levels.Value = _levels.Maximum;
            else _levels.Value = token.Levels;
        }

        protected override PosterizeConfigToken CreateTokenFromControls() => new PosterizeConfigToken { Levels = Decimal.ToInt32(_levels.Value) };

        protected override GeglPresetRecord CreatePresetRecord(PosterizeConfigToken token, string name, DateTime timestamp)
        {
            return new GeglPresetRecord { EffectId = "Posterize", Name = name, Timestamp = timestamp, Levels = token.Levels };
        }

        protected override PosterizeConfigToken CreateTokenFromPreset(GeglPresetRecord record) => new PosterizeConfigToken { Levels = record.Levels };

        private void SettingChanged(object sender, EventArgs e) => NotifySettingsChanged();
    }
}