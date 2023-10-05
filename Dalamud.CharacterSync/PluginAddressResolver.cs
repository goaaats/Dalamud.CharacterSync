using System;

using Dalamud.Game;
using Dalamud.Logging;

namespace Dalamud.CharacterSync
{
    /// <summary>
    /// Plugin address resolver.
    /// </summary>
    internal class PluginAddressResolver : BaseAddressResolver
    {
        private const string FileInterfaceOpenFileSignature = "E8 ?? ?? ?? ?? 48 8D 4C 24 ?? 3C 01 74 0A";

        /// <summary>
        /// Gets the address of the FileInterface::OpenFile method.
        /// </summary>
        public IntPtr FileInterfaceOpenFileAddress { get; private set; }

        /// <inheritdoc/>
        protected override void Setup64Bit(ISigScanner scanner)
        {
            this.FileInterfaceOpenFileAddress = scanner.ScanText(FileInterfaceOpenFileSignature);

            PluginLog.Verbose("===== CHARACTER SYNC =====");
            PluginLog.Verbose($"{nameof(this.FileInterfaceOpenFileAddress)} {this.FileInterfaceOpenFileAddress:X}");
        }
    }
}
