using System.Globalization;
using System.Text;
using Solhigson.Utilities.Security;

namespace Solhigson.Utilities.Extensions;

public static class CryptoExtensions
{
    #region Crypto

    private const string HexAlphabetLower = "0123456789abcdef";
    private const string HexAlphabetUpper = "0123456789ABCDEF";
    public static string Hex(this byte[] bytes, bool useUpper = false)
    {
        var result = new StringBuilder(bytes.Length);
        var hexAlphabet = useUpper ? HexAlphabetUpper : HexAlphabetLower;

        foreach (var b in bytes)
        {
            result.Append(hexAlphabet[b >> 4]);
            result.Append(hexAlphabet[b & 0xF]);
        }

        return result.ToString();
    }

    public static byte[] FromHexString(this string hexString)
    {
        if (hexString.Length % 2 != 0)
        {
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "The binary key cannot have an odd number of digits: {0}", hexString));
        }

        var hexAsBytes = new byte[hexString.Length / 2];
        for (var index = 0; index < hexAsBytes.Length; index++)
        {
            var byteValue = hexString.Substring(index * 2, 2);
            hexAsBytes[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        return hexAsBytes;
    }

    public static string Base64(this byte[] data)
    {
        return Convert.ToBase64String(data);
    }
        
    public static byte[] Base64(this string data)
    {
        return Convert.FromBase64String(data);
    }
    
    public static string Md5(this string s)
    {
        return CryptoHelper.HashData(s, HashAlgorithmType.Md5);
    }
        
    public static string Sha256(this string s)
    {
        return CryptoHelper.HashData(s, HashAlgorithmType.Sha256);
    }
        
    public static string Sha512(this string s)
    {
        return CryptoHelper.HashData(s, HashAlgorithmType.Sha512);
    }
    
    public static string Hash(this string s, HashAlgorithmType hashAlgorithmType)
    {
        return CryptoHelper.HashData(s, hashAlgorithmType);
    }

    #endregion
}