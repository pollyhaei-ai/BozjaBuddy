using BozjaBuddy.Utils;
using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Logging;
using BozjaBuddy.Data;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.TextureWraps;

namespace BozjaBuddy.GUI.Sections
{
    public class RelicSection : Section, IDisposable
    {
        public static Job kDefaultCurrJob = Job.PLD;
        private readonly float kSectionHeight;

        protected override Plugin mPlugin { get; set; }
        private RelicStep mCurrTopic = 0;
        private RelicSectionGuiFlag mGuiFlag = RelicSectionGuiFlag.None;
        private Dictionary<RelicStep, string> mTopicHeaders = new()
        {
            { RelicStep.None, "簡介" },
            { RelicStep.Resistance, "義軍武器" },
            { RelicStep.ResistanceA, "義軍武器（強化）" },
            { RelicStep.Recollection, "追憶" },
            { RelicStep.LawsOrder, "律法" },
            { RelicStep.OTG_1, "一次性收集 1" },
            { RelicStep.LawsOrderA, "律法（強化）" },
            { RelicStep.OTG_2, "一次性收集 2" },
            { RelicStep.Blades, "刃" }
        };
        private List<RelicStep> mOrderedTopics = new()
        {
            RelicStep.None,
            RelicStep.Resistance,
            RelicStep.ResistanceA,
            RelicStep.Recollection,
            RelicStep.LawsOrder,
            RelicStep.OTG_1,
            RelicStep.LawsOrderA,
            RelicStep.OTG_2,
            RelicStep.Blades
        };
        private Dictionary<string, Location> _iterriTypeAndDefaultLocation = new();
        private List<string> _terriTypeToLoad = new()
        {
            "e3ec",             // doman enclave
            "a2f1",            // sea of cloud,
            "r2f1",              // coerthas western highland
            "d2f1",             // drav forelands
            "d2f3",             // churning mist
            "a2f2",             // azys lla
            "d2f2",              // drav hinterlands

            "g3f1",             // the fringes
            "g3f2",             // the peaks
            "g3f3",              // the lochs
            "e3f1",             // ruby sea
            "e3f2",             // yanxia
            "e3f3",             // azim steppe
        };

        public RelicSection(Plugin pPlugin)
        {
            this.mPlugin = pPlugin;
            this.kSectionHeight = this.mPlugin.TEXT_BASE_HEIGHT * (15 + 3.5f) * 2; // default=(15 + 3.5f)*1
            foreach (string terriType in this._terriTypeToLoad)
            {
                this._iterriTypeAndDefaultLocation.TryAdd(terriType, new(this.mPlugin, terriType));
            }
        }

        public override bool DrawGUI()
        {
            var tHeight = this.mPlugin.TEXT_BASE_HEIGHT * (15 + 3.48f);
            ImGui.BeginChild("##rlcSectionTpcList", new Vector2(ImGui.GetWindowWidth() / 10 * 2.28f, tHeight < this.kSectionHeight
                                                                                            ? tHeight
                                                                                            : this.kSectionHeight));
            this.DrawTopicList();
            ImGui.EndChild();
            ImGui.SameLine();
            ImGui.BeginChild("##rlcSectionTpc", new Vector2(
                ImGui.GetWindowWidth() / 10 * 7.72f - ImGui.GetStyle().WindowPadding.X * 2, tHeight < this.kSectionHeight
                                                                                            ? tHeight
                                                                                            : this.kSectionHeight));
            this.DrawTopic();
            ImGui.EndChild();
            return true;
        }
        public void DrawTopicList()
        {
            Job tCurrJob = this.mPlugin.Configuration.mRelicCurrJob;
            if (ImGui.BeginListBox("##rlcSectLb", ImGui.GetContentRegionAvail()))
            {
                // Get progress
                if (!this.mPlugin.Configuration.mRelicProgress.TryGetValue(tCurrJob, out var tProgress))
                {
                    tCurrJob = RelicSection.kDefaultCurrJob;
                    tProgress = this.mPlugin.Configuration.mRelicProgress[tCurrJob];
                }

                // Intro
                if (ImGui.Selectable(this.TopicLabel(RelicStep.None, tCurrJob), this.mCurrTopic == RelicStep.None)) this.mCurrTopic = RelicStep.None;
                UtilsGUI.GreyText("各階段任務名稱來自遊戲資料。");
                ImGui.Separator();
                ImGui.Spacing();
                // ===================================
                // Tip
                UtilsGUI.GreyText("（點圖示可連結到聊天）");
                // Relic steps
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(Vector2.One.X, ImGui.GetStyle().ItemSpacing.Y));
                foreach (var step in this.mOrderedTopics)
                {
                    var header = this.TopicLabel(step, tCurrJob);
                    if (step == RelicStep.None) continue;
                    if (!this.mPlugin.Configuration.mIsRelicFirstTime && (step == RelicStep.OTG_1 || step == RelicStep.OTG_2)) continue;
                    // Relic progress button
                    ImGui.PushID(header);
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.Check, defaultColor: tProgress.HasFlag(step) ? UtilsGUI.AdjustTransparency(UtilsGUI.Colors.Button_Green, 0.7f) : null, hoveredColor: UtilsGUI.Colors.Button_Green))
                    {
                        // Add prog and up to it
                        if (!tProgress.HasFlag(step))
                        {
                            foreach (var progToAdd in this.mOrderedTopics)
                            {
                                this.mPlugin.Configuration.mRelicProgress[tCurrJob] |= progToAdd;
                                if (progToAdd == step) break;
                            }
                        }
                        // Remove prog and after it
                        else
                        {
                            bool isRemoving = false;
                            foreach (var progToRemove in this.mOrderedTopics)
                            {
                                if (progToRemove == step) isRemoving = true;
                                if (isRemoving)
                                {
                                    this.mPlugin.Configuration.mRelicProgress[tCurrJob] &= ~progToRemove;
                                }
                            }
                        }
                    }
                    else UtilsGUI.SetTooltipForLastItem(tProgress.HasFlag(step) ? "標記為未完成。" : "標記為已完成。");
                    // Relic icon
                    ImGui.SameLine();
                    float tIconHeight = 15;
                    if (step != RelicStep.None && step != RelicStep.OTG_1 && step != RelicStep.OTG_2)
                    {
                        IDalamudTextureWrap? tIDalamudTextureWrap = UtilsGameData.kTextureCollection?.GetTextureFromItemId(
                                                        Convert.ToUInt32(UtilsGameData.kRelicsAndJobs[step][tCurrJob]),
                                                        pSheet: Data.TextureCollection.Sheet.Item,
                                                        pTryLoadTexIfFailed: true
                                                        );
                        if (tIDalamudTextureWrap != null)
                        {
                            UtilsGUI.ItemLinkButton_Image(this.mPlugin, UtilsGameData.kRelicsAndJobs[step][tCurrJob], tIDalamudTextureWrap, pImageScaling: tIconHeight / tIDalamudTextureWrap.Height * 1.53f);
                        }
                    }
                    else ImGui.Text("----- ");
                    ImGui.PopID();
                    ImGui.SameLine();
                    // Relic topic selectable
                    if (ImGui.Selectable(header, this.mCurrTopic == step))  this.mCurrTopic = step;
                }
                ImGui.PopStyleVar();

                // Configs
                // First time
                ImGui.SetCursorScreenPos(ImGui.GetCursorScreenPos() + new Vector2(0, ImGui.GetContentRegionAvail().Y - 12f * 2 * 2.15f));
                if (ImGui.Checkbox("##rlcFirst", ref this.mPlugin.Configuration.mIsRelicFirstTime))
                {
                    this.mPlugin.Configuration.Save();
                }
                ImGui.SameLine();
                UtilsGUI.GreyText("第一次");
                ImGui.SameLine();
                UtilsGUI.ShowHelpMarker("如果這個角色是第一次做義軍武器，請勾選。\n會依此調整要顯示的步驟。");
                // Job
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                if (ImGui.BeginCombo("##rlcJobCmb", this.mPlugin.Configuration.mRelicCurrJob.ToString()))
                {
                    foreach (Job job in UtilsGameData.kRelicValidJobs)
                    {
                        if (ImGui.Selectable(job.ToString()))
                        {
                            this.mPlugin.Configuration.mRelicCurrJob = job;
                            this.mPlugin.PLog.Debug($"> RelicSeciton: cJ={this.mPlugin.Configuration.mRelicCurrJob} j={job}");
                        }
                    }
                    ImGui.EndCombo();
                }
                else UtilsGUI.SetTooltipForLastItem("目前要追蹤的職業");

                ImGui.EndListBox();
            }
        }
        public void DrawTopic()
        {
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, UtilsGUI.AdjustTransparency(UtilsGUI.Colors.BackgroundText_Grey, 0.3f));
            switch (this.mCurrTopic)
            {
                case RelicStep.None: this.DrawTopic_Intro(); break;
                case RelicStep.Resistance: this.DrawTopic_Resistance(); break;
                case RelicStep.ResistanceA: this.DrawTopic_ResistanceA(); break;
                case RelicStep.Recollection: this.DrawTopic_Recollection(); break;
                case RelicStep.LawsOrder: this.DrawTopic_LawsOrder(); break;
                case RelicStep.OTG_1: this.DrawTopic_OTG1(); break;
                case RelicStep.LawsOrderA: this.DrawTopic_LawsOrderA(); break;
                case RelicStep.OTG_2: this.DrawTopic_OTG2(); break;
                case RelicStep.Blades: this.DrawTopic_Blades(); break;
            }
            ImGui.PopStyleColor();
        }
        public void DrawTopic_Intro()
        {
            ImGui.PushTextWrapPos();
            ImGui.Text("這頁主要是義軍武器的進度清單。");
            UtilsGUI.GreyText("  若要看整體說明，");
            ImGui.SameLine();
            // detail
            if (UtilsGUI.SelectableLink(this.mPlugin, $"點這裡 {(this.mGuiFlag.HasFlag(RelicSectionGuiFlag.Intro_ShowingDetail) ? "▼" : "▲")}", -1, pIsClosingPUOnClick: false, pIsAuxiLinked: false))
            {
                if (this.mGuiFlag.HasFlag(RelicSectionGuiFlag.Intro_ShowingDetail)) this.mGuiFlag &= ~RelicSectionGuiFlag.Intro_ShowingDetail;
                else this.mGuiFlag |= RelicSectionGuiFlag.Intro_ShowingDetail;
            }
            if (this.mGuiFlag.HasFlag(RelicSectionGuiFlag.Intro_ShowingDetail))
            {
                if (ImGui.BeginChild("##introDetail", new(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().FramePadding.X * 2, 300), true))
                {
                    ImGui.Text("在博茲雅前線內容裡，大致會同時做兩件事：");
                    UtilsGUI.GreyText(
                        """
                        1. 推進博茲雅前線區域（解鎖各區與攻城戰等）。
                        2. 推進義軍武器（解鎖下一個武器階段）。
                        """
                    );
                    UtilsGUI.GreyText("""可以把它想成兩條並行的小任務線。""");
                    ImGui.Text("各階段左側會列出對應任務，名稱來自遊戲資料。");

                    ImGui.Separator();

                    if (ImGui.CollapsingHeader("推進區域："))
                    {
                        ImGui.PushTextWrapPos();
                        UtilsGUI.GreyText("""- 收集戰勲，拿去提升軍階，然後重複。""");
                        ImGui.SameLine();
                        UtilsGUI.ShowHelpMarker("最快的戰勲來自危命任務與特殊遭遇戰。\r\n坐騎地圖（移動速度加成）建議盡早買，基地的軍需官以 25 個戰勲簇出售。");

                        UtilsGUI.GreyText("- 基礎流程可以對照公開的博茲雅前線說明。");
                        ImGui.SameLine();
                        UtilsGUI.UrlButton("https://ffxiv.consolegameswiki.com/wiki/The_Bozjan_Southern_Front");

                        UtilsGUI.GreyText(
                            """
                            - 建議找固定隊。
                            - 有人一起打會輕鬆很多。
                            """
                            );
                        ImGui.SameLine();
                        UtilsGUI.ShowHelpMarker("此精簡版已移除社群與女王古殿分頁，請自行尋找固定隊。");
                        UtilsGUI.GreyText(
                            """
                        - 軍階節點：
                                1       博茲雅前線・一區
                                5       博茲雅前線・二區
                                8       博茲雅前線・三區
                                10      湖岸堡攻城戰，接著女王古殿，再來札杜諾爾高原
                                15      女王古殿零式
                                18      札杜諾爾高原・二區
                                22      札杜諾爾高原・三區
                                25      達利阿達攻城戰
                        """
                            );
                        ImGui.PopTextWrapPos();
                    }
                    
                    if (ImGui.CollapsingHeader("推進義軍武器："))
                    {
                        ImGui.PushTextWrapPos();

                        UtilsGUI.GreyText("""- 義軍武器的進度通常也需要博茲雅前線的內容進度。""");
                        UtilsGUI.GreyText(
                            """
                            - 每個階段通常有兩種做法：
                                    1. 博茲雅路線：刷博茲雅前線相關內容。
                                    2. 非博茲雅路線：刷博茲雅以外的內容。
                            - 若有青魔，部分步驟比較適合走非博茲雅路線。

                            - 第一次做義軍武器時：
                                    + 會多「一次性收集 1」與「一次性收集 2」。
                                    + 第一階段不需要詩學神典石。
                                    + 還有一些小差異。
                            - 請勾選左側下方的「第一次」。
                            """);

                        ImGui.PopTextWrapPos();
                    }

                    ImGui.EndChild();
                }
            }

            UtilsGUI.GreyText("- 若是這個角色第一次做，請勾選左側下方的「第一次」。");
            ImGui.Text(
                    """
                    - 前置：
                            + 80 級以上
                            + 完成暗影之逆焰 5.55 主線
                            + 完成重返伊瓦利斯系列
                    """
                );
            ImGui.Text("- 各步驟都要限制等級，職業與裝備沒有限制。");

            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Separator();

            UtilsGUI.GreyText(
                            """
                            - 每個階段通常有兩種做法：
                                    1. 博茲雅路線：刷博茲雅前線相關內容。
                                    2. 非博茲雅路線：刷博茲雅以外的內容。
                            - 若有青魔，部分步驟比較適合走非博茲雅路線。

                            - 第一次做義軍武器時：
                                    + 會多「一次性收集 1」與「一次性收集 2」。
                                    + 第一階段不需要詩學神典石。
                                    + 還有一些小差異。
                            """);

            ImGui.PopTextWrapPos();
        }
        public void DrawTopic_Resistance()
        {
            var tConfig = this.mPlugin.Configuration;
            var tHeight = this.mPlugin.TEXT_BASE_HEIGHT * (15 + 3.48f);
            tHeight = tHeight < this.kSectionHeight ? tHeight : this.kSectionHeight;

            ImGui.PushStyleColor(ImGuiCol.Button, UtilsGUI.AdjustTransparency(UtilsGUI.Colors.BackgroundText_Grey, 0.15f));
            // Content
            if (ImGui.BeginChild("##tr_content", new(ImGui.GetContentRegionAvail().X / 10 * 7.2f, tHeight)))
            {
                if (tConfig.mIsRelicFirstTime)
                {
                    ImGui.Text("1. Q:"); ImGui.SameLine(); UtilsGUI.SelectableLink_Quest(this.mPlugin, 69370);
                    ImGui.Text("2. Q:"); ImGui.SameLine(); UtilsGUI.SelectableLink_Quest(this.mPlugin, 69371);
                    ImGui.Text("3. Q:"); ImGui.SameLine(); UtilsGUI.SelectableLink_Quest(this.mPlugin, 69372);
                    ImGui.Text("4. Q:"); ImGui.SameLine(); UtilsGUI.SelectableLink_Quest(this.mPlugin, 69380);
                }
                else
                {
                    ImGui.Text("Q:"); ImGui.SameLine(); UtilsGUI.SelectableLink_Quest(this.mPlugin, 69381);
                    this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, 30273, pMaxCount: 4);
                    this._textSectionEnd(); ImGui.SameLine(); ImGui.Text("|___ ");
                    ImGui.SameLine(); UtilsGUI.GreyText("商人：");
                    if (this.mPlugin.mBBDataManager.mVendors.TryGetValue(3, out var tVendor) && tVendor != null && tVendor.mLocation != null)
                    {
                        ImGui.SameLine();
                        ImGui.PushStyleColor(ImGuiCol.Text, UtilsGUI.Colors.BackgroundText_Grey);
                        UtilsGUI.LocationLinkButton(this.mPlugin, tVendor.mLocation, pDesc: tVendor.mLocation.ToStringFull());
                        ImGui.PopStyleColor();
                    }
                    this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); ImGui.SameLine(); UtilsGUI.GreyText("（每個 250 詩學神典石）");

                }
                ImGui.EndChild();
            }
            ImGui.PopStyleColor();

            ImGui.SameLine();
        }
        public void DrawTopic_ResistanceA()
        {
            var tConfig = this.mPlugin.Configuration;
            var tHeight = this.mPlugin.TEXT_BASE_HEIGHT * (15 + 3.48f);
            tHeight = tHeight < this.kSectionHeight ? tHeight : this.kSectionHeight;

            ImGui.PushStyleColor(ImGuiCol.Button, UtilsGUI.AdjustTransparency(UtilsGUI.Colors.BackgroundText_Grey, 0.15f));
            // Content
            if (ImGui.BeginChild("##tr_content", new(ImGui.GetContentRegionAvail().X / 10 * 7.2f, tHeight)))
            {
                ImGui.Text("Q:"); ImGui.SameLine(); UtilsGUI.SelectableLink_Quest(this.mPlugin, 69506);
                // Tortured
                this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.Memory_Tortured, pMaxCount: 20);
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("博茲雅前線・一區的危命／特殊遭遇戰會掉落"); ImGui.SameLine(); this._textLoc("e3ec", pUseIcon: true);
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("也會從這些地圖的危命掉落：");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); this._textLoc("a2f1", "阿巴拉提亞雲海"); ImGui.SameLine(); this._textLoc("r2f1");
                // Sorrowful
                this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.Memory_Sorrowful, pMaxCount: 20);
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("博茲雅前線・二區的危命／特殊遭遇戰會掉落"); ImGui.SameLine(); this._textLoc("e3ec", pUseIcon: true);
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("也會從這些地圖的危命掉落：");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); this._textLoc("d2f1"); ImGui.SameLine(); this._textLoc("d2f3");
                // Harrowing
                this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.Memory_Harrowing, pMaxCount: 20);
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("博茲雅前線・三區的危命／特殊遭遇戰會掉落"); ImGui.SameLine(); this._textLoc("e3ec", pUseIcon: true);
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("也會從這些地圖的危命掉落：");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); this._textLoc("a2f2"); ImGui.SameLine(); this._textLoc("d2f2");
                ImGui.EndChild();
            }

            ImGui.SameLine();

            // Prereq
            if (ImGui.BeginChild("##tr_prereq", new(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().FramePadding.X, tHeight), true, ImGuiWindowFlags.MenuBar))
            {
                if (ImGui.BeginMenuBar())
                {
                    UtilsGUI.GreyText("前置");
                    ImGui.EndMenuBar();
                }
                UtilsGUI.GreyText("任務");
                UtilsGUI.SelectableLink_Quest(this.mPlugin, 69477);
                UtilsGUI.SelectableLink_Quest(this.mPlugin, 69505);
                ImGui.EndChild();
            }
            ImGui.PopStyleColor();
        }
        public void DrawTopic_Recollection()
        {
            var tConfig = this.mPlugin.Configuration;
            var tHeight = this.mPlugin.TEXT_BASE_HEIGHT * (15 + 3.48f);
            tHeight = tHeight < this.kSectionHeight ? tHeight : this.kSectionHeight;

            ImGui.PushStyleColor(ImGuiCol.Button, UtilsGUI.AdjustTransparency(UtilsGUI.Colors.BackgroundText_Grey, 0.15f));
            // Content
            if (ImGui.BeginChild("##tr_content", new(ImGui.GetContentRegionAvail().X, tHeight)))
            {
                ImGui.Text("Q:"); ImGui.SameLine(); UtilsGUI.SelectableLink_Quest(this.mPlugin, 69507);
                // Bitter
                this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.Memory_Bitter, pMaxCount: 6);
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("60 級同步迷宮必定掉落（建議逆塔）。");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("每日一次的等級提升任務輪盤必定取得。");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("博茲雅前線的敵人與特殊遭遇戰有低機率掉落。"); ImGui.SameLine(); this._textLoc("e3ec", pUseIcon: true);

                ImGui.EndChild();
            }
            ImGui.PopStyleColor();

            ImGui.SameLine();
        }
        public void DrawTopic_LawsOrder()
        {
            var tConfig = this.mPlugin.Configuration;
            var tHeight = this.mPlugin.TEXT_BASE_HEIGHT * (15 + 3.48f);
            tHeight = tHeight < this.kSectionHeight ? tHeight : this.kSectionHeight;

            ImGui.PushStyleColor(ImGuiCol.Button, UtilsGUI.AdjustTransparency(UtilsGUI.Colors.BackgroundText_Grey, 0.15f));
            // Content
            if (ImGui.BeginChild("##tr_content", new(ImGui.GetContentRegionAvail().X / 10 * 7.2f, tHeight)))
            {
                ImGui.Text("Q:"); ImGui.SameLine(); UtilsGUI.SelectableLink_Quest(this.mPlugin, 69574);
                // Loathsome
                this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.Memory_Loathsome, pMaxCount: 15);
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("（×5）每通過一次湖岸堡攻城戰必定取得"); ImGui.SameLine(); this._textLoc("e3ec", pUseIcon: true);
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("（×1）博茲雅前線的特殊遭遇戰有低機率掉落"); ImGui.SameLine(); this._textLoc("e3ec", pUseIcon: true);
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("（×1）任一水晶塔系列討伐必定取得");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); ImGui.SameLine(); UtilsGUI.GreyText("- 希爾科斯塔"); ImGui.SameLine(); this._textRecSign("想求快的話建議這個");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); ImGui.SameLine(); UtilsGUI.GreyText("- 暗影世界");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); ImGui.SameLine(); UtilsGUI.GreyText("- 古代人迷宮");
                ImGui.EndChild();
            }
            ImGui.PopStyleColor();

            ImGui.SameLine();

            // Prereq
            if (ImGui.BeginChild("##tr_prereq", new(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().FramePadding.X, tHeight), true, ImGuiWindowFlags.MenuBar))
            {
                if (ImGui.BeginMenuBar())
                {
                    UtilsGUI.GreyText("前置");
                    ImGui.EndMenuBar();
                }
                UtilsGUI.GreyText("任務");
                UtilsGUI.SelectableLink_Quest(this.mPlugin, 69477);
                UtilsGUI.GreyText("需已通過");
                ImGui.Text("湖岸堡攻城戰");
                ImGui.Text("女王古殿");
                ImGui.EndChild();
            }
        }
        public void DrawTopic_OTG1()
        {
            var tConfig = this.mPlugin.Configuration;
            var tHeight = this.mPlugin.TEXT_BASE_HEIGHT * (15 + 3.48f);
            tHeight = tHeight < this.kSectionHeight ? tHeight : this.kSectionHeight;

            ImGui.PushStyleColor(ImGuiCol.Button, UtilsGUI.AdjustTransparency(UtilsGUI.Colors.BackgroundText_Grey, 0.15f));
            // Content
            if (ImGui.BeginChild("##tr_content", new(ImGui.GetContentRegionAvail().X, tHeight)))
            {
                ImGui.Text("Q:"); ImGui.SameLine(); UtilsGUI.SelectableLink_Quest(this.mPlugin, 69575);
                // Haunting
                this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.Memory_Haunting, pMaxCount: 18);
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("（×3）任一瑪哈之影系列討伐必定取得");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); ImGui.SameLine(); UtilsGUI.GreyText("- 虛空方舟"); ImGui.SameLine(); this._textRecSign("想求快的話建議這個");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); ImGui.SameLine(); UtilsGUI.GreyText("- 瑪哈的泣城");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); ImGui.SameLine(); UtilsGUI.GreyText("- 影之國");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, UtilsGUI.Colors.BackgroundText_Grey);
                ImGui.TextUnformatted("（×1，約 66%）這些地圖的危命會掉落：");
                ImGui.PopStyleColor();
                this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); this._textLoc("g3f1"); ImGui.SameLine(); this._textLoc("g3f2"); ImGui.SameLine(); this._textLoc("g3f3"); ImGui.SameLine(); this._textRecSign();
                // Vexing
                this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.Memory_Vexatious, pMaxCount: 18);
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("（×3）任一重返伊瓦利斯系列討伐必定取得");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); ImGui.SameLine(); UtilsGUI.GreyText("- 拉巴納斯塔王城");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); ImGui.SameLine(); UtilsGUI.GreyText("- 里多拉納大燈塔");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); ImGui.SameLine(); UtilsGUI.GreyText("- 歐邦修道院");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); 
                ImGui.PushStyleColor(ImGuiCol.Text, UtilsGUI.Colors.BackgroundText_Grey);
                ImGui.TextUnformatted("（×1，約 66%）這些地圖的危命會掉落：");
                ImGui.PopStyleColor();
                this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); this._textLoc("e3f1", "紅玉海"); ImGui.SameLine(); this._textLoc("e3f2"); ImGui.SameLine(); this._textLoc("e3f3", "太陽神草原"); ImGui.SameLine(); this._textRecSign();

                ImGui.EndChild();
            }
            ImGui.PopStyleColor();

            ImGui.SameLine();
        }
        public void DrawTopic_LawsOrderA()
        {
            var tConfig = this.mPlugin.Configuration;
            var tHeight = this.mPlugin.TEXT_BASE_HEIGHT * (15 + 3.48f);
            tHeight = tHeight < this.kSectionHeight ? tHeight : this.kSectionHeight;

            ImGui.PushStyleColor(ImGuiCol.Button, UtilsGUI.AdjustTransparency(UtilsGUI.Colors.BackgroundText_Grey, 0.15f));
            // Content
            if (ImGui.BeginChild("##tr_content", new(ImGui.GetContentRegionAvail().X, tHeight)))
            {
                ImGui.Text("Q:"); ImGui.SameLine(); UtilsGUI.SelectableLink_Quest(this.mPlugin, 69576);
                // Timeworn
                this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.TimeWorn, pMaxCount: 15);
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("（×1）死者宮殿每 10 層");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); ImGui.SameLine(); UtilsGUI.GreyText("- 機率偏低，越往後越高");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("（×3）每通過一次女王古殿必定取得");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); ImGui.SameLine(); UtilsGUI.GreyText("- 需要自行招募，或加入固定隊");

                ImGui.EndChild();
            }
            ImGui.PopStyleColor();

            ImGui.SameLine();
        }
        public void DrawTopic_OTG2()
        {
            var tConfig = this.mPlugin.Configuration;
            var tHeight = this.mPlugin.TEXT_BASE_HEIGHT * (15 + 3.48f);
            tHeight = tHeight < this.kSectionHeight ? tHeight : this.kSectionHeight;
            float tContent1Height = 20 * 6;

            // Content 1
            ImGui.PushStyleColor(ImGuiCol.Button, UtilsGUI.AdjustTransparency(UtilsGUI.Colors.BackgroundText_Grey, 0.15f));
            if (ImGui.BeginChild("##tr_content1", new(ImGui.GetContentRegionAvail().X / 10 * 6f, tContent1Height)))
            {
                ImGui.Text("1. Q:"); ImGui.SameLine(); UtilsGUI.SelectableLink_Quest(this.mPlugin, 69632);
                ImGui.Text("2. 再接下面這 3 個任務：");
                this._textSectionCont(); ImGui.SameLine(); UtilsGUI.SelectableLink_Quest(this.mPlugin, 69633);
                this._textSectionCont(); ImGui.SameLine(); UtilsGUI.SelectableLink_Quest(this.mPlugin, 69634);
                this._textSectionCont(); ImGui.SameLine(); UtilsGUI.SelectableLink_Quest(this.mPlugin, 69635);
                ImGui.EndChild();
            }
            ImGui.PopStyleColor();

            ImGui.SameLine();

            // Prereq
            if (ImGui.BeginChild("##tr_prereq", new(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().FramePadding.X, tContent1Height), true, ImGuiWindowFlags.MenuBar))
            {
                if (ImGui.BeginMenuBar())
                {
                    UtilsGUI.GreyText("前置");
                    ImGui.EndMenuBar();
                }
                UtilsGUI.GreyText("任務");
                UtilsGUI.SelectableLink_Quest(this.mPlugin, 69620);
                ImGui.PushTextWrapPos();
                UtilsGUI.GreyText("（這個任務會解鎖札杜諾爾高原，本步驟在那裡）");
                ImGui.PopTextWrapPos();
                ImGui.EndChild();
            }

            // Content 2
            ImGui.PushStyleColor(ImGuiCol.Button, UtilsGUI.AdjustTransparency(UtilsGUI.Colors.BackgroundText_Grey, 0.15f));
            if (ImGui.BeginChild("##tr_content2", new(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().FramePadding.X * 2, tHeight - tContent1Height - ImGui.GetStyle().FramePadding.Y * 2 + 2)))
            {
                // Path options
                ImGui.Text("3. 收集方式");
                ImGui.SameLine();
                if (ImGui.BeginCombo("##cmbRelicOTG2Path", tConfig.mRelicOTG2Path == 0 ? "札杜諾爾危命／特殊遭遇戰" : "討伐"))
                {
                    Vector2 tAnchor = ImGui.GetCursorScreenPos();
                    if (ImGui.Selectable("札杜諾爾危命／特殊遭遇戰")) tConfig.mRelicOTG2Path = 0;
                    ImGui.SetCursorScreenPos(tAnchor + new Vector2(90, 0));
                    UtilsGUI.GreyText("軍階 25 以前建議走這條，以便打達利阿達攻城戰");
                    if (ImGui.Selectable("討伐")) tConfig.mRelicOTG2Path = 1;
                    ImGui.EndCombo();
                }
                if (tConfig.mRelicOTG2Path == 0)
                {
                    ImGui.SameLine(); this._textLoc("e3ec", pUseIcon: true);
                }
                ImGui.SameLine(); UtilsGUI.ShowHelpMarker("兩種方式可以切換，但集中做一種通常比較快。");
                // Path
                if (tConfig.mRelicOTG2Path == 0)        // zadnor
                {
                    // zone 1
                    UtilsGUI.GreyText("| 一區");
                    this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.CompactAxle, pMaxCount: 30); ImGui.SameLine(); UtilsGUI.GreyText("（危命 ×1）");
                    this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.CompactSpring, pMaxCount: 30); ImGui.SameLine(); UtilsGUI.GreyText("（特殊遭遇戰 ×2）");
                    // zone 2
                    UtilsGUI.GreyText("| 二區");
                    this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.RealmBook, pMaxCount: 30); ImGui.SameLine(); UtilsGUI.GreyText("（危命 ×1）");
                    this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.RiftBook, pMaxCount: 30); ImGui.SameLine(); UtilsGUI.GreyText("（特殊遭遇戰 ×2）");
                    // zone 3
                    UtilsGUI.GreyText("| 三區");
                    this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.Memory_Bleak, pMaxCount: 30); ImGui.SameLine(); UtilsGUI.GreyText("（危命 ×1）");
                    this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.Memory_Lurid, pMaxCount: 30); ImGui.SameLine(); UtilsGUI.GreyText("（特殊遭遇戰 ×2）");
                }
                else                                    // raids
                {
                    // alexander
                    UtilsGUI.GreyText("| 亞歷山大");
                    this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.CompactAxle, pMaxCount: 30); ImGui.SameLine(); UtilsGUI.GreyText("亞歷山大 1、2、5、6、9、10 各 ×1");
                    this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.CompactSpring, pMaxCount: 30); ImGui.SameLine(); UtilsGUI.GreyText("亞歷山大 3、4、7、8、11、12 各 ×1");
                    // omega
                    UtilsGUI.GreyText("| 歐米茄");
                    this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.RealmBook, pMaxCount: 30); ImGui.SameLine(); UtilsGUI.GreyText("歐米茄 1、2、5、6、9、10 各 ×1");
                    this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.RiftBook, pMaxCount: 30); ImGui.SameLine(); UtilsGUI.GreyText("歐米茄 3、4、7、8、11、12 各 ×1");
                    // eden
                    UtilsGUI.GreyText("| 伊甸");
                    this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.Memory_Bleak, pMaxCount: 30); ImGui.SameLine(); UtilsGUI.GreyText("伊甸 1、2、5、6、9、10 各 ×1");
                    this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.Memory_Lurid, pMaxCount: 30); ImGui.SameLine(); UtilsGUI.GreyText("伊甸 3、4、7、8、11、12 各 ×1");
                }
                ImGui.Text("4. Q:"); ImGui.SameLine(); UtilsGUI.SelectableLink_Quest(this.mPlugin, 69636);

                ImGui.EndChild();
            }
            ImGui.PopStyleColor();
        }
        public void DrawTopic_Blades()
        {
            var tConfig = this.mPlugin.Configuration;
            var tHeight = this.mPlugin.TEXT_BASE_HEIGHT * (15 + 3.48f);
            tHeight = tHeight < this.kSectionHeight ? tHeight : this.kSectionHeight;

            // Content
            ImGui.PushStyleColor(ImGuiCol.Button, UtilsGUI.AdjustTransparency(UtilsGUI.Colors.BackgroundText_Grey, 0.15f));
            if (ImGui.BeginChild("##tr_content", new(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().FramePadding.X * 2, tHeight)))
            {
                ImGui.Text("Q:"); ImGui.SameLine(); UtilsGUI.SelectableLink_Quest(this.mPlugin, 69637);
                // Loathsome
                this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.RawEmotion, pMaxCount: 15);
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("（×3）每通過一次達利阿達攻城戰必定取得"); ImGui.SameLine(); this._textLoc("e3ec", pUseIcon: true);
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("（×2）每通過一次女王古殿必定取得");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("（×1）任一 70 級迷宮必定取得");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("（×1）天之御柱每 10 層（機率偏低，越往後越高）");
                ImGui.EndChild();
            }
            ImGui.PopStyleColor();
        }

        private void _textSectionBegin() => ImGui.Text("|___ ");
        private void _textSectionCont() => ImGui.Text("\t\t ");
        private void _textSectionEnd() => ImGui.Text("\t\t ");
        private void _textLoc(string territoryType, string? pDesc = null, bool pUseIcon = false)
        {
            if (this._iterriTypeAndDefaultLocation.TryGetValue(territoryType, out var tLoc) && tLoc != null)
            {
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, UtilsGUI.Colors.BackgroundText_Grey);
                UtilsGUI.LocationLinkButton(this.mPlugin, tLoc, pIsTeleporting: true, pDesc: pDesc ?? tLoc.ToString(), pUseIcon: pUseIcon);
                ImGui.PopStyleColor();
            }
        }
        private string TopicLabel(RelicStep step, Job job)
        {
            if (step != RelicStep.None && step != RelicStep.OTG_1 && step != RelicStep.OTG_2
                && UtilsGameData.kRelicsAndJobs.TryGetValue(step, out var jobs)
                && jobs.TryGetValue(job, out var itemId))
            {
                var name = SheetNames.Item(this.mPlugin, (uint)itemId);
                if (!string.IsNullOrWhiteSpace(name)) return name;
            }
            return this.mTopicHeaders[step];
        }
        private void _textRecSign(string? pDesc = null)
        {
            ImGui.Text("*");
            UtilsGUI.SetTooltipForLastItem(pDesc ?? "建議");
        }

        public override void DrawGUIDebug() { }

        public override void Dispose() { }

        [Flags]
        public enum RelicStep
        {
            None = 0,
            Resistance = 1,
            ResistanceA = 2,
            Recollection = 4,
            LawsOrder = 8,
            OTG_1 = 16,
            LawsOrderA = 32,
            OTG_2 = 64,
            Blades = 128
        }

        [Flags]
        private enum RelicSectionGuiFlag
        {
            None = 0,
            Intro_ShowingDetail = 1
        }
    }
}
