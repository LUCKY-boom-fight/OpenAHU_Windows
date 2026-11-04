using System.Text.Json;

namespace OpenAHU.Contracts.Json;

/// <summary>
/// 桌面端与 API 共用的 JSON 约定，避免两侧各自配置导致字段名/大小写漂移。
/// 每次调用返回新实例，禁止共享可变实例。
/// </summary>
public static class ContractJson
{
    /// <summary>
    /// 创建统一序列化选项：camelCase 命名、读取大小写不敏感、允许字符串数字。
    /// <see cref="DateOnly"/> 输出 <c>yyyy-MM-dd</c>，
    /// <see cref="DateTimeOffset"/> 输出带偏移的 ISO 8601，枚举按数字输出。
    /// </summary>
    /// <returns>可在 HttpClient 反序列化与 ASP.NET Core 配置中重复使用的新选项对象。</returns>
    public static JsonSerializerOptions CreateOptions()
    {
        return new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }
}
