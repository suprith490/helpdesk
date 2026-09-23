using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Application.DTOs.Comments;

public class CreateCommentRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(2000)]
    public string Body { get; set; } = string.Empty;

    public bool IsInternal { get; set; }
}
