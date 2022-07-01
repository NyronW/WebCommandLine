using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Text;
using System.Text.Json;
using WebCommandLine;

namespace WebApp.Authorization
{
    public class WebCmdLineAuthHandler : AuthorizationHandler<WebCmdLineRequirement>
    {
        private readonly Type AttributeType = typeof(ConsoleCommandAttribute);

        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly WebCommandLineConfiguration configuration;
        private readonly IEnumerable<IConsoleCommand> consoleCommands;

        public WebCmdLineAuthHandler(IHttpContextAccessor httpContextAccessor, WebCommandLineConfiguration configuration,
            IEnumerable<IConsoleCommand> consoleCommands)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.configuration = configuration;
            this.consoleCommands = consoleCommands;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, WebCmdLineRequirement requirement)
        {
            try
            {
                if (!consoleCommands.Any())
                {
                    context.Succeed(requirement);
                    return;
                }

                var req = httpContextAccessor.HttpContext.Request;
                var url = req.PathBase.Value;

                if (url.Equals(configuration.WebCliUrl, StringComparison.OrdinalIgnoreCase))
                {
                    req.EnableBuffering();

                    using (var stream = new StreamReader(req.Body, encoding: Encoding.UTF8, bufferSize: 1024, leaveOpen: true))
                    {
                        var jsonString = await stream.ReadToEndAsync();
                        var command = JsonSerializer.Deserialize<CommandInput>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        req.Body.Seek(0, SeekOrigin.Begin);

                        var found = false;

                        foreach (var cmdType in consoleCommands)
                        {
                            var attr = (ConsoleCommandAttribute)cmdType.GetType().GetTypeInfo().GetCustomAttributes(AttributeType).FirstOrDefault();
                            if (attr == null || !attr.Name.Equals(command.CmdLine, StringComparison.OrdinalIgnoreCase)) continue;

                            found = true; break;
                        }

                        if (!found)
                        {
                            context.Succeed(requirement);
                            return;
                        }

                        if (!requirement.Commands.Any(c => c.Equals(command.CmdLine, StringComparison.OrdinalIgnoreCase)))
                        {
                            context.Fail();
                            return;
                        }
                    }

                }

                context.Succeed(requirement);
            }
            catch
            {
                context.Fail();
            }
        }
    }
}
