namespace Proy_DSWI_NinaJose.Models.ViewModels
{
    public class OrderItem
    {
        public string Nombre { get; set; } = "";
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal => Cantidad * PrecioUnitario;
    }
}
