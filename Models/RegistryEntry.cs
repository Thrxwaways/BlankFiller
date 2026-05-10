namespace BlankFiller.Models;

public class RegistryEntry
{
    public string FullName { get; set; } = "";
    public string ShortName { get; set; } = "";
    public string ActNumber { get; set; } = "";
    public string Sum { get; set; } = "";
    public string PaymentDate { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public string? ReceiptNumber { get; set; }
}