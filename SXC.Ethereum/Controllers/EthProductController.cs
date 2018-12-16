using System.Linq;
using System.Web.Mvc;
using Nethereum.Web3;
using Sitecore;
using Sitecore.Commerce.Services.Orders;
using Sitecore.Commerce.XA.Foundation.Connect.Providers;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Mvc.Controllers;
using VF.SXC.Ethereum.Utilities;

namespace VF.SXC.Ethereum.Controllers
{
    public class EthProductController : SitecoreController
    {
        public ActionResult DigitalDownloadRemote()
        {
            var contextUser = Context.User;
            if (contextUser == null || !contextUser.IsAuthenticated)
                return new EmptyResult();

            var commerceUser = Customer.GetCommerceUser(contextUser);

            var ethContractAddress =
                commerceUser.GetPropertyValue(Constants.IdentityContractAddressFieldName) as string ??
                Settings.GetSetting("VF.SXC.Ethereum.IdentityContractAddress");
            var nodeUrl = Settings.GetSetting("VF.SXC.Ethereum.NodeUrl");
            var sxaEthAccountAddress = Settings.GetSetting("VF.SXC.Ethereum.SXAEthAccountAddress");

            if (string.IsNullOrWhiteSpace(nodeUrl))
            {
                Log.Warn("Ethereum: Missing NodeUrl configuration setting. Cannot connect to the blockchain.", this);
                return new EmptyResult();
            }

            var abi = Settings.GetSetting("VF.SXC.Ethereum.IdentityContractABI");
            if (string.IsNullOrWhiteSpace(abi))
            {
                Log.Warn("Ethereum: Missing ABI configuration setting. Cannot connect to the blockchain.", this);
                return new EmptyResult();
            }


            var web3 = new Web3(nodeUrl);
            var idContract = web3.Eth.GetContract(abi, ethContractAddress);
            var checkProductFunction = idContract.GetFunction("contactHasPurchasedProduct");

            var url = Request.Url.AbsolutePath;
            var productId = Product.GetProductIdFromUrl(url);

            var hasProduct = checkProductFunction.CallAsync<bool>(productId).Result;
            if (!hasProduct)
                return new EmptyResult();

            var royalryCcontractAddress = Settings.GetSetting("VF.SXC.Ethereum.RoyaltyContractAddress");
            var royalryCcontractAbi = Settings.GetSetting("VF.SXC.Ethereum.RoyaltyContractABI");
            var royaltyContract = web3.Eth.GetContract(royalryCcontractAbi, royalryCcontractAddress);
            var getPurchasedProductUrlFunctino = royaltyContract.GetFunction("getPurchasedAssetUrl");
            var productDownloadToken =
                getPurchasedProductUrlFunctino.CallAsync<string>(productId, ethContractAddress).Result;

            if (string.IsNullOrWhiteSpace(productDownloadToken))
                return new EmptyResult();

            ViewBag.BlockchainDownloadToken = productDownloadToken;
            return View("DigitalDownload");
        }


        public ActionResult DigitalDownload()
        {
            var contextUser = Context.User;
            if (contextUser == null || !contextUser.IsAuthenticated)
                return new EmptyResult();

            var shopName = Settings.GetSetting("VF.SXC.Ethereum.ShopName", "Storefront");
            var commerceCustomer = Customer.GetCommerceUser(contextUser);
            var ethContractAddress =
                commerceCustomer.GetPropertyValue(Constants.IdentityContractAddressFieldName) as string ??
                Settings.GetSetting("VF.SXC.Ethereum.IdentityContractAddress");

            var url = Request.Url.AbsolutePath;
            var productId = Product.GetProductIdFromUrl(url);

            var connectServiceProvider = new ConnectServiceProvider();
            var orderServiceProvider = connectServiceProvider.GetOrderServiceProvider();
            var orderHeaderResult =
                orderServiceProvider.GetVisitorOrders(
                    new GetVisitorOrdersRequest(commerceCustomer.ExternalId, shopName));

            if (!orderHeaderResult.Success || orderHeaderResult.OrderHeaders == null ||
                !orderHeaderResult.OrderHeaders.Any())
                return new EmptyResult();

            var productDownloadToken = string.Empty;
            var orderHeaders = orderHeaderResult.OrderHeaders;
            var isTokenSet = false;
            foreach (var orderHeader in orderHeaders)
            {
                var orderId = orderHeader.ExternalId;
                var orderResult =
                    orderServiceProvider.GetVisitorOrder(new GetVisitorOrderRequest(orderId,
                        commerceCustomer.ExternalId, shopName));

                if (!orderResult.Success || orderResult.Order == null || !orderResult.Order.Lines.Any())
                    continue;

                var orderLines = orderResult.Order.Lines;
                foreach (var line in orderLines)
                    if (line.Product.ProductId.ToLower() == productId.ToLower())
                    {
                        if (line == null || !line.ContainsKey(Constants.BlockchainDownloadToken))
                            continue;
                        productDownloadToken = line.GetPropertyValue(Constants.BlockchainDownloadToken) as string;
                        isTokenSet = true;
                        break;
                    }

                if (isTokenSet)
                    break;
            }

            if (isTokenSet && string.IsNullOrWhiteSpace(productDownloadToken))
                Log.Warn("Ethereum: token is set, but empty.", this);

            ViewBag.BlockchainDownloadToken = productDownloadToken;
            return View();
        }
    }
}