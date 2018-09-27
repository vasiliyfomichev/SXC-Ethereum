using Sitecore.Commerce.Entities;
using Sitecore.Commerce.Entities.Customers;
using Sitecore.Commerce.EntityViews;
using Sitecore.Commerce.Pipelines;
using Sitecore.Commerce.Services;
using Sitecore.Commerce.Services.Customers;
using Sitecore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Commerce.Engine.Connect.Pipelines.Customers;
using Sitecore.Commerce.Engine.Connect.Pipelines;

namespace VF.SXC.Ethereum.Processors
{
    public class GetUser : PipelineProcessor
    {
        public GetUser(IEntityFactory entityFactory)
        {
            Assert.ArgumentNotNull((object)entityFactory, nameof(entityFactory));
            this.EntityFactory = entityFactory;
        }

        public IEntityFactory EntityFactory { get; set; }

        public override void Process(ServicePipelineArgs args)
        {
            GetUserRequest request;
            GetUserResult result;
            ValidateArguments(args, out request, out result);
            Assert.IsFalse(string.IsNullOrEmpty(request.UserName) && string.IsNullOrEmpty(request.ExternalId), "at least one request.UserName or request.ExternalId needs to be set");
            EntityView entityView = this.GetEntityView(this.GetContainer(request.Shop.Name, string.Empty, request.UserName, "", args.Request.CurrencyCode, new DateTime?()), string.IsNullOrEmpty(request.ExternalId) ? "Entity-Customer-" + request.UserName : request.ExternalId, string.Empty, "Details", string.Empty, result);
            if (!result.Success)
                return;
            result.CommerceUser = this.TranslateViewToCommerceUser(entityView);
            base.Process(args);
        }

        protected virtual CommerceUser TranslateViewToCommerceUser(EntityView view)
        {
            CommerceUser commerceUser = this.EntityFactory.Create<CommerceUser>("CommerceUser");
            commerceUser.ExternalId = "Entity-Customer-" + view.Properties.FirstOrDefault(p => p.Name.Equals("AccountNumber")).Value;
            commerceUser.FirstName = view.Properties.FirstOrDefault(p => p.Name.Equals("FirstName")).Value;
            commerceUser.LastName = view.Properties.FirstOrDefault(p => p.Name.Equals("LastName")).Value;
            commerceUser.UserName = view.Properties.FirstOrDefault(p => p.Name.Equals("UserName")).Value;
            commerceUser.Email = view.Properties.FirstOrDefault(p => p.Name.Equals("Email")).Value;
            commerceUser.SetPropertyValue("Phone", view.Properties.FirstOrDefault(p => p.Name.Equals("PhoneNumber")).Value);
            commerceUser.SetPropertyValue(Constants.IdentityContractAddressFieldName, (view.ChildViews.FirstOrDefault(v=>v.Name.ToLower()== "blockchaininformation") as EntityView).Properties.FirstOrDefault(p=>p.Name == Constants.IdentityContractAddressFieldName).Value);

            Assert.IsNotNullOrEmpty(commerceUser.ExternalId, "commerceUser.ExternalId");
            if (commerceUser.Customers == null || commerceUser.Customers.Count == 0)
            {
                List<string> stringList = new List<string>()
        {
          commerceUser.ExternalId
        };
                commerceUser.Customers = stringList;
            }
            return commerceUser;
        }

        internal static void ValidateArguments<TRequest, TResult>(ServicePipelineArgs args, out TRequest request, out TResult result) where TRequest : ServiceProviderRequest where TResult : ServiceProviderResult
        {
            Assert.ArgumentNotNull((object)args, nameof(args));
            Assert.ArgumentNotNull((object)args.Request, "args.Request");
            Assert.ArgumentNotNull((object)args.Request.RequestContext, "args.Request.RequestContext");
            Assert.ArgumentNotNull((object)args.Result, "args.Result");
            request = args.Request as TRequest;
            result = args.Result as TResult;
            Assert.IsNotNull((object)request, "The parameter args.Request was not of the expected type.  Expected {0}.  Actual {1}.", typeof(TRequest).Name, args.Request.GetType().Name);
            Assert.IsNotNull((object)result, "The parameter args.Result was not of the expected type.  Expected {0}.  Actual {1}.", typeof(TResult).Name, args.Result.GetType().Name);
        }
    }
}