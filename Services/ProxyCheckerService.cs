using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ProxyChecker.Common;
using ProxyChecker.Models;

namespace ProxyChecker.Services;

public class ProxyCheckerService(int timeoutSeconds = 10, int retryCount = 1, int retryDelayMs = 200)
{
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(timeoutSeconds);
    private readonly int _retryCount = Math.Max(0, retryCount);
    private readonly TimeSpan _retryDelay = TimeSpan.FromMilliseconds(Math.Max(0, retryDelayMs));

    public async Task<CheckResult> CheckProxyAsync(
        ProxyInfo proxy,
        CancellationToken cancellationToken = default)
    {
        var setting = GlobalSetting.Instance.Setting;
        var (proxyUri, credentials) = BuildProxyConfiguration(proxy, setting.ProxyType);
        var webProxy = new WebProxy(proxyUri)
        {
            UseDefaultCredentials = false,
            Credentials = credentials
        };

        var handler = new SocketsHttpHandler
        {
            Proxy = webProxy,
            UseProxy = true,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        };

        var effectiveTimeout = setting.Timeout > 0
            ? TimeSpan.FromSeconds(setting.Timeout)
            : _timeout;

        using var client = new HttpClient(handler);
        client.Timeout = effectiveTimeout;

        var stopwatch = Stopwatch.StartNew();
        string? lastError = null;

        // 尝试多个 API
        var apis = new (string Name, Func<HttpClient, CancellationToken, Task<IpInfo?>> Query)[]
        {
            ("ip-api.com", QueryIpApiAsync),
            ("ipinfo.io", QueryIpInfoAsync),
            ("ipwho.is", QueryIpWhoAsync)
        };

        foreach (var (name, query) in apis)
        {
            var fatalProxyError = false;

            for (var attempt = 0; attempt <= _retryCount; attempt++)
            {
                try
                {
                    var ipInfo = await query(client, cancellationToken);
                    if (ipInfo != null && !string.IsNullOrEmpty(ipInfo.Ip))
                    {
                        stopwatch.Stop();
                        var isp = IspIdentifier.Identify(ipInfo.Org, ipInfo.Isp, ipInfo.Asn);

                        var locationParts = new[] { ipInfo.Country, ipInfo.Region, ipInfo.City }
                            .Where(s => !string.IsNullOrEmpty(s))
                            .Distinct();
                        var location = string.Join(" ", locationParts);

                        var result = new CheckResult(proxy)
                        {
                            RealIp = ipInfo.Ip,
                            Isp = isp,
                            Location = location,
                            Success = true,
                            ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                            State = CheckState.Success
                        };

                        return result;
                    }

                    break;
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("proxy"))
                {
                    lastError = "代理连接失败";
                    if (attempt < _retryCount)
                    {
                        await DelayBeforeRetryAsync(attempt, cancellationToken);
                        continue;
                    }

                    fatalProxyError = true;
                    break; // 代理本身有问题，不用再尝试其他 API
                }
                catch (TaskCanceledException)
                {
                    lastError = $"{name}超时";
                    if (attempt < _retryCount)
                    {
                        await DelayBeforeRetryAsync(attempt, cancellationToken);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message.Length > 50 ? ex.Message[..50] : ex.Message;
                    if (attempt < _retryCount)
                    {
                        await DelayBeforeRetryAsync(attempt, cancellationToken);
                        continue;
                    }
                }
            }

            if (fatalProxyError)
            {
                break;
            }
        }

        stopwatch.Stop();
        return new CheckResult(proxy)
        {
            RealIp = null,
            Isp = "未知",
            Success = false,
            ResponseTimeMs = stopwatch.ElapsedMilliseconds,
            State = CheckState.Failed,
            Error = lastError ?? "所有API均失败"
        };
    }

    private static (Uri ProxyUri, ICredentials? Credentials) BuildProxyConfiguration(ProxyInfo proxy, int proxyType)
    {
        var scheme = proxyType switch
        {
            1 => "socks4",
            2 => "socks5",
            _ => "http"
        };

        // SOCKS4 协议本身不支持认证
        if (proxyType == 1 && !string.IsNullOrEmpty(proxy.Username))
        {
            // SOCKS4 不支持认证，忽略用户名密码
            return (new Uri($"{scheme}://{proxy.Ip}:{proxy.Port}"), null);
        }

        // HTTP 或 SOCKS5 代理，认证通过 Credentials 属性设置
        var uri = new Uri($"{scheme}://{proxy.Ip}:{proxy.Port}");
        if (!string.IsNullOrEmpty(proxy.Username))
        {
            var credentials = string.IsNullOrEmpty(proxy.Password)
                ? new NetworkCredential(proxy.Username, "")
                : new NetworkCredential(proxy.Username, proxy.Password);
            return (uri, credentials);
        }

        return (uri, null);
    }

    private Task DelayBeforeRetryAsync(int attempt, CancellationToken ct)
    {
        if (_retryDelay == TimeSpan.Zero)
            return Task.CompletedTask;

        var delay = TimeSpan.FromMilliseconds(_retryDelay.TotalMilliseconds * (attempt + 1));
        return Task.Delay(delay, ct);
    }

    private async Task<IpInfo?> QueryIpApiAsync(HttpClient client, CancellationToken ct)
    {
        var response = await client.GetAsync(
            "http://ip-api.com/json/?lang=zh-CN&fields=status,message,query,country,regionName,city,isp,org,as",
            ct);

        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.GetProperty("status").GetString() != "success") return null;

        return new IpInfo
        {
            Ip = root.TryGetProperty("query", out var q) ? q.GetString() : null,
            Country = root.TryGetProperty("country", out var country) ? country.GetString() : null,
            Region = root.TryGetProperty("regionName", out var region) ? region.GetString() : null,
            City = root.TryGetProperty("city", out var city) ? city.GetString() : null,
            Isp = root.TryGetProperty("isp", out var i) ? i.GetString() : null,
            Org = root.TryGetProperty("org", out var o) ? o.GetString() : null,
            Asn = root.TryGetProperty("as", out var a) ? a.GetString() : null
        };
    }

    private async Task<IpInfo?> QueryIpInfoAsync(HttpClient client, CancellationToken ct)
    {
        var response = await client.GetAsync("https://ipinfo.io/json", ct);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var org = root.TryGetProperty("org", out var o) ? o.GetString() : null;
        return new IpInfo
        {
            Ip = root.TryGetProperty("ip", out var ip) ? ip.GetString() : null,
            Country = root.TryGetProperty("country", out var country) ? country.GetString() : null,
            Region = root.TryGetProperty("region", out var region) ? region.GetString() : null,
            City = root.TryGetProperty("city", out var city) ? city.GetString() : null,
            Isp = org,
            Org = org,
            Asn = org?.Split(' ').FirstOrDefault()
        };
    }

    private async Task<IpInfo?> QueryIpWhoAsync(HttpClient client, CancellationToken ct)
    {
        var response = await client.GetAsync("https://ipwho.is/", ct);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("success", out var success) || !success.GetBoolean())
            return null;

        var conn = root.TryGetProperty("connection", out var c) ? c : default;

        return new IpInfo
        {
            Ip = root.TryGetProperty("ip", out var ip) ? ip.GetString() : null,
            Country = root.TryGetProperty("country", out var country) ? country.GetString() : null,
            Region = root.TryGetProperty("region", out var region) ? region.GetString() : null,
            City = root.TryGetProperty("city", out var city) ? city.GetString() : null,
            Isp = conn.ValueKind != JsonValueKind.Undefined && conn.TryGetProperty("isp", out var isp)
                ? isp.GetString()
                : null,
            Org = conn.ValueKind != JsonValueKind.Undefined && conn.TryGetProperty("org", out var org)
                ? org.GetString()
                : null,
            Asn = conn.ValueKind != JsonValueKind.Undefined && conn.TryGetProperty("asn", out var asn)
                ? $"AS{asn.GetInt32()}"
                : null
        };
    }

    private class IpInfo
    {
        public string? Ip { get; init; }
        public string? Country { get; init; }
        public string? Region { get; init; }
        public string? City { get; init; }
        public string? Isp { get; init; }
        public string? Org { get; init; }
        public string? Asn { get; init; }
    }
}