using EzyTaskin.Data.Model;
using EzyTaskin.Services;
using Microsoft.AspNetCore.Mvc;

namespace EzyTaskin.Controllers;

[Route("api/[controller]")]
public class CategoryController : ControllerBase
{
    private readonly CategoryService _categoryService;

    public CategoryController(CategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public ActionResult<IAsyncEnumerable<Category>> GetCategories()
    {
        return Ok(_categoryService.GetCategories());
    }
}
