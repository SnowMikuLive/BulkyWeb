using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BulkyWeb.Models;

public class Product
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(40)]
    public string ISBN { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Author { get; set; } = string.Empty;

    [Required]
    [Range(1, 10000)]
    [Display(Name = "List Price")]
    public double ListPrice { get; set; }

    [Required]
    [Range(1, 10000)]
    [Display(Name = "Price for 1-50")]
    public double Price { get; set; }

    [Required]
    [Range(1, 10000)]
    [Display(Name = "Price for 51-100")]
    public double Price50 { get; set; }

    [Required]
    [Range(1, 10000)]
    [Display(Name = "Price for 100+")]
    public double Price100 { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Please select a category.")]
    [ValidateNever]
    public int CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    [ValidateNever]
    public Category? Category { get; set; }

    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;
}
