using AutoMapper;
using Domain.AttachedResources;
using Scheduling.Contracts.AttachedResources.DTOs;
using Scheduling.Contracts.Schedule.DTOs;

namespace Infrastructure;

public class MappingProfile: Profile
{
    public MappingProfile()
    {
        CreateMap<Domain.Schedule.Schedule, ScheduleDto>();
        CreateMap<ScheduleDto, Domain.Schedule.Schedule>();
        CreateMap<Domain.AttachedResources.ScheduleResourceMapping, ScheduleResourceDto>();
        CreateMap<ScheduleResourceDto, Domain.AttachedResources.ScheduleResourceMapping>();
    }
}