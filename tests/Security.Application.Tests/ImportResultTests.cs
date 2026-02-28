using Security.Application.Models;

namespace Security.Application.Tests;

public class ImportResultTests
{
    [Fact]
    public void ImportResult_DefaultValues_AreCorrect()
    {
        var result = new ImportResult();
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(0, result.ErrorCount);
        Assert.Empty(result.RowErrors);
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void ImportResult_HasErrors_WhenRowErrorsAdded()
    {
        var result = new ImportResult();
        result.RowErrors.Add(new RowError { RowNumber = 2, Field = "Email", Error = "Required" });
        result.ErrorCount = 1;
        Assert.True(result.HasErrors);
        Assert.Equal(1, result.RowErrors.Count);
    }

    [Fact]
    public void RowError_Properties_AreSettable()
    {
        var error = new RowError { RowNumber = 5, Field = "FirstName", Error = "First Name is required." };
        Assert.Equal(5, error.RowNumber);
        Assert.Equal("FirstName", error.Field);
        Assert.Equal("First Name is required.", error.Error);
    }

    [Fact]
    public void ImportResult_SuccessCount_CanBeSet()
    {
        var result = new ImportResult { SuccessCount = 10 };
        Assert.Equal(10, result.SuccessCount);
    }
}
