using Sitecore.Commerce.Engine.Connect.Entities;
using Sitecore.Commerce.Engine.Connect.Pipelines.Arguments;
using Sitecore.Commerce.Engine.Connect.Pipelines.Carts;
using Sitecore.Commerce.Entities;
using Sitecore.Commerce.Plugin.Carts;
using System.Linq;
using VF.SXC.Plugin.Ethereum.Components;

namespace VF.SXC.Ethereum.Processors
{
    public class TranslateEthereumCartLineToEntity : TranslateCartLineToEntity
    {
        public TranslateEthereumCartLineToEntity(IEntityFactory entityFactory) : base(entityFactory)
        {
        }

        protected override void Translate(TranslateCartLineToEntityRequest request, CartLineComponent source, Sitecore.Commerce.Engine.Connect.Entities.CommerceCartLine destination)
        {
            base.Translate(request, source, destination);
            destination.ExternalCartLineId = source.Id;
            destination.Quantity = source.Quantity;
            this.TranslateProduct(request, source, destination);
            this.TranslateAdjustments(request, source, destination);
            this.TranslateTotals(request, source, destination);
            TranslateBlockchainInfo(request, source, destination);
        }

        protected virtual void TranslateBlockchainInfo(TranslateCartLineToEntityRequest request, CartLineComponent source, CommerceCartLine destination)
        {
            var blokchainTokenComponent = source.ChildComponents.FirstOrDefault(c => c.Name.ToLower() == "BlockchainDownloadToken".ToLower()) as DigitalDownloadBlockchainTokenComponent;
            if (blokchainTokenComponent == null)
                return;
            destination.SetPropertyValue(Constants.BlockchainDownloadToken, blokchainTokenComponent.BlockchainDownloadToken);
        }
    }
}