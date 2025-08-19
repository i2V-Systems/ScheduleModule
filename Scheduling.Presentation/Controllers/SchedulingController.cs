using CommonUtilityModule.CrudUtilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly ILogger<SchedulingController> _logger;
        private readonly IResourceManager _resourceManager;
        private readonly IScheduleManager _scheduleManager;
        
        public SchedulingController( 
            ILogger<SchedulingController> logger,
            IScheduleManager scheduleManager,
            IResourceManager resourceManager)
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
        
        [HttpGet]
        [Route("GetAllResourceDetails")]
        public async Task<IActionResult> GetAllResourceDetails()
        { 
            try
            {
                HttpContext.Request.Headers.TryGetValue("Username", out StringValues UserName);
                return Ok(await  _scheduleManager.GetScheduleWithAllDetails(UserName));
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
        public async Task<IActionResult> Create([FromBody] ScheduleAllDetails schedule)
        {
            try
            {
                HttpContext.Request.Headers.TryGetValue("userid", out StringValues userid);

                Guid scheduleId = await _scheduleManager.CreateScheduleAsync(schedule.schedules, userid);
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
                return Ok(schedulesource.schedules);
            }
            catch (System.Exception ex)
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
                HttpContext.Request.Headers.TryGetValue("clientId", out StringValues clientId);
                await _scheduleManager.UpdateScheduleAsync(schedule.schedules);
                ScheduleAllDetails updatedSchedule =   _scheduleManager.GetDetailed(schedule.schedules.Id);
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
                ScheduleAllDetails? scheduleWithAllDetails = _scheduleManager.GetScheduleDetailsFromCache(id);
                await _scheduleManager.DeleteScheduleAsync(id);
                var objectToSend = new Dictionary<string, dynamic>()
                {
                    {
                        "scheduleAllDetailsList",
                        new List<ScheduleAllDetails?>() { scheduleWithAllDetails }
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
            List<ScheduleAllDetails?> scheduleAllDetailsList =
                new List<ScheduleAllDetails?>();
            foreach (var id in ScheduleToBeDeleted)
            {
                scheduleAllDetailsList.Add(
                    _scheduleManager.GetScheduleDetailsFromCache(id)
                );
            }
            
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

        [HttpPost("attachSchedule")]
        public async Task<IActionResult> AttachSchedule([FromBody] ScheduleResourceDto resourceDto)
        {
            try
            {
                await _resourceManager.AddScheduleResourceMap(resourceDto);
                var schedule = _scheduleManager.GetScheduleFromCache(resourceDto.ScheduleId);
                _scheduleManager.UpdateInMemory(schedule);
                List<ScheduleAllDetails?> scheduleAllDetailsList =
                    new List<ScheduleAllDetails?>();
              
                var updatedSchedule= _scheduleManager.GetScheduleDetailsFromCache(schedule.Id);
                var objectToSend = 
                    new Dictionary<string, dynamic>()
                    {
                        {
                            "scheduleAllDetailsList",
                            new List<ScheduleAllDetails>() { updatedSchedule }
                        },
                    };
                await _scheduleManager.SendCrudDataToClientAsync(
                    CrudMethodType.Update,
                    objectToSend
                );
                return Ok(updatedSchedule);
            }
            catch (Exception e)
            {
                Log.Error("error in SchedulingController AttachSchedule",e.Message);
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
        public async Task<IActionResult> DetachMultipleSchedule([FromBody] DetachScheduleRequest data)
        {
            try
            {
                await _resourceManager.DeleteMultipleScheduleResourceMap(data.Ids,data.Schedule);
                var schedule = _scheduleManager.GetScheduleFromCache(data.Schedule.schedules.Id);
                _scheduleManager.UpdateInMemory(schedule);
                var updatedSchedule= _scheduleManager.GetScheduleDetailsFromCache(data.Schedule.schedules.Id);
                var objectToSend = 
                    new Dictionary<string, dynamic>()
                    {
                        {
                            "scheduleAllDetailsList",
                            new List<ScheduleAllDetails>() { updatedSchedule }
                        },
                    };
                await _scheduleManager.SendCrudDataToClientAsync(
                    CrudMethodType.Update,
                    objectToSend
                );
                return Ok(updatedSchedule);
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
            IEnumerable<ScheduleResourceDto> resources =  _resourceManager.GetAllCachedResources();
            return resources;
        }
        
        
        [HttpGet("resources/{id}")]
        public  IEnumerable<ScheduleResourceDto> GetResourcesByScheduleId([FromRoute] Guid id)
        {
            IEnumerable<ScheduleResourceDto> resources =  _resourceManager.GetResourcesByScheduleId(id);
            return resources;
        }
        
        [HttpDelete("resources/{id}")]
        public async Task<IActionResult> DeleteResources([FromRoute] Guid id)
        {
            try
            {
                Guid scheduleId= await _resourceManager.DeleteScheduleResourceMap(id);
                if (scheduleId == Guid.Empty)
                {
                    return BadRequest();
                }
                var scheduleWithAllDetails= _scheduleManager.GetScheduleDetailsFromCache(scheduleId);
                _scheduleManager.UpdateInMemory(scheduleWithAllDetails.schedules);
                var updatedSchedule= _scheduleManager.GetScheduleDetailsFromCache(scheduleWithAllDetails.schedules.Id);
                var objectToSend = new Dictionary<string, dynamic>()
                    {
                        {
                            "scheduleAllDetailsList",
                            new List<ScheduleAllDetails?>() { updatedSchedule }
                        },
                    };
                await _scheduleManager.SendCrudDataToClientAsync(
                        CrudMethodType.Update,
                        objectToSend
                    );
                return Ok();
            }
            catch(Exception e)
            {
                Log.Error("error in SchedulingController [DeleteResources]",e.Message);
                throw;
            }

        }
        
        [HttpPost("resources")]
        public async Task<IActionResult> CreateResource([FromBody] ScheduleResourceDto resourceDto)
        {
            try
            {
                await _resourceManager.AddScheduleResourceMap(resourceDto);
                var schedule = _scheduleManager.GetScheduleFromCache(resourceDto.ScheduleId);
                _scheduleManager.UpdateInMemory(schedule);
                List<ScheduleAllDetails?> scheduleAllDetailsList =
                    new List<ScheduleAllDetails?>();
              
                var updatedSchedule= _scheduleManager.GetScheduleDetailsFromCache(schedule.Id);
                var objectToSend = 
                    new Dictionary<string, dynamic>()
                    {
                        {
                            "scheduleAllDetailsList",
                            new List<ScheduleAllDetails>() { updatedSchedule }
                        },
                    };
                await _scheduleManager.SendCrudDataToClientAsync(
                    CrudMethodType.Update,
                    objectToSend
                );
                return Ok(updatedSchedule);
            }
            catch (Exception e)
            {
                Log.Error("error in SchedulingController [CreateResource]",e.Message);
                throw;
            }
        }

        [HttpPut("resources")]
        public async Task<IActionResult> UpdateResource([FromBody] ScheduleResourceDto resourceDto)
        {
            try
            {
                await _resourceManager.UpdateScheduleResourceMap(resourceDto);
                var schedule = _scheduleManager.GetScheduleFromCache(resourceDto.ScheduleId);
                _scheduleManager.UpdateInMemory(schedule);
                List<ScheduleAllDetails?> scheduleAllDetailsList =
                    new List<ScheduleAllDetails?>();
              
                var updatedSchedule= _scheduleManager.GetScheduleDetailsFromCache(schedule.Id);
                var objectToSend = 
                    new Dictionary<string, dynamic>()
                    {
                        {
                            "scheduleAllDetailsList",
                            new List<ScheduleAllDetails>() { updatedSchedule }
                        },
                    };
                await _scheduleManager.SendCrudDataToClientAsync(
                    CrudMethodType.Update,
                    objectToSend
                );
                return Ok(updatedSchedule);
            }
            catch (Exception e)
            {
                Log.Error("error in SchedulingController [UpdateResource]",e.Message);
                throw;
            }
        }

    }
}
