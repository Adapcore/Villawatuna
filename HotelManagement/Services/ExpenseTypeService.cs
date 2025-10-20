using HotelManagement.Models.DTO;
using HotelManagement.Services.Interface;
using Umbraco.Cms.Core.Web;

namespace HotelManagement.Services
{
    public class ExpenseTypeService : IExpenseTypeService
    {
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;

        public ExpenseTypeService(IUmbracoContextAccessor umbracoContextAccessor)
        {
            _umbracoContextAccessor = umbracoContextAccessor;
        }

        public async Task<IEnumerable<ExpenseTypeDTO>> GetExpenseTypesAsync()
        {
            var context = _umbracoContextAccessor.GetRequiredUmbracoContext();
            var root = context.Content?.GetAtRoot().FirstOrDefault();

            if (root == null)
                return Enumerable.Empty<ExpenseTypeDTO>();

            var items = root
                .DescendantsOfType("expenseType")
                .Select(x => new ExpenseTypeDTO
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToList();

            return await Task.FromResult(items);
        }
    }
}
