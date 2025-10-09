using Microsoft.AspNetCore.Mvc.Rendering;

namespace HotelManagement.Helper
{
    public static class EnumHelper
    {
        public static IEnumerable<SelectListItem> ToSelectList<TEnum>() where TEnum : Enum
        {
            return Enum.GetValues(typeof(TEnum))
                       .Cast<TEnum>()
                       .Select(e => new SelectListItem
                       {
                           Value = Convert.ToInt32(e).ToString(),
                           Text = e.ToString()
                       });
        }
    }

}
