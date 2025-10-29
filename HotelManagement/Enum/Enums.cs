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
        [Display(Name = "In Progress / Open")]
        InProgress = 1,

        [Display(Name = "Complete")]
        Complete = 2,

        [Display(Name = "Partially Paid")]
        PartiallyPaid = 3,

        [Display(Name = "Paid")]
        Paid = 4
    }

    public enum InvoicePaymentType
    {
        [Display(Name = "Cash")]
        Cash = 1,

        [Display(Name = "Card")]
        Card = 2,

        [Display(Name = "Bank Transfer")]
        BankTransfer = 3
    }

    public enum PaymentMethod
    {
        [Display(Name = "Cash")]
        Cash = 1,

        [Display(Name = "Card")]
        Card = 2,

        [Display(Name = "Bank Transfer")]
        BankTransfer = 3
    }
}
