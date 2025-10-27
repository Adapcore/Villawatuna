﻿using HotelManagement.Enums;
using HotelManagement.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Models.ViewModels
{
    public class CreateInvoiceViewModel
    {
        public int InvoiceNo { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public int Type { get; set; }
        public string Currency { get; set; }

        public string? ReferenceNo { get; set; }
        public int Status { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        public decimal CurySubTotal { get; set; }
        public decimal SubTotal { get; set; }
        public decimal ServiceCharge { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal Paid { get; set; }
        public decimal Balance { get; set; }
        public List<CreateInvoiceDetailViewModel> InvoiceDetails { get; set; } = new();

        public CreateInvoiceViewModel()
        {
        }

        public CreateInvoiceViewModel(Invoice invoice)
        {
            InvoiceNo = invoice.InvoiceNo;
            Date = invoice.Date;
            Type = (int)invoice.Type;
            Currency = invoice.Currency;
            ReferenceNo = invoice.ReferenceNo;
            CustomerId = invoice.CustomerId;
            Note = invoice.Note;
            CurySubTotal = invoice.CurySubTotal;
            SubTotal = invoice.SubTotal;
            ServiceCharge = invoice.ServiceCharge;
            GrossAmount = invoice.GrossAmount;
            Status = (int)invoice.Status;
            Paid = invoice.Paid;
            Balance = invoice.GrossAmount - invoice.Paid;
        }

        public static Invoice ConvertToInvoice(CreateInvoiceViewModel model)
        {
            Invoice invoice = new Invoice
            {
                InvoiceNo = model.InvoiceNo,
                Date = model.Date,
                Type = Enum.Parse<InvoiceType>(model.Type.ToString()),
                Currency = model.Currency,
                ReferenceNo = model.ReferenceNo,
                CustomerId = model.CustomerId,
                Note = model.Note,
                CurySubTotal = model.CurySubTotal,
                SubTotal = model.SubTotal,
                ServiceCharge = model.ServiceCharge,
                GrossAmount = model.GrossAmount,
                Status = InvoiceStatus.InProgress,
                Paid = model.Paid,
                InvoiceDetails = model.InvoiceDetails.Select((d, index) => new InvoiceDetail
                {
                    LineNumber = index + 1,
                    ItemId = d.ItemId,
                    Note = d.Note,
                    CheckIn = d.CheckIn,
                    CheckOut = d.CheckOut,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Amount = d.Amount
                }).ToList()
            };

            return invoice;
        }

    }

    public class CreateInvoiceDetailViewModel
    {
        public int ItemId { get; set; }
        public string? Note { get; set; }
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
    }
}
