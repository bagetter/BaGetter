using BaGetter;
using BaGetter.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder();

// This will add the BaGetter services and options to the container.
builder.Services.AddBaGetterWebApplication(bagetter =>
{
    bagetter.AddSqliteDatabase();
    bagetter.AddFileStorage();
});
var app = builder.Build();

if (builder.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();

// Add BaGetter's endpoints.
new BaGetterEndpointBuilder().MapEndpoints(app);

await app.RunMigrationsAsync();
await app.RunAsync();
