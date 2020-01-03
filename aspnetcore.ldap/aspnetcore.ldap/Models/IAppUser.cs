using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace aspnetcore.ldap.Models
{
    public interface IAppUser
    {
        string Username { get; }
        string DisplayName { get; }
        string Email { get; }
        string[] Roles { get; }
    }
}
