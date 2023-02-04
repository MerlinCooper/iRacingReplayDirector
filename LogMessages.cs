using System.Windows.Forms;

namespace iRacingReplayDirector
{
    public partial class LogMessages : Form
    {
        public LogMessages()
        {
            InitializeComponent();
        }

        public TextBox TraceMessage
        {
            get
            {
                return this.TraceMessageTextBox;
            }
        }

        private void LogMessages_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
    }
}
