using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace WebCommandLine;

public static class ApplicationBuilderExtensions
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    private static AuthorizationPolicy _authorizationPolicy;

    public static IApplicationBuilder UseWebCommandLine(this IApplicationBuilder app)
    {
        var config = app.ApplicationServices.GetService<WebCommandLineConfiguration>();
        if (config == null) return app;

        app.Map(config.StaticFilesUrl, builder =>
        {
            var provider = new ManifestEmbeddedFileProvider(
                assembly: Assembly.GetExecutingAssembly(), "Resources");

            builder.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = provider
            });
        });

        app.Map(config.WebCliUrl, builder =>
        {
            var authPolicyProvider = app.ApplicationServices.GetService<IAuthorizationPolicyProvider>();
            var policyEvaluator = app.ApplicationServices.GetService<IPolicyEvaluator>();
            var logger = app.ApplicationServices.GetRequiredService<ILogger<IConsoleCommand>>();

            builder.Run(async context =>
            {
                var cancellationToken = context.RequestAborted;

                try
                {
                    var req = context.Request;

                    if (req.Method != HttpMethod.Post.Method)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                        await context.Response.WriteModelAsync(ConsoleResult.CreateError("Method not supported."));
                        return;
                    }

                    if (req.ContentLength == 0)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        await context.Response.WriteModelAsync(ConsoleResult.CreateError("No command argument detected."));
                        return;
                    }

                    var command = new CommandInput();

                    if (req.Body.CanSeek) req.Body.Seek(0, SeekOrigin.Begin);

                    using (var reader = new StreamReader(req.Body, encoding: Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024))
                    {
                        var jsonString = await reader.ReadToEndAsync();
                        command = JsonSerializer.Deserialize<CommandInput>(jsonString, JsonSerializerOptions);
                    }

                    if (string.IsNullOrWhiteSpace(command?.CmdLine))
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        await context.Response.WriteModelAsync(ConsoleResult.CreateError("Invalid command"));
                        logger.LogDebug("Received invalid command from client");
                        return;
                    }

                    var args = command.GetArgs();
                    var cmd = args.First();

                    var commands = app.ApplicationServices.GetServices<IConsoleCommand>();

                    if (cmd.Equals(config.HelpCommand, StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                        await context.Response.WriteModelAsync(Help(commands, config));
                        return;
                    }

                    IConsoleCommand cmdToRun = null!;

                    foreach (var cmdType in commands)
                    {
                        var attr = cmdType.GetType().GetTypeInfo().GetCustomAttributes(typeof(ConsoleCommandAttribute)).FirstOrDefault() as ConsoleCommandAttribute;
                        if (attr == null || !attr.Name.Equals(cmd, StringComparison.OrdinalIgnoreCase)) continue;
                        cmdToRun = cmdType; break;
                    }

                    if (cmdToRun == null)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        await context.Response.WriteModelAsync(new ConsoleErrorResult($"Invalid or missing command. Run the {config.HelpCommand} command to see a list of available commands"));
                        return;
                    }

                    // Authorization check
                    var authorizeAttribute = cmdToRun.GetType().GetCustomAttribute<AuthorizeAttribute>();
                    if (!await AuthorizeAsync(context, policyEvaluator!, config, authorizeAttribute!, authPolicyProvider!))
                    {
                        return;
                    }

                    var commandContext = new CommandContext(context);
                    await context.Response.WriteModelAsync(await cmdToRun.RunAsync(commandContext, args.Skip(1).ToArray()));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unhandled exception occurred while processing command");
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    await context.Response.WriteModelAsync(new ConsoleErrorResult("Internal server error occurred while executing command"));
                }
            });
        });

        return app;
    }


    private static ConsoleResult Help(IEnumerable<IConsoleCommand> commands, WebCommandLineConfiguration config)
    {
        var sb = new StringBuilder($"<table class='webcli-tbl'><tr><td class='webcli-lbl'>{config.HelpCommand}</td> <td>:</td> <td class='webcli-val'>Lists available commands</td></tr>");

        foreach (var cmdType in commands.OrderBy(c => c.GetType().Name))
        {
            var attr = cmdType.GetType().GetTypeInfo().GetCustomAttributes(typeof(ConsoleCommandAttribute))
                                            .FirstOrDefault() as ConsoleCommandAttribute;
            if (attr == null) { continue; }

            sb.Append($"<tr><td class='webcli-lbl'>{HttpUtility.HtmlEncode(attr.Name)}</td> <td>:</td> <td class='webcli-val'>{HttpUtility.HtmlEncode(attr.Description)}</td></tr>");
        }
        sb.Append("<tr><td colspan='3' class='webcli-lbl'>Arguments that containing space must be surrouned be a double quotes. e.g \"My Value\"</td></tr>");
        sb.Append("</table>");

        return new ConsoleResult(sb.ToString()) { isHTML = true };
    }

    private static async Task<bool> AuthorizeAsync(HttpContext context, IPolicyEvaluator policyEvaluator,
        WebCommandLineConfiguration configuration, AuthorizeAttribute authorizeAttribute, IAuthorizationPolicyProvider policyProvider)
    {
        // If no authorization policy is configured and no authorize attribute is on the command, allow the request
        if (configuration.Authorization == null && authorizeAttribute == null)
        {
            return true;
        }

        // Combine the configured policies
        if (_authorizationPolicy == null)
        {
            List<IAuthorizeData> authorizeDatas = [];
            if (authorizeAttribute != null) authorizeDatas.Add(authorizeAttribute);
            if (configuration.Authorization != null) authorizeDatas.AddRange(configuration.Authorization);

            _authorizationPolicy = await AuthorizationPolicy.CombineAsync(policyProvider, authorizeDatas);
        }

        // Authenticate the request (this will set the context.User principal)
        AuthenticateResult authenticateResult = await policyEvaluator.AuthenticateAsync(_authorizationPolicy, context);

        // Evaluate the authorization policy
        PolicyAuthorizationResult authorizeResult = await policyEvaluator.AuthorizeAsync(_authorizationPolicy, authenticateResult, context, resource: null);

        if (authorizeResult.Challenged)
        {
            // If the user is not authenticated, return 401 Unauthorized
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteModelAsync(ConsoleResult.CreateError("Cannot execute command: Unauthenticated."));
            return false;
        }
        else if (authorizeResult.Forbidden)
        {
            // If the user is authenticated but not authorized, return 403 Forbidden
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await context.Response.WriteModelAsync(ConsoleResult.CreateError("Cannot execute command: Access denied."));
            return false;
        }

        // If the request is authorized, proceed
        return true;
    }
    #region Extension Methods
    internal static Task WriteModelAsync<T>(this HttpResponse response, T arg, CancellationToken cancellationToken = default)
    {
        var content = JsonSerializer.Serialize(arg);
        return response.WriteAsync(content, cancellationToken);
    }

    internal static async ValueTask WriteAccessDeniedResponse(this HttpContext context,
        string? message = null, int? statusCode = null, CancellationToken cancellationToken = default)
    {
        context.Response.StatusCode = statusCode ?? (int)HttpStatusCode.Forbidden;
        await context.Response.WriteModelAsync(ConsoleResult.CreateError(message ?? "Access denied"), cancellationToken);
    }
    #endregion
}