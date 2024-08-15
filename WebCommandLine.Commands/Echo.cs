namespace WebCommandLine.Commands
{
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
}