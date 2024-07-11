using System.Numerics;

using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using ImGuiNET;

namespace Dalamud.CharacterSync.Interface
{
    /// <summary>
    /// Main configuration window.
    /// </summary>
    internal class ConfigWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigWindow"/> class.
        /// </summary>
        public ConfigWindow()
            : base("Character Sync Config", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar)
        {
            this.Size = new Vector2(750, 520);
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            ImGui.Text("This window allows you to configure Character Sync.");
            ImGui.Text("Click the button below while being logged in on your main character - all logins from now on will use this character's save data!");
            ImGui.Text("None of your save data will be modified.");
            ImGui.Text("Please note that it is recommended to restart your game after changing these settings.");
            ImGui.Separator();

            if (Service.ClientState.LocalPlayer == null)
            {
                ImGui.Text("Please log in before using this plugin.");
                return;
            }

            if (ImGui.Button("Set save data to current character"))
            {
                Service.Configuration.Cid = Service.ClientState.LocalContentId;
                Service.Configuration.SetName = $"{Service.ClientState.LocalPlayer.Name} on {Service.ClientState.LocalPlayer.HomeWorld.GameData.Name}";
                Service.Configuration.Save();
                CharacterSyncPlugin.PluginLog.Info("CS saved.");
            }

            if (Service.Configuration.Cid == 0)
            {
                ImGui.Text("No character was set as main character yet.");
                ImGui.Text("Please click the button above while being logged in on your main character.");
            }
            else
            {
                ImGui.Text($"Currently set to {Service.Configuration.SetName}(FFXIV_CHR{Service.Configuration.Cid:X16})");
            }

            ImGui.Dummy(new Vector2(5, 5));

            ImGui.Text($"The logged in character is {Service.ClientState.LocalPlayer.Name} on {Service.ClientState.LocalPlayer.HomeWorld.GameData.Name}(FFXIV_CHR{Service.ClientState.LocalContentId:X16})");

            ImGui.Dummy(new Vector2(20, 20));

            ImGui.Checkbox("Sync Hotbars", ref Service.Configuration.SyncHotbars);
            ImGui.Checkbox("Sync Macros", ref Service.Configuration.SyncMacro);
            ImGui.Checkbox("Sync Keybinds", ref Service.Configuration.SyncKeybind);
            ImGui.Checkbox("Sync Chatlog Settings", ref Service.Configuration.SyncLogfilter);
            ImGui.Checkbox("Sync Character Settings", ref Service.Configuration.SyncCharSettings);
            ImGui.Checkbox("Sync Keyboard Settings", ref Service.Configuration.SyncKeyboardSettings);
            ImGui.Checkbox("Sync Gamepad Settings", ref Service.Configuration.SyncGamepadSettings);
            ImGui.Checkbox("Sync Card Sets and Verminion Settings", ref Service.Configuration.SyncCardSets);

            ImGui.Separator();

            if (ImGui.Button("Save"))
            {
                this.IsOpen = false;
                Service.Configuration.Save();
                CharacterSyncPlugin.PluginLog.Information("CS saved.");
            }
        }
    }
}
