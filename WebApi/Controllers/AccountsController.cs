using Microsoft.AspNetCore.Mvc;
using WebApi.Services;

namespace WebApi.Controllers;
[Route("api/[controller]")]
[ApiController]
public class AccountsController(IAccountService accountService) : ControllerBase
{
    private readonly IAccountService _accountService = accountService;
}
