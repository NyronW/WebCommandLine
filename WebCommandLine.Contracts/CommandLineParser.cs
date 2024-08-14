using System.Collections.Generic;
using System.Linq.Expressions;
using System;

namespace WebCommandLine;

public class CommandLineParser<T> where T : new()
{
    private readonly T _instance = new();
    private readonly List<Func<string[], string?>> _bindings = new();
    private readonly string _prefix;

    public CommandLineParser(string prefix = "-")
    {
        _prefix = prefix;
    }

    public CommandLineBinding<T, TProperty> Bind<TProperty>(
        Expression<Func<T, TProperty>> property)
    {
        return new CommandLineBinding<T, TProperty>(this, property);
    }

    internal void AddBinding(Func<string[], string?> binding)
    {
        _bindings.Add(binding);
    }

    public CommandLineParserResult<T> Parse(string[] args)
    {
        foreach (var binding in _bindings)
        {
            var error = binding(args);
            if (error != null)
            {
                return new CommandLineParserResult<T>(_instance, error);
            }
        }

        return new CommandLineParserResult<T>(_instance, null);
    }

    internal T GetInstance()
    {
        return _instance;
    }

    internal string GetPrefix()
    {
        return _prefix;
    }
}


