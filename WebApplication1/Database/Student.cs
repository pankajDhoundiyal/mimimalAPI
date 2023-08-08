using System;
using System.Collections.Generic;

namespace WebApplication1.Database;

public partial class Student
{
    public int StudentId { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public DateTime? Birthdate { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }
}
