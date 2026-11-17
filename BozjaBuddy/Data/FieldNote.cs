using BozjaBuddy.Utils;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Dalamud.Game.Text.SeStringHandling;
using System.Linq;

namespace BozjaBuddy.Data
{
    public class FieldNote : GeneralObject
    {
        protected override GeneralObjectSalt mIdSalt { get; set; } = GeneralObjectSalt.FieldNote;
        public override int mId { get; set; } = 0;
        public int mRarity { get; set; } = 0;
        public int mNumber { get; set; } = 0;
        public int mItemLinkId { get; set; } = 0;
        public override string mName { get; set; } = String.Empty;
        protected override Plugin mPlugin { get; set; }

        public FieldNote(Plugin pPlugin, SQLiteDataReader pPackage)
        {
            this.mPlugin = pPlugin;
            mId = (int)(long)pPackage["id"];
            mName = (string)pPackage["name"];
            mRarity = (int)(long)pPackage["rarity"];
            mDescription = pPackage["description"] is System.DBNull ? string.Empty : (string)pPackage["description"];
            mItemLinkId = (int)(long)pPackage["itemLinkId"];

            var tSheet = this.mPlugin.DataManager.Excel.GetSheet<Lumina.Excel.Sheets.MYCWarResultNotebook>();
            if (tSheet != null)
            {
                var tLuminaObj = tSheet.FirstOrDefault(o => o.RowId == (uint)mId);
                mNumber = (int)tLuminaObj.Number;
                var liveName = tLuminaObj.Name.ToString();
                if (!string.IsNullOrWhiteSpace(liveName) && tLuminaObj.RowId == (uint)mId)
                    mName = liveName;
            }

            this.mTabColor = UtilsGUI.Colors.GenObj_BrownFieldNote;

            this.SetUpAuxiliary();
            this.SetUpNodeInfo();
        }
        public override SeString? GetReprItemLink() => SeString.CreateItemLink((uint)this.mItemLinkId, false);
        public override string GetReprClipboardTooltip()
        {
            string tFateText = string.Join(
                ", ",
                this.mLinkFates.Select(o => this.mPlugin.mBBDataManager.mFates.ContainsKey(o)
                                            ? this.mPlugin.mBBDataManager.mFates[o].mName + $" --- ({this.mPlugin.mBBDataManager.mFates[o].mLocation?.ToStringFull()})"
                                            : "不明")
                                   .ToList()
                );
            return $"[{this.mName}] • [稀有度：{this.mRarity}] • [{this.mDescription}] • [來源：{tFateText}]";
        }
        protected override string GenReprUiTooltip()
        {
            string tFateText = string.Join(
                "\n\t\t\t    ",
                this.mLinkFates.Select(o => this.mPlugin.mBBDataManager.mFates.ContainsKey(o)
                                            ? this.mPlugin.mBBDataManager.mFates[o].mName + $" --- ({this.mPlugin.mBBDataManager.mFates[o].mLocation?.ToStringFull()})"
                                            : "不明")
                                   .ToList()
                );

            this.mUiTooltip = $"名稱：\t{this.mName}"
                            + $"\n說明：\t{this.mDescription}"
                            + $"\n來源：\t{tFateText}";
            return this.mUiTooltip;
        }

        protected override void SetUpAuxiliary()
        {
            this.mDetail = $"稀有度：{this.mRarity}";
        }
        protected override void SetUpNodeInfo()
        {
            this.mDetailPackage = new()
            {
                { TextureCollection.StandardIcon.Rarity, this.mRarity.ToString() }
            };
        }
    }
}
