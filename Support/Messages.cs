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
//

using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace iRacingReplayDirector.Support
{
    public class MyListener : TraceListener
    {
        public const string INFO = "INFO";
        public const string ERROR = "ERROR";

        TextBox textBox;
        SynchronizationContext context;

        public MyListener(TextBox textBox)
        {
            context = SynchronizationContext.Current;
            this.textBox = textBox;
        }

        public override void Write(string message, string category)
        {
            if (category == INFO || category == ERROR)
                WriteInfo(message);
        }

        public override void WriteLine(string message, string category)
        {
            if (category == INFO || category == ERROR)
                WriteInfoLine(message);
        }

        public void WriteInfo(string message)
        {
            context.Post(ignore =>
            {
                textBox.Text = textBox.Text + message;
                textBox.SelectionStart = textBox.Text.Length;
                textBox.ScrollToCaret();
            }, null);
        }

        public void WriteInfoLine(string message)
        {
            WriteInfo(message);
            WriteInfo("\r\n");
        }

        public override void Write(string message)
        {
            if (message.StartsWith(INFO) || message.StartsWith(ERROR))
                WriteInfo(message);
        }

        public override void WriteLine(string message)
        {
            if (message.StartsWith(INFO) || message.StartsWith(ERROR))
                WriteInfoLine(message);
        }
    }
}
