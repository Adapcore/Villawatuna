using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HotelManagement.Enums;

namespace HotelManagement.Models.Entities
{
	public class EmployeeLeave : BaseEntity
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int ID { get; set; }

		[Required]
		public int EmployeeId { get; set; }

		[ForeignKey(nameof(EmployeeId))]
		public Employee? Employee { get; set; }

		[Required]
		public LeaveType Type { get; set; }

		[Required]
		public DateTime RequestDate { get; set; }

		[Required]
		public DateTime FromDate { get; set; }

		[Required]
		public DateTime ToDate { get; set; }

		[Required]
		[Column(TypeName = "decimal(3,1)")]
		public decimal NoOfDays { get; set; }

		[Required]
		public LeaveDuration Duration { get; set; }

		public HalfDaySession? HalfDaySession { get; set; }

		[Required]
		[MaxLength(100)]
		public string Reason { get; set; } = string.Empty;

		[MaxLength(100)]
		public string? Comment { get; set; }

		[Required]
		public LeaveStatus Status { get; set; }

		public DateTime? ApproveRejectAt { get; set; }

		public int? ApproveRejectUserId { get; set; }
	}
}

