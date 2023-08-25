using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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

        private Queue<(string, int)> bfsQueue = new();
        private Stack<(string, int)> reverseBfs = new();
        private ConcurrentDictionary<string, (bool, long, int)> details = new();

        private List<string> foundPaths;

        readonly TaskConsumerListener thisListener = null;

        readonly SynchronizationContext mainSyncContext;

        bool pause = false;
        bool stop = false;

        bool fillingStage = true;

        public TaskConsumer(string rootPath, TaskConsumerListener listener, SynchronizationContext returnThread)
        {
            thisListener = listener;
            bfsQueue.Enqueue((rootPath, 0));
            mainSyncContext = returnThread;
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
                    if (fillingStage)
                    {
                        FillNext();
                    }
                    else
                    {
                        ProcessNextPath();
                    }
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
                directoryEnumeration = Directory.EnumerateDirectories(currentPath.Item1);
            }
            catch
            {
                Debug.WriteLine("Access not permitted");
            }

            foreach (var directory in directoryEnumeration)
            {
                bfsQueue.Enqueue((directory, currentPath.Item2 + 1));
            }

            if (bfsQueue.Count == 0)
            {
                fillingStage = false;
                while (reverseBfs.Count > 0)
                {
                    bfsQueue.Enqueue(reverseBfs.Pop());
                }
            }
        }

        private static (IEnumerable<string>, IEnumerable<string>) GetFilesDirectoriesEnumeration(string currentPath)
        {
            IEnumerable<string> directoryEnumeration = Enumerable.Empty<string>();
            try
            {
                directoryEnumeration = Directory.EnumerateDirectories(currentPath);
            }
            catch
            {
                Debug.WriteLine("Access not permitted");
            }

            IEnumerable<string> filesEnumeration = Enumerable.Empty<string>();
            try
            {
                filesEnumeration = Directory.EnumerateFiles(currentPath);
            }
            catch
            {
                Debug.WriteLine("Access not permitted");
            }
            return (filesEnumeration, directoryEnumeration);
        }


        private void ProcessPath(string currentPath)
        {
            var (files, directories) = GetFilesDirectoriesEnumeration(currentPath);

            var (bigFile, size, count) = (false, (long)0, (int)0);

            foreach (var directory in directories)
            {
                if (!details.ContainsKey(directory))
                {
                    Debug.WriteLine("The directory occured in the meantime.");
                }
                else
                {
                    var (localBigFile, localSize, localCount) = details[directory];
                    (bigFile, size, count) = (bigFile | localBigFile, localSize + size, count + localCount);
                }
            }

            foreach (var file in files)
            {
                var directoryInfo = new FileInfo(file);
                (bigFile, size, count) = (bigFile | (directoryInfo.Length > ThresholdFileSize), size + directoryInfo.Length, count + 1);
            }
            details[currentPath] = (bigFile, size, count);

            if (bigFile)
            {
                foundPaths.Add(currentPath);
                mainSyncContext.Post(state =>
                {
                    thisListener.NewFolderFound(new List<string> { currentPath + " " + (size / (1024 * 1024)) + "MB " + count + " files" });
                }, null);
            }
        }

        private void ProcessNextPath()
        {
            var currentPath = bfsQueue.Dequeue();
            var threadsCount = 1;

            List<Task> dispatchedTasks = new();

            dispatchedTasks.Add(Task.Run(() => ProcessPath(currentPath.Item1)));

            if (bfsQueue.Count > 0)
            {
                while (currentPath.Item2 == bfsQueue.Peek().Item2 && threadsCount < MAXThreadCount)
                {
                    var nextCurrentPath = bfsQueue.Dequeue();
                    dispatchedTasks.Add(Task.Run(() => ProcessPath(nextCurrentPath.Item1)));
                    threadsCount++;
                }
            }

            Debug.WriteLine(threadsCount + " Threads in the same time, remaining " + bfsQueue.Count);

            foreach (var task in dispatchedTasks)
            {
                task.Wait();
            }

        }
    }
}
