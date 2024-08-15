using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace WebCommandLine;

public interface IConsoleCommand
{
    Task<ConsoleResult> RunAsync(CommandContext context, string[] args);
}

public class CommandContext
{
    public HttpContext HttpContext { get; }

    public CommandContext(HttpContext httpContext)
    {
        HttpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
    }
}
