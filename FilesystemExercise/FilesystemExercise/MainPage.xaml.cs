using System.IO;

namespace FilesystemExercise
{
    public partial class MainPage : ContentPage
    {
        List<Button> driveButtons = new List<Button>();

        public MainPage()
        {
            InitializeComponent();
            RefreshDriveList();
        }

        public void RefreshDriveList()
        {
            var driveInfo = DriveInfo.GetDrives();

            foreach(var button in driveButtons)
            {
                leftVerticalLayout.Children.Remove(button);
            }

            driveButtons.Clear();

            foreach (var drive in driveInfo)
            {
                Button button = new()
                {
                    Text = "(" + drive.Name + ") " + drive.VolumeLabel
                };

                driveButtons.Add(button);

                leftVerticalLayout.Children.Add(button);
            }
        }
    }
}