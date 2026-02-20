using AutoMapper;
using Domain.AttachedResources;
using Scheduling.Contracts.AttachedResources.DTOs;
using Scheduling.Contracts.Schedule.DTOs;

namespace Infrastructure;

public class MappingProfile: Profile
{
    public MappingProfile()
    {
        CreateMap<Domain.Scheduling.Schedule, ScheduleDto>();
        CreateMap<ScheduleDto, Domain.Scheduling.Schedule>();
        CreateMap<Domain.AttachedResources.ScheduleResourceMapping, ScheduleResourceDto>();
        CreateMap<ScheduleResourceDto, Domain.AttachedResources.ScheduleResourceMapping>();
    }
}