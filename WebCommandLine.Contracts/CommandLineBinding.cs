using System.Linq.Expressions;
using System.Linq;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WebCommandLine;

public class CommandLineBinding<T, TProperty> where T: new()
{
    private readonly CommandLineParser<T> _parser;
    private readonly PropertyInfo _property;
    private string _shortForm;
    private string _longForm;
    private TProperty _defaultValue;
    private Func<TProperty, bool> _validation;
    private string? _validationErrorMessage;
    private bool _isRequired;

    internal CommandLineBinding(CommandLineParser<T> parser, Expression<Func<T, TProperty>> property)
    {
        _parser = parser;
        _property = (property.Body as MemberExpression)?.Member as PropertyInfo
            ?? throw new ArgumentException("Invalid property expression.");
    }

    public CommandLineBinding<T, TProperty> As(char shortForm)
    {
        _shortForm = shortForm.ToString();
        _longForm = null!;
        RegisterBinding();
        return this;
    }

    public CommandLineBinding<T, TProperty> As(string longForm)
    {
        _shortForm = null!;
        _longForm = longForm;
        RegisterBinding();
        return this;
    }

    public CommandLineBinding<T, TProperty> As(char shortForm, string longForm = null!)
    {
        _shortForm = shortForm.ToString();
        _longForm = longForm;
        RegisterBinding();
        return this;
    }

    public CommandLineBinding<T, TProperty> WithDefault(TProperty value)
    {
        _defaultValue = value;
        RegisterBinding();
        return this;
    }

    public CommandLineBinding<T, TProperty> WithValidation(Func<TProperty, bool> validate)
    {
        _validation = validate;
        RegisterBinding();
        return this;
    }

    public CommandLineBinding<T, TProperty> Required()
    {
        _isRequired = true;
        return WithValidation(value => !EqualityComparer<TProperty>.Default.Equals(value, default(TProperty)))
               .WithValidationErrorMessage($"{_longForm ?? _shortForm} is required");
    }

    public CommandLineBinding<T, TProperty> WhereGreaterThan(TProperty threshold, string errorText = null!)
    {
        return WithValidation(value => Comparer<TProperty>.Default.Compare(value, threshold) > 0)
               .WithValidationErrorMessage(errorText ?? $"{_longForm ?? _shortForm} must be greater than {threshold}");
    }

    public CommandLineBinding<T, TProperty> WhereLessThan(TProperty threshold, string errorText = null!)
    {
        return WithValidation(value => Comparer<TProperty>.Default.Compare(value, threshold) < 0)
               .WithValidationErrorMessage(errorText ?? $"{_longForm ?? _shortForm} must be less than {threshold}");
    }

    public CommandLineBinding<T, TProperty> WhereMatchesRegex(string pattern, string errorText = null!)
    {
        return WithValidation(value => Regex.IsMatch(value.ToString()!, pattern))
               .WithValidationErrorMessage(errorText ?? $"{_longForm ?? _shortForm} must match pattern {pattern}");
    }

    public CommandLineBinding<T, TProperty> WhereIn(IEnumerable<TProperty> collection, string errorText = null!)
    {
        return WithValidation(value => collection.Any(i => i.Equals(value)))
               .WithValidationErrorMessage(errorText ?? $"{_longForm ?? _shortForm} is invalid");
    }

    public CommandLineBinding<T, TProperty> WhereNotEqual(TProperty comparisonValue, string errorText = null!)
    {
        return WithValidation(value => !EqualityComparer<TProperty>.Default.Equals(value, comparisonValue))
               .WithValidationErrorMessage($"{_longForm ?? _shortForm} must not equal {comparisonValue}");
    }

    public CommandLineBinding<T, TProperty> WithValidationErrorMessage(string errorMessage)
    {
        _validationErrorMessage = errorMessage;
        return this;
    }

    private void RegisterBinding()
    {
        _parser.AddBinding(args =>
        {
            var argValue = args.SkipWhile(a => !(a == _parser.GetPrefix() + _shortForm)
                                               && !string.Equals(a, _parser.GetPrefix() + _longForm, StringComparison.OrdinalIgnoreCase))
                               .Skip(1).FirstOrDefault();

            if (argValue != null)
            {
                var value = (TProperty)Convert.ChangeType(argValue, typeof(TProperty));
                if (_validation != null && !_validation(value))
                {
                    var errorMsg = _validationErrorMessage ?? $"Syntax error: {_shortForm ?? _longForm}";
                    return string.Format(errorMsg, _shortForm ?? _longForm);
                }

                _property.SetValue(_parser.GetInstance(), value);
            }
            else
            {
                if (_isRequired)
                {
                    var errorMsg = _validationErrorMessage ?? $"{_longForm ?? _shortForm} is required";
                    return string.Format(errorMsg, _shortForm ?? _longForm);
                }

                _property.SetValue(_parser.GetInstance(), _defaultValue);
            }

            return null;
        });
    }
}


