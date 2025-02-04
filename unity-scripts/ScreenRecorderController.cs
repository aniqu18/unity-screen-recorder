using System.Diagnostics;
using System.IO.Pipes;
using System.IO;
using System.Threading;
using System;

public class ScreenRecorderController
{
    private Process screenRecorderProcess;
    private NamedPipeClientStream pipeClient;
    private NamedPipeClientStream pipeResponseClient;
    
    private string pathToScreenRecorderExe = "Assets\\StreamingAssets\\ScreenRecorder\\screen-recorder-tool.exe";
    private Thread recordingThread;
    private Thread stoppingThread;
    private Thread responseThread;
    private Thread waitForAppStatusChange;

    private bool stopRecordingRequested = false;
    private bool canStopRecording = false;
    private bool sendStopToken = true;
    private bool emergencyStopToken = true;
    private bool waitForResponse;

    internal bool runningUnityAppToken = false;

    private string statusMessage = "";
    private string tempMessage = "The recorder is off";


    public void StartRecording_Click(ScreenRecorderConfig config)
    {
        if (runningUnityAppToken)
        {
            ExecuteStartActions(config);
        }
        else
        {
            statusMessage = "Run the Unity project before the recorder.";
        }
    }

    internal void StartWithPlay(ScreenRecorderConfig config)
    {
        ExecuteStartActions(config);
    }


    private void ExecuteStartActions(ScreenRecorderConfig config)
    {
        if (recordingThread == null && screenRecorderProcess == null)
        {
            stopRecordingRequested = false;
            emergencyStopToken = true;
            recordingThread = new Thread(() =>
            {
                config.LoadConfig();
                StartRecorderProcess();
                StartResponseListener();
                SendCommand($"@SET_UNITY_PID@{Process.GetCurrentProcess().Id}");
                SendCommand($"@START@!frameLimit={config.FrameLimit}_frameRate={config.FrameRate}_directoryPath={config.DirectoryPath}");

                while (!stopRecordingRequested)
                {
                    Thread.Sleep(100);
                }

                if (sendStopToken)
                {
                    SendCommand("@STOP@");
                }

                stoppingThread = new Thread(() =>
                {
                    screenRecorderProcess.WaitForExit();
                    screenRecorderProcess = null;
                    DisposePipeClient();
                });

                stoppingThread.Start();
            });
            recordingThread.Start();
            canStopRecording = true;

            waitForAppStatusChange = new Thread(() =>
            {
                while (runningUnityAppToken)
                {
                    Thread.Sleep(1000);
                }

                if (screenRecorderProcess != null && recordingThread != null)
                {
                    sendStopToken = true;
                    ExecuteStopActions();
                }
            });

            waitForAppStatusChange.Start();
        }
        else
        {
            statusMessage = "The recorder is on. You have to stop the previous recording to start a new process.";
        }
    }


    private void StartResponseListener()
    {
        responseThread = new Thread(() =>
        {
            pipeResponseClient = new NamedPipeClientStream(".", "ScreenRecorderResponsePipe", PipeDirection.In);
            pipeResponseClient.Connect();
            waitForResponse = true;
            using StreamReader reader = new(pipeResponseClient);
            while (waitForResponse)
            {
                string response = reader.ReadLine();
                if (response != null)
                {
                    if (response == "@EMERGENCY_STOP@")
                    {
                        if (emergencyStopToken)
                        {
                            sendStopToken = false;
                            ExecuteStopActions();
                            emergencyStopToken = false;
                        }
                    }
                    else if (response == "@END@")
                    {
                        tempMessage = "The recorder is off";
                    }
                    else
                    {
                        statusMessage = response;
                    }
                }
            }
        });
        responseThread.Start();
    }


    public void StopRecording_Click()
    {
        if (recordingThread != null && recordingThread.IsAlive && screenRecorderProcess != null && canStopRecording)
        {
            sendStopToken = true;
            tempMessage = "Turning off the previous process... Wait a moment.";
            ExecuteStopActions();
        }
        else
        {
            statusMessage = tempMessage;
        }
    }

    private void ExecuteStopActions()
    {
        if (recordingThread != null && recordingThread.IsAlive && screenRecorderProcess != null && canStopRecording)
        {
            canStopRecording = false;
            stopRecordingRequested = true;
            recordingThread.Join();
            recordingThread = null;
        }
    }


    private void StartRecorderProcess()
    {
        if (screenRecorderProcess == null)
        {
            screenRecorderProcess = new Process();
            screenRecorderProcess.StartInfo.FileName = pathToScreenRecorderExe;
            screenRecorderProcess.Start();
        }
    }


    private void SendCommand(string command)
    {
        try
        {
            if (pipeClient == null || !pipeClient.IsConnected)
            {
                pipeClient = new NamedPipeClientStream(".", "ScreenRecorderPipe", PipeDirection.Out);
                pipeClient.Connect();
            }

            StreamWriter writer = new StreamWriter(pipeClient);
            writer.AutoFlush = true;
            writer.WriteLine(command);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Error sending command '{command}': {ex.Message}. This error is related to problems in the communication between controller script and screen recorder process.");
        }
    }


    private void DisposePipeClient()
    {
        if (pipeClient != null)
        {
            pipeClient.Close();
            pipeClient.Dispose();
            pipeClient = null;
        }

        if (pipeResponseClient != null)
        {
            waitForResponse = false;
            pipeResponseClient.Close();
            pipeResponseClient.Dispose();
            pipeResponseClient = null;
        }

        if (responseThread != null)
        {
            responseThread.Join();
            responseThread = null;

        }

    }


    public string GetStatusMessage()
    {
        return statusMessage;
    }

}