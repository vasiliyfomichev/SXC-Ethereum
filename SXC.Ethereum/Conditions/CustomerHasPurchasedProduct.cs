using Nethereum.Web3;
using Sitecore.Commerce.Engine.Connect.Pipelines.Arguments;
using Sitecore.Commerce.Entities.Customers;
using Sitecore.Commerce.Services;
using Sitecore.Commerce.Services.Customers;
using Sitecore.Commerce.XA.Feature.Catalog.Repositories;
using Sitecore.Commerce.XA.Foundation.Connect.Providers;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Rules;
using Sitecore.Rules.Conditions;
using Sitecore.Security.Accounts;
using System.Linq;
using System.Web;

namespace VF.SXC.Ethereum.Conditions
{
    public class CustomerHasPurchasedProductpublic<T> : WhenCondition<T> where T : RuleContext
    {
        protected override bool Execute(T ruleContext)
        {
            var contextUser = Sitecore.Context.User;
            var commerceUser = GetCommerceUser(contextUser);

            var ethContractAddress = Settings.GetSetting("VF.SXC.Ethereum.IdentityContractAddress"); //contextUser.Profile.GetCustomProperty("EthereiumIdentityContract");
            var nodeUrl = Settings.GetSetting("VF.SXC.Ethereum.NodeUrl");
            var sxaEthAccountAddress = Settings.GetSetting("VF.SXC.Ethereum.SXAEthAccountAddress");

            if (string.IsNullOrWhiteSpace(nodeUrl))
            {
                Log.Warn("Ethereum: Missing NodeUrl configuration setting. Cannot connect to the blockchain.", this);
                return false;
            }

            var abi = Settings.GetSetting("VF.SXC.Ethereum.IdentityContractABI");
            if (string.IsNullOrWhiteSpace(abi))
            {
                Log.Warn("Ethereum: Missing ABI configuration setting. Cannot connect to the blockchain.", this);
                return false;
            }


            var web3 = new Web3(nodeUrl);
            var idContract = web3.Eth.GetContract(abi, ethContractAddress);
            var checkProductFunction = idContract.GetFunction("contactHasPurchasedProduct");

            var url = HttpContext.Current.Request.Url.AbsolutePath;
            if(string.IsNullOrWhiteSpace(url))
            {
                Log.Warn("Ethereum: The context URL is null or empty.", this);
                return false;
            }

            var productId = url.Split('=').LastOrDefault();

            if(string.IsNullOrWhiteSpace(productId))
            {
                Log.Error("Ethereum: the URL does not follow the expected format. Unable to get product Id.", this);
                return false;
            }

            var hasProduct = checkProductFunction.CallAsync<bool>(productId).Result;

            return hasProduct;
        }

        private CommerceUser GetCommerceUser(User user)
        {
            var connectServiceProvider = new ConnectServiceProvider();
            var getCustomerServieProvider = connectServiceProvider.GetCustomerServiceProvider();
            var userResult = getCustomerServieProvider.GetUser(new GetUserRequest($"{user.Domain}/{user.Profile.UserName}"));
            if (!userResult.Success || userResult.CommerceUser == null)
            {
                return null;
            }

            var commerceUser = userResult.CommerceUser;
            return commerceUser;
        }
    }
}