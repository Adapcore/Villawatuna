using HotelManagement.Data;
using HotelManagement.Enums;
using HotelManagement.Models.Entities;
using HotelManagement.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly HotelContext _context;

        public InvoiceService(HotelContext context)
        {
            _context = context;
        }

        public async Task<List<Invoice>> GetAllInvoicesAsync()
        {
            return await _context.Invoices
                .Include(i => i.Customer)
                .OrderByDescending(i => i.Date)
                .ToListAsync();
        }

        public async Task<Invoice?> GetByIdAsync(int invoiceNo)
        {
            return await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.InvoiceDetails)
                .FirstOrDefaultAsync(i => i.InvoiceNo == invoiceNo);
        }

        public async Task<Invoice> CreateAsync(Invoice invoice)
        {
            // Set line numbers for invoice details
            int lineNumber = 1;
            foreach (var detail in invoice.InvoiceDetails)
            {
                detail.LineNumber = lineNumber++;
            }

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();
            return invoice;
        }

        //public async Task<int> CreateInvoiceAsync(CreateInvoiceViewModel model)
        //{
        //    var invoice = new Invoice
        //    {
        //        OrderNo = model.OrderNo,
        //        Date = model.Date,
        //        Type = model.Type,
        //        ReferenceNo = model.ReferenceNo,
        //        CustomerId = model.CustomerId,
        //        Status = InvoiceStatus.InProgress,
        //        Note = model.Note,
        //        SubTotal = model.SubTotal,
        //        ServiceCharge = model.ServiceCharge,
        //        GrossAmount = model.GrossAmount,
        //        Paid = 0,
        //        Balance = model.GrossAmount,
        //        InvoiceDetails = new List<InvoiceDetail>()
        //    };

        //    int lineNo = 1;
        //    foreach (var d in model.InvoiceDetails)
        //    {
        //        invoice.InvoiceDetails.Add(new InvoiceDetail
        //        {
        //            LineNumber = lineNo++,
        //            ItemId = d.ItemId,
        //            Note = d.Note,
        //            CheckIn = d.CheckIn,
        //            CheckOut = d.CheckOut,
        //            Quantity = d.Quantity,
        //            UnitPrice = d.UnitPrice,
        //            Amount = d.Amount
        //        });
        //    }

        //    _context.Invoices.Add(invoice);
        //    await _context.SaveChangesAsync();

        //    return invoice.InvoiceNo;
        //}

        public async Task UpdateInvoiceAsync(Invoice invoice)
        {
            _context.Entry(invoice).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteInvoiceAsync(int invoiceNo)
        {
            var invoice = await _context.Invoices.FindAsync(invoiceNo);
            if (invoice != null)
            {
                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();
            }
        }
    }
}
