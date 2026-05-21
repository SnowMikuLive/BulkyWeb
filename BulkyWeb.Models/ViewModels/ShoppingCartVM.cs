namespace BulkyWeb.Models.ViewModels;

public class ShoppingCartVM
{
    public IEnumerable<ShoppingCart> ShoppingCartList { get; set; } = new List<ShoppingCart>();

    public OrderHeader OrderHeader { get; set; } = new();
}
