using System.Text;

namespace WebCommandLine.Commands;

[ConsoleCommand("greet", "Returns a greeting message")]
public class Greet : ConsoleCommandBase
{
    public override ConsoleResult Help()
    {
        //greet "Nyron Williams"
        var sb = new StringBuilder("<table class='webcli-tbl'><tr><td colspan='3' class='webcli-val'>Lists available arguments</td></tr>");
        //sb.Append("<tr><td class='webcli-lbl'>-n</td><td>:</td><td class='webcli-val'>Username, this is the network identity used when signing on to VMG network</td></tr>");
        sb.Append("<tr><td class='webcli-lbl'>USAGE:</td><td colspan='2' class='webcli-val'>greet nyron</td></tr>");
        sb.Append("</table>");

        return new ConsoleResult(sb.ToString()) { isHTML = true };
    }

    protected override Task<ConsoleResult> RunAsyncCore(string[] args)
    {
        if (args.Length == 0)
        {
            return Task.FromResult(ConsoleResult.CreateError("Invalid argument pass"));
        }

        return Task.FromResult(new ConsoleResult($"Hello, {args[0]}. Nice to meet you!!") { isHTML = false });
    }
}
