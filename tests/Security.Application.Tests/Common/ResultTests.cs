using Security.Application.Common.Models;

namespace Security.Application.Tests.Common;

public class ResultTests
{
    [Fact]
    public void Success_Result_Has_Succeeded_True()
    {
        var result = Result.Success();

        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Failure_Result_Has_Succeeded_False_And_Errors()
    {
        var result = Result.Failure("Error one", "Error two");

        Assert.False(result.Succeeded);
        Assert.Equal(2, result.Errors.Length);
    }

    [Fact]
    public void Typed_Success_Result_Carries_Value()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.Succeeded);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Typed_Failure_Result_Has_No_Value()
    {
        var result = Result<string>.Failure("Something went wrong");

        Assert.False(result.Succeeded);
        Assert.Null(result.Value);
        Assert.Single(result.Errors);
    }
}
