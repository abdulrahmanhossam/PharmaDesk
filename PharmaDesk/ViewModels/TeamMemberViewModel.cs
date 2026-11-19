using System.ComponentModel.DataAnnotations;

namespace PharmaDesk.ViewModels
{
    public class TeamMemberViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required.")]
        public string Role { get; set; } = "Staff"; // Admin or Staff

        public bool IsActive { get; set; } = true;

        // هذا الحقل مخصص لإدخال الـ PIN فقط عند الإنشاء أو التعديل
        // (لا نعرض الـ PinHash أبداً)
        [StringLength(10, MinimumLength = 4, ErrorMessage = "PIN must be between 4 and 10 characters.")]
        public string? NewPin { get; set; }
    }
}