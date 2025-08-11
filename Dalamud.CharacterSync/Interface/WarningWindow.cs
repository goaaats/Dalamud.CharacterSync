using System;
using System.IO;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace Dalamud.CharacterSync.Interface
{
    /// <summary>
    /// A window that shows warning messages.
    /// </summary>
    internal class WarningWindow : Window, IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WarningWindow"/> class.
        /// </summary>
        public WarningWindow()
            : base("Character Sync Message", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoTitleBar)
        {
            this.Size = new Vector2(650, 400);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public override void PreDraw()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
        }

        /// <inheritdoc/>
        public override void PostDraw()
        {
            ImGui.PopStyleVar();
            ImGui.PopStyleColor();
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            ImGui.SetWindowFontScale(2.5f);

            TextCentered("HEY.");
            TextCentered("Dalamud.CharacterDataSync");
            TextCentered("has been freshly installed or updated");
            TextCentered("and is not loaded correctly.");
            TextCentered("Please confirm the plugin's settings");
            TextCentered("and restart your game!");

            ImGui.SetWindowFontScale(1.5f);

            TextCentered("\n");
            TextCentered("only then, will it work :)");

            ImGui.SetWindowFontScale(1.0f);

            TextCentered("\n");
            TextCentered("This message will not go away until then");

            ImGui.SetWindowFontScale(1.0f);

            void TextCentered(string text)
            {
                ImGui.SetCursorPosX((ImGui.GetWindowSize().X - ImGui.CalcTextSize(text).X) * 0.5f);
                ImGui.Text(text);
            }
        }
    }
}
