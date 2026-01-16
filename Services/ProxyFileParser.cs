using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using ProxyChecker.Common;
using ProxyChecker.Models;

namespace ProxyChecker.Services;

public static partial class ProxyFileParser
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
        // 优先使用自定义格式解析
        var parts = line.Split(separator);

        // 尝试直接映射
        try
        {
            if (setting.IpIndex < parts.Length)
            {
                var content = parts[setting.IpIndex].Trim();

                // 智能检测：如果提取出的内容本身像是一个完整的代理字符串（包含逗号或冒号）
                // 并且端口解析失败(或者端口索引指向的内容无效)，则尝试递归解析该内容
                bool tryRecursive = false;
                // 复原检测逻辑：内容含逗号/冒号，或者明确匹配了递归分隔符
                if (content.Contains(',') || content.Contains(':') || (!string.IsNullOrEmpty(setting.RecursiveSeparator) && content.Contains(setting.RecursiveSeparator)))
                {
                    // 如果用户指定的端口无效，或者端口索引和IP索引相同（说明用户想从这同一列提取所有信息）
                    if (setting.PortIndex >= parts.Length || setting.PortIndex == setting.IpIndex)
                    {
                        tryRecursive = true;
                    }
                    else
                    {
                        // 尝试解析端口，如果失败也进入递归
                        var pStr = parts[setting.PortIndex].Trim();
                        if (!int.TryParse(pStr, out _))
                        {
                            tryRecursive = true;
                        }
                    }
                }

                if (tryRecursive)
                {
                    // 1. 如果配置了递归分隔符，优先使用自定义分隔符解析
                    if (!string.IsNullOrEmpty(setting.RecursiveSeparator))
                    {
                        var subParts = content.Split(setting.RecursiveSeparator);
                        if (subParts.Length >= 2)
                        {
                            var subIp = GetPart(subParts, 0);
                            var subPortStr = GetPart(subParts, 1);

                            if (!string.IsNullOrEmpty(subIp) && int.TryParse(subPortStr, out var subPort))
                            {
                                // 提取用户名
                                string? subUser;
                                if (setting.UsernameIndex == setting.IpIndex) // 设定索引相同，认为在同一列内部
                                    subUser = GetPart(subParts, 2);
                                else
                                    subUser = GetPart(parts, setting.UsernameIndex); // 否则去外部列找

                                // 提取密码
                                string? subPass;
                                if (setting.PasswordIndex == setting.IpIndex) // 同上
                                    subPass = GetPart(subParts, 3);
                                else
                                    subPass = GetPart(parts, setting.PasswordIndex);

                                return new ProxyInfo(
                                    Index: defaultIndex,
                                    Ip: subIp,
                                    Port: subPort,
                                    Username: subUser ?? "",
                                    Password: subPass ?? ""
                                );
                            }
                        }
                    }

                    // 2. 否则（或者自定义解析失败），尝试标准格式 (Regex)
                    var subProxy = ParseStandardFormats(content, defaultIndex);
                    if (subProxy != null) return subProxy;
                }
            }


            var ip = GetPart(parts, setting.IpIndex);
            var portStr = GetPart(parts, setting.PortIndex);

            // 如果只有 ip 和 port，user/pass 允许为空
            if (!string.IsNullOrEmpty(ip) && int.TryParse(portStr, out var port))
            {
                // 使用正则提取/验证 IP，防止非法字符
                var ipMatch = IpRegex().Match(ip);
                if (ipMatch.Success)
                {
                    ip = ipMatch.Value;
                }
                else
                {
                    // IP 格式不正确，跳过
                    return null;
                }

                var username = GetPart(parts, setting.UsernameIndex);
                var password = GetPart(parts, setting.PasswordIndex);

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
            // 解析失败，继续尝试标准格式
        }

        // 如果自定义解析失败，尝试标准格式
        return ParseStandardFormats(line, defaultIndex);
    }

    private static ProxyInfo? ParseStandardFormats(string line, int defaultIndex)
    {
        // 备用格式1: 序号→IP,端口,用户名,ASN,运营商
        var match1 = IndexArrowIpPortUserRegex().Match(line);
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
        var match2 = IpPortUserPassRegex().Match(line);
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
        var match3 = IpPortUserAsnRegex().Match(line);
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
        var match4 = IpPortUserRegex().Match(line);
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
    [GeneratedRegex(@"^([^,]+),(\d+),([^,]+),([^,]+)$")]
    private static partial Regex IpPortUserPassRegex();

    [GeneratedRegex(@"^\s*(\d+)→([^,]+),(\d+),([^,]+)")]
    private static partial Regex IndexArrowIpPortUserRegex();

    [GeneratedRegex(@"^([^,]+),(\d+),([^,]+),AS\d+")]
    private static partial Regex IpPortUserAsnRegex();

    [GeneratedRegex(@"^([^,]+),(\d+),([^,]+)$")]
    private static partial Regex IpPortUserRegex();

    [GeneratedRegex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b")]
    private static partial Regex IpRegex();
}
