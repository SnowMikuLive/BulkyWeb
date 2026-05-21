using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyWeb.Models.ViewModels;

public class ProductVM
{
    public Product Product { get; set; } = new();

    public IEnumerable<SelectListItem> CategoryList { get; set; } = new List<SelectListItem>();
}
