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

//basic registration

builder.Services.AddWebCommandLine(typeof(MyClass));// Tells WebCommandLine which assembly to scan for console commands

//Or you can customize its behavior
builder.Services.AddWebCommandLine(options =>
{
    options.StaticFilesUrl = "/MyWebAssets"; //This will be the base path for static files
    options.WebCliUrl = "/MyWebCli"; // cammand requests will goes to this endpoint
    // If true the JavaScript bjects with be automatically initialized, otherwise you have to manually inti window.cli object
    // You would typically set this value to false when you want to override the default httpHandler
    options.AutoInitJsInstance = false; 
    
    // Copy and paste configuration
    options.EnableAutoCopy = true; // Enable select-to-copy functionality
    options.EnableRightClickPaste = true; // Enable right-click-to-paste functionality
}, typeof(ShowTable).Assembly);

//...

app.UseWebCommandLine();

```

Create a class that implements the IConsoleCommand interface and add the ConsoleCommandAttribute to the class definition:

```csharp
[ConsoleCommand("echo", "Echos back the first arg received")]
public class Echo : IConsoleCommand
{
    public Task<ConsoleResult> RunAsync(CommandContext context, string[] args)
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

    protected override Task<ConsoleResult> RunAsyncCore(CommandContext context, string[] args)
    {
        if (args.Length == 0)
        {
            return Task.FromResult(ConsoleResult.CreateError("Invalid argument pass"));
        }

        return Task.FromResult(new ConsoleResult($"Hello, {args[0]}. Nice to meet you!!") { isHTML = false });
    }
}
```
There is also support for strongly typed commands, using the build in  argument parser or you can use your own parsing tool or logic.
```csharp
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
            .WhereIn(["basic","gold","platinum"]);
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

    protected override Task<ConsoleResult> RunAsyncCore(CommandContext context, AddMemberArguments model)
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
```
The example below shows how to implement a strongly typed command using [Fluent Command Line Parser](https://fclp.github.io/fluent-command-line-parser/) to parse the arguments:

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

    protected override Task<ConsoleResult> RunAsyncCore(CommandContext context, AddUserArguments userToAdd)
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
### How do I secure WebCommandLine?

WebCommandLine leverages existing ASP.NET Authorization features and requires little effort for integration. The WebCommandLine endpoint can be secured by setting the Authorization property of the WebCommandLineConfiguration class when calling the AddWebCommandLine method during your application startup or
You can simply add the add the Authorize attribute to the command class.

```csharp

//...

builder.Services.AddWebCommandLine(options =>
{
    options.Authorization = new[] { new WebCommandLineAuthorization { Policy = "AdminUser", Roles="Operator,Developer" } };
});

//...

//Adding sample policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminUser", policyBuilder =>
    {
        policyBuilder.RequireAuthenticatedUser();
        policyBuilder.RequireClaim("isAdmin", "true");
    });

    options.AddPolicy("PowerUser", policyBuilder =>
    {
        policyBuilder.RequireAuthenticatedUser();
        policyBuilder.AddRequirements(new WebCmdLineRequirement());
        policyBuilder.RequireAssertion(ctx =>
        {
            return ctx.User.IsInRole("BusinessAdmin");
        });
    });
});

//....


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

```

### How do I customize WebCommandLine?

You can change WebCommandLine url base paths by modifying the WebCommandLineConfiguration class during your application startup.

```csharp

//...

builder.Services.AddWebCommandLine(options =>
{
    options.StaticFilesUrl = "/MyWebAssets"; //This will be the base path for static files
    options.WebCliUrl = "/MyWebCli"; //command requests will go to this endpoint

    // If true the JavaScript objects with be automatically initialized, otherwise you have to manually inti window.cli object
    // You would typically set this value to false when you want to override the default httpHandler
    options.AutoInitJsInstance = false; 
});

//...

```

Client side code to override httpHandler
```javascript
document.addEventListener("DOMContentLoaded", function () {
   function ajaxHttpHandler(endpoint, options) {
        return new Promise((resolve, reject) => {
            $.ajax({
                url: endpoint,
                method: options.method,
                headers: options.headers,
                data: options.body,
                success: function(data) {
                    resolve(data); // Return raw data as is
                },
                error: function(xhr) {
                    // Handle error response here (including parsing)
                    reject(xhr.responseText || xhr.statusText);
                }
            });
        });
    }

    function axiosHttpHandler(endpoint, options) {
        const { method, headers, body } = options;
        return axios({
            url: endpoint,
            method: method,
            headers: headers,
            data: body,
        }).then(response => response.data) // Return the response as raw text
          .catch(error => {
              // Handle the error here (including parsing)
              return Promise.reject(error.response?.data || error.message);
          });
    }


    window.cli = new WebCLI('/MyWebCli', ajaxHttpHandler);
});

```

### Breaking Changes
2.0.0 - Added a new command context parameter to the ICommand interface and implementing base classes. This will enable greater flexibility and make the code more extendable.
        This new context currently exposes the current HttpContexc, which can be used to access the HttpRequest (headers, users, claims etc) which can make integrating with other
        areas of asp.net request pipeline much easier
