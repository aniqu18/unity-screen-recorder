using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO.Pipes;


namespace new_screen_recorder
{
    public partial class RecorderApp : Form
    {
        private Thread recordingThread;
        private Thread monitoringThread;
        private CancellationTokenSource? cancellationTokenSource;

        private readonly object lockObject = new();

        private NamedPipeServerStream? pipeServer;
        private NamedPipeServerStream? pipeResponseServer;

        private bool waitToken = true;
        private bool showMessageToken = true;

        // User cannot define these variables
        private int frameNumber = 0;
        private int refreshRate = 0;
        private readonly int frameWarning = 50000;
        private int recControllerPID;


        // User can define these variables
        private int frameLimit = 60000;
        private int frameRate = 14;
        private string directoryPath = "Recording";



        public RecorderApp()
        {
            InitializeComponent();
            WindowState = FormWindowState.Minimized;
            ShowInTaskbar = false;
            Visible = false;
        }


        private void ListenForCommands()
        {
            while (pipeServer != null)
            {
                pipeServer.WaitForConnection();
                using (StreamReader reader = new StreamReader(pipeServer))
                {
                    string command;
                    while ((command = reader.ReadLine()) != null)
                    {
                        if (command.StartsWith("@START@"))
                        {
                            SendResponse("Turning on the recorder...");
                            Parse(command);
                            StartRecording();
                        }
                        else if (command == "@STOP@")
                        {
                            ExecuteStopActions();
                        }
                        else if (command.StartsWith("@SET_UNITY_PID@"))
                        {
                            if (int.TryParse(command.Substring("@SET_UNITY_PID@".Length), out int pid))
                            {
                                recControllerPID = pid;
                            }
                        }
                    }
                }
            }
        }

        private void Parse(string command)
        {
            string[] parameters = command.Substring(8).Split('_');
            foreach (string parameter in parameters)
            {
                string[] keyValue = parameter.Split('=');
                if (keyValue.Length == 2)
                {
                    switch (keyValue[0])
                    {
                        case "frameLimit":
                            if (int.TryParse(keyValue[1], out int fl))
                            {
                                frameLimit = fl;
                            }
                            break;
                        case "frameRate":
                            if (int.TryParse(keyValue[1], out int fr))
                            {
                                frameRate = fr;
                            }
                            break;
                        case "directoryPath":
                            directoryPath = keyValue[1];
                            break;
                    }
                }
            }
        }


        private void SendResponse(string response)
        {
            try
            {
                if (pipeResponseServer == null || !pipeResponseServer.IsConnected)
                {
                    pipeResponseServer = new NamedPipeServerStream("ScreenRecorderResponsePipe", PipeDirection.Out);
                    pipeResponseServer.WaitForConnection();
                }

                StreamWriter writer = new StreamWriter(pipeResponseServer);
                writer.AutoFlush = true;
                writer.WriteLine(response);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send response: {response} Error message: {ex.Message} " +
                    $"This error is related to problems in the communication between controller script and screen recorder process.");
            }
        }


        private void StopRecording()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                recordingThread?.Join();
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }
        }


        private void StartRecording()
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            DirectoryInfo di = new DirectoryInfo(directoryPath);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }

            cancellationTokenSource = new();
            var cancellationToken = cancellationTokenSource.Token;

            recordingThread = new Thread(() => RecordScreenT(cancellationToken));
            monitoringThread = new Thread(() => MonitorControllerProcess(cancellationToken));
            recordingThread.Start();
            monitoringThread.Start();
            SendResponse("Recording...");

        }


        private void CaptureScreen_Click(object sender, EventArgs e)
        {
            StartRecording();
        }


        private void RecordScreenT(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                DoCapture();
                CheckFramesNumber();

                try
                {
                    Thread.Sleep(refreshRate);
                }
                catch (ThreadInterruptedException)
                {
                    break;
                }
            }
        }

        private void CheckFramesNumber()
        {
            if (frameNumber > frameWarning && showMessageToken)
            {
                SendResponse($"Number of frames exceeds {frameWarning}. After {frameLimit} recording will be stopped.");
                showMessageToken = false;
            }

            if (frameNumber > frameLimit)
            {
                SendResponse($"Number of frames exceeds {frameLimit}. Pausing the recorder...");
                waitToken = false;
                SendResponse("@EMERGENCY_STOP@");
            }
        }

        private void SaveFrame(Bitmap frame)
        {
            frame.Save($"{directoryPath}/screenshot_{frameNumber}.jpg", ImageFormat.Jpeg);
            frameNumber++;
        }


        private void DoCapture()
        {
            try
            {
                var screen = Screen.PrimaryScreen;

                if (screen != null)
                {
                    Rectangle bounds = screen.Bounds;
                    Bitmap bitmap = new(bounds.Width, bounds.Height);
                    try
                    {
                        using (Graphics graphics = Graphics.FromImage(bitmap))
                        {
                            graphics.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                        }

                        lock (lockObject)
                        {
                            SaveFrame(bitmap);
                        }
                    }
                    finally
                    {
                        bitmap.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                SendResponse($"An error occured while capturing the screen: {ex.Message}");
            }
        }


        private void StopButton_Click(object sender, EventArgs e)
        {
            ExecuteStopActions();
        }


        private void ExecuteStopActions()
        {
            SendResponse("Turning off the recorder...");
            StopRecording();
            SendResponse("Recorder is off. Creating the video... It may take some time.");
            MergeImages();
            SendResponse("Cleaning temporary files.");
            DeleteJpgFiles();
            SendResponse($"Video created and saved in the directory {directoryPath}.");
            Thread.Sleep(5);
            SendResponse("@END@");


            if (pipeServer != null)
            {
                pipeServer.Close();
                pipeServer.Dispose();
                pipeServer = null;
            }

            Invoke((MethodInvoker)delegate {Close();});
        }


        private void MergeImages()
        {
            string screenshotsDirectory = directoryPath;
            string outputVideoFilePath = Path.Combine(screenshotsDirectory, "output_video.mp4");

            var imageFiles = Directory.GetFiles(screenshotsDirectory, "screenshot_*.jpg")
                                      .Select(f => new { FileName = f, Index = ExtractIndex(f) })
                                      .OrderBy(f => f.Index) 
                                      .Select(f => f.FileName)
                                      .ToList();


            if (imageFiles.Count == 0)
            {
                SendResponse("ERROR: No frames to merge.");
                return;
            }

            try
            {
                using (Bitmap firstImage = new Bitmap(imageFiles[0]))
                {
                    int width = firstImage.Width;
                    int height = firstImage.Height;

                    using (VideoWriter writer = new VideoWriter(outputVideoFilePath, VideoWriter.Fourcc('H', '2', '6', '4'), frameRate, new Size(width, height), true))
                    {
                        foreach (var imageFile in imageFiles)
                        {
                            using (Bitmap image = new Bitmap(imageFile))
                            {
                                Mat mat = new Mat();
                                mat = BitmapToMat(image);
                                writer.Write(mat);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SendResponse($"An error occured while creating the video: {ex.Message}");
            }
        }


        private void DeleteJpgFiles()
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(directoryPath);
                foreach (FileInfo file in di.GetFiles("*.jpg"))
                {
                    file.Delete();
                }
            }
            catch (Exception ex)
            {
                SendResponse($"An error occurred while deleting JPEG files: {ex.Message}");
            }
        }


        private int ExtractIndex(string filename)
        {
            string numberPart = Path.GetFileNameWithoutExtension(filename).Split('_').Last();
            if (int.TryParse(numberPart, out int index))
            {
                return index;
            }
            return -1; 
        }


        private Mat BitmapToMat(Bitmap bitmap)
        {
            Mat mat = new Mat();
            bitmap = new Bitmap(bitmap);
            CvInvoke.CvtColor(bitmap.ToImage<Bgr, byte>(), mat, ColorConversion.Bgr2Bgra);
            return mat;
        }


        private void recorder_load(object sender, EventArgs e)
        {
            pipeServer = new NamedPipeServerStream("ScreenRecorderPipe", PipeDirection.InOut);
            Task.Run(ListenForCommands);
            Task.Run(WaitForEndSingal);
        }

        private void WaitForEndSingal()
        {
            while (waitToken)
            {
                Thread.Sleep(1000);
            }

            ExecuteStopActions();
        }

        private void MonitorControllerProcess(CancellationToken cancellationToken)
        {
            if (recControllerPID <= 0)
            {
                return;
            }

            try
            {
                Process controllerProc = Process.GetProcessById(recControllerPID);
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (controllerProc.HasExited)
                    {
                        MessageBox.Show("Screen recorder has lost a connection with the controller. The recording will be stopped. " +
                            "You may see messages with many errors. For every message click \"OK\". Do not kill the screen-recorder-tool process – it should finish its work despite the errors.");
                        ExecuteStopActions();
                        break;
                    }
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                SendResponse($"Error monitoring controller process: {ex.Message}");
            }
        }

        private void tbDebugBox_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
