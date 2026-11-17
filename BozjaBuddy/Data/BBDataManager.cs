using System.Data.SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using BozjaBuddy.GUI.Sections;
using System.Text.Json;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Lumina.Excel.Sheets;
using System.Linq;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.TextureWraps;
using FFXIVClientStructs.Interop;

namespace BozjaBuddy.Data
{
    public class BBDataManager : IDisposable
    {
        private Plugin mPlugin;
        private string mCsLostAction;
        public Dictionary<int, GeneralObject> mGeneralObjects;
        public Dictionary<int, Fragment> mFragments;
        public Dictionary<int, LostAction> mLostActions;
        public Dictionary<int, Fate> mFates;
        public Dictionary<int, Mob> mMobs;
        public Dictionary<int, Vendor> mVendors;
        public Dictionary<int, FieldNote> mFieldNotes;
        private Dictionary<int, Item> mItems;
        public Lumina.Excel.ExcelSheet<Lumina.Excel.Sheets.Action>? mSheetAction;
        public Lumina.Excel.ExcelSheet<Lumina.Excel.Sheets.Item>? mSheetItem;
        public Lumina.Excel.ExcelSheet<Lumina.Excel.Sheets.Quest>? mSheetQuest;
        public Lumina.Excel.ExcelSheet<Lumina.Excel.Sheets.MYCWarResultNotebook>? mSheetMycWarResultNotebook;
        public Lumina.Excel.ExcelSheet<ENpcResident>? mSheetNpc;
        public List<List<int>> mUiMap_MycItemBox;
        public Dictionary<string, IDalamudTextureWrap?> mImages;

        public BBDataManager(Plugin pPlugin) 
        {
            this.mPlugin = pPlugin;
            this.mFragments = new Dictionary<int, Fragment>();
            this.mLostActions = new Dictionary<int, LostAction>();
            this.mFates = new Dictionary<int, Fate>();
            this.mMobs = new Dictionary<int, Mob>();
            this.mVendors = new Dictionary<int, Vendor>();
            this.mFieldNotes = new Dictionary<int, FieldNote>();
            this.mGeneralObjects = new Dictionary<int, GeneralObject>();
            this.mItems = new();

            // lumina
            this.mSheetAction = this.mPlugin.DataManager.Excel.GetSheet<Lumina.Excel.Sheets.Action>();
            this.mSheetItem = this.mPlugin.DataManager.Excel.GetSheet<Lumina.Excel.Sheets.Item>();
            this.mSheetMycWarResultNotebook = this.mPlugin.DataManager.Excel.GetSheet<Lumina.Excel.Sheets.MYCWarResultNotebook>();
            this.mSheetQuest = this.mPlugin.DataManager.Excel.GetSheet<Lumina.Excel.Sheets.Quest>();
            this.mSheetNpc = this.mPlugin.DataManager.Excel.GetSheet<ENpcResident>();

            // db
            this.mCsLostAction = String.Format("Data Source={0}", this.mPlugin.DATA_PATHS["db"]);
            using (SQLiteConnection mConnLostAction = new SQLiteConnection(this.mCsLostAction))
            {
                mConnLostAction.Open();
                SQLiteCommand tCommand = mConnLostAction.CreateCommand();

                this.DataSetUpFragment(tCommand);
                this.DataSetUpAction(tCommand);
                this.DataSetUpFate(tCommand);
                this.DataSetUpMob(tCommand);
                this.DataSetUpVendor(tCommand);
                this.DataSetUpFieldNote(tCommand);
            }
            // lumina 2
            this.ApplyLiveNames();
            this.SetUpGeneralObjects();
            this.SetUpAuxiliary();

            // json - UIMap
            List<UIMap_MycItemBoxRow>? tUIMap_MycItemBox = JsonSerializer.Deserialize<List<UIMap_MycItemBoxRow>>(
                        File.ReadAllText(this.mPlugin.DATA_PATHS["UIMap_LostAction.json"])
                    );
            if (tUIMap_MycItemBox != null)
            {
                this.SetUpUiMap(tUIMap_MycItemBox);
                this.mUiMap_MycItemBox = tUIMap_MycItemBox.Select(o => o.objIds ?? new List<int>()).ToList();
            }

            // db images
            this.mImages = new();
            try
            {
                foreach (string tImagePath in Directory.GetFiles(
                                    Path.Combine(this.mPlugin.PluginInterface.AssemblyLocation.DirectoryName!, @"db\img"),
                                    "*.png",
                                    SearchOption.TopDirectoryOnly
                ))
                {
                    // get IDalamudTextureWrap
                    var b = File.ReadAllBytes(tImagePath);
                    //var twrap = this.mPlugin.PluginInterface.UiBuilder.LoadImage(b);
                    var twrap = this.mPlugin.TextureProvider.CreateFromImageAsync(b).Result;
                    // prep key
                    string fname = tImagePath.Split("\\").LastOrDefault("");
                    if (fname == "") { continue; }
                    // add to collection
                    this.mImages.Add( fname, twrap);
                }
            }
            catch (Exception e) { this.mPlugin.PLog.Error(e.Message); }
        }

        private Dictionary<int, TDbObj> DbLoader<TDbObj> (out Dictionary<int, TDbObj> pDict, SQLiteCommand pCommand, string pQuery, Func<Plugin, SQLiteDataReader, TDbObj> tDel, string pKeyCollumn = "id")
        {
            pDict = new Dictionary<int, TDbObj>();
            pCommand.CommandText = pQuery;
            SQLiteDataReader tReader = pCommand.ExecuteReader();

            while (tReader.Read())
            {
                pDict.Add(
                    (int)(long)tReader[pKeyCollumn],
                    (TDbObj)tDel(this.mPlugin, tReader)
                );
            }
            tReader.Close();

            return pDict;
        }

        private void DataSetUpFragment(SQLiteCommand pCommand)
        {
            DbLoader<Fragment>(out this.mFragments, pCommand, "SELECT * FROM Fragment;", (p, t) => new Fragment(p, t));
            foreach (int id in this.mFragments.Keys)
            {
                string iLinkCol;

                // Linking idAction
                iLinkCol = "idAction";
                pCommand.CommandText = $@"SELECT FragmentToAction.{iLinkCol} 
                                            FROM FragmentToAction 
                                            WHERE FragmentToAction.id = {id};";
                SQLiteDataReader tReader = pCommand.ExecuteReader();
                while (tReader.Read())
                {
                    this.mFragments[id].mLinkActions.Add((int)(long)tReader[iLinkCol]);
                }
                tReader.Close();

                // Linking idFate
                iLinkCol = "idFate";
                pCommand.CommandText = $@"SELECT DISTINCT FragmentToFate.{iLinkCol}
                                            FROM FragmentToFate
                                            WHERE FragmentToFate.id = {id};";
                tReader = pCommand.ExecuteReader();
                while (tReader.Read())
                {
                    this.mFragments[id].mLinkFates.Add((int)(long)tReader[iLinkCol]);
                }
                tReader.Close();

                // Linking idMob
                iLinkCol = "idMob";
                pCommand.CommandText = $@"SELECT DISTINCT FragmentToMob.{iLinkCol}
                                            FROM FragmentToMob
                                            WHERE FragmentToMob.id = {id};";
                tReader = pCommand.ExecuteReader();
                while (tReader.Read())
                {
                    this.mFragments[id].mLinkMobs.Add((int)(long)tReader[iLinkCol]);
                }

                tReader.Close();
                // Linking idVendor
                iLinkCol = "id";
                pCommand.CommandText = $@"SELECT VendorToItem.{iLinkCol}
                                            FROM VendorToItem
                                            WHERE VendorToItem.idItem = {id};";
                tReader = pCommand.ExecuteReader();
                while (tReader.Read())
                {
                    this.mFragments[id].mLinkVendors.Add((int)(long)tReader[iLinkCol]);
                }

                tReader.Close();
            }
        }
        private void DataSetUpAction(SQLiteCommand pCommand)
        {
            DbLoader<LostAction>(out this.mLostActions, pCommand, "SELECT * FROM LostAction;", (p, t) => new LostAction(p, t));

            foreach (int id in this.mLostActions.Keys)
            {
                string iLinkCol;

                // Linking idAction
                iLinkCol = "id";
                pCommand.CommandText = $@"SELECT FragmentToAction.{iLinkCol} 
                                            FROM FragmentToAction 
                                            WHERE FragmentToAction.idAction = {id};";
                SQLiteDataReader tReader = pCommand.ExecuteReader();
                while (tReader.Read())
                {
                    this.mLostActions[id].mLinkFragments.Add((int)(long)tReader[iLinkCol]);
                }

                tReader.Close();
            }
        }
        private void DataSetUpFate(SQLiteCommand pCommand)
        {
            DbLoader<Fate>(out this.mFates, pCommand, "SELECT * FROM Fate;", (p, t) => new Fate(p, t));

            foreach (int id in this.mFates.Keys)
            {
                string iLinkCol;

                // Linking idAction
                iLinkCol = "id";
                pCommand.CommandText = $@"SELECT FragmentToFate.{iLinkCol} 
                                            FROM FragmentToFate 
                                            WHERE FragmentToFate.idFate = {id};";
                SQLiteDataReader tReader = pCommand.ExecuteReader();
                while (tReader.Read())
                {
                    this.mFates[id].mLinkFragments.Add((int)(long)tReader[iLinkCol]);
                }
                tReader.Close();
                // Linking idFieldNote
                iLinkCol = "id";
                pCommand.CommandText = $@"SELECT FieldNoteToFate.{iLinkCol} 
                                            FROM FieldNoteToFate 
                                            WHERE FieldNoteToFate.idFate = {id};";
                tReader = pCommand.ExecuteReader();
                while (tReader.Read())
                {
                    this.mFates[id].mLinkFieldNotes.Add((int)(long)tReader[iLinkCol]);
                }

                tReader.Close();
            }
        }
        private void DataSetUpMob(SQLiteCommand pCommand)
        {
            DbLoader<Mob>(out this.mMobs, pCommand, "SELECT * FROM Mob;", (p, t) => new Mob(p, t));

            foreach (int id in this.mMobs.Keys)
            {
                string iLinkCol;

                // Linking idAction
                iLinkCol = "id";
                pCommand.CommandText = $@"SELECT FragmentToMob.{iLinkCol} 
                                            FROM FragmentToMob
                                            WHERE FragmentToMob.idMob = {id};";
                SQLiteDataReader tReader = pCommand.ExecuteReader();
                while (tReader.Read())
                {
                    this.mMobs[id].mLinkFragments.Add((int)(long)tReader[iLinkCol]);
                }

                tReader.Close();
            }
        }
        private void DataSetUpVendor(SQLiteCommand pCommand)
        {
            DbLoader<Vendor>(out this.mVendors, pCommand, "SELECT * FROM Vendor;", (p, t) => new Vendor(p, t));
            foreach (int id in this.mVendors.Keys)
            {
                string iLinkCol;

                // Linking idFragment
                iLinkCol = "idItem";
                pCommand.CommandText = $@"SELECT VendorToItem.{iLinkCol}, VendorToItem.idItem, VendorToItem.amount, VendorToItem.price, VendorToItem.idCurrencyItem
                                            FROM VendorToItem
                                            WHERE VendorToItem.id = {id};";
                SQLiteDataReader tReader = pCommand.ExecuteReader();
                while (tReader.Read())
                {
                    this.mVendors[id].mLinkFragments.Add((int)(long)tReader[iLinkCol]);
                    this.mVendors[id].AddItemToStock((int)(long)tReader["idItem"], (int)(long)tReader["amount"], (int)(long)tReader["price"], (int)(long)tReader["idCurrencyItem"]);
                }

                tReader.Close();
            }
        }
        private void DataSetUpFieldNote(SQLiteCommand pCommand)
        {
            DbLoader<FieldNote>(out this.mFieldNotes, pCommand, "SELECT * FROM FieldNote;", (p, t) => new FieldNote(p, t));

            foreach (int id in this.mFieldNotes.Keys)
            {
                string iLinkCol;

                // Linking
                iLinkCol = "idFate";
                pCommand.CommandText = $@"SELECT FieldNoteToFate.{iLinkCol} 
                                            FROM FieldNoteToFate
                                            WHERE FieldNoteToFate.id = {id};";
                SQLiteDataReader tReader = pCommand.ExecuteReader();
                while (tReader.Read())
                {
                    this.mFieldNotes[id].mLinkFates.Add((int)(long)tReader[iLinkCol]);
                }

                tReader.Close();
            }
        }

        /// <summary>
        /// Swap English action-name tokens inside community blurbs for the live Lumina name.
        /// </summary>
        private void ApplyLiveNames()
        {
            var pairs = this.mLostActions.Values
                .Where(a => !string.IsNullOrEmpty(a.mNameEn) && a.mNameEn != a.mName)
                .OrderByDescending(a => a.mNameEn.Length)
                .ToList();
            if (pairs.Count == 0) return;
            string Swap(string s)
            {
                foreach (var a in pairs) s = s.Replace(a.mNameEn, a.mName);
                return s;
            }
            foreach (var a in this.mLostActions.Values)
            {
                a.mDescription_semi = Swap(a.mDescription_semi);
                a.mDescription_full = Swap(a.mDescription_full);
                a.mUsage = Swap(a.mUsage);
                a.mDescription = a.mDescription_full;
                a.mIGMarkup = new GUI.IGMarkup.IGMarkup(a.mDescription_full);
                a.ResetReprUiTooltip();
            }
        }

        private void SetUpGeneralObjects()
        {
            foreach (int id in this.mFragments.Keys)
                this.mGeneralObjects[this.mFragments[id].GetGenId()] = this.mFragments[id];
            foreach (int id in this.mMobs.Keys)
                this.mGeneralObjects[this.mMobs[id].GetGenId()] = this.mMobs[id];
            foreach (int id in this.mFates.Keys)
                this.mGeneralObjects[this.mFates[id].GetGenId()] = this.mFates[id];
            foreach (int id in this.mLostActions.Keys)
                this.mGeneralObjects[this.mLostActions[id].GetGenId()] = this.mLostActions[id];
            foreach (int id in this.mVendors.Keys)
                this.mGeneralObjects[this.mVendors[id].GetGenId()] = this.mVendors[id];
            foreach (int id in this.mFieldNotes.Keys)
                this.mGeneralObjects[this.mFieldNotes[id].GetGenId()] = this.mFieldNotes[id];
        }

        public void SetUpAuxiliary()
        {
            foreach (int id in this.mFragments.Keys)
                AuxiliaryViewerSection.BindToGenObj(this.mPlugin, this.mFragments[id].GetGenId());
            foreach (int id in this.mMobs.Keys)
                AuxiliaryViewerSection.BindToGenObj(this.mPlugin, this.mMobs[id].GetGenId());
            foreach (int id in this.mFates.Keys)
                AuxiliaryViewerSection.BindToGenObj(this.mPlugin, this.mFates[id].GetGenId());
            foreach (int id in this.mLostActions.Keys)
                AuxiliaryViewerSection.BindToGenObj(this.mPlugin, this.mLostActions[id].GetGenId());
            foreach (int id in this.mVendors.Keys)
                AuxiliaryViewerSection.BindToGenObj(this.mPlugin, this.mVendors[id].GetGenId());
            foreach (int id in this.mFieldNotes.Keys)
                AuxiliaryViewerSection.BindToGenObj(this.mPlugin, this.mFieldNotes[id].GetGenId());
        }
        public void SetUpUiMap(List<UIMap_MycItemBoxRow> tUIMap_MycInfo)
        {
            unsafe
            {
                if (tUIMap_MycInfo != null)
                {
                    foreach (UIMap_MycItemBoxRow iRow in tUIMap_MycInfo)
                    {
                        if (iRow.objIds == null) { continue; }
                        for (int i = 0; i < iRow.objIds.Count; i++)
                        {
                            this.mLostActions[iRow.objIds[i]].mUINode = new(
                                    "MYCItemBox",
                                    new List<int>(new int[] { iRow.head, iRow.head + i + 1 }),
                                    iRow.order,
                                    i + 1
                                );
                        }
                    }
                }
            }
        }
        /// <summary> 
        /// Get BozjaBuddy Item. 
        /// If not available in cache, it will try to pull data from lumina item sheet and use that to create a BB Item, then cache it. 
        /// Returns null if item id not found in lumina item sheet.
        /// </summary>
        public Item? GetItem(int pLuminaItemId)
        {
            if (!this.mItems.TryGetValue(pLuminaItemId, out var tItem) || tItem == null)
            {
                // Init data
                var luminaItem = this.mSheetItem?.GetRow(Convert.ToUInt32(pLuminaItemId));
                if (luminaItem == null) return null;
                tItem = new(this.mPlugin, luminaItem.Value);

                // Add data
                this.mItems.TryAdd(pLuminaItemId, tItem);

                // Setups
                this.mGeneralObjects[tItem.GetGenId()] = tItem;
                AuxiliaryViewerSection.BindToGenObj(this.mPlugin, tItem.GetGenId());
            }

            return tItem;
        }

        public void Dispose()
        {
            foreach (var tTex in this.mImages.Values)
            {
                tTex?.Dispose();
            }
        }

        /// <summary>
        /// Does not take care of Section's idList. 
        /// Said section needs to implement RefreshIdList() if they have dynamic idList.
        /// </summary>
        public static void DynamicAddGeneralObject<T>(Plugin pPlugin, T pGenObj, Dictionary<int, T> pDataManagerItemDict) where T : GeneralObject
        {
            // Save to cache
            pDataManagerItemDict[pGenObj.mId] = pGenObj;
            pPlugin.mBBDataManager.mGeneralObjects[pGenObj.GetGenId()] = pGenObj;
            AuxiliaryViewerSection.BindToGenObj(pPlugin, pGenObj.GetGenId());
            // Write to disc
            AuxiliaryViewerSection.mIsRefreshRequired = true;
        }
        /// <summary>
        /// Does not take care of Section's idList. 
        /// Said section needs to implement RefreshIdList() if they have dynamic idList.
        /// </summary>
        public static void DynamicRemoveGeneralObject<T>(Plugin pPlugin, T pGenObj, Dictionary<int, T> pDataManagerItemDict) where T : GeneralObject
        {
            // Remove from cache
            pDataManagerItemDict.Remove(pGenObj.mId);
            pPlugin.mBBDataManager.mGeneralObjects.Remove(pGenObj.GetGenId());
            AuxiliaryViewerSection.UnbindFromGenObj(pPlugin, pGenObj.GetGenId());
            AuxiliaryViewerSection.mTabGenIdsToDraw.Remove(pGenObj.GetGenId());
            AuxiliaryViewerSection.mIsRefreshRequired = true;
        }

        public unsafe static AgentMycItemBox* GetAgentMycItemBox()
        {
            return (AgentMycItemBox*)Framework.Instance()
                ->GetUIModule()
                ->GetAgentModule()
                ->GetAgentByInternalId(AgentId.MycItemBox);
        }
    }

    public enum LostActionType
    {
        None,
        Offensive,
        Defensive,
        Restorative,
        Beneficial,
        Tactical,
        Detrimental,
        Item
    }
    public enum StatusId
    {
        None = 0,
        HoofingItA = 1778,
        HoofingItB = 1945,
        DutyAsAssigned = 2415
    }

    public class UIMap_MycItemBoxRow
    {
        public int order { get; set; }
        public int head { get; set; }
        public List<int>? objIds { get; set; }
    }
}
