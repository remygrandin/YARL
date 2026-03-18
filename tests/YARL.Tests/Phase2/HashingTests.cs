using YARL.Infrastructure.Scanning;

namespace YARL.Tests.Phase2;

[Trait("Category", "Phase2")]
public class HashingTests
{
    private static string CreateTempFileWithContent(string content)
    {
        var path = Path.GetTempFileName();
        File.WriteAllBytes(path, System.Text.Encoding.UTF8.GetBytes(content));
        return path;
    }

    [Fact]
    public async Task CRC32_matches_known_reference_value()
    {
        var path = CreateTempFileWithContent("Hello, World!");
        try
        {
            var (crc32, _, _) = await FileHasher.ComputeHashesAsync(path);
            // CRC32 of "Hello, World!" (UTF-8) = EC4AC3D0
            Assert.Equal("EC4AC3D0", crc32, ignoreCase: true);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task MD5_matches_known_reference_value()
    {
        var path = CreateTempFileWithContent("Hello, World!");
        try
        {
            var (_, md5, _) = await FileHasher.ComputeHashesAsync(path);
            // MD5 of "Hello, World!" (UTF-8)
            Assert.Equal("65A8E27D8879283831B664BD8B7F0AD4", md5, ignoreCase: true);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task SHA1_matches_known_reference_value()
    {
        var path = CreateTempFileWithContent("Hello, World!");
        try
        {
            var (_, _, sha1) = await FileHasher.ComputeHashesAsync(path);
            // SHA1 of "Hello, World!" (UTF-8)
            Assert.Equal("0A0A9F2A6772942557AB5355D76AF442F8F65E01", sha1, ignoreCase: true);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task All_three_hashes_computed_from_single_file_read()
    {
        var path = CreateTempFileWithContent("Hello, World!");
        try
        {
            var (crc32, md5, sha1) = await FileHasher.ComputeHashesAsync(path);
            Assert.NotNull(crc32);
            Assert.NotEmpty(crc32);
            Assert.NotNull(md5);
            Assert.NotEmpty(md5);
            Assert.NotNull(sha1);
            Assert.NotEmpty(sha1);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
