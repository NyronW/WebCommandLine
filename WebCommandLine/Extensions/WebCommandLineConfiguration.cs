using Microsoft.AspNetCore.Authorization;

namespace WebCommandLine
{
    public class WebCommandLineConfiguration
    {
        public string StaticFilesUrl { get; set; } = "/WebCliFiles";
        public string WebCliUrl { get; set; } = "/webcli";
        public string HelpCommand { get; set; } = "help";
        public IAuthorizeData[]? Authorization { get; set; }
    }
}