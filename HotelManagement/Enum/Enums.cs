using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Enums
{
    public enum InvoiceType
    {
        [Display(Name = "Dining")]
        Dining = 1,

        [Display(Name = "Take Away")]
        TakeAway = 2,

        [Display(Name = "Stay")]
        Stay = 3,

        [Display(Name = "Other")]
        Other = 4,

        [Display(Name = "Tour")]
        Tour = 5
    }

    public enum InvoiceStatus
    {
        [Display(Name = "In Progress")]
        InProgress = 1,

        [Display(Name = "Partially Paid")]
        PartiallyPaid = 3,

        [Display(Name = "Paid")]
        Paid = 4,

        [Display(Name = "Complete")]
        Complete = 2
    }

    public enum PaymentMethod
    {
        [Display(Name = "Cash")]
        Cash = 1,

        [Display(Name = "Bank Transfer")]
        BankTransfer = 2,

        [Display(Name = "Card")]
        Card = 3,

        [Display(Name = "Cheque")]
        Cheque = 4
    }
}
