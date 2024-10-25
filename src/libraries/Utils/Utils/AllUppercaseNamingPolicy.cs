using System.Text.Json;

namespace Utils;

public sealed class AllUppercaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name) => name.ToUpper();
}
