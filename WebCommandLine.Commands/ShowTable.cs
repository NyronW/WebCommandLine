using System.ComponentModel;

namespace WebCommandLine.Commands;

[ConsoleCommand("gen-table", "Returns a striped html table")]
public class ShowTable : IConsoleCommand
{
    public Task<ConsoleResult> RunAsync(string[] args)
    {
        var people = new List<Person>
        {
            new Person { FirstName = "John", LastName = "Doe", Age = 30 },
            new Person { FirstName = "Jane", LastName = "Smith", Age = 25 }
        };

        var result = ConsoleResult.AsHtmlTable(people);
        return Task.FromResult(result);
    }
}

public class Person
{
    [DisplayName("First Name")]
    public string FirstName { get; set; }

    [DisplayName("Last Name")]
    public string LastName { get; set; }

    public int Age { get; set; }
}