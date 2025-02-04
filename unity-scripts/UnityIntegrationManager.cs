using Newtonsoft.Json;
using System;
using System.IO;
using UnityEngine;

public static class UnityIntegrationManager
{
    private static ScreenRecorderController screenRecorderController = null;
    private static ScreenRecorderConfig config;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnRuntimeMethodLoad()
    {
        if (screenRecorderController != null)
        {
            screenRecorderController.runningUnityAppToken = true;
            Application.quitting += OnApplicationQuit;

            config.LoadConfig();
            if (config.RecordOnPlay)
            {
                screenRecorderController.StartWithPlay(config);
            }
        }
    }


    private static void OnApplicationQuit()
    {
        if (screenRecorderController != null)
        {
            screenRecorderController.runningUnityAppToken = false;
            Application.quitting -= OnApplicationQuit;
        }
    }

    public static void SetScreenRecorderController(ScreenRecorderController screenRecorderController)
    {
        UnityIntegrationManager.screenRecorderController = screenRecorderController;
    }

    public static void SetConfig(ScreenRecorderConfig config)
    {
        UnityIntegrationManager.config = config;
    }
}

public class ScreenRecorderConfig
{
    private const string PATH = "screen-recorder-config.json";
    private int frameRate = 14;
    public int FrameRate
    {
        get { return frameRate; }
        set { frameRate = value; }
    }

    private int frameLimit = 60000;
    public int FrameLimit
    {
        get { return frameLimit; }
        set { frameLimit = value; }
    }

    private string directoryPath = "Recording";
    public string DirectoryPath
    {
        get { return directoryPath; }
        set { directoryPath = value; }
    }

    private bool recordOnPlay = false;
    public bool RecordOnPlay
    {
        get { return recordOnPlay; }
        set { recordOnPlay = value; }
    }

    public void SaveConfig()
    {
        string configJson = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(PATH, configJson);
    }

    public void LoadConfig()
    {
        if (File.Exists(PATH))
        {
            try
            {
                string configJson = File.ReadAllText(PATH);
                var config = JsonConvert.DeserializeObject<ScreenRecorderConfig>(configJson);

                FrameRate = config.FrameRate;
                FrameLimit = config.FrameLimit;
                DirectoryPath = config.DirectoryPath;
                RecordOnPlay = config.RecordOnPlay;
            }
            catch (Exception ex)
            {
                Debug.Log($"Error reading configuration file: {ex.Message}");
            }
        }
        else
        {
            Debug.Log("Configuration file not found, using default values.");
        }
    }
}
