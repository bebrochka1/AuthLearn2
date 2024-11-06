using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication1;
using WebApplication1.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddAuthentication("Cookies")
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/accessdenied";
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

if(!app.Environment.IsDevelopment())
{
    var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

app.MapGet("/", async (context) =>
{
    context.Response.ContentType = "text/html";
    string menu = @"<!DOCTYPE html>
    <html>
    <head>
        <meta charset='utf-8' />
        <title>METANIT.COM</title>
    </head>
    <body>
        <h2>Authentication app</h2><br>
        <button><a href='/main'>Info about authenticated user</a></button><br>
        <button><a href='/register'>Register</a></button><br>
        <button><a href='/login'>login</a></button><br>
        <button><a href='/logout'>logout</a></button><br>
        <button><a href='/admin'>admin page</a></button><br>
    </body>
    </html>";
    await context.Response.WriteAsync(menu);
});

app.MapGet("/register", async (context) =>
{
    string registerForm = @"<!DOCTYPE html>
    <html>
    <head>
        <meta charset='utf-8' />
        <title>METANIT.COM</title>
    </head>
    <body>
        <h2>Login Form</h2>
        <form method='post'>
            <p>
                <label>Name</label><br />
                <input name='userName' />
            </p>
            <p>
                <label>Email</label><br />
                <input name='email' />
            </p>
            <p>
                <label>Password</label><br />
                <input type='password' name='password' />
            </p>
            <input type='submit' value='Register' />
        </form>
    </body>
    </html>";

    await context.Response.WriteAsync(registerForm);
});

app.MapPost("/register", async (HttpContext httpContext,AppDbContext dbContext) =>
{
    var form = httpContext.Request.Form;

    string? userName = form["userName"];
    string? email = form["email"];
    string? password = form["password"];

    if(!form.ContainsKey("email") || !form.ContainsKey("password") || !form.ContainsKey("userName"))
    {
        return Results.BadRequest("No data provided");
    }

    var user = new User { Name = userName, Email = email, Password = password, RoleId = 2 };
    if (user is not null)
    {
        var claims = new List<Claim>() { 
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(ClaimTypes.Role, "User")
        };

        var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        await httpContext.SignInAsync(claimsPrincipal);

        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();
    }

    return Results.Redirect("/");
});

app.MapGet("/login", async (context) =>
{
    var loginForm = @"<!DOCTYPE html>
    <html>
    <head>
        <meta charset='utf-8' />
        <title>METANIT.COM</title>
    </head>
    <body>
        <h2>Login Form</h2>
        <form method='post'>
            <p>
                <label>Email</label><br />
                <input name='email' />
            </p>
            <p>
                <label>Password</label><br />
                <input type='password' name='password' />
            </p>
            <input type='submit' value='Login' />
        </form>
    </body>
    </html>";

    await context.Response.WriteAsync(loginForm);
});

app.MapGet("/authenticated", [Authorize] () => "you are authenticated user!");

app.MapPost("/login", async (HttpContext httpContext, AppDbContext dbContext) =>
{
    var form = httpContext.Request.Form;

    string? email = form["email"];
    string? password = form["password"];

    if (!form.ContainsKey("email") || !form.ContainsKey("password"))
    {
        return Results.BadRequest("No data provided");
    }

    var userInDb = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email && u.Password == password);

    if (userInDb is null) return Results.Unauthorized();

    var userRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Id == userInDb.RoleId);

    var claims = new List<Claim> {
        new Claim(ClaimTypes.Name, userInDb.Email),
        new Claim(ClaimTypes.Role, userRole.Name)
    };
    var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
    var  claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
    await httpContext.SignInAsync(claimsPrincipal);

    return Results.Redirect("/");
});

app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync("Cookies");
    return Results.Redirect("/login");
});

app.MapGet("/admin", [Authorize(Roles = "Admin")] async (context) =>
{
    if (!context.User.IsInRole("Admin")) context.Response.StatusCode = 403;
    await context.Response.WriteAsync("Admin panel");
});

app.MapGet("/main", [Authorize(Roles = "Admin, User")] async (context) =>
{
    var login = context.User.FindFirst(ClaimsIdentity.DefaultNameClaimType);
    var role = context.User.FindFirst(ClaimsIdentity.DefaultRoleClaimType);
    await context.Response.WriteAsync($"User: {login?.Value} Role: {role?.Value}");
});

app.MapGet("/accessdenied", async (context) =>
{
    context.Response.StatusCode = 403;
    //await context.Response.WriteAsync("Access denied");
});

app.Run();