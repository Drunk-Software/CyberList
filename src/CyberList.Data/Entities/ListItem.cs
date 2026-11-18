namespace CyberList.Data.Entities;

public class ListItem
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public required string Title { get; set; }

    public bool IsDone { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ListItem? Parent { get; set; }
    public List<ListItem> Children { get; set; } = [];
}
