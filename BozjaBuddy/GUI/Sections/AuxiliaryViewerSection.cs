using Dalamud.Bindings.ImGui;
using BozjaBuddy.Data;
using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using System.Text.Json;
using Dalamud.Logging;
using BozjaBuddy.Utils;
using Dalamud.Interface.Style;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.TextureWraps;

namespace BozjaBuddy.GUI.Sections
{
    internal class AuxiliaryViewerSection : Section
    {
        public static Dictionary<int, bool> mTabGenIds = new Dictionary<int, bool>();
        public static List<int> mTabGenIdsToDraw = new List<int>();
        public static bool mIsRefreshRequired = false;
        public static TextureCollection? mTextureCollection = null;
        public static int _mGenIdToTabFocus = -1;
        private static ImGuiTabBarFlags AUXILIARY_TAB_FLAGS = ImGuiTabBarFlags.Reorderable | ImGuiTabBarFlags.AutoSelectNewTabs | ImGuiTabBarFlags.ListPopupButton;
        public static GUIFilter mGUIFilter = new GUIFilter();
        unsafe static ImGuiTextFilterPtr mFilter = new ImGuiTextFilterPtr(ImGuiNative.ImGuiTextFilter(null));

        protected override Plugin mPlugin { get; set; }

        public AuxiliaryViewerSection(Plugin tPLugin)
        {
            this.mPlugin = tPLugin;
        }

        public override bool DrawGUI()
        {
            if (mTabGenIds.Keys.Count == 0) return false;
            if (ImGui.BeginTabBar("Aux", AuxiliaryViewerSection.AUXILIARY_TAB_FLAGS))
            {
                int tCurr = 0;
                while (true)
                {
                    if (tCurr >= AuxiliaryViewerSection.mTabGenIdsToDraw.Count) break;
                    this.DrawTab(this.mPlugin.mBBDataManager.mGeneralObjects[AuxiliaryViewerSection.mTabGenIdsToDraw[tCurr]]);
                    tCurr++;
                }
                ImGui.EndTabBar();
            }
            return true;
        }
        private void DrawTab(GeneralObject pObj)
        {
            bool[] tIsOpened = { true };
            
            if (pObj.mTabColor != null) ImGui.PushStyleColor(ImGuiCol.Tab, pObj.mTabColor.Value);

            if (ImGui.BeginTabItem($"{pObj.mName}##{pObj.mId}", ref tIsOpened[0], pObj.GetGenId() == AuxiliaryViewerSection._mGenIdToTabFocus
                                                                    ? ImGuiTabItemFlags.SetSelected
                                                                    : ImGuiTabItemFlags.None))
            {
                if (pObj.GetGenId() == AuxiliaryViewerSection._mGenIdToTabFocus) AuxiliaryViewerSection._mGenIdToTabFocus = -1;   // Release selected
                ImGui.BeginChild($"##{pObj.mId}", new Vector2(ImGui.GetWindowWidth(), this.mPlugin.Configuration.isAuxiVisible < 2
                                                                      ? 390
                                                                      : ImGui.GetWindowHeight() - ImGui.GetStyle().WindowPadding.Y
                                                                      ));
                this.DrawTabHeader(pObj);
                this.DrawTabContent(pObj);
                ImGui.Separator();
                ImGui.EndChild();
                ImGui.EndTabItem();
            }
            if (pObj.mTabColor != null) ImGui.PopStyleColor();

            if (!tIsOpened[0])
            {
                AuxiliaryViewerSection.RemoveTab(this.mPlugin, pObj.GetGenId());
            }
        }

        public void DrawTabHeader(GeneralObject pObj)
        {
            // Icon
            IDalamudTextureWrap? tIconWrap;
            Vector2? tSize = null;
            switch (pObj.GetSalt())
            {
                case GeneralObject.GeneralObjectSalt.Fragment:
                    //PluginLog.LogDebug($"DrawTab(): Active iconIds in sheet {TextureCollection.Sheet.Item.ToString()}: {String.Join(", ", AuxiliaryViewerSection.mTextureCollection!.mIcons[Sheet.Item].Keys)}");
                    //PluginLog.LogDebug($"DrawTab(): Loading icon of itemId {pObj.mId} from sheet {TextureCollection.Sheet.Item.ToString()}");
                    tIconWrap = AuxiliaryViewerSection.mTextureCollection?.GetTextureFromItemId(Convert.ToUInt32(pObj.mId), TextureCollection.Sheet.Item);
                    break;
                case GeneralObject.GeneralObjectSalt.Fate:
                    tIconWrap = AuxiliaryViewerSection.mTextureCollection?.GetStandardTexture((uint)this.mPlugin.mBBDataManager.mFates[pObj.mId].mType);
                    break;
                case GeneralObject.GeneralObjectSalt.Mob:
                    tIconWrap = AuxiliaryViewerSection.mTextureCollection?.GetStandardTexture((uint)this.mPlugin.mBBDataManager.mMobs[pObj.mId].mType);
                    break;
                case GeneralObject.GeneralObjectSalt.FieldNote:
                    tIconWrap = UtilsGameData.kTexCol_FieldNote?.GetTextureFromItemId(Convert.ToUInt32(pObj.mId), TextureCollection.Sheet.FieldNote);
                    if (tIconWrap != null) tSize = new Vector2(tIconWrap.Width * 0.5f, tIconWrap.Height * 0.5f);
                    break;
                default:
                    tIconWrap = AuxiliaryViewerSection.mTextureCollection?.GetTextureFromItemId(Convert.ToUInt32(pObj.mId));
                    if (tIconWrap == null)      // try reloading the texture, which is sometimes lost due to LoadoutTab's disposing process
                    {
                        AuxiliaryViewerSection.mTextureCollection?.AddTextureFromItemId(Convert.ToUInt32(pObj.mId), GeneralObject.GeneralObjectSalt.LostAction);
                        tIconWrap = AuxiliaryViewerSection.mTextureCollection?.GetTextureFromItemId(Convert.ToUInt32(pObj.mId));
                    }
                    break;
            }
            if (tIconWrap != null)
            {
                ImGui.Image(tIconWrap.Handle, tSize ?? new System.Numerics.Vector2(tIconWrap.Width, tIconWrap.Height));
                ImGui.SameLine();           // Do not Sameline() if there's no image, since it'll Sameline() to the TabItem above
            }
            // Name and Details and Location
            ImGui.BeginGroup();
            UtilsGUI.SelectableLink_WithPopup(this.mPlugin, pObj.mName, pObj.GetGenId());
            ImGui.Text(pObj.mDetail);
            if (pObj.mLocation != null)
            {
                ImGui.SameLine();
                UtilsGUI.LocationLinkButton(this.mPlugin, pObj.mLocation, true, pRightAlignOffset: 12f);
            }
            ImGui.EndGroup();
        }
        public void DrawTabContent(GeneralObject pObj)
        {
            if (ImGui.BeginTabBar(pObj.mName))
            {
                // Description
                if (ImGui.BeginTabItem("說明"))
                {
                    this.DrawTabContent_Description(pObj);
                    ImGui.EndTabItem();
                }
                // Sources
                if (ImGui.BeginTabItem("來源"))
                {
                    this.DrawTabContent_Source(pObj);
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }
        public void DrawTabContent_Description(GeneralObject pObj)
        {
            ImGui.BeginChild("", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y), false, ImGuiWindowFlags.None);
            ImGui.PushTextWrapPos(0);
            // Fate chains
            if (pObj.GetSalt() == GeneralObject.GeneralObjectSalt.Fate
                && (this.mPlugin.mBBDataManager.mFates[pObj.mId].mChainFatePrev != -1
                    || this.mPlugin.mBBDataManager.mFates[pObj.mId].mChainFateNext != -1))
            {
                int iCurrFateId = this.mPlugin.mBBDataManager.mFates[pObj.mId].mChainFatePrev != -1
                    ? this.mPlugin.mBBDataManager.mFates[pObj.mId].mChainFatePrev
                    : this.mPlugin.mBBDataManager.mFates[pObj.mId].mChainFateNext;
                while (this.mPlugin.mBBDataManager.mFates[iCurrFateId].mChainFatePrev != -1)        // Find the starting point of FATE chain
                    iCurrFateId = this.mPlugin.mBBDataManager.mFates[iCurrFateId].mChainFatePrev;
                ImGui.Text("系列：");
                do
                {
                    ImGui.SameLine();
                    UtilsGUI.SelectableLink_WithPopup(this.mPlugin,
                        this.mPlugin.mBBDataManager.mFates[iCurrFateId].mName,
                        this.mPlugin.mBBDataManager.mFates[iCurrFateId].GetGenId(),
                        true);
                    iCurrFateId = this.mPlugin.mBBDataManager.mFates[iCurrFateId].mChainFateNext;
                }
                while (iCurrFateId != -1);
                ImGui.Separator();
            }
            if (pObj.mIGMarkup == null) 
                ImGui.TextUnformatted(pObj.mDescription);
            else
                pObj.mIGMarkup!.DrawGUI();
            ImGui.PopTextWrapPos();
            ImGui.EndChild();
        }
        public void DrawTabContent_Source(GeneralObject pObj)
        {
            ImGui.BeginChild("", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y), false, ImGuiWindowFlags.None);
            if (pObj.mLinkFragments.Count != 0 && ImGui.CollapsingHeader($"碎片 ({pObj.mLinkFragments.Count})"))
            {
                foreach (int iId in pObj.mLinkFragments)
                {
                    Fragment tFragment = this.mPlugin.mBBDataManager.mFragments[iId];
                    // LOCATION
                    if (tFragment.mLocation != null)
                    {
                        UtilsGUI.LocationLinkButton(this.mPlugin, tFragment.mLocation);
                        ImGui.SameLine();
                    }
                    // NAME
                    UtilsGUI.SelectableLink_WithPopup(this.mPlugin, tFragment.mName, tFragment.GetGenId());
                    // Display as vendor's stock
                    if (pObj.GetSalt() == GeneralObject.GeneralObjectSalt.Vendor)
                    {
                        Vendor tVendor = this.mPlugin.mBBDataManager.mVendors[pObj.mId];
                        ImGui.SameLine();
                        ImGui.TextUnformatted($" ({tVendor.GetAmountPriceCurrency(iId).Item1}) {tVendor.GetAmountPriceCurrency(iId).Item2} SheetNames.Currency(this.mPlugin, tVendor.GetAmountPriceCurrency(iId).Item3)");
                    }
                    ImGui.Separator();
                }
            }
            if (pObj.mLinkFates.Count != 0 && ImGui.CollapsingHeader($"危命／特殊遭遇戰 ({pObj.mLinkFates.Count})"))
            {
                foreach (int iId in pObj.mLinkFates)
                {
                    BozjaBuddy.Data.Fate tFate = this.mPlugin.mBBDataManager.mFates[iId];
                    // LOCATION
                    if (tFate.mLocation != null)
                    {
                        UtilsGUI.LocationLinkButton(this.mPlugin, tFate.mLocation);
                        ImGui.SameLine();
                    }
                    // NAME
                    UtilsGUI.SelectableLink_WithPopup(this.mPlugin, $"{tFate.mName}", tFate.GetGenId());
                    ImGui.Separator();
                }
            }
            if (pObj.mLinkMobs.Count != 0 && ImGui.CollapsingHeader($"敵人 ({pObj.mLinkMobs.Count})"))
            {
                foreach (int iId in pObj.mLinkMobs)
                {
                    Mob tMob = this.mPlugin.mBBDataManager.mMobs[iId];
                    // LOCATION
                    if (tMob.mLocation != null)
                    {
                        UtilsGUI.LocationLinkButton(this.mPlugin, tMob.mLocation);
                        ImGui.SameLine();
                    }
                    // NAME
                    UtilsGUI.SelectableLink_WithPopup(this.mPlugin, $"{tMob.mName}", tMob.GetGenId());
                    ImGui.Separator();
                }
            }
            if (pObj.mLinkActions.Count != 0 && ImGui.CollapsingHeader($"失傳技能 ({pObj.mLinkActions.Count})"))
            {
                foreach (int iId in pObj.mLinkActions)
                {
                    LostAction tAction = this.mPlugin.mBBDataManager.mLostActions[iId];
                    // LOCATION
                    if (tAction.mLocation != null)
                    {
                        UtilsGUI.LocationLinkButton(this.mPlugin, tAction.mLocation);
                        ImGui.SameLine();
                    }
                    // NAME
                    UtilsGUI.SelectableLink_WithPopup(this.mPlugin, $"{tAction.mName}", tAction.GetGenId());
                    ImGui.Separator();
                }
            }
            if (pObj.mLinkVendors.Count != 0 && ImGui.CollapsingHeader($"商人 ({pObj.mLinkVendors.Count})"))
            {
                foreach (int iId in pObj.mLinkVendors)
                {
                    Vendor tVendor = this.mPlugin.mBBDataManager.mVendors[iId];
                    // LOCATION
                    if (tVendor.mLocation != null)
                    {
                        UtilsGUI.LocationLinkButton(this.mPlugin, tVendor.mLocation);
                        ImGui.SameLine();
                    }
                    // NAME
                    UtilsGUI.SelectableLink_WithPopup(this.mPlugin, $"{tVendor.mName}\t({tVendor.GetAmountPriceCurrency(pObj.mId).Item1}) {tVendor.GetAmountPriceCurrency(pObj.mId).Item2} SheetNames.Currency(this.mPlugin, tVendor.GetAmountPriceCurrency(pObj.mId).Item3)", tVendor.GetGenId());
                    ImGui.Separator();
                }
            }
            if (pObj.mLinkFieldNotes.Count != 0 && ImGui.CollapsingHeader($"戰地筆記 ({pObj.mLinkFieldNotes.Count})"))
            {
                foreach (int iId in pObj.mLinkFieldNotes)
                {
                    FieldNote tFieldNote = this.mPlugin.mBBDataManager.mFieldNotes[iId];
                    // LOCATION
                    if (tFieldNote.mLocation != null)
                    {
                        UtilsGUI.LocationLinkButton(this.mPlugin, tFieldNote.mLocation);
                        ImGui.SameLine();
                    }
                    // NAME
                    UtilsGUI.SelectableLink_WithPopup(this.mPlugin, $"{tFieldNote.mName}", tFieldNote.GetGenId());
                    ImGui.Separator();
                }
            }
            ImGui.EndChild();
        }

        /// <summary>
        /// For first-time binding a GeneralObject to an Auxiliary Tab. Do nothing if the object has already been bound.
        /// </summary>
        /// <param name="tPlugin"></param>
        /// <param name="pGenId"></param>
        public static void BindToGenObj(Plugin pPlugin, int pGenId)
        {
            if (AuxiliaryViewerSection.mTabGenIds.ContainsKey(pGenId)) return;
            AuxiliaryViewerSection.mTabGenIds.Add(pGenId, false);
        }
        public static void UnbindFromGenObj(Plugin pPlugin, int pGenId)
        {
            AuxiliaryViewerSection.mTabGenIds.Remove(pGenId);
        }
        public static void AddTab(Plugin pPlugin, int pGenId)
        {
            if (!AuxiliaryViewerSection.mTabGenIds.ContainsKey(pGenId)) return;
            int tId = pPlugin.mBBDataManager.mGeneralObjects[pGenId].mId;
            GeneralObject.GeneralObjectSalt tSalt = pPlugin.mBBDataManager.mGeneralObjects[pGenId].GetSalt();
            AuxiliaryViewerSection.mTabGenIds[pGenId] = true;
            AuxiliaryViewerSection.mTabGenIdsToDraw.Add(pGenId);
            AuxiliaryViewerSection._mGenIdToTabFocus = pGenId;
            AuxiliaryViewerSection.mTextureCollection ??= new TextureCollection(pPlugin);
            AuxiliaryViewerSection.mTextureCollection.AddTextureFromItemId(Convert.ToUInt32(tId), tSalt);
        }
        public static void RemoveTab(Plugin pPlugin, int pGenId)
        {
            if (!AuxiliaryViewerSection.mTabGenIds.ContainsKey(pGenId)) return;
            int tId = pPlugin.mBBDataManager.mGeneralObjects[pGenId].mId;
            GeneralObject.GeneralObjectSalt tSalt = pPlugin.mBBDataManager.mGeneralObjects[pGenId].GetSalt();
            AuxiliaryViewerSection.mTabGenIds[pGenId] = false;
            AuxiliaryViewerSection.mTabGenIdsToDraw.Remove(pGenId);
            AuxiliaryViewerSection.mTextureCollection?.RemoveTextureFromItemId(Convert.ToUInt32(tId), tSalt);
        }

        public static void GUIAlignRight(float pTargetItemWidth)
        {
            ImGuiStylePtr tStyle = ImGui.GetStyle();
            float tPadding = tStyle.WindowPadding.X + tStyle.FramePadding.X * 2 + tStyle.ScrollbarSize;
            ImGui.SetCursorPosX(ImGui.GetWindowWidth() - pTargetItemWidth - tPadding);
        }
        public static void GUIAlignRight(string pText)
        {
            AuxiliaryViewerSection.GUIAlignRight(ImGui.CalcTextSize(pText).X);
        }

        public override void DrawGUIDebug()
        {
            string tRes = "";
            foreach (int i in AuxiliaryViewerSection.mTabGenIdsToDraw) tRes += i.ToString();
            ImGui.Text($"Aux: {tRes}");
        }
        public override void Dispose()
        {
            unsafe 
            {
                AuxiliaryViewerSection.mFilter.Destroy();
            }
        }
    }
}
