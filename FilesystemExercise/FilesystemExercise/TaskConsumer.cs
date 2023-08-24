using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilesystemExercise
{
    public class TaskConsumer
    {
        private const int ThresholdFileSize = 10 * 1024 * 1024;

        Queue<string> examinationTasks = new();
        TaskConsumerListener thisListener = null;

        List<string> searchResults = new();

        bool pause = false;
        bool stop = false;

        SynchronizationContext mainSyncContext;

        public TaskConsumer(string rootPath, TaskConsumerListener listener, SynchronizationContext returnThread)
        {
            thisListener = listener;
            examinationTasks.Enqueue(rootPath);
            mainSyncContext = returnThread;
        }

        public async Task Start()
        {
            mainSyncContext.Post(state =>
            {
                thisListener.Started();
            }, null);


            bool cachedPause = pause;

            while (!stop && examinationTasks.Count() != 0)
            {
                if (!cachedPause && pause)
                {
                    cachedPause = pause;
                    mainSyncContext.Post(state =>
                    {
                        thisListener.Paused();
                    }, null);
                }
                if (cachedPause && !pause)
                {
                    cachedPause = pause;
                    mainSyncContext.Post(state =>
                    {
                        thisListener.Resumed();
                    }, null);
                }
                if (pause)
                {
                    await Task.Delay(100);
                }
                ExamineNextPath();
            }
            if (stop)
            {
                mainSyncContext.Post(state =>
                {
                    thisListener.Stopped();
                }, null);
            }
            else
            {
                mainSyncContext.Post(state =>
                {
                    thisListener.Finished();
                }, null);
            }
        }

        public void Stop()
        {
            stop = true;
        }

        public void Pause()
        {
            pause = true;
        }

        public void Resume()
        {
            pause = false;
        }

        private void ExamineNextPath()
        {
            var currentPath = examinationTasks.Dequeue();

            if (!Path.Exists(currentPath))
            {
                Console.WriteLine("The path " + currentPath + " doesn't exist.");
                return;
            }

            if (Directory.Exists(currentPath))
            {
                IEnumerable<string> directoryEnumeration = Enumerable.Empty<string>();
                try
                {
                    directoryEnumeration = Directory.EnumerateFiles(currentPath);
                }
                catch {
                    Console.WriteLine("Access not permitted");
                }
                foreach (var file in directoryEnumeration)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.Length >= ThresholdFileSize)
                    {
                        searchResults.Add(currentPath);
                        mainSyncContext.Post(state =>
                        {
                            thisListener.NewFolderFound(currentPath);
                        }, null);

                        break;
                    }
                }

                IEnumerable<string> filesEnumeration = Enumerable.Empty<string>();
                try
                {
                    filesEnumeration = Directory.EnumerateDirectories(currentPath);
                }
                catch
                {
                    Console.WriteLine("Access not permitted");
                }

                foreach (var folder in filesEnumeration)
                {
                    examinationTasks.Enqueue(folder);
                }
            }
            else
            {
                Console.WriteLine("This path is not a directory ", currentPath);
            }
        }
    }
}
