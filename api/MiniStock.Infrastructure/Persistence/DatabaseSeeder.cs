using Microsoft.EntityFrameworkCore;
using MiniStock.Domain.Entities;

namespace MiniStock.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    private static readonly Guid AdminRoleId = new("00000000-0000-0000-0000-000000000001");

    public static async Task SeedAsync(AppDbContext db)
    {
        var adminUser = await db.Users.FirstOrDefaultAsync(u => u.Email == "admin@ministock.com");
        if (adminUser == null)
        {
            var hash = BCrypt.Net.BCrypt.HashPassword("Admin123!");
            var newAdmin = User.Create("Admin", "admin@ministock.com", hash, AdminRoleId);
            db.Users.Add(newAdmin);
            await db.SaveChangesAsync();
            adminUser = newAdmin;
        }

        if (await db.Categories.AnyAsync()) return;

        var electronica   = Category.Create("Electrónica",   "Dispositivos electrónicos y accesorios");
        var herramientas  = Category.Create("Herramientas",  "Herramientas manuales y eléctricas");
        var oficina       = Category.Create("Oficina",       "Artículos de oficina y papelería");
        var limpieza      = Category.Create("Limpieza",      "Productos de limpieza e higiene");
        db.Categories.AddRange(electronica, herramientas, oficina, limpieza);
        await db.SaveChangesAsync();

        var laptop         = Product.Create("Laptop Dell Inspiron 15",    "LAP-001", 899.99m,  15, 5,  electronica.Id,  "Intel i5, 8GB RAM, 256GB SSD");
        var monitor        = Product.Create("Monitor Samsung 24\" FHD",   "MON-001", 249.99m,  12, 3,  electronica.Id,  "Full HD 1080p, HDMI, DisplayPort");
        var teclado        = Product.Create("Teclado Mecánico RGB",        "TEC-001",  79.99m,   8, 5,  electronica.Id,  "Switch Blue, retroiluminado");
        var martillo       = Product.Create("Martillo Stanley 16oz",       "HER-001",  24.99m,  30, 8,  herramientas.Id, "Mango de fibra de vidrio");
        var destornillador = Product.Create("Set Destornilladores x12",    "HER-002",  18.99m,  45, 10, herramientas.Id, "Phillips y plano, magnéticos");
        var resma          = Product.Create("Resma A4 500 hojas",          "PAP-001",   8.99m, 200, 50, oficina.Id,      "75g/m², blancura 92%");
        var boligrafos     = Product.Create("Bolígrafos BIC Azul x12",     "PAP-002",   5.49m, 150, 30, oficina.Id,      "Punta media 1.0mm");
        var lavandina      = Product.Create("Lavandina 1L Concentrada",    "LIM-001",   2.99m,  80, 20, limpieza.Id,     "Concentración 55g/L");
        var desinfectante  = Product.Create("Desinfectante Spray 500ml",   "LIM-002",   6.49m,  60, 15, limpieza.Id,     "Aroma lavanda, mata 99.9% gérmenes");
        db.Products.AddRange(laptop, monitor, teclado, martillo, destornillador, resma, boligrafos, lavandina, desinfectante);
        await db.SaveChangesAsync();

        var adminId = adminUser.Id;
        db.StockMovements.AddRange(
            StockMovement.Create(laptop.Id,         20, MovementType.Entry,      adminId, "Compra inicial — proveedor TechStore"),
            StockMovement.Create(monitor.Id,        15, MovementType.Entry,      adminId, "Compra inicial — proveedor TechStore"),
            StockMovement.Create(teclado.Id,        10, MovementType.Entry,      adminId, "Compra inicial — proveedor Gaming Pro"),
            StockMovement.Create(martillo.Id,       30, MovementType.Entry,      adminId, "Compra inicial — proveedor Ferremax"),
            StockMovement.Create(destornillador.Id, 50, MovementType.Entry,      adminId, "Compra inicial — proveedor Ferremax"),
            StockMovement.Create(resma.Id,         250, MovementType.Entry,      adminId, "Reposición mensual — proveedor Papelera Norte"),
            StockMovement.Create(laptop.Id,          5, MovementType.Exit,       adminId, "Venta — cliente Empresa ABC S.A."),
            StockMovement.Create(monitor.Id,         3, MovementType.Exit,       adminId, "Venta — cliente Juan Pérez"),
            StockMovement.Create(teclado.Id,         2, MovementType.Exit,       adminId, "Venta — cliente Freelancer MR"),
            StockMovement.Create(resma.Id,          50, MovementType.Exit,       adminId, "Consumo interno oficina"),
            StockMovement.Create(lavandina.Id,     100, MovementType.Entry,      adminId, "Compra trimestral"),
            StockMovement.Create(lavandina.Id,      20, MovementType.Exit,       adminId, "Distribución sucursales"),
            StockMovement.Create(destornillador.Id,  5, MovementType.Adjustment, adminId, "Ajuste por merma — conteo físico Q1")
        );
        await db.SaveChangesAsync();
    }
}
