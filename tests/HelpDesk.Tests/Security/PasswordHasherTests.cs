using HelpDesk.Infrastructure.Security;

namespace HelpDesk.Tests.Security;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_ThenVerify_ReturnsTrue_ForCorrectPassword()
    {
        var hash = _hasher.Hash("Passw0rd!");

        Assert.True(_hasher.Verify("Passw0rd!", hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForWrongPassword()
    {
        var hash = _hasher.Hash("Passw0rd!");

        Assert.False(_hasher.Verify("wrong-password", hash));
    }

    [Fact]
    public void Hash_ProducesDifferentOutput_ForSamePassword()
    {
        var first = _hasher.Hash("Passw0rd!");
        var second = _hasher.Hash("Passw0rd!");

        Assert.NotEqual(first, second);
    }
}
