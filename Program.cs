using Microsoft.AspNetCore.Http.HttpResults;


var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "olá  !");

app.MapPost("/login", (MinimalApi.DTOs.loginDTO loginDTO) =>
{
    if(loginDTO.Email == "adm@teste.com" && loginDTO.Senha == "123456")
    {
        return Results.Ok("Login Com sucesso!");}
    else
    {
        return Results.Unauthorized();
    }
});

app.Run();

