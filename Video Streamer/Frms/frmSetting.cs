using AForge.Video.DirectShow;
using MetroFramework;
using MetroFramework.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Video_Streamer.Clss;

namespace Video_Streamer.Frms
{
    public partial class frmSetting : MetroForm
    {
        public frmSetting()
        {
            InitializeComponent();

            GetCamerasRes();

            Program.Engin.PX2 = pictureBox1;
        }

        private void GetCamerasRes()
        {
            //get cams
            //Program.Engin.RefreshCamList();

            metroComboBox1.Items.Clear();
            metroComboBox1.Items.AddRange(Program.Engin.GetCamerasNameList().ToArray<object>());
            metroComboBox1.SelectedIndex = -1;
        }

        private void metroComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (metroComboBox1.SelectedIndex == -1)
                return;
            Program.Engin.CloseOpenCam();
            Thread.Sleep(1000);
            Program.Engin.UseCam(metroComboBox1.SelectedIndex);
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void frmSetting_FormClosing(object sender, FormClosingEventArgs e)
        {
            Program.Engin.PX2 = null;
            if (metroRadioButton2.Checked)
            {
                try
                {
                    Program.Engin.CloseOpenVideoFile();
                    Program.Engin.OpenVideoFile(metroTextBox1.Text);
                }
                catch (Exception ex)
                {
                    MetroMessageBox.Show(Program.Mainfrm, "error at open file");
                }
            }
            //Program.Engin.CloseOpenCam();
        }

        private void BtnOpenVideo_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "MP4 file|*.mp4";
            if (dialog.ShowDialog() == DialogResult.Cancel)
                return;

            metroTextBox1.Text = dialog.FileName;
            //Program.Engin.OpenVideoFile(dialog.FileName);
        }
    }
}
