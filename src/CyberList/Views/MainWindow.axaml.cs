using Avalonia.Controls;
using Avalonia.Input;
using CyberList.ViewModels;

namespace CyberList.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void EditBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox { DataContext: ListItemViewModel item })
            return;

        switch (e.Key)
        {
            case Key.Enter:
                if (item.SaveEditCommand.CanExecute(null))
                    item.SaveEditCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Escape:
                item.CancelEditCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }
}
