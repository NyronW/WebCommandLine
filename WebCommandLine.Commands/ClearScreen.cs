namespace WebCommandLine.Commands
{
    [ConsoleCommand("cls", "Clears the console")]
    public class ClearScreen : IConsoleCommand
    {
        public Task<ConsoleResult> RunAsync(string[] args)
        {
            throw new NotImplementedException();   //Implemented on client
        }
    }
}