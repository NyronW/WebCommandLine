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
            }

            if (string.Equals(context.TagName, "body",
                StringComparison.OrdinalIgnoreCase))
            {
                var javascript = $"<script src=\"{config.StaticFilesUrl}/webcli.js\" asp-append-version=\"true\"></script>";
                javascript += $"<script type=\"text/javascript\">$(function(){{window.cli = window.cli || new WebCLI(\"{config.WebCliUrl}\");}})</script> ";

                output.PostContent.AppendHtml(javascript);
            }
        }
    }
}
