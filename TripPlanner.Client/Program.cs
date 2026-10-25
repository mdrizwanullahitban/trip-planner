using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TripPlanner.Client;
var builder=WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddScoped(_=>new HttpClient { BaseAddress=new Uri(builder.Configuration["ApiBaseUrl"]??"http://localhost:5000/") });
await builder.Build().RunAsync();