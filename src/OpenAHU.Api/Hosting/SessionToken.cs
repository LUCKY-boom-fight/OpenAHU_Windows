using System.Security.Cryptography;
using System.Text;

namespace OpenAHU.Api.Hosting;

/// <summary>
/// 本地 API 的随机会话令牌。桌面端启动后端时由后端生成，经交接文件交给客户端；
/// 之后每个 <c>/api/v1/*</c> 请求都必须带上 <see cref="HeaderName"/> 头。
/// 目的：防止本机浏览器或其他进程调用写入接口——CORS 不能做到这一点。
/// </summary>
public sealed class SessionToken
{
    /// <summary>请求头名称。</summary>
    public const string HeaderName = "X-OpenAHU-Session";

    private readonly byte[] _expected;

    private SessionToken(string value)
    {
        Value = value;
        _expected = Encoding.UTF8.GetBytes(value);
    }

    /// <summary>令牌明文（只允许写入受限交接文件，不得写日志）。</summary>
    public string Value { get; }

    /// <summary>生成 32 字节随机令牌，Base64Url 编码（43 字符，无填充）。</summary>
    /// <returns>新的令牌实例。</returns>
    public static SessionToken Create()
    {
        Span<byte> buffer = stackalloc byte[32];
        RandomNumberGenerator.Fill(buffer);
        var text = Convert.ToBase64String(buffer).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return new SessionToken(text);
    }

    /// <summary>以固定时间比较校验候选令牌，避免计时侧信道。</summary>
    /// <param name="candidate">请求头中原样取出的值。</param>
    /// <returns>完全一致时为 <c>true</c>。</returns>
    public bool Matches(string? candidate)
    {
        if (string.IsNullOrEmpty(candidate))
        {
            return false;
        }

        var provided = Encoding.UTF8.GetBytes(candidate);
        return CryptographicOperations.FixedTimeEquals(provided, _expected);
    }
}
