using System.ComponentModel.DataAnnotations;

namespace Admin.Application.DTOs;

public class CreateRoomRequest
{
    [Required(ErrorMessage = "Room name is required")]
    [MinLength(2, ErrorMessage = "Room name must be at least 2 characters")]
    [MaxLength(50, ErrorMessage = "Room name cannot exceed 50 characters")]
    [Display(Name = "Room Name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;
}
