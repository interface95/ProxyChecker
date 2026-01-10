using System.Collections.Generic;
using System.Linq;

namespace ProxyChecker.Services;

public static class IspIdentifier
{
    private static readonly HashSet<string> MobileKeywords =
    [
        "china mobile", "chinamobile", "cmcc", "移动",
        "as9808", "as56041", "as56040", "as56042", "as56044", "as56046", "as56048",
        "as9231", "as24400", "as24547", "as58453"
    ];

    private static readonly HashSet<string> TelecomKeywords =
    [
        "china telecom", "chinanet", "电信", "ctg",
        "as4134", "as4812", "as4809", "as23724", "as134773", "as134774",
        "as17638", "as136167", "as136190", "as136195"
    ];

    private static readonly HashSet<string> UnicomKeywords =
    [
        "china unicom", "chinaunicom", "cu-", "联通", "cncgroup",
        "as4837", "as17621", "as17623", "as9929", "as10099", "as17816"
    ];

    public static string Identify(string? org, string? isp, string? asn)
    {
        var combined = $"{org ?? ""} {isp ?? ""} {asn ?? ""}".ToLowerInvariant();

        if (MobileKeywords.Any(k => combined.Contains(k)))
            return "移动";

        if (TelecomKeywords.Any(k => combined.Contains(k)))
            return "电信";

        if (UnicomKeywords.Any(k => combined.Contains(k)))
            return "联通";

        // 云服务商
        if (combined.Contains("alibaba") || combined.Contains("aliyun") || combined.Contains("alicloud"))
            return "阿里云";
        if (combined.Contains("tencent") || combined.Contains("qcloud"))
            return "腾讯云";
        if (combined.Contains("huawei") || combined.Contains("hwcloud"))
            return "华为云";
        if (combined.Contains("amazon") || combined.Contains("aws") || combined.Contains("ec2"))
            return "AWS";
        if (combined.Contains("microsoft") || combined.Contains("azure"))
            return "Azure";
        if (combined.Contains("google") || combined.Contains("gcp"))
            return "GCP";
        if (combined.Contains("cloudflare") || combined.Contains("as13335"))
            return "Cloudflare";

        // 返回原始信息
        if (!string.IsNullOrEmpty(isp)) return isp;
        if (!string.IsNullOrEmpty(org)) return org;
        if (!string.IsNullOrEmpty(asn)) return $"ASN:{asn}";

        return "其他";
    }

    public static string GetIspGroup(string isp)
    {
        return isp switch
        {
            "移动" => "移动",
            "电信" => "电信",
            "联通" => "联通",
            _ => "其他"
        };
    }
}
