namespace CyberList.ViewModels;

/// <summary>One entry in the header's navigation panel: "01  TASK GRID".</summary>
public sealed record NavItem(string Number, string Title, Type PageType);
