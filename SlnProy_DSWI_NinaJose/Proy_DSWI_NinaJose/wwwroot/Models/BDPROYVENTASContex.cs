// Models/BDPROYVENTASContex.cs
using Microsoft.EntityFrameworkCore;

namespace Proy_DSWI_NinaJose.Models
{
    public class BDPROYVENTASContex : DbContext
    {
        public BDPROYVENTASContex(DbContextOptions<BDPROYVENTASContex> options)
            : base(options)
        {
        }

        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Categoria> Categorias { get; set; } = null!;
        public DbSet<Producto> Productos { get; set; } = null!;
        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Orden> Ordenes { get; set; } = null!;
        public DbSet<OrdenDetalle> OrdenDetalles { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseCollation("Modern_Spanish_CI_AI");

            // ─── Producto ───────────────────────────────────────────
            modelBuilder.Entity<Producto>(entity =>
            {
                entity.HasKey(e => e.IdProducto).HasName("PK_Productos");
                entity.Property(e => e.IdProducto).ValueGeneratedOnAdd();
                entity.Property(e => e.Nombre)
                      .IsRequired()
                      .HasMaxLength(150);
                entity.Property(e => e.Descripcion)
                      .HasMaxLength(500);
                entity.Property(e => e.Precio)
                      .HasColumnType("decimal(18,2)");
                entity.Property(e => e.Stock);
                entity.Property(e => e.ImagenUrl)
                      .HasMaxLength(255)
                      .IsUnicode(false);

                entity.HasOne(e => e.Categoria)
                      .WithMany(c => c.Productos)
                      .HasForeignKey(e => e.IdCategoria)
                      .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasMany(e => e.OrdenDetalles)
                      .WithOne(d => d.Producto)
                      .HasForeignKey(d => d.IdProducto)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ─── Usuario ────────────────────────────────────────────
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasKey(e => e.IdUsuario).HasName("PK_Usuarios");
                entity.Property(e => e.IdUsuario).ValueGeneratedOnAdd();
                entity.Property(e => e.Nombre)
                      .IsRequired()
                      .HasMaxLength(100);
                entity.Property(e => e.Email)
                      .IsRequired()
                      .HasMaxLength(100);
                entity.Property(e => e.PasswordHash)
                      .IsRequired()
                      .HasMaxLength(255);
                entity.Property(e => e.Rol)
                      .IsRequired()
                      .HasMaxLength(50)
                      .HasDefaultValue("Cliente");

                entity.HasMany(e => e.Ordenes)
                      .WithOne(o => o.Usuario)
                      .HasForeignKey(o => o.IdUsuario)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ─── Orden ──────────────────────────────────────────────
            modelBuilder.Entity<Orden>(entity =>
            {
                entity.HasKey(e => e.IdOrden).HasName("PK_Ordenes");
                entity.Property(e => e.IdOrden).ValueGeneratedOnAdd();
                entity.Property(e => e.Fecha)
                      .HasColumnType("datetime")
                      .IsRequired();
                entity.Property(e => e.Total)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();
                entity.Property(e => e.Estado)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.HasMany(e => e.OrdenDetalles)
                      .WithOne(d => d.Orden)
                      .HasForeignKey(d => d.IdOrden)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ─── OrdenDetalle ───────────────────────────────────────
            modelBuilder.Entity<OrdenDetalle>(entity =>
            {
                // Definimos la PK sobre la propiedad IdDetalle,
                // que hemos anotado para mapearla a la columna IdOrdenDetalle.
                entity.HasKey(d => d.IdDetalle)
                      .HasName("PK_OrdenDetalles");
                entity.Property(d => d.IdDetalle)
                      .HasColumnName("IdOrdenDetalle")
                      .ValueGeneratedOnAdd();

                entity.Property(d => d.Cantidad)
                      .IsRequired();
                entity.Property(d => d.PrecioUnitario)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();

                entity.HasOne(d => d.Orden)
                      .WithMany(o => o.OrdenDetalles)
                      .HasForeignKey(d => d.IdOrden)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Producto)
                      .WithMany(p => p.OrdenDetalles)
                      .HasForeignKey(d => d.IdProducto)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
