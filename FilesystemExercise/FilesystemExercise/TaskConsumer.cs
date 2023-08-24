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

        private Queue<string> bfsQueue = new();
        private Stack<string> reverseBfs = new();
        private Queue<string> detailsQueue = new();
        private ConcurrentDictionary<string, (bool, long, int)> details = new();

        readonly TaskConsumerListener thisListener = null;

        readonly SynchronizationContext mainSyncContext;

        bool pause = false;
        bool stop = false;

        bool fillingStage = true;

        public TaskConsumer(string rootPath, TaskConsumerListener listener, SynchronizationContext returnThread)
        {
            thisListener = listener;
            bfsQueue.Enqueue(rootPath);
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

            while (!stop && (bfsQueue.Count > 0 || detailsQueue.Count > 0))
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
                directoryEnumeration = Directory.EnumerateDirectories(currentPath);
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
                    detailsQueue.Enqueue(reverseBfs.Pop());
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


        private (bool, long, int) ProcessPath(string currentPath)
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

            return (bigFile, size, count);
        }

        private void ProcessNextPath()
        {
            var currentPath = detailsQueue.Dequeue();
            var (bigFile, size, count) = ProcessPath(currentPath);
            if (bigFile)
            {
                mainSyncContext.Post(state =>
                {
                    thisListener.NewFolderFound(new List<string> { currentPath + " " + (size / (1024 * 1024)) + "MB " + count + " files" });
                }, null);
            }
        }
    }
}
