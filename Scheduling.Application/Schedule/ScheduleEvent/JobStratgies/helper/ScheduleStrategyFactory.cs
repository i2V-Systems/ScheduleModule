using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scheduling.Contracts.Schedule.Enums;
using Serilog;
using TanvirArjel.Extensions.Microsoft.DependencyInjection;

namespace Application.Schedule.ScheduleEvent.JobStratgies.helper;

[TransientService]
internal class ScheduleStrategyFactory: IScheduleStrategyFactory
{
    private readonly Dictionary<string, IScheduleJobStrategy> _instanceCache;
  
    private readonly Dictionary<string, Type> _strategyCache;
    
    private readonly ILogger<ScheduleStrategyFactory> _logger;
    private readonly IServiceProvider _serviceProvider;
    public ScheduleStrategyFactory(ILogger<ScheduleStrategyFactory> logger,IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider;
        _instanceCache = new Dictionary<string, IScheduleJobStrategy>();
        _strategyCache = new Dictionary<string, Type>();
        DiscoverStrategies();
    }

    private void DiscoverStrategies()
    { 
        // Get all registered strategies from DI container
        List<Type> strategies=Assembly.GetExecutingAssembly().GetTypes()
            .Where(type => typeof(IScheduleJobStrategy).IsAssignableFrom(type) && 
                           !type.IsInterface && 
                           !type.IsAbstract)
            .ToList();
        foreach (var strategy in strategies)
        {
            // var key = CreateCacheKey();
            
            if (!_instanceCache.ContainsKey(strategy.Name))
            {
                _strategyCache[strategy.Name] = strategy;
                _instanceCache[strategy.Name] = CreateStrategyInstance(strategy);
                Log.Debug($"Registered strategy: {strategy.GetType().Name} for {strategy.Name}.");
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
        // Try to create from type cache
        if (_strategyCache.TryGetValue(key, out var strategyType))
        {
            var instance = CreateStrategyInstance(strategyType);
            if (instance != null)
            { 
                _instanceCache[key] = instance;
                return instance;
            }
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
    
    private  IScheduleJobStrategy CreateStrategyInstance(Type strategyType)
    {
        try
        {
            // get from DI container 
            var serviceFromDi = _serviceProvider.GetRequiredService(strategyType);
            if (serviceFromDi != null)
            {
                return (IScheduleJobStrategy)serviceFromDi;
            }
            return Activator.CreateInstance(strategyType) as IScheduleJobStrategy;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create instance of {strategyType.Name}: {ex.Message}", ex);
        }
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