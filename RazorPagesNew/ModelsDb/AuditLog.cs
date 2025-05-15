using System;
using System.Collections.Generic;

namespace RazorPagesNew.ModelsDb;

public partial class AuditLog
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public int ActionType { get; set; }

    public string EntityName { get; set; } = null!;

    public string EntityId { get; set; } = null!;

    public string Details { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
