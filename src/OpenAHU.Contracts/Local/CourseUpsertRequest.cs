namespace OpenAHU.Contracts.Local;

/// <summary>
/// 本地课程新增/编辑请求体（<c>POST /api/v1/local/courses</c>、<c>PUT /api/v1/local/courses/{id}</c>）。
/// 不含 Id：新增由后端生成，编辑以路径参数为准。
/// </summary>
/// <remarks>
/// <b>状态：待跨组评审 + 待 4/5 号确认字段语义。</b> 冻结文档只给出写接口的路径与“具备请求校验”要求，
/// 未定义请求体；本类型由 3 号提出，供 2 号本地课程维护页面先绑定。
/// 字段合法性判定属 04 Application，Api 只做结构反序列化与基础校验。
/// </remarks>
/// <param name="Name">课程名称，非空。</param>
/// <param name="Teacher">任课教师，可为空字符串。</param>
/// <param name="Location">上课地点，可为空字符串。</param>
/// <param name="DayOfWeek">
/// 上课星期，C# 语义（周日=0、周一=1 …… 周六=6）；入库时由 Application 转成 1–7（周日=7）。
/// </param>
/// <param name="StartSection">起始节次 1–20。</param>
/// <param name="EndSection">结束节次，不小于 <paramref name="StartSection"/>，不超过 20。</param>
/// <param name="WeekNumbers">开课周次，必须落在所属学期 <c>week_count</c> 范围内，去重后非空。</param>
/// <param name="TermId">所属学期，必须存在于库内。</param>
public sealed record CourseUpsertRequest(
    string Name,
    string Teacher,
    string Location,
    System.DayOfWeek DayOfWeek,
    int StartSection,
    int EndSection,
    IReadOnlyList<int> WeekNumbers,
    string TermId);
