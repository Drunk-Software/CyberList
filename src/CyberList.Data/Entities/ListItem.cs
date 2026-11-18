namespace CyberList.Data.Entities;

public class ListItem
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public bool IsDone { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
