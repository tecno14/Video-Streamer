using MetroFramework;
using MetroFramework.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Video_Streamer.Frms
{
    public partial class frmConnect : MetroForm
    {
        public frmConnect()
        {
            InitializeComponent();
            this.DialogResult = DialogResult.Cancel;
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (!Program.ChkValidIPPort(metroTextBox1.Text, metroTextBox2.Text, this))
                return;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
