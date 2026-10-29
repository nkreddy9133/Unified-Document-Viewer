using Microsoft.EntityFrameworkCore;
using UnifiedDocumentViewer.Clients;
using UnifiedDocumentViewer.Data;
using UnifiedDocumentViewer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Unified Document Viewer API",
        Version = "v1"
    });
});

builder.Services.AddDbContext<DocumentDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ISearchHistoryRepository, SearchHistoryRepository>();
builder.Services.AddScoped<IDocumentAggregationService, DocumentAggregationService>();

var externalSystems = builder.Configuration.GetSection("ExternalSystems");
var httpClientTimeout = TimeSpan.FromSeconds(
    externalSystems.GetValue("TimeoutSeconds", 5));

builder.Services.AddHttpClient<SalesSystemClient>(client =>
{
    client.BaseAddress = new Uri(externalSystems["SalesBaseUrl"]!);
    client.Timeout = httpClientTimeout;
});

builder.Services.AddHttpClient<ServiceSystemClient>(client =>
{
    client.BaseAddress = new Uri(externalSystems["ServiceBaseUrl"]!);
    client.Timeout = httpClientTimeout;
});

builder.Services.AddTransient<IDocumentSourceClient>(serviceProvider =>
    serviceProvider.GetRequiredService<SalesSystemClient>());
builder.Services.AddTransient<IDocumentSourceClient>(serviceProvider =>
    serviceProvider.GetRequiredService<ServiceSystemClient>());

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
