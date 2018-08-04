using Celeste.Mod;
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
        [SettingRange(1, 10)]
        public int TimeStuckThreshold { get; set; } = 4;
        public bool ShowDetailedPlayerInfo { get; set; } = true;
        public bool ShowPlayerBrain { get; set; } = true;
        public bool ShowPlayerFitness { get; set; } = true;

    }
}
