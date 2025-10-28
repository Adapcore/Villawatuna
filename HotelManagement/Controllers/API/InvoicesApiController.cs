using HotelManagement.Enums;
using HotelManagement.Models.Entities;
using HotelManagement.Models.ViewModels;
using HotelManagement.Services.Interface;
using HotelManagement.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace HotelManagement.Controllers.API
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoicesApiController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IPaymentService _paymentService;

        public InvoicesApiController(IInvoiceService invoiceService, IPaymentService paymentService)
        {
            _invoiceService = invoiceService;
            _paymentService = paymentService;
        }

        [HttpPost("Save")]
        public async Task<IActionResult> Save([FromBody] CreateInvoiceViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (model.Paid < 0)
                return BadRequest(ModelState);

            // Get the existing invoice
            Invoice invoice = await _invoiceService.GetByIdAsync(model.InvoiceNo);

            if (invoice == null)
            {
                invoice = CreateInvoiceViewModel.ConvertToInvoice(model);

                if (invoice.TotalPaid > invoice.GrossAmount)
                    return BadRequest(ModelState);

                else if (invoice.TotalPaid == invoice.GrossAmount)
                    invoice.Status = InvoiceStatus.Paid;

                else if (invoice.TotalPaid > 0)
                    invoice.Status = InvoiceStatus.PartiallyPaid;

                invoice.Balance = invoice.GrossAmount - invoice.TotalPaid;

                await _invoiceService.CreateAsync(invoice);

                if (model.Paid > 0)
                    await _paymentService.AddPaymentForInvoiceAsync(invoice.InvoiceNo, model.Paid);
            }
            else
            {
                // Update header fields
                invoice.Date = model.Date;
                invoice.ReferenceNo = model.ReferenceNo;
                invoice.CustomerId = model.CustomerId;
                invoice.Note = model.Note;
                invoice.Status = (InvoiceStatus)model.Status;
                invoice.CurySubTotal = model.CurySubTotal;
                invoice.SubTotal = model.SubTotal;
                invoice.ServiceCharge = model.ServiceCharge;
                invoice.GrossAmount = model.GrossAmount;
                invoice.LastPaid = model.Cash;
                invoice.Change = model.Change;

                if (model.Paid > 0)
                {
                    if (model.Paid > invoice.Balance)
                        return BadRequest(ModelState);

                    else if (model.Paid == invoice.Balance)
                        invoice.Status = InvoiceStatus.Paid;

                    else
                        invoice.Status = InvoiceStatus.PartiallyPaid;

                    invoice.Balance = invoice.Balance - model.Paid;
                    invoice.TotalPaid = invoice.GrossAmount - invoice.Balance;
                }

                // Delete existing details before adding new ones
                await _invoiceService.DeleteInvoiceDetailsAsync(invoice.InvoiceNo);

                // Add new details from the model
                invoice.InvoiceDetails = model.InvoiceDetails.Select(d => new InvoiceDetail
                {
                    ItemId = d.ItemId,
                    Note = d.Note,
                    CheckIn = d.CheckIn,
                    CheckOut = d.CheckOut,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Amount = d.Amount
                }).ToList();

                // Save changes
                await _invoiceService.UpdateInvoiceAsync(invoice);

                // Add a payment record
                if (model.Paid > 0)
                    await _paymentService.AddPaymentForInvoiceAsync(invoice.InvoiceNo, model.Paid);

            }

            return Ok(new { success = true, invoice = new CreateInvoiceViewModel(invoice) });
        }

        [Obsolete]
        [HttpPost("update")]
        public async Task<IActionResult> Update([FromBody] CreateInvoiceViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (model.Paid < 0)
                return BadRequest(ModelState);

            // Get the existing invoice
            var invoice = await _invoiceService.GetByIdAsync(model.InvoiceNo);
            if (invoice == null)
                return NotFound(new { message = "Invoice not found." });

            // Update header fields
            invoice.Date = model.Date;
            invoice.ReferenceNo = model.ReferenceNo;
            invoice.CustomerId = model.CustomerId;
            invoice.Note = model.Note;
            invoice.Status = (InvoiceStatus)model.Status;
            invoice.CurySubTotal = model.CurySubTotal;
            invoice.SubTotal = model.SubTotal;
            invoice.ServiceCharge = model.ServiceCharge;
            invoice.GrossAmount = model.GrossAmount;

            if (model.Paid > 0)
            {
                if (model.Paid > invoice.Balance)
                    return BadRequest(ModelState);

                else if (model.Paid == invoice.Balance)
                    invoice.Status = InvoiceStatus.Paid;

                else
                    invoice.Status = InvoiceStatus.PartiallyPaid;

                invoice.Balance = invoice.Balance - model.Paid;
                invoice.TotalPaid = invoice.GrossAmount - invoice.Balance;
            }

            // Delete existing details before adding new ones
            await _invoiceService.DeleteInvoiceDetailsAsync(invoice.InvoiceNo);

            // Add new details from the model
            invoice.InvoiceDetails = model.InvoiceDetails.Select(d => new InvoiceDetail
            {
                ItemId = d.ItemId,
                Note = d.Note,
                CheckIn = d.CheckIn,
                CheckOut = d.CheckOut,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                Amount = d.Amount
            }).ToList();

            // Save changes
            await _invoiceService.UpdateInvoiceAsync(invoice);

            // Add a payment record
            if (model.Paid > 0)
            {
                await _paymentService.AddPaymentForInvoiceAsync(invoice.InvoiceNo, model.Paid);
            }

            return Ok(new
            {
                success = true,
                message = "Invoice updated successfully",
                invoiceNo = invoice.InvoiceNo
            });
        }

    }

}
