using Security.Application.Common.Models;

namespace Security.Application.Tests.Common;

public class PaginationTests
{
    [Fact]
    public void PaginatedList_Create_Returns_Correct_Page()
    {
        var source = Enumerable.Range(1, 50);

        var page = PaginatedList<int>.Create(source, pageNumber: 2, pageSize: 10);

        Assert.Equal(2, page.PageNumber);
        Assert.Equal(50, page.TotalCount);
        Assert.Equal(5, page.TotalPages);
        Assert.True(page.HasPreviousPage);
        Assert.True(page.HasNextPage);
        Assert.Equal(10, page.Items.Count);
        Assert.Equal(11, page.Items[0]);
    }

    [Fact]
    public void PaginatedRequest_PageSize_Capped_At_100()
    {
        var req = new PaginatedRequest { PageSize = 500 };

        Assert.Equal(100, req.PageSize);
    }
}
