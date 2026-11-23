using CyberList.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace CyberList.Interfaces
{
    public interface IHasTabs
    {
        IReadOnlyList<PageTab> Tabs { get; }
    }
}
