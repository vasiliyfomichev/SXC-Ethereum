using Sitecore.Commerce.Entities.Customers;
using Sitecore.Commerce.Services.Customers;
using Sitecore.Commerce.XA.Foundation.Connect.Providers;
using Sitecore.Security.Accounts;

namespace VF.SXC.Ethereum.Utilities
{
    public class Customer
    {
        public static CommerceUser GetCommerceUser(User user)
        {
            var connectServiceProvider = new ConnectServiceProvider();
            var getCustomerServieProvider = connectServiceProvider.GetCustomerServiceProvider();
            var userResult = getCustomerServieProvider.GetUser(new GetUserRequest(user.Profile.UserName));
            if (!userResult.Success || userResult.CommerceUser == null) return null;

            var commerceUser = userResult.CommerceUser;
            return commerceUser;
        }
    }
}