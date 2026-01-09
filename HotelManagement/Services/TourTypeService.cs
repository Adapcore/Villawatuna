using HotelManagement.Services.Interface;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace HotelManagement.Services
{
    public class TourTypeService : ITourTypeService
    {
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;

        public TourTypeService(IUmbracoContextAccessor umbracoContextAccessor)
        {
            _umbracoContextAccessor = umbracoContextAccessor;
        }

        public async Task<IEnumerable<ItemDto>> GetItemsAsync()
        {
            var context = _umbracoContextAccessor.GetRequiredUmbracoContext();
            var root = context.Content?.GetAtRoot().FirstOrDefault();

            if (root == null)
                return Enumerable.Empty<ItemDto>();

            // Find the "TourTypes" node under the root
            var tourTypeNode = root.DescendantsOrSelfOfType("tour").FirstOrDefault();

            if (tourTypeNode == null)
                return Enumerable.Empty<ItemDto>();

            // Get items only under Tour
            var items = tourTypeNode
                .DescendantsOfType("tourItem")
                .Select(x => new ItemDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Price = x.Value<decimal>("price")
                })
                .ToList();

            return await Task.FromResult(items);
        }

        public async Task<string?> GetDefaultCurrencyForBillingAsync()
        {
            var context = _umbracoContextAccessor.GetRequiredUmbracoContext();
            var root = context.Content?.GetAtRoot().FirstOrDefault();

            if (root == null)
                return null;

            // Find the "Tour" node under the root
            var tourNode = root.DescendantsOrSelfOfType("tour").FirstOrDefault();

            if (tourNode == null)
                return null;

            // Get the defaultCurrencyForBilling property (content picker returns IPublishedContent)
            var currencyNode = tourNode.Value<IPublishedContent>("defaultCurrencyForBilling");

            if (currencyNode == null)
                return null;

            // Get the currencyCode from the Currency content node
            var currencyCode = currencyNode.Value<string>("currencyCode");

            return await Task.FromResult(currencyCode);
        }
    }
}
