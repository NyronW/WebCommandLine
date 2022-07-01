using Fclp;
using System.Text;

namespace WebCommandLine.Commands
{
    public class AddUserArguments
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public List<string> Claims { get; set; }
    }

    [ConsoleCommand("add-user", "adds a new user account")]
    public class AddUser : ConsoleCommandBase<AddUserArguments>
    {
        protected readonly FluentCommandLineParser<AddUserArguments> _parser;

        public AddUser()
        {
            _parser = new FluentCommandLineParser<AddUserArguments>();

            // specify which property the value will be assigned too.
            _parser.Setup(arg => arg.UserName)
             .As('n', "userName") // define the short and long option name
             .Required(); // using the standard fluent Api to declare this Option as required.

            _parser.Setup(arg => arg.Password)
             .As('p', "password")
             .SetDefault("P@$$w0rd");

            _parser.Setup(arg => arg.Claims)
                .As('c', "claims");
        }

        public override ConsoleResult Help()
        {
            //add-user -n userName -p password -c claims [optional]
            var sb = new StringBuilder("<table class='webcli-tbl'><tr><td colspan='3' class='webcli-val'>Lists available arguments</td></tr>");
            sb.Append("<tr><td class='webcli-lbl'>-n</td><td>:</td><td class='webcli-val'>Name that uniquely identifies user</td></tr>");
            sb.Append("<tr><td class='webcli-lbl'>-p</td><td>:</td><td class='webcli-val'>User password, Default will be used is not value is provided</td></tr>");
            sb.Append("<tr><td class='webcli-lbl'>-c</td><td>:</td><td class='webcli-val'>Claims that determine what funcions the user can access. Valid options includes: reports,user, customer & webcli (optional)</td></tr>");
            sb.Append("<tr><td class='webcli-lbl'>USAGE:</td><td colspan='2' class='webcli-val'>add-user -n nyron.williams@willcorp.com -p MySecretPassword -c \"report,user,webcli\" -w 1</td></tr>");
            sb.Append("</table>");

            return new ConsoleResult(sb.ToString()) { isHTML = true };
        }

        protected override CommandLineParserResult<AddUserArguments> Parse(string[] args)
        {
            var result = _parser.Parse(args);

            return new CommandLineParserResult<AddUserArguments>(_parser.Object, result.ErrorText);
        }

        protected override Task<ConsoleResult> RunAsyncCore(AddUserArguments userToAdd)
        {
            if (!userToAdd.UserName.Equals("foo", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new ConsoleResult($"User created successfully"));
            }
            else
            {
                return Task.FromResult(ConsoleResult.CreateError("Invalid username"));
            }
        }
    }
}
