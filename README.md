# Walkthrough - Azure Active Directory multi-tenant
A step by step guide to creating an ASP.NET hosted Blazor WASM
client that uses Azure Active Directory to authenticate.

## Why multi-tenant?
Allows absolutely anyone to use your app as long as they have
authenticated using Azure Active Directory.  The user doesn't
need to be in *your* directory, just *some* directory.

Note that this doesn't restrict who can access your application,
nor what they can do once they've signed in. It just ensures
you know the person using your application has gone through
the necessary sign-in process on Azure Active Directory.

## Creating your Active Directory
### Enable Active Directory as a resource provider
1. Sign into https://portal.azure.com
1. Select your subscription
1. In the menu on the left hand side (Left menu) select `Resource providers`
1. Click `Microsoft.Azure.ActiveDirectory`
1. If its status is not `Registered`, click `Register` at the top of the page

### Set up Azure Active Directory multi-tenant
1. From the Azure home page click `Create a resource`
1. Search for `Azure Active Directory`
1. Find the `Azure Active Directory` icon (**NOT B2C**)
1. Click the `Create` link at the bottom of the item
1. Click `Azure Active Directory` in the popup menu
1. For `Tenant type` select `Azure Active Directory` (**NOT B2C**)
1. Click `Next`
1. Enter an `Organization name` and `Initial domain name` (e.g. `MyOrg` and `MyOrgInitialDomain`)
1. Select your `Location`
1. Click `Review + create`
1. Click `Create`

### Switch to your new directory
1. Click your user account icon at the top-right of the page
1. Click `Switch directory` in the popup menu
1. Refresh your browser if your new tenant is not listed
1. Click `Switch` next to your new directory in the list

## Registering your web application with Azure AAD
### Register your application
1. Click the three horizontal lines (burger menu) at the top-left of the page
1. Select `Azure Active Directory` from the popup menu
1. In the left-menu, click `App registrations`
1. Click `New registration` at the top of the page
1. Give it a name (e.g. "MyApp")
1. For `Supported account types` select `Accounts in any organizational directory (Any Azure AD directory - Multitenant)`
1. Under `Redirect URI` select `Single-page application (SPA)` for the platform
1. For the URI enter `https://localhost:6510/authentication/login-callback`
1. In the left menu, click `Overview`
1. Make a note of the value of `Application (client) ID`

### Callback URI of your website
1. In the left menu, click `Authentication`
1. Under `Single-page application` click `Add URI`
1. Enter the login-callback URI of your app (e.g. `https://ibm.com/authentication/login-callback`)
1. Click `Save`

### Expose an API for your application
1. Click the burger menu at the top-left of the page
1. Select `Azure Active Directory`
1. In the left menu, click `App registrations`
1. Click your application in the list
1. In the left menu, click `Expose an API`
1. At the top of the page, find `Application ID URI` and click `Set`
1. It will default to `api://{Application (client) ID}`, this ID is acceptable (*NOTE*: `{}` denotes the value, e.g. `d581756b-53b0-4957-820a-d6aa43fd69de`)
1. Click `Save`

### Add a scope to your API
1. Click `Add a scope`
1. For `Scope name` enter a name you wish to use to identify the scope (e.g. `access_as_user`)
1. For `Who can consent` select `Admins and users`
1. For `Admin consent display name enter `Read user's profile information`
1. For `Admin consent description` enter `Allows the application to read the user's profile information`
1. For `User consent display name` enter `Read your profile information`
1. For `User consent description` enter `Allows the application to read your profile information`
1. Ensure `Enabled` is selected
1. Click `Add scope`

### Add permissions for your application to use the `access_as_user` scope
1. In left menu, click `API permissions`
1. Click `Add a permission`
1. Click the `[My APIs]` tab at the top of the popup menu
1. Click your application in the list of items
1. In the `Permissions` section ensure `access_as_user` is checked
1. Click `Add permissions

### Add permissions for your application to access additional user information
1. In the same `API permissions` page, click `Add a permission`
1. Select the `Microsoft Graph` item at the top of the popup menu
1. Click the `Delegated permissions` item in the popup menu
1. Ensure `email` and `openid` are both checked
1. Click 'Add permissions`

### Finishing adding permissions
When you've finished the above steps, click the `Grant admin consent for {Your organisation name}
and then click `Yes`. Remember this step if you add any additional permissions
in future.

## Creating your web application
### Create the solution
1. Create a new ASP.NET hosted Blazor WASM application in Visual Studio
1. **IMPORTANT:** For `Authentication type` select `Microsoft identity platform`
1. The `Required components` wizard will appear and try to connect to your
   AAD registered app. This will also add items to AAD that we don't want
   (another app registration)
1. Click `Cancel`

### Match our sign-in callback URL to the registered application's
Edit `Properties/launchSettings.json` in both the client and
   server app, and set the `applicationUrl` to `https://localhost:6510`

### Set client application configuration for AAD
1. Edit `wwwroot/appsettings.json`
1. Replace the contents with the following
```json
{
	"AzureAd": {
		"ClientId": "{Application (client) ID}",
		"Authority": "https://login.microsoftonline.com/organizations",
		"ValidateAuthority": false
	},
	"ServerApi": {
		"Scopes": "api://{Application (client) ID}/access_as_user"
	}
}
```
3. Replace `{Application (client) ID}` with the GUID you noted earlier

### Set up client authentication to read from our configuration
1. Edit `Program.cs`
1. Change the `AddMsalAuthentication` section so scopes are read from the config file
```csharp
builder.Services.AddMsalAuthentication(options =>
{
  builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
  string? scopes =
    builder.Configuration!.GetSection("ServerApi")["Scopes"]
      ?? throw new InvalidOperationException("ServerApi::Scopes is missing from appsettings.json");

  options.ProviderOptions.DefaultAccessTokenScopes.Add(scopes);

  // Uncomment the next line if you have problems with a pop-up sign-in window
  // options.ProviderOptions.LoginMode = "redirect";
});
```

### Set server application configuration for AAD
1. Edit `appsettings.json`
1. Replace the `AzureAd` section with the following
```json
"AzureAd": {
  "Instance": "https://login.microsoftonline.com/",
  "TenantId": "organizations",
  "ClientId": "{Application (client) ID}",
  "Scopes": "api://{Application (client) ID}/access_as_user"
}
```
3. Replace `{Application (client) ID}` with the GUID you noted earlier

### Update the server to use WebApiAuthentication and add Authorization
1. Edit `Program.cs`
1. Replace
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
```
      with
```csharp
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);
builder.Services.AddAuthorization();
```

#### Remove server Views support (optional)
1. Edit `Program.cs`
1. Replace
```csharp
builder.Services.AddControllersWithViews();
```
      with
```csharp
builder.Services.AddControllers();
```

#### Remove server Razor Pages support (optional)
1. Edit `Program.cs`
1. Remove `builder.Services.AddRazorPages();`
1. Remove `app.UseRazorPages();`

#### Remove SCOPE requirement from server controller (workaround)
This step is only required until I work out why the user's scopes
are not being passed to the server.

1. Edit `WeatherForecastController.cs`
1. Comment out the line with the `[RequiredScope]` attribute on it
 