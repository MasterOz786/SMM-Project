using Xunit;

namespace SMM.Core.Tests;

public class TicketCodeTests
{
    [Fact]
    public void New_Returns32CharHexString()
    {
        var t = TicketCode.New();
        Assert.Equal(32, t.Length);
        Assert.Matches("^[0-9a-f]{32}$", t);
    }

    [Fact]
    public void New_TwoCalls_AreUniqueWithHighProbability()
    {
        Assert.NotEqual(TicketCode.New(), TicketCode.New());
    }
}
