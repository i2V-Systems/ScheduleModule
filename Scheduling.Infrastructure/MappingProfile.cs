using AutoMapper;
using Scheduling.Contracts.Schedule.DTOs;

namespace Infrastructure;

public class MappingProfile: Profile
{
    public MappingProfile()
    {
        CreateMap<Domain.Schedule.Schedule, ScheduleDto>();
        CreateMap<ScheduleDto, Domain.Schedule.Schedule>();
    }
}