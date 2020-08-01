using AForge.Video.DirectShow;
using MetroFramework.Controls;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using AForge.Video.FFMPEG;
using Video_Streamer.Properties;

namespace Video_Streamer.Clss
{
    public class Eng
    {
        const string Image = "Image";
        const string Audio = "Audio";
        const int FPS = 20;

        //var bytes = System.Text.Encoding.UTF8.GetBytes(str);
        //var decodeString = System.Text.Encoding.UTF8.GetString(bytes);;

        VideoCaptureDevice Camera = null;
        VideoFileSource source = null;
        List<FilterInfo> CamerasList = null;
        List<TcpClient> ClientsList = null;
        List<string> ClientsNameList = null;
        TcpListener StreamListener = null;
        TcpClient LinkClient = null;

        public PictureBox PX1 = null;
        public PictureBox PX2 = null;
        public Label lbWatchCount = null;
        public ListBox LB = null;
        public MetroPanel MP = null;
        public Panel P = null;

        int tmp = 0;
        int timeafterframe;
        Stopwatch watch;
        object SenderFlag = new object();

        bool IsMicRunning = true;

        bool isStreaming = false;
        public bool IsStreaming
        {
            get { return isStreaming; }
            set
            {
                if (MP != null)
                    MP.Visible = value;
                if (P != null)
                    P.Visible = value;
                isStreaming = value;
            }
        }

        bool isLiking = false;
        public bool IsLiking
        {
            get { return isLiking; }
            set
            {
                isLiking = value;
            }
        }


        public Eng()
        {
            ClientsNameList = new List<string>();
            timeafterframe = 1000 / FPS;
            ClientsList = new List<TcpClient>();
            CamerasList = new List<FilterInfo>();
            FilterInfoCollection c = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo f in c)
                CamerasList.Add(f);
        }

        //Camera
        int LastCam = -1;
        public void StopStartCam()
        {
            if (Camera != null)
            {
                CloseOpenCam();
                Task.WaitAll();
                TryInvokePic(null, PX1);
                TryInvokePic(null, PX2);
            }
            else if (LastCam != -1)
            {
                Program.Engin.UseCam(0);
            }
        }
        public List<string> GetCamerasNameList()
        {
            List<string> CamList = new List<string>();

            foreach (FilterInfo f in CamerasList)
                CamList.Add(f.Name);

            return CamList;
        }
        public void UseCam(int Index)
        {
            if (Index >= CamerasList.Count)
                return;

            CloseOpenCam();
            CloseOpenVideoFile();
            wf = Mywf;

            Camera = new VideoCaptureDevice(CamerasList[Index].MonikerString);
            //Camera.DesiredFrameRate = 22;
            Camera.NewFrame += Camera_NewFrame;
            watch = Stopwatch.StartNew();
            Camera.Start();
            LastCam = Index;
        }
        async private void Camera_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            //get frame from cam
            //print in pic box
            //if last fram not sended yet delete it 
            watch.Stop();
            if (watch.ElapsedMilliseconds < timeafterframe)
            {
                tmp++;
                watch.Start();
                return;
            }
            Debug.WriteLine(tmp);
            tmp = 0;
            watch = Stopwatch.StartNew();
            // the code that you want to measure comes here

            await Task.Run(new Action(() =>
            {
                try
                {
                    //put image to picbox
                    Bitmap frame = eventArgs.Frame.Clone() as Bitmap;
                    frame = MergedBitmaps(frame, Video_Streamer.Properties.Resources.icon2);

                    TryInvokePic(frame, PX1);
                    TryInvokePic(frame, PX2);


                    frame = frame.Clone() as Bitmap;
                    ///add icon


                    //rebare img:
                    MemoryStream ms = new MemoryStream();
                    frame.Save(ms, ImageFormat.Jpeg);
                    byte[] data = ms.ToArray();

                    //send img to clients
                    lock (SenderFlag)
                        MySender(data, Image);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("EX: frame.Clone() error:");
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(ex.ToString());
                    Debug.WriteLine("------------------------------");
                }
            }));
        }
        public void CloseOpenCam()
        {
            TryInvokePic(null, PX1);
            TryInvokePic(null, PX2);

            if (Camera != null && Camera.IsRunning)
            {
                Camera.NewFrame -= Camera_NewFrame;
                Camera.SignalToStop();
                Camera.WaitForStop();
                //Camera.Stop();
                Camera = null;
            }
        }
        private Bitmap MergedBitmaps(Bitmap bmp1, Bitmap bmp2)
        {
            Bitmap result = new Bitmap(Math.Max(bmp1.Width, bmp2.Width), Math.Max(bmp1.Height, bmp2.Height));
            using (Graphics g = Graphics.FromImage(result))
            {
                g.DrawImage(bmp1, Point.Empty);
                g.DrawImage(bmp2, 0, Math.Abs(bmp1.Height - bmp2.Height));
            }
            return result;
        }

        //Mic
        WaveIn WaveSource = null;
        WaveFormat wf = new WaveFormat(44100, 2);
        WaveFormat Mywf = new WaveFormat(44100, 2);
        MemoryStream AudioStream = new MemoryStream();
        public void UseMic()
        {
            Debug.WriteLine(WaveIn.DeviceCount);

            WaveSource = new WaveIn();
            WaveSource.DeviceNumber = 0;
            WaveSource.WaveFormat = wf;

            WaveSource.DataAvailable += new EventHandler<WaveInEventArgs>(waveSource_DataAvailable);
            WaveSource.RecordingStopped += new EventHandler<StoppedEventArgs>(waveSource_RecordingStopped);

            WaveSource.StartRecording();
            IsMicRunning = true;
        }
        void waveSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            //play then send
            if (WaveSource == null)
                return;

            byte[] data = e.Buffer;

            //send audio to clients
            lock (SenderFlag)
                MySender(data, Audio);
        }
        void waveSource_RecordingStopped(object sender, StoppedEventArgs e)
        {
            if (WaveSource != null)
            {
                WaveSource.Dispose();
                WaveSource = null;
            }

            //if (waveFile != null)
            //{
            //    waveFile.Dispose();
            //    waveFile = null;
            //}

            //StartBtn.Enabled = true;
        }
        public void CloseOpenMic()
        {
            if (IsMicRunning)
                WaveSource.StopRecording();
            IsMicRunning = false;
        }
        public void MuteUnmute()
        {
            if (IsMicRunning)
                WaveSource.StopRecording();
            else
                UseMic();

            IsMicRunning = !IsMicRunning;
        }

        //VideoFile
        bool IsVideoFile = false;
        async public void OpenVideoFile(string FileName)
        {
            //close all
            CloseOpenCam();
            CloseOpenMic();
            CloseOpenVideoFile();

            source = new VideoFileSource(FileName);

            source.NewFrame += Camera_NewFrame;

            string output = Path.ChangeExtension(Path.GetTempFileName(), "mp3");
            var CV = new NReco.VideoConverter.FFMpegConverter();
            CV.ConvertMedia(FileName, output, "mp3");

            WaveStream reader = new Mp3FileReader(output);
            wf = reader.WaveFormat;
            //WaveIn wi = new WaveIn();
            //wi.ge
            //WaveOut wo = new WaveOut();
            //wo.
            await Task.Run(new Action(() =>
            {
                byte[] data = new byte[reader.Length];
                while (reader.Read(data, 0, 500) > 0)
                {
                    lock (SenderFlag)
                    {
                        MySender(data, Audio);
                        PlayAudio(new MemoryStream(data));
                    }
                }
            }));

            source.Start();//video
            //IsMicRunning = true;
            IsVideoFile = true;
        }
        public short[] GetDataOf16Bit2ChannelFile(WaveStream reader)
        {
            reader.Seek(0, System.IO.SeekOrigin.Begin);
            byte[] data2 = new byte[reader.Length];
            reader.Read(data2, 0, data2.Length);

            short[] res2 = new short[data2.Length / 2];
            Buffer.BlockCopy(data2, 0, res2, 0, data2.Length);

            return res2;
        }
        public void CloseOpenVideoFile()
        {
            if (!IsVideoFile)
                return;
            source.Stop();
            source = null;
            IsVideoFile = false;
        }


        //Send
        public void MySender(byte[] data, string type)
        {
            //send to all
            try
            {
                if (ClientsList == null)
                    return;
                foreach (var c in ClientsList)
                {
                    BinaryWriter writer = new BinaryWriter(c.GetStream());
                    try
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(type);
                        writer.Write(bytes.Length);
                        writer.Write(bytes);

                        writer.Write(data.Length);
                        writer.Write(data);
                        writer.Flush();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("EX: BinaryWriter client error:");
                        Debug.WriteLine(ex.Message);
                        Debug.WriteLine(ex.ToString());
                        Debug.WriteLine("------------------------------");

                        if (c.Connected)
                            c.Close();
                        int index = ClientsList.IndexOf(c);
                        lock (ClientsList)
                        {
                            ClientsList.Remove(c);
                            ClientsNameList.RemoveAt(index);
                            RefreshWatchName();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("EX: MySender all client error:");
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.ToString());
                Debug.WriteLine("------------------------------");
            }
        }

        //Viewers Count
        private void RefreshWatchName()
        {
            int Count = 0;
            if (LB != null)
            {
                LB.Invoke(new Action(delegate ()
                {
                    LB.Items.Clear();
                    foreach (var item in ClientsNameList)
                    {
                        LB.Items.Add(item);
                        Debug.WriteLine(item);
                        LB.Refresh();
                    }
                    Count = LB.Items.Count;
                }));
                SetWatchCount(Count);
            }
        }
        private void SetWatchCount(int x)
        {
            if (lbWatchCount != null)
                lbWatchCount.Invoke(new Action(delegate ()
                {
                    lbWatchCount.Text = x.ToString();
                    lbWatchCount.Refresh();
                }));
        }

        //ViewVideo
        private void TryInvokePic(Image img, PictureBox PX)
        {
            if (PX == null)
                return;
            try
            {
                PX.Invoke(new Action(delegate ()
                {
                    PX.Image = img;
                }));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.ToString());
                Debug.WriteLine("------------------------------");
            }
        }

        //ViewAudio

        async public void PlayAudio(MemoryStream ms)
        {
            await Task.Run(new Action(() =>
            {
                WaveStream reader = new RawSourceWaveStream(ms, wf);
                WaveOut WaveOut = new WaveOut();
                WaveOut.Init(reader);
                WaveOut.Play();
            }));
        }


        //Stream
        async public void StartStream(int port)
        {
            if (StreamListener != null)
                StopStream();
            StreamListener = new TcpListener(IPAddress.Any, port);
            StreamListener.Start();
            IsStreaming = true;
            try
            {
                while (true)
                {
                    TcpClient client = await StreamListener.AcceptTcpClientAsync();
                    this.ClientsList.Add(client);
                    StreamReader reader = new StreamReader(client.GetStream());
                    String name = await reader.ReadLineAsync();
                    ClientsNameList.Add(name);
                    RefreshWatchName();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.ToString());
                Debug.WriteLine("------------------------------");
            }
            StopStream();
        }
        public void StopStream()
        {
            if (StreamListener != null)
                StreamListener.Stop();
            StreamListener = null;
            IsStreaming = false;
            RefreshWatchName();
            foreach (var item in ClientsList)
                item.Close();
            ClientsList = new List<TcpClient>();
        }

        //Reseve
        async public void StartLink()
        {
            if (Program.tmpIP == null || Program.tmpPort == -1)
                return;
            if (IsLiking)
                StopLink();
            IsLiking = true;
            try
            {
                LinkClient = new TcpClient();
                await LinkClient.ConnectAsync(Program.tmpIP, Program.tmpPort);
                StreamWriter writer = new StreamWriter(LinkClient.GetStream());
                await writer.WriteLineAsync(Program.RandName);
                await writer.FlushAsync();
                while (true)
                {
                    byte[] data, imageData;
                    int size, t;

                    ///read string 
                    data = new byte[4];
                    await LinkClient.GetStream().ReadAsync(data, 0, data.Length);
                    size = BitConverter.ToInt32(data, 0);
                    imageData = new byte[size];
                    t = 0;
                    while (t < size)
                        t += await LinkClient.GetStream().ReadAsync(imageData, t, imageData.Length - t);
                    string Status = Encoding.UTF8.GetString(imageData);

                    ///read data
                    data = new byte[4];
                    await LinkClient.GetStream().ReadAsync(data, 0, data.Length);
                    size = BitConverter.ToInt32(data, 0);
                    imageData = new byte[size];
                    t = 0;
                    while (t < size)
                        t += await LinkClient.GetStream().ReadAsync(imageData, t, imageData.Length - t);
                    if (Status == Image)
                    {
                        Bitmap b = new Bitmap(new MemoryStream(imageData));
                        TryInvokePic(b, PX1);
                        if (IsRecord)
                            SaveVidFrame(b);

                    }
                    else if (Status == Audio)
                    {
                        PlayAudio(new MemoryStream(imageData));
                        if (IsRecord)
                            SaveAudFrame(imageData);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.ToString());
                Debug.WriteLine("------------------------------");

                if (IsLiking)
                    MetroFramework.MetroMessageBox.Show(Program.Mainfrm, "stream not found", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                else
                    MetroFramework.MetroMessageBox.Show(Program.Mainfrm, "you disconnect stream", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Question);
                TryInvokePic(null, PX1);
            }
            StopLink();
        }
        public void StopLink()
        {
            if (LinkClient != null)
                LinkClient.Close();
            LinkClient = null;
            IsLiking = false;
            TryInvokePic(null, PX1);
        }

        //Record
        public bool IsRecord = false;
        VideoFileWriter VideoFileWriter;
        byte[] Audiob = null;
        string tmpVidPath = Path.GetTempFileName() + ".avi";
        string tmpAudPath = Path.GetTempFileName() + ".mp3";
        public Byte[] WavToMP3(WaveStream ws)//byte[] wavFile)
        {
            //using (MemoryStream source = new MemoryStream(wavFile))
            //using (NAudio.Wave.WaveFileReader rdr = new NAudio.Wave.WaveFileReader(source))
            {
                WaveLib.WaveFormat fmt = new WaveLib.WaveFormat(wf.SampleRate, wf.BitsPerSample, wf.Channels);
                //new WaveLib.WaveFormat(rdr.WaveFormat.SampleRate, rdr.WaveFormat.BitsPerSample, rdr.WaveFormat.Channels);

                // convert to MP3 at 96kbit/sec...
                Yeti.Lame.BE_CONFIG conf = new Yeti.Lame.BE_CONFIG(fmt, 96);

                // Allocate a 1-second buffer
                int blen = ws.WaveFormat.AverageBytesPerSecond;
                byte[] buffer = new byte[blen];

                // Do conversion
                using (MemoryStream output = new MemoryStream())
                {
                    Yeti.MMedia.Mp3.Mp3Writer mp3 = new Yeti.MMedia.Mp3.Mp3Writer(output, fmt, conf);

                    int readCount;
                    while ((readCount = ws.Read(buffer, 0, blen)) > 0)
                        mp3.Write(buffer, 0, readCount);

                    mp3.Close();
                    return output.ToArray();
                }
            }
        }
        public void RecStartStop()
        {
            if (IsRecord)
            {
                //save
                IsRecord = false;
                VideoFileWriter.Close();

                using (FileStream file = new FileStream(tmpAudPath, FileMode.Create, FileAccess.Write))
                {
                    var data = WavToMP3(reader2);

                    byte[] buffer = new byte[5000000];

                    MemoryStream mstream = new MemoryStream();
                    BinaryWriter writer2 = new BinaryWriter(mstream);
                    foreach (var item in data)
                        writer2.Write(item);

                    mstream.Seek(0, SeekOrigin.Begin);
                    while (true)
                    {
                        int r = mstream.Read(buffer, 0, buffer.Length);
                        if (r == 0)
                            break;
                        file.Write(buffer, 0, r);
                    }
                    mstream.Close();
                    file.Close();
                    //memoryStream.WriteTo(file);        
                }
                //stop audio
                //combain


                return;
            }


            int width = 640;
            int height = 480;

            VideoFileWriter = new VideoFileWriter();
            VideoFileWriter.Open(tmpVidPath, width, height, 20, VideoCodec.MPEG4, 1000000);


            IsRecord = true;
        }
        public void SaveVidFrame(Bitmap b)
        {
            try
            {
                VideoFileWriter.WriteVideoFrame(b);
            }
            catch { }
        }
        public void SaveAudFrame(byte[] n)
        {
            try
            {
                var A = new byte[Audiob.Length + n.Length];
                Audiob.CopyTo(A, 0);
                n.CopyTo(A, Audiob.Length);
                Audiob = A;
            }
            catch { }
        }
    }
}
