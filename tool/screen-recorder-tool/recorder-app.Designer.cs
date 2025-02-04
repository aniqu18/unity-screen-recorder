namespace new_screen_recorder
{
    partial class RecorderApp
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            CaptureScreen = new Button();
            stopButton = new Button();
            SuspendLayout();
            // 
            // CaptureScreen
            // 
            CaptureScreen.Location = new Point(278, 83);
            CaptureScreen.Name = "CaptureScreen";
            CaptureScreen.Size = new Size(255, 119);
            CaptureScreen.TabIndex = 0;
            CaptureScreen.Text = "Start recording";
            CaptureScreen.UseVisualStyleBackColor = true;
            CaptureScreen.Click += CaptureScreen_Click;
            // 
            // stopButton
            // 
            stopButton.Location = new Point(278, 231);
            stopButton.Name = "stopButton";
            stopButton.Size = new Size(255, 119);
            stopButton.TabIndex = 1;
            stopButton.Text = "Stop recording";
            stopButton.UseVisualStyleBackColor = true;
            stopButton.Click += StopButton_Click;
            // 
            // RecorderApp
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(stopButton);
            Controls.Add(CaptureScreen);
            Name = "RecorderApp";
            Text = "Recorder app";
            Load += recorder_load;
            ResumeLayout(false);
        }

        #endregion

        private Button CaptureScreen;
        private Button stopButton;
    }
}
