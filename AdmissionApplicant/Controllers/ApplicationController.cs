using Microsoft.AspNetCore.Mvc;
using AdmissionSystem.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AdmissionSystem.Controllers
{
    public class ApplicationController : Controller
    {
        private readonly AdmissionContext _context;
        private const bool AllowApplicationsForTesting = true;

        public ApplicationController(AdmissionContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserID == userId.Value);
            if (user == null || !user.ApplicantID.HasValue)
                return RedirectToAction("Login", "Account");

            var applications = await _context.Applications
                .Where(a => a.ApplicantID == user.ApplicantID.Value)
                .Include(a => a.Applicant)
                .Include(a => a.Specialization)
                .Include(a => a.Faculty)
                .Include(a => a.Exam)
                .ToListAsync();

            applications = applications
                .Where(a =>
                    a.Specialization != null &&
                    !string.IsNullOrEmpty(a.Specialization.SpecializationName ?? "") &&
                    a.Faculty != null &&
                    !string.IsNullOrEmpty(a.Faculty.FacultyName ?? ""))
                .ToList();

            var examStatuses = new Dictionary<int, object>();
            foreach (var app in applications)
            {
                var attempts = await _context.UserExamAttempts
                    .Where(a => a.ApplicationID == app.ApplicationID && a.Application.ApplicantID == user.ApplicantID.Value)
                    .GroupBy(a => a.AttemptDate.Date)
                    .CountAsync();

                var pastScores = await _context.UserExamAttempts
                    .Where(a => a.ApplicationID == app.ApplicationID)
                    .GroupBy(a => a.AttemptDate.Date)
                    .Select(g => g.Sum(a => a.Score))
                    .ToListAsync();

                var exam = await _context.Exams
                    .Where(e => e.SpecializationID == app.SpecializationID)
                    .Include(e => e.Questions)
                    .FirstOrDefaultAsync();

                // Рассчитываем максимальный балл как сумму MaxScore всех вопросов
                int maxExamScore = 0; // По умолчанию 0, если экзамен или вопросы отсутствуют
                if (exam != null && exam.Questions != null && exam.Questions.Any())
                {
                    double totalMaxScore = exam.Questions.Sum(q => q.MaxScore);
                    maxExamScore = totalMaxScore > 0 ? (int)Math.Ceiling(totalMaxScore) : 0; // Округляем вверх
                }

                // Форматируем pastScores в список строк
                var formattedScores = pastScores.Select(score => $"{(int)score}/{maxExamScore}").ToList();

                var isBlocked = HttpContext.Session.GetString($"ExamBlocked_{app.ApplicationID}") == "true";
                var hasStarted = HttpContext.Session.GetInt32($"ExamStartTime_{app.ApplicationID}") != null || attempts > 0;
                var hasCompleted = attempts > 0;
                var hasExamAssigned = await _context.ApplicationExams
                    .AnyAsync(ae => ae.ApplicationID == app.ApplicationID);

                examStatuses[app.ApplicationID] = new
                {
                    hasStarted,
                    hasCompleted,
                    isBlocked,
                    hasExamAssigned,
                    Attempts = attempts,
                    formattedScores,
                    maxExamScore
                };
            }

            ViewBag.Faculties = await GetFacultiesAsync();
            ViewBag.CanCreateApplication = !applications.Any() && AllowApplicationsForTesting;
            ViewBag.ExamStatuses = examStatuses;

            return View(applications);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var user = await _context.Users
                .Include(u => u.Applicant)
                .FirstOrDefaultAsync(u => u.UserID == userId.Value);
            if (user == null || !user.ApplicantID.HasValue)
                return RedirectToAction("Login", "Account");

            var hasApplication = await _context.Applications.AnyAsync(a => a.ApplicantID == user.ApplicantID.Value);
            if (hasApplication)
                return RedirectToAction("Index");

            if (!AllowApplicationsForTesting)
                return RedirectToAction("Index");

            ViewBag.Faculties = await GetFacultiesAsync();
            return View(user.Applicant);
        }

        [HttpPost]
        public async Task<IActionResult> Create(int specializationId, int facultyId, Applicant applicantModel)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var user = await _context.Users
                .Include(u => u.Applicant)
                .FirstOrDefaultAsync(u => u.UserID == userId.Value);
            if (user == null || !user.ApplicantID.HasValue || user.ApplicantID != applicantModel.ApplicantID)
                return Unauthorized();

            var hasApplication = await _context.Applications.AnyAsync(a => a.ApplicantID == user.ApplicantID.Value);
            if (hasApplication)
                return RedirectToAction("Index");

            var specialization = await _context.Specializations
                .FirstOrDefaultAsync(s => s.SpecializationID == specializationId && s.FacultyID == facultyId);
            if (specialization == null || !await _context.Faculties.AnyAsync(f => f.FacultyID == facultyId))
            {
                ModelState.AddModelError(string.Empty, "Недопустимая специальность или факультет");
                ViewBag.Faculties = await GetFacultiesAsync();
                return View(applicantModel);
            }

            if (string.IsNullOrEmpty(applicantModel.Gender) ||
                string.IsNullOrEmpty(applicantModel.Address) ||
                string.IsNullOrEmpty(applicantModel.PassportNumber) ||
                string.IsNullOrEmpty(applicantModel.PhoneNumber) ||
                string.IsNullOrEmpty(applicantModel.Email))
            {
                ModelState.AddModelError(string.Empty, "Заполните все контактные данные");
                ViewBag.Faculties = await GetFacultiesAsync();
                return View(applicantModel);
            }

            var applicant = await _context.Applicants.FindAsync(user.ApplicantID.Value);
            applicant.Gender = applicantModel.Gender;
            applicant.Address = applicantModel.Address;
            applicant.PassportNumber = applicantModel.PassportNumber;
            applicant.PhoneNumber = applicantModel.PhoneNumber;
            applicant.Email = applicantModel.Email;
            applicant.DateOfBirth = applicantModel.DateOfBirth;

            var application = new AdmissionApplication
            {
                ApplicantID = user.ApplicantID.Value,
                SpecializationID = specializationId,
                FacultyID = facultyId,
                SubmissionDate = DateTime.Now,
                Status = "Submitted",
                FundingType = "Ожидает"
            };

            _context.Applications.Add(application);
            await _context.SaveChangesAsync();

            var examResult = new ExamResult
            {
                ApplicationID = application.ApplicationID,
                SpecializationID = specializationId,
                ExamScore = applicant.CertificateScore
            };
            _context.ExamResults.Add(examResult);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> GetSpecializationsByFaculty(int facultyId)
        {
            var specializations = await _context.Specializations
                .Where(s => s.FacultyID == facultyId && !string.IsNullOrEmpty(s.SpecializationName ?? ""))
                .Select(s => new { s.SpecializationID, s.SpecializationName })
                .ToListAsync();
            return Json(specializations);
        }

        private async Task<List<Faculty>> GetFacultiesAsync()
        {
            return await _context.Faculties
                .Where(f => !string.IsNullOrEmpty(f.FacultyName))
                .ToListAsync();
        }
    }
}