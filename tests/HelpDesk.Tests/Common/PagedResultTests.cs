using HelpDesk.Application.Common;

namespace HelpDesk.Tests.Common;

public class PagedResultTests
{
    [Fact]
    public void TotalPages_RoundsUp()
    {
        var result = new PagedResult<int>(new[] { 1, 2, 3 }, page: 1, pageSize: 2, totalCount: 5);

        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public void HasPrevious_And_HasNext_ReflectCurrentPage()
    {
        var middle = new PagedResult<int>(Array.Empty<int>(), page: 2, pageSize: 10, totalCount: 25);

        Assert.True(middle.HasPrevious);
        Assert.True(middle.HasNext);
    }

    [Fact]
    public void HasNext_IsFalse_OnLastPage()
    {
        var last = new PagedResult<int>(Array.Empty<int>(), page: 3, pageSize: 10, totalCount: 25);

        Assert.True(last.HasPrevious);
        Assert.False(last.HasNext);
    }
}
