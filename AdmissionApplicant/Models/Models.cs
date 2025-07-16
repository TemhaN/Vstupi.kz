using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdmissionSystem.Models
{
    public class AdmissionContext : DbContext
    {
        public DbSet<Applicant> Applicants { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamQuestion> ExamQuestions { get; set; }
        public DbSet<ExamResult> ExamResults { get; set; }
        public DbSet<Specialization> Specializations { get; set; }
        public DbSet<AdmissionApplication> Applications { get; set; }
        public DbSet<Faculty> Faculties { get; set; }
        public DbSet<Grant> Grants { get; set; }
        public DbSet<UserExamAttempt> UserExamAttempts { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<ApplicationExam> ApplicationExams { get; set; } // Новая таблица

        public AdmissionContext(DbContextOptions<AdmissionContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("Users");

            modelBuilder.Entity<ExamResult>()
                .HasOne(er => er.Application)
                .WithMany(a => a.ExamResults)
                .HasForeignKey(er => er.ApplicationID)
                .IsRequired();

            modelBuilder.Entity<ExamResult>()
                .HasOne(er => er.Specialization)
                .WithMany()
                .HasForeignKey(er => er.SpecializationID)
                .IsRequired();

            modelBuilder.Entity<UserExamAttempt>()
                .HasOne(uea => uea.Question)
                .WithMany()
                .HasForeignKey(uea => uea.QuestionID);

            modelBuilder.Entity<UserExamAttempt>()
                .HasOne(uea => uea.Application)
                .WithMany()
                .HasForeignKey(uea => uea.ApplicationID)
                .IsRequired();

            modelBuilder.Entity<AdmissionApplication>()
                .HasOne(a => a.Applicant)
                .WithMany()
                .HasForeignKey(a => a.ApplicantID)
                .IsRequired();

            modelBuilder.Entity<AdmissionApplication>()
                .HasOne(a => a.Exam)
                .WithMany()
                .HasForeignKey(a => a.ExamID);

            modelBuilder.Entity<AdmissionApplication>()
                .HasOne(a => a.Specialization)
                .WithMany()
                .HasForeignKey(a => a.SpecializationID)
                .IsRequired();

            modelBuilder.Entity<AdmissionApplication>()
                .HasOne(a => a.Faculty)
                .WithMany()
                .HasForeignKey(a => a.FacultyID)
                .IsRequired();

            // Настройка для ApplicationExams
            modelBuilder.Entity<ApplicationExam>()
                .HasOne(ae => ae.Application)
                .WithMany()
                .HasForeignKey(ae => ae.ApplicationID)
                .IsRequired();

            modelBuilder.Entity<ApplicationExam>()
                .HasOne(ae => ae.Exam)
                .WithMany()
                .HasForeignKey(ae => ae.ExamID)
                .IsRequired();
        }
    }

    public class Applicant
    {
        [Key]
        public int ApplicantID { get; set; }

        [StringLength(50)]
        public string? FirstName { get; set; } // Nullable

        [StringLength(50)]
        public string? LastName { get; set; } // Nullable

        public DateTime? DateOfBirth { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; } // Nullable

        [StringLength(200)]
        public string? Address { get; set; } // Nullable

        [StringLength(20)]
        [RegularExpression(@"^\+?\d{10,15}$")]
        public string? PhoneNumber { get; set; } // Nullable

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; } // Nullable

        [StringLength(20)]
        public string? PassportNumber { get; set; } // Nullable

        public DateTime RegistrationDate { get; set; }

        public decimal? CertificateScore { get; set; }

        public string FullName => $"{LastName ?? "Неизвестно"} {FirstName ?? "Неизвестно"}".Trim();
        public override string ToString() => FullName;
    }

    public class Exam
    {
        [Key]
        public int ExamID { get; set; }
        public int SpecializationID { get; set; }
        public string? ExamName { get; set; } // Nullable
        public Specialization? Specialization { get; set; }
        public List<ExamQuestion> Questions { get; set; } = new List<ExamQuestion>();
        public override string ToString() => ExamName ?? "Без названия";
    }

    public class ExamResult
    {
        [Key]
        public int ResultID { get; set; }
        public int ApplicationID { get; set; }
        public int SpecializationID { get; set; }
        public decimal? ExamScore { get; set; }
        public AdmissionApplication Application { get; set; }
        public Specialization Specialization { get; set; }
        public override string ToString() => $"Заявление {ApplicationID}: {ExamScore}";
    }

    public class Specialization
    {
        [Key]
        public int SpecializationID { get; set; }
        public string? SpecializationName { get; set; } // Nullable
        public int FacultyID { get; set; }
        public Faculty? Faculty { get; set; }
        public List<Grant> Grants { get; set; } = new List<Grant>();
        public override string ToString() => SpecializationName ?? "Без названия";
    }

    public class AdmissionApplication
    {
        [Key]
        public int ApplicationID { get; set; }
        public int ApplicantID { get; set; }
        public int SpecializationID { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string? Status { get; set; } // Nullable
        public int FacultyID { get; set; }
        public int? ExamID { get; set; }
        public string? FundingType { get; set; } // Nullable
        public Applicant? Applicant { get; set; }
        public Specialization? Specialization { get; set; }
        public Faculty? Faculty { get; set; }
        public Exam? Exam { get; set; }
        public List<ExamResult> ExamResults { get; set; } = new List<ExamResult>();
        public override string ToString() => $"Заявление {ApplicationID} от {Applicant?.FullName ?? "Неизвестно"}";
    }
    public class Faculty
    {
        [Key]
        public int FacultyID { get; set; }
        public string? FacultyName { get; set; } // Nullable
        public string? Description { get; set; } // Nullable
        public override string ToString() => FacultyName ?? "Без названия";
    }

    public class User
    {
        [Key]
        public int UserID { get; set; }
        public string UserName { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }
        public int? ApplicantID { get; set; }
        public DateTime? LastLogin { get; set; }
        public Applicant Applicant { get; set; }
        public override string ToString() => UserName;
    }

    public class Grant
    {
        public int GrantID { get; set; }
        public int SpecializationID { get; set; }
        public int TotalGrantPlaces { get; set; }
        public int AvailableGrantPlaces { get; set; }
        public Specialization Specialization { get; set; }
        public override string ToString() => $"Грант для {Specialization?.SpecializationName}";
    }

    public class UserExamAttempt
    {
        [Key]
        public int AttemptID { get; set; }
        public int ApplicationID { get; set; }
        public int ExamID { get; set; }
        public int? QuestionID { get; set; }
        public string UserAnswer { get; set; }
        public decimal Score { get; set; } // Изменено на decimal
        public DateTime AttemptDate { get; set; }
        public AdmissionApplication Application { get; set; }
        public Exam Exam { get; set; }
        public ExamQuestion Question { get; set; }
        public override string ToString() => $"Попытка {AttemptID} для {Application?.Applicant?.FullName}";
    }

    public class ExamQuestion
    {
        [Key]
        public int QuestionID { get; set; }
        public int ExamID { get; set; }
        public string Question { get; set; }
        public string Options { get; set; }
        public string CorrectAnswer { get; set; }
        public double MaxScore { get; set; }
        public Exam Exam { get; set; }
        public override string ToString() => Question;
    }

    public class ApplicationExam
    {
        [Key]
        public int ApplicationExamID { get; set; }
        public int ApplicationID { get; set; }
        public int ExamID { get; set; }
        public DateTime AssignedDate { get; set; }
        public AdmissionApplication Application { get; set; }
        public Exam Exam { get; set; }
    }

    public class JwtToken
    {
        public string Token { get; set; }
        public DateTime Expiry { get; set; }
    }
    public class ExamStatus
    {
        public bool HasStarted { get; set; }
        public bool HasCompleted { get; set; }
        public bool IsBlocked { get; set; }
        public bool HasExamAssigned { get; set; }
        public int Attempts { get; set; }
        public List<decimal> PastScores { get; set; }
        public int MaxExamScore { get; set; }
    }
}