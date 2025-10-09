using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Enums
{
    public enum InvoiceType
    {
        [Display(Name = "Dining")]
        Dining = 1,

        [Display(Name = "TakeAway")]
        TakeAway = 2,

        [Display(Name = "Stay")]
        Stay = 3,

        [Display(Name = "Other")]
        Other = 4
    }

    public enum InvoiceStatus
    {
        [Display(Name = "InProgress")]
        InProgress = 1,

        [Display(Name = "Complete")]
        Complete = 2,

        [Display(Name = "PartiallyPaid")]
        PartiallyPaid = 3,

        [Display(Name = "Paid")]
        Paid = 4
    }
}
