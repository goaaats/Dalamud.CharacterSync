namespace Dalamud.CharacterSync
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.RegularExpressions;
    using Dalamud.Game.ClientState;
    using Dalamud.Game.Command;
    using Dalamud.Hooking;
    using Dalamud.IoC;
    using Dalamud.Logging;
    using Dalamud.Plugin;
    using Dalamud.RichPresence.Config;
    using ImGuiNET;
    using ImGuiScene;

    // ReSharper disable once UnusedType.Global
    public class CharacterSyncPlugin : IDalamudPlugin
    {
        private bool isMainConfigWindowDrawing = false;
        private bool isSafeMode = false;
        private bool showRestartMessage = false;

        private TextureWrap warningTex;
        private CharacterSyncConfig config;

        // And we're never going to deallocate.
        // JUST IN CASE it gets saved in the game somewhere.
        private IntPtr lpFileName;

        [PluginService]
        private DalamudPluginInterface Interface { get; set; }

        [PluginService]
        private CommandManager Command { get; set; }

        [PluginService]
        private ClientState State { get; set; }

        public CharacterSyncPlugin()
        {
            this.config = this.Interface.GetPluginConfig() as CharacterSyncConfig ?? new CharacterSyncConfig();

            this.Interface.UiBuilder.Draw += this.UiBuilder_OnBuildUi;
            this.Interface.UiBuilder.OpenConfigUi += () => this.isMainConfigWindowDrawing = true;

            this.Command.AddHandler("/pcharsync",
                new CommandInfo((string cmd, string args) => this.isMainConfigWindowDrawing = true)
                {
                    HelpMessage = "Open the Character Sync configuration.",
                });

            this.lpFileName = Marshal.AllocHGlobal(260);
            this.createFileHook = Hook<CreateFileWDelegate>.FromSymbol("Kernel32", "CreateFileW", this.CreateFileWDetour, true);
            this.createFileHook.Enable();

            if (this.Interface.Reason == PluginLoadReason.Installer)
            {
                this.isSafeMode = true;

                PluginLog.Log("Installer, safe mode...");
            }
            else if (this.State.LocalPlayer != null)
            {
                this.warningTex = this.Interface.UiBuilder.LoadImage(Path.Combine(this.Interface.AssemblyLocation.Directory.FullName, "warningtex.png"));
                PluginLog.Log("Boot while logged in, safe mode...");

                this.isSafeMode = true;
                this.showRestartMessage = true;
            }

            try
            {
                this.DoBackup();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Could not backup character data files.");
            }
        }

        public delegate IntPtr CreateFileWDelegate(
            IntPtr lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        private readonly Hook<CreateFileWDelegate> createFileHook;

        private Regex saveFolderRegex = new(@"(.*)FFXIV_CHR(.*)\/(?!ITEMODR\.DAT|ITEMFDR\.DAT|GEARSET\.DAT|UISAVE\.DAT|.*\.log)(.*)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public IntPtr CreateFileWDetour(
            IntPtr lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile)
        {
            try
            {
                var filename = Marshal.PtrToStringUni(lpFileName);
                PluginLog.Information($"CharSyncHook: {filename}");

                if (this.config.Cid != 0)
                {
                    var match = this.saveFolderRegex.Match(filename);
                    if (match.Success)
                    {
                        var rootPath = match.Groups[1].Value;
                        var datName = match.Groups[3].Value;

                        switch (datName)
                        {
                            case "HOTBAR.DAT" when !this.config.SyncHotbars:
                            case "MACRO.DAT" when !this.config.SyncMacro:
                            case "KEYBIND.DAT" when !this.config.SyncKeybind:
                            case "LOGFLTR.DAT" when !this.config.SyncLogfilter:
                            case "COMMON.DAT" when !this.config.SyncCharSettings:
                            case "CONTROL0.DAT" when !this.config.SyncKeyboardSettings:
                            case "CONTROL1.DAT" when !this.config.SyncGamepadSettings:
                            case "GS.DAT" when !this.config.SyncCardSets:
                                goto breakout;
                        }

                        if (this.isSafeMode)
                        {
                            PluginLog.Information("SAFE MODE: " + filename);
                            goto breakout;
                        }

                        var newFilename = $"{rootPath}FFXIV_CHR{this.config.Cid:X16}/{datName}";
                        var newBytes = Encoding.Unicode.GetBytes(newFilename + "\0\0");
                        Marshal.Copy(newBytes, 0, this.lpFileName, newBytes.Length);

                        PluginLog.Information("REWRITE: " + filename);
                        return this.createFileHook.Original(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
                    }
                }

            breakout: { /* Do nothing */ }
            }
            catch (Exception ex)
            {
                PluginLog.LogError(ex, "ERROR in CreateFileWDetour");
            }

            return this.createFileHook.Original(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
        }

        private void UiBuilder_OnBuildUi()
        {
            if (this.showRestartMessage)
            {
                ImGui.SetNextWindowSize(new Vector2(600, 400));

                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
                if (ImGui.Begin(
                    "Character Sync Message",
                    ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoTitleBar))
                {
                    ImGui.Image(this.warningTex.ImGuiHandle, new Vector2(600, 400));
                }

                ImGui.End();

                ImGui.PopStyleVar();
            }

            if (this.isMainConfigWindowDrawing)
            {
                ImGui.SetNextWindowSize(new Vector2(750, 520));

                if (ImGui.Begin("Character Sync Config", ref this.isMainConfigWindowDrawing, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar))
                {
                    ImGui.Text("This window allows you to configure Character Sync.");
                    ImGui.Text(
                        "Click the button below while being logged in on your main character - all logins from now on will use this character's save data!");
                    ImGui.Text("None of your save data will be modified.");
                    ImGui.Text(
                        "Please note that it is recommended to restart your game after changing these settings.");
                    ImGui.Separator();

                    if (this.State.LocalPlayer == null)
                    {
                        ImGui.Text("Please log in before using this plugin.");
                    }
                    else
                    {
                        if (ImGui.Button("Set save data to current character"))
                        {
                            this.config.Cid = this.State.LocalContentId;
                            this.config.SetName = $"{this.State.LocalPlayer.Name} on {this.State.LocalPlayer.HomeWorld.GameData.Name}";
                            this.Interface.SavePluginConfig(this.config);
                            PluginLog.Log("CS saved.");
                        }

                        if (this.config.Cid == 0)
                        {
                            ImGui.Text("No character was set as main character yet.");
                            ImGui.Text("Please click the button above while being logged in on your main character.");
                        }
                        else
                        {
                            ImGui.Text($"Currently set to {this.config.SetName}(FFXIV_CHR{this.config.Cid:X16})");
                        }

                        ImGui.Dummy(new Vector2(5, 5));

                        ImGui.Text(
                            $"The logged in character is {this.State.LocalPlayer.Name} on {this.State.LocalPlayer.HomeWorld.GameData.Name}(FFXIV_CHR{this.State.LocalContentId:X16})");

                        ImGui.Dummy(new Vector2(20, 20));

                        ImGui.Checkbox("Sync Hotbars", ref this.config.SyncHotbars);
                        ImGui.Checkbox("Sync Macros", ref this.config.SyncMacro);
                        ImGui.Checkbox("Sync Keybinds", ref this.config.SyncKeybind);
                        ImGui.Checkbox("Sync Chatlog Settings", ref this.config.SyncLogfilter);
                        ImGui.Checkbox("Sync Character Settings", ref this.config.SyncCharSettings);
                        ImGui.Checkbox("Sync Keyboard Settings", ref this.config.SyncKeyboardSettings);
                        ImGui.Checkbox("Sync Gamepad Settings", ref this.config.SyncGamepadSettings);
                        ImGui.Checkbox("Sync Card Sets and Verminion Settings", ref this.config.SyncCardSets);
                    }

                    ImGui.Separator();

                    if (ImGui.Button("Save"))
                    {
                        this.isMainConfigWindowDrawing = false;
                        this.Interface.SavePluginConfig(this.config);
                        PluginLog.Log("CS saved.");
                    }
                }

                ImGui.End();
            }
        }

        private void DoBackup()
        {
            var configFolder = this.Interface.GetPluginConfigDirectory();
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

        /// <inheritdoc/>
        public string Name => "Character Sync";

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Interface.UiBuilder.Draw -= this.UiBuilder_OnBuildUi;
            this.createFileHook.Dispose();
            this.warningTex?.Dispose();
            this.Command.RemoveHandler("/pcharsync");
        }
    }
}