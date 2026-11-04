using System.Diagnostics;
using OpenAHU.Contracts.Errors;

namespace OpenAHU.Api.Errors;

/// <summary>
/// 统一错误响应构造器：任何非 2xx 响应都必须经由这里，保证 <c>{code,message,traceId}</c> 形状一致，
/// 且 <c>Message</c> 不含数据库路径、SQL、连接串等内部信息。
/// </summary>
public static class ApiResults
{
    /// <summary>会话校验失败的错误码；冻结错误码表尚无此值，待评审后迁入 <see cref="ApiErrorCodes"/>。</summary>
    public const string UnauthorizedCode = "unauthorized";

    /// <summary>取本次请求的追踪号：优先用分布式追踪 ID，其次用 ASP.NET Core 的请求标识。</summary>
    /// <param name="context">当前请求上下文。</param>
    /// <returns>非空追踪号。</returns>
    public static string TraceId(HttpContext context)
    {
        return Activity.Current?.Id ?? context.TraceIdentifier;
    }

    /// <summary>构造错误体对象。</summary>
    /// <param name="code">错误码。</param>
    /// <param name="message">面向用户的安全文案。</param>
    /// <param name="context">当前请求上下文。</param>
    /// <returns>可直接序列化的错误体。</returns>
    public static ApiError ErrorBody(string code, string message, HttpContext context)
    {
        return new ApiError(code, message, TraceId(context));
    }

    /// <summary>构造带状态码的错误响应。</summary>
    /// <param name="statusCode">HTTP 状态码。</param>
    /// <param name="code">错误码。</param>
    /// <param name="message">面向用户的安全文案。</param>
    /// <param name="context">当前请求上下文。</param>
    /// <returns>可直接返回的最小 API 结果。</returns>
    public static IResult Error(int statusCode, string code, string message, HttpContext context)
    {
        return Results.Json(ErrorBody(code, message, context), statusCode: statusCode);
    }

    /// <summary>直接写出错误响应（用于中间件等无法返回 <see cref="IResult"/> 的场合）。</summary>
    /// <param name="context">当前请求上下文。</param>
    /// <param name="statusCode">HTTP 状态码。</param>
    /// <param name="code">错误码。</param>
    /// <param name="message">面向用户的安全文案。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>表示写出过程的任务。</returns>
    public static Task WriteAsync(HttpContext context, int statusCode, string code, string message, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(ErrorBody(code, message, context), cancellationToken);
    }

    /// <summary>数据层未就绪时的标准响应（503 + <c>db_unavailable</c>）。</summary>
    /// <param name="context">当前请求上下文。</param>
    /// <returns>503 错误响应。</returns>
    public static IResult DbUnavailable(HttpContext context)
    {
        return Error(
            StatusCodes.Status503ServiceUnavailable,
            ApiErrorCodes.DbUnavailable,
            "本地数据服务尚未就绪，请稍后重试。",
            context);
    }
}
