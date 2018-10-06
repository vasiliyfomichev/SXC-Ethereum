using System.Web;
using Nethereum.Web3;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Rules;
using Sitecore.Rules.Conditions;
using VF.SXC.Ethereum.Utilities;

namespace VF.SXC.Ethereum.Conditions
{
    public class CustomerHasPurchasedProductpublic<T> : WhenCondition<T> where T : RuleContext
    {
        protected override bool Execute(T ruleContext)
        {
            var contextUser = Context.User;
            if (!contextUser.IsAuthenticated)
                return false;

            var commerceUser = Customer.GetCommerceUser(contextUser);

            var ethContractAddress =
                commerceUser.GetPropertyValue(Constants.IdentityContractAddressFieldName) as string ??
                Settings.GetSetting("VF.SXC.Ethereum.IdentityContractAddress");
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
            var productId = Product.GetProductIdFromUrl(url);

            var hasProduct = checkProductFunction.CallAsync<bool>(productId).Result;

            return hasProduct;
        }
    }
}