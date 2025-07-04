using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Application.Schedule.ScheduleObj.Interfaces
{
    public interface ISchedulesEntityBase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        Guid Id { get; set; }
    }
}
