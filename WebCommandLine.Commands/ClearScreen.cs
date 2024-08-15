namespace WebCommandLine.Commands
{
    [ConsoleCommand("cls", "Clears the console")]
    public class ClearScreen : IConsoleCommand
    {
        public Task<ConsoleResult> RunAsync(CommandContext context, string[] args)
        {
            throw new NotImplementedException();   //Implemented on client
        }
    }
}