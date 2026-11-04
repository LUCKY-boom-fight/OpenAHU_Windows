using OpenAHU.Api.Endpoints;
using OpenAHU.Api.Errors;
using OpenAHU.Api.Hosting;
using OpenAHU.Contracts.Json;

// 本地伴随进程：只监听回环地址，端口由系统随机分配（0），启动后把真实端口与令牌写入交接文件。
// 边界：此进程不直接访问 SQLite，数据库访问经 04 Application → 05 Infrastructure。
var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://127.0.0.1:0");

// 与 Desktop 共用同一份 JSON 约定，避免两侧字段名/大小写漂移。
var contractJson = ContractJson.CreateOptions();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = contractJson.PropertyNamingPolicy;
    options.SerializerOptions.PropertyNameCaseInsensitive = contractJson.PropertyNameCaseInsensitive;
    options.SerializerOptions.NumberHandling = contractJson.NumberHandling;
});

var sessionToken = SessionToken.Create();
builder.Services.AddSingleton(sessionToken);
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseMiddleware<SessionTokenMiddleware>();
app.UseExceptionHandler();
app.MapCampusEndpoints();

await app.StartAsync();

var handshakePath = SessionHandshake.DefaultPath;
var handshake = SessionHandshake.Create(app.Urls, sessionToken.Value, Environment.ProcessId);
SessionHandshake.Write(handshakePath, handshake);
app.Logger.LogInformation("OpenAHU 本地 API 已启动：{BaseUrl}（交接文件 {Path}）", handshake.BaseUrl, handshakePath);

try
{
    await app.WaitForShutdownAsync();
}
finally
{
    // 正常退出时清除交接文件；崩溃残留由下次启动覆盖，客户端以 pid 判断进程是否存活。
    SessionHandshake.Delete(handshakePath);
}
