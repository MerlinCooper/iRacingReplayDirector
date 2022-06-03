// This file is part of iRacingReplayDirector.
//
// Copyright 2014 Dean Netherton (discontinued)
// https://github.com/vipoo/iRacingReplayDirector.net
//
// Copyright 2021 Merlin Cooper 
// https://github.com/MerlinCooper/iRacingReplayDirector
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

using System.Collections.Generic;
using System.Linq;
using WK.Libraries.HotkeyListenerNS;
using System.Windows.Forms;

namespace iRacingReplayDirector
{
    public partial class Settings
    {
        public List<string> PreferredDrivers
        {
            get
            {
                return PreferredDriverNames.Split(new [] { ',', ';' }).Select(name => name.Trim().ToLower()).ToList();
            }
        }

        //public Hotkey hotKeyStopStart = new Hotkey(Keys.Alt, Keys.F9);      //default HotKey to Stop/Start recording
        //public Hotkey hotKeyStopStart = new Hotkey(Settings.Default.strhotKeyStopStart);      //default HotKey to Stop/Start recording

        //public Hotkey hotKeyPauseResume = new Hotkey(Keys.Alt, Keys.F12);   //default HotKey to Pause/Resume recording
        //public Hotkey hotKeyPauseResume = new Hotkey(Settings.Default.strHotKeyPauseResume);   //default HotKey to Pause/Resume recording
    }
}
