using Microsoft.Extensions.Options;

namespace umm.Plugins.Opds;

internal sealed class OpdsOptions
{
    public int PageSize { get; set; } = 12;

    public sealed class Validator : IValidateOptions<OpdsOptions>
    {
        public ValidateOptionsResult Validate(string? name, OpdsOptions options)
        {
            if (options.PageSize <= 0) return ValidateOptionsResult.Fail($"{nameof(options.PageSize)} cannot be <= 0.");
            return ValidateOptionsResult.Success;
        }
    }
}
