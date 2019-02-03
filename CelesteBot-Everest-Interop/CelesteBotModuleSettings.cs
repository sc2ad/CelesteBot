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
        public bool ShowGraph { get; set; } = true;
        public bool ShowTarget { get; set; } = true;
        [SettingRange(2, 25)]
        public int GenerationsToSaveForGraph { get; set; } = 5;
        [SettingRange(10, 100), SettingNeedsRelaunch()]
        public int OrganismsPerGeneration { get; set; } = 30;
        [SettingRange(1, 10), SettingNeedsRelaunch()]
        public int WeightMaximum { get; set; } = 5;
        [SettingRange(2,50)]
        public int UpdateTargetThreshold { get; set; } = 8;
        [SettingRange(0, 20)]
        public int TargetReachedRewardFitness { get; set; } = 2;
        public bool ShowBestFitness { get; set; } = true;
        [SettingRange(1, 25)]
        public int CheckpointInterval { get; set; } = 3;
        [SettingRange(0, 500)]
        public int CheckpointToLoad { get; set; } = 20;
        [SettingRange(5, 50)]
        public int MaxTalkAttempts { get; set; } = 30;
        [SettingRange(60, 240)]
        public int TalkFrameBuffer { get; set; } = 100;
        [SettingRange(4, 30), SettingNeedsRelaunch()]
        public int XVisionSize { get; set; } = 10;
        [SettingRange(4, 30), SettingNeedsRelaunch()]
        public int YVisionSize { get; set; } = 10;
        [SettingRange(50, 100), SettingNeedsRelaunch()]
        public int ActionThreshold { get; set; } = 55;
        [SettingRange(1, 100), SettingNeedsRelaunch()]
        public int ReRandomizeWeightChance { get; set; } = 20;
        [SettingRange(1, 100), SettingNeedsRelaunch()]
        public int MutateWeight { get; set; } = 65;
        [SettingRange(1, 100), SettingNeedsRelaunch()]
        public int AddConnectionChance { get; set; } = 55;
        [SettingRange(1, 100), SettingNeedsRelaunch()]
        public int AddNodeChance { get; set; } = 15;
        [SettingRange(1, 100)]
        public int QLearningRate { get; set; } = 80;
        [SettingRange(1, 100)]
        public int QGamma { get; set; } = 95;
        [SettingRange(1, 100)]
        public int MinQEpsilon { get; set; } = 10;
        [SettingRange(1, 100)]
        public int MaxQEpsilon { get; set; } = 100;
        [SettingRange(1, 10000)]
        public int QEpsilonDecay { get; set; } = 50; // Decays to minimum over this many iterations
        [SettingRange(1, 1000)]
        public int QGraphIterations { get; set; } = 50;
    }
}
