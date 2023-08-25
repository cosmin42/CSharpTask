using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilesystemExercise
{
    public interface TaskConsumerListener
    {
        void Started();
        void Resumed();
        void Paused();
        void Stopped();
        void Finished();
        void NewFolderFound(List<string> folderName);
        void Replace(string oldPath, string newPath);


    }
}
