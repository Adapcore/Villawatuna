using HotelManagement.Services.Interface;
using Umbraco.Cms.Core.Web;

namespace HotelManagement.Services
{
    public class ExchangeTypeService : IExchangeTypeService
    {
        public const string CurrencyExchangeItemName = "Currency Exchange";

        private readonly IUmbracoContextAccessor _umbracoContextAccessor;

        public ExchangeTypeService(IUmbracoContextAccessor umbracoContextAccessor)
        {
            _umbracoContextAccessor = umbracoContextAccessor;
        }

        public async Task<IEnumerable<ItemDto>> GetItemsAsync()
        {
            var context = _umbracoContextAccessor.GetRequiredUmbracoContext();
            var root = context.Content?.GetAtRoot().FirstOrDefault();

            if (root == null)
                return Enumerable.Empty<ItemDto>();

            var exchangeNode = root.DescendantsOrSelfOfType("exchange").FirstOrDefault();
            if (exchangeNode == null)
                return Enumerable.Empty<ItemDto>();

            var items = exchangeNode
                .DescendantsOfType("exchangeItem")
                .Select(x => new ItemDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Price = 0
                })
                .ToList();

            return await Task.FromResult(items);
        }

        public async Task<ItemDto?> GetCurrencyExchangeItemAsync()
        {
            var items = await GetItemsAsync();
            return items.FirstOrDefault(i =>
                string.Equals(i.Name, CurrencyExchangeItemName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
