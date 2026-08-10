using Xunit;
using LPR381Project.Common.Errors;

namespace LPR381Project.Tests.ErrorHandling
{
    public class ResultTests
    {
        [Fact]
        public void SuccessResultHasValueAndIsSuccess()
        {
            var r = Result<int>.Success(42);
            Assert.True(r.IsSuccess);
            Assert.Equal(42, r.Value);
            Assert.Null(r.Error);
        }

        [Fact]
        public void FailureResultHasErrorAndIsNotSuccess()
        {
            var ex = new InputValidationException("bad input");
            var r = Result<int>.Failure(ex);
            Assert.False(r.IsSuccess);
            Assert.Null(r.Value);
            Assert.Equal(ex, r.Error);
        }
    }
}
