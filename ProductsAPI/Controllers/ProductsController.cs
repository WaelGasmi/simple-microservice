using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductsAPI.Data;
using Shared.Models;

namespace ProductsAPI.Controllers
{
    /// <summary>
    /// CRUD des produits. Chaque création est publiée dans RabbitMQ
    /// (message <see cref="ProductCreated"/>) afin d'être consommée par OrdersAPI.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class ProductsController : ControllerBase
    {
        private readonly ProductsAPIContext _context;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(
            ProductsAPIContext context,
            IPublishEndpoint publishEndpoint,
            ILogger<ProductsController> logger)
        {
            _context = context;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        /// <summary>Retourne la liste de tous les produits.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Product>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return await _context.Product.AsNoTracking().ToListAsync();
        }

        /// <summary>Retourne un produit par son identifiant.</summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Product.FindAsync(id);

            if (product == null)
            {
                return NotFound(new { message = $"Produit {id} introuvable." });
            }

            return product;
        }

        /// <summary>Met à jour un produit existant.</summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PutProduct(int id, Product product)
        {
            if (id != product.ProductId)
            {
                return BadRequest(new { message = "L'id de la route ne correspond pas à l'id du produit." });
            }

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound(new { message = $"Produit {id} introuvable." });
                }

                throw;
            }

            return NoContent();
        }

        /// <summary>
        /// Crée un produit puis publie l'événement <see cref="ProductCreated"/> dans RabbitMQ.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Product), StatusCodes.Status201Created)]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            _context.Product.Add(product);
            await _context.SaveChangesAsync();

            try
            {
                // Le message part dans l'exchange fanout de RabbitMQ, OrdersAPI le consomme.
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _publishEndpoint.Publish<ProductCreated>(
                    new ProductCreated
                    {
                        Id = product.ProductId,
                        Name = product.Name
                    },
                    cts.Token);

                _logger.LogInformation("ProductCreated publié pour le produit {ProductId}.", product.ProductId);
            }
            catch (Exception ex)
            {
                // RabbitMQ n'est pas démarré : le CRUD continue de fonctionner, la synchro est juste ignorée.
                _logger.LogWarning(ex, "Publication de ProductCreated impossible (RabbitMQ est-il démarré sur le port 4001 ?).");
            }

            return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, product);
        }

        /// <summary>Supprime un produit.</summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Product.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { message = $"Produit {id} introuvable." });
            }

            _context.Product.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProductExists(int id)
        {
            return _context.Product.Any(e => e.ProductId == id);
        }
    }
}
