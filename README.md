## About

Teknikal test untuk PT. Aegis Ultima Teknologi.

## Description

Uncomment code ini untuk push roles ke database via running aplikasi

```sh
//using (var scope = app.Services.CreateScope())
//{
//    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

//    await roleManager.CreateAsync(new IdentityRole("Admin"));
//    await roleManager.CreateAsync(new IdentityRole("User"));
//}
```

## Migrate

Migrate EntityFramework pada aplikasi untuk membuat database table

```sh
PM> Update-Database
```
