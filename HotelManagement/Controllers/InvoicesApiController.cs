using HotelManagement.Enums;
using HotelManagement.Models.Entities;
using HotelManagement.Models.ViewModels;
using HotelManagement.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Controllers
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
    }

}
