using System.ComponentModel.DataAnnotations;

namespace BulkyWeb.Models;

public class Category
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 100, ErrorMessage = "Display Order must be between 1-100")]
    public int DisplayOrder { get; set; }
}
