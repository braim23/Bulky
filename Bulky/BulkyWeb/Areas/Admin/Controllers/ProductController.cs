using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.IdentityModel.Tokens;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ProductController(IUnitOfWork db, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = db;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            List<Product> objProductList = _unitOfWork.Product.GetAll(includeProperties:"Category").ToList();
            IEnumerable<SelectListItem> CategoryList = _unitOfWork.Category.GetAll()
                .Select(u=> new SelectListItem
            {
                    Text = u.Name,
                    Value = u.Id.ToString()
            });

            return View(objProductList);
        }

        public IActionResult Upsert(int? id)
        {
            //List<Product> objProductList = _unitOfWork.Product.GetAll().ToList();
            ProductViewModel productViewModel = new()
            {
                CategoryList = _unitOfWork.Category.GetAll()
                .Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                }),
                Product = new Product()
            };
            if(id == null || id == 0)
            {
                //create
                return View(productViewModel);
            }
            else
            {
                //update
                productViewModel.Product = _unitOfWork.Product.GetFirstOrDefault(u => u.Id == id);
                if (productViewModel == null)
                {
                    return NotFound();
                }
                return View(productViewModel);
            }
        }
        [HttpPost]
        public IActionResult Upsert(ProductViewModel productViewModel, IFormFile? file)
        {
           
            if (ModelState.IsValid)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if(file != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath = Path.Combine(wwwRootPath, @"images\product");

                    if (!string.IsNullOrEmpty(productViewModel.Product.ImageUrl))
                    {
                        //delete the old image
                        var oldImagePath = Path.Combine(wwwRootPath, productViewModel.Product.ImageUrl.TrimStart('\\'));
                        if(System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName),FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }

                    productViewModel.Product.ImageUrl = @"\images\product\" + fileName;
                }

                //if (!string.IsNullOrEmpty(productViewModel.Product.Id.ToString()))
                if (productViewModel.Product.Id==0)
                {
                    _unitOfWork.Product.Add(productViewModel.Product);
                }
                else
                {
                    _unitOfWork.Product.Update(productViewModel.Product);
                }

                _unitOfWork.Save();
                TempData["success"] = "Product created successfully!";
                return RedirectToAction("Index");
            }
            else
            {
                productViewModel.CategoryList = _unitOfWork.Category.GetAll()
                .Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                });
            }
            return View(productViewModel);
        }


        //[HttpGet]
        //public IActionResult Edit(int id)
        //{
        //    if (id == null || id == 0)
        //    {
        //        return NotFound();
        //    }
        //    Product? ProductFromDb = _unitOfWork.Product.GetFirstOrDefault(u => u.Id == id);
        //    //Product? ProductFromDb2 = _db.Categories.FirstOrDefault(u => u.Id==id);
        //    //Product? ProductFromDb3= _db.Categories.Where(u => u.Id == id).FirstOrDefault();
        //    if (ProductFromDb == null)
        //    {
        //        return NotFound();
        //    }
        //    return View(ProductFromDb);
        //}

        //[HttpPost]
        //public IActionResult Edit(Product obj)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        _unitOfWork.Product.Update(obj);
        //        _unitOfWork.Save();
        //        TempData["success"] = "Product updated successfully!";
        //        return RedirectToAction("Index");
        //    }
        //    return View();
        //}

        

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Product> objProductList = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
            return Json(new {data = objProductList});
        }

        //[HttpDelete]
        public IActionResult Delete(int? id)
        {
            var productToBeDeleted = _unitOfWork.Product.GetFirstOrDefault(u=>u.Id == id);
            if(productToBeDeleted == null)
            {
                return Json(new { success = false, message = "Couldn't be deleted there was an error" });
            }
            var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, 
                               productToBeDeleted.ImageUrl.TrimStart('\\'));

            if (System.IO.File.Exists(oldImagePath))
            {
                System.IO.File.Delete(oldImagePath);
            }

            _unitOfWork.Product.Remove(productToBeDeleted);
            _unitOfWork.Save();

            return Json(new { success = true, message = "Product deleted successfully" });
        }
        #endregion
    }
}
