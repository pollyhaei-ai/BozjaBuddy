using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace BozjaBuddy.GUI.GUIExtension
{
    /// <summary>
    /// Holster trade window extension. The loadout miniview was removed with custom loadouts.
    /// Node fade/show for the cache grid stays in GUIAssistManager.
    /// </summary>
    internal class ExtGui_MycItemBagTrade : ExtGui
    {
        public override string mId { get; set; } = "extMycItemBagTrade";
        public override string mAddonName { get; set; } = "MYCItemBagTrade";
        private Plugin mPlugin;

        private ExtGui_MycItemBagTrade() { }
        public ExtGui_MycItemBagTrade(Plugin pPlugin)
        {
            this.mPlugin = pPlugin;
        }

        public override void Draw()
        {
        }
    }
}
