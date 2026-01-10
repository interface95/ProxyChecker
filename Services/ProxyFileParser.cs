using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using ProxyChecker.Common;
using ProxyChecker.Models;

namespace ProxyChecker.Services;

public static class ProxyFileParser
{
    public static List<ProxyInfo> Parse(string filePath)
    {
        var proxies = new List<ProxyInfo>();
        var lines = File.ReadAllLines(filePath);
        var index = 0;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line)) continue;

            index++;
            var proxy = ParseLine(line, index);
            if (proxy != null)
            {
                proxies.Add(proxy);
            }
        }

        return proxies;
    }

    public static List<ProxyInfo> ParseFromContent(string content)
    {
        var proxies = new List<ProxyInfo>();
        var lines = content.Split('\n', '\r');
        var index = 0;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line)) continue;

            index++;
            var proxy = ParseLine(line, index);
            if (proxy != null)
            {
                proxies.Add(proxy);
            }
        }

        return proxies;
    }

    private static ProxyInfo? ParseLine(string line, int defaultIndex)
    {
        var setting = GlobalSetting.Instance.Setting;
        var separator = string.IsNullOrEmpty(setting.Separator) ? "," : setting.Separator;

        // 优先使用自定义格式解析
        var parts = line.Split(separator);
        if (parts.Length >= 4)
        {
            try
            {
                var ip = GetPart(parts, setting.IpIndex);
                var portStr = GetPart(parts, setting.PortIndex);
                var username = GetPart(parts, setting.UsernameIndex);
                var password = GetPart(parts, setting.PasswordIndex);

                if (!string.IsNullOrEmpty(ip) && int.TryParse(portStr, out var port))
                {
                    return new ProxyInfo(
                        Index: defaultIndex,
                        Ip: ip,
                        Port: port,
                        Username: username ?? "",
                        Password: password ?? ""
                    );
                }
            }
            catch
            {
                // 解析失败，尝试其他格式
            }
        }

        // 备用格式1: 序号→IP,端口,用户名,ASN,运营商
        var match1 = Regex.Match(line, @"^\s*(\d+)→([^,]+),(\d+),([^,]+)");
        if (match1.Success)
        {
            return new ProxyInfo(
                Index: int.Parse(match1.Groups[1].Value),
                Ip: match1.Groups[2].Value,
                Port: int.Parse(match1.Groups[3].Value),
                Username: match1.Groups[4].Value
            );
        }

        // 备用格式2: IP,端口,用户名,密码
        var match2 = Regex.Match(line, @"^([^,]+),(\d+),([^,]+),([^,]+)$");
        if (match2.Success && !match2.Groups[4].Value.StartsWith("AS"))
        {
            return new ProxyInfo(
                Index: defaultIndex,
                Ip: match2.Groups[1].Value,
                Port: int.Parse(match2.Groups[2].Value),
                Username: match2.Groups[3].Value,
                Password: match2.Groups[4].Value
            );
        }

        // 备用格式3: IP,端口,用户名,ASN,运营商
        var match3 = Regex.Match(line, @"^([^,]+),(\d+),([^,]+),AS\d+");
        if (match3.Success)
        {
            return new ProxyInfo(
                Index: defaultIndex,
                Ip: match3.Groups[1].Value,
                Port: int.Parse(match3.Groups[2].Value),
                Username: match3.Groups[3].Value
            );
        }

        // 备用格式4: IP,端口,用户名
        var match4 = Regex.Match(line, @"^([^,]+),(\d+),([^,]+)$");
        if (match4.Success)
        {
            return new ProxyInfo(
                Index: defaultIndex,
                Ip: match4.Groups[1].Value,
                Port: int.Parse(match4.Groups[2].Value),
                Username: match4.Groups[3].Value
            );
        }

        return null;
    }

    private static string? GetPart(string[] parts, int index) =>
        index >= 0 && index < parts.Length ? parts[index].Trim() : null;
}
