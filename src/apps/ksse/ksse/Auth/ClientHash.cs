using System;
using System.Security.Cryptography;
using System.Text;

namespace ksse.Auth;

internal static class ClientHash
{
    public static string HashPassword(string password)
    {
        byte[] passwordBytes = Encoding.ASCII.GetBytes(password);
        byte[] hashedPasswordBytes = MD5.HashData(passwordBytes);
        string hashedPassword = Convert.ToHexStringLower(hashedPasswordBytes);
        return hashedPassword;
    }
}
