using HotelManagement.Data;
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
        private readonly ICurrencyExchangeService _currencyExchangeService;
        private readonly HotelContext _context;

        public InvoicesApiController(
            IInvoiceService invoiceService,
            IPaymentService paymentService,
            ICurrencyExchangeService currencyExchangeService,
            HotelContext context)
        {
            _invoiceService = invoiceService;
            _paymentService = paymentService;
            _currencyExchangeService = currencyExchangeService;
            _context = context;
        }

        [HttpPost("Save")]
        public async Task<IActionResult> Save([FromBody] CreateInvoiceViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (model.Paid < 0)
                return BadRequest(ModelState);

            // Validate that invoice has at least one valid item
            if (model.InvoiceDetails == null || !model.InvoiceDetails.Any() || 
                !model.InvoiceDetails.Any(d => d.ItemId > 0 && d.Quantity > 0)) //&& d.Amount > 0
            {
                return BadRequest(new { message = "Invoice must have at least one valid item with ItemId and Quantity greater than zero." });
            }

            // Validate payment reference for non-cash methods when paying
            if (model.Paid > 0)
            {
                var selectedMethod = (PaymentMethod)(model.PaymentType == 0 ? (int)PaymentMethod.Cash : model.PaymentType);
                if ((selectedMethod == PaymentMethod.Card || selectedMethod == PaymentMethod.BankTransfer) && string.IsNullOrWhiteSpace(model.PaymentReference))
                {
                    return BadRequest(new { message = "Payment Reference is required for the selected payment method." });
                }
            }

            var hasForeignCurrencyChange =
                model.Paid > 0 &&
                model.PaidCurrency == PaidCurrency.SelectedCurrency &&
                !string.Equals(model.Currency, "LKR", StringComparison.OrdinalIgnoreCase) &&
                model.CurryChange.GetValueOrDefault() > 0;

            var settlementCurrency = model.BalanceSettlementCurrency?.Trim().ToUpperInvariant();
            if (hasForeignCurrencyChange)
            {
                var foreignCurrency = model.Currency?.Trim().ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(settlementCurrency))
                    return BadRequest(new { message = "Please select a balance settlement currency." });

                if (settlementCurrency != "LKR" && settlementCurrency != foreignCurrency)
                    return BadRequest(new { message = "The selected balance settlement currency is invalid." });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            // Get the existing invoice
            Invoice? invoice = await _invoiceService.GetByIdAsync(model.InvoiceNo);

            if (invoice == null)
            {
                invoice = CreateInvoiceViewModel.ConvertToInvoice(model);

                if (invoice.TotalPaid > invoice.GrossAmount)
                    return BadRequest(ModelState);

                else if (invoice.TotalPaid == invoice.GrossAmount && invoice.Status == InvoiceStatus.Complete)
                    invoice.Status = InvoiceStatus.Paid;

                else if (invoice.TotalPaid > 0)
                    invoice.Status = InvoiceStatus.PartiallyPaid;

                invoice.Balance = invoice.GrossAmount - invoice.TotalPaid;
                invoice.CurryBalance = invoice.Balance / invoice.CurrencyRate;
                invoice.PaidCurrency = (PaidCurrency)model.PaidCurrency;
                invoice.CurryGrossAmount = model.CurryGrossAmount;
                invoice.CurryLastPaid = model.CurryLastPaid;
                invoice.CurryChange = model.CurryChange;
                invoice.CurryTotalPaid = model.CurryTotalPaid ?? 0;

                await _invoiceService.CreateAsync(invoice);

                if (model.Paid > 0)
                {
                    var method = (InvoicePaymentType)(model.PaymentType == 0 ? (int)InvoicePaymentType.Cash : model.PaymentType);
                    await _paymentService.AddPaymentForInvoiceAsync(invoice.InvoiceNo, model.Paid, method, model.PaymentReference, model.CurryPaid, model.PaidCurrency);
                    invoice.LastPaymentType = method;
                }
            }
            else
            {
                // Update header fields
                invoice.Date = model.Date;
                invoice.ReferenceNo = model.ReferenceNo;
                invoice.CustomerId = model.CustomerId;
                invoice.Note = model.Note;
                invoice.Status = (InvoiceStatus)model.Status;                
                invoice.Currency = model.Currency ?? invoice.Currency;
                invoice.CurrencyRate = model.CurrencyRate ?? invoice.CurrencyRate;
                invoice.CurySubTotal = model.CurySubTotal;
                invoice.SubTotal = model.SubTotal;
                invoice.ServiceCharge = model.ServiceCharge;
                invoice.GrossAmount = model.GrossAmount;
                invoice.LastPaid = model.Cash;
                invoice.Change = model.Change;
                invoice.LastPaymentType = (InvoicePaymentType)model.PaymentType;
                invoice.PaidCurrency = (PaidCurrency)model.PaidCurrency;
                invoice.CurryGrossAmount = model.CurryGrossAmount;
                invoice.CurryLastPaid = model.CurryLastPaid;
                invoice.CurryChange = model.CurryChange;
                invoice.CurryTotalPaid = model.CurryTotalPaid ?? 0;
                invoice.Balance = invoice.GrossAmount - invoice.TotalPaid;
                invoice.CurryBalance = invoice.Balance / invoice.CurrencyRate;

                if (model.Paid > 0)
                {
                    if (model.Paid > invoice.Balance)
                        return BadRequest(ModelState);

                    else if (model.Paid == invoice.Balance)
                        invoice.Status = InvoiceStatus.Paid;

                    else
                        invoice.Status = InvoiceStatus.PartiallyPaid;

                    invoice.Balance = invoice.Balance - model.Paid;
                    invoice.CurryBalance = invoice.Balance == 0 ? 0 : invoice.Balance / invoice.CurrencyRate;
                    invoice.TotalPaid = invoice.GrossAmount - invoice.Balance;
                    invoice.CurryTotalPaid = invoice.TotalPaid == 0 ? 0 : invoice.TotalPaid / invoice.CurrencyRate;
                }
                else if(invoice.TotalPaid == invoice.GrossAmount && invoice.Status == InvoiceStatus.Complete)
                {
                    invoice.Status = InvoiceStatus.Paid;
                }

                // Delete existing details before adding new ones
                await _invoiceService.DeleteInvoiceDetailsAsync(invoice.InvoiceNo);

                // Add new details from the model
                invoice.InvoiceDetails = model.InvoiceDetails.Select((d, index) => new InvoiceDetail
                {
                    LineNumber = index + 1,
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
                    InvoicePaymentType paymentType = (InvoicePaymentType)(model.PaymentType == 0 ? (int)InvoicePaymentType.Cash : model.PaymentType);
                    await _paymentService.AddPaymentForInvoiceAsync(invoice.InvoiceNo, model.Paid, paymentType, model.PaymentReference, model.CurryPaid, model.PaidCurrency);
                }
            }

            if (hasForeignCurrencyChange && settlementCurrency == "LKR")
            {
                if (invoice.ExchangeId.HasValue)
                    return BadRequest(new { message = "A balance-settlement exchange is already linked to this invoice." });

                var exchange = new CurrencyExchange
                {
                    Date = DateTime.Today,
                    CustomerId = invoice.CustomerId,
                    ForeignCurrency = invoice.Currency,
                    ExchangeRate = invoice.CurrencyRate.GetValueOrDefault(),
                    ForeignAmount = model.CurryChange!.Value,
                    IsInvoiceBalanceExchange = true,
                    Remarks = $"Balance settlement for Invoice #{invoice.InvoiceNo}"
                };

                if (exchange.ExchangeRate <= 0)
                    return BadRequest(new { message = "A valid currency exchange rate is required for LKR settlement." });

                await _currencyExchangeService.AddForInvoiceBalanceSettlementAsync(exchange, invoice);
            }

            await transaction.CommitAsync();
            return Ok(new { success = true, invoice = new CreateInvoiceViewModel(invoice) });
        }

        //[Obsolete]
        //[HttpPost("update")]
        //public async Task<IActionResult> Update([FromBody] CreateInvoiceViewModel model)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);

        //    if (model.Paid < 0)
        //        return BadRequest(ModelState);

        //    // Get the existing invoice
        //    var invoice = await _invoiceService.GetByIdAsync(model.InvoiceNo);
        //    if (invoice == null)
        //        return NotFound(new { message = "Invoice not found." });

        //    // Update header fields
        //    invoice.Date = model.Date;
        //    invoice.ReferenceNo = model.ReferenceNo;
        //    invoice.CustomerId = model.CustomerId;
        //    invoice.Note = model.Note;
        //    invoice.Status = (InvoiceStatus)model.Status;
        //    invoice.CurySubTotal = model.CurySubTotal;
        //    invoice.SubTotal = model.SubTotal;
        //    invoice.ServiceCharge = model.ServiceCharge;
        //    invoice.GrossAmount = model.GrossAmount;

        //    if (model.Paid > 0)
        //    {
        //        if (model.Paid > invoice.Balance)
        //            return BadRequest(ModelState);

        //        else if (model.Paid == invoice.Balance)
        //            invoice.Status = InvoiceStatus.Paid;

        //        else
        //            invoice.Status = InvoiceStatus.PartiallyPaid;

        //        invoice.Balance = invoice.Balance - model.Paid;
        //        invoice.TotalPaid = invoice.GrossAmount - invoice.Balance;
        //    }

        //    // Delete existing details before adding new ones
        //    await _invoiceService.DeleteInvoiceDetailsAsync(invoice.InvoiceNo);

        //    // Add new details from the model
        //    invoice.InvoiceDetails = model.InvoiceDetails.Select(d => new InvoiceDetail
        //    {
        //        ItemId = d.ItemId,
        //        Note = d.Note,
        //        CheckIn = d.CheckIn,
        //        CheckOut = d.CheckOut,
        //        Quantity = d.Quantity,
        //        UnitPrice = d.UnitPrice,
        //        Amount = d.Amount
        //    }).ToList();

        //    // Save changes
        //    await _invoiceService.UpdateInvoiceAsync(invoice);

        //    // Add a payment record
        //    if (model.Paid > 0)
        //    {
        //        InvoicePaymentType paymentType = (InvoicePaymentType)(model.PaymentType == 0 ? (int)InvoicePaymentType.Cash : model.PaymentType);
        //        await _paymentService.AddPaymentForInvoiceAsync(invoice.InvoiceNo, model.Paid, paymentType, model.PaymentReference, model.CurryLastPaid, model.PaidCurrency);
        //        invoice.LastPaymentType = paymentType;
        //    }

        //    return Ok(new
        //    {
        //        success = true,
        //        message = "Invoice updated successfully",
        //        invoiceNo = invoice.InvoiceNo
        //    });
        //}

    }

}
