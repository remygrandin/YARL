using System.IO.Hashing;
using System.Security.Cryptography;

namespace YARL.Infrastructure.Scanning;

public static class FileHasher
{
    public static async Task<(string Crc32, string Md5, string Sha1)> ComputeHashesAsync(
        string filePath, CancellationToken ct = default)
    {
        var crc32 = new Crc32();
        using var md5 = MD5.Create();
        using var sha1 = SHA1.Create();
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read,
            FileShare.Read, bufferSize: 81920, useAsync: true);

        var buffer = new byte[81920];
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
        {
            var span = buffer.AsSpan(0, bytesRead);
            crc32.Append(span);
            md5.TransformBlock(buffer, 0, bytesRead, null, 0);
            sha1.TransformBlock(buffer, 0, bytesRead, null, 0);
        }
        md5.TransformFinalBlock([], 0, 0);
        sha1.TransformFinalBlock([], 0, 0);

        return (
            crc32.GetCurrentHashAsUInt32().ToString("X8"),
            Convert.ToHexString(md5.Hash!),
            Convert.ToHexString(sha1.Hash!)
        );
    }
}
