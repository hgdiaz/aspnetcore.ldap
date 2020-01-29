# aspnetcore.ldap
Authenticate Active Directory users with asp.net core

The main goal of this project is to show how to use Active Directory authetication in an ASP.NET Core web app, using the Novell.Directory.Ldap.NETStandard library to communicate with AD.
Based on David Liang response in this thread: [https://stackoverflow.com/a/50141578/4798452]

The project shows how to Authenticate against AD and also Authorize  using custom policies according to the role of each user in AD.


## How to implement in your project?
First, create a new Web Applicatt (MVC) in ASP.NET Core 3.1 and then   add the [Novell.Directory.Ldap.NETStandard](https://www.nuget.org/packages/Novell.Directory.Ldap.NETStandard/) library using the nuget package
You can findthe source code and more information of this project in [this link](https://github.com/dsbenghe/Novell.Directory.Ldap.NETStandard)

Then we must create the model for the users of our application:

    public interface IAppUser
    {
        string Username { get; }
        string DisplayName { get; }
        string Email { get; }
        string[] Roles { get; }
    }
    public class AppUser : IAppUser
    {
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string[] Roles { get; set; }
    }
Also, we must create a class for mapping values from appsettings.json so we can have a Ldap configuration object:

    public class LdapConfig
    {
        public string Url { get; set; }
        public string BindDn { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string SearchBase { get; set; }
        public string SearchFilter { get; set; }
    }
Then we must create one of the most important classes in the project: the [Ldap Authentication Service](https://github.com/hgdiaz/aspnetcore.ldap/tree/master/aspnetcore.ldap/aspnetcore.ldap/Services)
The most important thing here is the Login() method. Here is were we use the Novell.Directory.Ldap.NETStandard library in order to connect to the AD Server.

The next step is configure our application in the Startup.cs class.
In the ConfigureServices method we must read the Ldap configuration from the appsettings.json file and inject the LdapAuthenticationService.

    services.Configure<LdapConfig>(this.Configuration.GetSection("ldap"));
    services.AddScoped<IAuthenticationService, LdapAuthenticationService>();
When adding the MVC middleware, a good practice is configuring our site to only be accesible by autheticated users, unless we specifically allow public access to certain sections (methods in the controllers). We can do this by creating a an authorization policy with .RequireAuthenticatedUser()
As we are using cookies, we must add the middeware and read the configuration from our appsettings:

        var cookiesConfig = this.Configuration.GetSection("cookies")
            .Get<CookiesConfig>();
        services.AddAuthentication(
            CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = cookiesConfig.CookieName;
                options.LoginPath = cookiesConfig.LoginPath;
                options.LogoutPath = cookiesConfig.LogoutPath;
                options.AccessDeniedPath = cookiesConfig.AccessDeniedPath;
                options.ReturnUrlParameter = cookiesConfig.ReturnUrlParameter;
            });
Now that we have Authentication, we can add Authorization to our site. in this example we can see just one custom policy, but you can add as many as you want just copying the "options.AddPolicy()" lines.
In this case we add a policy named "Require.Ldap.User", which will require the claim "aspnetcore.ldap.user" with a true value:

        services.AddAuthorization(options =>
        {
            options.AddPolicy("Require.Ldap.User", policy =>
                              policy.RequireClaim("aspnetcore.ldap.user", "true")
                                    .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme)
                                  );
        });
Also, remember to add these lines in the Configure() method:

        app.UseAuthorization();
        app.UseAuthentication();

Now that we have the service for connecting to the AD and the configuration is complete, we can create a controller for user Login/Logout.
In this case we have a controller named AccountController where we inject the authentication service:
        private readonly Services.IAuthenticationService _authService;

    public AccountController(Services.IAuthenticationService authService)
    {
        _authService = authService;
    }
When the user tries to login, we use the user name and password to check if they are valid against the AD:

    var user = _authService.Login(model.Username, model.Password);
If the credentials are valid, we can check the AD groups associated with this user, and add the claims according to these groups:

                    var claimsIdentity = new ClaimsIdentity(userClaims, _authService.GetType().Name);
                    if (Array.Exists(user.Roles, s => s.Contains("aspnetcore.ldap")))
                    {
                        claimsIdentity.AddClaim(new Claim("aspnetcore.ldap.user", "true"));
                    }         
What we do here is check if the user belongs to the AD group called "aspnetcore.ldap". If he belongs to this group, we add a claim named "aspnetcore.ldap.user" with a value equals to "true" (this is what we have configured before in the Startup.cs).

Now, how can we restrict sections of our site?
If you check the HomeController, you can see that the index is permitted for everyone, because we use "AllowAnonymous":

    [AllowAnonymous]
    public IActionResult Index()
    {
        return View();
    }
But for a restricted section, we use the "Authorize" attribute. And here we can specify which policies must have the user in order to get access to the section:

    [Authorize(Policy = "Require.Ldap.User", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public IActionResult SecurePage()
    {
        return View();
    }
Remember, ""Require.Ldap.User"" is the name of the policy we added in the services.AddAuthorization() in the Startup class.


## Meta

This is an open source project that welcomes contributions/suggestions/bug reports from those who use it. If you have any ideas on how to improve the sample, please [post an issue here on github](https://github.com/hgdiaz/aspnetcore.ldap/issues). 


