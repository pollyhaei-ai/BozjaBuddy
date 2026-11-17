using System.Data.SQLite;
using System;
using System.Collections.Generic;
using BozjaBuddy.Utils;
using System.Linq;

namespace BozjaBuddy.Data
{
    public class Fate : GeneralObject
    {
        protected override GeneralObjectSalt mIdSalt { get; set; } = GeneralObjectSalt.Fate;
        public override int mId { get; set; }
        public override string mName { get; set; }
        public FateType mType { get; set; }
        public int mRewardMettleMin { get; set; }
        public int mRewardMettleMax { get; set; }
        public int mRewardExpMin { get; set; }
        public int mRewardExpMax { get; set; }
        public int mRewardTome { get; set; }
        public int mChainFatePrev { get; set; }
        public int mChainFateNext { get; set; }
        private string mTerritoryType { get; set; } = string.Empty;
        private double mMapCoordX { get; set; }
        private double mMapCoordY { get; set; }
        protected override Plugin mPlugin { get; set; }

        public Fate(Plugin pPlugin, SQLiteDataReader pPackage)
        {
            this.mPlugin = pPlugin;
            this.mId = Convert.ToInt32(pPackage["id"]);
            this.mName = (string)pPackage["name"];
            this.mType = (FateType)Convert.ToInt32(pPackage["type"] is System.DBNull ? -1 : pPackage["type"]);
            this.mRewardMettleMin = Convert.ToInt32(pPackage["rewardMettleMin"] is System.DBNull ? -1 : pPackage["rewardMettleMin"]);
            this.mRewardMettleMax = Convert.ToInt32(pPackage["rewardMettleMax"] is System.DBNull ? -1 : pPackage["rewardMettleMax"]);
            this.mRewardExpMin = Convert.ToInt32(pPackage["rewardExpMin"] is System.DBNull ? -1 : pPackage["rewardExpMin"]);
            this.mRewardExpMax = Convert.ToInt32(pPackage["rewardExpMax"] is System.DBNull ? -1 : pPackage["rewardExpMax"]);
            this.mRewardTome = Convert.ToInt32(pPackage["rewardTome"] is System.DBNull ? -1 : pPackage["rewardTome"]);
            this.mChainFatePrev = Convert.ToInt32(
                                    pPackage["chainFateIdPrev"] is System.DBNull || Convert.ToInt32(pPackage["chainFateIdPrev"]) == 0
                                    ? -1 : pPackage["chainFateIdPrev"]);
            this.mChainFateNext = Convert.ToInt32(
                                    pPackage["chainFateIdNext"] is System.DBNull || Convert.ToInt32(pPackage["chainFateIdNext"]) == 0
                                    ? -1 : pPackage["chainFateIdNext"]);
            this.mTerritoryType = (string)pPackage["territoryType"];
            this.mMapCoordX = Convert.ToDouble(pPackage["mapCoordX"] is System.DBNull ? -1 : pPackage["mapCoordX"]);
            this.mMapCoordY = Convert.ToDouble(pPackage["mapCoordY"] is System.DBNull ? -1 : pPackage["mapCoordY"]);

            this.mLinkFragments = new List<int>();
            this.mLocation = new Location(this.mPlugin, this.mTerritoryType, this.mMapCoordX, this.mMapCoordY);
            this.mTabColor = UtilsGUI.Colors.GenObj_PinkFate;

            this.SetUpAuxiliary();
            this.SetUpNodeInfo();
        }
        public override string GetReprClipboardTooltip()
        {
            string tFateChainText;
            List<string> tFateChainTexts = new();
            if (this.mPlugin.mBBDataManager.mFates[this.mId].mChainFatePrev != -1
                || this.mPlugin.mBBDataManager.mFates[this.mId].mChainFateNext != -1)
            {
                int iCurrFateId = this.mPlugin.mBBDataManager.mFates[this.mId].mChainFatePrev != -1
                                ? this.mPlugin.mBBDataManager.mFates[this.mId].mChainFatePrev
                                : this.mPlugin.mBBDataManager.mFates[this.mId].mChainFateNext;
                while (this.mPlugin.mBBDataManager.mFates[iCurrFateId].mChainFatePrev != -1)        // Find the starting point of FATE chain
                    iCurrFateId = this.mPlugin.mBBDataManager.mFates[iCurrFateId].mChainFatePrev;
                do
                {
                    tFateChainTexts.Add($"{this.mPlugin.mBBDataManager.mFates[iCurrFateId].mName}");
                    iCurrFateId = this.mPlugin.mBBDataManager.mFates[iCurrFateId].mChainFateNext;
                }
                while (iCurrFateId != -1);
            }
            tFateChainText = string.Join(" > ", tFateChainTexts);

            string tFieldNoteDrops = string.Join(
                ", ",
                this.mLinkFieldNotes.Select(o => mPlugin.mBBDataManager.mFieldNotes.TryGetValue(o, out FieldNote? value) && value != null
                                            ? value.mName
                                            : "不明")
                                    .ToList()
                );

            return $"[{this.mName}] • [戰勲：{this.mRewardMettleMin}-{this.mRewardMettleMax}]"
                    + (this.mLocation != null ? $" • [{this.mLocation.ToStringFull()}]" : "")
                    + $" • [系列：{tFateChainText}]"
                    + $" • [戰地筆記：{tFieldNoteDrops}]";
        }
        protected override string GenReprUiTooltip()
        {
            string tFateChainText = "";
            List<string> tFateChainTexts = new();
            if (this.mPlugin.mBBDataManager.mFates[this.mId].mChainFatePrev != -1
                || this.mPlugin.mBBDataManager.mFates[this.mId].mChainFateNext != -1)
            {
                int iCurrFateId = this.mPlugin.mBBDataManager.mFates[this.mId].mChainFatePrev != -1
                                ? this.mPlugin.mBBDataManager.mFates[this.mId].mChainFatePrev
                                : this.mPlugin.mBBDataManager.mFates[this.mId].mChainFateNext;
                while (this.mPlugin.mBBDataManager.mFates[iCurrFateId].mChainFatePrev != -1)        // Find the starting point of FATE chain
                    iCurrFateId = this.mPlugin.mBBDataManager.mFates[iCurrFateId].mChainFatePrev;
                do
                {
                    tFateChainTexts.Add($"{this.mPlugin.mBBDataManager.mFates[iCurrFateId].mName}");
                    iCurrFateId = this.mPlugin.mBBDataManager.mFates[iCurrFateId].mChainFateNext;
                }
                while (iCurrFateId != -1);
            }
            tFateChainText = string.Join(" > ", tFateChainTexts);

            string tFragDrops = string.Join(
                ", ",
                this.mLinkFragments.Select(o => this.mPlugin.mBBDataManager.mFragments.ContainsKey(o)
                                            ? this.mPlugin.mBBDataManager.mFragments[o].mName
                                            : "不明")
                                   .ToList()
                );
            string tFieldNoteDrops = string.Join(
                ", ",
                this.mLinkFieldNotes.Select(o => mPlugin.mBBDataManager.mFieldNotes.TryGetValue(o, out FieldNote? value) && value != null
                                            ? value.mName
                                            : "不明")
                                    .ToList()
                );

            this.mUiTooltip = $"名稱：\t{this.mName}"
                            + $"\n系列：\t{tFateChainText}"
                            + $"\n碎片：\t{tFragDrops}"
                            + $"\n戰勲：\t{Utils.Utils.FormatNum(this.mRewardMettleMin)} - {Utils.Utils.FormatNum(this.mRewardMettleMax)}"
                            + $"\n經驗值：\t{Utils.Utils.FormatNum(this.mRewardExpMin)} - {Utils.Utils.FormatNum(this.mRewardExpMax)}"
                            + $"\n神典石：\t{Utils.Utils.FormatNum(this.mRewardTome)}"
                            + $"\n戰地筆記：\t{tFieldNoteDrops}";
            return this.mUiTooltip;
        }

        protected override void SetUpAuxiliary()
        {
            this.mDetail = $"[神典石：{this.mRewardTome}] \t•\t[戰勲：{this.mRewardMettleMin} - {this.mRewardMettleMax}]";
            this.mDescription = string.Empty;
            this.mIGMarkup = null;
        }
        protected override void SetUpNodeInfo()
        {
            this.mDetailPackage = new()
            {
                { TextureCollection.StandardIcon.Mettle, $"{Utils.Utils.FormatNum(this.mRewardMettleMin, pShorter: true)}-{Utils.Utils.FormatNum(this.mRewardMettleMax, pShorter: true)}" },
                { TextureCollection.StandardIcon.Exp, $"{Utils.Utils.FormatNum(this.mRewardExpMin, pShorter: true)}-{Utils.Utils.FormatNum(this.mRewardExpMax, pShorter: true)}" },
                { TextureCollection.StandardIcon.Poetic, $"{Utils.Utils.FormatNum(this.mRewardTome, pShorter: true)}" }
            };
        }

        public enum FateType
        {
            None = 0,
            Fate = 63914,
            CriticalEngagement = 63909,
            Duel = 63910,
            Raid = 63912
        }
    }
}
