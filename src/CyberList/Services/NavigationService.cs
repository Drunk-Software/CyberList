using CyberList.Interfaces;
using CyberList.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CyberList.Services
{
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _services;
        private readonly Stack<ViewModelBase> _navigationHistory = new Stack<ViewModelBase>();
        public ViewModelBase? CurrentPage => _navigationHistory.Count > 0 ? _navigationHistory.Peek() : null;
        public NavigationService(IServiceProvider services)
        {
            _services = services;
        }


        public bool CanGoBack => _navigationHistory.Count > 1;

        public event Action<ViewModelBase>? CurrentPageChanged;

        public void GoBack()
        {
            if (_navigationHistory.Count > 1)
            {
                _navigationHistory.Pop();
                CurrentPageChanged?.Invoke(_navigationHistory.Peek());
            }
        }

        public void NavigateTo<T>() where T : ViewModelBase => NavigateTo(typeof(T));

        public void NavigateTo(Type pageType)
        {
            // The generic overload's "where T : ViewModelBase" can't protect this one, so check by hand.
            if (!typeof(ViewModelBase).IsAssignableFrom(pageType))
                throw new ArgumentException($"{pageType.Name} is not a page view model.", nameof(pageType));

            var page = (ViewModelBase)_services.GetRequiredService(pageType);
            if(CurrentPage == page)
                return;
            _navigationHistory.Push(page);
            CurrentPageChanged?.Invoke(page);
        }
    }
}
