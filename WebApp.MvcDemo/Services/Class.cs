using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace WebApp.MvcDemo.Services
{
    public class InMemoryUserStore
    {
        private readonly Dictionary<string, (string Password, string[] Roles, Dictionary<string, string> Claims)> _users;

        public InMemoryUserStore()
        {
            _users = new Dictionary<string, (string, string[], Dictionary<string, string>)>
            {
                { "admin", ("password", [ "Admin" ], new Dictionary<string, string> { { "isAdmin", "true" } }) },
                { "poweruser", ("password", ["BusinessAdmin"], new Dictionary<string, string> { { "isAdmin", "false" } }) }
            };
        }

        public bool ValidateUser(string username, string password, out ClaimsPrincipal principal)
        {
            principal = null!;
            if (_users.TryGetValue(username, out var user) && user.Password == password)
            {
                var claims = new List<Claim> { new(ClaimTypes.Name, username) };
                claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
                claims.AddRange(user.Claims.Select(claim => new Claim(claim.Key, claim.Value)));

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                principal = new ClaimsPrincipal(identity);
                return true;
            }
            return false;
        }
    }

}
