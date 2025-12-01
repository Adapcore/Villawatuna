using System.ComponentModel.DataAnnotations.Schema;
using HotelManagement.Models.DTO;

namespace HotelManagement.Models.Entities
{
    public abstract class BaseEntity
    {
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public int? LastModifiedBy { get; set; }
        
        // Non-mapped property for Umbraco member creator info
        [NotMapped]
        public MemberDTO CreatedByMember { get; set; }
    }

}
