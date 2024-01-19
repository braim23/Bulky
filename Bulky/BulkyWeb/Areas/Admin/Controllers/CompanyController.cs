using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.IdentityModel.Tokens;
using System.Data;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public CompanyController(IUnitOfWork db)
        {
            _unitOfWork = db;
        }
        public IActionResult Index()
        {
            List<Company> objCompanyList = _unitOfWork.Company.GetAll().ToList();
            IEnumerable<SelectListItem> CategoryList = _unitOfWork.Category.GetAll()
                .Select(u=> new SelectListItem
            {
                    Text = u.Name,
                    Value = u.Id.ToString()
            });

            return View(objCompanyList);
        }

        public IActionResult Upsert(int? id)
        {
            
            if(id == null || id == 0)
            {
                //create
                return View(new Company());
            }
            else
            {
                //update
                Company companyObj = _unitOfWork.Company.GetFirstOrDefault(u => u.Id == id);
                
                return View(companyObj);
            }
        }
        [HttpPost]
        public IActionResult Upsert(Company CompanyObj)
        {
           
            if (ModelState.IsValid)
            {
                
                if (CompanyObj.Id == 0)
                {
                    _unitOfWork.Company.Add(CompanyObj);
                }
                else
                {
                    _unitOfWork.Company.Update(CompanyObj);
                }

                _unitOfWork.Save();
                TempData["success"] = "Company created successfully!";
                return RedirectToAction("Index");
            }
            else
            {
            return View(CompanyObj);
            }
        }


        //[HttpGet]
        //public IActionResult Edit(int id)
        //{
        //    if (id == null || id == 0)
        //    {
        //        return NotFound();
        //    }
        //    Company? CompanyFromDb = _unitOfWork.Company.GetFirstOrDefault(u => u.Id == id);
        //    //Company? CompanyFromDb2 = _db.Categories.FirstOrDefault(u => u.Id==id);
        //    //Company? CompanyFromDb3= _db.Categories.Where(u => u.Id == id).FirstOrDefault();
        //    if (CompanyFromDb == null)
        //    {
        //        return NotFound();
        //    }
        //    return View(CompanyFromDb);
        //}

        //[HttpPost]
        //public IActionResult Edit(Company obj)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        _unitOfWork.Company.Update(obj);
        //        _unitOfWork.Save();
        //        TempData["success"] = "Company updated successfully!";
        //        return RedirectToAction("Index");
        //    }
        //    return View();
        //}

        

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Company> objCompanyList = _unitOfWork.Company.GetAll().ToList();
            return Json(new {data = objCompanyList});
        }

        //[HttpDelete]
        public IActionResult Delete(int? id)
        {
            var CompanyToBeDeleted = _unitOfWork.Company.GetFirstOrDefault(u=>u.Id == id);
            if(CompanyToBeDeleted == null)
            {
                return Json(new { success = false, message = "Couldn't be deleted there was an error" });
            }
            

            _unitOfWork.Company.Remove(CompanyToBeDeleted);
            _unitOfWork.Save();

            return Json(new { success = true, message = "Company deleted successfully" });
        }
        #endregion
    }
}
