using DisCacheClient.Extensions.Caching;
using WebApiDemo.Cache;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDisCacheCache(options =>
{
    options.Configuration = "https://localhost:5001/";
    options.UseFallback = false;
    options.CacheType = CacheType.gRpc;
});

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<CacheService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
