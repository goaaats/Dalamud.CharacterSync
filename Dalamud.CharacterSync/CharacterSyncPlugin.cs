using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using Dalamud.CharacterSync.Interface;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.RichPresence.Config;

namespace Dalamud.CharacterSync
{
    /// <summary>
    /// Main plugin class.
    /// </summary>
    internal class CharacterSyncPlugin : IDalamudPlugin
    {
        private readonly WindowSystem windowSystem;
        private readonly ConfigWindow configWindow;
        private readonly WarningWindow warningWindow;

        private readonly bool isSafeMode = false;

        private readonly Hook<FileInterfaceOpenFileDelegate> openFileHook;
        private readonly Regex saveFolderRegex = new(
            @"(?<path>.*)FFXIV_CHR(?<cid>.*)\/(?!ITEMODR\.DAT|ITEMFDR\.DAT|GEARSET\.DAT|UISAVE\.DAT|.*\.log)(?<dat>.*)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterSyncPlugin"/> class.
        /// </summary>
        /// <param name="pluginInterface">Dalamud plugin interface.</param>
        public CharacterSyncPlugin(DalamudPluginInterface pluginInterface)
        {
            pluginInterface.Create<Service>();

            Service.Configuration = Service.Interface.GetPluginConfig() as CharacterSyncConfig ?? new CharacterSyncConfig();

            this.configWindow = new();
            this.warningWindow = new();
            this.windowSystem = new("CharacterSync");
            this.windowSystem.AddWindow(this.configWindow);
            this.windowSystem.AddWindow(this.warningWindow);

            Service.Interface.UiBuilder.Draw += this.windowSystem.Draw;
            Service.Interface.UiBuilder.OpenConfigUi += this.OnOpenConfigUi;

            Service.CommandManager.AddHandler("/pcharsync", new CommandInfo(this.OnChatCommand)
            {
                HelpMessage = "Open the Character Sync configuration.",
                ShowInHelp = true,
            });

            if (Service.Interface.Reason == PluginLoadReason.Installer)
            {
                PluginLog.Warning("Installer, safe mode...");
                this.isSafeMode = true;
            }
            else if (Service.ClientState.LocalPlayer != null)
            {
                PluginLog.Warning("Boot while logged in, safe mode...");
                this.isSafeMode = true;

                this.warningWindow.IsOpen = true;
            }

            try
            {
                this.DoBackup();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Could not backup character data files.");
            }

            var address = new PluginAddressResolver();
            address.Setup();

            this.openFileHook = new Hook<FileInterfaceOpenFileDelegate>(address.FileInterfaceOpenFileAddress, this.OpenFileDetour);
            this.openFileHook.Enable();
        }

        private delegate IntPtr FileInterfaceOpenFileDelegate(
            IntPtr pFileInterface,
            [MarshalAs(UnmanagedType.LPWStr)] string filepath, // IntPtr pFilepath
            uint a3);

        /// <inheritdoc/>
        public string Name => "Character Sync";

        /// <inheritdoc/>
        public void Dispose()
        {
            Service.CommandManager.RemoveHandler("/pcharsync");
            Service.Interface.UiBuilder.Draw -= this.windowSystem.Draw;
            this.warningWindow?.Dispose();
            this.openFileHook?.Dispose();
        }

        private void OnOpenConfigUi()
        {
            this.configWindow.Toggle();
        }

        private void OnChatCommand(string command, string arguments)
        {
            this.configWindow.Toggle();
        }

        private void DoBackup()
        {
            var configFolder = Service.Interface.GetPluginConfigDirectory();
            Directory.CreateDirectory(configFolder);

            var backupFolder = new DirectoryInfo(Path.Combine(configFolder, "backups"));
            Directory.CreateDirectory(backupFolder.FullName);

            var folders = backupFolder.GetDirectories().OrderBy(x => long.Parse(x.Name)).ToArray();
            if (folders.Length > 2)
            {
                folders.FirstOrDefault()?.Delete(true);
            }

            var thisBackupFolder = Path.Combine(backupFolder.FullName, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
            Directory.CreateDirectory(thisBackupFolder);

            var xivFolder = new DirectoryInfo(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "My Games",
                "FINAL FANTASY XIV - A Realm Reborn"));

            if (!xivFolder.Exists)
            {
                PluginLog.Error("Could not find XIV folder.");
                return;
            }

            foreach (var directory in xivFolder.GetDirectories("FFXIV_CHR*"))
            {
                var thisBackupFile = Path.Combine(thisBackupFolder, directory.Name);
                PluginLog.Information(thisBackupFile);
                Directory.CreateDirectory(thisBackupFile);

                foreach (var filePath in directory.GetFiles("*.DAT"))
                {
                    File.Copy(filePath.FullName, filePath.FullName.Replace(directory.FullName, thisBackupFile), true);
                }
            }

            PluginLog.Information("Backup OK!");
        }

        private IntPtr OpenFileDetour(IntPtr pFileInterface, [MarshalAs(UnmanagedType.LPWStr)] string filepath, uint a3)
        {
            try
            {
                if (Service.Configuration.Cid != 0)
                {
                    var match = this.saveFolderRegex.Match(filepath);
                    if (match.Success)
                    {
                        var rootPath = match.Groups["path"].Value;
                        var datName = match.Groups["dat"].Value;

                        if (this.isSafeMode)
                        {
                            PluginLog.Information($"SAFE MODE: {filepath}");
                        }
                        else if (this.PerformRewrite(datName))
                        {
                            filepath = $"{rootPath}FFXIV_CHR{Service.Configuration.Cid:X16}/{datName}";
                            PluginLog.Debug("REWRITE: " + filepath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.LogError(ex, "ERROR in OpenFileDetour");
            }

            return this.openFileHook.Original(pFileInterface, filepath, a3);
        }

        private bool PerformRewrite(string datName)
        {
            switch (datName)
            {
                case "HOTBAR.DAT" when Service.Configuration.SyncHotbars:
                case "MACRO.DAT" when Service.Configuration.SyncMacro:
                case "KEYBIND.DAT" when Service.Configuration.SyncKeybind:
                case "LOGFLTR.DAT" when Service.Configuration.SyncLogfilter:
                case "COMMON.DAT" when Service.Configuration.SyncCharSettings:
                case "CONTROL0.DAT" when Service.Configuration.SyncKeyboardSettings:
                case "CONTROL1.DAT" when Service.Configuration.SyncGamepadSettings:
                case "GS.DAT" when Service.Configuration.SyncCardSets:
                case "ADDON.DAT":
                    return true;
            }

            return false;
        }
    }
}