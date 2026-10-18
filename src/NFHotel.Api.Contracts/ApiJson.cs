using System.Text.Json;

namespace NFHotel.Api.Contracts;

/// <summary>The JSON settings that the API and the client both use, so a value written by one is read by the other.</summary>
public static class ApiJson
{
    /// <summary>Web defaults: camelCase names, case-insensitive reading, enums as numbers.</summary>
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}