using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace Samples.AspNetMvc5.Controllers
{
    public class ApiController : System.Web.Http.ApiController
    {
        [HttpGet]
        [Route("api/delay/{seconds}")]
        public int Delay(int seconds)
        {
            Thread.Sleep(TimeSpan.FromSeconds(seconds));
            return seconds;
        }

        [HttpGet]
        [Route("api/delay-async/{seconds}")]
        public async Task<int> DelayAsync(int seconds)
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds)).ConfigureAwait(false);
            return seconds;
        }

        [HttpGet]
        [Route("api/environment")]
        public IDictionary GetEnvironmentVariables()
        {
            return System.Environment.GetEnvironmentVariables();
        }
    }
}
