namespace OpenAHU.Contracts.Errors;

/// <summary>
/// 统一错误响应体。所有非 2xx 响应都必须是该结构，前端只按 <see cref="Code"/> 分支。
/// </summary>
/// <param name="Code">机器可读错误码，取值见 <see cref="ApiErrorCodes"/>。</param>
/// <param name="Message">
/// 面向用户的安全文案：不得包含数据库绝对路径、SQL 语句、连接串或个人资料。
/// </param>
/// <param name="TraceId">本次请求的追踪号，用于和服务端日志对照。</param>
public sealed record ApiError(
    string Code,
    string Message,
    string TraceId);
