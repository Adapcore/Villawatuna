using HotelManagement.Enums;
using HotelManagement.Models.Entities;
using HotelManagement.Models.ViewModels;
using HotelManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Controllers.API
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoicesApiController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoicesApiController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] CreateInvoiceViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var invoice = new Invoice
            {
                Date = model.Date,
                Type = Enum.Parse<InvoiceType>(model.Type.ToString()),
                ReferenceNo = model.ReferenceNo,
                CustomerId = model.CustomerId,
                Note = model.Note,
                SubTotal = model.SubTotal,
                ServiceCharge = model.ServiceCharge,
                GrossAmount = model.GrossAmount,
                Status = InvoiceStatus.InProgress,
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

            await _invoiceService.CreateAsync(invoice);

            return Ok(new { success = true, invoiceNo = invoice.InvoiceNo });
        }

        [HttpPost("update")]
        public async Task<IActionResult> Update([FromBody] CreateInvoiceViewModel model)
        {
            if (!ModelState.IsValid)
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
            invoice.SubTotal = model.SubTotal;
            invoice.ServiceCharge = model.ServiceCharge;
            invoice.GrossAmount = model.GrossAmount;

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

            return Ok(new
            {
                success = true,
                message = "Invoice updated successfully",
                invoiceNo = invoice.InvoiceNo
            });
        }

    }

}
