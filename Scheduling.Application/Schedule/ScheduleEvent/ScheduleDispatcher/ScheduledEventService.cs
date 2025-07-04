using Application.Schedule.ScheduleEvent.Scheduler;
using Scheduling.Contracts;
using Scheduling.Contracts.AttachedResources.DTOs;
using Scheduling.Contracts.Schedule.DTOs;
using TanvirArjel.Extensions.Microsoft.DependencyInjection;
namespace Application.Schedule.ScheduleEvent.ScheduleDispatcher
{
    [ScopedService]
    public class ScheduledEventService
    {
        private ScheduleDto _schedule;
        private readonly TopicAwareDispatcher _topicAwareDispatcher;
       
        private IEnumerable<ScheduleResourceDto> _topics;
        

        public ScheduledEventService(IServiceProvider serviceProvider)
        {
         
            _topicAwareDispatcher = new TopicAwareDispatcher(serviceProvider);
            
        }

        public async Task ExecuteAsync(ScheduleDto schedule, IUnifiedScheduler scheduler,List<ScheduleResourceDto> topics)
        {
            _schedule = schedule;
            _topics = topics;
            
            var currentTime = DateTime.Now;
            if (currentTime >= _schedule.StartDateTime && currentTime <= _schedule.EndDateTime)
            {
                await ScheduleEventManager.scheduleEventService.ScheduleJob(HandleScheduledJob, schedule, scheduler);
                await ScheduleEventManager.scheduleEventService.ScheduleJob(HandleScheduledJob, schedule, scheduler);
            }
            else if (currentTime > _schedule.EndDateTime)
            {
                Console.WriteLine("Task execution skipped as it is outside the allowed schedule range.");
            }
        }

        public void HandleScheduledJob(Guid scheduleId, ScheduleEventType type)
        {
            foreach (var topic in _topics)
            {
                _topicAwareDispatcher.Broadcast(
                    new ScheduleEventTrigger(scheduleId, type,topic.ResourceType)
                );
            }
        }

        public async Task UpdateAsync(ScheduleDto schedule)
        {
        }

        public async Task DeleteAsync(Guid id)
        {
        }
    }

}
