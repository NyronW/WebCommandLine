using Microsoft.AspNetCore.Authorization;

namespace WebApp.Authorization
{
    public class WebCmdLineRequirement : IAuthorizationRequirement
    {
        public string[] Commands { get; set; } = ["help", "diskspace"];
    }
}
