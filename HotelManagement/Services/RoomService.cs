using HotelManagement.Services.Interface;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace HotelManagement.Services
{
    public class RoomService : IRoomService
    {
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;

        public RoomService(IUmbracoContextAccessor umbracoContextAccessor)
        {
            _umbracoContextAccessor = umbracoContextAccessor;
        }

        public async Task<IEnumerable<ItemDto>> GetRoomCategoriesAsync()
        {
            var context = _umbracoContextAccessor.GetRequiredUmbracoContext();
            var root = context.Content?.GetAtRoot().FirstOrDefault();

            if (root == null)
                return Enumerable.Empty<ItemDto>();

            var items = root
                .DescendantsOfType("roomCategory")
                .Select(x => new ItemDto
                {
                    Id = x.Id,
                    Name = x.Name
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

            // Find the "Rooms" node under the root
            var roomsNode = root.DescendantsOrSelfOfType("rooms").FirstOrDefault();

            if (roomsNode == null)
                return null;

            // Get the defaultCurrencyForBilling property (content picker returns IPublishedContent)
            var currencyNode = roomsNode.Value<IPublishedContent>("defaultCurrencyForBilling");

            if (currencyNode == null)
                return null;

            // Get the currencyCode from the Currency content node
            var currencyCode = currencyNode.Value<string>("currencyCode");

            return await Task.FromResult(currencyCode);
        }
    }
}
