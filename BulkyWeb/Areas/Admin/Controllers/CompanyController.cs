using BulkyWeb.DataAccess.Repository.IRepository;
using BulkyWeb.Models;
using BulkyWeb.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SD.Role_Admin)]
public class CompanyController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public CompanyController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index() => View();

    public IActionResult Upsert(int? id)
    {
        if (id is null or 0)
        {
            return View(new Company());
        }

        var companyFromDb = _unitOfWork.Company.Get(u => u.Id == id);
        return companyFromDb == null ? NotFound() : View(companyFromDb);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Upsert(Company company)
    {
        if (!ModelState.IsValid)
        {
            return View(company);
        }

        if (company.Id == 0)
        {
            _unitOfWork.Company.Add(company);
            TempData["success"] = "Company created successfully";
        }
        else
        {
            _unitOfWork.Company.Update(company);
            TempData["success"] = "Company updated successfully";
        }

        _unitOfWork.Save();
        return RedirectToAction(nameof(Index));
    }

    #region API CALLS

    [HttpGet]
    public IActionResult GetAll()
    {
        var companyList = _unitOfWork.Company.GetAll();
        return Json(new { data = companyList });
    }

    [HttpDelete]
    public IActionResult Delete(int? id)
    {
        var companyToBeDeleted = _unitOfWork.Company.Get(u => u.Id == id);
        if (companyToBeDeleted == null)
        {
            return Json(new { success = false, message = "Error while deleting" });
        }

        _unitOfWork.Company.Remove(companyToBeDeleted);
        _unitOfWork.Save();
        return Json(new { success = true, message = "Delete successful" });
    }

    #endregion
}
