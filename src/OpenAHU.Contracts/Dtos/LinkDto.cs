namespace OpenAHU.Contracts.Dtos;

/// <summary>
/// 本地常用入口（来自 campus_links 表）。首版只包含项目开发相关的固定链接，
/// 不指向学校业务站点，也不做任何抓取。
/// </summary>
/// <param name="Id">链接主键。</param>
/// <param name="Title">展示标题。</param>
/// <param name="Url">绝对 URL（http/https）。</param>
/// <param name="Category">分组，例如 <c>项目</c>、<c>开发</c>。</param>
public sealed record LinkDto(
    string Id,
    string Title,
    string Url,
    string Category);
