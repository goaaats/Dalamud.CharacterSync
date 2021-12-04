using System.Diagnostics.CodeAnalysis;

using Dalamud.CharacterSync;
using Dalamud.Configuration;

namespace Dalamud.RichPresence.Config
{
    /// <summary>
    /// Plugin configuration.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Personal choice.")]
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Easier ImGui ref usage.")]
    public class CharacterSyncConfig : IPluginConfiguration
    {
        /// <inheritdoc/>
        public int Version { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value indicating whether the plugin is enabled.
        /// </summary>
        public bool IsEnabled = true;

        /// <summary>
        /// Gets or sets a value indicating whether to sync the HOTBAR.DAT file.
        /// </summary>
        public bool SyncHotbars = true;

        /// <summary>
        /// Gets or sets a value indicating whether to sync the MACRO.DAT file.
        /// </summary>
        public bool SyncMacro = true;

        /// <summary>
        /// Gets or sets a value indicating whether to sync the KEYBIND.DAT file.
        /// </summary>
        public bool SyncKeybind = true;

        /// <summary>
        /// Gets or sets a value indicating whether to sync the LOGFLTR.DAT file.
        /// </summary>
        public bool SyncLogfilter = true;

        /// <summary>
        /// Gets or sets a value indicating whether to sync the COMMON.DAT file.
        /// </summary>
        public bool SyncCharSettings = true;

        /// <summary>
        /// Gets or sets a value indicating whether to sync the CONTROL0.DAT file.
        /// </summary>
        public bool SyncKeyboardSettings = true;

        /// <summary>
        /// Gets or sets a value indicating whether to sync the CONTROL1.DAT file.
        /// </summary>
        public bool SyncGamepadSettings = true;

        /// <summary>
        /// Gets or sets a value indicating whether to sync the GS.DAT file.
        /// </summary>
        public bool SyncCardSets = true;

        /// <summary>
        /// Gets or sets the Character ID, the value after CHR_*.
        /// </summary>
        public ulong Cid;

        /// <summary>
        /// Gets or sets the "set name".
        /// </summary>
        public string SetName;

        /// <summary>
        /// Save the configuration.
        /// </summary>
        public void Save() => Service.Interface.SavePluginConfig(this);
    }
}
