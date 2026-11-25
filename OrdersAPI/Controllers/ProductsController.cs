using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersAPI.Data;

namespace OrdersAPI.Controllers
{
    /// <summary>
    /// Lecture de la table Products locale, alimentée de façon asynchrone par
    /// RabbitMQ (message <c>ProductCreated</c> publié par ProductsAPI).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class ProductsController : ControllerBase
    {
        private readonly OrdersAPIContext _context;

        public ProductsController(OrdersAPIContext context)
        {
            _context = context;
        }

        /// <summary>Retourne les produits synchronisés depuis ProductsAPI.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Product>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Product>>> GetSyncedProducts()
        {
            return await _context.Products.AsNoTracking().ToListAsync();
        }

        /// <summary>Retourne un produit synchronisé par son identifiant local.</summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Product>> GetSyncedProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound(new { message = $"Produit synchronisé {id} introuvable." });
            }

            return product;
        }
    }
}
