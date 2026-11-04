using OpenAHU.Api.Errors;
using OpenAHU.Contracts.Dtos;
using OpenAHU.Contracts.Errors;

namespace OpenAHU.Api.Endpoints;

/// <summary>
/// <c>/api/v1</c> 路由表。本骨架实现了不依赖数据层的部分：<c>data-status</c> 与
/// <c>termId</c> 参数校验（400）。其余端点按冻结路径先占位，返回 503 + <c>db_unavailable</c>，
/// 等 04 Application / 05 Infrastructure 就绪后逐个替换为真实实现。
/// </summary>
public static class CampusEndpoints
{
    /// <summary>本程序集版本号，供 <c>/health</c> 排障使用（不含构建路径）。</summary>
    private static readonly string ApiVersion =
        typeof(CampusEndpoints).Assembly.GetName().Version?.ToString() ?? "0.0.0";

    /// <summary>注册全部 v1 端点。</summary>
    /// <param name="endpoints">端点路由构建器。</param>
    /// <returns>同一个构建器，便于链式调用。</returns>
    public static IEndpointRouteBuilder MapCampusEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1");

        // 真实实现：就绪状态如实声明。数据层未接入前 DatabaseReady 恒为 false，
        // 5 号提供探针后改为读取真实状态（不得回显数据库路径）。
        group.MapGet("/health", () => Results.Ok(new HealthDto(
            Status: "starting",
            DatabaseReady: false,
            Version: ApiVersion)));

        // 真实实现：数据来源声明是常量，不依赖 SQLite。
        group.MapGet("/data-status", () => Results.Ok(new DataStatusDto(
            Provider: "LocalSQLite",
            IsDemo: true,
            UpdatedAt: null,
            Message: "本地演示数据，非学校官方信息")));

        // 真实实现：参数校验属于 3 号职责，不依赖数据层即可断言。
        // 缺参/空白 termId 一律 400 invalid_request；未知 termId 归 404 还是 400 待 4 号定（见 INTERFACE_PLAN.md）。
        MapPendingTermScoped(group, "GET", "/courses");
        MapPendingTermScoped(group, "GET", "/grades");
        MapPendingTermScoped(group, "GET", "/exams");

        // 占位：形状与错误码先冻结，实现待 Application 层。
        // 注意：这些路由目前恒返回 503，不得作为 Gate A 的通过证据。
        MapPending(group, "GET", "/profile");
        MapPending(group, "GET", "/terms");
        MapPending(group, "GET", "/links");
        MapPending(group, "POST", "/local/courses");
        MapPending(group, "PUT", "/local/courses/{id}");
        MapPending(group, "DELETE", "/local/courses/{id}");

        return endpoints;
    }

    private static void MapPending(RouteGroupBuilder group, string method, string pattern)
    {
        group.MapMethods(
            pattern,
            new[] { method },
            (HttpContext context) => ApiResults.DbUnavailable(context));
    }

    /// <summary>
    /// 需要 <c>termId</c> 的只读端点占位：先做参数校验（400），参数合法后才因数据层未接返回 503。
    /// 数据层接入时只替换最后的 503 分支，校验与错误码保持不变。
    /// </summary>
    private static void MapPendingTermScoped(RouteGroupBuilder group, string method, string pattern)
    {
        group.MapMethods(
            pattern,
            new[] { method },
            (HttpContext context) =>
            {
                var termId = context.Request.Query["termId"].ToString();
                if (string.IsNullOrWhiteSpace(termId))
                {
                    return ApiResults.Error(
                        StatusCodes.Status400BadRequest,
                        ApiErrorCodes.InvalidRequest,
                        "termId 不能为空，请先选择学期。",
                        context);
                }

                return ApiResults.DbUnavailable(context);
            });
    }
}
