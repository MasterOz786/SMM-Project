using SMM.Core.Security;
using Xunit;

namespace SMM.Core.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_ReturnsNonEmpty_v1FormattedString()
    {
        var h = PasswordHasher.Hash("any-password");
        Assert.False(string.IsNullOrWhiteSpace(h));
        Assert.StartsWith("v1:100000:", h);
        var parts = h.Split(':');
        Assert.Equal(4, parts.Length);
    }

    [Theory]
    [InlineData("Admin123")]
    [InlineData("short")]
    [InlineData("Unicode-测试-🔐")]
    public void Verify_AfterHash_ReturnsTrue(string password)
    {
        var stored = PasswordHasher.Hash(password);
        Assert.True(PasswordHasher.Verify(password, stored));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var stored = PasswordHasher.Hash("correct");
        Assert.False(PasswordHasher.Verify("wrong", stored));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-four-parts")]
    [InlineData("v1:100000:onlythree")]
    [InlineData("v2:100000:YQ==:YQ==")]
    public void Verify_MalformedStored_ReturnsFalse(string stored)
    {
        Assert.False(PasswordHasher.Verify("password", stored));
    }

    [Fact]
    public void Verify_InvalidBase64InStored_ReturnsFalse()
    {
        Assert.False(PasswordHasher.Verify("x", "v1:100000:not-base64!!!:also-not!!!"));
    }
}
