using Grpc.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApi.Entities;

namespace WebApi.Services;

public class UserService(UserManager<AccountEntity> userManager, IConfiguration configuration, ProtoProfileService.ProtoProfileServiceClient profileService) : AccountService.AccountServiceBase
{
    private readonly UserManager<AccountEntity> _userManager = userManager;
    private readonly ProtoProfileService.ProtoProfileServiceClient _profileService = profileService;
    private readonly IConfiguration _configuration = configuration;


    // Create
    // Sign up
    public override async Task<CreateAccountResponse> CreateAccount(CreateAccountRequest request, ServerCallContext context)
    {
        var account = new AccountEntity
        {
            UserName = request.Email,
            Email = request.Email
        };

        // Check if the account already exists
        var userExists = await _userManager.FindByEmailAsync(account.Email);
        if (userExists != null)
        {
            return new CreateAccountResponse
            {
                Success = false,
                Message = "Account with this email already exists"
            };
        }

        // Create the account
        var result = await _userManager.CreateAsync(account, request.Password);
        if (!result.Succeeded)
        {
            return new CreateAccountResponse
            {
                Success = false,
                Message = "Failed to create account"
            };
        }

        // Add user role to this account
        await _userManager.AddToRoleAsync(account, "User");

        // Create the profile
        var profileRequest = new AddEmptyProfileRequest
        {
            UserId = account.Email
        };
        var profileResponse = await _profileService.AddEmptyProfileAsync(profileRequest);
        Console.WriteLine($"Action - Tried to create an profile for userId: {account.Email}. Result: {profileResponse}");


        // Token
        var response = await GetAccount(new GetAccountRequest
        {
            Email = account.Email,
            Password = request.Password
        }, context);

        return new CreateAccountResponse
        {
            Success = result.Succeeded,
            Message = "Account created successfully",
            Token = response.Token
        };
    }

    // Read
    // Sign in
    public override async Task<GetAccountResponse> GetAccount(GetAccountRequest request, ServerCallContext context)
    {
        var account = new GetAccountRequest
        {
            Email = request.Email,
            Password = request.Password
        };

        // Check if the account exists
        var user = await _userManager.FindByEmailAsync(account.Email);
        if (user == null)
        {
            return new GetAccountResponse
            {
                Success = false,
                Message = "Account not found"
            };
        }

        // Check if the password is correct
        var result = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!result)
        {
            return new GetAccountResponse
            {
                Success = false,
                Message = "Invalid password"
            };
        }

        // Get the roles of the user
        var roles = await _userManager.GetRolesAsync(user);
        var roleList = new List<string>();
        foreach (var role in roles)
        {
            roleList.Add(role);
        }

        // Token
        var authClaims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserName!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        // Add roles to claims
        authClaims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        // Create the token
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            expires: DateTime.Now.AddMinutes(double.Parse(_configuration["Jwt:ExpiryMinutes"]!)),
            claims: authClaims,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)),
                SecurityAlgorithms.HmacSha256Signature)
            );

        // Write the token to a string
        var tokenHandler = new JwtSecurityTokenHandler().WriteToken(token);

        return new GetAccountResponse
        {
            Success = true,
            Message = "Successfully signed in to account",
            Roles = { roleList },
            Token = tokenHandler
        };
    }


    // Check if the account exists
    // Used when signing up
    public override async Task<CheckExistsResonse> Exists(CheckExistsRequest request, ServerCallContext context)
    {
        // Check if the account exists
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return new CheckExistsResonse
            {
                Success = false,
                Message = "Account not found"
            };
        }
        return new CheckExistsResonse
        {
            Success = true,
            Message = "Already exists"
        };
    }
}
