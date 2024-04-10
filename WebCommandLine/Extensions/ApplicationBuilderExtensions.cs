using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
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

namespace WebCommandLine
{
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

                builder.Run(async context =>
                {
                    var cancellationToken = context.RequestAborted;

                    if (!await AuthorizeAsync(context, policyEvaluator!, config, authPolicyProvider@))
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        await context.Response.WriteModelAsync(ConsoleResult.CreateError("Not authorized."));
                        return;
                    }

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

                    if (string.IsNullOrWhiteSpace(command.CmdLine))
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                        await context.Response.WriteModelAsync(ConsoleResult.CreateError("Invalid command"));
                        return;
                    }

                    var args = command.GetArgs();
                    var cmd = args.First();

                    var commands = app.ApplicationServices.GetServices<IConsoleCommand>();

                    if (cmd.Equals(config.HelpCommand,StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                        await context.Response.WriteModelAsync(Help(commands, config));
                        return;
                    }

                    IConsoleCommand cmdToRun = null;

                    foreach (var cmdType in commands)
                    {
                        var attr = (ConsoleCommandAttribute)cmdType.GetType().GetTypeInfo().GetCustomAttributes(typeof(ConsoleCommandAttribute)).FirstOrDefault();
                        if (attr == null || !attr.Name.Equals(cmd, StringComparison.OrdinalIgnoreCase)) continue;
                        cmdToRun = cmdType; break;
                    }

                    if (cmdToRun == null)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                        await context.Response.WriteModelAsync(new ConsoleErrorResult($"Invalid or missing command. Run the {config.HelpCommand} command to see a list of available commands"));
                        return;
                    }

                    //Instantiate and run the command
                    try
                    {
                        await context.Response.WriteModelAsync(await cmdToRun.RunAsync(args.Skip(1).ToArray()));
                    }
                    catch
                    {
                        await context.Response.WriteModelAsync(new ConsoleErrorResult());
                    }
                });
            });

            return app;
        }

        private static ConsoleResult Help(IEnumerable<IConsoleCommand> commands, WebCommandLineConfiguration config)
        {
            var sb = new StringBuilder($"<table class='webcli-tbl'><tr><td class='webcli-lbl'>{config.HelpCommand}</td> <td>:</td> <td class='webcli-val'>Lists available commands</td></tr>");

            foreach (var cmdType in commands)
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
            WebCommandLineConfiguration configuration, IAuthorizationPolicyProvider policyProvider)
        {
            bool authorized = false;

            if (configuration.Authorization is null)
            {
                authorized = true;
            }
            else
            {
                if (_authorizationPolicy is null)
                {
                    _authorizationPolicy = await AuthorizationPolicy.CombineAsync(policyProvider, configuration.Authorization);
                }

                AuthenticateResult authenticateResult = await policyEvaluator.AuthenticateAsync(_authorizationPolicy, context);
                PolicyAuthorizationResult authorizeResult = await policyEvaluator.AuthorizeAsync(_authorizationPolicy, authenticateResult, context, null);

                if (authorizeResult.Challenged)
                {
                    await ChallengeAsync(context);
                }
                else if (authorizeResult.Forbidden)
                {
                    await ForbidAsync(context);
                }
                else
                {
                    authorized = true;
                }
            }

            return authorized;
        }

        private static async Task ChallengeAsync(HttpContext httpContext)
        {
            if (_authorizationPolicy.AuthenticationSchemes.Count > 0)
            {
                foreach (string authenticationScheme in _authorizationPolicy.AuthenticationSchemes)
                {
                    await httpContext.ChallengeAsync(authenticationScheme);
                }
            }
            else
            {
                await httpContext.ChallengeAsync();
            }
        }

        private static async Task ForbidAsync(HttpContext httpContext)
        {
            if (_authorizationPolicy.AuthenticationSchemes.Count > 0)
            {
                foreach (string authenticationScheme in _authorizationPolicy.AuthenticationSchemes)
                {
                    await httpContext.ForbidAsync(authenticationScheme);
                }
            }
            else
            {
                await httpContext.ForbidAsync();
            }
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
}