using HotelManagement.Services.Interface;
using Umbraco.Cms.Core.Web;

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
    }
}
