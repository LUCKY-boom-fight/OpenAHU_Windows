namespace OpenAHU.Contracts.Dtos;

/// <summary>
/// 当前本机演示资料（来自 SQLite students 表，由后端 active_demo_student_id 定位）。
/// 前端不得据此推断这是学校官方学籍。
/// </summary>
/// <param name="Id">学生主键，例如 <c>demo-001</c>。</param>
/// <param name="DisplayName">演示姓名，例如 <c>演示学生甲</c>。</param>
/// <param name="College">学院；数据库可空。</param>
/// <param name="Major">专业；数据库可空。</param>
/// <param name="GradeYear">入学年级，如 2024；数据库可空。</param>
/// <param name="IsDemo">恒定 <c>true</c>：本版只提供虚构演示资料。</param>
public sealed record StudentProfileDto(
    string Id,
    string DisplayName,
    string? College,
    string? Major,
    int? GradeYear,
    bool IsDemo);
