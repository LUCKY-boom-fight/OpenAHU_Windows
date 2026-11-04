namespace OpenAHU.Contracts.Dtos;

/// <summary>
/// 学期（来自 SQLite terms 表）。
/// </summary>
/// <param name="Id">学期主键，例如 <c>term-2026-fall</c>。</param>
/// <param name="Name">学期名称，例如 <c>2026年秋季学期（演示）</c>。</param>
/// <param name="StartDate">教学首周周一（ISO <c>yyyy-MM-dd</c>）；周次推算的唯一基准。</param>
/// <param name="WeekCount">总教学周数，数据库约束 1–30。</param>
/// <param name="IsCurrent">是否当前学期；同一时刻最多一个为 <c>true</c>。</param>
public sealed record TermDto(
    string Id,
    string Name,
    DateOnly StartDate,
    int WeekCount,
    bool IsCurrent);
