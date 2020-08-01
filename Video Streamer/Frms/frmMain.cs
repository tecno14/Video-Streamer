using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MetroFramework.Forms;
using Video_Streamer.Frms;

namespace Video_Streamer
{
    public partial class frmMain : MetroForm
    {
        public frmMain()
        {
            InitializeComponent();
            Program.Engin.PX1 = pictureBox1;
            Program.Engin.LB = listBox1;
            Program.Engin.lbWatchCount = lbWatchCount;
            Program.Engin.MP = metroPanel1;
            Program.Engin.P = panel1;
            Program.Mainfrm = this;
            Program.Engin.UseMic();
            
        }

        private void btnSetting_Click(object sender, EventArgs e)
        {
            frmSetting frm = new frmSetting();
            frm.ShowDialog();
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            Program.Engin.PX1 = null;
            Program.Engin.CloseOpenCam();
            Program.Engin.CloseOpenMic();
        }

        private void BtnStream_Click(object sender, EventArgs e)
        {
            ChgStreamState();
        }

        private void BtnLink_Click(object sender, EventArgs e)
        {
            ChgStreamState(true);
            Program.Engin.CloseOpenCam();
            Program.Engin.CloseOpenMic();
            ChgLinkState();
        }
        public void ChgStreamState(bool Force2Stop=false)
        {
            if (Force2Stop || Program.Engin.IsStreaming)
                Program.Engin.StopStream();
            else
                Program.Engin.StartStream(int.Parse(mtbPort.Text));
        }

        public void ChgLinkState(bool Force2Stop = false)
        {
            if (Force2Stop || Program.Engin.IsLiking)
                Program.Engin.StopLink();
            else
            {
                frmConnect frm = new frmConnect();
                frm.ShowDialog();
                if (frm.DialogResult == DialogResult.OK)
                    Program.Engin.StartLink();
            }
        }

        private void BtnVideo_Click(object sender, EventArgs e)
        {
            Program.Engin.StopStartCam();
        }

        private void BtnMice_Click(object sender, EventArgs e)
        {
            Program.Engin.MuteUnmute();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            //use first cam
            if (Program.Engin.GetCamerasNameList().Count >= 1)
                Program.Engin.UseCam(0);
        }
    }
}
