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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace iRacingReplayDirector
{
    public partial class PluginSettings : Form
    {
        private Action onSave;
        readonly PluginProxySettings[] pluginSettings;
        readonly Settings settings;

        public PluginSettings(Settings settings, PluginProxySettings[] pluginSettings)
        {
            this.settings = settings;
            this.pluginSettings = pluginSettings;
            InitializeComponent();
            AddPanelComponents();
        }

        void AddPanelComponents()
        {
            var f = new GeneralSettingFields(panel, helpText, () => this.ActiveControl, Settings.Default);

            var storedSettings = Settings.Default.PluginStoredSettings == null ? new List<PluginProxySettings>() : Settings.Default.PluginStoredSettings.ToList();

            foreach (var s in this.pluginSettings)
            {
                var storedValue = storedSettings.FirstOrDefault(ss => ss.Name == s.Name);
                if (storedValue == null)
                    storedSettings.Add(storedValue = new PluginProxySettings { Name = s.Name });

                if (s.Type == typeof(bool))
                    f.AddCheckboxField(s.Name, s.Description, i => (bool)(storedValue.Value ?? s.Value), (i, v) => storedValue.Value = v);
                else
                    f.AddStringField(s.Name, s.Description, i => (string)(storedValue.Value ?? s.Value), (i, v) => storedValue.Value = v);
            }

            Settings.Default.PluginStoredSettings = storedSettings.ToArray();

            this.onSave = f.OnSave;
        }

        void okButton_Click(object sender, EventArgs e)
        {
            this.onSave();
        }

        void OnFocus(object sender, EventArgs e)
        {
            helpText.Text = "";
            if (this.ActiveControl.Tag != null)
                helpText.Text = this.ActiveControl.Tag.ToString();
        }

        private void PluginSettings_Load(object sender, EventArgs e)
        {

        }
    }
}