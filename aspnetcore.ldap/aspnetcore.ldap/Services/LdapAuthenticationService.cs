using aspnetcore.ldap.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Novell.Directory.Ldap;
using System.Text.RegularExpressions;

namespace aspnetcore.ldap.Services
{
    public class LdapAuthenticationService : IAuthenticationService
    {
        private const string MemberOfAttribute = "memberOf";
        private const string DisplayNameAttribute = "displayName";
        private const string SAMAccountNameAttribute = "sAMAccountName";
        private const string MailAttribute = "mail";

        private readonly LdapConfig _config;
        private readonly LdapConnection _connection;

        public LdapAuthenticationService(IOptions<LdapConfig> configAccessor)
        {
            _config = configAccessor.Value;
            _connection = new LdapConnection();
        }

        public IAppUser Login(string username, string password)
        {
            _connection.Connect(_config.Url, LdapConnection.DEFAULT_PORT);
            _connection.Bind(_config.Username, _config.Password);

            var searchFilter = String.Format(_config.SearchFilter, username);
            var result = _connection.Search(
                _config.SearchBase,
                LdapConnection.SCOPE_SUB,
                searchFilter,
                new[] {
                    MemberOfAttribute,
                    DisplayNameAttribute,
                    SAMAccountNameAttribute,
                    MailAttribute
                },
                false
            );

            try
            {
                var user = result.next();
                if (user != null)
                {
                    _connection.Bind(user.DN, password);
                    if (_connection.Bound)
                    {
                        var accountNameAttr = user.getAttribute(SAMAccountNameAttribute);
                        if (accountNameAttr == null)
                        {
                            throw new Exception("Your account is missing the account name.");
                        }

                        var displayNameAttr = user.getAttribute(DisplayNameAttribute);
                        if (displayNameAttr == null)
                        {
                            throw new Exception("Your account is missing the display name.");
                        }

                        var emailAttr = user.getAttribute(MailAttribute);
                        if (emailAttr == null)
                        {
                            throw new Exception("Your account is missing an email.");
                        }

                        var memberAttr = user.getAttribute(MemberOfAttribute);
                        if (memberAttr == null)
                        {
                            throw new Exception("Your account is missing roles.");
                        }

                        return new AppUser
                        {
                            DisplayName = displayNameAttr.StringValue,
                            Username = accountNameAttr.StringValue,
                            Email = emailAttr.StringValue,
                            Roles = memberAttr.StringValueArray
                                .Select(x => GetGroup(x))
                                .Where(x => x != null)
                                .Distinct()
                                .ToArray()
                        };
                    }
                }
            }
            finally
            {
                _connection.Disconnect();
            }

            return null;
        }

        private string GetGroup(string value)
        {
            Match match = Regex.Match(value, "^CN=([^,]*)");
            if (!match.Success)
            {
                return null;
            }

            return match.Groups[1].Value;
        }
    }
}
