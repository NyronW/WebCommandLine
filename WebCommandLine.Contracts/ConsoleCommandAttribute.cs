using System;

namespace WebCommandLine
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ConsoleCommandAttribute : Attribute
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public ConsoleCommandAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
