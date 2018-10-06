using System.Linq;
using Sitecore.Diagnostics;

namespace VF.SXC.Ethereum.Utilities
{
    public class Product
    {
        public static string GetProductIdFromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                Log.Warn("Ethereum: The context URL is null or empty.", url);
                return string.Empty;
            }

            var productId = url.Split('=').LastOrDefault();

            if (string.IsNullOrWhiteSpace(productId))
            {
                Log.Error("Ethereum: the URL does not follow the expected format. Unable to get product Id.",
                    productId);
                return string.Empty;
            }

            return productId;
        }
    }
}