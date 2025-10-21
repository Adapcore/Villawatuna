namespace HotelManagement.Services.Interfaces
{
    public interface IPaymentService
    {
        Task AddPaymentAsync(int invoiceNo, decimal amount);
    }
}