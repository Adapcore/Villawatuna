namespace HotelManagement.Models.DTO
{
    public record PrintDTO(
        string ReceiptNo,
        string StoreName,
        string AddressLine,
        List<ReceiptItemVm> Items,
        decimal Subtotal,
        decimal Tax,
        decimal Discount,
        decimal Total,
        decimal Paid,
        DateTime? Date,
        string? Footer = "Thank you!"
        
    );

    public record ReceiptItemVm(string Name, decimal Qty, decimal Price, decimal Total);
}
