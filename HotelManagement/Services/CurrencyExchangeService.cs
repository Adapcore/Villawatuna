using HotelManagement.Data;
using HotelManagement.Enums;
using HotelManagement.Models.Entities;
using HotelManagement.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using X.PagedList;
using X.PagedList.Extensions;

namespace HotelManagement.Services
{
    public class CurrencyExchangeService : ICurrencyExchangeService
    {
        public const string ExchangeExpenseTypeName = "Exchange";

        private readonly HotelContext _context;
        private readonly ICustomerService _customerService;
        private readonly IExchangeTypeService _exchangeTypeService;
        private readonly IExpenseTypeService _expenseTypeService;

        public CurrencyExchangeService(
            HotelContext context,
            ICustomerService customerService,
            IExchangeTypeService exchangeTypeService,
            IExpenseTypeService expenseTypeService)
        {
            _context = context;
            _customerService = customerService;
            _exchangeTypeService = exchangeTypeService;
            _expenseTypeService = expenseTypeService;
        }

        public async Task<IEnumerable<CurrencyExchange>> GetAllAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            int? customerId = null,
            string? foreignCurrency = null)
        {
            var query = BuildFilteredQuery(startDate, endDate, customerId, foreignCurrency)
                .OrderByDescending(e => e.Id);

            return await query.ToListAsync();
        }

        public async Task<(IPagedList<CurrencyExchange> Items, decimal TotalLkr)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int? customerId = null,
            string? foreignCurrency = null)
        {
            var filtered = BuildFilteredQuery(startDate, endDate, customerId, foreignCurrency);

            var totalLkr = await filtered.SumAsync(e => (decimal?)e.LKRAmount) ?? 0m;

            var paged = filtered
                .OrderByDescending(e => e.Id)
                .ToPagedList(pageNumber, pageSize);

            return (paged, totalLkr);
        }

        public async Task<CurrencyExchange?> GetByIdAsync(int id)
        {
            return await _context.CurrencyExchanges
                .Include(e => e.Customer)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        private IQueryable<CurrencyExchange> BuildFilteredQuery(
            DateTime? startDate,
            DateTime? endDate,
            int? customerId,
            string? foreignCurrency)
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

            if (!string.IsNullOrWhiteSpace(foreignCurrency))
            {
                var code = foreignCurrency.Trim();
                query = query.Where(e => e.ForeignCurrency == code);
            }

            return query;
        }

        public async Task AddAsync(CurrencyExchange exchange)
        {
            PrepareForSave(exchange);

            var catalog = await ResolveCatalogAsync(exchange);

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // Save first so exchange.Id is available for "EXC #{Id}" on invoice/payment.
                _context.CurrencyExchanges.Add(exchange);
                await _context.SaveChangesAsync();

                await UpsertLinkedRecordsAsync(exchange, catalog, isNew: true);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task AddForInvoiceBalanceSettlementAsync(
            CurrencyExchange exchange,
            Invoice sourceInvoice)
        {
            PrepareForSave(exchange);
            exchange.InvoiceId = sourceInvoice.InvoiceNo;

            var catalog = await ResolveCatalogAsync(exchange);
            IDbContextTransaction? transaction = null;
            var ownsTransaction = _context.Database.CurrentTransaction == null;

            if (ownsTransaction)
                transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _context.CurrencyExchanges.Add(exchange);
                await _context.SaveChangesAsync();

                await UpsertLinkedRecordsAsync(exchange, catalog, isNew: true);
                sourceInvoice.ExchangeId = exchange.Id;
                await _context.SaveChangesAsync();

                if (transaction != null)
                    await transaction.CommitAsync();
            }
            catch
            {
                if (transaction != null)
                    await transaction.RollbackAsync();
                throw;
            }
            finally
            {
                if (transaction != null)
                    await transaction.DisposeAsync();
            }
        }

        public async Task UpdateAsync(CurrencyExchange exchange)
        {
            var existing = await _context.CurrencyExchanges.FirstOrDefaultAsync(e => e.Id == exchange.Id);
            if (existing == null)
                throw new InvalidOperationException("Currency exchange not found.");

            PrepareForSave(exchange);

            existing.Date = exchange.Date.Date;
            existing.CustomerId = exchange.CustomerId;
            existing.ForeignCurrency = exchange.ForeignCurrency;
            existing.ExchangeRate = exchange.ExchangeRate;
            existing.ForeignAmount = exchange.ForeignAmount;
            existing.LKRAmount = exchange.LKRAmount;
            existing.Remarks = exchange.Remarks;

            var catalog = await ResolveCatalogAsync(existing);

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                await UpsertLinkedRecordsAsync(existing, catalog, isNew: false);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            var existing = await _context.CurrencyExchanges.FirstOrDefaultAsync(e => e.Id == id);
            if (existing == null)
                return;

            var invoiceNo = existing.InvoiceNo;
            var sourceInvoiceId = existing.InvoiceId;
            var expenseId = existing.ExpenseId;

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                existing.InvoiceNo = null;
                existing.InvoiceId = null;
                existing.ExpenseId = null;

                if (sourceInvoiceId.HasValue)
                {
                    var sourceInvoice = await _context.Invoices.FindAsync(sourceInvoiceId.Value);
                    if (sourceInvoice?.ExchangeId == existing.Id)
                        sourceInvoice.ExchangeId = null;
                }

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
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Resolves Umbraco catalog values required to create the linked invoice and expense
        /// for a currency exchange: the "Currency Exchange" invoice item id, the "Exchange"
        /// expense type id, and the customer display name.
        /// </summary>
        /// <param name="exchange">The exchange whose customer is resolved.</param>
        /// <returns>
        /// ItemId (Umbraco exchange item), ExpenseTypeId (Umbraco expense type), and CustomerName.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the Umbraco "Currency Exchange" item or "Exchange" expense type is missing.
        /// </exception>
        private async Task<(int ItemId, int ExpenseTypeId, string CustomerName)> ResolveCatalogAsync(CurrencyExchange exchange)
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

            var customer = exchange.Customer ?? await _customerService.GetByIdAsync(exchange.CustomerId);
            var customerName = customer == null
                ? string.Empty
                : $"{customer.FirstName} {customer.LastName}".Trim();

            return (item.Id, expenseType.Id, customerName);
        }

        private async Task UpsertLinkedRecordsAsync(
            CurrencyExchange exchange,
            (int ItemId, int ExpenseTypeId, string CustomerName) catalog,
            bool isNew)
        {
            var lkrAmount = exchange.LKRAmount;
            var foreignAmount = exchange.ForeignAmount;
            var rate = exchange.ExchangeRate;
            var reference = $"EXC #{exchange.Id}";
            var note = $"{foreignAmount:0.##} {exchange.ForeignCurrency} @ {rate:0.####}";

            if (!string.IsNullOrWhiteSpace(exchange.Remarks))
                note = $"{note} — {exchange.Remarks}";

            var expenseDescription = $"{reference} — {note}";
            if (expenseDescription.Length > 500)
                expenseDescription = expenseDescription.Substring(0, 500);

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
                invoice = CreateInvoice(exchange, reference);
                _context.Invoices.Add(invoice);
            }
            else
            {
                ApplyInvoiceValues(invoice, exchange, reference);
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
                expense = CreateExpense(exchange, catalog, expenseDescription);
                _context.Expenses.Add(expense);
            }
            else
            {
                ApplyExpenseValues(expense, exchange, catalog, expenseDescription);
            }

            await _context.SaveChangesAsync();

            exchange.InvoiceNo = invoice.InvoiceNo;
            exchange.ExpenseId = expense.ID;
        }

        private static Invoice CreateInvoice(
            CurrencyExchange exchange,
            string reference)
        {
            var invoice = new Invoice
            {
                InvoiceDetails = new List<InvoiceDetail>(),
                Payments = new List<Payment>()
            };
            ApplyInvoiceValues(invoice, exchange, reference);
            return invoice;
        }

        private static void ApplyInvoiceValues(
            Invoice invoice,
            CurrencyExchange exchange,
            string reference)
        {
            invoice.Date = exchange.Date.Date;
            invoice.Type = InvoiceType.Exchange;
            invoice.Currency = exchange.ForeignCurrency;
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
            (int ItemId, int ExpenseTypeId, string CustomerName) catalog,
            string note)
        {
            var expense = new Expense();
            ApplyExpenseValues(expense, exchange, catalog, note);
            return expense;
        }

        private static void ApplyExpenseValues(
            Expense expense,
            CurrencyExchange exchange,
            (int ItemId, int ExpenseTypeId, string CustomerName) catalog,
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
            exchange.Date = exchange.Date.Date;
            exchange.Remarks = string.IsNullOrWhiteSpace(exchange.Remarks) ? null : exchange.Remarks.Trim();
            exchange.ForeignCurrency = (exchange.ForeignCurrency ?? string.Empty).Trim();
            exchange.LKRAmount = CalculateLkrAmount(exchange.ExchangeRate, exchange.ForeignAmount);
        }

        private static decimal CalculateLkrAmount(decimal exchangeRate, decimal foreignAmount)
        {
            return Math.Round(exchangeRate * foreignAmount, 2, MidpointRounding.AwayFromZero);
        }
    }
}
