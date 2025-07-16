using Microsoft.AspNetCore.Mvc;
using AdmissionSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using BCrypt.Net;
using System.Text.RegularExpressions;

namespace AdmissionSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly AdmissionContext _context;

        public AccountController(AdmissionContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(string username, string password, string email, string phoneNumber, string firstName, string lastName, decimal certificateScore)
        {
            if (await _context.Users.AnyAsync(u => u.UserName == username))
            {
                ModelState.AddModelError("username", "Пользователь с таким именем уже существует");
                return View();
            }

            if (phoneNumber.Length > 20)
            {
                ModelState.AddModelError("phoneNumber", "Номер телефона не может превышать 20 символов");
                return View();
            }
            if (!Regex.IsMatch(phoneNumber, @"^\+?\d{10,15}$"))
            {
                ModelState.AddModelError("phoneNumber", "Введите корректный номер телефона (10-15 цифр, может начинаться с +)");
                return View();
            }

            if (email.Length > 100)
            {
                ModelState.AddModelError("email", "Email не может превышать 100 символов");
                return View();
            }

            if (firstName.Length > 50 || lastName.Length > 50)
            {
                ModelState.AddModelError(string.Empty, "Имя и фамилия не могут превышать 50 символов");
                return View();
            }

            if (certificateScore < 0 || certificateScore > 100)
            {
                ModelState.AddModelError("certificateScore", "Балл аттестата должен быть от 0 до 100");
                return View();
            }

            var applicant = new Applicant
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PhoneNumber = phoneNumber,
                RegistrationDate = DateTime.Now,
                CertificateScore = certificateScore
            };
            _context.Applicants.Add(applicant);
            await _context.SaveChangesAsync();

            var user = new User
            {
                UserName = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                ApplicantID = applicant.ApplicantID,
                Applicant = applicant,
                Role = "User",
                LastLogin = null
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            HttpContext.Session.SetInt32("UserID", user.UserID);
            return RedirectToAction("Index", "Application");
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                user.LastLogin = DateTime.Now;
                await _context.SaveChangesAsync();

                HttpContext.Session.SetInt32("UserID", user.UserID);
                return RedirectToAction("Index", "Application");
            }
            ModelState.AddModelError(string.Empty, "Неверное имя пользователя или пароль");
            return View();
        }

        [HttpGet]
        public IActionResult Logout() => View();

        [HttpPost]
        public IActionResult LogoutConfirmed()
        {
            HttpContext.Session.Remove("UserID");
            return RedirectToAction("LoggedOut");
        }

        [HttpGet]
        public IActionResult LoggedOut() => View();
    }
}