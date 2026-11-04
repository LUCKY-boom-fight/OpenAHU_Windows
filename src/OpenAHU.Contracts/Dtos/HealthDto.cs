namespace OpenAHU.Contracts.Dtos;

/// <summary>
/// 服务与数据库就绪状态（<c>GET /api/v1/health</c>）。
/// 严禁包含数据库绝对路径、连接串或进程信息。
/// </summary>
/// <remarks>
/// <b>状态：待跨组评审。</b> 冻结文档只要求“服务/DB 就绪状态，不能泄露数据库绝对路径”，未定义响应体；
/// 本类型由 3 号提出，评审通过后移入冻结清单（并同步更新 <c>00_所有AI必须遵守_统一上下文.md</c>）。
/// </remarks>
/// <param name="Status">就绪阶段：<c>starting</c>（已监听、数据层未就绪）、<c>ok</c>（全部就绪）、<c>degraded</c>（可服务但部分能力降级）。</param>
/// <param name="DatabaseReady">SQLite 是否可用（迁移已应用且能打开）。</param>
/// <param name="Version">API 版本号，仅供排障使用，不含构建路径。</param>
public sealed record HealthDto(
    string Status,
    bool DatabaseReady,
    string Version);
