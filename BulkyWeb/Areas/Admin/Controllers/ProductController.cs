using BulkyWeb.DataAccess.Repository.IRepository;
using BulkyWeb.Models;
using BulkyWeb.Models.ViewModels;
using BulkyWeb.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            return View();
        }

        private static IEnumerable<SelectListItem> BuildCategorySelectList(
            IUnitOfWork unitOfWork) =>
            unitOfWork.Category.GetAll().Select(u => new SelectListItem
            {
                Text = u.Name,
                Value = u.Id.ToString()
            });

        public IActionResult Upsert(int? id)
        {
            var productVm = new ProductVM
            {
                CategoryList = BuildCategorySelectList(_unitOfWork)
            };

            if (id == null || id == 0)
            {
                return View(productVm);
            }

            var productFromDb = _unitOfWork.Product.Get(u => u.Id == id);
            if (productFromDb == null)
            {
                return NotFound();
            }

            productVm.Product = productFromDb;
            return View(productVm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(20 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 20 * 1024 * 1024)]
        public IActionResult Upsert([FromForm] ProductVM productVm, IFormFile? file = null)
        {
            if (file == null || file.Length == 0)
            {
                file = Request.Form?.Files?["file"];
            }
            if (file != null && file.Length == 0)
            {
                file = null;
            }

            if (!ModelState.IsValid)
            {
                productVm.CategoryList = BuildCategorySelectList(_unitOfWork);
                return View(productVm);
            }

            BulkyWeb.Models.Product? productFromDb = null;
            if (productVm.Product.Id != 0)
            {
                productFromDb = _unitOfWork.Product.Get(p => p.Id == productVm.Product.Id, tracked: false);
                if (productFromDb == null)
                {
                    return NotFound();
                }
            }

            var wwwRootPath = _webHostEnvironment.WebRootPath;
            if (file != null && file.Length > 0)
            {
                var productPath = Path.Combine(wwwRootPath, "images", "product");
                if (!Directory.Exists(productPath))
                {
                    Directory.CreateDirectory(productPath);
                }


                var oldUrl = productFromDb?.ImageUrl;
                if (string.IsNullOrEmpty(oldUrl))
                {
                    oldUrl = productVm.Product.ImageUrl;
                }

                if (!string.IsNullOrEmpty(oldUrl))
                {
                    var oldImagePath = GetPhysicalImagePath(wwwRootPath, oldUrl);
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                using (var fileStream = new FileStream(
                           Path.Combine(productPath, fileName), FileMode.Create))
                {
                    file.CopyTo(fileStream);
                }

                productVm.Product.ImageUrl = @"/images/product/" + fileName;
            }
            else if (productFromDb != null)
            {

                if (string.IsNullOrEmpty(productVm.Product.ImageUrl))
                {
                    productVm.Product.ImageUrl = productFromDb.ImageUrl;
                }
            }

            if (productVm.Product.Id == 0)
            {
                _unitOfWork.Product.Add(productVm.Product);
                TempData["success"] = "Product created successfully";
            }
            else
            {
                _unitOfWork.Product.Update(productVm.Product);
                TempData["success"] = "Product updated successfully";
            }

            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        private static string GetPhysicalImagePath(string wwwRoot, string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
            {
                return string.Empty;
            }

            var relative = imageUrl
                .TrimStart('~', '/', '\\')
                .Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(wwwRoot, relative);
        }

        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            var productList = _unitOfWork.Product.GetAll(includeProperties: "Category");
            return Json(new { data = productList });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var productToBeDeleted = _unitOfWork.Product.Get(u => u.Id == id);
            if (productToBeDeleted == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

            if (!string.IsNullOrEmpty(productToBeDeleted.ImageUrl))
            {
                var oldImagePath = GetPhysicalImagePath(_webHostEnvironment.WebRootPath, productToBeDeleted.ImageUrl);
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }

            _unitOfWork.Product.Remove(productToBeDeleted);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete successful" });
        }

        #endregion
    }
}
