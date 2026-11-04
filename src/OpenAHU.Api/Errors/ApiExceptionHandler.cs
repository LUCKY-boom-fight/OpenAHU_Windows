using Microsoft.AspNetCore.Diagnostics;
using OpenAHU.Contracts.Errors;

namespace OpenAHU.Api.Errors;

/// <summary>
/// 兜底异常处理：未捕获异常统一转成 500 + <see cref="ApiErrorCodes.Internal"/>，
/// 响应体不含异常消息、堆栈、数据库路径等内部信息，细节只进日志。
/// </summary>
/// <remarks>
/// 数据库不可用（文件被占用、只读、迁移未完成）应表现为 503 + <c>db_unavailable</c>，
/// 但判定需要 05 Infrastructure 的异常类型。为避免 Api 反向依赖 EF Core，
/// 这一判定必须由 04 Application 以结果对象或领域异常表达（见 README「待 4 号确认」）。
/// </remarks>
public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;

    /// <summary>创建处理器。</summary>
    /// <param name="logger">日志。</param>
    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>写出 500 错误响应。</summary>
    /// <param name="httpContext">当前请求上下文。</param>
    /// <param name="exception">未处理异常。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>总是 <c>true</c>（已处理）。</returns>
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (httpContext.Response.HasStarted)
        {
            return false;
        }

        var traceId = ApiResults.TraceId(httpContext);
        _logger.LogError(exception, "未处理异常 {Method} {Path}，traceId={TraceId}", httpContext.Request.Method, httpContext.Request.Path, traceId);

        await ApiResults.WriteAsync(
            httpContext,
            StatusCodes.Status500InternalServerError,
            ApiErrorCodes.Internal,
            "本地服务发生未预期错误，请稍后重试。",
            cancellationToken);
        return true;
    }
}
