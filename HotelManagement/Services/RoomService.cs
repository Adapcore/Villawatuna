using Umbraco.Cms.Core.Web;

namespace HotelManagement.Services
{
    public class RoomService : IRoomService
    {
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;

        public RoomService(IUmbracoContextAccessor umbracoContextAccessor)
        {
            _umbracoContextAccessor = umbracoContextAccessor;
        }

        public async Task<IEnumerable<RoomTypeDto>> GetRoomCategoriesAsync()
        {
            var context = _umbracoContextAccessor.GetRequiredUmbracoContext();
            var root = context.Content?.GetAtRoot().FirstOrDefault();

            if (root == null)
                return Enumerable.Empty<RoomTypeDto>();

            var items = root
                .DescendantsOfType("roomCategory")
                .Select(x => new RoomTypeDto
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToList();

            return await Task.FromResult(items);
        }
    }
}
