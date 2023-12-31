﻿using System;
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
        void NewFolderFound((string, long, int) folderDetails);
        void Remove(string path);

    }
}
