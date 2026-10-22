using System;
using System.Drawing;
using System.Windows.Forms;
using GeglSharp;
using PaintDotNet;
using PaintDotNet.Effects;

namespace BlaymsGeglPlugin
{
    [PluginSupportInfo(typeof(GeglSharpPluginInfo))]
    internal sealed class GeglDitherEffect : GeglBitmapEffectBase<DitherConfigToken>
    {
        private static readonly DitherMethod[] MethodChoices =
        {
            DitherMethod.None,
            DitherMethod.FloydSteinberg,
            DitherMethod.Bayer,
            DitherMethod.Random,
            DitherMethod.RandomCovariant,
            DitherMethod.ArithmeticAdd,
            DitherMethod.ArithmeticAddCovariant,
            DitherMethod.ArithmeticXor,
            DitherMethod.ArithmeticXorCovariant,
            DitherMethod.BlueNoise,
            DitherMethod.BlueNoiseCovariant
        };

        public GeglDitherEffect() : base("Dither", GeglEffectIcons.Dither, "GEGL", new DitherConfigToken()) { }

        protected override DitherConfigToken CloneToken(DitherConfigToken token) => new DitherConfigToken(token);
        protected override IEffectConfigForm CreateConfigForm(DitherConfigToken settings) => new GeglDitherConfigForm(this, settings);

        protected override byte[] ProcessPixels(byte[] pixels, int width, int height, DitherConfigToken settings)
        {
            return Gegl.Dither(pixels, width, height, new DitherSettings
            {
                Method = (DitherMethod)settings.Method,
                RedLevels = settings.RedLevels,
                GreenLevels = settings.GreenLevels,
                BlueLevels = settings.BlueLevels,
                AlphaLevels = settings.AlphaLevels,
                Seed = (uint)settings.Seed
            });
        }

        internal static DitherMethod[] GetMethodChoices()
        {
            var result = new DitherMethod[MethodChoices.Length];
            Array.Copy(MethodChoices, result, MethodChoices.Length);
            return result;
        }
    }

    internal sealed class GeglDitherConfigForm : GeglPresetConfigFormBase<DitherConfigToken>
    {
        private static readonly string[] MethodLabels =
        {
            "None",
            "Floyd-Steinberg",
            "Bayer",
            "Random",
            "Random Covariant",
            "Arithmetic Add",
            "Arithmetic Add Covariant",
            "Arithmetic XOR",
            "Arithmetic XOR Covariant",
            "Blue Noise",
            "Blue Noise Covariant"
        };

        private readonly DitherMethod[] _methods;
        private readonly ComboBox _method;
        private readonly NumericUpDown _redLevels, _greenLevels, _blueLevels, _alphaLevels, _seed;

        public GeglDitherConfigForm(GeglDitherEffect effect, DitherConfigToken settings)
            : base("GEGL Dither", "Dither", effect, settings, 600, 520)
        {
            _methods = GeglDitherEffect.GetMethodChoices();

            _method = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Margin = new Padding(0, 2, 0, 2) };
            foreach (var label in MethodLabels) _method.Items.Add(label);

            _redLevels = CreateNumeric(2, 65536);
            _greenLevels = CreateNumeric(2, 65536);
            _blueLevels = CreateNumeric(2, 65536);
            _alphaLevels = CreateNumeric(2, 65536);
            _seed = CreateNumeric(0, int.MaxValue);

            TableLayoutPanel group = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 1, RowCount = 2 };
            group.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            group.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            group.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            Label titleLbl = new Label { Text = "Dither settings", Font = new Font(Font, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 0, 0, 8) };

            TableLayoutPanel table = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 2, RowCount = 6 };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            AddRow(table, 0, "Dithering", _method);
            AddRow(table, 1, "Red levels", _redLevels);
            AddRow(table, 2, "Green levels", _greenLevels);
            AddRow(table, 3, "Blue levels", _blueLevels);
            AddRow(table, 4, "Alpha levels", _alphaLevels);
            AddRow(table, 5, "Random seed", _seed);

            group.Controls.Add(titleLbl, 0, 0);
            group.Controls.Add(table, 0, 1);

            _method.SelectedIndexChanged += SettingChanged;
            _redLevels.ValueChanged += SettingChanged;
            _greenLevels.ValueChanged += SettingChanged;
            _blueLevels.ValueChanged += SettingChanged;
            _alphaLevels.ValueChanged += SettingChanged;
            _seed.ValueChanged += SettingChanged;

            CompleteInitialization(group);
        }

        protected override DitherConfigToken CloneToken(DitherConfigToken token) => new DitherConfigToken(token);
        protected override DitherConfigToken CastToken(EffectConfigToken token) => new DitherConfigToken((DitherConfigToken)token);

        protected override void CopyToken(DitherConfigToken src, DitherConfigToken dest)
        {
            dest.Method = src.Method; dest.RedLevels = src.RedLevels; dest.GreenLevels = src.GreenLevels;
            dest.BlueLevels = src.BlueLevels; dest.AlphaLevels = src.AlphaLevels; dest.Seed = src.Seed;
        }

        protected override void ApplyTokenToControls(DitherConfigToken token)
        {
            int methodIndex = 0;
            for (int i = 0; i < _methods.Length; i++) if ((int)_methods[i] == token.Method) { methodIndex = i; break; }

            _method.SelectedIndex = methodIndex;
            _redLevels.Value = Clamp(token.RedLevels, _redLevels.Minimum, _redLevels.Maximum);
            _greenLevels.Value = Clamp(token.GreenLevels, _greenLevels.Minimum, _greenLevels.Maximum);
            _blueLevels.Value = Clamp(token.BlueLevels, _blueLevels.Minimum, _blueLevels.Maximum);
            _alphaLevels.Value = Clamp(token.AlphaLevels, _alphaLevels.Minimum, _alphaLevels.Maximum);
            _seed.Value = Clamp(token.Seed, _seed.Minimum, _seed.Maximum);
        }

        protected override DitherConfigToken CreateTokenFromControls()
        {
            int methodIndex = _method.SelectedIndex;
            if (methodIndex < 0 || methodIndex >= _methods.Length) methodIndex = 0;

            return new DitherConfigToken
            {
                Method = (int)_methods[methodIndex],
                RedLevels = (int)_redLevels.Value,
                GreenLevels = (int)_greenLevels.Value,
                BlueLevels = (int)_blueLevels.Value,
                AlphaLevels = (int)_alphaLevels.Value,
                Seed = (int)_seed.Value
            };
        }

        protected override GeglPresetRecord CreatePresetRecord(DitherConfigToken token, string name, DateTime timestamp)
        {
            return new GeglPresetRecord
            {
                EffectId = "Dither",
                Name = name,
                Timestamp = timestamp,
                Method = token.Method,
                RedLevels = token.RedLevels,
                GreenLevels = token.GreenLevels,
                BlueLevels = token.BlueLevels,
                AlphaLevels = token.AlphaLevels,
                Seed = token.Seed
            };
        }

        protected override DitherConfigToken CreateTokenFromPreset(GeglPresetRecord record)
        {
            return new DitherConfigToken
            {
                Method = record.Method,
                RedLevels = record.RedLevels,
                GreenLevels = record.GreenLevels,
                BlueLevels = record.BlueLevels,
                AlphaLevels = record.AlphaLevels,
                Seed = record.Seed
            };
        }

        private void SettingChanged(object sender, EventArgs e) => NotifySettingsChanged();

        private static NumericUpDown CreateNumeric(int min, int max) => new NumericUpDown { Minimum = min, Maximum = max, Dock = DockStyle.Fill, ThousandsSeparator = true, Margin = new Padding(0, 2, 0, 2) };

        private static void AddRow(TableLayoutPanel table, int row, string labelText, Control control)
        {
            table.Controls.Add(new Label { Text = labelText, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = true, Margin = new Padding(0, 4, 8, 4) }, 0, row);
            table.Controls.Add(control, 1, row);
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        private static decimal Clamp(int value, decimal min, decimal max) => value < min ? min : (value > max ? max : value);
    }
}