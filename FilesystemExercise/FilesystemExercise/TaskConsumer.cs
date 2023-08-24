using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilesystemExercise
{
    public class TaskConsumer
    {
        Queue<string> tasks = new();

        bool pause = false;
        bool stop = false;

        public TaskConsumer(string rootPath)
        {
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


    }
}
