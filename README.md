# 🍱 午餐訂購系統 Lunch Order System

> 這個專案是用 AI 幫我寫的，我只是告訴它我要什麼，它就生出來了，真的很神奇。

---

## 這是什麼？

一個給辦公室/小團體用的**午餐團購系統**。

以前每次揪便當都要在 LINE 群組裡一直問「你要什麼」、「你要什麼」，最後自己在筆記本上抄，很麻煩。

所以就叫 AI 幫我做了這個網站，現在只要丟連結給同事，大家自己填，結帳的時候也知道每個人要付多少錢。

---

## 功能介紹

### 🏪 商家管理
- 可以新增便當店（店名、電話、菜單圖片）
- 菜單圖片上傳後可以在點餐頁面直接看到
- 圖片點一下可以放大（這個我特別要求加的）

### 👥 建立訂購群組
- 選擇今天要訂哪家
- 設定**開放時間**（例如 11:00 ～ 11:30）
- 系統會產生一個專屬連結，丟給大家就可以開始訂

### 🛒 點餐頁面
- 填名字、餐點內容、金額、備註
- 截止時間到了會自動鎖起來，不給送出
- 尚未開始也會顯示提示
- 右上角有「複製連結」可以直接複製分享

### 📋 訂單總表
- 可以看到所有人的訂單
- 顯示每筆金額跟總計
- 旁邊有菜單圖片可以對照
- 有列印按鈕（要去跟店家結帳的時候用）

---

## 畫面長這樣

```
首頁
├── 管理商家
├── 建立訂購群組
└── 訂購群組列表（顯示狀態：未開始 / 進行中 / 已截止）

點餐頁面
├── 左：開放時間資訊 + 填寫表單
├── 右：菜單圖片（可點擊放大）
└── 下：訂單列表 + 合計金額

總表頁面
├── 商家資訊 + 菜單圖片
├── 完整訂單列表
└── 列印按鈕
```

---

## 技術說明（給看得懂的人）

> 我也不太懂，是 AI 跟我解釋的

| 項目 | 使用技術 |
|------|---------|
| 後端框架 | ASP.NET Core 10 Minimal API |
| 資料儲存 | JSON 檔案（不用資料庫） |
| 前端 | 純 HTML + Inline CSS（沒有 React、Vue 之類的） |
| 部署 | Docker + Render |
| 語言 | C# |

整個系統只有一個 `Program.cs` 檔案，AI 說這樣比較簡單。

---

## 本地端跑起來

### 需要先裝
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- 或是裝 [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### 方法一：直接跑
```bash
git clone https://github.com/你的帳號/leonkao-lunch-order-system.git
cd leonkao-lunch-order-system/leonkao-lunch-order-system
dotnet run
```
然後打開瀏覽器去 `http://localhost:5000`

### 方法二：用 Docker
```bash
docker build -t lunch-order .
docker run -p 8080:8080 lunch-order
```
然後打開瀏覽器去 `http://localhost:8080`

---

## 部署到 Render（免費）

> Render 是一個可以免費架網站的平台，不用自己買伺服器

### 步驟

**1. 把程式碼放到 GitHub**
```bash
git remote add origin https://github.com/你的帳號/leonkao-lunch-order-system.git
git push -u origin main
```

**2. 去 Render 建立服務**
1. 登入 [render.com](https://render.com)
2. 點 **New → Web Service**
3. 連結你的 GitHub repository
4. Environment 選 **Docker**
5. Instance Type 選 **Free**
6. 點 **Deploy**

**3. 等它跑完就好了**

Render 會自動讀取專案裡的 `Dockerfile` 幫你建置跟部署，不用做其他設定。

---

## ⚠️ 注意事項（很重要）

### 免費方案的限制

| 問題 | 說明 |
|------|------|
| **網站睡著了** | Render 免費方案閒置 15 分鐘後會自動停止，第一個人打開會等 30～60 秒才醒來 |
| **資料會消失** | 免費方案的檔案系統是暫時性的，**每次重新部署或重啟，所有資料（商家、訂單、圖片）都會清空** |
| **效能比較慢** | 免費方案只有 0.1 顆 CPU，不適合大量使用者同時使用 |

### 所以這個系統適合
- ✅ 辦公室小團體（10～20 人以內）
- ✅ 私人使用、測試用途
- ✅ 每天重新建立群組的使用情境
- ❌ 不適合需要長期保存資料的情境

---

## 使用流程（給第一次用的人）

```
第一步：新增商家
  → 點「管理商家」→ 填店名、電話、上傳菜單照片

第二步：建立今天的訂購群組
  → 點「建立訂購群組」→ 選商家、選日期、設開放時間

第三步：分享連結給同事
  → 進入點餐頁面 → 點右上角「複製連結」→ 貼到 LINE 群

第四步：同事各自填單
  → 打開連結 → 填名字、餐點、金額、備註 → 送出

第五步：截止後去看總表
  → 點「總表」→ 對照菜單、確認訂單 → 列印或截圖去結帳
```

---

## 檔案結構

```
leonkao-lunch-order-system/
├── Dockerfile                        # Docker 設定
├── .dockerignore                     # Docker 不要打包的檔案
├── .gitignore                        # Git 不要追蹤的檔案
├── global.json                       # .NET 版本設定
├── leonkao-lunch-order-system.sln    # 方案檔
└── leonkao-lunch-order-system/
    ├── Program.cs                    # 全部的程式碼都在這裡
    ├── appsettings.json
    └── leonkao-lunch-order-system.csproj
```

執行後會自動產生：
```
├── merchants.json    # 商家資料
├── groups.json       # 群組 + 訂單資料
└── wwwroot/uploads/  # 上傳的菜單圖片
```

---

## 這個專案是怎麼做出來的

1. 我跟 AI 說「我要做一個訂便當的系統」
2. AI 問我要什麼功能
3. 我說不要資料庫、要上傳圖片、要有截止時間
4. AI 就把整個 `Program.cs` 寫出來了
5. 中途我說要改一些地方（像是時間格式、新增金額欄位）
6. AI 就幫我改

整個過程大概花了... 不知道，反正比我自己學再寫快很多。

---

## 已知問題 / 未來想加的功能

- [ ] 資料持久化（現在重啟就消失了）
- [ ] 通知功能（截止前提醒大家填單）
- [ ] 匯出 Excel
- [ ] 商家可以編輯（目前只能刪掉重建）
- [ ] 手機版 UI 優化

---

*這份文件也是用 AI 幫我寫的。*
