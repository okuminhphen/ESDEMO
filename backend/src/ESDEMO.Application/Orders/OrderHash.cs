using System.Security.Cryptography;
using System.Text;

namespace ESDEMO.Application.Orders;

internal static class OrderHash
{
    public static string Create(params string[] values) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("|", values))));
}
