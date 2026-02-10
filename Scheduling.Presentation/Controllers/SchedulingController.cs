using CommonUtilityModule.CrudUtilities;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Scheduling.Contracts.AttachedResources;
using Scheduling.Contracts.AttachedResources.DTOs;
using Scheduling.Contracts.Schedule;
using Scheduling.Contracts.Schedule.DTOs;
using Serilog;

namespace Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public  class SchedulingController : ControllerBase
    {
        private readonly IScheduledEntitiesManager _scheduledEntitiesManager;
        private readonly IScheduleManager _scheduleManager;

        public SchedulingController(
            IScheduleManager scheduleManager,
            IScheduledEntitiesManager scheduledEntitiesManager
            )
        {
            _scheduleManager = scheduleManager;
            _scheduledEntitiesManager = scheduledEntitiesManager;
        }

        [HttpGet]
        public  IEnumerable<ScheduleDto> GetAll()
        {
            IEnumerable<ScheduleDto> schedules =  _scheduleManager.GetAllCachedSchedules();
            return schedules;
        }

        [HttpGet]
        [Route("GetAllResourceDetails")]
        public async Task<IActionResult> GetAllResourceDetails()
        {
            try
            {
                HttpContext.Request.Headers.TryGetValue("Username", out StringValues userName);
                return Ok(await  _scheduleManager.GetScheduleWithAllDetails(userName!));
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message);
                return BadRequest(exception.Message);
            }
        }

        [HttpGet("{id}")]
        public  ActionResult<ScheduleDto> Get([FromRoute] Guid id)
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

                ScheduleDto scheduleDto = _scheduleManager.Get(id);
                return Ok(scheduleDto );
            }
            catch (Exception exception)
            {
                Log.Error(exception, exception.Message);
                return BadRequest(exception.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ScheduleAllDetails schedule)
        {
            try
            {
                HttpContext.Request.Headers.TryGetValue("userid", out StringValues userid);

                if (schedule?.schedules == null || string.IsNullOrWhiteSpace(schedule?.schedules.Name))
                {
                    return BadRequest("Schedule name is required");
                }
                var isNameAvailable =  _scheduleManager.IsScheduleNameAvailable(schedule.schedules.Name);
                if (!isNameAvailable)
                {
                    return Conflict(new {
                        message = "A schedule with this name already exists",
                        field = "name",
                        code = "DUPLICATE_NAME"
                    });
                }

                ScheduleAllDetails scheduleAllDetails = await _scheduleManager.CreateScheduleAsync(schedule.schedules, userid!);
                return Ok(scheduleAllDetails.schedules);
            }
            catch (Exception exception)
            {
                Log.Error(exception, exception.Message);
                return BadRequest(exception.Message);
            }
        }



        [HttpPut("{id}")]
        public async Task<IActionResult> Update( [FromRoute] Guid id, [FromBody] ScheduleAllDetails schedule)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (id != schedule.schedules.Id) return BadRequest();

            try
            {
                var isNameAvailable =  _scheduleManager.IsScheduleNameAvailable(schedule.schedules.Name,id);
                if (!isNameAvailable)
                {
                    return Conflict(new {
                        message = "A schedule with this name already exists",
                        field = "name",
                        code = "DUPLICATE_NAME"
                    });
                }
                ScheduleAllDetails scheduleAllDetails = await _scheduleManager.UpdateScheduleAsync(schedule.schedules);

                return Ok(scheduleAllDetails);
            }
            catch (Exception exception)
            {
                Log.Error(exception, exception.Message);
                return BadRequest(exception.Message);
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
                ScheduleAllDetails? scheduleWithAllDetails = _scheduleManager.GetScheduleDetailsFromCache(id);
                await _scheduleManager.DeleteScheduleAsync(id);

                return Ok(scheduleWithAllDetails);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                Log.Error(dbUpdateConcurrencyException, dbUpdateConcurrencyException.Message);
                throw;
            }
            catch (Exception exception)
            {
                Log.Error(exception, exception.Message);
                return BadRequest(exception.Message);
            }
        }

        [HttpPut("DeleteMultiple")]
        public async Task<IActionResult> DeleteMultiple([FromBody] List<Guid> ScheduleToBeDeleted)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }


            await _scheduleManager.DeleteMultipleSchedulesAsync(ScheduleToBeDeleted);
            return Ok();
        }

        [HttpPut("UpdateMultiple")]
        public async Task<IActionResult> UpdateMultiple([FromBody] List<ScheduleAllDetails> schedulesToUpdate)
        {
            HttpContext.Request.Headers.TryGetValue("Username", out StringValues userName);
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }


            await _scheduleManager.UpdateMultipleSchedulesAsync(schedulesToUpdate);
            return Ok(schedulesToUpdate);
        }
        [HttpPost("attachSchedule")]
        public async Task<IActionResult> AttachSchedule([FromBody] ScheduleResourceDto resourceDto)
        {
            try
            {
                await _scheduledEntitiesManager.AddScheduleResourceMap(resourceDto);
                var schedule = _scheduleManager.GetScheduleFromCache(resourceDto.ScheduleId);

                if (schedule != null)
                {
                  ScheduleAllDetails scheduleAllDetails = await _scheduleManager.UpdateInMemory(schedule);

                  return Ok(scheduleAllDetails);
                }

                  throw new NullReferenceException("Schedule not found");

            }
            catch (Exception exception)
            {
                Log.Error("error in SchedulingController AttachSchedule",exception.Message);
                return BadRequest(exception.Message);
            }
        }



        [HttpPut("removeMultipleAttachedResource")]
        public async Task<IActionResult> DetachMultipleScheduleResource([FromBody] DetachScheduleRequest data)
        {
            try
            {
                await _scheduledEntitiesManager.DeleteMultipleScheduleResourceMap(data.Ids,data.Schedule);
                var schedule = _scheduleManager.GetScheduleFromCache(data.Schedule.schedules.Id);
                if (schedule != null)
                {
                  ScheduleAllDetails scheduleAllDetails = await _scheduleManager.UpdateInMemory(schedule);

                  return Ok(scheduleAllDetails);
                }

                  throw new NullReferenceException("Schedule not found");

            }
            catch (Exception exception)
            {
                Log.Error("error in SchedulingController AttachSchedule",exception.Message);
                throw;
            }
        }

        [HttpGet("resources")]
        public  IEnumerable<ScheduleResourceDto> GetAllResources()
        {
            IEnumerable<ScheduleResourceDto> resources =  _scheduledEntitiesManager.GetAllCachedResources();
            return resources;
        }


        [HttpGet("resources/{id}")]
        public  IEnumerable<ScheduleResourceDto> GetResourcesByScheduleId([FromRoute] Guid id)
        {
            IEnumerable<ScheduleResourceDto> resources =  _scheduledEntitiesManager.GetResourcesByScheduleId(id);
            return resources;
        }

        [HttpDelete("resources/{id}")]
        public async Task<IActionResult> DeleteResources([FromRoute] Guid id)
        {
            try
            {
                Guid scheduleId= await _scheduledEntitiesManager.DeleteScheduleResourceMap(id);
                if (scheduleId == Guid.Empty)
                {
                    return BadRequest();
                }
                var scheduleWithAllDetails= _scheduleManager.GetScheduleDetailsFromCache(scheduleId);
                if (scheduleWithAllDetails != null)
                {
                  ScheduleAllDetails scheduleAllDetails= await _scheduleManager.UpdateInMemory(scheduleWithAllDetails.schedules);
                }

                return Ok();
            }
            catch(Exception exception)
            {
                Log.Error(exception.Message,"error in SchedulingController [DeleteResources]");
                throw;
            }

        }

        [HttpPost("resources")]
        public async Task<IActionResult> CreateResource([FromBody] ScheduleResourceDto resourceDto)
        {
            try
            {
                await _scheduledEntitiesManager.AddScheduleResourceMap(resourceDto);
                var schedule = _scheduleManager.GetScheduleFromCache(resourceDto.ScheduleId);
                if (schedule != null)
                {
                  ScheduleAllDetails scheduleAllDetails = await _scheduleManager.UpdateScheduleAsync(schedule);

                  return Ok(scheduleAllDetails.schedules);
                }

                  throw new NullReferenceException("Schedule not found");

            }
            catch (Exception exception)
            {
                Log.Error(exception.Message,"error in SchedulingController [CreateResource]");
                throw;
            }
        }

        [HttpPut("resources")]
        public async Task<IActionResult> UpdateResource([FromBody] ScheduleResourceDto resourceDto)
        {
            try
            {
                await _scheduledEntitiesManager.UpdateScheduleResourceMap(resourceDto);
                var schedule = _scheduleManager.GetScheduleFromCache(resourceDto.ScheduleId);
                if (schedule != null)
                {
                  ScheduleAllDetails scheduleAllDetails= await _scheduleManager.UpdateInMemory(schedule);

                  return Ok(scheduleAllDetails);
                }

                  throw new NullReferenceException("Schedule not found");

            }
            catch (Exception exception)
            {
                Log.Error(exception.Message,"error in SchedulingController [UpdateResource]");
                throw;
            }
        }

        [HttpPut("resources/createAndUpdate")]
        public async Task<IActionResult> CreateAndUpdateResource([FromBody] ScheduleResourceDto payload)
        {
          try
          {
            List<ScheduleAllDetails> affectedSchedules = await _scheduleManager.CreateAndUpdateResourceMapping(payload);

            return Ok(affectedSchedules);
          }
          catch (Exception exception)
          {
            Log.Error(exception.Message,"error in SchedulingController [UpdateResource]");
            throw;
          }
        }

    }
}
