using Microsoft.Extensions.Logging;
using Shared_CS;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Server_CS
{
    public class MyCalculator : ICalculator
    {
        [Authorize]
        public ValueTask<MultiplyResult> MultiplyAsync(MultiplyRequest request)
        {
            var result = new MultiplyResult { Result = request.X * request.Y };
            return new ValueTask<MultiplyResult>(result);
        }
    }
}
