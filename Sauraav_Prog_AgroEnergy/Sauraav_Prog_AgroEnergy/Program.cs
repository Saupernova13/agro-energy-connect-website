var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseSession();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "itemDetail",
    pattern: "Item/ItemDetail/{itemId}",
    defaults: new { controller = "Item", action = "ItemDetail" });

app.MapControllerRoute(
    name: "admin",
    pattern: "{controller=Admin}/{action=AdminDashboard}/{id?}");

app.MapControllerRoute(
    name: "approveFarmer",
    pattern: "{controller=Admin}/{action=ApproveFarmer}");

app.Run();
