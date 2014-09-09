﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaViewer.Model.Media.File
{
    public enum MediaFileItemState
    {
        DUMMY,
        EMPTY,
        LOADING,
        LOADED,
        CANCELLED,
        TIMED_OUT,
        FILE_NOT_FOUND,
        DELETED,
        ERROR
    }
}
