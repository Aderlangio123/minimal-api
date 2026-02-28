using MinimalApi.Dominio.Entidades;
using MinimalApi.DTOs;

namespace MinimalApi.Dominio.Servicos.Interfaces;

public interface IAdministradorServico
{
    Administrador? Login(loginDTO loginDTO);
    Administrador? Incluir(Administrador administrador);

    List<Administrador> Todos(int? pagina);
    Administrador?  BuscaPorId(int id);
}