using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using BozjaBuddy.Windows;
using BozjaBuddy.Data;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Command;
using System.Collections.Generic;
using BozjaBuddy.GUI.GUIAssist;
using System;
using Dalamud.Game.ClientState;
using BozjaBuddy.Utils;
using BozjaBuddy.GUI;
using System.Numerics;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using Dalamud.Interface.ManagedFontAtlas;

namespace BozjaBuddy
{
    public sealed class Plugin : IDalamudPlugin
    {
        public const string Edition = "1.2.0-tw";
        public const string MainWindowTitle = "博茲雅助手";
        public const string ConfigWindowTitle = "設定 - 博茲雅助手";

        public string Name => "博茲雅助手";
        private const string CommandName = "/bb";
        public bool mIsMainWindowActive = false;
        private DateTime _mCycle1 = DateTime.Now;
        public static bool _isImGuiSafe = false;
        private static Dictionary<string, Window> WINDOWS = new();

        public float TEXT_BASE_HEIGHT = ImGui.GetTextLineHeightWithSpacing();
        public Dictionary<string, string> DATA_PATHS = new Dictionary<string, string>();
        public static IPluginLog PluginLog { get; private set; }
        public IPluginLog PLog { get; init; }

        public IDalamudPluginInterface PluginInterface { get; init; }
        public ICommandManager CommandManager { get; init; }
        public IGameGui GameGui { get; init; }
        public IChatGui ChatGui { get; init; }
        public IClientState ClientState { get; init; }
        public IDataManager DataManager { get; init; }
        public IKeyState KeyState { get; init; }
        public ITextureProvider TextureProvider { get; init; }
        public BBDataManager mBBDataManager;

        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("博茲雅助手");
        public GuiScraper GuiScraper { get; init; }
        public GUIAssistManager GUIAssistManager { get; init; }
        public MainWindow MainWindow { get; init; }
        public IFramework Framework { get; init; }

        public IUiBuilder UIBuilder { get; set; }

        public Plugin(
            IDalamudPluginInterface pluginInterface,
            ICommandManager commandManager,
            IDataManager dataManager,
            IGameGui gameGui,
            IChatGui chatGui,
            IClientState clientState,
            IKeyState keyState,
            IFramework framework,
            ITextureProvider textureProvider,
            IPluginLog pLog)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
            this.DataManager = dataManager;
            this.GameGui = gameGui;
            this.ChatGui = chatGui;
            this.ClientState = clientState;
            this.KeyState = keyState;
            this.Framework = framework;
            this.TextureProvider = textureProvider;
            this.UIBuilder = this.PluginInterface.UiBuilder;
            PluginLog = pLog;
            this.PLog = pLog;

            string tDir = PluginInterface.AssemblyLocation.DirectoryName!;
            this.DATA_PATHS["db"] = Path.Combine(tDir, @"db\LostAction.db");
            this.DATA_PATHS["UIMap_LostAction.json"] = Path.Combine(tDir, @"db\UIMap_LostAction.json");
            this.DATA_PATHS["YurukaStd-UB-AlphaNum.ttf"] = Path.Combine(tDir, @"db\YurukaStd-UB-AlphaNum.otf");

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);
            this.Configuration.Save();

            mBBDataManager = new BBDataManager(this);
            UtilsGameData.Init(this);
            this.MainWindow = new(this);
            Plugin.AddWindow(this.WindowSystem, new ConfigWindow(this));
            Plugin.AddWindow(this.WindowSystem, this.MainWindow);

            this.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "開啟博茲雅助手主視窗"
            });

            this.GuiScraper = new(this);
            this.Framework.Update += this.OnUpdate;

            this.GUIAssistManager = new(this);

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            this.PluginInterface.UiBuilder.OpenMainUi += OpenMain;

            this.Configuration.SizeConstraints = new Window.WindowSizeConstraints
            {
                MinimumSize = new Vector2(675, 509),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };

            this.BuildFont();
            this.MainWindow.RearrangeSection();
        }

        private void BuildFont()
        {
            unsafe
            {
                UtilsGameData.kFontHandle_Yuruka = this.UIBuilder.FontAtlas.NewDelegateFontHandle(
                        e => e.OnPreBuild(
                            tk =>
                            {
                                var config = new SafeFontConfig { SizePx = 30 };
                                config.MergeFont = tk.AddFontFromFile(this.DATA_PATHS["YurukaStd-UB-AlphaNum.ttf"], config);
                                tk.Font = config.MergeFont;
                            }
                        )
                    );
            }
        }

        public void Dispose()
        {
            this.Framework.Update -= this.OnUpdate;
            this.PluginInterface.UiBuilder.Draw -= DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
            this.PluginInterface.UiBuilder.OpenMainUi -= OpenMain;
            this.WindowSystem.RemoveAllWindows();
            this.CommandManager.RemoveHandler(CommandName);
            UtilsGameData.Dispose();
            this.mBBDataManager.Dispose();
        }

        private void OnCommand(string command, string args)
        {
            Plugin.GetWindow(Plugin.MainWindowTitle)!.IsOpen = true;
        }

        private void OnUpdate(IFramework framework)
        {
            if (!ClientState.IsLoggedIn) return;
            this.GuiScraper.Scrape();
        }

        private void OpenMain()
        {
            Plugin._isImGuiSafe = true;
            Plugin.GetWindow(Plugin.MainWindowTitle)!.IsOpen = true;
        }

        private void DrawUI()
        {
            Plugin._isImGuiSafe = true;
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);
            this.WindowSystem.Draw();
            this.GUIAssistManager.Draw();
            ImGui.PopStyleVar();
            if ((DateTime.Now - this._mCycle1).TotalSeconds > 2)
            {
                this.mIsMainWindowActive = Plugin.GetWindow(Plugin.MainWindowTitle)!.IsOpen;
                this._mCycle1 = DateTime.Now;
            }
        }

        public void DrawConfigUI()
        {
            Plugin._isImGuiSafe = true;
            Plugin.GetWindow(Plugin.ConfigWindowTitle)!.IsOpen = true;
        }

        public bool isKeyPressed(VirtualKey key) => this.KeyState.IsVirtualKeyValid(key) && KeyState[key];

        private static bool AddWindow(WindowSystem winSys, Window window)
        {
            if (!Plugin.WINDOWS.TryAdd(window.WindowName, window)) return false;
            winSys.AddWindow(window);
            return true;
        }

        public static Window? GetWindow(string windowName) => Plugin.WINDOWS.TryGetValue(windowName, out Window? window) && window != null ? window : null;
    }
}
