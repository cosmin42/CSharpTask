using System.Collections.ObjectModel;
using System.Diagnostics;

namespace FilesystemExercise
{
    public partial class MainPage : ContentPage, TaskConsumerListener
    {
        List<Button> driveButtons = new();

        TaskConsumer fileScanner = null;

        bool scannerIsStopped = true;

        private ObservableCollection<string> pathsList;

        private Stopwatch stopwatch = new();

        private FileSystemWatcher watcher;

        private string drivePath;

        public MainPage()
        {
            InitializeComponent();
            pathsList = new ObservableCollection<string>();
            itemListView.ItemsSource = pathsList;
            RefreshDriveList();
        }

        private void OnPauseBtnClicked(object sender, EventArgs e)
        {
            fileScanner?.Pause();
        }

        private void OnResumeBtnClicked(object sender, EventArgs e)
        {
            fileScanner?.Resume();
        }

        private void OnStopBtnClicked(object sender, EventArgs e)
        {
            fileScanner?.Stop();
        }

        public void RefreshDriveList()
        {
            pathsList.Clear();
            itemListView.ItemsSource = pathsList;



            foreach (var button in driveButtons)
            {
                leftVerticalLayout.Children.Remove(button);
            }

            driveButtons.Clear();

            var driveInfo = DriveInfo.GetDrives();

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
            if (scannerIsStopped)
            {
                scannerIsStopped = false;
                if (watcher != null)
                {
                    watcher.Dispose();
                }
                pathsList.Clear();
                itemListView.ItemsSource = pathsList;

                drivePath = driveInfo.RootDirectory.ToString();

                SynchronizationContext mainSyncContext = SynchronizationContext.Current;

                fileScanner = new TaskConsumer(drivePath, this, mainSyncContext);

                _ = Task.Run(fileScanner.Start);
            }
            else
            {
                // Add popup, the program is still running
            }
        }

        public void Started()
        {
            StopWatchLabel.Text = "0.0s";

            PauseBtn.IsEnabled = true;
            StopBtn.IsEnabled = true;
            ResumeBtn.IsEnabled = false;

            WaitingIndicator.IsVisible = true;
            WaitingIndicator.IsRunning = true;

            stopwatch.Reset();
            stopwatch.Start();
        }

        public void Resumed()
        {
            PauseBtn.IsEnabled = true;
            StopBtn.IsEnabled = true;
            ResumeBtn.IsEnabled = false;

            WaitingIndicator.IsVisible = true;
            WaitingIndicator.IsRunning = true;
        }

        public void Paused()
        {
            PauseBtn.IsEnabled = false;
            StopBtn.IsEnabled = true;
            ResumeBtn.IsEnabled = true;

            WaitingIndicator.IsVisible = false;
            WaitingIndicator.IsRunning = false;
        }

        public void Stopped()
        {
            stopwatch.Reset();
            PauseBtn.IsEnabled = false;
            StopBtn.IsEnabled = false;
            ResumeBtn.IsEnabled = false;

            WaitingIndicator.IsVisible = false;
            WaitingIndicator.IsRunning = false;

            scannerIsStopped = true;
        }

        public void Finished()
        {
            stopwatch.Stop();

            StopWatchLabel.Text = stopwatch.Elapsed.TotalSeconds.ToString() + "s";

            StopBtn.IsEnabled = false;
            ResumeBtn.IsEnabled = false;
            PauseBtn.IsEnabled = false;

            WaitingIndicator.IsVisible = false;
            WaitingIndicator.IsRunning = false;

            watcher = new FileSystemWatcher(drivePath)
            {
                NotifyFilter = NotifyFilters.FileName
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Size
            };

            watcher.Deleted += (object sender, FileSystemEventArgs e) =>
            {
                if (e.ChangeType != WatcherChangeTypes.Deleted)
                {
                    return;
                }
                fileScanner.ProcessDeletedFile(e.FullPath);
            };

            watcher.Created += (object sender, FileSystemEventArgs e) =>
            {
                if (e.ChangeType != WatcherChangeTypes.Created)
                {
                    return;
                }
                fileScanner.ProcessCreatedFile(e.FullPath);
            };

            watcher.EnableRaisingEvents = true;
            watcher.IncludeSubdirectories = true;

            scannerIsStopped = true;
        }

        public void NewFolderFound((string, long, int) folderDetails)
        {
            var (path, size, count) = folderDetails;
            pathsList.Add(path + " " + (size / (1024 * 1024)) + "MB " + count + " files");

            itemListView.ItemsSource = pathsList;
        }

        public void Replace(string oldPath, (string, long, int) newFolderDetails)
        {
            foreach (var path in pathsList)
            {
                if (path.Contains(oldPath))
                {
                    pathsList.Remove(path);
                    break;
                }
            }
            var (newPath, newSize, newCount) = newFolderDetails;

            pathsList.Add(newPath + " " + (newSize / (1024 * 1024)) + "MB " + newCount + " files");
            itemListView.ItemsSource = pathsList;
        }

        public void Remove(string toBeRemoved)
        {
            if (string.IsNullOrEmpty(toBeRemoved))
            {
                return;
            }

            // MAUI seems to have a problem with removing the first element in an ObservableList by value
            for (var i = 0; i < pathsList.Count; ++i)
            {
                if (pathsList[i].Contains(toBeRemoved + " "))
                {
                    pathsList.RemoveAt(i);
                    break;
                }
            }

            itemListView.ItemsSource = pathsList;
        }
    }
}