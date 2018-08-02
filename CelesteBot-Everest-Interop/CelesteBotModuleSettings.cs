﻿using Celeste.Mod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelesteBot_Everest_Interop
{
    public class CelesteBotModuleSettings : EverestModuleSettings
    {
        public bool Enabled { get; set; } = true;
        public bool DrawAlways { get; set; } = true;
        public int TimeStuckThreshold { get; set; } = 4;
    }
}
