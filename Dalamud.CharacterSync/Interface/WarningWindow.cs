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
        }

        /// <inheritdoc/>
        public override void PostDraw()
        {
            ImGui.PopStyleVar();
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            // TODO: stylize this to be like warningtex.png
            ImGui.Text("Hey! Please set up character data sync and restart your game! Thanks!");
        }
    }
}
