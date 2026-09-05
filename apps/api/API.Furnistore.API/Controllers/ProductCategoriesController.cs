using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using API.Furnistore.Data;
using API.Furnistore.Shared;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Authorization;

namespace API.Furnistore.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductCategoriesController : ControllerBase
    {
        private readonly APIFurnistoreContext _context;

         public ProductCategoriesController(APIFurnistoreContext context)
         {
            _context = context;
         }

        // El catálogo se muestra antes de iniciar sesión, así que las lecturas son públicas.
        [AllowAnonymous]
        [HttpGet]
        public async Task<IEnumerable<ProductCategory>> Get()
        {
            return await _context.ProductCategories.ToListAsync();
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetails(int id)
        {
            var productCategory = await _context.ProductCategories.FirstOrDefaultAsync(p => p.Id == id);

            if (productCategory == null)
            {
                return NotFound();
            }

            return Ok(productCategory);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Post(ProductCategory productCategory)
        {

            await _context.ProductCategories.AddAsync(productCategory);
            await _context.SaveChangesAsync();

            return CreatedAtAction("Post", productCategory.Id, productCategory);
        }

         [Authorize]
         [HttpPut]
         public async Task<IActionResult> Put(ProductCategory productCategory)
        {
            _context.ProductCategories.Update(productCategory);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> Delete(ProductCategory productCategory)
        {
            if (productCategory == null) return NotFound();

            _context.ProductCategories.Remove(productCategory);

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}