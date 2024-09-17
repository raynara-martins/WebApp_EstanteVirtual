﻿
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;
using WebApp_EstanteVirtual.Data;
using WebApp_EstanteVirtual.Models;
using Microsoft.EntityFrameworkCore;
using WebApp_EstanteVirtual.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace WebApp_EstanteVirtual.Controllers
{
    public class AccountController : Controller
    { 
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Action para a página de registro
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new Usuario
                {
                    Id = Guid.NewGuid().ToString(), 
                    Nome = model.UserName,
                    Email = model.Email,
                    CPF = model.CPF,
                    Senha = CryptographyService.EncryptPassword(model.Password),
                    IsAdmin = false
                };

                _context.Usuarios.Add(user);
                await _context.SaveChangesAsync();

                // Definir o cookie de autenticação manualmente
                HttpContext.Session.SetString("UserId", user.Id);

                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user != null && IsPasswordValid(user.Senha, model.Password))
                {
                    
                    var claims = new[]
                    {
                    new Claim(ClaimTypes.Name, user.Nome),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.IsAdmin == true ? "Admin" : "User"),
                    new Claim(ClaimTypes.NameIdentifier, user.Id)
                };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    
                    HttpContext.Session.SetString("UserId", user.Id);

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Tentativa de login inválida. Verifique os dados e tente novamente.");
                }
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear(); 

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> EditarConta()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Usuarios.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var model = new EditProfileViewModel
            {
                Nome = user.Nome,
                Email = user.Email,
                //Senha = user.Senha,
                Telefone = user.Telefone,
                Endereco = user.Endereco,
                CEP = user.CEP,
                NumeroCartao = user.NumeroCartao
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarConta(EditProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (userId == null)
                {
                    return RedirectToAction("Login");
                }

                var user = await _context.Usuarios.FindAsync(userId);
                if (user == null)
                {
                    return NotFound();
                }

                user.Nome = model.Nome;
                user.Email = model.Email;

                if (!string.IsNullOrEmpty(model.Telefone))
                {
                    user.Telefone = model.Telefone;
                }

                if (!string.IsNullOrEmpty(model.Endereco))
                {
                    user.Endereco = model.Endereco;
                }

                if (!string.IsNullOrEmpty(model.CEP))
                {
                    user.CEP = model.CEP;
                }

                if (!string.IsNullOrEmpty(model.NumeroCartao))
                {
                    user.NumeroCartao = model.NumeroCartao;
                }

                _context.Usuarios.Update(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Dados atualizados com sucesso!";
                return RedirectToAction("EditarConta");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Usuarios.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            _context.Usuarios.Remove(user);
            await _context.SaveChangesAsync();

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();

            return RedirectToAction("Index", "Home");
        }

        private bool IsPasswordValid(string storedPassword, string password)
        {
            return CryptographyService.EncryptPassword(password).Equals(storedPassword);
        }
    }
}

