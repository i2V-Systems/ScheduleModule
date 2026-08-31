using Scheduling.Contracts.Schedule.Enums;
using Serilog;
using TanvirArjel.Extensions.Microsoft.DependencyInjection;

namespace Application.Schedule.ScheduleEvent.JobStratgies.helper;

[TransientService]
internal class ScheduleStrategyFactory: IScheduleStrategyFactory
{
    private readonly Dictionary<string, IScheduleJobStrategy> _instanceCache;

    // Strategies are resolved once, from DI, instead of scanning the assembly with
    // reflection and falling back to Activator.CreateInstance. Indexed by concrete type
    // name, exactly as the old Assembly.GetTypes() scan keyed its _instanceCache/_strategyCache
    // (which lines up with CreateCacheKey's "{ScheduleType}ScheduleStrategy" convention).
    public ScheduleStrategyFactory(IEnumerable<IScheduleJobStrategy> strategies)
    {
        _instanceCache = new Dictionary<string, IScheduleJobStrategy>();
        foreach (var strategy in strategies)
        {
            var key = strategy.GetType().Name;
            if (!_instanceCache.ContainsKey(key))
            {
                _instanceCache[key] = strategy;
                Log.Debug($"Registered strategy: {strategy.GetType().Name} for {key}.");
            }
        }
    }

    public IScheduleJobStrategy GetStrategy(ScheduleType scheduleType)
    {
        var key = CreateCacheKey(scheduleType);
        // First try exact match from instance cache
        if (_instanceCache.TryGetValue(key, out var cachedInstance))
        {
            return cachedInstance;
        }
        // Try fallback - look for strategies that can handle this type
        var fallbackStrategy = _instanceCache.Values.Where(s => s.CanHandle(scheduleType)).FirstOrDefault();
        if (fallbackStrategy != null)
        {
            Log.Error($"Using fallback strategy {fallbackStrategy.GetType().Name} for {key}");
            return fallbackStrategy;
        }
        throw new NotSupportedException($"No strategy found for schedule type {scheduleType}. " +
                                        $"Available strategies: {string.Join(", ", _instanceCache.Keys)}");
    }

    private static string CreateCacheKey(ScheduleType scheduleType)
    {
        return  scheduleType.ToString()+"ScheduleStrategy";
    }

    public  Dictionary<string, IScheduleJobStrategy> GetAllStrategies()
    {
        return _instanceCache;
    }
}
