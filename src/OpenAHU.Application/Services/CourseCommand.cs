using OpenAHU.Application.Abstractions;
using OpenAHU.Application.Abstractions.Commands;
using OpenAHU.Application.Models;

namespace OpenAHU.Application.Services;

/// <summary>
/// 本地课程维护写用例。<see cref="ICourseCommand"/> 的唯一实现。
/// </summary>
/// <remarks>
/// 设计要点：
/// <list type="bullet">
/// <item>可预期失败（字段非法、学期/课程不存在、关联冲突）一律 <b>返回</b> <see cref="CourseWriteResult"/>，不抛异常；</item>
/// <item>只有数据库不可用才抛异常（由 05 号包装成 <c>CampusStoreUnavailableException</c>）；</item>
/// <item>跨表写入统一走 <see cref="IUnitOfWork"/>，保证“课程行 + 周次 + 选课关系”同生共死。</item>
/// </list>
/// </remarks>
public sealed class CourseCommand : ICourseCommand
{
    /// <summary>本地课程 id 前缀，便于在演示库里一眼区分手工录入与种子课程。</summary>
    private const string CourseIdPrefix = "course-local-";

    private readonly ITermRepository _terms;
    private readonly ICourseRepository _courses;
    private readonly IEnrollmentRepository _enrollments;
    private readonly IGradeRepository _grades;
    private readonly IExamRepository _exams;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ActiveStudentProvider _activeStudent;

    /// <summary>创建写用例。</summary>
    /// <param name="terms">学期仓储。</param>
    /// <param name="courses">课程仓储。</param>
    /// <param name="enrollments">选课仓储。</param>
    /// <param name="grades">成绩仓储（删除前的冲突判定）。</param>
    /// <param name="exams">考试仓储（删除前的冲突判定）。</param>
    /// <param name="unitOfWork">事务边界。</param>
    /// <param name="activeStudent">演示资料解析器。</param>
    public CourseCommand(
        ITermRepository terms,
        ICourseRepository courses,
        IEnrollmentRepository enrollments,
        IGradeRepository grades,
        IExamRepository exams,
        IUnitOfWork unitOfWork,
        ActiveStudentProvider activeStudent)
    {
        _terms = terms;
        _courses = courses;
        _enrollments = enrollments;
        _grades = grades;
        _exams = exams;
        _unitOfWork = unitOfWork;
        _activeStudent = activeStudent;
    }

    /// <inheritdoc />
    public async Task<CourseWriteResult> CreateAsync(CourseDraft draft, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(draft);

        // 身份先解析：库没初始化时直接 503，不要拿半截数据继续跑。
        var student = await _activeStudent.LoadAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(draft.TermId))
        {
            return CourseWriteResult.ValidationFailed("termId", "学期不能为空。");
        }

        var term = await _terms.FindAsync(draft.TermId, cancellationToken);
        if (term is null)
        {
            return CourseWriteResult.UnknownTerm(draft.TermId);
        }

        var invalid = CourseWriteValidator.Validate(draft, term);
        if (invalid is not null)
        {
            return invalid;
        }

        var course = new Course(
            Id: CreateCourseId(),
            Name: draft.Name!.Trim(),
            Teacher: draft.Teacher?.Trim() ?? string.Empty,
            Location: draft.Location?.Trim() ?? string.Empty,
            DayOfWeek: draft.DayOfWeek,
            StartSection: draft.StartSection,
            EndSection: draft.EndSection,
            WeekNumbers: WeekNumberNormalizer.Normalize(draft.WeekNumbers),
            TermId: term.Id);

        var siblings = await _courses.GetByStudentAndTermAsync(student.Id, term.Id, cancellationToken);
        var conflict = FindScheduleConflict(course, siblings);
        if (conflict is not null)
        {
            return conflict;
        }

        // 事务：课程行 + 周次（AddAsync 内）+ 选课关系（否则课表看不到这门课）。
        await _unitOfWork.ExecuteAsync(
            async token =>
            {
                await _courses.AddAsync(course, token).ConfigureAwait(false);
                await _enrollments.AddAsync(student.Id, course.Id, token).ConfigureAwait(false);
                return true;
            },
            cancellationToken).ConfigureAwait(false);

        return CourseWriteResult.Ok(course.Id, enrolledStudentCount: 1, otherStudentCount: 0);
    }

    /// <inheritdoc />
    public async Task<CourseWriteResult> UpdateAsync(
        string courseId,
        CourseDraft draft,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(draft);

        if (string.IsNullOrWhiteSpace(courseId))
        {
            return CourseWriteResult.NotFound(courseId ?? string.Empty);
        }

        var student = await _activeStudent.LoadAsync(cancellationToken);

        var existing = await _courses.FindAsync(courseId, cancellationToken);
        if (existing is null)
        {
            return CourseWriteResult.NotFound(courseId);
        }

        if (string.IsNullOrWhiteSpace(draft.TermId))
        {
            return CourseWriteResult.ValidationFailed("termId", "学期不能为空。");
        }

        var term = await _terms.FindAsync(draft.TermId, cancellationToken);
        if (term is null)
        {
            return CourseWriteResult.UnknownTerm(draft.TermId);
        }

        var invalid = CourseWriteValidator.Validate(draft, term);
        if (invalid is not null)
        {
            return invalid;
        }

        // 保留原 Id 与选课关系，只替换用户能编辑的字段。
        var updated = existing with
        {
            Name = draft.Name!.Trim(),
            Teacher = draft.Teacher?.Trim() ?? string.Empty,
            Location = draft.Location?.Trim() ?? string.Empty,
            DayOfWeek = draft.DayOfWeek,
            StartSection = draft.StartSection,
            EndSection = draft.EndSection,
            WeekNumbers = WeekNumberNormalizer.Normalize(draft.WeekNumbers),
            TermId = term.Id,
        };

        var siblings = await _courses.GetByStudentAndTermAsync(student.Id, term.Id, cancellationToken);
        var conflict = FindScheduleConflict(updated, siblings);
        if (conflict is not null)
        {
            return conflict;
        }

        // 影响面先统计好再写：编辑共享课程行会同步改变其他选修者的课表。
        var enrolled = await _enrollments.CountByCourseAsync(courseId, cancellationToken);
        var others = await _enrollments.CountOthersByCourseAsync(courseId, student.Id, cancellationToken);

        await _unitOfWork.ExecuteAsync(
            async token =>
            {
                await _courses.UpdateAsync(updated, token).ConfigureAwait(false);
                return true;
            },
            cancellationToken).ConfigureAwait(false);

        return CourseWriteResult.Ok(courseId, enrolled, others);
    }

    /// <inheritdoc />
    public async Task<CourseWriteResult> DeleteAsync(string courseId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(courseId))
        {
            return CourseWriteResult.NotFound(courseId ?? string.Empty);
        }

        var student = await _activeStudent.LoadAsync(cancellationToken);

        var existing = await _courses.FindAsync(courseId, cancellationToken);
        if (existing is null)
        {
            return CourseWriteResult.NotFound(courseId);
        }

        // 成绩与考试先于课程存在，删除课程会让它们随外键级联消失 —— 这是不可逆的数据丢失，
        // 因此一律 409 拒绝，并告诉用户“先删成绩/考试”。判定不区分学生：别人的成绩同样不能被静默删掉。
        if (await _grades.ExistsByCourseAsync(courseId, cancellationToken))
        {
            return CourseWriteResult.Conflict(
                CourseConflictReason.HasGrades,
                "该课程已有成绩记录，删除会导致成绩一并丢失，已拒绝。请先移除对应成绩后再删除课程。");
        }

        if (await _exams.ExistsByCourseAsync(courseId, cancellationToken))
        {
            return CourseWriteResult.Conflict(
                CourseConflictReason.HasExams,
                "该课程已有考试安排，删除会导致考试信息一并丢失，已拒绝。请先移除对应考试后再删除课程。");
        }

        var enrolled = await _enrollments.CountByCourseAsync(courseId, cancellationToken);
        var others = await _enrollments.CountOthersByCourseAsync(courseId, student.Id, cancellationToken);

        var deleted = await _unitOfWork.ExecuteAsync(
            token => _courses.DeleteAsync(courseId, token),
            cancellationToken).ConfigureAwait(false);

        return deleted
            ? CourseWriteResult.Ok(courseId, enrolled, others)
            : CourseWriteResult.NotFound(courseId);
    }

    /// <summary>生成本地课程主键。</summary>
    /// <returns>形如 <c>course-local-9f2c7a1b4e60</c> 的稳定唯一 id。</returns>
    private static string CreateCourseId()
    {
        return CourseIdPrefix + Guid.NewGuid().ToString("N")[..12];
    }

    /// <summary>检测与同一学生同学期其他课程的时间冲突。</summary>
    /// <param name="candidate">待写入课程（编辑时 Id 与库内一致，会被跳过）。</param>
    /// <param name="siblings">该学生同学期已有课程。</param>
    /// <returns>冲突时返回 <see cref="CourseWriteStatus.ValidationFailed"/>，否则 <c>null</c>。</returns>
    private static CourseWriteResult? FindScheduleConflict(Course candidate, IEnumerable<Course> siblings)
    {
        foreach (var other in siblings)
        {
            if (string.Equals(other.Id, candidate.Id, StringComparison.Ordinal))
            {
                continue;
            }

            if (other.DayOfWeek != candidate.DayOfWeek)
            {
                continue;
            }

            // 节次区间不重叠则不算冲突。
            if (other.EndSection < candidate.StartSection || other.StartSection > candidate.EndSection)
            {
                continue;
            }

            // 节次重叠但没有任何共同周次（如单双周错开）也不算冲突。
            if (!other.WeekNumbers.Any(candidate.WeekNumbers.Contains))
            {
                continue;
            }

            return CourseWriteResult.ValidationFailed(
                "startSection",
                $"与《{other.Name}》时间冲突（周{WeekdayText(other.DayOfWeek)} 第 {other.StartSection}–{other.EndSection} 节），请调整星期、节次或周次。");
        }

        return null;
    }

    /// <summary>把 <see cref="DayOfWeek"/> 转成中文单字，仅用于提示文案。</summary>
    /// <param name="dayOfWeek">星期。</param>
    /// <returns>“一”…“日”。</returns>
    private static string WeekdayText(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => "一",
            DayOfWeek.Tuesday => "二",
            DayOfWeek.Wednesday => "三",
            DayOfWeek.Thursday => "四",
            DayOfWeek.Friday => "五",
            DayOfWeek.Saturday => "六",
            DayOfWeek.Sunday => "日",
            _ => "?",
        };
    }
}
