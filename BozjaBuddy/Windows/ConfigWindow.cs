using System.Numerics;
using BozjaBuddy.GUI.GUIAssist;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using static BozjaBuddy.GUI.GUIAssist.GUIAssistManager;
using BozjaBuddy.Utils;

namespace BozjaBuddy.Windows;

public class ConfigWindow : Window
{
    private Plugin mPlugin;

    public ConfigWindow(Plugin plugin) : base(Plugin.ConfigWindowTitle)
    {
        this.SizeConstraints = new Window.WindowSizeConstraints
        {
            MinimumSize = new Vector2(490, 280),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        this.SizeCondition = ImGuiCond.Once;
        this.mPlugin = plugin;
    }

    public override void Draw()
    {
        ImGui.TextWrapped("博茲雅助手 " + Plugin.Edition + "。只保留義軍武器、戰地筆記、失傳技能／遺忘的碎片，以及持有欄覆蓋視窗。");
        ImGui.Separator();
        if (ImGui.BeginTabBar("##cfg"))
        {
            if (ImGui.BeginTabItem("持有欄覆蓋"))
            {
                ConfigWindow.DrawTabUiHint_LostFindCacheSection(this.mPlugin);
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("介面"))
            {
                this.DrawMycInfo();
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }

    private void DrawMycInfo()
    {
        bool tField2 = !this.mPlugin.Configuration.mGuiAssistConfig.itemInfo.isDisabled_All;
        if (ImGuiComponents.ToggleButton("allwin", ref tField2)) this.mPlugin.Configuration.Save();
        this.mPlugin.Configuration.mGuiAssistConfig.itemInfo.isDisabled_All = !tField2;
        ImGui.SameLine();
        UtilsGUI.GreyText("在戰勲情報（MYCInfo）視窗顯示搜尋列與開啟主視窗的按鈕。");

        bool tField3 = this.mPlugin.Configuration.mGuiAssistConfig.itemInfo.isDisabled_WhenNotFocused;
        if (ImGuiComponents.ToggleButton("focus", ref tField3)) this.mPlugin.Configuration.Save();
        this.mPlugin.Configuration.mGuiAssistConfig.itemInfo.isDisabled_WhenNotFocused = tField3;
        ImGui.SameLine();
        UtilsGUI.GreyText("戰勲情報視窗未聚焦時隱藏搜尋列。");
        ImGui.TextWrapped("節點編號沿用國際服的唯讀路徑。台灣客戶端若版面不同，此搜尋列可能對不齊，需要在遊戲內確認。");
    }

    /// <summary>
    /// Lost Find Cache overlay options. Loadout highlighting is omitted with the loadout feature.
    /// </summary>
    public static void DrawTabUiHint_LostFindCacheSection(Plugin pPlugin, float? pFixedSize = null, float pGuiButtonsPadding = 32 * 3)
    {
        float tInnerSpacing = 12;

        bool tTemp = !pPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_All;
        if (ImGuiComponents.ToggleButton("tg_1", ref tTemp))
        {
            pPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_All = !tTemp;
            if (!pPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_All)
            {
                pPlugin.GUIAssistManager.RequestRestore(GUIAssistOption.MycItemBoxRoleFilter);
            }
            pPlugin.Configuration.Save();
        }
        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + tInnerSpacing);
        UtilsGUI.GreyText("1. 此視窗的覆蓋功能。");

        bool tTemp2 = !pPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_Toolbar;
        if (ImGuiComponents.ToggleButton("tg_2", ref tTemp2))
        {
            pPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_Toolbar = !tTemp2;
            pPlugin.Configuration.Save();
        }
        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + tInnerSpacing);
        UtilsGUI.GreyText("2. 工具列（名稱、職能、碎片、重量）。");

        bool tTemp4 = !pPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_FilterText;
        if (ImGuiComponents.ToggleButton("tg_4", ref tTemp4))
        {
            pPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_FilterText = !tTemp4;
            if (!pPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_FilterText)
            {
                pPlugin.GUIAssistManager.RequestRestore(GUIAssistOption.MycItemBoxRoleFilter);
            }
            pPlugin.Configuration.Save();
        }
        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + tInnerSpacing);
        UtilsGUI.GreyText("3. 篩選強度 ");
        ImGui.SameLine();
        ImGui.PushItemWidth((pFixedSize == null ? ImGui.GetContentRegionAvail().X : (pFixedSize!.Value - ImGui.GetCursorPosX())));
        if (ImGui.SliderInt("##ibfl", ref pPlugin.Configuration.mGuiAssistConfig.itemBox.filterTextLevel, 0, 2))
        {
            pPlugin.Configuration.Save();
        }
        ImGui.PopItemWidth();
        ImGui.SameLine();
        UtilsGUI.ShowHelpMarker("0：關閉。\n1：不符合的技能變暗。\n2：不符合的技能隱藏。");

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + tInnerSpacing);
        ImGui.Separator();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + tInnerSpacing);

        UtilsGUI.GreyText("4. 更新間隔 ");
        ImGui.SameLine();
        ImGui.PushItemWidth((pFixedSize == null ? ImGui.GetContentRegionAvail().X : (pFixedSize!.Value - ImGui.GetCursorPosX() - pGuiButtonsPadding)));
        if (ImGui.SliderFloat("##ibrf", ref pPlugin.Configuration.mGuiAssistConfig.itemBox.refreshRate, 0.1f, 1))
        {
            pPlugin.Configuration.Save();
        }
        ImGui.PopItemWidth();
        ImGui.SameLine();
        if (ImGuiComponents.IconButton("##ibrf_bar", Dalamud.Interface.FontAwesomeIcon.ArrowsSpin))
        {
            pPlugin.Configuration.mGuiAssistConfig.itemBox.refreshRate = pPlugin.Configuration.mGuiAssistConfig.itemBox.refreshRateDefault;
            pPlugin.Configuration.Save();
        }
        ImGui.SameLine();
        UtilsGUI.ShowHelpMarker($"覆蓋視窗重繪的間隔（秒）。較低較順，但可能增加負擔。預設為 {pPlugin.Configuration.mGuiAssistConfig.itemBox.refreshRateDefault}。");

        bool tTemp7 = !pPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_AutoRoleFilter;
        if (ImGuiComponents.ToggleButton("tg_7", ref tTemp7))
        {
            pPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_AutoRoleFilter = !tTemp7;
            pPlugin.Configuration.Save();
        }
        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + tInnerSpacing);
        UtilsGUI.GreyText("5. 依目前職能自動篩選。");
        ImGui.SameLine();
        UtilsGUI.ShowHelpMarker("每次打開失傳技能持有欄時，套用你目前的職能。關閉後不會自動清掉已選的篩選。");

        ImGui.Spacing();
        ImGui.TextWrapped("圖示位置來自 UIMap_LostAction.json 的節點路徑，只讀取遊戲介面，不掛鉤、不掃描特徵碼。台灣客戶端的節點編號可能不同，覆蓋若錯位請在遊戲內確認。");
    }
}
