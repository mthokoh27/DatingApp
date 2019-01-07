using DatingApp.API.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DatingApp.API.Models;
using DatingApp.API.Dtos;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;
using System;
using System.IdentityModel.Tokens.Jwt;

namespace DatingApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController: ControllerBase
    {
        private readonly IAuthRepository _repo;
        private readonly IConfiguration _config;

        public AuthController (IAuthRepository repo, IConfiguration config)
        {
            _config=config;
            _repo=repo;
        }

        [HttpPost("register")]
        public async Task <IActionResult> Register(UserForRegisterDto userForRegister)
        {
            //validate user request
            userForRegister.Username=userForRegister.Username.ToLower();

            if (await _repo.UserExists(userForRegister.Username))
            {
                return BadRequest("User already exist!");
            }

            var UserToCreate= new User
            {
                Username=userForRegister.Username
            };

            var CreatedUser= await _repo.Register(UserToCreate, userForRegister.Password);

            return StatusCode(201);
        }

        [HttpPost("login")]
        public async Task <IActionResult> Login(UserForLoginDto userForLogin)
        {
            var userFromRepo= await _repo.Login(userForLogin.Username.ToLower(), userForLogin.Password);

            if (userForLogin==null)
                return Unauthorized();

            var claims=new []
            {
                new Claim(ClaimTypes.NameIdentifier, userFromRepo.Id.ToString()),
                new Claim(ClaimTypes.Name, userFromRepo.Username) 
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8
            .GetBytes(_config.GetSection("AppSettings:Token").Value));

            var creds= new SigningCredentials (key, SecurityAlgorithms.HmacSha512Signature);
            
            var tokenDescriptor=new SecurityTokenDescriptor
            {
                Subject=new ClaimsIdentity(claims),
                Expires=DateTime.Now.AddDays(1),
                SigningCredentials=creds
            };

            var tokenHandler=new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return Ok(new {
                token=tokenHandler.WriteToken(token)
            });

        }
    }
}