// This file is part of iRacingReplayDirector.
//
// Copyright 2014 Dean Netherton
// https://github.com/vipoo/iRacingReplayDirector.net
//
// iRacingReplayDirector is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// iRacingReplayDirector is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with iRacingReplayDirector.  If not, see <http://www.gnu.org/licenses/>.

using iRacingSDK.Support;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Globalization;

namespace iRacingReplayDirector
{
    public class GeneralSettingFields
    {
        readonly Panel panel;
        readonly Settings settings;
        readonly Label helpText;
        readonly Func<Control> activeControl;

        public GeneralSettingFields(Panel panel, Label helpText, Func<Control> activeControl, Settings settings)
        {
            this.panel = panel;
            this.helpText = helpText;
            this.activeControl = activeControl;
            this.settings = settings;
        }
        
        int nextTabIndex = 1;
        int nextRowPosition = 14;
        List<Action> onSave = new List<Action>();

        public Action OnSave
        {
            get
            {
                return () =>
                {
                    foreach (var s in onSave)
                        s();

                    settings.Save();
                };
            }
        }

        internal void AddBlankRow()
        {
            nextRowPosition += 20;
        }

        internal void AddPasswordField(string caption, string description, Func<Settings, string> getter, Action<Settings, string> setter)
        {
            var textBox = AddStringField(caption, description, getter, setter);
            textBox.PasswordChar = '*';
        }

        internal TextBox AddStringField(string caption, string description, Func<Settings, string> getter, Action<Settings, string> setter)
        {
            return AddField(caption, description, getter(settings), tb => setter(settings, tb.Text));
        }

        internal void AddStringField(string caption, string description, string setting)
        {
            AddField(caption, description, settings[setting].ToString(), tb => settings[setting] = tb.Text);
        }

        internal void AddKeyPressField(string caption, string description)
        {
            if(caption.Contains("start"))
            {
                var textBox = AddField(caption, description, /*"ALT+F9"*/Settings.Default.strHotKeyStopStart, tb => { });
                textBox.ReadOnly = true;
            }else{
                var textBox = AddField(caption, description, /*"ALT+F10"*/Settings.Default.strHotKeyPauseResume, tb => { });
                textBox.ReadOnly = true;
            }
        }

        internal void AddNumberField(string caption, string description, string setting)
        {
            AddField(caption, description, settings[setting].ToString(), tb =>
            {
                var number = 0.0d;
                if (double.TryParse(tb.Text, out number))
                    settings[setting] = number;
            });
        }

        internal void AddIntField(string caption, string description, string setting)
        {
            AddField(caption, description, settings[setting].ToString(), tb =>
            {
                var number = 0;
                if (int.TryParse(tb.Text, out number))
                    settings[setting] = number;
            });
        }

        internal void AddTimeField(string caption, string description, string setting)
        {
            panel.Controls.Add(new Label
            {
                Location = new System.Drawing.Point(440, nextRowPosition),
                Size = new System.Drawing.Size(60, 16),
                TabIndex = nextTabIndex++,
                Text = "seconds"
            });

            // Create a NumberFormatInfo object and set some of its properties.
            NumberFormatInfo provider = new NumberFormatInfo();
            provider.NumberDecimalSeparator = ".";

            AddField(caption, description, ((TimeSpan)settings[setting]).TotalSeconds.ToString(provider), tb =>
            {
                var newSeconds = 0.0;

                if (double.TryParse(tb.Text, out newSeconds))
                {
                    if (string.Compare(setting, "BattleGap") == 0)
                    {
                        var newMilliSec = 1000 * Convert.ToDouble(tb.Text.ToString(), provider);
                        settings[setting] = TimeSpan.FromMilliseconds(newMilliSec);
                    }
                    else
                        settings[setting] = newSeconds.Seconds();
                }

            });
        }

        internal void AddMinuteField(string caption, string description, string setting)
        {
            panel.Controls.Add(new Label
            {
                Location = new System.Drawing.Point(440, nextRowPosition),
                Size = new System.Drawing.Size(60, 16),
                TabIndex = nextTabIndex++,
                Text = "Minutes"
            });

            var startValue = (int)((TimeSpan)settings[setting]).TotalMinutes;

            AddField(caption, description, startValue.ToString(), tb =>
            {
                var newMinutes = 0;
                if (int.TryParse(tb.Text, out newMinutes))
                    settings[setting] = newMinutes.Minutes();
            });
        }

        internal void AddCheckboxField(string caption, string description, Func<Settings, bool> getter, Action<Settings, bool> setter)
        {
            panel.Controls.Add(new Label
            {
                Location = new System.Drawing.Point(14, nextRowPosition),
                Size = new System.Drawing.Size(269, 16),
                TabIndex = nextTabIndex++,
                Text = caption
            });

            var checkBox = new CheckBox
            {
                AutoSize = true,
                Checked = getter(settings),
                Location = new System.Drawing.Point(301, nextRowPosition + 3),
                Size = new System.Drawing.Size(186, 22),
                TabIndex = nextTabIndex++,
                UseVisualStyleBackColor = true,
                Tag = description
            };

            checkBox.Enter += new EventHandler(this.OnFocus);

            onSave.Add(() => setter(settings, checkBox.Checked));

            panel.Controls.Add(checkBox);
            nextRowPosition += 28;
        }

        internal void AddPluginSelectorField()
        {
            const string setting = "PluginName";

            panel.Controls.Add(new Label
            {
                Location = new System.Drawing.Point(14, nextRowPosition),
                Size = new System.Drawing.Size(269, 16),
                TabIndex = nextTabIndex++,
                Text = "Overlay"
            });

            var comboBox = new ComboBox()
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FormattingEnabled = true,
                Location = new System.Drawing.Point(301, nextRowPosition + 3),
                Margin = new Padding(4, 5, 4, 5),
                Size = new System.Drawing.Size(186, 22),
                Tag = "Choose the style of overlay to be applied",
                TabIndex = nextTabIndex++
            };
            comboBox.Enter += new EventHandler(this.OnFocus);

            foreach (var name in PluginProxy.Names)
            {
                comboBox.Items.Add(name);
            }

            comboBox.SelectedItem = settings[setting];

            onSave.Add(() => settings[setting] = comboBox.SelectedItem);
            panel.Controls.Add(comboBox);
            nextRowPosition += 28;
        }

        internal void AddCheckboxField(string caption, string description, string setting)
        {
            AddCheckboxField(caption, description, (i) => (bool)settings[setting], (i, b) => settings[setting] = b);
        }

        internal TextBox AddField(string caption, string description, string value, Action<TextBox> onSave)
        {
            panel.Controls.Add(new Label
            {
                Location = new System.Drawing.Point(14, nextRowPosition),
                Size = new System.Drawing.Size(269, 16),
                TabIndex = nextTabIndex++,
                Text = caption
            });

            var textBox = new TextBox
            {
                Location = new System.Drawing.Point(301, nextRowPosition - 3),
                Size = new System.Drawing.Size(134, 22),
                TabIndex = nextTabIndex++,
                Tag = description,
                Text = value
            };
            textBox.Enter += new EventHandler(this.OnFocus);
            panel.Controls.Add(textBox);

            this.onSave.Add(() => onSave(textBox));
            nextRowPosition += 28;

            return textBox;
        }

        internal void OnFocus(object sender, EventArgs e)
        {
            helpText.Text = "";
            if (activeControl().Tag != null)
                helpText.Text = activeControl().Tag.ToString();
        }
    }
}
