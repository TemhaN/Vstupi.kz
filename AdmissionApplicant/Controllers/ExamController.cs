using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdmissionSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AdmissionSystem.Controllers
{
    public class ExamController : Controller
    {
        private readonly AdmissionContext _context;

        public ExamController(AdmissionContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Start(int applicationId)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId.Value);
            if (user == null || !user.ApplicantID.HasValue)
                return Unauthorized("Пользователь не найден или ApplicantID отсутствует.");

            var application = await _context.Applications
                .Include(a => a.Specialization)
                .FirstOrDefaultAsync(a => a.ApplicationID == applicationId);

            if (application == null)
                return NotFound($"Заявка с ID {applicationId} не найдена.");
            if (application.ApplicantID != user.ApplicantID)
                return Unauthorized("Заявка принадлежит другому пользователю.");

            var applicationExam = await _context.ApplicationExams
                .Include(ae => ae.Exam)
                .ThenInclude(e => e.Questions)
                .FirstOrDefaultAsync(ae => ae.ApplicationID == applicationId);

            if (applicationExam == null)
                return BadRequest("Экзамен не назначен для этой заявки.");

            var exam = applicationExam.Exam;
            if (!exam.Questions.Any())
                return NotFound("Вопросы для экзамена не найдены.");

            var attempts = await _context.UserExamAttempts
                .Where(a => a.ApplicationID == applicationId && a.Application.ApplicantID == user.ApplicantID)
                .GroupBy(a => a.AttemptDate.Date)
                .CountAsync();

            var isBlocked = await _context.UserExamAttempts
                .AnyAsync(a => a.ApplicationID == applicationId && a.UserAnswer == "Blocked" && a.Score == 0);

            if (attempts >= 2 || isBlocked || HttpContext.Session.GetString($"ExamBlocked_{applicationId}") == "true")
            {
                Console.WriteLine($"Блокировка: applicationId={applicationId}, attempts={attempts}, isBlocked={isBlocked}, sessionBlocked={HttpContext.Session.GetString($"ExamBlocked_{applicationId}")}");
                return RedirectToAction("Blocked");
            }

            HttpContext.Session.SetInt32($"ExamStartTime_{applicationId}", (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds);
            HttpContext.Session.SetInt32($"CheatAttempts_{applicationId}", 0);

            var pastAnswers = await _context.UserExamAttempts
                .Where(a => a.ApplicationID == applicationId)
                .Select(a => new { a.QuestionID, a.UserAnswer, Score = a.Score, a.AttemptDate })
                .ToListAsync();

            var pastScores = await _context.UserExamAttempts
                .Where(a => a.ApplicationID == applicationId)
                .GroupBy(a => a.AttemptDate.Date)
                .Select(g => g.Sum(a => a.Score))
                .ToListAsync();

            ViewBag.ApplicationId = applicationId;
            ViewBag.PastAnswers = pastAnswers.Cast<object>().ToList();
            ViewBag.PastScores = pastScores;
            ViewBag.TotalQuestions = exam.Questions.Count;
            ViewBag.AttemptsUsed = attempts;

            return View(exam.Questions);
        }

        [HttpPost]
        public async Task<IActionResult> Submit(int applicationId, Dictionary<int, string> answers = null)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId.Value);
            if (user == null || !user.ApplicantID.HasValue)
                return Unauthorized("Пользователь не найден или ApplicantID отсутствует.");

            var application = await _context.Applications
                .Include(a => a.Specialization)
                .FirstOrDefaultAsync(a => a.ApplicationID == applicationId);

            if (application == null)
                return NotFound($"Заявка с ID {applicationId} не найдена.");
            if (application.ApplicantID != user.ApplicantID)
                return Unauthorized("Заявка принадлежит другому пользователю.");

            var applicationExam = await _context.ApplicationExams
                .Include(ae => ae.Exam)
                .ThenInclude(e => e.Questions)
                .FirstOrDefaultAsync(ae => ae.ApplicationID == applicationId);

            if (applicationExam == null)
                return BadRequest("Экзамен не назначен для этой заявки.");

            var exam = applicationExam.Exam;
            var allQuestions = exam.Questions;

            if (answers != null && answers.Any())
            {
                foreach (var answer in answers.Where(a => !string.IsNullOrEmpty(a.Value)))
                {
                    var question = allQuestions.FirstOrDefault(q => q.QuestionID == answer.Key);
                    if (question != null)
                    {
                        var userAnswer = answer.Value.Length == 1 && "ABCD".Contains(answer.Value) ? answer.Value : answer.Value.Substring(0, 1);
                        Console.WriteLine($"[Submit] QuestionID={answer.Key}, RawAnswer={answer.Value}, UserAnswer={userAnswer}, CorrectAnswer={question.CorrectAnswer}, MaxScore={question.MaxScore}");
                        _context.UserExamAttempts.Add(new UserExamAttempt
                        {
                            ApplicationID = applicationId,
                            ExamID = exam.ExamID,
                            QuestionID = answer.Key,
                            UserAnswer = userAnswer,
                            Score = question.CorrectAnswer == userAnswer ? (decimal)question.MaxScore : 0,
                            AttemptDate = DateTime.Now
                        });
                    }
                }

                var totalScore = await _context.UserExamAttempts
                    .Where(a => a.ApplicationID == applicationId && a.AttemptDate.Date == DateTime.Now.Date)
                    .SumAsync(a => a.Score);

                var examResult = await _context.ExamResults
                    .FirstOrDefaultAsync(er => er.ApplicationID == applicationId);
                if (examResult != null)
                {
                    examResult.ExamScore = totalScore;
                    _context.ExamResults.Update(examResult);
                }
                else
                {
                    _context.ExamResults.Add(new ExamResult
                    {
                        ApplicationID = applicationId,
                        SpecializationID = application.SpecializationID,
                        ExamScore = totalScore
                    });
                }
            }
            else
            {
                Console.WriteLine($"[Submit] Нет ответов, записываем NoAnswers для applicationId={applicationId}");
                _context.UserExamAttempts.Add(new UserExamAttempt
                {
                    ApplicationID = applicationId,
                    ExamID = exam.ExamID,
                    QuestionID = 0,
                    UserAnswer = "NoAnswers",
                    Score = 0,
                    AttemptDate = DateTime.Now
                });
            }

            application.Status = "PendingExam";
            _context.Applications.Update(application);
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove($"ExamStartTime_{applicationId}");
            HttpContext.Session.Remove($"CheatAttempts_{applicationId}");
            return RedirectToAction("Index", "Application");
        }

        [HttpPost]
        public async Task<IActionResult> LogCheatAttempt(int applicationId)
        {
            try
            {
                var cheatAttempts = HttpContext.Session.GetInt32($"CheatAttempts_{applicationId}") ?? 0;
                cheatAttempts++;
                HttpContext.Session.SetInt32($"CheatAttempts_{applicationId}", cheatAttempts);
                Console.WriteLine($"[LogCheatAttempt] Попытка списывания: applicationId={applicationId}, cheatAttempts={cheatAttempts}");

                if (cheatAttempts >= 3)
                {
                    HttpContext.Session.SetString($"ExamBlocked_{applicationId}", "true");
                    var application = await _context.Applications
                        .FirstOrDefaultAsync(a => a.ApplicationID == applicationId);
                    if (application == null)
                    {
                        Console.WriteLine($"[LogCheatAttempt] Ошибка: Заявка с ID {applicationId} не найдена.");
                        return Json(new { blocked = true, error = "Application not found" });
                    }

                    var applicationExam = await _context.ApplicationExams
                        .FirstOrDefaultAsync(ae => ae.ApplicationID == applicationId);
                    if (applicationExam == null)
                    {
                        Console.WriteLine($"[LogCheatAttempt] Ошибка: Экзамен для ApplicationID {applicationId} не найден.");
                        return Json(new { blocked = true, error = "Exam not found" });
                    }

                    _context.UserExamAttempts.Add(new UserExamAttempt
                    {
                        ApplicationID = applicationId,
                        ExamID = applicationExam.ExamID,
                        QuestionID = 0,
                        UserAnswer = "Blocked",
                        Score = 0,
                        AttemptDate = DateTime.Now
                    });
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"[LogCheatAttempt] Блокировка записана: applicationId={applicationId}");
                    return Json(new { blocked = true, cheatAttempts });
                }

                Console.WriteLine($"[LogCheatAttempt] Возвращаем: blocked=false, cheatAttempts={cheatAttempts}");
                return Json(new { blocked = false, cheatAttempts });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LogCheatAttempt] Ошибка: {ex.Message}, StackTrace: {ex.StackTrace}");
                return Json(new { blocked = false, error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult UpdateStartTime(int applicationId, int startTime)
        {
            try
            {
                HttpContext.Session.SetInt32($"ExamStartTime_{applicationId}", startTime);
                Console.WriteLine($"Время старта обновлено: applicationId={applicationId}, startTime={startTime}");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в UpdateStartTime: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }

        public IActionResult Blocked() => View();
    }
}