using System.Threading.Tasks;

namespace WebCommandLine
{
    public interface IConsoleCommand
    {
        Task<ConsoleResult> RunAsync(string[] args);
    }
}