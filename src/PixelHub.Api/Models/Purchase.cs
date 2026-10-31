namespace PixelHub.Api.Models;

public class Purchase
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public string Titre { get; set; } = "";
    public int PrixPaye { get; set; }
    public DateTime Date { get; set; }
}
