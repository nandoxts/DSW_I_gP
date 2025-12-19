USE BD_PROY_VENTA;
GO

/* 1) Eliminar tablas existentes (en orden inverso a las FK) */
IF OBJECT_ID('OrdenDetalles','U')   IS NOT NULL DROP TABLE OrdenDetalles;
IF OBJECT_ID('Ordenes','U')         IS NOT NULL DROP TABLE Ordenes;
IF OBJECT_ID('Usuarios','U')        IS NOT NULL DROP TABLE Usuarios;
IF OBJECT_ID('Productos','U')       IS NOT NULL DROP TABLE Productos;
IF OBJECT_ID('Categorias','U')      IS NOT NULL DROP TABLE Categorias;
GO

/* 2) Crear tablas */

/* 2.1 Categorías */
CREATE TABLE Categorias (
    IdCategoria   INT           IDENTITY(1,1) PRIMARY KEY,
    Nombre        NVARCHAR(100) NOT NULL,
    Descripcion   NVARCHAR(500) NULL
);
GO

/* 2.2 Productos (ImagenUrl calculado a partir de IdProducto) */
CREATE TABLE Productos (
    IdProducto    INT           IDENTITY(1,1) PRIMARY KEY,
    Nombre        NVARCHAR(150) NOT NULL,
    Descripcion   NVARCHAR(500) NULL,
    Precio        DECIMAL(18,2) NOT NULL,
    Stock         INT           NOT NULL,
    IdCategoria   INT           NOT NULL,
    ImagenUrl     AS (
        '/productos_catalogo/'
        + 'A'
        + RIGHT('0000' + CAST(IdProducto AS VARCHAR(4)), 4)
        + '.jpg'
    ) PERSISTED,
    CONSTRAINT FK_Productos_Categorias
       FOREIGN KEY (IdCategoria)
       REFERENCES Categorias(IdCategoria)
       ON DELETE NO ACTION
);
GO

/* 2.3 Usuarios */
CREATE TABLE Usuarios (
    IdUsuario     INT           IDENTITY(1,1) PRIMARY KEY,
    Nombre        NVARCHAR(100) NOT NULL,
    Email         NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash  NVARCHAR(255) NOT NULL,
    Rol           NVARCHAR(50)  NOT NULL DEFAULT('Cliente')
);
GO

/* 2.4 Órdenes */
CREATE TABLE Ordenes (
    IdOrden       INT           IDENTITY(1,1) PRIMARY KEY,
    IdUsuario     INT           NOT NULL,
    Fecha         DATETIME      NOT NULL DEFAULT(GETDATE()),
    Total         DECIMAL(18,2) NOT NULL,
    Estado        NVARCHAR(50)  NOT NULL DEFAULT('Pendiente'),
    CONSTRAINT FK_Ordenes_Usuarios
       FOREIGN KEY (IdUsuario)
       REFERENCES Usuarios(IdUsuario)
       ON DELETE NO ACTION
);
GO

/* 2.5 Detalles de Orden */
CREATE TABLE OrdenDetalles (
    IdOrdenDetalle   INT           IDENTITY(1,1) PRIMARY KEY,
    IdOrden          INT           NOT NULL,
    IdProducto       INT           NOT NULL,
    Cantidad         INT           NOT NULL,
    PrecioUnitario   DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_Detalle_Ordenes
       FOREIGN KEY (IdOrden)
       REFERENCES Ordenes(IdOrden)
       ON DELETE NO ACTION,
    CONSTRAINT FK_Detalle_Productos
       FOREIGN KEY (IdProducto)
       REFERENCES Productos(IdProducto)
       ON DELETE NO ACTION
);
GO

/* 3) Seed inicial */

/* 3.1 Categorías base */
INSERT INTO Categorias (Nombre, Descripcion) VALUES
 ('Textiles',              'Ponchos y tejidos tradicionales peruanos'),
 ('Cerámica',              'Figuras de cerámica de costa y sierra'),
 ('Joyería',               'Collares y pulseras artesanales'),
 ('Esculturas',            'Obras en madera y piedra de alta calidad'),
 ('Pintura',               'Lienzos y cuadros con motivos andinos'),
 ('Madera',                'Tallados y mobiliario en maderas nobles'),
 ('Metalurgia',            'Piezas de metal forjado y repujado'),
 ('Vidrio',                'Objetos de vidrio soplado y esmaltado'),
 ('Cuero',                 'Accesorios y artículos en cuero genuino'),
 ('Tapices',               'Textiles decorativos de diferentes regiones'),
 ('Cestería',              'Canastos y cestas tejidas a mano'),
 ('Orfebrería',            'Piezas de oro y plata de diseño fino'),
 ('Cerámica Decorativa',   'Vajillas y adornos de cerámica pintada'),
 ('Arte Urbano',           'Murales y piezas de arte contemporáneo'),
 ('Instrumentos',          'Instrumentos musicales tradicionales');
GO

/* 3.2 Productos de ejemplo */
INSERT INTO Productos (Nombre, Descripcion, Precio, Stock, IdCategoria) VALUES
 ('Poncho Andino',    'Poncho de alpaca colores tradicionales.',    120.00, 10, 1),
 ('Manta Cusqueña',   'Manta fina de Cusco con motivos geométricos.', 85.50, 15, 1),
 ('Figura Nazca',     'Cerámica pintada a mano de cultura Nazca.',   45.00, 25, 2),
 ('Vasija Chulucanas','Vasija esmaltada de Chulucanas.',            60.00, 20, 2),
 ('Collar Plata',     'Collar de plata 950 con lapislázuli.',       150.00,  8, 3),
 ('Pulsera Andina',   'Pulsera con cuentas de plata y semillas.',     35.00, 30, 3);
GO

/* 3.3 Usuarios de ejemplo */
INSERT INTO Usuarios (Nombre, Email, PasswordHash, Rol) VALUES
 ('José Nina',   'josenina@gmail.com',   '202216351', 'Admin'),
 ('Demo User',   'usuario@gmail.com',    '202216351', 'Cliente');
GO

/* 4) Agregar más categorías */
INSERT INTO Categorias (Nombre) VALUES
('Esculturas'),('Pintura'),('Madera'),('Metalurgia'),
('Vidrio'),('Cuero'),('Tapices'),('Cestería'),
('Orfebrería'),('Cerámica Decorativa'),('Arte Urbano'),
('Instrumentos'),('Textiles Tradicionales'),('Arte Contemporáneo');
GO

INSERT INTO Productos (Nombre, Descripcion, Precio, Stock, IdCategoria) VALUES
-- Textiles (IdCategoria = 1)
('Poncho Andino Tradicional', 'Poncho de alpaca con diseño multicolor y acabado artesano.',         180.00, 12, 1),
('Manta Cusqueña de Alpaca',   'Manta tejida en alpaca pura con motivos geométricos andinos.',64.50, 20, 1),
('Bufanda de Alpaca Premium',  'Bufanda larga de alpaca fina, suave y térmica.',                  75.00, 25, 1),
-- Cerámica (IdCategoria = 2)
('Figura Nazca Pintada',       'Figura de cerámica pintada a mano inspirada en dibujos Nazca.', 55.00, 30, 2),
('Vasija Chulucanas Artesanal','Vasija esmaltada tradicional de Chulucanas, acabado bruñido.',    80.00, 15, 2),
('Taza de Barro Decorativa',   'Juego de 4 tazas de barro con engobe y motivos coloniales.',      95.00, 10, 2),
-- Joyería (IdCategoria = 3)
('Collar de Plata y Lapislázuli','Collar de plata 950 con colgante de lapislázuli pulido.',       220.00,  7, 3),
('Pulsera de Filigrana',       'Pulsera de plata finamente trabajada con técnica de filigrana.',180.00, 10, 3),
('Aretes de Plata Incaicos',   'Aretes de plata con grabado inspirado en iconografía inca.',      85.00, 20, 3),
-- Esculturas (IdCategoria = 4)
('Estatuilla Inca en Madera',  'Escultura pequeña tallada en madera de cedro con acabado natural.',120.00,  5, 4),
('Máscara de Madera Artesanal','Máscara ceremonial tallada a mano en madera de flores.',          150.00,  8, 4),
-- Pintura (IdCategoria = 5)
('Cuadro Paisaje Andino',      'Óleo sobre lienzo con paisaje de los Andes y laguna brillante.',     350.00,  3, 5),
('Pintura de Alpacas',         'Acuarela sobre papel artesanal con grupo de alpacas pastando.',      95.00, 10, 5),
-- Madera (IdCategoria = 6)
('Lámpara de Madera Tallada',  'Lámpara de mesa en madera de nogal con calados geométricos.',       260.00,  4, 6),
('Tablero de Ajedrez Artesanal','Tablero de ajedrez en madera de cerezo y nogal con piezas talladas.',450.00,  2, 6),
-- Metalurgia (IdCategoria = 7)
('Campana Andina de Cobre',    'Campana decorativa fabricada en cobre con repujado manual.',         130.00,  6, 7),
('Portavelas Latón Repujado',  'Portavelas en latón con detalles florales repujados a mano.',        90.00, 12, 7),
-- Vidrio (IdCategoria = 8)
('Florero de Vidrio Soplado',  'Florero en vidrio soplado con vetas de color azul y verde.',          140.00,  8, 8),
('Móvil de Vidrio Coloreado',  'Móvil de piezas de vidrio esmaltado, ideal para decoración.',        110.00,  7, 8),
-- Cuero (IdCategoria = 9)
('Cartera de Cuero Artesanal', 'Cartera de cuero curtido natural, con costuras reforzadas.',          125.00, 15, 9),
('Llavero de Cuero Repujado',  'Llavero en cuero con repujado de motivos andinos y anilla metálica.',  25.00, 40, 9),
-- Tapices (IdCategoria = 10)
('Tapiz Mural Andino',         'Tapiz tejido a mano con motivos de flora y fauna andina.',            300.00,  2,10),
('Alfombra Pequeña de Lana',   'Alfombra tejida en lana de oveja con patrones tradicionales.',       200.00,  5,10),
-- Cestería (IdCategoria = 11)
('Cesta de Cestería Mixta',    'Cesta tejida en totora y carrizo, resistente y decorativa.',         75.00, 20,11),
('Canasta de Frutas Natural',  'Canasta tejida a mano con fibras de sisal, ideal para fruta.',       60.00, 25,11),
-- Orfebrería (IdCategoria = 12)
('Anillo de Oro 14K',          'Anillo sencillo de oro 14 quilates con diseño minimalista.',        800.00,  3,12),
('Broche de Plata y Ónice',    'Broche de plata con incrustación de ónice negro pulido.',            180.00,  6,12),
-- Cerámica Decorativa (IdCategoria = 13)
('Juego de Platos Decorativos','Juego de 4 platos de cerámica decorativos con relieve artesanal.',    160.00,  8,13),
('Maceta de Cerámica Coloreada','Maceta pintada a mano con motivos florales vivos.',                   55.00, 18,13),
-- Arte Urbano (IdCategoria = 14)
('Mural Miniatura Peruano',    'Miniatura de mural urbano en madera con pintura acrílica.',          120.00,  4,14),
('Stencil Callejero Andino',   'Stencil decorativo en metal con siluetas de cóndor y montañas.',       85.00, 12,14),
-- Instrumentos (IdCategoria = 15)
('Zampoña Tradicional',        'Instrumento andino de cañas afinadas, sonido auténtico.',            220.00,  5,15),
('Charango de Madera',         'Charango con caja de resonancia en madera de nogal y cuerdas de nylon.',450.00,  2,15);
GO


PRINT '✔️ Esquema, seed inicial y 100 productos generados correctamente.';
