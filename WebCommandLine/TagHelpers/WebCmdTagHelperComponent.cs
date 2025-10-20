using Microsoft.AspNetCore.Razor.TagHelpers;
using System;

namespace WebCommandLine.TagHelpers
{
    public class WebCmdTagHelperComponent : TagHelperComponent
    {
        private readonly WebCommandLineConfiguration config;

        public WebCmdTagHelperComponent(WebCommandLineConfiguration configuration)
        {
            config = configuration;
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (string.Equals(context.TagName, "head",
                StringComparison.OrdinalIgnoreCase))
            {

                output.PostContent.AppendHtml($"<link rel=\"stylesheet\" href=\"{config.StaticFilesUrl}/webcli.css\" asp-append-version=\"true\" />");
                var javascript = $"<script src=\"{config.StaticFilesUrl}/webcli.js\" asp-append-version=\"true\"></script>";
                if (config.AutoInitJsInstance)
                {
                    var options = $"{{enableAutoCopy: {config.EnableAutoCopy.ToString().ToLower()}, enableRightClickPaste: {config.EnableRightClickPaste.ToString().ToLower()}}}";
                    javascript += $"<script type=\"text/javascript\">document.addEventListener(\"DOMContentLoaded\", function(){{window.cli = window.cli || new WebCLI(\"{config.WebCliUrl}\", null, {options});}});</script> ";
                }

                output.PostContent.AppendHtml(javascript);
            }
        }
    }
}
