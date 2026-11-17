using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using BozjaBuddy.Utils;
using BozjaBuddy.Data;
using Dalamud.Interface.Components;
using Dalamud.Interface;

namespace BozjaBuddy.GUI.Sections
{
    internal class GeneralSection : Section, IDisposable
    {
        protected override Plugin mPlugin { get; set; }
        private string mSearch = "";

        public GeneralSection(Plugin pPlugin)
        {
            this.mPlugin = pPlugin;
        }

        public override bool DrawGUI()
        {
            UtilsGUI.WindowLinkedButton(mPlugin, Plugin.ConfigWindowTitle, FontAwesomeIcon.Cog, "開啟設定。");
            ImGui.SameLine();
            string tSearchVal = this.mSearch;
            GeneralSection.DrawSearchAllBox(this.mPlugin, ref tSearchVal, pTextBoxWidth: ImGui.GetContentRegionAvail().X - 70);
            this.mSearch = tSearchVal;
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Question))
            {
                ImGui.OpenPopup("##helperpu");
            }
            else
            {
                UtilsGUI.SetTooltipForLastItem("帶有 » 的連結：懸停看簡介，左鍵在下方開啟詳細內容，右鍵可複製或開啟市價板。");
            }
            if (ImGui.BeginPopup("##helperpu", ImGuiWindowFlags.NoResize))
            {
                ImGui.PushTextWrapPos();
                ImGui.TextWrapped("這是 kaleidocli / K-Cli 的 Bozja Buddy 精簡台灣版（" + Plugin.Edition + "）。");
                ImGui.TextWrapped("保留：義軍武器、失傳技能／遺忘的碎片、戰地筆記、失傳技能持有欄覆蓋視窗。");
                ImGui.TextWrapped("技能、道具、戰地筆記與任務名稱在遊戲內由 Lumina 表讀取，會跟著客戶端語言。");
                ImGui.TextWrapped("持有欄覆蓋的節點路徑沿用國際服。台灣客戶端若不同，請在遊戲內確認。");
                ImGui.PopTextWrapPos();
                ImGui.EndPopup();
            }
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.ProjectDiagram, defaultColor: this.mPlugin.Configuration.isAuxiVisible == 0
                                                                                                         ? null
                                                                                                         : this.mPlugin.Configuration.isAuxiVisible == 1
                                                                                                            ? UtilsGUI.Colors.MycItemBoxOverlay_Red
                                                                                                            : UtilsGUI.AdjustTransparency(UtilsGUI.Colors.MycItemBoxOverlay_Red, 1f)))
            {
                this.mPlugin.Configuration.isAuxiVisible = this.mPlugin.Configuration.isAuxiVisible == 2 ? 0 : this.mPlugin.Configuration.isAuxiVisible + 1;
                this.mPlugin.Configuration.Save();
                this.mPlugin.MainWindow.RearrangeSection();
            }
            else
            {
                UtilsGUI.SetTooltipForLastItem($"詳細資料：{(this.mPlugin.Configuration.isAuxiVisible == 0
                                                                        ? "隱藏"
                                                                        : this.mPlugin.Configuration.isAuxiVisible == 1
                                                                            ? "分割"
                                                                            : "全畫面")}");
            }

            return true;
        }

        public static void DrawSearchAllBox(Plugin pPlugin, ref string pCurrValue, Vector2? pPUSize = null, float? pTextBoxWidth = null)
        {
            var tAnchor = ImGui.GetCursorScreenPos();

            if (pTextBoxWidth != null) ImGui.SetNextItemWidth(pTextBoxWidth.Value);
            ImGui.InputTextWithHint("", "搜尋技能、碎片、筆記…", ref pCurrValue, 200);
            bool tIsInputActive = ImGui.IsItemActive();
            bool tIsItemPUOpened = false;
            UtilsGUI.SetTooltipForLastItem("依名稱搜尋失傳技能、遺忘的碎片與戰地筆記。不區分大小寫。");

            if (pCurrValue.Length != 0 && tIsInputActive)
                ImGui.OpenPopup("##searchAllPU");

            {
                ImGui.SetNextWindowPos(tAnchor + new Vector2(0, 25));
                ImGui.SetNextWindowSizeConstraints(new Vector2(50, 25), pPUSize ?? new Vector2(360, 300));

                if (ImGui.BeginPopup("##searchAllPU", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.ChildWindow))
                {
                    foreach (GeneralObject o in pPlugin.mBBDataManager.mGeneralObjects.Values)
                    {
                        var salt = o.GetSalt();
                        if (salt != GeneralObject.GeneralObjectSalt.LostAction
                            && salt != GeneralObject.GeneralObjectSalt.Fragment
                            && salt != GeneralObject.GeneralObjectSalt.FieldNote)
                            continue;
                        if (!o.mName.Contains(pCurrValue, StringComparison.CurrentCultureIgnoreCase)) continue;
                        if (o.mTabColor.HasValue) ImGui.PushStyleColor(ImGuiCol.Text, o.mTabColor!.Value);
                        UtilsGUI.SelectableLink_WithPopup(pPlugin, o.mName, o.GetGenId(), pIsWrappedToText: false, pIsClosingPUOnClick: false);
                        if (o.mTabColor.HasValue) ImGui.PopStyleColor();
                        if (ImGui.IsPopupOpen(o.mName)) tIsItemPUOpened = true;
                    }
                    if (!tIsInputActive && !ImGui.IsWindowFocused() && !tIsItemPUOpened)
                    {
                        ImGui.CloseCurrentPopup();
                    }
                    ImGui.EndPopup();
                }
            }
        }

        public override void DrawGUIDebug() { }
        public override void Dispose() { }
    }
}
