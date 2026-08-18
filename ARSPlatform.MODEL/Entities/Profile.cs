using System;
using System.Collections.Generic;

namespace ARSPlatform.MODEL.Entities;

public partial class Profile
{
    public int UserId { get; set; }
    public string? PhoneNumber { get; set; }
    public string? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string? FullName { get; set; }
    public string? AcademicTitle { get; set; }
    public string? Institution { get; set; }
    public string? Bio { get; set; }
    public string? Keywords { get; set; }
    public string? AvatarInitials { get; set; }
    public virtual User User { get; set; } = null!;
}
