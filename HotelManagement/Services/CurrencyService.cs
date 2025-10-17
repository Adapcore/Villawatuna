using HotelManagement.Models.DTO;
using HotelManagement.Services.Interface;
using Umbraco.Cms.Core.Web;

namespace HotelManagement.Services
{
    public class CurrencyService : ICurrencyService
    {
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;

        public CurrencyService(IUmbracoContextAccessor umbracoContextAccessor)
        {
            _umbracoContextAccessor = umbracoContextAccessor;
        }

        public async Task<IEnumerable<CurrencyDTO>> GetCurencyTypesAsync()
        {
            var context = _umbracoContextAccessor.GetRequiredUmbracoContext();
            var root = context.Content?.GetAtRoot().FirstOrDefault();

            if (root == null)
                return Enumerable.Empty<CurrencyDTO>();

            // Find the "CurrencyTypes" node under the root
            var tourTypeNode = root.DescendantsOrSelfOfType("currencyTypes").FirstOrDefault();

            if (tourTypeNode == null)
                return Enumerable.Empty<CurrencyDTO>();

            var items = tourTypeNode
                .DescendantsOfType("currency")
                .Select(x => new CurrencyDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Value<string>("currencyCode")
                })
                .ToList();

            return await Task.FromResult(items);
        }
    }
}
