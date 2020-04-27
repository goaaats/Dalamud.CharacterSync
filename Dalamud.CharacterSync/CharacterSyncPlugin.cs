using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Plugin;
using Dalamud.RichPresence.Config;
using EasyHook;
using ImGuiNET;

namespace Dalamud.CharacterSync
{
    // ReSharper disable once UnusedType.Global
    public class CharacterSyncPlugin : IDalamudPlugin
    {
        private DalamudPluginInterface _pi;

        private bool _isMainConfigWindowDrawing = false;

        private CharacterSyncConfig Config;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            _pi = pluginInterface;

            Config = pluginInterface.GetPluginConfig() as CharacterSyncConfig ?? new CharacterSyncConfig();

            _pi.UiBuilder.OnBuildUi += UiBuilder_OnBuildUi;
            _pi.UiBuilder.OnOpenConfigUi += (sender, args) => _isMainConfigWindowDrawing = true;

            _pi.CommandManager.AddHandler("/pcharsync",
                new CommandInfo((string cmd, string args) => _isMainConfigWindowDrawing = true)
                {
                    HelpMessage = "Open the Character Sync configuration."
                });

            this._createFileHook = new Hook<CreateFileWDelegate>(LocalHook.GetProcAddress("Kernel32", "CreateFileW"), new CreateFileWDelegate(CreateFileWDetour));
            this._createFileHook.Enable();
        }

        public delegate IntPtr CreateFileWDelegate(
            [MarshalAs(UnmanagedType.LPWStr)] string filename,
            [MarshalAs(UnmanagedType.U4)] FileAccess access,
            [MarshalAs(UnmanagedType.U4)] FileShare share,
            IntPtr securityAttributes,
            [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
            [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
            IntPtr templateFile);

        public Hook<CreateFileWDelegate> _createFileHook;

        private Regex saveFolderRegex = new Regex(@"(.*)FFXIV_CHR(.*)\/(?!ITEMODR\.DAT|ITEMFDR\.DAT|GEARSET\.DAT|.*\.log)(.*)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public IntPtr CreateFileWDetour(
            [MarshalAs(UnmanagedType.LPWStr)] string filename,
            [MarshalAs(UnmanagedType.U4)] FileAccess access,
            [MarshalAs(UnmanagedType.U4)] FileShare share,
            IntPtr securityAttributes,
            [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
            [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
            IntPtr templateFile)
        {
            try
            {
                if (Config.Cid != 0)
                {
                    var match = this.saveFolderRegex.Match(filename);
                    if (match.Success)
                    {
                        if (!Config.SyncHotbars && match.Groups[3].Value == "HOTBAR.DAT")
                            goto breakout;

                        if (!Config.SyncMacro && match.Groups[3].Value == "MACRO.DAT")
                            goto breakout;

                        if (!Config.SyncKeybind && match.Groups[3].Value == "KEYBIND.DAT")
                            goto breakout;

                        if (!Config.SyncLogfilter && match.Groups[3].Value == "LOGFLTR.DAT")
                            goto breakout;

                        if (!Config.SyncCharSettings && match.Groups[3].Value == "COMMON.DAT")
                            goto breakout;

                        if (!Config.SyncKeyboardSettings && match.Groups[3].Value == "CONTROL0.DAT")
                            goto breakout;

                        if (!Config.SyncGamepadSettings && match.Groups[3].Value == "CONTROL1.DAT")
                            goto breakout;

                        if (!Config.SyncGamepadSettings && match.Groups[3].Value == "CONTROL1.DAT")
                            goto breakout;

                        if (!Config.SyncCardSets && match.Groups[3].Value == "GS.DAT")
                            goto breakout;

                        filename = $"{match.Groups[1].Value}FFXIV_CHR{Config.Cid:X16}/{match.Groups[3].Value}";
                        PluginLog.Log("REWRITE: " + filename);
                    }
                }

                breakout:
                return this._createFileHook.Original(filename, access, share, securityAttributes, creationDisposition,
                    flagsAndAttributes, templateFile);
            }
            catch (Exception ex)
            {
                PluginLog.LogError(ex, "ERROR in CreateFileWDetour");
                return _createFileHook.Original(filename, access, share, securityAttributes, creationDisposition,
                    flagsAndAttributes, templateFile);
            }
        }

        private void UiBuilder_OnBuildUi()
        {
            ImGui.SetNextWindowSize(new Vector2(750, 520));

            if (_isMainConfigWindowDrawing && ImGui.Begin("Character Sync Config", ref _isMainConfigWindowDrawing,
                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar))
            {
                ImGui.Text("This window allows you to configure Character Sync.");
                ImGui.Text("Click the button below while being logged in on your main character - all logins from now on will use this character's save data!");
                ImGui.Text("None of your save data will be modified.");
                ImGui.Text("Please note that it is recommended to restart your game after changing these settings.");
                ImGui.Separator();

                if (_pi.ClientState.LocalPlayer == null)
                {
                    ImGui.Text("Please log in before using this plugin.");
                }
                else
                {
                    if (ImGui.Button("Set save data to current character"))
                    {
                        Config.Cid = _pi.ClientState.LocalContentId;
                        Config.SetName = $"{_pi.ClientState.LocalPlayer.Name} on {_pi.ClientState.LocalPlayer.HomeWorld.GameData.Name}";
                        _pi.SavePluginConfig(Config);
                        PluginLog.Log("CS saved.");
                    }

                    if (Config.Cid == 0)
                    {
                        ImGui.Text("No character was set as main character yet.");
                        ImGui.Text("Please click the button above while being logged in on your main character.");
                    }
                    else
                    {
                        ImGui.Text($"Currently set to {Config.SetName}(FFXIV_CHR{Config.Cid:X16})");
                    }

                    ImGui.Dummy(new Vector2(20, 20));

                    ImGui.Checkbox("Sync Hotbars", ref Config.SyncHotbars);
                    ImGui.Checkbox("Sync Macros", ref Config.SyncMacro);
                    ImGui.Checkbox("Sync Keybinds", ref Config.SyncKeybind);
                    ImGui.Checkbox("Sync Chatlog Settings", ref Config.SyncLogfilter);
                    ImGui.Checkbox("Sync Character Settings", ref Config.SyncCharSettings);
                    ImGui.Checkbox("Sync Keyboard Settings", ref Config.SyncKeyboardSettings);
                    ImGui.Checkbox("Sync Gamepad Settings", ref Config.SyncGamepadSettings);
                    ImGui.Checkbox("Sync Card Sets and Verminion Settings", ref Config.SyncCardSets);
                }

                ImGui.Separator();

                if (ImGui.Button("Save"))
                {
                    _isMainConfigWindowDrawing = false;
                    _pi.SavePluginConfig(Config);
                    PluginLog.Log("CS saved.");
                }

                ImGui.End();
            }
        }

        public string Name => "Character Sync";

        public void Dispose()
        {
            _createFileHook.Dispose();
            _pi.CommandManager.RemoveHandler("/pcharsync");
            _pi.Dispose();
        }
    }
}
