# Proyecto DSWI - Sistema de Ventas

Sistema de gestión de tienda en línea desarrollado en ASP.NET Core 8.0 con Entity Framework Core.
## Descripción

Aplicación web para la venta de productos en línea con funcionalidades de:
## Tecnologías Utilizadas

- ASP.NET Core 8.0
## Dependencias del Proyecto

### NuGet Packages:
## Requisitos Previos

- .NET 8.0 SDK instalado
## Configuración de Base de Datos

1. Abre SQL Server Management Studio
## Instalación y Ejecución

1. Navega al directorio del proyecto:
2. Restaura las dependencias:
```bash
dotnet restore
```
5. Inicia la aplicación:
```bash
dotnet run
```
## Estructura del Proyecto

```
SlnProy_DSWI_NinaJose/
├── Controllers/
│   ├── AccountController.cs          - Autenticación y registro
│   ├── CarritoController.cs          - Gestión del carrito de compras
│   ├── CategoriaController.cs        - Gestión de categorías
│   ├── HomeController.cs             - Página de inicio
│   ├── OrdenController.cs            - Gestión de órdenes
│   ├── ProductoController.cs         - Gestión de productos
│   ├── ProfileController.cs          - Perfil de usuario
│   └── UsuarioController.cs          - Gestión de usuarios
├── Models/
│   ├── BDPROYVENTASContex.cs        - DbContext
│   ├── Carrito.cs
│   ├── CarItem.cs
│   ├── Categoria.cs
│   ├── Orden.cs
│   ├── OrdenDetalle.cs
│   ├── Producto.cs
│   ├── Usuario.cs
│   └── ViewModels/
│       ├── Login.cs
│       ├── LoginViewModel.cs
│       ├── Register.cs
│       ├── RegisterViewModel.cs
│       ├── ProductoCat.cs              - ViewModel para catálogo de productos
│       ├── OrdenConfirmation.cs
│       └── OrdenItem.cs
├── Views/
│   ├── Account/                      - Vistas de autenticación
│   ├── Carrito/                      - Vistas del carrito
│   ├── Home/                         - Vistas de inicio
│   ├── Orden/                        - Vistas de órdenes
│   ├── Productos/                    - Vistas de productos
│   ├── Profile/                      - Vistas de perfil
│   └── Shared/                       - Vistas compartidas
├── wwwroot/                          - Archivos estáticos
│   ├── css/
│   ├── js/
│   └── lib/
├── appsettings.json                  - Configuración de la aplicación
└── Program.cs                        - Configuración de startup
```
## Características Principales

### Autenticación y Autorización
- Sistema de login/registro con cookies
- Validación de credenciales
- Sesión de usuario
### Gestión de Productos

- Catálogo de productos organizado por categorías
- Página de inicio con productos destacados
- Visualización de detalles del producto
- Administración CRUD de productos (solo Admin)
- ViewModel ProductoCat para separación de responsabilidades en la presentación

### Carrito de Compras
- Carrito basado en sesiones
- Agregar/eliminar productos
- Actualizar cantidades
### Órdenes de Compra

- Crear nuevas órdenes
- Historial de órdenes del usuario
- Confirmación de compra
### Perfil de Usuario

- Visualización de información personal
- Actualización de datos
## Variables de Entorno

La aplicación utiliza las siguientes variables de conexión en appsettings.json:
- DefaultConnection: Cadena de conexión a SQL Server

## Notas de Desarrollo
- El proyecto utiliza Nullable Reference Types (C# 11+)
- Las sesiones se almacenan en memoria
- La autenticación se basa en cookies HTTP-only
- ViewModels utilizados para separación de responsabilidades en las vistas
- Uso de LINQ para consultas a base de datos con Entity Framework Core
- Incluye validación de modelos con atributos de datos
## Autores

Proyecto desarrollado por Nina y Jose para el curso DSWI.
## Licencia

Este proyecto es de uso educativo.
# Proyecto DSWI - Sistema de Ventas

Sistema de gestión de tienda en línea desarrollado en ASP.NET Core 8.0 con Entity Framework Core.

## Descripción

Aplicación web para la venta de productos en línea con funcionalidades de:
- Gestión de usuarios y autenticación
- Catálogo de productos por categorías
- Carrito de compras con sesiones
- Gestión de órdenes de compra
- Perfil de usuario

## Tecnologías Utilizadas

- ASP.NET Core 8.0
- Entity Framework Core 9.0.4
- SQL Server
- Bootstrap 5
- jQuery
- Razor Views

## Dependencias del Proyecto

### NuGet Packages:
- Microsoft.EntityFrameworkCore (v9.0.4)
- Microsoft.EntityFrameworkCore.SqlServer (v9.0.4)
- Microsoft.EntityFrameworkCore.Tools (v9.0.4)
- Microsoft.VisualStudio.Web.CodeGeneration.Design (v8.0.7)

## Requisitos Previos

- .NET 8.0 SDK instalado
- SQL Server 2022 (o superior)
- Git

## Configuración de Base de Datos

1. Abre SQL Server Management Studio
2. Ejecuta los scripts en el siguiente orden:
   - BD_PROY_VENTA.sql - Crea la estructura de la base de datos
   - PA_DSWII_PROYECTO.sql - Crea procedimientos almacenados

3. Actualiza la cadena de conexión en appsettings.json:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=xts\\MSSQLSERVER2022;Database=BD_PROY_VENTA;User ID=sa;Password=sql;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```

## Instalación y Ejecución

1. Navega al directorio del proyecto:
```bash
cd SlnProy_DSWI_NinaJose/Proy_DSWI_NinaJose
```

2. Restaura las dependencias:
```bash
dotnet restore
```

3. Compila el proyecto:
```bash
dotnet build
```

4. Ejecuta las migraciones (si es necesario):
```bash
dotnet ef database update
```

5. Inicia la aplicación:
```bash
dotnet run
```

La aplicación estará disponible en: https://localhost:5001

## Estructura del Proyecto

```
SlnProy_DSWI_NinaJose/
├── Controllers/
│   ├── AccountController.cs          - Autenticación y registro
│   ├── CarritoController.cs          - Gestión del carrito de compras
│   ├── CategoriaController.cs        - Gestión de categorías
│   ├── HomeController.cs             - Página de inicio
│   ├── OrdenController.cs            - Gestión de órdenes
│   ├── ProductoController.cs         - Gestión de productos
│   ├── ProfileController.cs          - Perfil de usuario
│   └── UsuarioController.cs          - Gestión de usuarios
├── Models/
│   ├── BDPROYVENTASContex.cs        - DbContext
│   ├── Carrito.cs
│   ├── CarItem.cs
│   ├── Categoria.cs
│   ├── Orden.cs
│   ├── OrdenDetalle.cs
│   ├── Producto.cs
│   ├── Usuario.cs
│   └── ViewModels/
│       ├── Login.cs
│       ├── LoginViewModel.cs
│       ├── Register.cs
│       ├── RegisterViewModel.cs
│       ├── ProductoCat.cs              - ViewModel para catálogo de productos
│       ├── OrdenConfirmation.cs
│       └── OrdenItem.cs
├── Views/
│   ├── Account/                      - Vistas de autenticación
│   ├── Carrito/                      - Vistas del carrito
│   ├── Home/                         - Vistas de inicio
│   ├── Orden/                        - Vistas de órdenes
│   ├── Productos/                    - Vistas de productos
│   ├── Profile/                      - Vistas de perfil
│   └── Shared/                       - Vistas compartidas
├── wwwroot/                          - Archivos estáticos
│   ├── css/
│   ├── js/
│   └── lib/
├── appsettings.json                  - Configuración de la aplicación
└── Program.cs                        - Configuración de startup
```

## Características Principales

### Autenticación y Autorización
- Sistema de login/registro con cookies
- Validación de credenciales
- Sesión de usuario

### Gestión de Productos
- Catálogo de productos organizado por categorías
- Página de inicio con productos destacados
- Visualización de detalles del producto
- Administración CRUD de productos (solo Admin)
- ViewModel ProductoCat para separación de responsabilidades en la presentación

### Carrito de Compras
- Carrito basado en sesiones
- Agregar/eliminar productos
- Actualizar cantidades

### Órdenes de Compra
- Crear nuevas órdenes
- Historial de órdenes del usuario
- Confirmación de compra

### Perfil de Usuario
- Visualización de información personal
- Actualización de datos

## Variables de Entorno

La aplicación utiliza las siguientes variables de conexión en appsettings.json:
- DefaultConnection: Cadena de conexión a SQL Server

## Notas de Desarrollo

- El proyecto utiliza Nullable Reference Types (C# 11+)
- Las sesiones se almacenan en memoria
- La autenticación se basa en cookies HTTP-only
- ViewModels utilizados para separación de responsabilidades en las vistas
- Uso de LINQ para consultas a base de datos con Entity Framework Core
- Incluye validación de modelos con atributos de datos

## Autores

Proyecto desarrollado por Nina y Jose para el curso DSWI.

## Licencia

Este proyecto es de uso educativo.
