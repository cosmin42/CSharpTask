using System.IO;

namespace FilesystemExercise
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
            RefreshDriveList();
        }

        public void RefreshDriveList()
        {
            var driveInfo = DriveInfo.GetDrives();

            leftVerticalLayout.Children.Clear();
            foreach (var drive in driveInfo)
            {
                Button button = new()
                {

                    Text = "(" + drive.Name + ") " + drive.VolumeLabel
                };
                leftVerticalLayout.Children.Add(button);
            }
        }
    }
}