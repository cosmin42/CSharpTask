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

        public TaskConsumer(string rootPath, TaskConsumerListener listener)
        {
            thisListener = listener;
            tasks.Enqueue(rootPath);
        }

        public void Start() { }

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

        private void examineNextPath()
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
                        thisListener.newFolderFound(currentPath);
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
