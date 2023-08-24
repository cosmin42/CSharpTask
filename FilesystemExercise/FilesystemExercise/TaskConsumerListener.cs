﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilesystemExercise
{
    public interface TaskConsumerListener
    {
        void paused();
        void stopped();
        void finished();

        void newFolderFound(string folderName);
    }
}
