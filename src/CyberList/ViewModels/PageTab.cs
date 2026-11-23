using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace CyberList.ViewModels
{
    /// <summary>
    /// One tab in the shell's tab strip. The page that owns it decides when to create it and when to show it.
    /// </summary>
    public partial class PageTab : ObservableObject
    {
        [ObservableProperty]
        public partial bool IsSelected { get; set; }

        partial void OnIsSelectedChanged(bool value)
        {
            if (value)
                _onSelected();
        }
        public string Label { get; }
        private readonly Action _onSelected;
        public PageTab(string label, Action onSelected)
        {
            Label = label;
            _onSelected = onSelected;
        }

    }
}
