using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Configuration;

namespace Dalamud.RichPresence.Config
{
    class CharacterSyncConfig : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public bool IsEnabled = true;

        public bool SyncHotbars = true;
        public bool SyncMacro = true;
        public bool SyncKeybind = true;
        public bool SyncLogfilter = true;
        public bool SyncCharSettings = true;
        public bool SyncKeyboardSettings = true;
        public bool SyncGamepadSettings = true;
        public bool SyncCardSets = true;

        public ulong Cid;
        public string SetName;
    }
}
