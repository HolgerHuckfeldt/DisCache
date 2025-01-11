using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using WebApiDemo.Cache;
using WebApiDemo.Model;

namespace WebApiDemo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DiscController(CacheService cache) : ControllerBase
    {

        [HttpGet]
        [Route("{id}")]
        public async Task<ActionResult> Get( int id)
        {
            var fromCache = await cache.GetDocumentAsync(id);

            if (fromCache != null)
            {
                return Ok(fromCache);
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<ActionResult> Post([FromBody]TestClass c)
        {
            await cache.AddAsync(c);
            return Ok("Added to Cache");
        }

    }
}
