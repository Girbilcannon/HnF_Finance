namespace GrannyManager.Core.Models;

public sealed class BillReceipt
{
    public long Id { get; set; }
    public string ReceiptType { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public string ReceiptDateText => ReceiptDate.ToString("yyyy-MM-dd");
}
