using DisCache.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stamon.Services;
using System.Threading.Tasks;

namespace DisCache.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticController : ControllerBase
    {

        private readonly StamonService _statisticManager;

        public StatisticController(StamonService manager)
        {
            _statisticManager = manager;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Route("hits")]
        public async Task<ActionResult> Hits()
        {
            return Ok(await _statisticManager.GetSlotDatas("default","Hit"));
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Route("misses")]
        public async Task<ActionResult> Misses()
        {
            return Ok(await _statisticManager.GetSlotDatas("default", "Miss"));
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Route("inserts")]
        public async Task<ActionResult> Inserts()
        {
            return Ok(await _statisticManager.GetSlotDatas("default","Insert"));
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Route("requests")]
        public async Task<ActionResult> Requests()
        {
            return Ok( await _statisticManager.GetSlotDatas("default","Request"));
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Route("hits/{name}")]
        public async Task<ActionResult> Hits(string name)
        {
            return Ok(await _statisticManager.GetSlotDatas(name,"Hit"));
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Route("misses/{name}")]
        public async Task<ActionResult> Misses(string name)
        {
            return Ok(await _statisticManager.GetSlotDatas(name,"Miss"));
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Route("inserts/{name}")]
        public async Task<ActionResult> Inserts(string name)
        {
            return Ok(await _statisticManager.GetSlotDatas(name,"Insert"));
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Route("requests/{name}")]
        public async Task<ActionResult> Requests(string name)
        {
            return Ok(await _statisticManager.GetSlotDatas(name,"Request"));
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Route("requests")]
        public async Task<ActionResult> Requests([FromBody]StatisticRequest request)
        {
            return Ok(await _statisticManager.GetSlotDatas(request.SlotName, "Request"));
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Route("all/{name}")]
        public async Task<ActionResult> All(string name)
        {
            return Ok(
                new StatisticResult()
                {
                    Hits = await _statisticManager.GetSlotDatas(name,"Request"),
                    Inserts = await _statisticManager.GetSlotDatas(name,"Insert"),
                    Requetsts = await _statisticManager.GetSlotDatas(name,"Request"),
                    Misses = await _statisticManager.GetSlotDatas(name,"Miss")
                }
                );
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Route("all")]
        public async Task<ActionResult> All()
        {
            return Ok(
                new StatisticResult()
                {
                    Hits = await _statisticManager.GetSlotDatas("default","Hit"),
                    Inserts = await _statisticManager.GetSlotDatas("default", "Insert"),
                    Requetsts = await _statisticManager.GetSlotDatas("default", "Request"),
                    Misses = await _statisticManager.GetSlotDatas("default", "Miss")
                }
            );
        }

        [HttpGet]
        [ProducesResponseType(typeof(SystemInformation),StatusCodes.Status200OK)]
        [Route("information")]
        public Task<ActionResult> GetSystemInformation()
        {
            var result = new SystemInformation()
            {
                Name = RuntimeInformation.Name,
                Backend = RuntimeInformation.Backend,
                BackendInformation = RuntimeInformation.BackendInformation,
                Startup = RuntimeInformation.Startup,
                Version = RuntimeInformation.Version
            };

            return Task.FromResult((ActionResult)Ok(result));
        }


        [HttpGet]
        [ProducesResponseType(typeof(SystemInformation), StatusCodes.Status200OK)]
        [Route("slots")]
        public async Task<ActionResult> GetSlots()
        {
            return Ok( await _statisticManager.GetSlotNames());
        }
    }
}
