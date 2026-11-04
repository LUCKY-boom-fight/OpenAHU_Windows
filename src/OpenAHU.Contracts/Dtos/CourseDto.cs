namespace OpenAHU.Contracts.Dtos;

/// <summary>
/// 一门课程的基础信息与固定上课时段（来自 courses 表，周次来自 course_weeks 表）。
/// 课程表按 <c>TermId, DayOfWeek, StartSection</c> 排序后下发。
/// </summary>
/// <param name="Id">课程主键，写入接口使用的稳定标识。</param>
/// <param name="Name">课程名称。</param>
/// <param name="Teacher">任课教师（演示值，可空字符串）。</param>
/// <param name="Location">上课地点（演示值，可空字符串）。</param>
/// <param name="DayOfWeek">
/// 上课星期，使用 C# <see cref="System.DayOfWeek"/> 语义：周日=0、周一=1 …… 周六=6。
/// 注意 SQLite courses.day_of_week 存储为 周一=1 …… 周日=7，
/// 换算公式：<c>(DayOfWeek)(dbDayOfWeek % 7)</c>（7 -> 0）。JSON 默认以数字下发。
/// </param>
/// <param name="StartSection">起始节次，1–20。</param>
/// <param name="EndSection">结束节次，不小于 <paramref name="StartSection"/>，不超过 20。</param>
/// <param name="WeekNumbers">开课周次（升序去重，支持单双周/隔周）；为空表示尚未排课。</param>
/// <param name="TermId">所属学期主键。</param>
public sealed record CourseDto(
    string Id,
    string Name,
    string Teacher,
    string Location,
    System.DayOfWeek DayOfWeek,
    int StartSection,
    int EndSection,
    IReadOnlyList<int> WeekNumbers,
    string TermId);
