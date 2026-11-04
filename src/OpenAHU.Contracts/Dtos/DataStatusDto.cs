namespace OpenAHU.Contracts.Dtos;

/// <summary>
/// 数据来源声明（GET /api/v1/data-status）。
/// 本版恒定：<c>Provider="LocalSQLite"</c>、<c>IsDemo=true</c>。
/// 前端必须在全局状态区展示 <see cref="Message"/>，不得改写为学校来源。
/// </summary>
/// <param name="Provider">数据提供方，固定字符串 <c>LocalSQLite</c>。</param>
/// <param name="IsDemo">固定 <c>true</c>：全部为虚构演示数据。</param>
/// <param name="UpdatedAt">库内数据的最后更新时间（带偏移 ISO 8601）；无数据时为 <c>null</c>。</param>
/// <param name="Message">固定文案 <c>本地演示数据，非学校官方信息</c>。</param>
public sealed record DataStatusDto(
    string Provider,
    bool IsDemo,
    DateTimeOffset? UpdatedAt,
    string Message);
