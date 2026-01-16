namespace ProxyChecker.Models;

public record ProxyInfo(
    int Index,
    string Ip,
    int Port,
    string Username,
    string Password = "",
    string OriginalLine = ""
)
{
    public string Address => $"{Ip}:{Port}";
}
