using System.Text;

namespace WebCommandLine.Commands;

public class AddMemberArguments
{
    public string Name { get; set; }
    public int Age { get; set; }
    public string Category { get; set; }
}

[ConsoleCommand("add-member", "adds a new club member")]
public class AddMember : ConsoleCommandBase<AddMemberArguments>
{
    protected readonly CommandLineParser<AddMemberArguments> _parser;

    public AddMember()
    {
        _parser = new CommandLineParser<AddMemberArguments>();

        _parser.Bind(arg => arg.Name)
            .As('n', "name") 
            .Required(); 

        _parser.Bind(arg => arg.Age)
            .As('a', "age")
            .WhereGreaterThan(18,"Must be over 18 to join!");

        _parser.Bind(arg => arg.Category)
            .As('c', "category")
            .WhereIn(["basic","gold","platinum"],"Member category must be one of the following: basic, gold, platinum");
    }

    public override ConsoleResult Help()
    {
        var sb = new StringBuilder("<table class='webcli-tbl'><tr><td colspan='3' class='webcli-val'>Lists available arguments</td></tr>");
        sb.Append("<tr><td class='webcli-lbl'>-n | -name</td><td>:</td><td class='webcli-val'>Name that uniquely identifies member</td></tr>");
        sb.Append("<tr><td class='webcli-lbl'>-a</td><td>:</td><td class='webcli-val'>Age of member. Must be over 18</td></tr>");
        sb.Append("<tr><td class='webcli-lbl'>-c</td><td>:</td><td class='webcli-val'>Member category. Valid options includes: basic,gold, platinum</td></tr>");
        sb.Append("<tr><td class='webcli-lbl'>USAGE:</td><td colspan='2' class='webcli-val'>add-member -a 34 -c platinum -w 1</td></tr>");
        sb.Append("</table>");

        return new ConsoleResult(sb.ToString()) { isHTML = true };
    }

    protected override CommandLineParserResult<AddMemberArguments> Parse(string[] args)
    {
        var result = _parser.Parse(args);

        return result;
    }

    protected override Task<ConsoleResult> RunAsyncCore(AddMemberArguments model)
    {
        if (!model.Name.Equals("Jone Doe", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new ConsoleResult($"Member created successfully"));
        }
        else
        {
            return Task.FromResult(ConsoleResult.CreateError("Invalid name"));
        }
    }
}
