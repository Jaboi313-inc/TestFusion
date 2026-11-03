using System.Diagnostics;
using Microsoft.Extensions.Options;
using TestFusion.Services.Models;

namespace TestFusion.Web.Middleware;

public class RequestLoggingMiddleware {
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly LoggingSettings _loggingSettings;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger, IOptions<LoggingSettings> loggingSettings) {
        _next = next;
        _logger = logger;
        _loggingSettings = loggingSettings.Value;
    }

    public async Task InvokeAsync(HttpContext context) {
        var controller = context.Request.RouteValues["controller"]?.ToString();
        var action = context.Request.RouteValues["action"]?.ToString();

        if (string.IsNullOrWhiteSpace(controller) || string.IsNullOrWhiteSpace(action)) {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        try {
            await _next(context);
            stopwatch.Stop();
            var durationMs = stopwatch.ElapsedMilliseconds;
            var isSlow = durationMs >= _loggingSettings.SlowOperationThresholds.RequestMs;
            using var scope = _logger.BeginScope(new Dictionary<string, object> { ["DurationMs"] = durationMs });

            if (HttpMethods.IsGet(context.Request.Method) && action.Equals("Index", StringComparison.OrdinalIgnoreCase)) {
                if (isSlow) _logger.LogWarning("Opened {Controller}/{Action}, but the request was slower than expected", controller, action);
                else _logger.LogInformation("Opened {Controller}/{Action}", controller, action);
            }
            else if (isSlow) {
                _logger.LogWarning("{Method} {Controller}/{Action} completed slowly with status {StatusCode}", context.Request.Method, controller, action, context.Response.StatusCode);
            }
        }
        catch (Exception ex) {
            stopwatch.Stop();
            using var scope = _logger.BeginScope(new Dictionary<string, object> { ["DurationMs"] = stopwatch.ElapsedMilliseconds });
            _logger.LogError(ex, "{Method} {Controller}/{Action} failed", context.Request.Method, controller, action);
            throw;
        }
    }
}
