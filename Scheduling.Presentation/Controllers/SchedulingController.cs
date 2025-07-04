
using Application.AttachedResources;
using CommonUtilityModule.CrudUtilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Scheduling.Contracts.AttachedResources.DTOs;
using Scheduling.Contracts.Schedule;
using Scheduling.Contracts.Schedule.DTOs;
using Serilog;

namespace Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public  class SchedulingController : Controller
    {
        private readonly IScheduleManager _scheduleManager;
        private readonly ILogger<SchedulingController> _logger;
        private readonly ResourceManager _resourceManager;
        
        public SchedulingController(ILogger<SchedulingController> logger,IScheduleManager scheduleManager,ResourceManager resourceManager)
        {
            _logger = logger;
            _scheduleManager = scheduleManager;
            _resourceManager = resourceManager;
        }

        [HttpGet]
        public  IEnumerable<ScheduleDto> GetAll()
        {
            IEnumerable<ScheduleDto> schedules =  _scheduleManager.GetAllCachedSchedules();
            return schedules;
        }
        
        [HttpGet("{id}")]
        public  IEnumerable<ScheduleResourceDto> GetResourcesByScheduleId([FromRoute] Guid id)
        {
            IEnumerable<ScheduleResourceDto> resources =  _resourceManager.GetResourcesByScheduleId(id);
            return resources;
        }
        
       
        [HttpGet("~/api/Schedules/GetAllResourceDetails")]
        public  IActionResult GetAllResourceDetails()
        { 
            try
            {
                HttpContext.Request.Headers.TryGetValue("Username", out StringValues UserName);
                return Ok( _resourceManager.GetAllCachedResources());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
                return BadRequest(ex.Message.ToString());
            }
        }
        
        [HttpGet("{id}")]
        public  IActionResult Get([FromRoute] Guid id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                if (!_scheduleManager.IsScheduleLoaded(id))
                {
                    return NotFound();
                }
                return Ok( _scheduleManager.Get(id));
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ScheduleDto schedule)
        {
            try
            {
                HttpContext.Request.Headers.TryGetValue("userid", out StringValues userid);

                Guid scheduleId = await _scheduleManager.CreateScheduleAsync(schedule, userid);
                ScheduleAllDetails schedulesource = _scheduleManager.GetDetailed(scheduleId);
                var objectToSend = new Dictionary<string, dynamic>()
                {
                    {
                        "scheduleAllDetailsList",
                        new List<ScheduleAllDetails>() { schedulesource }
                    }
                };
                _scheduleManager.SendCrudDataToClientAsync(
                    CrudMethodType.Add,
                    objectToSend
                );
                return Ok(schedule);
            }
            catch (System.Exception ex)
            {
                Log.Error(ex, ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update( [FromRoute] Guid id, [FromBody] ScheduleDto schedule)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (id != schedule.Id) return BadRequest();

            try
            {
                HttpContext.Request.Headers.TryGetValue("clientId", out StringValues clientId);
                await _scheduleManager.UpdateScheduleAsync(schedule);
                ScheduleAllDetails updatedSchedule =   _scheduleManager.GetDetailed(schedule.Id);
                var objectToSend = new Dictionary<string, dynamic>()
                {
                    {
                        "scheduleAllDetailsList",
                        new List<ScheduleAllDetails>() { updatedSchedule }
                    },
                };
                
                _scheduleManager.SendCrudDataToClientAsync(
                    CrudMethodType.Update,
                    objectToSend
                );
                return Ok(updatedSchedule);
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                return BadRequest(ex.Message);
            }
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            HttpContext.Request.Headers.TryGetValue("clientId", out StringValues clientId);
            if (!_scheduleManager.IsScheduleLoaded(id))
            {
                return NotFound();
            }
            try
            {
                ScheduleAllDetails scheduleWithAllDetails = _scheduleManager.GetScheduleDetailsFromCache(id);
                _scheduleManager.DeleteScheduleAsync(id);
                var objectToSend = new Dictionary<string, dynamic>()
                {
                    {
                        "scheduleAllDetailsList",
                        new List<ScheduleAllDetails>() { scheduleWithAllDetails }
                    },
                };
                await _scheduleManager.SendCrudDataToClientAsync(
                    CrudMethodType.Delete,
                    objectToSend
                );
                return Ok(scheduleWithAllDetails);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                Log.Error(ex, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                return BadRequest(ex.Message);
            }
        }
        
        [HttpPut("DeleteMultiple")]
        public async Task<IActionResult> DeleteMultiple([FromBody] List<Guid> ScheduleToBeDeleted)
        {
            HttpContext.Request.Headers.TryGetValue("Username", out StringValues userName);
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            IEnumerable<ScheduleAllDetails> scheduleAllDetailsList = _scheduleManager.GetScheduleWithAllDetails(userName);
           
            
            await _scheduleManager.DeleteMultipleSchedulesAsync(ScheduleToBeDeleted);
            var objectToSend = new Dictionary<string, dynamic>()
            {
                { "scheduleAllDetailsList", scheduleAllDetailsList },
            };
            await _scheduleManager.SendCrudDataToClientAsync(
                CrudMethodType.Delete,
                objectToSend
            );
            return Ok();
        }
    }
}
