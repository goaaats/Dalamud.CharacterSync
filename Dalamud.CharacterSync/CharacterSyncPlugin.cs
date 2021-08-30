﻿using System;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
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

namespace Dalamud.CharacterSync
{
    // ReSharper disable once UnusedType.Global
    public class CharacterSyncPlugin : IDalamudPlugin
    {
        private bool _isMainConfigWindowDrawing = false;
        private bool _isSafeMode = false;
        private bool _showRestartMessage = false;

        private TextureWrap _warningTex;

        private CharacterSyncConfig Config;

        [PluginService]
        private DalamudPluginInterface Interface { get; set; }

        [PluginService]
        private CommandManager Command { get; set; }

        [PluginService]
        private ClientState State { get; set; }

        public CharacterSyncPlugin()
        {
            Config = Interface.GetPluginConfig() as CharacterSyncConfig ?? new CharacterSyncConfig();

            Interface.UiBuilder.Draw += UiBuilder_OnBuildUi;
            Interface.UiBuilder.OpenConfigUi += () => _isMainConfigWindowDrawing = true;

            Command.AddHandler("/pcharsync",
                new CommandInfo((string cmd, string args) => _isMainConfigWindowDrawing = true)
                {
                    HelpMessage = "Open the Character Sync configuration."
                });

            this._createFileHook = Hook<CreateFileWDelegate>.FromSymbol("Kernel32", "CreateFileW", this.CreateFileWDetour);
            this._createFileHook.Enable();

            if (Interface.Reason == PluginLoadReason.Installer)
            {
                _isSafeMode = true;

                PluginLog.Log("Installer, safe mode...");
            }

            if (Interface.Reason == PluginLoadReason.Boot && State.LocalPlayer != null)
            {
                _isSafeMode = true;
                _showRestartMessage = true;

                _warningTex = Interface.UiBuilder.LoadImage(Path.Combine(
                    Path.GetDirectoryName(Assembly.GetAssembly(typeof(CharacterSyncPlugin)).Location), "warningtex.png"));
                PluginLog.Log("Boot while logged in, safe mode...");
            }
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

        private Regex saveFolderRegex = new Regex(@"(.*)FFXIV_CHR(.*)\/(?!ITEMODR\.DAT|ITEMFDR\.DAT|GEARSET\.DAT|UISAVE\.DAT|.*\.log)(.*)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

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

                        if (_isSafeMode)
                        {
                            PluginLog.Log("SAFE MODE: " + filename);
                            goto breakout;
                        }

                        filename = $"{match.Groups[1].Value}FFXIV_CHR{Config.Cid:X16}/{match.Groups[3].Value}";
                        //PluginLog.Log("REWRITE: " + filename);
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
            if (_showRestartMessage)
            {
                ImGui.SetNextWindowSize(new Vector2(600, 400));

                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
                if (this._showRestartMessage && ImGui.Begin("Character Sync Message",
                    ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar |
                    ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoTitleBar))
                    ImGui.Image(this._warningTex.ImGuiHandle, new Vector2(600, 400));
                ImGui.PopStyleVar();
            }

            if (_isMainConfigWindowDrawing) {
                ImGui.SetNextWindowSize(new Vector2(750, 520));

            if (_isMainConfigWindowDrawing && ImGui.Begin("Character Sync Config", ref _isMainConfigWindowDrawing,
                    ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar))
            {
                ImGui.Text("This window allows you to configure Character Sync.");
                ImGui.Text("Click the button below while being logged in on your main character - all logins from now on will use this character's save data!");
                ImGui.Text("None of your save data will be modified.");
                ImGui.Text("Please note that it is recommended to restart your game after changing these settings.");
                ImGui.Separator();

                if (State.LocalPlayer == null)
                {
                    ImGui.Text("Please log in before using this plugin.");
                }
                else
                {
                    if (ImGui.Button("Set save data to current character"))
                    {
                        Config.Cid = State.LocalContentId;
                        Config.SetName = $"{State.LocalPlayer.Name} on {State.LocalPlayer.HomeWorld.GameData.Name}";
                        Interface.SavePluginConfig(Config);
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

                    ImGui.Dummy(new Vector2(5, 5));

                    ImGui.Text($"The logged in character is {State.LocalPlayer.Name} on {State.LocalPlayer.HomeWorld.GameData.Name}(FFXIV_CHR{State.LocalContentId:X16})");

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
                    Interface.SavePluginConfig(Config);
                    PluginLog.Log("CS saved.");
                }

                ImGui.End();
            }
            }
        }

        public string Name => "Character Sync";

        public void Dispose()
        {
            Interface.UiBuilder.Draw -= this.UiBuilder_OnBuildUi;
            _createFileHook.Dispose();
            _warningTex?.Dispose();
            Command.RemoveHandler("/pcharsync");
        }
    }
}
