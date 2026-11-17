# 博茲雅助手（Bozja Buddy 1.2.0-tw）

精簡版台灣客戶端分支，給 XIVTCLauncher 與 yanmucorp Dalamud API 13 側載測試。這不是原作者的新版本，也沒有把 1.1.6.9 直接改號。

上游作者是 kaleidocli / K-Cli：[kaleidocli/BozjaBuddy](https://github.com/kaleidocli/BozjaBuddy)。本倉庫沒有上游授權檔，因此不加授權、不宣稱原創。

## 保留

- 義軍武器進度
- 戰地筆記（條目上可以顯示來源名稱；沒有獨立的危命／任務瀏覽器，也不顯示打法筆記）
- 失傳技能／遺忘的碎片
- 失傳技能持有欄覆蓋視窗（`UIMap_LostAction.json` 與 GUIAssist）。名稱、職能、碎片、重量篩選仍在。自訂配裝與配裝迷你檢視已隨配裝功能移除。
- 上述功能需要的設定

技能、道具、碎片、戰地筆記與任務名稱在遊戲內從 Lumina 表讀取，會跟著客戶端語言。社群寫的效果說明翻成繁體中文；句子裡的技能原名會在執行時換成表上的名稱。

## 移除

配裝、敵人、任務、危命／特殊遭遇戰瀏覽器、節點圖、打法筆記、鬧鐘、刷怪、女王古殿分頁、測試視窗，以及只被這些功能用到的 NAudio、FFXIVWeather、QuickGraph。角色的軍階／戰勲視窗也不保留；義軍武器進度是手動勾選，不依賴 MYCInfo 數值。

## 安裝

把 `bozjaBuddy` 資料夾放到：

`%AppData%\FFXIVSimpleLauncher\Dalamud\Config\devPlugins\bozjaBuddy\`

內容有 `BozjaBuddy.dll`、`BozjaBuddy.json`、`db`（`LostAction.db`、`UIMap_LostAction.json`、字型）、`images/icon.png`，以及讀資料庫用的 `System.Data.SQLite.dll` 與 `SQLite.Interop.dll`。InternalName 仍是 `bozjaBuddy`，指令是 `/bb`。此 dll 是對 yanmucorp Dalamud 13.0.0.16（tag 26-10-03-01）建置的。

## 遊戲內請先確認

台灣客戶端的 MYCInfo、MYCWarResultNotebook、MYCItemBox 節點編號可能和國際服不同。持有欄覆蓋、戰地筆記掃描、戰勲情報上的搜尋列都只讀介面，沒有掛鉤或特徵碼掃描；若對不齊，要在遊戲裡看，不能照國服版面猜。
