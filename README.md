# WebCommandLine
Add a CLI to an ASP.NET Core web application that supports dependency injection, authorization and authentication with as little as two lines of code.

### Installing WebCommandLine

You should install [WebCommandLine with NuGet](https://www.nuget.org/packages/WebCommandLine):

    Install-Package WebCommandLine
    
Or via the .NET command line interface (.NET CLI):

    dotnet add package WebCommandLine

Either commands, from Package Manager Console or .NET Core CLI, will allow download and installation of WebCommandLine and all its required dependencies.

### How do I get started?

First, configure WebCommandLine to know where the commands are located, in the startup of your application:

```csharp
var builder = WebApplication.CreateBuilder(args);

//...

builder.Services.AddWebCommandLine(typeof(MyClass));// Tells WebCommandLine which assembly to scan for console commands

//...

app.UseWebCommandLine();

```

Create a class that implements the IConsoleCommand interface and add the ConsoleCommandAttribute to the class definition:

```csharp
[ConsoleCommand("echo", "Echos back the first arg received")]
public class Echo : IConsoleCommand
{
    public Task<ConsoleResult> RunAsync(string[] args)
    {
        if (args.Length != 0)
        {
            return Task.FromResult(new ConsoleResult(args[0]));
        }

        return Task.FromResult(ConsoleResult.CreateError("I didn't hear anything!"));
    }
}
```
Run the application and then press the CTRL + ` keys to launch the web command line.

You can also implement the abstract base class <em>ConsoleCommandBase</em> to support help text for your commands by passing '?' or help as argument when running your command:

```csharp
[ConsoleCommand("greet", "Returns a greeting message")]
public class Greet : ConsoleCommandBase
{
    public override ConsoleResult Help()
    {
        var sb = new StringBuilder("<table class='webcli-tbl'><tr><td colspan='3' class='webcli-val'>Lists available arguments</td></tr>");
        sb.Append("<tr><td class='webcli-lbl'>USAGE:</td><td colspan='2' class='webcli-val'>greet Nyron</td></tr>");
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
```
There is also support for strongly typed commands. The example below shows how to implement a strongly typed command using [Fluent Command Line Parser](https://fclp.github.io/fluent-command-line-parser/) to parse the arguments:

```csharp
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
        sb.Append("<tr><td class='webcli-lbl'>-c</td><td>:</td><td class='webcli-val'>Claims that determine what functions the user can access. Valid options includes:              reports,user, customer & webcli (optional)</td></tr>");
        sb.Append("<tr><td class='webcli-lbl'>USAGE:</td><td colspan='2' class='webcli-val'>add-user -n nyron.williams@willcorp.com -p MySecretPassword -c                          \"report,user,webcli\" -w 1</td></tr>");
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
```
