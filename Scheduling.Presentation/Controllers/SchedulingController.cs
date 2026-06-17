using CommonUtilityModule.CrudUtilities;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    public  class SchedulingController : ControllerBase
    {
        private readonly IScheduledEntitiesManager _scheduledEntitiesManager;
        private readonly IScheduleManager _scheduleManager;

        public SchedulingController(
            ILogger<SchedulingController> logger,
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
                return Ok(await  _scheduleManager.GetScheduleWithAllDetails());
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                return BadRequest(ex.Message);
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
                return Ok( _scheduleManager.Get(id));
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ScheduleAllDetails schedule)
        {
            try
            {
                if (schedule.schedules == null || string.IsNullOrWhiteSpace(schedule.schedules.Name))
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

                ScheduleAllDetails scheduleAllDetails = await _scheduleManager.CreateScheduleAsync(schedule.schedules);
                return Ok(scheduleAllDetails.schedules);
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                return BadRequest(ex.Message);
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
                ScheduleAllDetails? scheduleWithAllDetails = _scheduleManager.GetScheduleDetailsFromCache(id);
                await _scheduleManager.DeleteScheduleAsync(id);

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
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            List<ScheduleAllDetails?> scheduleAllDetailsList =
                new List<ScheduleAllDetails?>();
            foreach (var id in ScheduleToBeDeleted)
            {
                scheduleAllDetailsList.Add(
                    _scheduleManager.GetScheduleDetailsFromCache(id)
                );
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
                var scheduleAllDetails = await _scheduleManager.CreateResourceMapping(resourceDto);
                return Ok(scheduleAllDetails);
            }
            catch (Exception e)
            {
                Log.Error("error in SchedulingController AttachSchedule",e.Message);
                throw;
            }
        }

        [HttpPost("detachScheduleResourceMultiple")]
        public async Task<IActionResult> DeleteAttachedResourcesByResourceIds([FromBody] List<DetachScheduleResourceDto> resourceDto)
        {
          try
          {
            await _scheduleManager.DeleteAttachedResources(resourceDto);
            return Ok();
          }
          catch (Exception e)
          {
            Log.Error("error in SchedulingController DeleteAttachedResourcesByResourceIds",e.Message);
            throw;
          }
        }


        // [HttpPost("AttachOrUpdateSchedules")]
        // public async Task<IActionResult> AttachOrUpdateSchedules([FromBody] ScheduleResourceDto dto)
        // {
        //     try
        //     {
        //         // Step 1: Get existing mappings
        //         var existingMappings = _resourceManager.GetResourcesByScheduleId(dto.ScheduleId);
        //         var existingScheduleIds = existingMappings.Select(m => m.ScheduleId).ToHashSet();
        //
        //         // Step 2: Distinct new schedules
        //         var newScheduleIds = dto.ScheduleId.Distinct().ToHashSet();
        //
        //         // Step 3: Determine schedules to add and remove
        //         var schedulesToRemoveIds = existingScheduleIds.Except(newScheduleIds).ToList();
        //         var schedulesToAdd = newScheduleIds.Except(existingScheduleIds).ToList();
        //
        //         // Step 4: Get mappings to remove (by ID)
        //         var mappingsToRemove = existingMappings
        //             .Where(m => schedulesToRemoveIds.Contains(m.ScheduleId))
        //             .Select(m => m.Id)
        //             .ToList();
        //
        //         // Step 5: Remove mappings from database and memory
        //         if (mappingsToRemove.Any())
        //         {
        //             await _resourceManager.DeleteScheduleResourceMap(mappingsToRemove);
        //         }
        //
        //         // Step 6: Add new mappings to database and memory
        //         foreach (var scheduleId in schedulesToAdd)
        //         {
        //             var mapDto = new ScheduleResourceDto(Guid.NewGuid(), scheduleId, dto.ResourceId, dto.ResourceType);
        //             await _resourceManager.AddScheduleResourceMap(mapDto);
        //         }
        //
        //         // Step 7: Update in-memory schedule for affected schedule IDs
        //         var affectedScheduleIds = schedulesToAdd.Union(schedulesToRemoveIds);
        //         foreach (var scheduleId in affectedScheduleIds)
        //         {
        //             var schedule = _scheduleManager.GetScheduleFromCache(scheduleId);
        //             if (schedule != null)
        //             {
        //                 _scheduleManager.UpdateInMemory(schedule);
        //             }
        //         }
        //
        //         // Step 8: Prepare final list of schedule details
        //         var finalScheduleDetails = newScheduleIds
        //             .Select(id => _scheduleManager.GetScheduleDetailsFromCache(id))
        //             .Where(details => details != null)
        //             .ToList();
        //
        //         // Step 9: Notify clients about updated state (if needed)
        //         if (finalScheduleDetails.Any())
        //         {
        //             var objectToSend = new Dictionary<string, dynamic>
        //             {
        //                 { "scheduleAllDetailsList", finalScheduleDetails }
        //             };
        //
        //             await _scheduleManager.SendCrudDataToClientAsync(CrudMethodType.Update, objectToSend);
        //         }
        //
        //         return Ok(finalScheduleDetails);
        //     }
        //     catch (Exception ex)
        //     {
        //         Log.Error("AttachOrUpdateSchedules error: {Message}", ex.Message);
        //         return BadRequest("Failed to update schedule mappings");
        //     }
        // }


        [HttpPut("removeMultipleAttachedResource")]
        public async Task<IActionResult> DetachMultipleScheduleResource([FromBody] DetachScheduleRequest data)
        {
            try
            {
                await _scheduledEntitiesManager.DeleteMultipleScheduleResourceMap(data.Ids,data.Schedule);
                var schedule = _scheduleManager.GetScheduleFromCache(data.Schedule.schedules.Id);
                ScheduleAllDetails scheduleAllDetails = await _scheduleManager.UpdateInMemory(schedule);
                List<ScheduleAllDetails> scheduleAllDetailsList = new List<ScheduleAllDetails>() { scheduleAllDetails };
                _scheduleManager.SendClientNotificationWithSchedule(scheduleAllDetailsList,
                  CrudMethodType.ScheduleAttachmentChanged);
                return Ok(scheduleAllDetails);
            }
            catch (Exception e)
            {
                Log.Error("error in SchedulingController AttachSchedule",e.Message);
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
                  List<ScheduleAllDetails> scheduleAllDetailsList = new List<ScheduleAllDetails>(){scheduleAllDetails};
                  _scheduleManager.SendClientNotificationWithSchedule(scheduleAllDetailsList,CrudMethodType.ScheduleAttachmentChanged);
                }

                return Ok();
            }
            catch(Exception e)
            {
                Log.Error(e.Message,"error in SchedulingController [DeleteResources]");
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
                ScheduleAllDetails scheduleAllDetails = await _scheduleManager.UpdateScheduleAsync(schedule);
                return Ok(scheduleAllDetails.schedules);
            }
            catch (Exception e)
            {
                Log.Error(e.Message,"error in SchedulingController [CreateResource]");
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
                ScheduleAllDetails scheduleAllDetails= await _scheduleManager.UpdateInMemory(schedule);
                List<ScheduleAllDetails> scheduleAllDetailsList = new List<ScheduleAllDetails>(){scheduleAllDetails};
                _scheduleManager.SendClientNotificationWithSchedule(scheduleAllDetailsList,CrudMethodType.ScheduleAttachmentChanged);
                return Ok(scheduleAllDetails);
            }
            catch (Exception e)
            {
                Log.Error(e.Message,"error in SchedulingController [UpdateResource]");
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
          catch (Exception e)
          {
            Log.Error(e.Message,"error in SchedulingController [UpdateResource]");
            throw;
          }
        }

    }
}
