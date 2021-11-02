using System;
using System.IO;
using System.Numerics;

using Dalamud.Interface.Windowing;
using ImGuiNET;
using ImGuiScene;

namespace Dalamud.CharacterSync.Interface
{
    /// <summary>
    /// A window that shows warning messages.
    /// </summary>
    internal class WarningWindow : Window, IDisposable
    {
        private readonly TextureWrap warningTex;

        /// <summary>
        /// Initializes a new instance of the <see cref="WarningWindow"/> class.
        /// </summary>
        public WarningWindow()
            : base("Character Sync Message", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoTitleBar)
        {
            this.Size = new Vector2(600, 400);

            var warningTexPath = Path.Combine(Service.Interface.AssemblyLocation.Directory.FullName, "warningtex.png");
            this.warningTex = Service.Interface.UiBuilder.LoadImage(warningTexPath);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.warningTex?.Dispose();
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
            ImGui.Image(this.warningTex.ImGuiHandle, new Vector2(600, 400));
        }
    }
}
