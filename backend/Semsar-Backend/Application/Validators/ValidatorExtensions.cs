using System.Text.RegularExpressions;
using FluentValidation;

namespace Application.Validators;

public static partial class ValidatorExtensions
{
    private static readonly Regex HtmlTagRegex = HtmlTagRegexGenerated();
    private static readonly Regex ScriptPatternRegex = ScriptPatternRegexGenerated();
    private static readonly Regex ControlCharRegex = ControlCharRegexGenerated();

    [GeneratedRegex(@"<[^>]*>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagRegexGenerated();

    [GeneratedRegex(@"</?\s*(script|iframe|object|embed|form|input|textarea|select|button|style|link|meta|applet|frame|frameset|ilayer)\b[^>]*>|javascript\s*:|on\w+\s*=|data\s*:", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ScriptPatternRegexGenerated();

    [GeneratedRegex(@"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", RegexOptions.Compiled)]
    private static partial Regex ControlCharRegexGenerated();

    public static IRuleBuilderOptions<T, string> MustNotContainHtml<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.Must(value => value == null || !HtmlTagRegex.IsMatch(value))
            .WithMessage("{PropertyName} must not contain HTML tags");
    }

    public static IRuleBuilderOptions<T, string> MustNotContainScriptPatterns<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.Must(value => value == null || !ScriptPatternRegex.IsMatch(value))
            .WithMessage("{PropertyName} contains prohibited script patterns");
    }

    public static IRuleBuilderOptions<T, string> MustNotContainControlChars<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.Must(value => value == null || !ControlCharRegex.IsMatch(value))
            .WithMessage("{PropertyName} must not contain control characters");
    }

    public static IRuleBuilderOptions<T, string> MustBeSafeText<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .MustNotContainHtml()
            .MustNotContainScriptPatterns()
            .MustNotContainControlChars();
    }

    public static IRuleBuilderOptions<T, string?> MustBeHoneypot<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder.Must(value => string.IsNullOrWhiteSpace(value))
            .WithMessage("Invalid submission detected");
    }
}
