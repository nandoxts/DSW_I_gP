namespace Proy_DSWI_NinaJose.Models
{
    public class Carrito
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = null!;
        public decimal Precio { get; set; }
        public int Cantidad { get; set; }
        public string? ImagenUrl { get; set; }
        public decimal Subtotal => Precio * Cantidad;
    }
}
