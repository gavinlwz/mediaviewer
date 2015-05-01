﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaViewer.Model.Media.State
{
    public enum MediaStateChangedAction
    {
        Add,
        Remove,
        Clear,
        Modified,
        Replace
    }
}
