using app_ointment_backend.Models;

namespace app_ointment_backend.ViewModels
{
    public class UsersViewModel
    {
        public IEnumerable<User> Users;
        public string? CurrentViewName;

        public UsersViewModel(IEnumerable<User> users, string? currentViewName)
        {
            Users = users;
            CurrentViewName = currentViewName;
        }
    }
}