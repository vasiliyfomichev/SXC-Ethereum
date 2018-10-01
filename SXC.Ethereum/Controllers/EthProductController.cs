using Sitecore.Commerce.Services.Orders;
using Sitecore.Commerce.XA.Foundation.Connect.Providers;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Mvc.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VF.SXC.Ethereum.Utilities;

namespace VF.SXC.Ethereum.Controllers
{
    public class EthProductController : SitecoreController
    {
        public ActionResult DigitalDownload()
        {
            var contextUser = Sitecore.Context.User;
            if (contextUser == null || !contextUser.IsAuthenticated)
                return new EmptyResult();

            var shopName = "JoyceMeyer";
            var commerceCustomer = Customer.GetCommerceUser(contextUser);
            var ethContractAddress = commerceCustomer.GetPropertyValue(Constants.IdentityContractAddressFieldName) as string ?? Settings.GetSetting("VF.SXC.Ethereum.IdentityContractAddress");

            var url = Request.Url.AbsolutePath;
            var productId = Product.GetProductIdFromUrl(url);

            var connectServiceProvider = new ConnectServiceProvider();
            var orderServiceProvider = connectServiceProvider.GetOrderServiceProvider();
            var orderHeaderResult = orderServiceProvider.GetVisitorOrders(new GetVisitorOrdersRequest(commerceCustomer.ExternalId, shopName));

            if (!orderHeaderResult.Success || orderHeaderResult.OrderHeaders == null || !orderHeaderResult.OrderHeaders.Any())
                return new EmptyResult();

            var productDownloadToken = string.Empty;
            var orderHeaders = orderHeaderResult.OrderHeaders;
            var isTokenSet = false;
            foreach (var orderHeader in orderHeaders)
            {
                var orderId = orderHeader.ExternalId;
                var orderResult = orderServiceProvider.GetVisitorOrder(new GetVisitorOrderRequest(orderId, commerceCustomer.ExternalId, shopName));

                if (!orderResult.Success || orderResult.Order == null || !orderResult.Order.Lines.Any())
                    continue;

                var orderLines = orderResult.Order.Lines;
                foreach (var line in orderLines)
                {
                    if(line.Product.ProductId.ToLower() == productId.ToLower())
                    {
                        if (line == null || line.GetPropertyValue(Constants.BlockchainDownloadToken) == null)
                            continue;
                        productDownloadToken =  line.GetPropertyValue(Constants.BlockchainDownloadToken) as string;
                        isTokenSet = true;
                        break; 
                    }
                }
                if (isTokenSet)
                    break;
            }

            if(isTokenSet && string.IsNullOrWhiteSpace(productDownloadToken))
            {
                Log.Warn("Ethereum: token is set, but empty.", this);
            }

            ViewBag.BlockchainDownloadToken = productDownloadToken;
            return View();
        }
    }
}