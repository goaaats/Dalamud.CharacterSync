using System;
using System.IO;
using System.Numerics;

using Dalamud.Interface.Windowing;
using ImGuiNET;

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
            this.Size = new Vector2(600, 400);
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
            ImGui.SetWindowFontScale(4.0f);

            TextCentered("HEY.");
            TextCentered("Please set up");
            TextCentered("Character Data Sync");
            TextCentered("and restart your game!");

            ImGui.SetWindowFontScale(2.0f);

            TextCentered("only then, it will work :)");

            ImGui.SetWindowFontScale(1.0f);

            void TextCentered(string text)
            {
                ImGui.SetCursorPosX((ImGui.GetWindowSize().X - ImGui.CalcTextSize(text).X) * 0.5f);
                ImGui.Text(text);
            }
        }
    }
}
