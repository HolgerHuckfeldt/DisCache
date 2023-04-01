using DisCache.Model;
using DisCache.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Stamon.Services;

namespace DisCache.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CacheController : ControllerBase
    {
        private readonly IDistributedCache _service;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CacheController> _logger;
        private readonly StamonService _stamonSlotService;
        private readonly KeyService _keyService;

        public CacheController(
            IDistributedCache memoryService,
            IMemoryCache memoryCache,
            ILogger<CacheController> logger,
            KeyService keyService,
            StamonService stamonSlotService)
        {
            _service = memoryService;
            _memoryCache = memoryCache;
            _logger = logger;
            _keyService = keyService;
            _stamonSlotService = stamonSlotService;
        }

        /// <summary>
        /// Ping the service
        /// </summary>
        /// <returns>Returns a pong.</returns>
        [HttpGet]
        //[CustomAuthorize]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Route("Ping")]
        public async Task<ActionResult> Ping()
        {
            await _stamonSlotService.StatisticEvent("Request");

            return Ok("Pong");
        }

        /// <summary>
        /// Get a cached item by Id
        /// </summary>
        /// <param name="key">key of the item to be fetched</param>
        /// <returns></returns>
        [HttpGet]
        //[CustomAuthorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("{key}")]
        public async Task<ActionResult> Get(string key)
        {
            await _stamonSlotService.StatisticEvent("Request");

            if (string.IsNullOrEmpty(key))
            {
                return StatusCode(StatusCodes.Status400BadRequest, "Key must not be empty");
            }

            try
            {
                if (_keyService.HasAction(key) == KeyAction.Ignore)
                {
                    await _stamonSlotService.StatisticEvent("Miss");
                    return NotFound();
                }


                var result = await _service.GetAsync(key);
                if (result == null)
                {
                    await _stamonSlotService.StatisticEvent("Miss");
                    return NotFound();
                }
                await _stamonSlotService.StatisticEvent("Hit");
                return File(new MemoryStream(result), "application/octet-stream");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
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
        //[Authorize]
        [Route("{key}")]
        public async Task<ActionResult> Refresh(string key)
        {
            await _stamonSlotService.StatisticEvent("Request");
            if (string.IsNullOrEmpty(key))
            {
                return StatusCode(StatusCodes.Status400BadRequest, "Key must not be empty");
            }

            try
            {
                await _service.RefreshAsync(key);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
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
        //[Authorize]
        [Route("{key}")]
        public async Task<ActionResult> Remove(string key)
        {
            await _stamonSlotService.StatisticEvent("Request");
            try
            {
                if (string.IsNullOrEmpty(key))
                {
                    return StatusCode(StatusCodes.Status400BadRequest, "Key must not be empty");
                }

                await _stamonSlotService.StatisticEvent("Delete");
                await _service.RemoveAsync(key);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
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
        //[Authorize]
        public async Task<ActionResult> Set()
        {
            await _stamonSlotService.StatisticEvent("Request");
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
                    await _stamonSlotService.StatisticEvent("Insert");

                    _logger.LogInformation("Insert key {FileFileName}", file.FileName);

                    if (Configuration.UseMemoryCacheAccelerator)
                    {
                        _memoryCache.Set(file.FileName, buff,
                            new MemoryCacheEntryOptions()
                            {
                                AbsoluteExpiration = options.AbsoluteExpiration,
                                SlidingExpiration = TimeSpan.FromSeconds(Configuration.UseMemoryCacheAcceleratorTime)
                            });
                    }

                    await _service.SetAsync(file.FileName, buff, options);
                }
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
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
        //[Authorize]
        public async Task<ActionResult> SetString([FromBody] CacheRequest data)
        {
            await _stamonSlotService.StatisticEvent("Request");
            try
            {
                var options = new DistributedCacheEntryOptions();
                options.SetAbsoluteExpiration(TimeSpan.FromSeconds(data.Duration));
                await _stamonSlotService.StatisticEvent("Insert");

                var binaryData = Encoding.ASCII.GetBytes(data.Value);

                if (Configuration.UseMemoryCacheAccelerator)
                {
                    _memoryCache.Set(data.Key, binaryData,
                        new MemoryCacheEntryOptions()
                        {
                            AbsoluteExpiration = options.AbsoluteExpiration,
                            SlidingExpiration = TimeSpan.FromSeconds(Configuration.UseMemoryCacheAcceleratorTime)
                        });
                }

                await _service.SetAsync(data.Key, binaryData, options);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
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
        //[Authorize]
        public async Task<ActionResult> GetString(string key)
        {
            await _stamonSlotService.StatisticEvent("Request");
            try
            {
                //var options = new DistributedCacheEntryOptions();

                string result = null;

                if (Configuration.UseMemoryCacheAccelerator)
                {
                    // Wenn der InMemory-Accelerator aktiviert ist, erst mal darein schauen.
                    if (_memoryCache.Get(key) is byte[] binaryResult)
                    {
                        result = Encoding.UTF8.GetString(binaryResult, 0, binaryResult.Length);
                    }
                }

                result ??= await _service.GetStringAsync(key);

                if (result != null)
                {
                    await _stamonSlotService.StatisticEvent("Hit");
                    return Ok(result);
                }
                await _stamonSlotService.StatisticEvent("Miss");
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
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
