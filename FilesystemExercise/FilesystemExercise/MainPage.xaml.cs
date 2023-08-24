using System.Diagnostics;

namespace FilesystemExercise
{
    public partial class MainPage : ContentPage, TaskConsumerListener
    {
        List<Button> driveButtons = new();

        TaskConsumer taskConsumer = null;

        public MainPage()
        {
            InitializeComponent();
            RefreshDriveList();
        }

        private void OnPauseBtnClicked(object sender, EventArgs e)
        {
        }

        private void OnResumeBtnClicked(object sender, EventArgs e)
        {
        }

        private void OnStopBtnClicked(object sender, EventArgs e)
        {
        }

        public void RefreshDriveList()
        {
            var driveInfo = DriveInfo.GetDrives();

            foreach (var button in driveButtons)
            {
                leftVerticalLayout.Children.Remove(button);
            }

            driveButtons.Clear();

            foreach (var drive in driveInfo)
            {
                Button button = new()
                {
                    Text = "(" + drive.Name + ") " + drive.VolumeLabel,
                    BindingContext = drive,

                };

                button.Clicked += (sender, e) =>
                {
                    if (sender is Button button)
                    {
                        OnDriveButtonClicked(button.BindingContext as DriveInfo);
                    }
                    else
                    {
                        Debug.Assert(false, "The function is called by an unknown type");
                    }
                };


                driveButtons.Add(button);

                leftVerticalLayout.Children.Add(button);
            }
        }

        public void OnDriveButtonClicked(DriveInfo driveInfo)
        {
            if (taskConsumer == null)
            {
                string rootPath = driveInfo.RootDirectory.ToString();
                taskConsumer = new TaskConsumer(rootPath, this);

                PauseBtn.IsEnabled = true;
                StopBtn.IsEnabled = true;
            }
        }

        public void paused()
        {
            PauseBtn.IsEnabled = false;
            StopBtn.IsEnabled = false;
            ResumeBtn.IsEnabled = true;
        }

        public void stopped()
        {
            PauseBtn.IsEnabled = false;
            StopBtn.IsEnabled = false;
            ResumeBtn.IsEnabled = false;
        }

        public void finished()
        {
            PauseBtn.IsEnabled = false;
            StopBtn.IsEnabled = false;
            ResumeBtn.IsEnabled = false;
        }

        public void newFolderFound(string folderName)
        {
            throw new NotImplementedException();
        }
    }
}