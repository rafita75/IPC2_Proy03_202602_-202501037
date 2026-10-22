using System;

var builder = WebApplication.CreateBuilder(args);

// Habilitar Razor Pages
builder.Services.AddRazorPages();

// Configurar HttpClient para comunicarse con el Servicio 2 (Backend API)
builder.Services.AddHttpClient("SatApi", client =>
{
    client.BaseAddress = new Uri("http://localhost:5297/");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

app.Run();