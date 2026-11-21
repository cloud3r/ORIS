using CardSite.ORM.Attributes;

namespace CardSite.Models;

[Table("admins")]
public class AdminUser
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Column("passwordhash")]
    public string PasswordHash { get; set; } = string.Empty;
    
    [Column("email")]
    public string Email { get; set; } = string.Empty;
}