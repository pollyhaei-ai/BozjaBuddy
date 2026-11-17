using System;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using BozjaBuddy.GUI.Tabs;
using BozjaBuddy.GUI.Sections;

namespace BozjaBuddy.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;
    private LostActionTab mLostActionTab;
    private GeneralSection mGeneralSection;
    private FieldNoteTab mFieldNoteTab;
    private RelicTab mRelicTab;

    public MainWindow(Plugin plugin) : base(
        Plugin.MainWindowTitle, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.RespectCloseHotkey = false;
        this.SizeConstraints = plugin.Configuration.SizeConstraints;
        this.Plugin = plugin;

        this.mLostActionTab = new LostActionTab(this.Plugin);
        this.mGeneralSection = new GeneralSection(this.Plugin);
        this.mFieldNoteTab = new FieldNoteTab(this.Plugin);
        this.mRelicTab = new(this.Plugin);
    }

    public void RearrangeSection()
    {
        this.mLostActionTab.RearrangeSection();
        this.mFieldNoteTab.RearrangeSection();
        this.mRelicTab.RearrangeSection();
    }

    public void Dispose()
    {
        this.mLostActionTab.Dispose();
        this.mFieldNoteTab.Dispose();
        this.mRelicTab.Dispose();
    }

    public override void Draw()
    {
        this.mGeneralSection.DrawGUI();
        if (ImGui.BeginTabBar("Tab Bat"))
        {
            this.mRelicTab.DrawGUI();
            this.mLostActionTab.DrawGUI();
            this.mFieldNoteTab.DrawGUI();
            ImGui.EndTabBar();
        }
    }
}
