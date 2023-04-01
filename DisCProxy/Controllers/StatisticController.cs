using DisCProxy.Model;
using DisCProxy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DisCProxy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticController : ControllerBase
    {
        private readonly StatisticsService _statisticService;

        public StatisticController(StatisticsService statisticsService)
        {
            _statisticService = statisticsService;
        }

        [HttpGet]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Route("hits")]
        public async Task<ActionResult> Hits()
        {
            return Ok(await _statisticService.GetHits());
        }

        [HttpGet]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Route("misses")]
        public async Task<ActionResult> Misses()
        {
            return Ok(await _statisticService.GetMiss());
        }

        [HttpGet]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Route("inserts")]
        public async Task<ActionResult> Inserts()
        {
            return Ok(await _statisticService.GetInserts());
        }

        [HttpGet]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Route("requests")]
        public async Task<ActionResult> Requests()
        {
            return Ok(await _statisticService.GetRequests());
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Route("hits/{name}")]
        public async Task<ActionResult> Hits(string name)
        {
            return Ok(await _statisticService.GetHits(name));
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Route("misses/{name}")]
        public async Task<ActionResult> Misses(string name)
        {
            return Ok(await _statisticService.GetMiss(name));
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Route("inserts/{name}")]
        public async Task<ActionResult> Inserts(string name)
        {
            return Ok(await _statisticService.GetInserts(name));
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Route("requests/{name}")]
        public async Task<ActionResult> Requests(string name)
        {
            return Ok(await _statisticService.GetRequests(name));
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Route("requests")]
        public async Task<ActionResult> Requests([FromBody] StatisticRequest request)
        {
            return Ok(await _statisticService.GetRequests(request));
        }

        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(SystemInformation), StatusCodes.Status200OK)]
        [Route("information")]
        public async Task<ActionResult> GetSystemInformation()
        {
            return Ok(await _statisticService.GetSystemInformation());
        }
    }
}
