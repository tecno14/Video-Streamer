using MetroFramework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using Video_Streamer.Clss;

namespace Video_Streamer
{
    static class Program
    {
        public static Eng Engin;

        public static IPAddress tmpIP = null;
        public static int tmpPort = -1;
        public static string RandName;
        public static frmMain Mainfrm;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            RandName = RandomString(5);
            Engin = new Eng();
            Mainfrm = new frmMain();
            Application.Run(Mainfrm);
        }

        static public bool ChkValidIPPort(string IP,string Port,IWin32Window Owner)
        {
            try
            {
                IPAddress ip;
                if (!IPAddress.TryParse(IP, out ip))
                    throw new Exception("IP not valid");        

                int port;
                if (!int.TryParse(Port, out port))
                    throw new Exception("Port not valid");
                if (port < 49152 || port > 65535)
                    throw new Exception("Port Range [ 49152 - 65535 ]");

                tmpIP = ip;
                tmpPort = port;

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.ToString());
                Debug.WriteLine("-------------------------------------------------------------");
                MetroMessageBox.Show(Owner, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return false;
        }
        
        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
