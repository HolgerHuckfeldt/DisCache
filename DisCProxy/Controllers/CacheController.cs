using DisCProxy.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Net.Http.Headers;
using System.Text;
using System.Web;

namespace DisCProxy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CacheController : ControllerBase
    {
        private readonly IDistributedCache _service;
        private readonly ILogger<CacheController> _logger;

        public CacheController(
            IDistributedCache memoryService,
            ILogger<CacheController> logger)
        {
            _service = memoryService;
            _logger = logger;
        }

        /// <summary>
        /// Ping the service
        /// </summary>
        /// <returns>Returns a pong.</returns>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Route("Ping")]
        public Task<ActionResult> Ping()
        {
            return Task.FromResult(Ok("Pong") as ActionResult);
        }

        /// <summary>
        /// Get a cached item by Id 
        /// </summary>
        /// <param name="key">key of the item to be fetched</param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("{key}")]
        public async Task<ActionResult> Get(string key)
        {
            var result = await _service.GetAsync(key);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        /// <summary>
        /// Refrech cached item 
        /// </summary>
        /// <param name="key">key of the item to be refreshed</param>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPut]
        [Authorize]
        [Route("{key}")]
        public async Task<ActionResult> Refresh(string key)
        {
            try
            {
                await _service.RefreshAsync(key);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

        }

        /// <summary>
        /// Remove an item by key
        /// </summary>
        /// <param name="key">key of the item to be removed</param>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpDelete]
        [Authorize]
        [Route("{key}")]
        public async Task<ActionResult> Remove(string key)
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                {
                    return StatusCode(StatusCodes.Status400BadRequest, "Key must not be empty");
                }

                await _service.RemoveAsync(key);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Add an item to the cache
        /// </summary>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
        [HttpPost]
        [Authorize]
        public async Task<ActionResult> Set()
        {

            try
            {
                var request = HttpContext.Request;

                if (!request.HasFormContentType ||
                    !MediaTypeHeaderValue.TryParse(request.ContentType, out var mediaTypeHeader) ||
                    string.IsNullOrEmpty(mediaTypeHeader.Boundary.Value))
                {
                    return new UnsupportedMediaTypeResult();
                }

                var formData = Request.Form;
                var optionsString = formData.Keys.FirstOrDefault();
                var options = new DistributedCacheEntryOptions();

                if (optionsString != default)
                {
                    var cacheOptions =
                        System.Text.Json.JsonSerializer.Deserialize<CacheOptions>(HttpUtility.UrlDecode(optionsString));

                    if (cacheOptions != null)
                    {
                        options.AbsoluteExpiration = cacheOptions.AbsoluteExpiration.HasValue
                            ? new DateTimeOffset(cacheOptions.AbsoluteExpiration.Value)
                            : null;
                        options.AbsoluteExpirationRelativeToNow = cacheOptions.AbsoluteExpirationRelativeToNow.HasValue
                            ? TimeSpan.FromMilliseconds(cacheOptions.AbsoluteExpirationRelativeToNow.Value)
                            : null;
                        options.SlidingExpiration = cacheOptions.SlidingExpiration.HasValue
                            ? TimeSpan.FromMilliseconds(cacheOptions.SlidingExpiration.Value)
                            : null;
                    }
                }

                foreach (var file in formData.Files)
                {
                    if (string.IsNullOrEmpty(file.FileName))
                    {
                        return StatusCode(StatusCodes.Status400BadRequest, "Key must not be empty");
                    }
                    var buff = ReadFully(file.OpenReadStream());

                    _logger.LogInformation($"Insert key {file.FileName}");

                    await _service.SetAsync(file.FileName, buff, options);
                }
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Add an item to the cache
        /// </summary>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
        [HttpPost("String")]
        [Authorize]
        public async Task<ActionResult> SetString([FromBody] CacheRequest data)
        {
            try
            {
                var options = new DistributedCacheEntryOptions();

                await _service.SetAsync(data.Key, Encoding.ASCII.GetBytes(data.Value), options);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Add an item to the cache
        /// </summary>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
        [HttpGet("String/{key}")]
        [Authorize]
        public async Task<ActionResult> GetString(string key)
        {
            try
            {
                var options = new DistributedCacheEntryOptions();

                var result = await _service.GetStringAsync(key);

                if (result != null)
                    return Ok(result);
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error", ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Read a stream fully into a byte-array
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using MemoryStream ms = new MemoryStream();
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                ms.Write(buffer, 0, read);
            }
            return ms.ToArray();
        }
    }
}
