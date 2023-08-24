﻿using System.Collections.ObjectModel;
using System.Diagnostics;

namespace FilesystemExercise
{
    public partial class MainPage : ContentPage, TaskConsumerListener
    {
        List<Button> driveButtons = new();

        TaskConsumer fileScanner = null;

        bool scannerIsStopped = true;

        private ObservableCollection<string> pathsList;

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
                pathsList.Clear();
                itemListView.ItemsSource = pathsList;

                string rootPath = driveInfo.RootDirectory.ToString();

                SynchronizationContext mainSyncContext = SynchronizationContext.Current;

                fileScanner = new TaskConsumer(rootPath, this, mainSyncContext);

                _ = Task.Run(fileScanner.Start);
            }
            else
            {
                // Add popup, the program is still running
            }
        }

        public void Started()
        {
            PauseBtn.IsEnabled = true;
            StopBtn.IsEnabled = true;
            ResumeBtn.IsEnabled = false;

            WaitingIndicator.IsVisible = true;
            WaitingIndicator.IsRunning = true;
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
            PauseBtn.IsEnabled = false;
            StopBtn.IsEnabled = false;
            ResumeBtn.IsEnabled = false;

            WaitingIndicator.IsVisible = false;
            WaitingIndicator.IsRunning = false;

            scannerIsStopped = true;
        }

        public void Finished()
        {
            StopBtn.IsEnabled = false;
            ResumeBtn.IsEnabled = false;

            WaitingIndicator.IsVisible = false;
            WaitingIndicator.IsRunning = false;
        }

        public void NewFolderFound(List<string> folderNames)
        {
            foreach (var folderName in folderNames)
            {
                pathsList.Add(folderName);
            }
            itemListView.ItemsSource = pathsList;
        }
    }
}