using Microsoft.Extensions.Options;

namespace umm.Plugins.Lite;

internal sealed class LiteOptions
{
    public int PageSize { get; set; } = 60;

    public sealed class Validator : IValidateOptions<LiteOptions>
    {
        public ValidateOptionsResult Validate(string? name, LiteOptions options)
        {
            if (options.PageSize <= 0) return ValidateOptionsResult.Fail($"{nameof(options.PageSize)} cannot be <= 0.");
            return ValidateOptionsResult.Success;
        }
    }
}
