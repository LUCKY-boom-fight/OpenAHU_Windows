namespace OpenAHU.Contracts.Dtos;

/// <summary>
/// 成绩（students -> enrollments -> courses -> grades）。
/// 本版只转述数据库中的演示分数，不推导、不宣称任何官方绩点算法。
/// </summary>
/// <param name="CourseId">课程主键。</param>
/// <param name="CourseName">课程名称（便于前端直接展示，避免二次请求）。</param>
/// <param name="Credits">学分，数据库约束 0–50。</param>
/// <param name="Score">百分制分数；<c>null</c> 表示未公布——不得以 0 代替。</param>
/// <param name="GradePoint">绩点；<c>null</c> 表示未公布或项目未提供。</param>
/// <param name="TermId">课程所属学期主键（来自 courses.term_id）。</param>
public sealed record GradeDto(
    string CourseId,
    string CourseName,
    decimal Credits,
    decimal? Score,
    decimal? GradePoint,
    string TermId);
