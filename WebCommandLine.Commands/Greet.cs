using Microsoft.AspNetCore.Authorization;
using System.Text;

namespace WebCommandLine.Commands;

[Authorize(Policy = "PowerUser")]
[ConsoleCommand("greet", "Returns a greeting message")]
public class Greet : ConsoleCommandBase
{
    public override ConsoleResult Help()
    {
        var sb = new StringBuilder("<table class='webcli-tbl'><tr><td colspan='3' class='webcli-val'>Lists available arguments</td></tr>");
        sb.Append("<tr><td class='webcli-lbl'>USAGE:</td><td colspan='2' class='webcli-val'>greet nyron</td></tr>");
        sb.Append("</table>");

        return new ConsoleResult(sb.ToString()) { isHTML = true };
    }

    protected override Task<ConsoleResult> RunAsyncCore(CommandContext context, string[] args)
    {
        if (args.Length != 0)
            return Task.FromResult(new ConsoleResult($"Hello, {args[0]}. Nice to meet you!!") { isHTML = false });

        var user = context.HttpContext.User;
        if (user != null)
        {
            var name = user.FindFirst("preferred_username")?.Value;
            if (!string.IsNullOrEmpty(name))
                return Task.FromResult(new ConsoleResult($"Hello, {name}. Nice to meet you!!"));
        }

        return Task.FromResult(ConsoleResult.CreateError("Invalid argument pass"));
    }
}
