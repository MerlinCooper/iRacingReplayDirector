using iRacingReplayDirector.Support;
using iRacingSDK.Support;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace iRacingDirector.Plugin.Tester
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var myDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var logFile = Path.Combine(myDocumentsPath, "iRacingDirector.Plugin.Tester.log");
            LogListener.ToFile(logFile);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.Run(new Main());
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            if (ex != null)
            {
                Trace.WriteLine(ex.Message, "ERROR");
                Trace.WriteLine(ex.StackTrace, "ERROR");
            }
            else
            {
                Trace.WriteLine("An unknown error occured. {0}, {1}".F(e.ExceptionObject.GetType().Name, e.ExceptionObject.ToString()));
            }
        }

        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            MessageBox.Show("An error occured.  Details have been logged.\n\n{0}".F(e.Exception.Message), "Error");
            Trace.WriteLine(e.Exception.Message, "ERROR");
            Trace.WriteLine(e.Exception.StackTrace, "ERROR");
        }
    }
}
