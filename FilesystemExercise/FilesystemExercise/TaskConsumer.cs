using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FilesystemExercise
{
    public class TaskConsumer
    {
        private const int ThresholdFileSize = 10 * 1024 * 1024;

        private const int MAXThreadCount = 32;

        private Queue<string> bfsQueue = new();
        private Stack<string> reverseBfs = new();
        private Queue<(string, bool, long, int)> detailsQueue = new();

        readonly TaskConsumerListener thisListener = null;

        readonly SynchronizationContext mainSyncContext;

        bool pause = false;
        bool stop = false;

        bool fillingStage = true;


        string root;

        public TaskConsumer(string rootPath, TaskConsumerListener listener, SynchronizationContext returnThread)
        {
            thisListener = listener;
            bfsQueue.Enqueue(rootPath);
            mainSyncContext = returnThread;
            root = rootPath;
        }

        public async Task Start()
        {
            stop = false;
            mainSyncContext.Post(state =>
            {
                thisListener.Started();
            }, null);


            bool cachedPause = pause;

            while (!stop && bfsQueue.Count > 0)
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
                else
                {
                    ExamineNextPath();
                }
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

        private void FillNext()
        {
            var currentPath = bfsQueue.Dequeue();

            reverseBfs.Push(currentPath);

            IEnumerable<string> directoryEnumeration = Enumerable.Empty<string>();
            try
            {
                directoryEnumeration = Directory.EnumerateFiles(currentPath);
            }
            catch
            {
                Debug.WriteLine("Access not permitted");
            }

            foreach (var directory in directoryEnumeration)
            {
                bfsQueue.Enqueue(directory);
            }

            if (bfsQueue.Count == 0)
            {
                fillingStage = false;
                while (reverseBfs.Count > 0)
                {
                    detailsQueue.Enqueue((reverseBfs.Pop(), false, 0, 0));
                }
            }
        }



        private static (List<string> ExpansionPaths, List<string> ValidDirectories) ExamineSinglePath(string currentPath)
        {
            var newPaths = new List<string>();
            var newValidDirectories = new List<string>();

            if (!Path.Exists(currentPath))
            {
                Debug.WriteLine("The path " + currentPath + " doesn't exist.");
                return (ExpansionPaths: newPaths, ValidDirectories: newValidDirectories);
            }

            if (Directory.Exists(currentPath))
            {
                IEnumerable<string> directoryEnumeration = Enumerable.Empty<string>();
                try
                {
                    directoryEnumeration = Directory.EnumerateFiles(currentPath);
                }
                catch
                {
                    Debug.WriteLine("Access not permitted");
                }

                foreach (var directory in directoryEnumeration)
                {
                    var directoryInfo = new FileInfo(directory);
                    if (directoryInfo.Length >= ThresholdFileSize)
                    {
                        newValidDirectories.Add(currentPath);

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
                    Debug.WriteLine("Access not permitted");
                }

                foreach (var file in filesEnumeration)
                {
                    newPaths.Add(file);
                }
            }
            else
            {
                Debug.WriteLine("This path is not a directory ", currentPath);
            }

            return (ExpansionPaths: newPaths, ValidDirectories: newValidDirectories); ;
        }

        private void ExamineNextPath()
        {
            Task<(List<string> ExpansionPaths, List<string> ValidDirectories)>[] dispatchedTasks = new Task<(List<string> ExpansionPaths, List<string> ValidDirectories)>[Math.Min(bfsQueue.Count, MAXThreadCount)];

            for (var i = 0; i < MAXThreadCount && bfsQueue.Count > 0; ++i)
            {
                var currentPath = bfsQueue.Dequeue();

                dispatchedTasks[i] = Task.Run(() => TaskConsumer.ExamineSinglePath(currentPath));
            }

            Task.WaitAll(dispatchedTasks);

            foreach (var task in dispatchedTasks)
            {
                var (ExpansionPaths, ValidDirectories) = task.Result;

                foreach (var item in ExpansionPaths)
                {
                    bfsQueue.Enqueue(item);
                }

                mainSyncContext.Post(state =>
                {
                    thisListener.NewFolderFound(ValidDirectories);
                }, null);
            }
        }
    }
}
