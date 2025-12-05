using CommonUtilityModule.CrudUtilities;
using Infrastructure;
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
    public  class SchedulingController : ControllerBase
    {
        private readonly IResourceManager _resourceManager;
        private readonly IScheduleManager _scheduleManager;
        private readonly INotificationManager _notificationManager;

        public SchedulingController(
            IScheduleManager scheduleManager,
            IResourceManager resourceManager,
            INotificationManager notificationManager)
        {
            _scheduleManager = scheduleManager;
            _resourceManager = resourceManager;
            _notificationManager = notificationManager;
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

                Guid scheduleId = await _scheduleManager.CreateScheduleAsync(schedule.schedules, userid!);
                ScheduleAllDetails scheduleAllDetails = _scheduleManager.GetDetailed(scheduleId);



                var objectToSend = _scheduleManager.GetAllDetailNotificationObj(new List<ScheduleAllDetails>()
                  { scheduleAllDetails });
                await _notificationManager.SendCrudDataToClientAsync(
                  CrudMethodType.Add,
                  objectToSend
                );
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
                await _scheduleManager.UpdateScheduleAsync(schedule.schedules);
                ScheduleAllDetails updatedSchedule =   _scheduleManager.GetDetailed(schedule.schedules.Id);

                var objectToSend =
                  _scheduleManager.GetAllDetailNotificationObj(new List<ScheduleAllDetails>() { updatedSchedule });
                await _notificationManager.SendCrudDataToClientAsync(
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
                var objectToSend = _scheduleManager.GetAllDetailNotificationObj(   new List<ScheduleAllDetails>() { scheduleWithAllDetails });

                await _notificationManager.SendCrudDataToClientAsync(
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

            var objectToSend = _scheduleManager.GetAllDetailNotificationObj(  scheduleAllDetailsList);
            await _notificationManager.SendCrudDataToClientAsync(
                CrudMethodType.Delete,
                objectToSend
            );
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

            var objectToSend = _scheduleManager.GetAllDetailNotificationObj(  schedulesToUpdate);
            await _notificationManager.SendCrudDataToClientAsync(
                CrudMethodType.Update,
                objectToSend
            );
            return Ok(schedulesToUpdate);
        }
        [HttpPost("attachSchedule")]
        public async Task<IActionResult> AttachSchedule([FromBody] ScheduleResourceDto resourceDto)
        {
            try
            {
                await _resourceManager.AddScheduleResourceMap(resourceDto);
                var schedule = _scheduleManager.GetScheduleFromCache(resourceDto.ScheduleId);
                _scheduleManager.UpdateInMemory(schedule);

                var updatedSchedule= _scheduleManager.GetScheduleDetailsFromCache(schedule.Id);

                var objectToSend = _scheduleManager.GetAllDetailNotificationObj(  new List<ScheduleAllDetails>() { updatedSchedule });
                await _notificationManager.SendCrudDataToClientAsync(
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



        [HttpPut("removeMultipleAttachedResource")]
        public async Task<IActionResult> DetachMultipleScheduleResource([FromBody] DetachScheduleRequest data)
        {
            try
            {
                await _resourceManager.DeleteMultipleScheduleResourceMap(data.Ids,data.Schedule);
                var schedule = _scheduleManager.GetScheduleFromCache(data.Schedule.schedules.Id);
                _scheduleManager.UpdateInMemory(schedule);
                var updatedSchedule= _scheduleManager.GetScheduleDetailsFromCache(data.Schedule.schedules.Id);

                var objectToSend = _scheduleManager.GetAllDetailNotificationObj(  new List<ScheduleAllDetails>() { updatedSchedule });
                await _notificationManager.SendCrudDataToClientAsync(
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
                if (scheduleWithAllDetails != null)
                {
                  _scheduleManager.UpdateInMemory(scheduleWithAllDetails.schedules);
                  var updatedSchedule= _scheduleManager.GetScheduleDetailsFromCache(scheduleWithAllDetails.schedules.Id);
                  var objectToSend = _scheduleManager.GetAllDetailNotificationObj(  new List<ScheduleAllDetails>() { updatedSchedule });


                  await _notificationManager.SendCrudDataToClientAsync(
                    CrudMethodType.Update,
                    objectToSend
                  );
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
                await _resourceManager.AddScheduleResourceMap(resourceDto);
                var schedule = _scheduleManager.GetScheduleFromCache(resourceDto.ScheduleId);
                await _scheduleManager.UpdateScheduleAsync(schedule);

                var updatedSchedule= _scheduleManager.GetScheduleDetailsFromCache(schedule.Id);
                var objectToSend = _scheduleManager.GetAllDetailNotificationObj(new List<ScheduleAllDetails>() {updatedSchedule});
                await _notificationManager.SendCrudDataToClientAsync(
                    CrudMethodType.Update,
                    objectToSend
                );
                return Ok(updatedSchedule);
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
                await _resourceManager.UpdateScheduleResourceMap(resourceDto);
                var schedule = _scheduleManager.GetScheduleFromCache(resourceDto.ScheduleId);
                _scheduleManager.UpdateInMemory(schedule);

                var updatedSchedule= _scheduleManager.GetScheduleDetailsFromCache(schedule.Id);

                var objectToSend =
                  _scheduleManager.GetAllDetailNotificationObj(new List<ScheduleAllDetails>() { updatedSchedule });
                await _notificationManager.SendCrudDataToClientAsync(
                    CrudMethodType.Update,
                    objectToSend
                );
                return Ok(updatedSchedule);
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
            var affectedSchedules = await _scheduleManager.CreateAndUpdateResourceMapping(payload);
            await _notificationManager.SendCrudDataToClientAsync(
              CrudMethodType.Update,
              _scheduleManager.GetAllDetailNotificationObj(affectedSchedules)

            );
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
