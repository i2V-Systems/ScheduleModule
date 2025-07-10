using System.Reflection;
using Application.Schedule.ScheduleEvent.Scheduler;
using Application.Schedule.ScheduleEvent.SchedulerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scheduling.Contracts;
using Scheduling.Contracts.AttachedResources.DTOs;
using Scheduling.Contracts.Schedule.DTOs;
using Scheduling.Contracts.Schedule.ScheduleEvent;
using TanvirArjel.Extensions.Microsoft.DependencyInjection;
namespace Application.Schedule.ScheduleEvent.ScheduleDispatcher
{
    [TransientService]
    public class ScheduledEventService
    {
        private ScheduleDto _schedule;
        private IEnumerable<ScheduleResourceDto> _topics;
       
        private readonly TopicAwareDispatcher _topicAwareDispatcher;
        public readonly ISchedulerService _schedulerService;
        private readonly IUnifiedScheduler _scheduler;
        
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public ScheduledEventService(IServiceProvider serviceProvider,IConfiguration configuration,IUnifiedScheduler scheduler,TopicAwareDispatcher topicAwareDispatcher)
        {

            _serviceProvider = serviceProvider;
            _configuration = configuration;
            
            string serviceName= configuration.GetValue<string>("SchedulingService") ??  throw new InvalidOperationException("SchedulingService configuration value is missing or empty.");
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _topicAwareDispatcher = topicAwareDispatcher;
            _schedulerService = ResolveService(serviceProvider, serviceName)
                                ?? throw new InvalidOperationException("Unable to resolve the scheduler task service.");

        }

        public async Task ExecuteAsync(ScheduleDto schedule,List<ScheduleResourceDto> topics)
        {
            _schedule = schedule;
            _topics = topics;
            
            var currentTime = DateTime.Now;
            if (currentTime >= _schedule.StartDateTime && currentTime <= _schedule.EndDateTime)
            {
                _schedulerService.ScheduleJob(HandleScheduledJob, schedule, _scheduler);
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
        
        public  ISchedulerService ResolveService(IServiceProvider serviceProvider, string serviceName)
        {
            var targetType = typeof(ISchedulerService);
            var matchingType = Assembly.GetExecutingAssembly()
                .GetTypes()
                .FirstOrDefault(t =>
                    t.IsClass &&
                    !t.IsAbstract &&
                    targetType.IsAssignableFrom(t) &&
                    t.Name.ToLower().Contains(serviceName));

            if (matchingType != null)
            {
                return CreateInstance(matchingType, serviceProvider);
            }
            // fallback to default implementation
            return serviceProvider.GetService<CoravelSchedulerService>();
        }
        
        public  ISchedulerService CreateInstance(Type serviceType, IServiceProvider serviceProvider)
        {
            try
            {
                // get from DI container 
                var serviceFromDi = serviceProvider.GetRequiredService(serviceType);
                if (serviceFromDi != null)
                {
                    return (ISchedulerService)serviceFromDi;
                }
                return serviceProvider.GetRequiredService<CoravelSchedulerService>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create instance of {serviceType.Name}: {ex.Message}", ex);
            }
        }
        
        public void UnscheduleJob(Guid id)
        {
            _schedulerService.UnscheduleJob(id, _scheduler);
        }
    }

}
