using CommonUtilityModule.Models;
using MassTransit;

namespace Scheduling.Contracts;


public abstract class BaseEntity : IEntityBase
{
    public Guid Id { get; set; }
    
    protected BaseEntity()
    {
        Id =  NewId.NextSequentialGuid();
    }
    
    public static Guid GenerateGuid()
    {
        return NewId.NextSequentialGuid();
    }

    protected BaseEntity(Guid id)
    {
        Id = id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (obj is not BaseEntity other) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id.Equals(other.Id);
    }
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(BaseEntity? left, BaseEntity? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(BaseEntity? left, BaseEntity? right)
    {
        return !Equals(left, right);
    }
    
}