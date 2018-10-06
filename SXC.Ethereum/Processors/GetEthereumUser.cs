using System.Collections.Generic;
using System.Linq;
using Sitecore.Commerce.Engine.Connect.Pipelines.Customers;
using Sitecore.Commerce.Entities;
using Sitecore.Commerce.Entities.Customers;
using Sitecore.Commerce.EntityViews;
using Sitecore.Diagnostics;

namespace VF.SXC.Ethereum.Processors
{
    public class GetEthereumUser : GetUser
    {
        public GetEthereumUser(IEntityFactory entityFactory) : base(entityFactory)
        {
        }

        protected override CommerceUser TranslateViewToCommerceUser(EntityView view)
        {
            var commerceUser = EntityFactory.Create<CommerceUser>("CommerceUser");
            commerceUser.ExternalId = "Entity-Customer-" +
                                      view.Properties.FirstOrDefault(p => p.Name.Equals("AccountNumber")).Value;
            commerceUser.FirstName = view.Properties.FirstOrDefault(p => p.Name.Equals("FirstName")).Value;
            commerceUser.LastName = view.Properties.FirstOrDefault(p => p.Name.Equals("LastName")).Value;
            commerceUser.UserName = view.Properties.FirstOrDefault(p => p.Name.Equals("UserName")).Value;
            commerceUser.Email = view.Properties.FirstOrDefault(p => p.Name.Equals("Email")).Value;
            commerceUser.SetPropertyValue("Phone",
                view.Properties.FirstOrDefault(p => p.Name.Equals("PhoneNumber")).Value);
            if (view.ChildViews != null && view.ChildViews.Any(v => v.Name.ToLower() == "blockchaininformation"))
                commerceUser.SetPropertyValue(Constants.IdentityContractAddressFieldName,
                    (view.ChildViews.FirstOrDefault(v => v.Name.ToLower() == "blockchaininformation") as EntityView)
                    .Properties.FirstOrDefault(p => p.Name == Constants.IdentityContractAddressFieldName).Value);
            Assert.IsNotNullOrEmpty(commerceUser.ExternalId, "commerceUser.ExternalId");
            if (commerceUser.Customers == null || commerceUser.Customers.Count == 0)
            {
                var stringList = new List<string>
                {
                    commerceUser.ExternalId
                };
                commerceUser.Customers = stringList;
            }

            return commerceUser;
        }
    }
}