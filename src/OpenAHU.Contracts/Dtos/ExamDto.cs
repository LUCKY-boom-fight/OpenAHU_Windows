namespace OpenAHU.Contracts.Dtos;

/// <summary>
/// 考试安排（students -> enrollments -> courses -> exams）。
/// 未公布的考试允许时间、地点全为 <c>null</c>，前端排到列表末尾并明确提示。
/// </summary>
/// <param name="Id">考试主键。</param>
/// <param name="CourseName">课程名称。</param>
/// <param name="StartsAt">开考时间，带时区偏移的 ISO 8601（如 <c>2026-12-21T09:00:00+08:00</c>）；未公布为 <c>null</c>。</param>
/// <param name="EndsAt">结束时间；未公布为 <c>null</c>，非空时必须晚于 <paramref name="StartsAt"/>。</param>
/// <param name="Location">考场；未公布为 <c>null</c>。</param>
/// <param name="TermId">所属学期主键。</param>
public sealed record ExamDto(
    string Id,
    string CourseName,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    string? Location,
    string TermId);
