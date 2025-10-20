using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

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

        public static string GetDisplayName(Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attr = field?.GetCustomAttribute<DisplayAttribute>();
            return attr?.Name ?? value.ToString();
        }
    }

}
