using HotelManagement.Data;
using HotelManagement.Enums;
using HotelManagement.Models.Entities;
using HotelManagement.Services.Interface;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using X.PagedList.Extensions;

namespace HotelManagement.Services
{
    public class CurrencyExchangeService : ICurrencyExchangeService
    {
        public const string ExchangeExpenseTypeName = "Exchange";

        private readonly HotelContext _context;
        private readonly ICurrencyService _currencyService;
        private readonly ICustomerService _customerService;
        private readonly IExchangeTypeService _exchangeTypeService;
        private readonly IExpenseTypeService _expenseTypeService;

        public CurrencyExchangeService(
            HotelContext context,
            ICurrencyService currencyService,
            ICustomerService customerService,
            IExchangeTypeService exchangeTypeService,
            IExpenseTypeService expenseTypeService)
        {
            _context = context;
            _currencyService = currencyService;
            _customerService = customerService;
            _exchangeTypeService = exchangeTypeService;
            _expenseTypeService = expenseTypeService;
        }

        public async Task<IEnumerable<CurrencyExchange>> GetAllAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            int? customerId = null,
            int? foreignCurrencyId = null)
        {
            var query = BuildFilteredQuery(startDate, endDate, customerId, foreignCurrencyId)
                .OrderByDescending(e => e.Id);

            var exchanges = await query.ToListAsync();
            await AttachCurrenciesAsync(exchanges);
            return exchanges;
        }

        public async Task<(IPagedList<CurrencyExchange> Items, decimal TotalLkr)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int? customerId = null,
            int? foreignCurrencyId = null)
        {
            var filtered = BuildFilteredQuery(startDate, endDate, customerId, foreignCurrencyId);

            var totalLkr = await filtered.SumAsync(e => (decimal?)e.LKRAmount) ?? 0m;

            var paged = filtered
                .OrderByDescending(e => e.Id)
                .ToPagedList(pageNumber, pageSize);

            await AttachCurrenciesAsync(paged);

            return (paged, totalLkr);
        }

        public async Task<CurrencyExchange?> GetByIdAsync(int id)
        {
            var exchange = await _context.CurrencyExchanges
                .Include(e => e.Customer)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exchange == null)
                return null;

            await AttachCurrenciesAsync(new[] { exchange });
            return exchange;
        }

        private IQueryable<CurrencyExchange> BuildFilteredQuery(
            DateTime? startDate,
            DateTime? endDate,
            int? customerId,
            int? foreignCurrencyId)
        {
            var query = _context.CurrencyExchanges
                .AsNoTracking()
                .Include(e => e.Customer)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(e => e.Date >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(e => e.Date < endDate.Value.Date.AddDays(1));

            if (customerId.HasValue && customerId.Value > 0)
                query = query.Where(e => e.CustomerId == customerId.Value);

            if (foreignCurrencyId.HasValue && foreignCurrencyId.Value > 0)
                query = query.Where(e => e.ForeignCurrencyId == foreignCurrencyId.Value);

            return query;
        }

        private async Task AttachCurrenciesAsync(IEnumerable<CurrencyExchange> exchanges)
        {
            var currencies = (await _currencyService.GetCurencyTypesAsync()).ToList();
            foreach (var item in exchanges)
            {
                item.ForeignCurrency = currencies.FirstOrDefault(c => c.Id == item.ForeignCurrencyId);
            }
        }

        public async Task AddAsync(CurrencyExchange exchange)
        {
            PrepareForSave(exchange);

            var catalog = await ResolveCatalogAsync(exchange);

            _context.CurrencyExchanges.Add(exchange);
            await _context.SaveChangesAsync();

            await UpsertLinkedRecordsAsync(exchange, catalog, isNew: true);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(CurrencyExchange exchange)
        {
            var existing = await _context.CurrencyExchanges.FirstOrDefaultAsync(e => e.Id == exchange.Id);
            if (existing == null)
                throw new InvalidOperationException("Currency exchange not found.");

            existing.Date = exchange.Date.Date;
            existing.CustomerId = exchange.CustomerId;
            existing.ForeignCurrencyId = exchange.ForeignCurrencyId;
            existing.ExchangeRate = exchange.ExchangeRate;
            existing.ForeignAmount = exchange.ForeignAmount;
            existing.LKRAmount = CalculateLkrAmount(exchange.ExchangeRate, exchange.ForeignAmount);
            existing.Remarks = string.IsNullOrWhiteSpace(exchange.Remarks) ? null : exchange.Remarks.Trim();
            existing.ForeignCurrency = null;

            var catalog = await ResolveCatalogAsync(existing);
            await UpsertLinkedRecordsAsync(existing, catalog, isNew: false);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var existing = await _context.CurrencyExchanges.FirstOrDefaultAsync(e => e.Id == id);
            if (existing == null)
                return;

            var invoiceNo = existing.InvoiceNo;
            var expenseId = existing.ExpenseId;

            existing.InvoiceNo = null;
            existing.ExpenseId = null;
            await _context.SaveChangesAsync();

            if (invoiceNo.HasValue)
            {
                var invoice = await _context.Invoices
                    .Include(i => i.InvoiceDetails)
                    .Include(i => i.Payments)
                    .FirstOrDefaultAsync(i => i.InvoiceNo == invoiceNo.Value);

                if (invoice != null)
                {
                    if (invoice.InvoiceDetails?.Any() == true)
                        _context.InvoiceDetails.RemoveRange(invoice.InvoiceDetails);
                    if (invoice.Payments?.Any() == true)
                        _context.Payments.RemoveRange(invoice.Payments);
                    _context.Invoices.Remove(invoice);
                }
            }

            if (expenseId.HasValue)
            {
                var expense = await _context.Expenses.FindAsync(expenseId.Value);
                if (expense != null)
                    _context.Expenses.Remove(expense);
            }

            _context.CurrencyExchanges.Remove(existing);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Resolves Umbraco catalog values required to create the linked invoice and expense
        /// for a currency exchange: the "Currency Exchange" invoice item id, the "Exchange"
        /// expense type id, the foreign currency code, and the customer display name.
        /// </summary>
        /// <param name="exchange">The exchange whose foreign currency and customer are resolved.</param>
        /// <returns>
        /// ItemId (Umbraco exchange item), ExpenseTypeId (Umbraco expense type),
        /// CurrencyCode, and CustomerName.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the Umbraco "Currency Exchange" item or "Exchange" expense type is missing.
        /// </exception>
        private async Task<(int ItemId, int ExpenseTypeId, string CurrencyCode, string CustomerName)> ResolveCatalogAsync(CurrencyExchange exchange)
        {
            var item = await _exchangeTypeService.GetCurrencyExchangeItemAsync();
            if (item == null)
            {
                throw new InvalidOperationException(
                    "Umbraco item 'Currency Exchange' was not found under Exchange. Please create it before saving.");
            }

            var expenseTypes = await _expenseTypeService.GetExpenseTypesAsync();
            var expenseType = expenseTypes.FirstOrDefault(t =>
                string.Equals(t.Name, ExchangeExpenseTypeName, StringComparison.OrdinalIgnoreCase));
            if (expenseType == null)
            {
                throw new InvalidOperationException(
                    "Umbraco expense type 'Exchange' was not found. Please create it before saving.");
            }

            var currencies = await _currencyService.GetCurencyTypesAsync();
            var currency = currencies.FirstOrDefault(c => c.Id == exchange.ForeignCurrencyId);
            var currencyCode = (currency?.Code ?? "USD").Trim();
            if (currencyCode.Length > 5)
                currencyCode = currencyCode.Substring(0, 5);

            var customer = exchange.Customer ?? await _customerService.GetByIdAsync(exchange.CustomerId);
            var customerName = customer == null
                ? string.Empty
                : $"{customer.FirstName} {customer.LastName}".Trim();

            return (item.Id, expenseType.Id, currencyCode, customerName);
        }

        private async Task UpsertLinkedRecordsAsync(
            CurrencyExchange exchange,
            (int ItemId, int ExpenseTypeId, string CurrencyCode, string CustomerName) catalog,
            bool isNew)
        {
            var lkrAmount = exchange.LKRAmount;
            var foreignAmount = exchange.ForeignAmount;
            var rate = exchange.ExchangeRate;
            var reference = $"EXC #{exchange.Id}";
            var note = $"{foreignAmount:0.##} {catalog.CurrencyCode} @ {rate:0.####}";

            if (!string.IsNullOrWhiteSpace(exchange.Remarks))
                note = $"{note} — {exchange.Remarks}";

            Invoice? invoice = null;
            if (!isNew && exchange.InvoiceNo.HasValue)
            {
                invoice = await _context.Invoices
                    .Include(i => i.InvoiceDetails)
                    .Include(i => i.Payments)
                    .FirstOrDefaultAsync(i => i.InvoiceNo == exchange.InvoiceNo.Value);
            }

            if (invoice == null)
            {
                invoice = CreateInvoice(exchange, catalog, reference);
                _context.Invoices.Add(invoice);
            }
            else
            {
                ApplyInvoiceValues(invoice, exchange, catalog, reference);
            }

            var detail = invoice.InvoiceDetails?.FirstOrDefault();
            if (detail == null)
            {
                detail = new InvoiceDetail { LineNumber = 1 };
                invoice.InvoiceDetails ??= new List<InvoiceDetail>();
                invoice.InvoiceDetails.Add(detail);
            }

            detail.ItemId = catalog.ItemId;
            detail.Note = note.Length > 250 ? note.Substring(0, 250) : note;
            detail.Quantity = foreignAmount;
            detail.UnitPrice = rate;
            detail.Amount = lkrAmount;

            var payment = invoice.Payments?.FirstOrDefault();
            if (payment == null)
            {
                payment = new Payment();
                invoice.Payments ??= new List<Payment>();
                invoice.Payments.Add(payment);
            }

            payment.Date = exchange.Date.Date;
            payment.Amount = lkrAmount;
            payment.CurryAmount = foreignAmount;
            payment.Type = InvoicePaymentType.Cash;
            payment.PaidCurrency = PaidCurrency.SelectedCurrency;
            payment.Reference = reference;

            Expense? expense = null;
            if (!isNew && exchange.ExpenseId.HasValue)
            {
                expense = await _context.Expenses.FindAsync(exchange.ExpenseId.Value);
            }

            if (expense == null)
            {
                expense = CreateExpense(exchange, catalog, note);
                _context.Expenses.Add(expense);
            }
            else
            {
                ApplyExpenseValues(expense, exchange, catalog, note);
            }

            await _context.SaveChangesAsync();

            exchange.InvoiceNo = invoice.InvoiceNo;
            exchange.ExpenseId = expense.ID;
        }

        private static Invoice CreateInvoice(
            CurrencyExchange exchange,
            (int ItemId, int ExpenseTypeId, string CurrencyCode, string CustomerName) catalog,
            string reference)
        {
            var invoice = new Invoice
            {
                InvoiceDetails = new List<InvoiceDetail>(),
                Payments = new List<Payment>()
            };
            ApplyInvoiceValues(invoice, exchange, catalog, reference);
            return invoice;
        }

        private static void ApplyInvoiceValues(
            Invoice invoice,
            CurrencyExchange exchange,
            (int ItemId, int ExpenseTypeId, string CurrencyCode, string CustomerName) catalog,
            string reference)
        {
            invoice.Date = exchange.Date.Date;
            invoice.Type = InvoiceType.Exchange;
            invoice.Currency = catalog.CurrencyCode;
            invoice.ReferenceNo = reference;
            invoice.CustomerId = exchange.CustomerId;
            invoice.Status = InvoiceStatus.Paid;
            invoice.Note = exchange.Remarks;
            invoice.CurySubTotal = exchange.ForeignAmount;
            invoice.SubTotal = exchange.LKRAmount;
            invoice.ServiceCharge = 0;
            invoice.GrossAmount = exchange.LKRAmount;
            invoice.TotalPaid = exchange.LKRAmount;
            invoice.CurryTotalPaid = exchange.ForeignAmount;
            invoice.Balance = 0;
            invoice.LastPaid = exchange.LKRAmount;
            invoice.Change = 0;
            invoice.CurryGrossAmount = exchange.ForeignAmount;
            invoice.CurryBalance = 0;
            invoice.CurryLastPaid = exchange.ForeignAmount;
            invoice.CurryChange = 0;
            invoice.LastPaymentType = InvoicePaymentType.Cash;
            invoice.CurrencyRate = exchange.ExchangeRate;
            invoice.PaidCurrency = PaidCurrency.SelectedCurrency;
        }

        private static Expense CreateExpense(
            CurrencyExchange exchange,
            (int ItemId, int ExpenseTypeId, string CurrencyCode, string CustomerName) catalog,
            string note)
        {
            var expense = new Expense();
            ApplyExpenseValues(expense, exchange, catalog, note);
            return expense;
        }

        private static void ApplyExpenseValues(
            Expense expense,
            CurrencyExchange exchange,
            (int ItemId, int ExpenseTypeId, string CurrencyCode, string CustomerName) catalog,
            string note)
        {
            expense.Date = exchange.Date.Date;
            expense.ExpenseTypeID = catalog.ExpenseTypeId;
            expense.PayeeName = catalog.CustomerName;
            expense.Amount = exchange.LKRAmount;
            expense.PaymentMethod = PaymentMethod.Cash;
            expense.Description = note.Length > 500 ? note.Substring(0, 500) : note;
            expense.ExpenseType = null;
        }

        private static void PrepareForSave(CurrencyExchange exchange)
        {
            // Keep FK ids only — nulling tracked navigations severs required relationships in EF.
            exchange.ForeignCurrency = null;
            exchange.Date = exchange.Date.Date;
            exchange.Remarks = string.IsNullOrWhiteSpace(exchange.Remarks) ? null : exchange.Remarks.Trim();
            exchange.LKRAmount = CalculateLkrAmount(exchange.ExchangeRate, exchange.ForeignAmount);
        }

        private static decimal CalculateLkrAmount(decimal exchangeRate, decimal foreignAmount)
        {
            return Math.Round(exchangeRate * foreignAmount, 2, MidpointRounding.AwayFromZero);
        }
    }
}
