namespace Proy_DSWI_NinaJose.Models
{
    public class CartItem
    {
        public int CartItemId { get; set; }
        public string UserId { get; set; } = null!;
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }

        public Producto Producto { get; set; } = null!;
    }
}
