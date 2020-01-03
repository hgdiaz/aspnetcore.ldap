using aspnetcore.ldap.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace aspnetcore.ldap.Services
{
    public interface IAuthenticationService
    {
        IAppUser Login(string username, string password);
    }
}
