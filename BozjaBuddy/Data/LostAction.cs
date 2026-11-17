using System.Data.SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using BozjaBuddy.GUI.GUIAssist;
using BozjaBuddy.Utils;
using System.Security.Cryptography;

namespace BozjaBuddy.Data
{
    public class LostAction : GeneralObject
    {
        protected override GeneralObjectSalt mIdSalt { get; set; } = GeneralObjectSalt.LostAction;
        public override int mId { get; set; } = 0;
        public override string mName { get; set; } = String.Empty;
        /// <summary>English database name, kept so blurbs can be rewritten with the live sheet name.</summary>
        public string mNameEn { get; set; } = String.Empty;
        public RoleFlag mRole { get; set; } = new RoleFlag();
        public string mDescription_semi { get; set; } = String.Empty;
        public string mUsage { get; set; } = String.Empty;
        public string mDescription_full { get; set; } = String.Empty;
        public int mWeight { get; set; } = 0;
        public LostActionType mType { get; set; } = LostActionType.Item;
        public double mCast { get; set; } = 0;
        public double mRecast { get; set; } = 0;
        public int mCharges { get; set; } = 0;
        public UINode_MycItemBox? mUINode = null;
        protected override Plugin mPlugin { get; set; }

        public LostAction(Plugin pPlugin, SQLiteDataReader pPackage) {
            this.mPlugin = pPlugin;
            mId = Convert.ToInt32(pPackage["id"]);
            mNameEn = (string)pPackage["name"];
            mName = SheetNames.Action(pPlugin, (uint)mId) ?? mNameEn;
            mRole = new RoleFlag(Convert.ToInt32(pPackage["role"]));
            mDescription_semi = (string)pPackage["effectSemi"];
            mUsage = (string)pPackage["usage"];
            mDescription_full = (string)pPackage["effectFull"];
            mLinkFragments = new List<int>(); //pPackage["fragment"];
            mWeight = Convert.ToInt32(pPackage["weight"] is System.DBNull ? (long)-1 : pPackage["weight"]);
            mType = (LostActionType)(int)(long)(pPackage["type"] is System.DBNull ? (long)6 : pPackage["type"]);
            mCast = Convert.ToDouble(pPackage["cast"] is System.DBNull ? -1 : pPackage["cast"]);
            mRecast = Convert.ToDouble(pPackage["cooldown"] is System.DBNull ? -1 : pPackage["cooldown"]);
            mCharges = Convert.ToInt32(pPackage["charges"] is System.DBNull ? -1 : pPackage["charges"]);

            this.mTabColor = UtilsGUI.Colors.GenObj_BlueAction;

            this.SetUpAuxiliary();
            this.SetUpNodeInfo();
        }

        public override string GetReprClipboardTooltip()
            => $"[{this.mName}] • [職能：{this.mRole}] • [次數：{this.mCharges}] • [重量：{this.mWeight}] • [詠唱／復唱：{this.mCast}秒／{this.mRecast}秒] • [{this.mDescription_semi.Replace("\n", "。")}] • [碎片：{String.Join("、", this.mLinkFragments.Select(id => this.mPlugin.mBBDataManager.mFragments[id].mName))}]";
        protected override string GenReprUiTooltip()
        {
            string tFateText = "";
            foreach (int iId in this.mLinkFragments)
            {
                if (!this.mPlugin.mBBDataManager.mFragments.ContainsKey(iId)) continue;
                tFateText += string.Join(
                    "\n\t\t\t\t\t\t\t",
                    this.mPlugin.mBBDataManager.mFragments[iId].mLinkFates
                                       .Select(o => this.mPlugin.mBBDataManager.mFates.ContainsKey(o)
                                                ? this.mPlugin.mBBDataManager.mFates[o].mName
                                                  + (this.mPlugin.mBBDataManager.mFates[o].mLinkVendors.Count > 0
                                                    ? "（戰勲簇）"
                                                    : "")
                                                : "不明")
                                       .ToList()
                    );
            }

            string tMobDrops = "";
            foreach (int iFragId in this.mLinkFragments)
            {
                if (!this.mPlugin.mBBDataManager.mFragments.ContainsKey(iFragId)) continue;
                tMobDrops += string.Join(
                    "\n\t\t\t\t\t\t\t",
                    this.mPlugin.mBBDataManager.mFragments[iFragId].mLinkMobs
                                       .Select(o => this.mPlugin.mBBDataManager.mMobs.ContainsKey(o)
                                                ? this.mPlugin.mBBDataManager.mMobs[o].mName
                                                : "不明")
                                       .ToList()
                    );
            }
            string tFragDrops = string.Join(
                ", ",
                this.mLinkFragments.Select(o => this.mPlugin.mBBDataManager.mFragments.ContainsKey(o)
                                            ? this.mPlugin.mBBDataManager.mFragments[o].mName
                                                + (this.mPlugin.mBBDataManager.mFragments[o].mLinkVendors.Count > 0
                                                    ? "（戰勲簇）"
                                                    : "")
                                            : "不明")
                                   .ToList()
                                    );

            this.mUiTooltip = $"名稱：\t{this.mName}"
                            + $"\n職能：\t{this.mRole}"
                            + $"\n\n{this.mDescription_semi}"
                            + $"\n\n碎片：\t{tFragDrops}"
                            + $"\n來源危命：\t{tFateText}"
                            + $"\n來源敵人：\t{tMobDrops}";
            return this.mUiTooltip;
        }

        protected override void SetUpAuxiliary()
        {
            this.mDetail = $"[{this.mCharges}/{this.mCharges}]\t•\t[重量：{this.mWeight}]\t•\t[詠唱：{this.mCast}秒]\t•\t[復唱：{this.mRecast}秒]";
            this.mDescription = $"{this.mDescription_full}";
            this.mIGMarkup = new GUI.IGMarkup.IGMarkup(this.mDescription_full);
        }
        protected override void SetUpNodeInfo()
        {
            this.mDetailPackage = new()
            {
                { TextureCollection.StandardIcon.Uses, $"{this.mCharges}/{this.mCharges}" },
                { TextureCollection.StandardIcon.Weight, $"{this.mWeight}" },
                { TextureCollection.StandardIcon.Cast, $"{this.mCast}" },
                { TextureCollection.StandardIcon.Recast, $"{this.mRecast}" }
            };
        }
    }
}
