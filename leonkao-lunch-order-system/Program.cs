using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.Extensions.FileProviders;

// ─── Setup ───────────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var wwwroot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
var uploadsDir = Path.Combine(wwwroot, "uploads");
Directory.CreateDirectory(uploadsDir);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(wwwroot),
    RequestPath = ""
});

// ─── JSON Helpers ─────────────────────────────────────────────────────────────

const string MerchantsFile = "merchants.json";
const string GroupsFile = "groups.json";

var jsonOpts = new JsonSerializerOptions
{
    WriteIndented = true,
    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    PropertyNameCaseInsensitive = true
};

List<Merchant> LoadMerchants() =>
    File.Exists(MerchantsFile)
        ? JsonSerializer.Deserialize<List<Merchant>>(File.ReadAllText(MerchantsFile), jsonOpts) ?? []
        : [];

void SaveMerchants(List<Merchant> list) =>
    File.WriteAllText(MerchantsFile, JsonSerializer.Serialize(list, jsonOpts), Encoding.UTF8);

List<Group> LoadGroups() =>
    File.Exists(GroupsFile)
        ? JsonSerializer.Deserialize<List<Group>>(File.ReadAllText(GroupsFile), jsonOpts) ?? []
        : [];

void SaveGroups(List<Group> list) =>
    File.WriteAllText(GroupsFile, JsonSerializer.Serialize(list, jsonOpts), Encoding.UTF8);

string H(string? text) => WebUtility.HtmlEncode(text ?? "");

// 固定使用 UTC+8（台灣時間），避免 Render 伺服器在 UTC 環境下時區錯誤
DateTime NowTw() => DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8)).DateTime;

// ─── Routes ───────────────────────────────────────────────────────────────────

// 首頁 - 列出所有群組
app.MapGet("/", async context =>
{
    var groups = LoadGroups();
    var merchants = LoadMerchants();

    var rows = new StringBuilder();
    foreach (var g in groups.OrderByDescending(x => x.StartTime))
    {
        var m = merchants.FirstOrDefault(x => x.Id == g.MerchantId);
        var name = H(m?.Name ?? "未知商家");
        var timeRange = $"{g.StartTime:yyyy-MM-dd HH:mm} ~ {g.EndTime:HH:mm}";
        var count = g.Orders.Count;
        var now = NowTw();
        var status = now < g.StartTime
            ? "<span style='color:#888;font-weight:bold;'>未開始</span>"
            : now > g.EndTime
                ? "<span style='color:red;font-weight:bold;'>已截止</span>"
                : "<span style='color:#4CAF50;font-weight:bold;'>進行中</span>";
        rows.Append($"<tr><td>{name}</td><td>{timeRange}</td><td>{count}</td><td>{status}</td>" +
                    $"<td><a href='/order/{g.Id}'>點餐</a> &nbsp; <a href='/order/{g.Id}/summary'>總表</a></td></tr>");
    }

    var html = $$"""
        <!DOCTYPE html>
        <html lang="zh-TW">
        <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width,initial-scale=1">
        <title>午餐訂購系統</title>
        <style>
          body { font-family: Arial, "Microsoft JhengHei", sans-serif; max-width: 900px; margin: auto; padding: 20px; background:#f7f7f7; }
          h1 { text-align:center; color:#333; }
          nav { text-align:center; margin:20px 0; }
          nav a { margin:0 8px; padding:9px 18px; background:#4CAF50; color:#fff; text-decoration:none; border-radius:4px; }
          nav a:hover { background:#388E3C; }
          table { width:100%; border-collapse:collapse; background:#fff; border-radius:8px; overflow:hidden; box-shadow:0 1px 4px #ccc; }
          th,td { border:1px solid #ddd; padding:11px 14px; text-align:left; }
          th { background:#f0f0f0; }
          tr:hover { background:#f9f9f9; }
          a { color:#4CAF50; text-decoration:none; }
          a:hover { text-decoration:underline; }
          .btn-del { background:#e53935; color:#fff; border:none; padding:4px 10px; border-radius:4px; cursor:pointer; font-size:13px; }
          .btn-del:hover { background:#b71c1c; }
        </style>
        </head>
        <body>
        <h1>🍱 午餐訂購系統</h1>
        <nav>
          <a href="/merchants">管理商家</a>
          <a href="/groups/create">建立訂購群組</a>
        </nav>
        <h2>訂購群組列表</h2>
        <table>
          <tr><th>商家名稱</th><th>開放時間</th><th>訂單數量</th><th>狀態</th><th>操作</th></tr>
          {{rows}}
        </table>
        </body>
        </html>
        """;

    context.Response.ContentType = "text/html; charset=UTF-8";
    await context.Response.WriteAsync(html, Encoding.UTF8);
});

// 商家列表頁
app.MapGet("/merchants", async context =>
{
    var merchants = LoadMerchants();

    var rows = new StringBuilder();
    foreach (var m in merchants)
    {
        var img = string.IsNullOrEmpty(m.MenuImageUrl)
            ? "（無圖片）"
            : $"<img src='{m.MenuImageUrl}' alt='菜單' style='max-height:70px;border-radius:4px;'>";
        rows.Append($"<tr><td>{H(m.Name)}</td><td>{H(m.Phone)}</td><td>{img}</td><td>" +
                    $"<form method='post' action='/merchants/{m.Id}/delete' style='display:inline'>" +
                    $"<button class='btn-del' onclick=\"return confirm('確定刪除此商家？')\">刪除</button></form>" +
                    $"</td></tr>");
    }

    var html = $$"""
        <!DOCTYPE html>
        <html lang="zh-TW">
        <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width,initial-scale=1">
        <title>商家管理</title>
        <style>
          body { font-family: Arial, "Microsoft JhengHei", sans-serif; max-width: 700px; margin: auto; padding: 20px; background:#f7f7f7; }
          h1,h2 { color:#333; }
          .card { background:#fff; padding:20px; border-radius:8px; box-shadow:0 1px 4px #ccc; margin-bottom:24px; }
          label { display:block; margin-top:12px; font-weight:bold; }
          input[type=text],input[type=file] { width:100%; padding:8px; margin-top:4px; box-sizing:border-box; border:1px solid #ccc; border-radius:4px; }
          button { margin-top:16px; padding:10px 22px; background:#4CAF50; color:#fff; border:none; border-radius:4px; cursor:pointer; font-size:15px; }
          button:hover { background:#388E3C; }
          table { width:100%; border-collapse:collapse; background:#fff; border-radius:8px; overflow:hidden; box-shadow:0 1px 4px #ccc; }
          th,td { border:1px solid #ddd; padding:11px 14px; text-align:left; }
          th { background:#f0f0f0; }
          a { color:#4CAF50; text-decoration:none; }
          .btn-del { background:#e53935; color:#fff; border:none; padding:4px 10px; border-radius:4px; cursor:pointer; font-size:13px; }
          .btn-del:hover { background:#b71c1c; }
        </style>
        </head>
        <body>
        <p><a href="/">← 回首頁</a></p>
        <h1>商家管理</h1>
        <div class="card">
          <h2>新增商家</h2>
          <form method="post" action="/merchants" enctype="multipart/form-data">
            <label>商家名稱</label>
            <input type="text" name="name" required placeholder="例：老鄧便當" />
            <label>電話（選填）</label>
            <input type="text" name="phone" placeholder="例：02-12345678" />
            <label>菜單圖片 (JPG / PNG)</label>
            <input type="file" name="image" accept="image/jpeg,image/png" required />
            <button type="submit">新增商家</button>
          </form>
        </div>
        <h2>商家列表</h2>
        <table>
          <tr><th>商家名稱</th><th>電話</th><th>菜單圖片</th><th>操作</th></tr>
          {{rows}}
        </table>
        </body>
        </html>
        """;

    context.Response.ContentType = "text/html; charset=UTF-8";
    await context.Response.WriteAsync(html, Encoding.UTF8);
});

// 新增商家（POST）
app.MapPost("/merchants", async (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();
    var name = form["name"].ToString().Trim();
    var phone = form["phone"].ToString().Trim();
    var imageFile = form.Files["image"];

    if (string.IsNullOrWhiteSpace(name) || imageFile == null || imageFile.Length == 0)
        return Results.Redirect("/merchants");

    var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
    if (ext is not (".jpg" or ".jpeg" or ".png"))
        return Results.Redirect("/merchants");

    var fileName = $"{Guid.NewGuid()}{ext}";
    var filePath = Path.Combine(uploadsDir, fileName);
    await using var fs = File.Create(filePath);
    await imageFile.CopyToAsync(fs);

    var merchants = LoadMerchants();
    merchants.Add(new Merchant { Id = Guid.NewGuid().ToString(), Name = name, Phone = phone, MenuImageUrl = $"/uploads/{fileName}" });
    SaveMerchants(merchants);

    return Results.Redirect("/merchants");
});

// 建立群組頁
app.MapGet("/groups/create", async context =>
{
    var merchants = LoadMerchants();

    var options = new StringBuilder("<option value=''>請選擇商家</option>");
    foreach (var m in merchants)
        options.Append($"<option value='{m.Id}'>{H(m.Name)}</option>");

    var html = $$"""
        <!DOCTYPE html>
        <html lang="zh-TW">
        <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width,initial-scale=1">
        <title>建立訂購群組</title>
        <style>
          body { font-family: Arial, "Microsoft JhengHei", sans-serif; max-width: 500px; margin: auto; padding: 20px; background:#f7f7f7; }
          h1 { color:#333; }
          .card { background:#fff; padding:24px; border-radius:8px; box-shadow:0 1px 4px #ccc; }
          label { display:block; margin-top:14px; font-weight:bold; }
          select,input[type=date],input[type=text] { width:100%; padding:9px; margin-top:5px; box-sizing:border-box; border:1px solid #ccc; border-radius:4px; font-size:15px; }
          .time-row { display:flex; gap:12px; margin-top:5px; }
          .time-row input { flex:1; }
          button { margin-top:20px; padding:11px 24px; background:#4CAF50; color:#fff; border:none; border-radius:4px; cursor:pointer; font-size:15px; }
          button:hover { background:#388E3C; }
          a { color:#4CAF50; text-decoration:none; }
        </style>
        </head>
        <body>
        <p><a href="/">← 回首頁</a></p>
        <h1>建立訂購群組</h1>
        <div class="card">
          <form method="post" action="/groups">
            <label>選擇商家</label>
            <select name="merchantId" required>{{options}}</select>
            <label>日期</label>
            <input type="date" name="orderDate" required />
            <label>開放訂購時間</label>
            <div class="time-row">
              <input type="text" name="startTime" required placeholder="例：11:00" pattern="[0-2][0-9]:[0-5][0-9]" maxlength="5" />
              <span style="display:flex;align-items:center;color:#666;">～</span>
              <input type="text" name="endTime" required placeholder="例：13:30" pattern="[0-2][0-9]:[0-5][0-9]" maxlength="5" />
            </div>
            <button type="submit">建立群組</button>
          </form>
        </div>
        </body>
        </html>
        """;

    context.Response.ContentType = "text/html; charset=UTF-8";
    await context.Response.WriteAsync(html, Encoding.UTF8);
});

// 建立群組（POST）
app.MapPost("/groups", async (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();
    var merchantId = form["merchantId"].ToString();
    var orderDateStr = form["orderDate"].ToString();
    var startTimeStr = form["startTime"].ToString();
    var endTimeStr = form["endTime"].ToString();

    if (string.IsNullOrWhiteSpace(merchantId)
        || !DateOnly.TryParse(orderDateStr, out var orderDate)
        || !TimeOnly.TryParse(startTimeStr, out var startTime)
        || !TimeOnly.TryParse(endTimeStr, out var endTime))
        return Results.Redirect("/groups/create");

    var startDt = orderDate.ToDateTime(startTime);
    var endDt = orderDate.ToDateTime(endTime);

    var groups = LoadGroups();
    var group = new Group { Id = Guid.NewGuid().ToString(), MerchantId = merchantId, StartTime = startDt, EndTime = endDt };
    groups.Add(group);
    SaveGroups(groups);

    return Results.Redirect($"/order/{group.Id}");
});

// 訂購頁面（GET）
app.MapGet("/order/{groupId}", async (string groupId, HttpContext context) =>
{
    var groups = LoadGroups();
    var group = groups.FirstOrDefault(g => g.Id == groupId);

    if (group is null)
    {
        context.Response.ContentType = "text/html; charset=UTF-8";
        await context.Response.WriteAsync("<h2 style='font-family:Arial;text-align:center;margin-top:60px;'>找不到此訂購群組</h2>", Encoding.UTF8);
        return;
    }

    var merchants = LoadMerchants();
    var merchant = merchants.FirstOrDefault(m => m.Id == group.MerchantId);
    var merchantName = merchant?.Name ?? "未知商家";
    var timeRange = $"{group.StartTime:yyyy年MM月dd日 HH:mm} ～ {group.EndTime:HH:mm}";
    var now = DateTime.Now;
    var isNotStarted = now < group.StartTime;
    var isClosed = now > group.EndTime;
    var isOpen = !isNotStarted && !isClosed;

    var menuImg = !string.IsNullOrEmpty(merchant?.MenuImageUrl)
        ? $"<img src='{merchant.MenuImageUrl}' alt='菜單' style='width:100%;border-radius:8px;display:block;cursor:zoom-in;' onclick='openLightbox(this.src)' title='點擊放大'>"
        : "<div style='color:#aaa;text-align:center;padding:20px;'>（無菜單圖片）</div>";

    var formSection = isNotStarted
        ? $"<div style='background:#fff8e1;padding:18px;border-radius:8px;color:#f57f17;font-size:18px;text-align:center;border:1px solid #ffe082;'>🕐 尚未開始，開放時間：{group.StartTime:HH:mm} ～ {group.EndTime:HH:mm}</div>"
        : isClosed
        ? "<div style='background:#ffeeee;padding:18px;border-radius:8px;color:#c62828;font-size:18px;text-align:center;border:1px solid #ef9a9a;'>⛔ 已截止訂購</div>"
        : $"""
          <form method="post" action="/order/{groupId}">
            <div style="margin-bottom:14px;">
              <label style="display:block;font-weight:bold;margin-bottom:4px;">名字</label>
              <input type="text" name="name" required placeholder="請輸入您的名字"
                     style="width:100%;padding:9px;box-sizing:border-box;border:1px solid #ccc;border-radius:4px;font-size:15px;" />
            </div>
            <div style="margin-bottom:14px;">
              <label style="display:block;font-weight:bold;margin-bottom:4px;">餐點內容</label>
              <input type="text" name="food" required placeholder="例：排骨便當"
                     style="width:100%;padding:9px;box-sizing:border-box;border:1px solid #ccc;border-radius:4px;font-size:15px;" />
            </div>
            <div style="margin-bottom:14px;">
              <label style="display:block;font-weight:bold;margin-bottom:4px;">金額（元）</label>
              <input type="number" name="amount" required min="0" placeholder="例：100"
                     style="width:100%;padding:9px;box-sizing:border-box;border:1px solid #ccc;border-radius:4px;font-size:15px;" />
            </div>
            <div style="margin-bottom:14px;">
              <label style="display:block;font-weight:bold;margin-bottom:4px;">備註</label>
              <input type="text" name="note" placeholder="例：少鹽、不要香菜（可留空）"
                     style="width:100%;padding:9px;box-sizing:border-box;border:1px solid #ccc;border-radius:4px;font-size:15px;" />
            </div>
            <button type="submit"
                    style="padding:11px 24px;background:#4CAF50;color:#fff;border:none;border-radius:4px;cursor:pointer;font-size:15px;">
              送出訂單
            </button>
          </form>
          """;

    var orderRows = new StringBuilder();
    var totalAmount = 0m;
    for (var i = 0; i < group.Orders.Count; i++)
    {
        var o = group.Orders[i];
        totalAmount += o.Amount;
        orderRows.Append($"<tr><td>{i + 1}</td><td>{H(o.Name)}</td><td>{H(o.Food)}</td><td>{o.Amount:N0}</td><td>{H(o.Note)}</td><td>" +
                          $"<form method='post' action='/order/{groupId}/orders/{o.Id}/delete' style='display:inline'>" +
                          $"<button class='btn-del' onclick=\"return confirm('確定刪除此筆訂單？')\">刪除</button></form>" +
                          $"</td></tr>");
    }

    var statusBadge = isNotStarted
        ? "<span style='color:#888;font-weight:bold;'>未開始</span>"
        : isClosed
            ? "<span style='color:red;font-weight:bold;'>已截止</span>"
            : "<span style='color:#4CAF50;font-weight:bold;'>進行中</span>";

    var html = $$"""
        <!DOCTYPE html>
        <html lang="zh-TW">
        <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width,initial-scale=1">
        <title>{{H(merchantName)}} - 訂購</title>
        <style>
          body { font-family: Arial, "Microsoft JhengHei", sans-serif; max-width: 960px; margin: auto; padding: 20px; background:#f7f7f7; }
          h1,h2 { color:#333; }
          .top-layout { display:flex; gap:20px; align-items:flex-start; margin-bottom:24px; }
          .top-left { flex:1; min-width:0; }
          .top-right { width:300px; flex-shrink:0; background:#fff; border-radius:8px; box-shadow:0 1px 4px #ccc; padding:12px; }
          .info { background:#fff; padding:16px 20px; border-radius:8px; box-shadow:0 1px 4px #ccc; margin-bottom:16px; }
          .form-card { background:#fff; padding:20px; border-radius:8px; box-shadow:0 1px 4px #ccc; }
          table { width:100%; border-collapse:collapse; background:#fff; border-radius:8px; overflow:hidden; box-shadow:0 1px 4px #ccc; }
          th,td { border:1px solid #ddd; padding:10px 13px; text-align:left; }
          th { background:#f0f0f0; }
          tr:hover { background:#f9f9f9; }
          .total-row td { font-weight:bold; background:#f0f7f0; }
          a { color:#4CAF50; text-decoration:none; }
          .btn-del { background:#e53935; color:#fff; border:none; padding:4px 10px; border-radius:4px; cursor:pointer; font-size:13px; }
          .btn-del:hover { background:#b71c1c; }
          .page-header { display:flex; justify-content:space-between; align-items:center; margin-bottom:12px; gap:12px; }
          .header-actions { display:flex; gap:8px; align-items:center; flex-shrink:0; }
          .btn-edit { background:#1976D2; color:#fff; text-decoration:none; padding:6px 14px; border-radius:4px; font-size:14px; }
          .btn-edit:hover { background:#1565C0; }
          .share-wrap { display:flex; gap:6px; align-items:center; flex:1; min-width:0; }
          .share-wrap input { flex:1; min-width:0; padding:6px 9px; border:1px solid #ccc; border-radius:4px; font-size:13px; background:#f9f9f9; }
          .btn-copy { padding:6px 12px; background:#1976D2; color:#fff; border:none; border-radius:4px; cursor:pointer; font-size:13px; white-space:nowrap; flex-shrink:0; }
          .btn-copy:hover { background:#1565C0; }
          .lightbox { display:none; position:fixed; inset:0; background:rgba(0,0,0,0.88); z-index:9999; justify-content:center; align-items:center; cursor:zoom-out; }
          .lightbox.active { display:flex; }
          .lightbox img { max-width:92vw; max-height:92vh; border-radius:8px; box-shadow:0 4px 32px #000; }
        </style>
        </head>
        <body>
        <div class="page-header">
          <a href="/" style="flex-shrink:0">← 回首頁</a>
          <div class="share-wrap">
            <input type="text" id="shareUrl" value="{{context.Request.Scheme}}://{{context.Request.Host}}/order/{{groupId}}" readonly />
            <button class="btn-copy" onclick="copyLink()">複製連結</button>
          </div>
          <div class="header-actions">
            <a href="/order/{{groupId}}/summary" class="btn-edit" style="background:#757575;">總表</a>
            <a href="/groups/{{groupId}}/edit" class="btn-edit">編輯</a>
            <form method="post" action="/groups/{{groupId}}/delete" style="display:inline">
              <button class="btn-del" onclick="return confirm('確定刪除此群組及所有訂單？')">刪除</button>
            </form>
          </div>
        </div>
        <div id="lightbox" class="lightbox" onclick="closeLightbox()">
          <img id="lightboxImg" src="" alt="放大菜單" />
        </div>
        <script>
        function copyLink() {
          var input = document.getElementById('shareUrl');
          navigator.clipboard.writeText(input.value).then(function() {
            var btn = document.querySelector('.btn-copy');
            btn.textContent = '已複製！';
            setTimeout(function() { btn.textContent = '複製連結'; }, 2000);
          });
        }
        function openLightbox(src) {
          document.getElementById('lightboxImg').src = src;
          document.getElementById('lightbox').classList.add('active');
        }
        function closeLightbox() {
          document.getElementById('lightbox').classList.remove('active');
        }
        document.addEventListener('keydown', function(e) {
          if (e.key === 'Escape') closeLightbox();
        });
        </script>
        <h1>🍱 {{H(merchantName)}}</h1>
        <div class="top-layout">
          <div class="top-left">
            <div class="info">
              <p style="margin:0 0 6px"><strong>開放時間：</strong>{{timeRange}}</p>
              <p style="margin:0"><strong>狀態：</strong>{{statusBadge}}</p>
            </div>
            <div class="form-card">
              <h2 style="margin-top:0">填寫訂單</h2>
              {{formSection}}
            </div>
          </div>
          <div class="top-right">
            <strong style="display:block;margin-bottom:8px;">📋 菜單</strong>
            {{menuImg}}
          </div>
        </div>
        <h2>目前訂單（共 {{group.Orders.Count}} 筆）</h2>
        <table>
          <tr><th>#</th><th>名字</th><th>餐點</th><th>金額</th><th>備註</th><th>操作</th></tr>
          {{orderRows}}
          <tr class="total-row">
            <td colspan="2">合計</td>
            <td>{{group.Orders.Count}} 筆</td>
            <td>{{totalAmount:N0}} 元</td>
            <td colspan="2"></td>
          </tr>
        </table>
        </body>
        </html>
        """;

    context.Response.ContentType = "text/html; charset=UTF-8";
    await context.Response.WriteAsync(html, Encoding.UTF8);
});

// 提交訂單（POST）
app.MapPost("/order/{groupId}", async (string groupId, HttpRequest request) =>
{
    var form = await request.ReadFormAsync();
    var name = form["name"].ToString().Trim();
    var food = form["food"].ToString().Trim();
    var note = form["note"].ToString().Trim();
    _ = decimal.TryParse(form["amount"].ToString(), out var amount);

    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(food) || amount <= 0)
        return Results.Redirect($"/order/{groupId}");

    var groups = LoadGroups();
    var group = groups.FirstOrDefault(g => g.Id == groupId);

    var now2 = NowTw();
    if (group is null || now2 < group.StartTime || now2 > group.EndTime)
        return Results.Redirect($"/order/{groupId}");

    group.Orders.Add(new Order { Id = Guid.NewGuid().ToString(), Name = name, Food = food, Note = note, Amount = amount });
    SaveGroups(groups);

    return Results.Redirect($"/order/{groupId}");
});

// 刪除群組
app.MapPost("/groups/{groupId}/delete", (string groupId) =>
{
    var groups = LoadGroups();
    groups.RemoveAll(g => g.Id == groupId);
    SaveGroups(groups);
    return Results.Redirect("/");
});

// 編輯群組（GET）
app.MapGet("/groups/{groupId}/edit", async (string groupId, HttpContext context) =>
{
    var groups = LoadGroups();
    var group = groups.FirstOrDefault(g => g.Id == groupId);
    if (group is null) { context.Response.Redirect("/"); return; }

    var merchants = LoadMerchants();
    var options = new StringBuilder();
    foreach (var m in merchants)
    {
        var sel = m.Id == group.MerchantId ? "selected" : "";
        options.Append($"<option value='{m.Id}' {sel}>{H(m.Name)}</option>");
    }

    var html = $$"""
        <!DOCTYPE html>
        <html lang="zh-TW">
        <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width,initial-scale=1">
        <title>編輯群組</title>
        <style>
          body { font-family: Arial, "Microsoft JhengHei", sans-serif; max-width: 500px; margin: auto; padding: 20px; background:#f7f7f7; }
          h1 { color:#333; }
          .card { background:#fff; padding:24px; border-radius:8px; box-shadow:0 1px 4px #ccc; margin-bottom:16px; }
          label { display:block; margin-top:14px; font-weight:bold; }
          select,input[type=date],input[type=text] { width:100%; padding:9px; margin-top:5px; box-sizing:border-box; border:1px solid #ccc; border-radius:4px; font-size:15px; }
          .time-row { display:flex; gap:12px; margin-top:5px; }
          .time-row input { flex:1; }
          button { margin-top:20px; padding:11px 24px; background:#4CAF50; color:#fff; border:none; border-radius:4px; cursor:pointer; font-size:15px; }
          button:hover { background:#388E3C; }
          a { color:#4CAF50; text-decoration:none; }
        </style>
        </head>
        <body>
        <p><a href="/order/{{groupId}}">← 回點餐頁</a></p>
        <h1>編輯訂購群組</h1>
        <div class="card">
          <form method="post" action="/groups/{{groupId}}/edit">
            <label>選擇商家</label>
            <select name="merchantId" required>{{options}}</select>
            <label>日期</label>
            <input type="date" name="orderDate" required value="{{group.StartTime:yyyy-MM-dd}}" />
            <label>開放訂購時間</label>
            <div class="time-row">
              <input type="text" name="startTime" required placeholder="例：11:00"
                     pattern="[0-2][0-9]:[0-5][0-9]" maxlength="5" value="{{group.StartTime:HH:mm}}" />
              <span style="display:flex;align-items:center;color:#666;">～</span>
              <input type="text" name="endTime" required placeholder="例：13:30"
                     pattern="[0-2][0-9]:[0-5][0-9]" maxlength="5" value="{{group.EndTime:HH:mm}}" />
            </div>
            <button type="submit">儲存變更</button>
          </form>
        </div>
        </body>
        </html>
        """;

    context.Response.ContentType = "text/html; charset=UTF-8";
    await context.Response.WriteAsync(html, Encoding.UTF8);
});

// 編輯群組（POST）
app.MapPost("/groups/{groupId}/edit", async (string groupId, HttpRequest request) =>
{
    var form = await request.ReadFormAsync();
    var merchantId = form["merchantId"].ToString();
    var orderDateStr = form["orderDate"].ToString();
    var startTimeStr = form["startTime"].ToString();
    var endTimeStr = form["endTime"].ToString();

    if (string.IsNullOrWhiteSpace(merchantId)
        || !DateOnly.TryParse(orderDateStr, out var orderDate)
        || !TimeOnly.TryParse(startTimeStr, out var startTime)
        || !TimeOnly.TryParse(endTimeStr, out var endTime))
        return Results.Redirect($"/groups/{groupId}/edit");

    var groups = LoadGroups();
    var group = groups.FirstOrDefault(g => g.Id == groupId);
    if (group is null) return Results.Redirect("/");

    group.MerchantId = merchantId;
    group.StartTime = DateTime.SpecifyKind(orderDate.ToDateTime(startTime), DateTimeKind.Local);
    group.EndTime = DateTime.SpecifyKind(orderDate.ToDateTime(endTime), DateTimeKind.Local);
    SaveGroups(groups);

    return Results.Redirect("/");
});

// 刪除單筆訂單
app.MapPost("/order/{groupId}/orders/{orderId}/delete", (string groupId, string orderId) =>
{
    var groups = LoadGroups();
    var group = groups.FirstOrDefault(g => g.Id == groupId);
    if (group is not null)
    {
        group.Orders.RemoveAll(o => o.Id == orderId);
        SaveGroups(groups);
    }
    return Results.Redirect($"/order/{groupId}");
});

// 刪除商家
app.MapPost("/merchants/{merchantId}/delete", (string merchantId) =>
{
    var merchants = LoadMerchants();
    merchants.RemoveAll(m => m.Id == merchantId);
    SaveMerchants(merchants);
    return Results.Redirect("/merchants");
});

// 總表頁
app.MapGet("/order/{groupId}/summary", async (string groupId, HttpContext context) =>
{
    var groups = LoadGroups();
    var group = groups.FirstOrDefault(g => g.Id == groupId);
    if (group is null) { context.Response.Redirect("/"); return; }

    var merchants = LoadMerchants();
    var merchant = merchants.FirstOrDefault(m => m.Id == group.MerchantId);
    var merchantName = merchant?.Name ?? "未知商家";
    var phone = !string.IsNullOrEmpty(merchant?.Phone) ? $" ｜ 電話：{H(merchant.Phone)}" : "";
    var timeRange = $"{group.StartTime:yyyy年MM月dd日 HH:mm} ～ {group.EndTime:HH:mm}";
    var menuImg = !string.IsNullOrEmpty(merchant?.MenuImageUrl)
        ? $"<img src='{merchant.MenuImageUrl}' alt='菜單' style='max-width:100%;max-height:480px;object-fit:contain;border-radius:8px;display:block;margin:0 auto;cursor:zoom-in;' onclick='openLightbox(this.src)' title='點擊放大'>"
        : "<div style='color:#aaa;padding:20px;text-align:center;'>（無菜單圖片）</div>";

    var orderRows = new StringBuilder();
    var total = 0m;
    for (var i = 0; i < group.Orders.Count; i++)
    {
        var o = group.Orders[i];
        total += o.Amount;
        orderRows.Append($"<tr><td>{i + 1}</td><td>{H(o.Name)}</td><td>{H(o.Food)}</td><td>{o.Amount:N0}</td><td>{H(o.Note)}</td></tr>");
    }

    var html = $$"""
        <!DOCTYPE html>
        <html lang="zh-TW">
        <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width,initial-scale=1">
        <title>總表 - {{H(merchantName)}}</title>
        <style>
          body { font-family: Arial, "Microsoft JhengHei", sans-serif; max-width: 860px; margin: auto; padding: 20px; background:#f7f7f7; }
          h1,h2 { color:#333; }
          .header { display:flex; justify-content:space-between; align-items:center; margin-bottom:12px; }
          .info { background:#fff; padding:16px 20px; border-radius:8px; box-shadow:0 1px 4px #ccc; margin-bottom:20px; }
          .layout { display:flex; gap:20px; align-items:flex-start; margin-bottom:24px; }
          .menu-col { width:340px; flex-shrink:0; background:#fff; border-radius:8px; box-shadow:0 1px 4px #ccc; padding:14px; }
          .order-col { flex:1; min-width:0; }
          table { width:100%; border-collapse:collapse; background:#fff; border-radius:8px; overflow:hidden; box-shadow:0 1px 4px #ccc; }
          th,td { border:1px solid #ddd; padding:10px 13px; text-align:left; }
          th { background:#f0f0f0; }
          tr:hover { background:#f9f9f9; }
          .total-row td { font-weight:bold; background:#f0f7f0; }
          a { color:#4CAF50; text-decoration:none; }
          .lightbox { display:none; position:fixed; inset:0; background:rgba(0,0,0,0.88); z-index:9999; justify-content:center; align-items:center; cursor:zoom-out; }
          .lightbox.active { display:flex; }
          .lightbox img { max-width:92vw; max-height:92vh; border-radius:8px; }
          @media print {
            .header a, .lightbox { display:none !important; }
            body { background:#fff; }
          }
        </style>
        </head>
        <body>
        <div class="header">
          <a href="/order/{{groupId}}">← 回點餐頁</a>
          <button onclick="window.print()" style="padding:7px 16px;background:#555;color:#fff;border:none;border-radius:4px;cursor:pointer;">🖨 列印</button>
        </div>
        <h1>📋 訂單總表</h1>
        <div class="info">
          <p style="margin:0 0 5px"><strong>商家：</strong>{{H(merchantName)}}{{phone}}</p>
          <p style="margin:0"><strong>開放時間：</strong>{{timeRange}}</p>
        </div>
        <div class="layout">
          <div class="menu-col">
            <strong style="display:block;margin-bottom:8px;">📋 菜單</strong>
            {{menuImg}}
          </div>
          <div class="order-col">
            <h2 style="margin-top:0">訂單列表（共 {{group.Orders.Count}} 筆）</h2>
            <table>
              <tr><th>#</th><th>名字</th><th>餐點</th><th>金額</th><th>備註</th></tr>
              {{orderRows}}
              <tr class="total-row">
                <td colspan="2">合計</td>
                <td>{{group.Orders.Count}} 筆</td>
                <td>{{total:N0}} 元</td>
                <td></td>
              </tr>
            </table>
          </div>
        </div>
        <div id="lightbox" class="lightbox" onclick="this.classList.remove('active')">
          <img id="lbImg" src="" alt="" />
        </div>
        <script>
        function openLightbox(src) {
          document.getElementById('lbImg').src = src;
          document.getElementById('lightbox').classList.add('active');
        }
        document.addEventListener('keydown', function(e) {
          if (e.key === 'Escape') document.getElementById('lightbox').classList.remove('active');
        });
        </script>
        </body>
        </html>
        """;

    context.Response.ContentType = "text/html; charset=UTF-8";
    await context.Response.WriteAsync(html, Encoding.UTF8);
});

app.Run();

// ─── Models ───────────────────────────────────────────────────────────────────

class Merchant
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string MenuImageUrl { get; set; } = "";
}

class Group
{
    public string Id { get; set; } = "";
    public string MerchantId { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public List<Order> Orders { get; set; } = [];
}

class Order
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Food { get; set; } = "";
    public decimal Amount { get; set; }
    public string Note { get; set; } = "";
}
