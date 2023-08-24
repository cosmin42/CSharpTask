﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FilesystemExercise
{
    public class TaskConsumer
    {
        private const int ThresholdFileSize = 10 * 1024 * 1024;

        private const int MAXThreadCount = 4;

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
            stop = false;
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

        private (List<string> ExpansionPaths, List<string> ValidDirectories) ExamineSinglePath(string currentPath)
        {
            var newPaths = new List<string>();
            var newValidDirectories = new List<string>();

            if (!Path.Exists(currentPath))
            {
                Console.WriteLine("The path " + currentPath + " doesn't exist.");
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
                    Console.WriteLine("Access not permitted");
                }

                foreach (var file in directoryEnumeration)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.Length >= ThresholdFileSize)
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
                    Console.WriteLine("Access not permitted");
                }

                foreach (var folder in filesEnumeration)
                {
                    newPaths.Add(folder);
                }
            }
            else
            {
                Console.WriteLine("This path is not a directory ", currentPath);
            }

            return (ExpansionPaths: newPaths, ValidDirectories: newValidDirectories); ;
        }

        private void ExamineNextPath()
        {
            Task<(List<string> ExpansionPaths, List<string> ValidDirectories)>[] dispatchedTasks = new Task<(List<string> ExpansionPaths, List<string> ValidDirectories)>[Math.Min(examinationTasks.Count, MAXThreadCount)];

            for (var i = 0; i < MAXThreadCount && examinationTasks.Count > 0; ++i)
            {
                var currentPath = examinationTasks.Dequeue();

                Task<(List<string> ExpansionPaths, List<string> ValidDirectories)> t = Task.Run(() => ExamineSinglePath(currentPath));

                dispatchedTasks[i] = t;
            }

            Task.WaitAll(dispatchedTasks);

            foreach (var task in dispatchedTasks)
            {
                var (ExpansionPaths, ValidDirectories) = task.Result;
                searchResults.AddRange(ValidDirectories);

                foreach (var item in ExpansionPaths)
                {
                    examinationTasks.Enqueue(item);
                }

                mainSyncContext.Post(state =>
                {
                    thisListener.NewFolderFound(ValidDirectories);
                }, null);
            }
        }
    }
}
