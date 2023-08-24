using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilesystemExercise
{
    public class TaskConsumer
    {
        private const int ThresholdFileSize = 10 * 1024 * 1024;

        Queue<string> tasks = new();
        TaskConsumerListener thisListener = null;

        List<string> searchResults = new();

        bool pause = false;
        bool stop = false;

        SynchronizationContext mainSyncContext;

        public TaskConsumer(string rootPath, TaskConsumerListener listener, SynchronizationContext returnThread)
        {
            thisListener = listener;
            tasks.Enqueue(rootPath);
            mainSyncContext = returnThread;
        }

        public async Task Start()
        {
            mainSyncContext.Post(state =>
            {
                thisListener.Started();
            }, null);


            bool cachedPause = pause;

            while (!stop && tasks.Count() != 0)
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

        public List<string> SearchResults()
        {
            return searchResults;
        }

        private void ExamineNextPath()
        {
            var currentPath = tasks.Dequeue();

            if (!Path.Exists(currentPath))
            {
                Console.WriteLine("The path " + currentPath + " doesn't exist.");
                return;
            }

            if (Directory.Exists(currentPath))
            {
                foreach (var file in Directory.EnumerateFiles(currentPath))
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

                foreach (var folder in Directory.EnumerateDirectories(currentPath))
                {
                    tasks.Enqueue(folder);
                }
            }
            else
            {
                Console.WriteLine("This path is not a directory ", currentPath);
            }
        }


    }
}
