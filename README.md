# unity-screen-recorder
The screen recorder app that can be controlled by Unity.

## Limitations
- The application can currently be used on the Unity project targeting Windows.
- The application can be controlled by Unity scripts but works as a separate process, just recording the entire screen.
- If you have multiple screens, only the primary screen is recorded; thus, if you want to record the gameplay, ensure the game runs on the primary screen.

## Preparation
1. Open the C# project called "tool" from this repository in Visual Studio and build it.
2. Place the built application in the Unity project of your choice. The preferred path is ``{project_name}/Assets/StreamingAssets/ScreenRecorder/`` (if you choose a different path, remember to update it in the ``ScreenRecorderController.cs``, which is mentioned later). Placing the application files in a folder other than StreamingAssets may cause Unity errors. Make sure to move all the files necessary for the application, like .dll and .exe!
3. Open this repository's " unity-scripts " directory and copy the files ``ScreenRecorderController.cs`` and ``UnityIntegrationManager.cs`` into your project. The preferred path would be ``{project_name}/Packages/ScreenRecorder/``, but feel free to place them wherever you want.
4. Create your script to communicate with ``ScreenRecorderController.cs``.
5. Run the Unity application!

## Communicating with ScreenRecorderController
To use the screen recorder correctly, you need a script to communicate with ``ScreenRecorderController.cs``. Create a new one or use one of your existing scripts.

At the beginning, add to your script the following code:

```C#
private void OnEnable()
{
    try
    {
        screenRecorderController = new ScreenRecorderController();
        config = new ScreenRecorderConfig();
        UnityIntegrationManager.SetScreenRecorderController(screenRecorderController);
        UnityIntegrationManager.SetConfig(config);
    }
    catch (Exception e)
    {
        Debug.LogError("Could not assign screenRecorderControler variable: " + e);
    }
}
```

Now, you can control the recording process and settings by performing operations on the ``screenRecorderController`` object.

You can, for example, change some settings:

```C#
config.FrameRate = 14;
config.FrameLimit = 60000;
config.DirectoryPath = "Recording"; // directory where the recording and temp files are stored
config.RecordOnPlay = false;
config.SaveConfig();
```

To turn on/off the recorder, you can:
```C#
screenRecorderController.StartRecording_Click(config);
screenRecorderController.StopRecording_Click();
```

## Additional information
The application was developed as part of another project, but I decided to publish this code, believing it may help others. 

It is an original work, but I found inspiration in other sources for some parts of the code (e.g., related to capturing the screen with C#). Unfortunately, the code was written about one year before publishing it here, and I cannot recall those sources. However, those inspirations should relate to the standard solutions used in many places.
