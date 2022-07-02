# WebCommandLine
Add a CLI to an aspnet core web application that supports depedency injection , authentication with as little two lines of code.

### Installing WebCommandLine

You should install [WebCommandLine with NuGet](https://www.nuget.org/packages/WebCommandLine):

    Install-Package WebCommandLine
    
Or via the .NET Core command line interface:

    dotnet add package MediatR

Either commands, from Package Manager Console or .NET Core CLI, will download and install MediatR and all required dependencies.

### How do I get started?]

First, configure WebCommandLibne to know where the commands are located, in the startup of your application:

```csharp
var builder = WebApplication.CreateBuilder(args);

//...

builder.Services.AddWebCommandLine(typeof(MyClass));// Tell WebCommandLine which assembly to scan for console commands

//...

app.UseWebCommandLine();

```

Create a class that implements the IConsoleCommand interface and add the ConsoleCommandAttribute to the class definition

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
