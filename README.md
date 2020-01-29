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
