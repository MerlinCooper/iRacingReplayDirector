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
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace iRacingReplayDirector
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            if (!Settings.Default.NewSettings)
                CopyFromOldSettings(Settings.Default);
            else
                MakePortable(Settings.Default);


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;


            //if( AwsKeys.HaveKeys)
            //    using (var awsLogListener = new AwsLogListener())
            //    {
            //        Trace.Listeners.Add(awsLogListener);
            //        TraceInfo.WriteLine("Application Start");
            //        Application.Run(new Main());
            //        TraceInfo.WriteLine("Application End");
            //    }
            //else
            //    Application.Run(new Main());

            Application.Run(new Main());
        }

        internal static void MakePortable(Settings settings)
        {
            var pp = settings.Providers.OfType<IracingReplayDirectorProvider>().First();

            foreach (SettingsProperty p in settings.Properties)
                if (p.Provider.GetType() != typeof(IAVMSettingsProvider))
                    p.Provider = pp;
            settings.Reload();
        }

        private static void CopyFromOldSettings(Settings settings)
        {
            var pp = settings.Providers.OfType<IracingReplayDirectorProvider>().First();

            var currentValues = new Dictionary<string, object>();

            foreach (SettingsProperty p in settings.Properties)
            {
                currentValues[p.Name] = settings[p.Name];
                TraceDebug.WriteLine("Capturing settings -- {0}: {1}", p.Name, currentValues[p.Name]);
                p.Provider = pp;
            }
            settings.Reload();

            foreach (var kv in currentValues)
            {
                settings[kv.Key] = kv.Value;
            }
            settings.NewSettings = true;
            settings.Save();
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            if (ex != null)
            {
                TraceError.WriteLine(ex.Message);
                TraceError.WriteLine(ex.StackTrace);
            }
            else
            {
                Trace.WriteLine("An unknown error occured. {0}, {1}".F(e.ExceptionObject.GetType().Name, e.ExceptionObject.ToString()));
            }
        }

        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            MessageBox.Show("An error occured.  Details have been logged.\n\n{0}".F(e.Exception.Message), "Error");
            TraceError.WriteLine(e.Exception.Message);
            TraceError.WriteLine(e.Exception.StackTrace);
        }
    }
}
