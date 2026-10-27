using DG_viajes.Models;
using DG_viajes.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace DG_viajes.Controllers;

public class DestinosController : Controller
{
    private readonly IRepositorioDestino repositorio;

    public DestinosController(IRepositorioDestino repositorio)
    {
        this.repositorio = repositorio;
    }
}