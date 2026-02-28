using Microsoft.AspNetCore.Http.HttpResults;
using MinimalApi.Dominio.Entidades;
using MinimalApi.Dominio.Servicos;
using MinimalApi.Dominio.Servicos.Interfaces;
using MinimalApi.DTOs;
using MinimalApi.Infraestrutura.Db;
using Microsoft.AspNetCore.Mvc;
using MinimalApi.Dominio.ModelViews;
using MinimalApi.Dominio.Enuns;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Unicode;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Authorization;


#region Builder

var builder = WebApplication.CreateBuilder(args);

var key = builder.Configuration.GetSection("Jwt").ToString();

if (string.IsNullOrEmpty(key)) key = "123456";

builder.Services.AddAuthentication(option => {
    option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;

    option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(option =>
{
   option.TokenValidationParameters = new TokenValidationParameters
   {
     ValidateLifetime = true,
     IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
     ValidateIssuer = false,
     ValidateAudience = false,
   };
});

builder.Services.AddAuthorization();

builder.Services.AddScoped<IAdministradorServico, AdministradorServico>();
builder.Services.AddScoped<IVeiculoServico, VeiculoServico>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token JWT desta maneira: Bearer {seu token}",
    };

    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", document, null), new List<string>() }
    }); 
});

builder.Services.AddDbContext<DbContexto>();
var app = builder.Build();

#endregion

#region Home

app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous().WithTags("home");
// app.MapGet("/", () => Results.Json( new Home()));

#endregion

#region Administradores

string GerarTokenJwt(Administrador administrador)
{
    if(string.IsNullOrEmpty(key)) return string.Empty;
    
    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));  
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>()
    {
      new Claim("Email", administrador.Email),  
      new Claim( ClaimTypes.Role, administrador.Perfil),  
      new Claim("Perfil", administrador.Perfil),  
    };

    var token = new JwtSecurityToken(
        claims : claims,
        expires : DateTime.Now.AddDays(1),
        signingCredentials : credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
};

app.MapPost("/administradores/login", ( [FromBody] loginDTO loginDTO, IAdministradorServico administradorServico) =>
{
    var adm = administradorServico.Login(loginDTO);
    if(adm != null)
    {
        string token = GerarTokenJwt(adm);
        return Results.Ok(new administradorLogado
        {
            Email = adm.Email,
            Perfil = adm.Perfil,
            Token = token
        });
        
    }
    else
    {
        return Results.Unauthorized();
    }
}).AllowAnonymous().WithTags("administrador");

app.MapGet("/administradores", ( [FromQuery] int? pagina  , IAdministradorServico administradorServico) =>
{
    var adms = new List<administradorModelView>();
    var administradores = administradorServico.Todos(pagina);

    foreach(var adm in administradores)
    {
        adms.Add(new administradorModelView
        {
            Id = adm.Id,
            Email = adm.Email,
            Perfil = adm.Perfil
        });
    }
    
    return Results.Ok(adms);
     
}).RequireAuthorization().RequireAuthorization(new AuthorizeAttribute
{
    Roles = "Adm"
}).WithTags("administrador");

app.MapGet("/administradores/{id}", ( [FromRoute] int id, IAdministradorServico administradorServico ) =>
{
    var administrador = administradorServico.BuscaPorId(id); 
    
    if(administrador == null)
    {
        return Results.NotFound();
    }

        return Results.Ok(new administradorModelView
        {
            Id = administrador.Id,
            Email = administrador.Email,
            Perfil = administrador.Perfil
        });
}).RequireAuthorization().RequireAuthorization(new AuthorizeAttribute
{
    Roles = "Adm"
}).WithTags("administrador");

app.MapPost("/administradores", ( [FromBody] AdministradorDTO administradorDTO, IAdministradorServico administradorServico) =>
{
    var validacao = new ErrosDeValidacao
    {
        Mensagens = new List<string>()
    };

    if(string.IsNullOrEmpty(administradorDTO.Email))
        validacao.Mensagens.Add("email nÃ£o pode ser vazio!");
    if(string.IsNullOrEmpty(administradorDTO.Senha))
        validacao.Mensagens.Add("senha nÃ£o pode ser vazia!");
    if( administradorDTO.Perfil == null)
        validacao.Mensagens.Add("Perfil nÃ£o pode ser vazio!");


    if(validacao.Mensagens.Count > 0) 
        return Results.BadRequest(validacao);

    
    var administrador = new Administrador
    {
        Email = administradorDTO.Email,
        Senha = administradorDTO.Senha,
        Perfil = administradorDTO.Perfil.ToString() ?? Perfil.editor.ToString()
    }; 
       administradorServico.Incluir(administrador);

        return Results.Created($"/administrador/{administrador.Id}", new administradorModelView
        {
            Id = administrador.Id,
            Email = administrador.Email,
            Perfil = administrador.Perfil
        });
    
        
}).RequireAuthorization().RequireAuthorization(new AuthorizeAttribute
{
    Roles = "Adm"
}).WithTags("administrador");

#endregion

#region Veiculos

ErrosDeValidacao validaDTO(VeiculoDTO veiculoDTO)
{
    var validacao = new ErrosDeValidacao
    {
        Mensagens = new List<string>()
    };

    if(string.IsNullOrEmpty(veiculoDTO.Nome))
        validacao.Mensagens.Add("o nome nÃ£o pode ser vazio");
    if(string.IsNullOrEmpty(veiculoDTO.Marca))
        validacao.Mensagens.Add("a marca nÃ£o pode ser vazia");
    if(veiculoDTO.Ano < 1950)
        validacao.Mensagens.Add("veiculo muito antigo, aceito somente anos superiores a 1950");

    return validacao;

}

app.MapPost("/veiculos", ( [FromBody] VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
{

    var validacao = validaDTO(veiculoDTO);

    if(validacao.Mensagens.Count > 0) 
        return Results.BadRequest(validacao);

    var veiculo = new Veiculo
    {
        Nome = veiculoDTO.Nome,
        Marca = veiculoDTO.Marca,
        Ano = veiculoDTO.Ano,
    }; 
       veiculoServico.Incluir(veiculo);

        return Results.Created($"/veiculo/{veiculo.Id}", veiculo);

}).RequireAuthorization().RequireAuthorization(new AuthorizeAttribute
{
    Roles = "Adm, Editor"
}).WithTags("veiculos");

app.MapGet("/veiculos", ( [FromQuery] int? pagina, IVeiculoServico veiculoServico ) =>
{
    var veiculos = veiculoServico.Todos(pagina); 
    

        return Results.Ok(veiculos);
}).WithTags("veiculos");

app.MapGet("/veiculos/{id}", ( [FromRoute] int id, IVeiculoServico veiculoServico ) =>
{
    var veiculo = veiculoServico.BuscaPorId(id); 
    
    if(veiculo == null)
    {
        return Results.NotFound();
    }

        return Results.Ok(veiculo);
}).RequireAuthorization().RequireAuthorization(new AuthorizeAttribute
{
    Roles = "Adm, Editor"
}).WithTags("veiculos");

app.MapPut("/veiculos/{id}", ( [FromRoute] int id,VeiculoDTO veiculoDTO , IVeiculoServico veiculoServico ) =>
{

    var veiculo = veiculoServico.BuscaPorId(id); 
    
    if(veiculo == null)
    {
        return Results.NotFound();
    }

    var validacao = validaDTO(veiculoDTO);

    if(validacao.Mensagens.Count > 0) 
        return Results.BadRequest(validacao);


        veiculo.Nome = veiculoDTO.Nome;     
        veiculo.Marca = veiculoDTO.Marca;
        veiculo.Ano = veiculoDTO.Ano;

        veiculoServico.Atualizar(veiculo);

        return Results.Ok(veiculo);

}).RequireAuthorization().RequireAuthorization(new AuthorizeAttribute
{
    Roles = "Adm"
}).WithTags("veiculos");

app.MapDelete("/veiculos/{id}", ( [FromRoute] int id, IVeiculoServico veiculoServico ) =>
{
    var veiculo = veiculoServico.BuscaPorId(id); 
    
    if(veiculo == null)
    {
        return Results.NotFound();
    }

        veiculoServico.Apagar(veiculo);

        return Results.NoContent();

}).RequireAuthorization().RequireAuthorization(new AuthorizeAttribute
{
    Roles = "Adm"
}).WithTags("veiculos");

#endregion

#region App
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.Run();

#endregion

