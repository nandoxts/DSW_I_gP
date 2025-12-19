USE BD_PROY_VENTA;
GO

/* --- Categorías --- */
CREATE OR ALTER PROCEDURE PA_ListarCategorias
AS
BEGIN
    SELECT * FROM Categorias;
END;
GO

CREATE OR ALTER PROCEDURE PA_InsertarCategoria
    @Nombre      NVARCHAR(100),
    @Descripcion NVARCHAR(500) = NULL
AS
BEGIN
    INSERT INTO Categorias (Nombre, Descripcion)
    VALUES (@Nombre, @Descripcion);
END;
GO

CREATE OR ALTER PROCEDURE PA_ActualizarCategoria
    @IdCategoria INT,
    @Nombre      NVARCHAR(100),
    @Descripcion NVARCHAR(500)
AS
BEGIN
    UPDATE Categorias
       SET Nombre      = @Nombre,
           Descripcion = @Descripcion
     WHERE IdCategoria = @IdCategoria;
END;
GO

CREATE OR ALTER PROCEDURE PA_EliminarCategoria
    @IdCategoria INT
AS
BEGIN
    DELETE FROM Categorias
     WHERE IdCategoria = @IdCategoria;
END;
GO


/* --- Productos --- */
CREATE OR ALTER PROCEDURE PA_ListarProductos
AS
BEGIN
    SELECT p.IdProducto,
           p.Nombre,
           p.Descripcion,
           p.Precio,
           p.Stock,
           p.IdCategoria,
           p.ImagenUrl,
           c.Nombre AS CategoriaNombre
      FROM Productos p
      JOIN Categorias c 
        ON p.IdCategoria = c.IdCategoria;
END;
GO

CREATE OR ALTER PROCEDURE PA_DetalleProducto
    @IdProducto INT
AS
BEGIN
    SELECT p.IdProducto,
           p.Nombre,
           p.Descripcion,
           p.Precio,
           p.Stock,
           p.IdCategoria,
           p.ImagenUrl,
           c.Nombre AS CategoriaNombre
      FROM Productos p
      JOIN Categorias c 
        ON p.IdCategoria = c.IdCategoria
     WHERE p.IdProducto = @IdProducto;
END;
GO

CREATE OR ALTER PROCEDURE PA_InsertarProducto
    @Nombre      NVARCHAR(150),
    @Descripcion NVARCHAR(500) = NULL,
    @Precio      DECIMAL(18,2),
    @Stock       INT,
    @IdCategoria INT
AS
BEGIN
    INSERT INTO Productos
      (Nombre, Descripcion, Precio, Stock, IdCategoria)
    VALUES
      (@Nombre, @Descripcion, @Precio, @Stock, @IdCategoria);
END;
GO

CREATE OR ALTER PROCEDURE PA_ActualizarProducto
    @IdProducto  INT,
    @Nombre      NVARCHAR(150),
    @Descripcion NVARCHAR(500),
    @Precio      DECIMAL(18,2),
    @Stock       INT,
    @IdCategoria INT
AS
BEGIN
    UPDATE Productos
       SET Nombre      = @Nombre,
           Descripcion = @Descripcion,
           Precio      = @Precio,
           Stock       = @Stock,
           IdCategoria = @IdCategoria
     WHERE IdProducto = @IdProducto;
END;
GO

CREATE OR ALTER PROCEDURE PA_EliminarProducto
    @IdProducto INT
AS
BEGIN
    DELETE FROM Productos
     WHERE IdProducto = @IdProducto;
END;
GO


/* --- Usuarios --- */
CREATE OR ALTER PROCEDURE PA_ListarUsuarios
AS
BEGIN
    SELECT * FROM Usuarios;
END;
GO

CREATE OR ALTER PROCEDURE PA_InsertarUsuario
    @Nombre       NVARCHAR(100),
    @Email        NVARCHAR(100),
    @PasswordHash NVARCHAR(255),
    @Rol          NVARCHAR(50) = 'Cliente'
AS
BEGIN
    INSERT INTO Usuarios (Nombre, Email, PasswordHash, Rol)
    VALUES (@Nombre, @Email, @PasswordHash, @Rol);
END;
GO

CREATE OR ALTER PROCEDURE PA_ActualizarUsuario
    @IdUsuario    INT,
    @Nombre       NVARCHAR(100),
    @Email        NVARCHAR(100),
    @PasswordHash NVARCHAR(255),
    @Rol          NVARCHAR(50)
AS
BEGIN
    UPDATE Usuarios
       SET Nombre       = @Nombre,
           Email        = @Email,
           PasswordHash = @PasswordHash,
           Rol          = @Rol
     WHERE IdUsuario = @IdUsuario;
END;
GO

CREATE OR ALTER PROCEDURE PA_EliminarUsuario
    @IdUsuario INT
AS
BEGIN
    DELETE FROM Usuarios
     WHERE IdUsuario = @IdUsuario;
END;
GO


/* --- Órdenes --- */
CREATE OR ALTER PROCEDURE PA_ListarOrdenes
AS
BEGIN
    SELECT o.IdOrden,
           o.IdUsuario,
           u.Nombre  AS UsuarioNombre,
           o.Fecha,
           o.Total,
           o.Estado
      FROM Ordenes o
      JOIN Usuarios u 
        ON o.IdUsuario = u.IdUsuario;
END;
GO

CREATE OR ALTER PROCEDURE PA_InsertarOrden
    @IdUsuario  INT,
    @Total      DECIMAL(18,2),
    @NewOrderId INT OUTPUT
AS
BEGIN
    INSERT INTO Ordenes (IdUsuario, Total)
    VALUES (@IdUsuario, @Total);
    SET @NewOrderId = SCOPE_IDENTITY();
END;
GO

CREATE OR ALTER PROCEDURE PA_ActualizarOrden
    @IdOrden INT,
    @Estado  NVARCHAR(50)
AS
BEGIN
    UPDATE Ordenes
       SET Estado = @Estado
     WHERE IdOrden = @IdOrden;
END;
GO

CREATE OR ALTER PROCEDURE PA_EliminarOrden
    @IdOrden INT
AS
BEGIN
    DELETE FROM Ordenes
     WHERE IdOrden = @IdOrden;
END;
GO


/* --- Detalles de Orden --- */
CREATE OR ALTER PROCEDURE PA_ListarDetallesPorOrden
    @IdOrden INT
AS
BEGIN
    SELECT d.IdOrdenDetalle,
           d.IdOrden,
           d.IdProducto,
           p.Nombre          AS ProductoNombre,
           d.Cantidad,
           d.PrecioUnitario
      FROM OrdenDetalles d
      JOIN Productos p 
        ON d.IdProducto = p.IdProducto
     WHERE d.IdOrden = @IdOrden;
END;
GO

CREATE OR ALTER PROCEDURE PA_InsertarDetalle
    @IdOrden        INT,
    @IdProducto     INT,
    @Cantidad       INT,
    @PrecioUnitario DECIMAL(18,2)
AS
BEGIN
    INSERT INTO OrdenDetalles (IdOrden, IdProducto, Cantidad, PrecioUnitario)
    VALUES (@IdOrden, @IdProducto, @Cantidad, @PrecioUnitario);
END;
GO

CREATE OR ALTER PROCEDURE PA_ActualizarDetalle
    @IdOrdenDetalle INT,
    @Cantidad       INT
AS
BEGIN
    UPDATE OrdenDetalles
       SET Cantidad = @Cantidad
     WHERE IdOrdenDetalle = @IdOrdenDetalle;
END;
GO

CREATE OR ALTER PROCEDURE PA_EliminarDetalle
    @IdOrdenDetalle INT
AS
BEGIN
    DELETE FROM OrdenDetalles
     WHERE IdOrdenDetalle = @IdOrdenDetalle;
END;
GO

PRINT ' Todos los procedimientos PA_… han sido creados o actualizados correctamente.';
