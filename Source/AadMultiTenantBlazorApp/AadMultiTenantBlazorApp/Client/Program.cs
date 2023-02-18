using AadMultiTenantBlazorApp.Client;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace AadMultiTenantBlazorApp.Client
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			var builder = WebAssemblyHostBuilder.CreateDefault(args);
			builder.RootComponents.Add<App>("#app");
			builder.RootComponents.Add<HeadOutlet>("head::after");

			builder.Services.AddHttpClient("AadMultiTenantBlazorApp.ServerAPI", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
					.AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

			// Supply HttpClient instances that include access tokens when making requests to the server project
			builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("AadMultiTenantBlazorApp.ServerAPI"));

			builder.Services.AddMsalAuthentication(options =>
			{
				builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
				string? scopes =
					builder.Configuration!.GetSection("ServerApi")["Scopes"]
					?? throw new InvalidOperationException("ServerApi::Scopes is missing from appsettings.json");
				options.ProviderOptions.DefaultAccessTokenScopes.Add(scopes);
				options.ProviderOptions.LoginMode = "redirect";
			});

			await builder.Build().RunAsync();
		}
	}
}