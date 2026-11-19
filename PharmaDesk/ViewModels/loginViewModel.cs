using System.ComponentModel.DataAnnotations;

namespace PharmaDesk.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Please select a user.")]
        public int TeamMemberId { get; set; }

        [Required(ErrorMessage = "PIN is required.")]
        [DataType(DataType.Password)]
        public string Pin { get; set; } = string.Empty;

        // This property will hold the list of users for the dropdown in the login view
        public List<PharmaDesk.Models.TeamMember> Users { get; set; } = new List<PharmaDesk.Models.TeamMember>();
    }
}